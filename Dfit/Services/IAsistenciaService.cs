// Dfit/Services/IAsistenciaService.cs
// Contrato de servicio de aplicación para control de acceso, aforo y check-in por código QR.
// Encapsula las reglas de acceso al gimnasio y la emisión de tokens dinámicos.

using System.Collections.Generic;
using System.Threading.Tasks;
using Dfit.Models;

namespace Dfit.Services
{
    // Servicio de orquestación para control de asistencia, accesos y validación QR.
    public interface IAsistenciaService
    {
        // Obtiene las asistencias registradas en la jornada de hoy y la lista de socios activos.
        Task<(IReadOnlyList<Asistencia> AsistenciasHoy, IReadOnlyList<Usuario> UsuariosActivos)> ObtenerAsistenciaDashboardAsync();

        // Registra el ingreso manual de un socio verificando previamente la vigencia de su plan.
        Task<(bool Exito, string Mensaje)> RegistrarAsistenciaManualAsync(string usuarioId);

        // Genera o recupera el token criptográfico actual para la pantalla QR de recepción.
        string GenerarTokenQR();

        // Valida el código QR escaneado por el socio y efectúa el check-in físico en el torniquete.
        (bool Exito, string Mensaje) CheckInQR(string token, string userId);
    }
}
