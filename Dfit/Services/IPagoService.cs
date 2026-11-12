// Dfit/Services/IPagoService.cs
// Contrato de servicio de aplicación para finanzas, cobro de mensualidades y reportería contable.
// Desacopla la lógica de facturación y el cálculo de vigencias tras el pago.

using System.Collections.Generic;
using System.Threading.Tasks;
using Dfit.Models;

namespace Dfit.Services
{
    // Servicio de orquestación para cobranza, pasarela de pago y balances financieros.
    public interface IPagoService
    {
        // Obtiene los datos del socio y los planes activos para inicializar la vista de cobro.
        Task<(Usuario? Usuario, IReadOnlyList<Membresia> Membresias)> ObtenerDatosRegistrarPagoAsync(string usuarioId);

        // Procesa el pago aplicando la estrategia de descuento y actualiza la vigencia de la membresía.
        Task<(bool Exito, string Mensaje, decimal MontoFinal, string? NombrePlan, System.DateTime FechaFin)> ProcesarPagoAsync(Pago model);

        // Obtiene el historial de pagos de un socio con la información de sus planes asociados.
        Task<IReadOnlyList<Pago>> ObtenerPagosUsuarioAsync(string usuarioId);

        // Genera el consolidado financiero con ingresos totales, recaudación mensual y detalle de transacciones.
        Task<(IReadOnlyList<Pago> Pagos, decimal IngresosTotales, decimal PagosDelMes)> ObtenerReporteFinancieroAsync();
    }
}
