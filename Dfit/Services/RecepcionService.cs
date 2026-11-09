// Dfit/Services/RecepcionService.cs
// Implementación de la consola operativa de recepción: agrupa socios, vigencias y pagos diarios.
// Resuelve la integración en memoria evitando el antipatrón N+1 y optimizando lecturas de Firestore.

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
    // Servicio de aplicación para recepción, cobro en mostrador y monitoreo de vigencias.
    // Provee métodos de cálculo de estado de vigencia, cobro simultáneo y filtrado ágil.
    public class RecepcionService : IRecepcionService
    {
        // Repositorios requeridos para consultas agregadas.
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly IMembresiaRepository _membresiaRepo;
        private readonly IMembresiaUsuarioRepository _memUsuarioRepo;
        private readonly IPagoRepository _pagoRepo;
        private readonly PaymentContext _paymentContext;
        private readonly IPasswordHasherService _passwordHasher;

        // Constructor con inyección de repositorios y contexto de pago.
        public RecepcionService(
            IUsuarioRepository usuarioRepo,
            IMembresiaRepository membresiaRepo,
            IMembresiaUsuarioRepository memUsuarioRepo,
            IPagoRepository pagoRepo,
            PaymentContext paymentContext,
            IPasswordHasherService passwordHasher)
        {
            _usuarioRepo = usuarioRepo;
            _membresiaRepo = membresiaRepo;
            _memUsuarioRepo = memUsuarioRepo;
            _pagoRepo = pagoRepo;
            _paymentContext = paymentContext;
            _passwordHasher = passwordHasher;
        }

        // Obtiene el estado consolidado de recepción: socios, estado de vigencia y pagos del día.
        // Agrupa colecciones en memoria mediante diccionarios O(1) eliminando bucles N+1.
        public async Task<RecepcionViewModel> ObtenerRecepcionDashboardAsync(string? search = null, string? estado = null)
        {
            // 1. Obtener socios registrados con rol Usuario.
            var todosSocios = await _usuarioRepo.GetSociosAsync();

            // 2. Obtener planes y mapear diccionario en memoria O(1) para resolución instantánea.
            var planes = await _membresiaRepo.GetActivasAsync();
            var planesDict = planes.ToDictionary(p => p.Id, p => p);

            // 3. Obtener membresías activas y agrupar la más reciente por usuario.
            var muList = await _memUsuarioRepo.GetActivasAsync();
            var membresiasDict = new Dictionary<string, (string DocId, string MembresiaId, DateTime FechaFin)>();

            // Itera las asignaciones activas para conservar solo la que tiene mayor fecha de expiración.
            foreach (var doc in muList)
            {
                // Si el socio no está en el diccionario o su nueva fecha es mayor, se actualiza.
                if (!membresiasDict.TryGetValue(doc.UsuarioId, out var prev) || doc.FechaFin > prev.FechaFin)
                {
                    membresiasDict[doc.UsuarioId] = (doc.Id, doc.MembresiaId, doc.FechaFin);
                }
            }

            // 4. Obtener pagos de hoy mediante consulta optimizada por rango de fechas.
            var ahora = DateTime.UtcNow;
            var hoyInicio = ahora.Date;
            var hoyFin = hoyInicio.AddDays(1).AddTicks(-1);
            var pagosHoy = await _pagoRepo.GetByRangoFechaAsync(hoyInicio, hoyFin);

            decimal totalCobradoHoy = 0;
            var ultimosPagosHoy = new List<Pago>();

            // Itera las transacciones del día vinculando planes y usuarios.
            foreach (var pago in pagosHoy)
            {
                totalCobradoHoy += pago.Monto;
                // Vincula el plan de membresía correspondiente.
                if (planesDict.TryGetValue(pago.MembresiaId, out var plan))
                {
                    pago.Membresia = plan;
                }

                // Asocia el socio correspondiente.
                pago.Usuario = todosSocios.FirstOrDefault(s => s.Id == pago.UsuarioId);
                ultimosPagosHoy.Add(pago);
            }

            // 5. Mapear estado y vigencia de cada socio para alimentar la grilla operativa.
            var sociosItems = new List<SocioRecepcionItem>();
            int sociosActivos = 0;
            int porVencer = 0;
            int vencidos = 0;

            // Calcula el indicador de vigencia para cada socio registrado.
            foreach (var u in todosSocios)
            {
                var item = MapearSocioRecepcion(u, membresiasDict, planesDict, ahora);

                // Incrementa los contadores del tablero de control.
                if (item.EstadoVigencia == EstadosMembresia.Activa || item.EstadoVigencia == "Vigente")
                {
                    sociosActivos++;
                }
                else if (item.EstadoVigencia == EstadosMembresia.PorVencer)
                {
                    sociosActivos++;
                    porVencer++;
                }
                else if (item.EstadoVigencia == EstadosMembresia.Vencida)
                {
                    vencidos++;
                }

                sociosItems.Add(item);
            }

            // Filtros de búsqueda en memoria para respuesta rápida en frontend.
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                sociosItems = sociosItems.Where(x =>
                    x.Nombres.ToLower().Contains(s) ||
                    x.Apellidos.ToLower().Contains(s) ||
                    x.DNI.Contains(s) ||
                    x.Correo.ToLower().Contains(s)).ToList();
            }

            // Filtro por estado de vigencia si fue seleccionado en el dropdown.
            if (!string.IsNullOrWhiteSpace(estado) && estado != "Todos")
            {
                sociosItems = sociosItems.Where(x => x.EstadoVigencia == estado).ToList();
            }

            // Construye el ViewModel consolidado para la vista de recepción.
            return new RecepcionViewModel
            {
                Socios = sociosItems.OrderBy(x => x.Nombres).ToList(),
                PlanesDisponibles = planes.ToList(),
                UltimosPagosHoy = ultimosPagosHoy.OrderByDescending(x => x.FechaPago).Take(15).ToList(),
                TotalSocios = todosSocios.Count,
                SociosConMembresiaActiva = sociosActivos,
                MembresiasPorVencer = porVencer,
                MembresiasVencidas = vencidos,
                TotalCobradoHoy = totalCobradoHoy,
                PagosRealizadosHoy = pagosHoy.Count,
                SearchTerm = search,
                FiltroEstado = estado
            };
        }

        // Registra un socio en mostrador y opcionalmente activa membresía inicial con cobro.
        public async Task<(bool Exito, string Mensaje, bool EsError)> RegistrarSocioRecepcionAsync(Usuario model, string? membresiaId = null, decimal? montoPago = null, string? metodoPago = null)
        {
            // Valida campos obligatorios en el modelo de socio.
            if (string.IsNullOrWhiteSpace(model.Nombres) || string.IsNullOrWhiteSpace(model.Apellidos) || string.IsNullOrWhiteSpace(model.Correo))
            {
                return (false, MensajesNotificacion.CamposObligatoriosIncompletos, true);
            }

            // Normaliza correo y valida que no exista una cuenta previa registrada.
            var correoNormalizado = model.Correo.Trim();
            var existente = await _usuarioRepo.GetByEmailAsync(correoNormalizado);
            if (existente != null)
            {
                return (false, string.Format(MensajesNotificacion.CorreoYaRegistrado, correoNormalizado), true);
            }

            // Genera contraseña temporal a partir de DNI si no fue indicada.
            var password = string.IsNullOrWhiteSpace(model.PasswordHash)
                ? (!string.IsNullOrWhiteSpace(model.DNI) ? model.DNI.Trim() : "123456")
                : model.PasswordHash.Trim();

            // Asigna atributos normalizados y hashea la contraseña.
            model.Nombres = model.Nombres.Trim();
            model.Apellidos = model.Apellidos.Trim();
            model.Correo = correoNormalizado;
            model.DNI = model.DNI?.Trim() ?? "";
            model.Celular = model.Celular?.Trim() ?? "";
            model.PasswordHash = _passwordHasher.HashPassword(password);
            model.Rol = RolesSistema.Usuario;
            model.Activo = true;
            model.FechaRegistro = DateTime.UtcNow;

            // Inserta el nuevo socio en la base de datos.
            var nuevoUsuarioId = await _usuarioRepo.AddAsync(model);
            model.Id = nuevoUsuarioId;

            // Cobro y asignación inmediata de plan si fue seleccionado en mostrador.
            if (!string.IsNullOrEmpty(membresiaId))
            {
                var plan = await _membresiaRepo.GetByIdAsync(membresiaId);
                if (plan != null)
                {
                    // Aplica estrategia de cobro (PaymentContext).
                    var montoFinal = montoPago ?? plan.Precio;
                    montoFinal = _paymentContext.ExecutePaymentForAmount(montoFinal);

                    // Registra la transacción de cobro inicial.
                    await _pagoRepo.AddAsync(new Pago
                    {
                        UsuarioId = nuevoUsuarioId,
                        MembresiaId = membresiaId,
                        Monto = montoFinal,
                        FechaPago = DateTime.UtcNow,
                        MetodoPago = string.IsNullOrEmpty(metodoPago) ? MetodosPago.Efectivo : metodoPago
                    });

                    // Calcula el período de vigencia a partir de la fecha actual.
                    var (fechaInicio, fechaFin) = ReglasMembresia.CalcularPeriodoVigencia(null, plan.DuracionDias, DateTime.UtcNow);

                    // Activa la membresía en la cuenta del nuevo socio.
                    await _memUsuarioRepo.AddAsync(new MembresiaUsuario
                    {
                        UsuarioId = nuevoUsuarioId,
                        MembresiaId = membresiaId,
                        FechaInicio = fechaInicio,
                        FechaFin = fechaFin,
                        Activa = true
                    });

                    var msjConPlan = string.Format(MensajesNotificacion.SocioYPlanRegistradoExito, model.Nombres, plan.Nombre, fechaFin);
                    return (true, msjConPlan, false);
                }
            }

            var msjSimple = string.Format(MensajesNotificacion.SocioRegistradoExito, model.Nombres, model.Apellidos);
            return (true, msjSimple, false);
        }

        // Método auxiliar privado para calcular vigencia y armar el ítem de recepción.
        private static SocioRecepcionItem MapearSocioRecepcion(
            Usuario u,
            Dictionary<string, (string DocId, string MembresiaId, DateTime FechaFin)> membresiasDict,
            Dictionary<string, Membresia> planesDict,
            DateTime ahora)
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

            // Evalúa si el usuario tiene asignación de membresía registrada.
            if (membresiasDict.TryGetValue(u.Id, out var mu))
            {
                item.MembresiaUsuarioId = mu.DocId;
                item.FechaFin = mu.FechaFin;
                // Asocia el nombre de la membresía.
                if (planesDict.TryGetValue(mu.MembresiaId, out var p))
                {
                    item.MembresiaNombre = p.Nombre;
                }

                // Calcula los días restantes de vigencia respecto a UTC actual.
                var diasRestantes = (int)Math.Ceiling((mu.FechaFin - ahora).TotalDays);
                item.DiasRestantes = diasRestantes;

                // Determina la etiqueta visual de vigencia.
                if (mu.FechaFin > ahora)
                {
                    item.TieneMembresiaActiva = true;
                    item.EstadoVigencia = diasRestantes <= 5 ? EstadosMembresia.PorVencer : "Vigente";
                }
                else
                {
                    item.TieneMembresiaActiva = false;
                    item.EstadoVigencia = EstadosMembresia.Vencida;
                }
            }
            else
            {
                // Usuario sin ninguna suscripción asignada.
                item.EstadoVigencia = EstadosMembresia.SinPlan;
            }

            return item;
        }
    }
}
