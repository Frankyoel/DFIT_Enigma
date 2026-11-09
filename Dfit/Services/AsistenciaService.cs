// Dfit/Services/AsistenciaService.cs
// Implementación de control de ingresos físicos y validación de tokens QR temporales.
// Encapsula las reglas de acceso con IGymAccess (Proxy Pattern) e IQRTokenService (Singleton).

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
    // Servicio de aplicación para control de acceso, asistencia y validación de check-in por QR.
    // Coordina la verificación de membresías activas y el registro de entradas en tiempo real.
    public class AsistenciaService : IAsistenciaService
    {
        // Repositorios y componentes de patrones inyectados.
        private readonly IAsistenciaRepository _asistenciaRepo;
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly IMembresiaUsuarioRepository _memUsuarioRepo;
        private readonly IQRTokenService _qrTokenService;
        private readonly IGymAccess _gymAccess;

        // Constructor con inyección de dependencias de repositorios y servicios de acceso.
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

        // Obtiene las asistencias del día y lista de socios activos para el panel operativo.
        public async Task<(IReadOnlyList<Asistencia> AsistenciasHoy, IReadOnlyList<Usuario> UsuariosActivos)> ObtenerAsistenciasHoyAsync()
        {
            var ahora = DateTime.UtcNow;

            // Recupera asistencias registradas en la fecha actual.
            var asistencias = await _asistenciaRepo.GetByFechaAsync(ahora);

            // Carga todos los usuarios para asociar nombres a las entradas.
            var usuarios = await _usuarioRepo.GetAllAsync();
            var usuariosDict = usuarios.ToDictionary(u => u.Id, u => u);

            // Asocia el objeto Usuario a cada registro de asistencia.
            foreach (var a in asistencias)
            {
                if (usuariosDict.TryGetValue(a.UsuarioId, out var socio))
                {
                    a.Usuario = socio;
                }
            }

            // Filtra solo socios activos para el selector de registro manual.
            var usuariosActivos = usuarios.Where(u => u.Activo).ToList();
            return (asistencias, usuariosActivos);
        }

        // Registra ingreso manual comprobando que el socio cuente con suscripción vigente.
        public async Task<(bool Exito, string Mensaje)> RegistrarAccesoManualAsync(string usuarioId)
        {
            // Cláusula de guarda para ID de usuario vacío.
            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return (false, MensajesNotificacion.UsuarioNoEncontrado);
            }

            // Carga membresías activas del socio en Cloud Firestore.
            var asignaciones = await _memUsuarioRepo.GetByUsuarioIdAsync(usuarioId, soloActivas: true);
            var ahora = DateTime.UtcNow;
            bool tieneAcceso = false;

            // Evalúa si alguna de las membresías activas se encuentra dentro del rango de vigencia.
            foreach (var doc in asignaciones)
            {
                if (ReglasMembresia.PermiteAcceso(doc.Activa, doc.FechaFin, ahora))
                {
                    tieneAcceso = true;
                    break;
                }
            }

            // Si el socio no tiene plan vigente, se rechaza el acceso físico.
            if (!tieneAcceso)
            {
                return (false, MensajesNotificacion.SinMembresiaActivaOAccesoVencido);
            }

            // Registra el ingreso exitoso en la colección de asistencias.
            await _asistenciaRepo.AddAsync(new Asistencia
            {
                UsuarioId = usuarioId,
                FechaHoraEntrada = ahora
            });

            return (true, MensajesNotificacion.AccesoConcedidoAsistenciaExito);
        }

        // Genera el token rotativo actual mediante QRTokenManager (Singleton).
        public string GenerarTokenQR()
        {
            // Retorna el token criptográfico actual de corta duración.
            return _qrTokenService.GetCurrentToken();
        }

        // Valida el código QR y delega a IGymAccess (Proxy) para registrar asistencia y verificar estado.
        public Task<(bool Exito, string Mensaje)> ProcesarCheckInQRAsync(string token, string usuarioId)
        {
            // Verifica que el token esté presente y sea válido según el Singleton.
            if (string.IsNullOrWhiteSpace(token) || !_qrTokenService.ValidateToken(token))
            {
                return Task.FromResult((false, MensajesNotificacion.QRExpiradoOInvalido));
            }

            // Ejecuta el acceso a través del Proxy (GymAccessProxy) para validar permisos y registrar.
            bool exito = _gymAccess.RegistrarAsistencia(usuarioId);
            string mensaje = _gymAccess.MensajeError;

            return Task.FromResult((exito, mensaje));
        }
    }
}
