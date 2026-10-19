namespace Dfit.Models;

/// <summary>Reglas de vigencia y acceso independientes del almacenamiento.</summary>
public static class ReglasMembresia
{
    // Conserva la regla existente: dias completos, no meses de calendario.
    // El llamador proporciona el instante para no depender del reloj en las pruebas.
    public static DateTime CalcularFechaFin(DateTime fechaInicio, int duracionDias)
        => fechaInicio.AddDays(duracionDias);

    // Al llegar exactamente a FechaFin la membresia ya no permite acceso.
    public static bool PermiteAcceso(bool activa, DateTime fechaFin, DateTime ahora)
        => activa && fechaFin > ahora;
}
