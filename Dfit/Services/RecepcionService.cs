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
        // Repositorio para gestión y consulta de socios en recepción.
        private readonly IUsuarioRepository _usuarioRepo;
        // Repositorio de catálogo de membresías activas.
        private readonly IMembresiaRepository _membresiaRepo;
        // Repositorio de asignaciones activas de planes a usuarios.
        private readonly IMembresiaUsuarioRepository _memUsuarioRepo;
        // Repositorio de transacciones y cobros realizados.
        private readonly IPagoRepository _pagoRepo;
        // Contexto para aplicación dinámica de descuentos y estrategias de pago.
        private readonly PaymentContext _paymentContext;
        // Servicio criptográfico de hasheo de contraseñas de socios.
        private readonly IPasswordHasherService _passwordHasher;

        // Constructor con inyección de repositorios y contexto de pago.
        // Recibe dependencias desacopladas para pruebas unitarias e inversión de control.
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
            // 1. Obtener socios registrados con rol Usuario en una única consulta batch a Firestore.
            var todosSocios = await _usuarioRepo.GetSociosAsync();

            // 2. Obtener planes y mapear diccionario en memoria O(1) para resolución instantánea.
            // Evita consultas repetitivas a la base de datos por cada fila renderizada.
            var planes = await _membresiaRepo.GetActivasAsync();
            var planesDict = planes.ToDictionary(p => p.Id, p => p);

            // 3. Obtener membresías activas y agrupar la más reciente por usuario.
            // Esto resuelve múltiples asignaciones históricas manteniendo solo el plan vigente.
            var muList = await _memUsuarioRepo.GetActivasAsync();
            var membresiasDict = new Dictionary<string, (string DocId, string MembresiaId, DateTime FechaFin)>();

            // Itera las asignaciones activas para conservar solo la que tiene mayor fecha de expiración.
            foreach (var doc in muList)
            {
                // Si el socio no está en el diccionario o su nueva fecha es mayor, se actualiza la entrada.
                if (!membresiasDict.TryGetValue(doc.UsuarioId, out var prev) || doc.FechaFin > prev.FechaFin)
                {
                    membresiasDict[doc.UsuarioId] = (doc.Id, doc.MembresiaId, doc.FechaFin);
                }
            }

            // 4. Obtener pagos de hoy mediante consulta optimizada por rango de fechas.
            // Establece la ventana horaria completa del día en UTC (desde las 00:00:00 hasta las 23:59:59).
            var ahora = DateTime.UtcNow;
            var hoyInicio = ahora.Date;
            var hoyFin = hoyInicio.AddDays(1).AddTicks(-1);
            var pagosHoy = await _pagoRepo.GetByRangoFechaAsync(hoyInicio, hoyFin);

            // Acumulador de ingresos financieros del día y lista de últimas transacciones.
            decimal totalCobradoHoy = 0;
            var ultimosPagosHoy = new List<Pago>();

            // Itera las transacciones del día vinculando planes y usuarios en memoria.
            foreach (var pago in pagosHoy)
            {
                // Acumula el monto cobrado al consolidado del día.
                totalCobradoHoy += pago.Monto;

                // Vincula el plan de membresía correspondiente desde el diccionario en memoria.
                if (planesDict.TryGetValue(pago.MembresiaId, out var plan))
                {
                    pago.Membresia = plan;
                }

                // Asocia el socio correspondiente buscando por ID en memoria sin queries extras.
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
                // Transforma la entidad de dominio a un ítem ligero para el panel de recepción.
                var item = MapearSocioRecepcion(u, membresiasDict, planesDict, ahora);

                // Incrementa los contadores del tablero de control según el estado calculado.
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

                // Agrega el socio evaluado a la colección de presentación.
                sociosItems.Add(item);
            }

            // Filtros de búsqueda en memoria para respuesta rápida en frontend.
            // Permite filtrar por nombres, apellidos, documento DNI o correo electrónico.
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                sociosItems = sociosItems.Where(x =>
                    x.Nombres.ToLower().Contains(s) ||
                    x.Apellidos.ToLower().Contains(s) ||
                    x.DNI.Contains(s) ||
                    x.Correo.ToLower().Contains(s)).ToList();
            }

            // Filtro por estado de vigencia si fue seleccionado en el dropdown del filtro.
            // Valida que no sea la opción por defecto 'Todos' antes de aplicar el predicado.
            if (!string.IsNullOrWhiteSpace(estado) && estado != "Todos")
            {
                sociosItems = sociosItems.Where(x => x.EstadoVigencia == estado).ToList();
            }

            // Construye el ViewModel consolidado para la vista de recepción con todas las métricas.
            return new RecepcionViewModel
            {
                // Lista de socios ordenada alfabéticamente por nombres.
                Socios = sociosItems.OrderBy(x => x.Nombres).ToList(),
                // Catálogo de planes de entrenamiento disponibles para venta rápida.
                PlanesDisponibles = planes.ToList(),
                // Transacciones recientes del día limitadas a las 15 más nuevas.
                UltimosPagosHoy = ultimosPagosHoy.OrderByDescending(x => x.FechaPago).Take(15).ToList(),
                // Contadores y métricas operativas del tablero superior.
                TotalSocios = todosSocios.Count,
                SociosConMembresiaActiva = sociosActivos,
                MembresiasPorVencer = porVencer,
                MembresiasVencidas = vencidos,
                TotalCobradoHoy = totalCobradoHoy,
                PagosRealizadosHoy = pagosHoy.Count,
                // Términos de búsqueda aplicados para preservar el estado de la vista.
                SearchTerm = search,
                FiltroEstado = estado
            };
        }

        // Registra un nuevo socio en mostrador y opcionalmente activa membresía inicial con cobro.
        // Soporta alta simple de cuenta o enrolamiento completo con membresía y facturación.
        public async Task<(bool Exito, string Mensaje, string? NombreSocio)> RegistrarSocioRecepcionAsync(
            Usuario model,
            string? membresiaId = null,
            decimal? montoPago = null,
            string? metodoPago = null)
        {
            // Valida campos obligatorios en el modelo de socio antes de continuar.
            if (string.IsNullOrWhiteSpace(model.Nombres) || string.IsNullOrWhiteSpace(model.Apellidos) || string.IsNullOrWhiteSpace(model.Correo))
            {
                return (false, MensajesNotificacion.CamposObligatoriosIncompletos, null);
            }

            // Normaliza correo y valida que no exista una cuenta previa registrada en Firestore.
            var correoNormalizado = model.Correo.Trim();
            var existente = await _usuarioRepo.GetByEmailAsync(correoNormalizado);
            if (existente != null)
            {
                // Rechazo inmediato por correo ya existente evitando cuentas duplicadas.
                return (false, string.Format(MensajesNotificacion.CorreoYaRegistrado, correoNormalizado), null);
            }

            // Genera contraseña temporal a partir de DNI si no fue indicada explícitamente.
            var password = string.IsNullOrWhiteSpace(model.PasswordHash)
                ? (!string.IsNullOrWhiteSpace(model.DNI) ? model.DNI.Trim() : "123456")
                : model.PasswordHash.Trim();

            // Asigna atributos normalizados y hashea la contraseña de manera segura.
            model.Nombres = model.Nombres.Trim();
            model.Apellidos = model.Apellidos.Trim();
            model.Correo = correoNormalizado;
            model.DNI = model.DNI?.Trim() ?? "";
            model.Celular = model.Celular?.Trim() ?? "";
            model.PasswordHash = _passwordHasher.HashPassword(password);
            model.Rol = RolesSistema.Usuario;
            model.Activo = true;
            model.FechaRegistro = DateTime.UtcNow;

            // Inserta el nuevo socio en la base de datos de usuarios y recupera su ID generado.
            var nuevoUsuarioId = await _usuarioRepo.AddAsync(model);
            model.Id = nuevoUsuarioId;

            // Si no se seleccionó membresía en mostrador, retorna éxito de alta simple.
            if (string.IsNullOrEmpty(membresiaId))
            {
                var msjSimple = string.Format(MensajesNotificacion.SocioRegistradoExito, model.Nombres, model.Apellidos);
                return (true, msjSimple, model.Nombres);
            }

            // Consulta el catálogo de planes para el cobro y cálculo de duración.
            var plan = await _membresiaRepo.GetByIdAsync(membresiaId);
            if (plan == null)
            {
                // Retorno simple si el ID del plan no corresponde a un catálogo válido.
                var msjSimple = string.Format(MensajesNotificacion.SocioRegistradoExito, model.Nombres, model.Apellidos);
                return (true, msjSimple, model.Nombres);
            }

            // Aplica estrategia de cobro (PaymentContext) según política de descuentos.
            var montoFinal = montoPago ?? plan.Precio;
            montoFinal = _paymentContext.ExecutePaymentForAmount(montoFinal);

            // Registra la transacción de cobro inicial del nuevo socio en Firestore.
            await _pagoRepo.AddAsync(new Pago
            {
                UsuarioId = nuevoUsuarioId,
                MembresiaId = membresiaId,
                Monto = montoFinal,
                FechaPago = DateTime.UtcNow,
                MetodoPago = string.IsNullOrEmpty(metodoPago) ? MetodosPago.Efectivo : metodoPago
            });

            // Calcula el período de vigencia a partir de la fecha actual de registro sin desfases.
            var (fechaInicio, fechaFin) = ReglasMembresia.CalcularPeriodoVigencia(null, plan.DuracionDias, DateTime.UtcNow);

            // Activa la membresía en la cuenta del nuevo socio en Cloud Firestore.
            await _memUsuarioRepo.AddAsync(new MembresiaUsuario
            {
                UsuarioId = nuevoUsuarioId,
                MembresiaId = membresiaId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                Activa = true
            });

            // Notificación exitosa que detalla socio, plan y fecha límite de vigencia calculada.
            var msjConPlan = string.Format(MensajesNotificacion.SocioYPlanRegistradoExito, model.Nombres, plan.Nombre, fechaFin);
            return (true, msjConPlan, model.Nombres);
        }

        // Método auxiliar privado para calcular vigencia y armar el ítem de recepción.
        // Descompone la complejidad ciclomática del dashboard en una función pura.
        private static SocioRecepcionItem MapearSocioRecepcion(
            Usuario u,
            Dictionary<string, (string DocId, string MembresiaId, DateTime FechaFin)> membresiasDict,
            Dictionary<string, Membresia> planesDict,
            DateTime ahora)
        {
            // Inicializa el modelo de presentación con los datos básicos del socio.
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

                // Asocia el nombre de la membresía para renderizado en tabla.
                if (planesDict.TryGetValue(mu.MembresiaId, out var p))
                {
                    item.MembresiaNombre = p.Nombre;
                }

                // Calcula los días restantes de vigencia respecto a UTC actual.
                var diasRestantes = (int)Math.Ceiling((mu.FechaFin - ahora).TotalDays);
                item.DiasRestantes = diasRestantes;

                // Determina la etiqueta visual de vigencia según fecha de expiración.
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
                // Usuario sin ninguna suscripción asignada en el historial.
                item.EstadoVigencia = EstadosMembresia.SinPlan;
            }

            // Retorna el objeto mapeado para la vista.
            return item;
        }
    }
}
