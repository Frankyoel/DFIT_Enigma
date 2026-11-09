// Dfit/Services/PagoService.cs
// Implementación de lógica financiera, cobranzas y renovación de membresías en DFIT Power.
// Integra PaymentContext (Strategy Pattern) y actualiza estados de vigencia según ReglasMembresia.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dfit.Models;
using Dfit.Models.Common;
using Dfit.Models.Patterns.Strategy;
using Dfit.Repositories.Contracts;
using Dfit.Utils;

namespace Dfit.Services
{
    // Servicio de aplicación para cobros, balance financiero e historial de pagos.
    // Coordina la extensión de suscripciones activas y la generación de reportes mensuales.
    public class PagoService : IPagoService
    {
        // Repositorios inyectados para operaciones de cobranza y usuarios.
        private readonly IPagoRepository _pagoRepo;
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly IMembresiaRepository _membresiaRepo;
        private readonly IMembresiaUsuarioRepository _memUsuarioRepo;
        private readonly PaymentContext _paymentContext;

        // Constructor con inyección de dependencias de repositorios y contexto de pago.
        public PagoService(
            IPagoRepository pagoRepo,
            IUsuarioRepository usuarioRepo,
            IMembresiaRepository membresiaRepo,
            IMembresiaUsuarioRepository memUsuarioRepo,
            PaymentContext paymentContext)
        {
            _pagoRepo = pagoRepo;
            _usuarioRepo = usuarioRepo;
            _membresiaRepo = membresiaRepo;
            _memUsuarioRepo = memUsuarioRepo;
            _paymentContext = paymentContext;
        }

        // Obtiene datos del socio y lista de planes para la vista de registro de cobro.
        public async Task<(Usuario? Usuario, IReadOnlyList<Membresia> Membresias)> ObtenerDatosParaRegistrarPagoAsync(string usuarioId)
        {
            // Recupera la entidad del socio a partir de su ID.
            var usuario = await _usuarioRepo.GetByIdAsync(usuarioId);

            // Carga los planes de membresía activos disponibles para venta.
            var membresias = await _membresiaRepo.GetActivasAsync();

            return (usuario, membresias);
        }

        // Procesa el pago, aplica reglas de descuento por Strategy y renueva la membresía del socio.
        public async Task<(bool Exito, string Mensaje)> ProcesarPagoAsync(Pago model)
        {
            // Cláusula de guarda para evitar procesamiento con payload nulo.
            if (model == null)
            {
                return (false, "Datos de pago inválidos.");
            }

            // 1. Aplica el cálculo de descuento según PaymentContext (Strategy Pattern).
            model.Monto = _paymentContext.ExecutePaymentForAmount(model.Monto);
            model.FechaPago = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(model.MetodoPago))
            {
                model.MetodoPago = MetodosPago.Efectivo;
            }

            // Guarda el registro transaccional del pago en Firestore.
            await _pagoRepo.AddAsync(model);

            // 2. Consulta el plan adquirido para calcular la duración.
            var plan = await _membresiaRepo.GetByIdAsync(model.MembresiaId);
            if (plan == null)
            {
                return (true, $"Pago de S/ {model.Monto:N2} registrado.");
            }

            // 3. Busca si el socio tiene una membresía activa actual para calcular fecha de extensión.
            var asignacionesActivas = await _memUsuarioRepo.GetByUsuarioIdAsync(model.UsuarioId, soloActivas: true);
            DateTime? fechaFinActual = null;
            var ahora = DateTime.UtcNow;

            // Identifica si hay una vigencia remanente para extenderla sin pérdida de días.
            foreach (var doc in asignacionesActivas)
            {
                if (doc.FechaFin > ahora)
                {
                    fechaFinActual = doc.FechaFin;
                    break;
                }
            }

            // 4. Calcula nuevo período de vigencia sin solapamiento de fechas.
            var (fechaInicio, fechaFin) = ReglasMembresia.CalcularPeriodoVigencia(fechaFinActual, plan.DuracionDias, ahora);

            // Desactiva membresías anteriores del socio para garantizar una única activa.
            await _memUsuarioRepo.DesactivarMembresiasPreviasAsync(model.UsuarioId);

            // Registra la nueva suscripción con la vigencia actualizada.
            await _memUsuarioRepo.AddAsync(new MembresiaUsuario
            {
                UsuarioId = model.UsuarioId,
                MembresiaId = model.MembresiaId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                Activa = true
            });

            // Genera mensaje formateado mediante el catálogo i18n.
            var mensajeExito = string.Format(MensajesNotificacion.PagoRegistradoExito, model.Monto, plan.Nombre, fechaFin);
            return (true, mensajeExito);
        }

        // Obtiene el historial de pagos de un socio con los datos del plan asociado.
        public async Task<IReadOnlyList<Pago>> ObtenerPagosUsuarioAsync(string usuarioId)
        {
            // Recupera las transacciones del socio.
            var pagos = await _pagoRepo.GetByUsuarioIdAsync(usuarioId);

            // Carga catálogo de planes en memoria para vincular nombres en O(1).
            var planes = await _membresiaRepo.GetAllAsync();
            var planesDict = planes.ToDictionary(p => p.Id, p => p);

            // Asocia la entidad Membresia a cada pago.
            foreach (var p in pagos)
            {
                if (planesDict.TryGetValue(p.MembresiaId, out var plan))
                {
                    p.Membresia = plan;
                }
            }

            return pagos;
        }

        // Genera el reporte financiero consolidado con ingresos acumulados y del mes actual.
        public async Task<(IReadOnlyList<Pago> Pagos, decimal IngresosTotales, decimal PagosDelMes)> ObtenerReporteFinancieroAsync()
        {
            // Recupera la totalidad de pagos, usuarios y planes.
            var pagos = await _pagoRepo.GetAllAsync();
            var usuarios = await _usuarioRepo.GetAllAsync();
            var planes = await _membresiaRepo.GetAllAsync();

            // Mapea diccionarios en memoria para resolución rápida sin consultas adicionales.
            var usuariosDict = usuarios.ToDictionary(u => u.Id, u => u);
            var planesDict = planes.ToDictionary(p => p.Id, p => p);

            var ahora = DateTime.UtcNow;
            decimal ingresosTotales = 0;
            decimal pagosDelMes = 0;

            // Itera las transacciones calculando totales y acumulados mensuales.
            foreach (var p in pagos)
            {
                // Vincula el socio asociado a la transacción.
                if (usuariosDict.TryGetValue(p.UsuarioId, out var usuario))
                {
                    p.Usuario = usuario;
                }

                // Vincula el plan adquirido.
                if (planesDict.TryGetValue(p.MembresiaId, out var plan))
                {
                    p.Membresia = plan;
                }

                // Suma al balance histórico y del mes corriente.
                ingresosTotales += p.Monto;
                if (p.FechaPago.Month == ahora.Month && p.FechaPago.Year == ahora.Year)
                {
                    pagosDelMes += p.Monto;
                }
            }

            // Ordena descendentemente por fecha para priorizar los pagos más recientes.
            var listaOrdenada = pagos.OrderByDescending(p => p.FechaPago).ToList();
            return (listaOrdenada, ingresosTotales, pagosDelMes);
        }
    }
}
