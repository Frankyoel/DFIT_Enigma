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

    /// <summary>
    /// Calcula el nuevo período de vigencia (FechaInicio, FechaFin) para un socio.
    /// Si el socio cuenta con una membresía activa vigente, la nueva vigencia se extiende a partir de su FechaFin actual.
    /// Si no cuenta con membresía previa o ya está vencida, el nuevo período inicia en la fecha actual.
    /// </summary>
    public static (DateTime FechaInicio, DateTime FechaFin) CalcularPeriodoVigencia(DateTime? fechaFinActual, int duracionDias, DateTime ahora)
    {
        if (fechaFinActual.HasValue && fechaFinActual.Value > ahora)
        {
            return (ahora, fechaFinActual.Value.AddDays(duracionDias));
        }
        return (ahora, ahora.AddDays(duracionDias));
    }
}

