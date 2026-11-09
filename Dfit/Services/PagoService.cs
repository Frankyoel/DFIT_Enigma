// Dfit/Services/PagoService.cs
// Implementación de lógica de negocio para finanzas, pasarela de pago y reportería.
// Integra el patrón Strategy (PaymentContext) y la actualización atómica de vigencias.
// Cumple con la directriz de profundidad condicional <= 2 niveles y cero código muerto.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dfit.Models;
using Dfit.Models.Patterns.Strategy;
using Dfit.Repositories.Contracts;
using Dfit.Utils;

namespace Dfit.Services
{
    // Servicio de aplicación para administración de transacciones, caja y reportería financiera.
    public class PagoService : IPagoService
    {
        // Repositorios y contexto de pago inyectados.
        private readonly IPagoRepository _pagoRepo;
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly IMembresiaRepository _membresiaRepo;
        private readonly IMembresiaUsuarioRepository _memUsuarioRepo;
        private readonly PaymentContext _paymentContext;

        // Constructor con inyección de dependencias.
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

        // Obtiene los datos del socio y los planes activos para inicializar la vista de cobro.
        public async Task<(Usuario? Usuario, IReadOnlyList<Membresia> Membresias)> ObtenerDatosRegistrarPagoAsync(string usuarioId)
        {
            // Cláusula de guarda para validar el identificador del socio.
            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return (null, Array.Empty<Membresia>());
            }

            // Consulta concurrente o secuencial del usuario y planes vigentes.
            var usuario = await _usuarioRepo.GetByIdAsync(usuarioId);
            var planesActivos = await _membresiaRepo.GetActivasAsync();

            return (usuario, planesActivos);
        }

        // Procesa el pago aplicando la estrategia de descuento y actualiza la vigencia de la membresía.
        public async Task<(bool Exito, string Mensaje, decimal MontoFinal, string? NombrePlan, DateTime FechaFin)> ProcesarPagoAsync(Pago model)
        {
            // Cláusula de guarda para validar el modelo de pago.
            if (model == null || string.IsNullOrWhiteSpace(model.UsuarioId) || string.IsNullOrWhiteSpace(model.MembresiaId))
            {
                return (false, "Datos de pago incompletos.", 0m, null, DateTime.MinValue);
            }

            // Aplicación de política de cobro mediante Strategy.
            var montoFinal = _paymentContext.ExecutePaymentForAmount(model.Monto);
            model.Monto = montoFinal;

            // Registro persistente de la transacción en Firestore.
            await _pagoRepo.AddAsync(model);

            // Recupera la configuración del plan adquirido.
            var plan = await _membresiaRepo.GetByIdAsync(model.MembresiaId);
            if (plan == null)
            {
                return (true, "Pago registrado sin plan asociado.", montoFinal, null, DateTime.UtcNow);
            }

            // Actualización de la suscripción y cálculo de nueva fecha de corte.
            var fechaFin = await ActualizarMembresiaTrasPagoAsync(model.UsuarioId, model.MembresiaId, plan.DuracionDias);

            var mensaje = string.Format(MensajesNotificacion.PagoRegistradoExito, montoFinal, plan.Nombre, fechaFin);
            return (true, mensaje, montoFinal, plan.Nombre, fechaFin);
        }

        // Obtiene el historial de pagos de un socio con la información de sus planes asociados.
        public async Task<IReadOnlyList<Pago>> ObtenerPagosUsuarioAsync(string usuarioId)
        {
            // Cláusula de guarda para validar el usuario.
            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return Array.Empty<Pago>();
            }

            // Consulta de transacciones históricas del socio.
            var pagos = await _pagoRepo.GetByUsuarioIdAsync(usuarioId);
            var planes = await _membresiaRepo.GetAllAsync();
            var planesDict = planes.ToDictionary(p => p.Id, p => p);

            // Vinculación en memoria del plan para cada pago.
            foreach (var pago in pagos)
            {
                if (planesDict.TryGetValue(pago.MembresiaId, out var p))
                {
                    pago.Membresia = p;
                }
            }

            return pagos;
        }

        // Genera el consolidado financiero con ingresos totales, recaudación mensual y detalle de transacciones.
        public async Task<(IReadOnlyList<Pago> Pagos, decimal IngresosTotales, decimal PagosDelMes)> ObtenerReporteFinancieroAsync()
        {
            // Recupera pagos, usuarios y planes en memoria para cruzar referencias.
            var pagos = await _pagoRepo.GetAllAsync();
            var usuarios = await _usuarioRepo.GetAllAsync();
            var planes = await _membresiaRepo.GetAllAsync();

            var usuariosDict = usuarios.ToDictionary(u => u.Id, u => u);
            var planesDict = planes.ToDictionary(p => p.Id, p => p);

            // Vinculación en memoria evitando consultas individuales N+1.
            foreach (var pago in pagos)
            {
                if (usuariosDict.TryGetValue(pago.UsuarioId, out var u))
                {
                    pago.Usuario = u;
                }

                if (planesDict.TryGetValue(pago.MembresiaId, out var m))
                {
                    pago.Membresia = m;
                }
            }

            var pagosOrdenados = pagos.OrderByDescending(p => p.FechaPago).ToList();
            var ahora = DateTime.UtcNow;

            // Cálculo agregado de métricas financieras.
            var ingresosTotales = pagosOrdenados.Sum(p => p.Monto);
            var pagosDelMes = pagosOrdenados
                .Where(p => p.FechaPago.Month == ahora.Month && p.FechaPago.Year == ahora.Year)
                .Sum(p => p.Monto);

            return (pagosOrdenados, ingresosTotales, pagosDelMes);
        }

        // Método auxiliar privado para actualizar la membresía del socio tras un pago exitoso.
        private async Task<DateTime> ActualizarMembresiaTrasPagoAsync(string usuarioId, string membresiaId, int duracionDias)
        {
            // Consulta de membresías activas actuales del socio.
            var membresiasActivas = await _memUsuarioRepo.GetByUsuarioIdAsync(usuarioId, soloActivas: true);
            var ahora = DateTime.UtcNow;

            DateTime? fechaFinActual = null;

            // Busca si alguna suscripción aún tiene vigencia remanente para extenderla.
            foreach (var m in membresiasActivas)
            {
                if (m.FechaFin > ahora)
                {
                    fechaFinActual = m.FechaFin;
                    break;
                }
            }

            // Cálculo puro del nuevo periodo de vigencia.
            var (fechaInicio, fechaFin) = ReglasMembresia.CalcularPeriodoVigencia(fechaFinActual, duracionDias, ahora);

            // Desactiva las suscripciones previas para mantener una única membresía activa canónica.
            await _memUsuarioRepo.DesactivarMembresiasPreviasAsync(usuarioId);

            // Inserción de la nueva asignación activa con vigencia extendida.
            var nuevaAsignacion = new MembresiaUsuario
            {
                UsuarioId = usuarioId,
                MembresiaId = membresiaId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                Activa = true
            };
            await _memUsuarioRepo.AddAsync(nuevaAsignacion);

            return fechaFin;
        }
    }
}
