// Dfit/Services/RecepcionService.cs
// Implementación de lógica de negocio para la consola operativa de recepción.
// Optimiza el cálculo de vigencias y recaudación diaria mitigando consultas N+1.
// Aplica cláusulas de guarda para mantener profundidad condicional <= 2 niveles.

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
    // Servicio de aplicación para la gestión de recepción, cobranza rápida y monitoreo de socios.
    public class RecepcionService : IRecepcionService
    {
        // Repositorios y servicios inyectados.
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly IMembresiaRepository _membresiaRepo;
        private readonly IMembresiaUsuarioRepository _memUsuarioRepo;
        private readonly IPagoRepository _pagoRepo;
        private readonly IPasswordHasherService _passwordHasher;
        private readonly PaymentContext _paymentContext;

        // Constructor con inyección de dependencias.
        public RecepcionService(
            IUsuarioRepository usuarioRepo,
            IMembresiaRepository membresiaRepo,
            IMembresiaUsuarioRepository memUsuarioRepo,
            IPagoRepository pagoRepo,
            IPasswordHasherService passwordHasher,
            PaymentContext paymentContext)
        {
            _usuarioRepo = usuarioRepo;
            _membresiaRepo = membresiaRepo;
            _memUsuarioRepo = memUsuarioRepo;
            _pagoRepo = pagoRepo;
            _passwordHasher = passwordHasher;
            _paymentContext = paymentContext;
        }

        // Construye el modelo integral para la consola de recepción con KPIs en tiempo real.
        public async Task<RecepcionViewModel> ObtenerRecepcionDashboardAsync(string? search = null, string? estado = null)
        {
            // Carga concurrente o secuencial de datos base para evitar bloqueos.
            var todosSocios = await _usuarioRepo.GetSociosAsync();
            var planesActivos = await _membresiaRepo.GetActivasAsync();
            var asignacionesActivas = await _memUsuarioRepo.GetActivasAsync();
            var pagosHistoricos = await _pagoRepo.GetAllAsync();

            var ahora = DateTime.UtcNow;
            var planesDict = planesActivos.ToDictionary(p => p.Id, p => p);

            // Mapeo indexado de la membresía con mayor vigencia por usuario.
            var membresiasDict = ConstruirMapaMembresiasVigentes(asignacionesActivas);

            // Procesamiento de métricas y pagos de la jornada de hoy.
            var (ultimosPagosHoy, totalCobradoHoy, pagosRealizadosHoy) = ProcesarPagosHoy(pagosHistoricos, planesDict, todosSocios, ahora);

            // Construcción de ítems de socios con cálculo de estados de vigencia.
            var (sociosItems, sociosActivos, porVencer, vencidos) = ConstruirItemsSocios(todosSocios, membresiasDict, planesDict, ahora);

            // Aplicación de filtros de búsqueda y estado seleccionado.
            var sociosFiltrados = FiltrarSocios(sociosItems, search, estado);

            return new RecepcionViewModel
            {
                Socios = sociosFiltrados.OrderBy(x => x.Nombres).ToList(),
                PlanesDisponibles = planesActivos.ToList(),
                UltimosPagosHoy = ultimosPagosHoy.OrderByDescending(x => x.FechaPago).Take(15).ToList(),
                TotalSocios = todosSocios.Count,
                SociosConMembresiaActiva = sociosActivos,
                MembresiasPorVencer = porVencer,
                MembresiasVencidas = vencidos,
                TotalCobradoHoy = totalCobradoHoy,
                PagosRealizadosHoy = pagosRealizadosHoy,
                SearchTerm = search,
                FiltroEstado = estado
            };
        }

        // Registra un nuevo socio desde mostrador con opción de cobro y activación de plan inmediata.
        public async Task<(bool Exito, string Mensaje, string? NombreSocio)> RegistrarSocioRecepcionAsync(
            Usuario model,
            string? membresiaId = null,
            decimal? montoPago = null,
            string? metodoPago = null)
        {
            // Cláusula de guarda para validar campos obligatorios.
            if (string.IsNullOrWhiteSpace(model.Nombres) || string.IsNullOrWhiteSpace(model.Apellidos) || string.IsNullOrWhiteSpace(model.Correo))
            {
                return (false, MensajesNotificacion.CamposObligatoriosIncompletos, null);
            }

            // Normalización del correo y verificación de duplicidad.
            var correoNormalizado = model.Correo.Trim();
            var usuarioExistente = await _usuarioRepo.GetByEmailAsync(correoNormalizado);
            if (usuarioExistente != null)
            {
                return (false, string.Format(MensajesNotificacion.CorreoYaRegistrado, correoNormalizado), null);
            }

            // Preparación y hasheo seguro de la contraseña inicial.
            var passwordInicial = DeterminarPasswordInicial(model);
            model.PasswordHash = _passwordHasher.HashPassword(passwordInicial);
            model.Correo = correoNormalizado;
            model.Nombres = model.Nombres.Trim();
            model.Apellidos = model.Apellidos.Trim();
            model.DNI = model.DNI?.Trim() ?? "";
            model.Celular = model.Celular?.Trim() ?? "";
            model.Rol = "Usuario";
            model.Activo = true;

            // Inserción del nuevo socio en Firestore.
            var nuevoId = await _usuarioRepo.AddAsync(model);
            model.Id = nuevoId;

            // Si no se seleccionó membresía inicial, finaliza el registro base.
            if (string.IsNullOrEmpty(membresiaId))
            {
                var mensajeBase = string.Format(MensajesNotificacion.SocioRegistradoExito, model.Nombres, model.Apellidos);
                return (true, mensajeBase, model.Nombres);
            }

            // Procesamiento de plan inicial y cobro en mostrador.
            return await ProcesarSuscripcionInicialRecepcionAsync(model, membresiaId, montoPago, metodoPago);
        }

        // Método auxiliar privado para procesar la suscripción inicial y cobro.
        private async Task<(bool Exito, string Mensaje, string? NombreSocio)> ProcesarSuscripcionInicialRecepcionAsync(
            Usuario usuario,
            string membresiaId,
            decimal? montoPago,
            string? metodoPago)
        {
            var plan = await _membresiaRepo.GetByIdAsync(membresiaId);
            if (plan == null)
            {
                var msg = string.Format(MensajesNotificacion.SocioRegistradoExito, usuario.Nombres, usuario.Apellidos);
                return (true, msg, usuario.Nombres);
            }

            // Cálculo del monto aplicando patrón Strategy.
            var precioBase = montoPago ?? plan.Precio;
            var montoFinal = _paymentContext.ExecutePaymentForAmount(precioBase);

            // Registro del pago en caja.
            var pago = new Pago
            {
                UsuarioId = usuario.Id,
                MembresiaId = membresiaId,
                Monto = montoFinal,
                FechaPago = DateTime.UtcNow,
                MetodoPago = string.IsNullOrEmpty(metodoPago) ? "Efectivo" : metodoPago
            };
            await _pagoRepo.AddAsync(pago);

            // Cálculo de vigencia y activación del plan.
            var (fechaInicio, fechaFin) = ReglasMembresia.CalcularPeriodoVigencia(null, plan.DuracionDias, DateTime.UtcNow);
            var asignacion = new MembresiaUsuario
            {
                UsuarioId = usuario.Id,
                MembresiaId = membresiaId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                Activa = true
            };
            await _memUsuarioRepo.AddAsync(asignacion);

            var mensajeExito = string.Format(MensajesNotificacion.SocioYPlanRegistradoExito, usuario.Nombres, plan.Nombre, fechaFin);
            return (true, mensajeExito, usuario.Nombres);
        }

        // Determina la contraseña por defecto si el usuario no ingresó una explícita.
        private static string DeterminarPasswordInicial(Usuario model)
        {
            if (!string.IsNullOrWhiteSpace(model.PasswordHash))
            {
                return model.PasswordHash.Trim();
            }

            if (!string.IsNullOrWhiteSpace(model.DNI))
            {
                return model.DNI.Trim();
            }

            return "123456";
        }

        // Construye un mapa de las asignaciones activas más recientes por usuario.
        private static Dictionary<string, (string DocId, string MembresiaId, DateTime FechaFin)> ConstruirMapaMembresiasVigentes(
            IReadOnlyList<MembresiaUsuario> asignaciones)
        {
            var dict = new Dictionary<string, (string DocId, string MembresiaId, DateTime FechaFin)>();

            foreach (var doc in asignaciones)
            {
                if (!dict.TryGetValue(doc.UsuarioId, out var prev) || doc.FechaFin > prev.FechaFin)
                {
                    dict[doc.UsuarioId] = (doc.Id, doc.MembresiaId, doc.FechaFin);
                }
            }

            return dict;
        }

        // Procesa los pagos de la jornada de hoy calculando totales recaudados.
        private static (List<Pago> PagosHoy, decimal TotalCobrado, int CantidadPagos) ProcesarPagosHoy(
            IReadOnlyList<Pago> pagos,
            Dictionary<string, Membresia> planesDict,
            IReadOnlyList<Usuario> socios,
            DateTime ahora)
        {
            var ultimosPagosHoy = new List<Pago>();
            decimal totalCobrado = 0;
            int cantidadPagos = 0;
            var fechaHoy = ahora.Date;

            foreach (var pago in pagos)
            {
                if (pago.FechaPago.Date == fechaHoy)
                {
                    totalCobrado += pago.Monto;
                    cantidadPagos++;

                    if (planesDict.TryGetValue(pago.MembresiaId, out var plan))
                    {
                        pago.Membresia = plan;
                    }

                    var socio = socios.FirstOrDefault(s => s.Id == pago.UsuarioId);
                    if (socio != null)
                    {
                        pago.Usuario = socio;
                    }

                    ultimosPagosHoy.Add(pago);
                }
            }

            return (ultimosPagosHoy, totalCobrado, cantidadPagos);
        }

        // Construye los ítems de socio con su respectiva semaforización de vigencia.
        private static (List<SocioRecepcionItem> Items, int Activos, int PorVencer, int Vencidos) ConstruirItemsSocios(
            IReadOnlyList<Usuario> socios,
            Dictionary<string, (string DocId, string MembresiaId, DateTime FechaFin)> membresiasDict,
            Dictionary<string, Membresia> planesDict,
            DateTime ahora)
        {
            var items = new List<SocioRecepcionItem>();
            int activos = 0;
            int porVencer = 0;
            int vencidos = 0;

            foreach (var u in socios)
            {
                var item = new SocioRecepcionItem
                {
                    Id = u.Id,
                    Nombres = u.Nombres,
                    Apellidos = u.Apellidos,
                    Correo = u.Correo,
                    DNI = u.DNI,
                    Celular = u.Celular,
                    Activo = u.Activo
                };

                if (membresiasDict.TryGetValue(u.Id, out var mu))
                {
                    item.MembresiaUsuarioId = mu.DocId;
                    item.FechaFin = mu.FechaFin;

                    if (planesDict.TryGetValue(mu.MembresiaId, out var p))
                    {
                        item.MembresiaNombre = p.Nombre;
                    }

                    var diasRestantes = (int)Math.Ceiling((mu.FechaFin - ahora).TotalDays);
                    item.DiasRestantes = diasRestantes;

                    if (mu.FechaFin > ahora)
                    {
                        item.TieneMembresiaActiva = true;
                        activos++;

                        if (diasRestantes <= 5)
                        {
                            item.EstadoVigencia = "Por Vencer";
                            porVencer++;
                        }
                        else
                        {
                            item.EstadoVigencia = "Vigente";
                        }
                    }
                    else
                    {
                        item.TieneMembresiaActiva = false;
                        item.EstadoVigencia = "Vencida";
                        vencidos++;
                    }
                }
                else
                {
                    item.EstadoVigencia = "Sin Plan";
                }

                items.Add(item);
            }

            return (items, activos, porVencer, vencidos);
        }

        // Aplica filtros de texto y estado sobre la lista de socios de recepción.
        private static List<SocioRecepcionItem> FiltrarSocios(
            List<SocioRecepcionItem> sociosItems,
            string? search,
            string? estado)
        {
            var resultado = sociosItems;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                resultado = resultado.Where(x =>
                    x.Nombres.ToLower().Contains(s) ||
                    x.Apellidos.ToLower().Contains(s) ||
                    x.DNI.Contains(s) ||
                    x.Correo.ToLower().Contains(s)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(estado) && estado != "Todos")
            {
                resultado = resultado.Where(x => x.EstadoVigencia == estado).ToList();
            }

            return resultado;
        }
    }
}
