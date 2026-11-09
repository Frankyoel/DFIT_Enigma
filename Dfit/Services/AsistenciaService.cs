// Dfit/Services/AsistenciaService.cs
// Implementación de lógica de negocio para accesos físicos, check-in QR y control de aforo.
// Integra el proxy de acceso al gimnasio (IGymAccess) y el emisor de tokens QR (IQRTokenService).
// Aplica cláusulas de guarda para mantener la profundidad condicional <= 2 niveles.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dfit.Models;
using Dfit.Models.Patterns.Proxy;
using Dfit.Models.Patterns.Singleton;
using Dfit.Repositories.Contracts;
using Dfit.Utils;

namespace Dfit.Services
{
    // Servicio de aplicación para control de ingresos, check-in QR y validación de membresías.
    public class AsistenciaService : IAsistenciaService
    {
        // Repositorios y componentes de acceso inyectados.
        private readonly IAsistenciaRepository _asistenciaRepo;
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly IMembresiaUsuarioRepository _memUsuarioRepo;
        private readonly IQRTokenService _qrTokenService;
        private readonly IGymAccess _gymAccess;

        // Constructor con inyección de dependencias.
        public AsistenciaService(
            IAsistenciaRepository asistenciaRepo,
            IUsuarioRepository usuarioRepo,
            IMembresiaUsuarioRepository memUsuarioRepo,
            IQRTokenService qrTokenService,
            IGymAccess gymAccess)
        {
            _asistenciaRepo = asistenciaRepo;
            _usuarioRepo = usuarioRepo;
            _memUsuarioRepo = memUsuarioRepo;
            _qrTokenService = qrTokenService;
            _gymAccess = gymAccess;
        }

        // Obtiene las asistencias registradas en la jornada de hoy y la lista de socios activos.
        public async Task<(IReadOnlyList<Asistencia> AsistenciasHoy, IReadOnlyList<Usuario> UsuariosActivos)> ObtenerAsistenciaDashboardAsync()
        {
            // Consulta de asistencias de la fecha actual.
            var asistenciasHoy = await _asistenciaRepo.GetByFechaAsync(DateTime.UtcNow);
            var usuarios = await _usuarioRepo.GetAllAsync();
            var usuariosDict = usuarios.ToDictionary(u => u.Id, u => u);

            // Vinculación en memoria de la información del socio.
            foreach (var a in asistenciasHoy)
            {
                if (usuariosDict.TryGetValue(a.UsuarioId, out var socio))
                {
                    a.Usuario = socio;
                }
            }

            // Filtrado de usuarios activos para el desplegable de check-in manual.
            var usuariosActivos = usuarios
                .Where(u => u.Activo)
                .OrderBy(u => u.Nombres)
                .ToList();

            return (asistenciasHoy, usuariosActivos);
        }

        // Registra el ingreso manual de un socio verificando previamente la vigencia de su plan.
        public async Task<(bool Exito, string Mensaje)> RegistrarAsistenciaManualAsync(string usuarioId)
        {
            // Cláusula de guarda para validar el socio.
            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return (false, MensajesNotificacion.DatosAsignacionIncompletos);
            }

            // Verifica si el usuario cuenta con alguna membresía activa vigente.
            var membresiaActiva = await ValidarMembresiaActivaSocioAsync(usuarioId);
            if (!membresiaActiva)
            {
                return (false, MensajesNotificacion.SinMembresiaActivaOAccesoVencido);
            }

            // Registro persistente del acceso en el repositorio de asistencias.
            var asistencia = new Asistencia
            {
                UsuarioId = usuarioId,
                FechaHoraEntrada = DateTime.UtcNow
            };
            await _asistenciaRepo.AddAsync(asistencia);

            return (true, MensajesNotificacion.AccesoConcedidoAsistenciaExito);
        }

        // Genera o recupera el token criptográfico actual para la pantalla QR de recepción.
        public string GenerarTokenQR()
        {
            // Delega la emisión al servicio de tokens temporales de ciclo corto.
            return _qrTokenService.GetCurrentToken();
        }

        // Valida el código QR escaneado por el socio y efectúa el check-in físico en el torniquete.
        public (bool Exito, string Mensaje) CheckInQR(string token, string userId)
        {
            // Cláusula de guarda si el usuario no tiene una sesión autenticada.
            if (string.IsNullOrWhiteSpace(userId))
            {
                return (false, "Sesión de usuario no identificada.");
            }

            // Validación temporal y criptográfica del token QR escaneado.
            if (!_qrTokenService.ValidateToken(token))
            {
                return (false, MensajesNotificacion.QRExpiradoOInvalido);
            }

            // Ejecución del registro de acceso a través del proxy de torniquetes.
            bool exito = _gymAccess.RegistrarAsistencia(userId);
            string mensaje = _gymAccess.MensajeError ?? (exito ? MensajesNotificacion.AccesoConcedidoAsistenciaExito : "Acceso no autorizado.");

            return (exito, mensaje);
        }

        // Método auxiliar privado para comprobar el acceso según reglas puras de vigencia.
        private async Task<bool> ValidarMembresiaActivaSocioAsync(string usuarioId)
        {
            var asignaciones = await _memUsuarioRepo.GetByUsuarioIdAsync(usuarioId, soloActivas: true);
            var ahora = DateTime.UtcNow;

            foreach (var doc in asignaciones)
            {
                if (ReglasMembresia.PermiteAcceso(doc.Activa, doc.FechaFin, ahora))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
