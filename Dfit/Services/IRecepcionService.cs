// Dfit/Services/IRecepcionService.cs
// Contrato de servicio de aplicación para la consola operativa de recepción.
// Centraliza el monitoreo de vigencias de socios, cobranza ágil en mostrador y altas de usuarios.

using System.Threading.Tasks;
using Dfit.Models;

namespace Dfit.Services
{
    // Servicio de orquestación para el panel de recepción del gimnasio.
    public interface IRecepcionService
    {
        // Construye el modelo integral para la consola de recepción con KPIs en tiempo real.
        Task<RecepcionViewModel> ObtenerRecepcionDashboardAsync(string? search = null, string? estado = null);

        // Registra un nuevo socio desde mostrador con opción de cobro y activación de plan inmediata.
        Task<(bool Exito, string Mensaje, string? NombreSocio)> RegistrarSocioRecepcionAsync(
            Usuario model,
            string? membresiaId = null,
            decimal? montoPago = null,
            string? metodoPago = null);
    }
}
