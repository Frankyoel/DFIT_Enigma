using Dfit.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PruebasUnitarias;

[TestClass]
[TestCategory("Vigencia")]
public class CalculoVigenciaTests : PruebaDocumentada
{

    [TestMethod]
    [TestProperty("Caso", "CP-VIG-001")]
    public void CalcularFechaFin_MembresiaMensual_SumaExactamente30Dias()
    {
        var inicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var resultado = ReglasMembresia.CalcularFechaFin(inicio, 30);

        Comprobar(new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), resultado);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-VIG-002")]
    public void CalcularFechaFin_MembresiaAnual_SumaExactamente365Dias()
    {
        var inicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var resultado = ReglasMembresia.CalcularFechaFin(inicio, 365);

        Comprobar(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), resultado);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-VIG-003")]
    public void CalcularFechaFin_MembresiaTrimestral_SumaExactamente90Dias()
    {
        var inicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var resultado = ReglasMembresia.CalcularFechaFin(inicio, 90);

        Comprobar(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), resultado);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-VIG-004")]
    public void CalcularFechaFin_FinDeMes_SumaDiasSinUsarMesCalendario()
    {
        var inicio = new DateTime(2026, 1, 31, 14, 30, 0, DateTimeKind.Utc);

        var resultado = ReglasMembresia.CalcularFechaFin(inicio, 30);

        Comprobar(new DateTime(2026, 3, 2, 14, 30, 0, DateTimeKind.Utc), resultado);
        Comprobar(DateTimeKind.Utc, resultado.Kind);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-VIG-005")]
    public void CalcularFechaFin_AnioBisiesto_Incluye29DeFebrero()
    {
        var inicio = new DateTime(2028, 2, 28, 0, 0, 0, DateTimeKind.Utc);

        var resultado = ReglasMembresia.CalcularFechaFin(inicio, 1);

        Comprobar(new DateTime(2028, 2, 29, 0, 0, 0, DateTimeKind.Utc), resultado);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-VIG-006")]
    public void CalcularFechaFin_CambioDeAnio_ConservaHora()
    {
        var inicio = new DateTime(2026, 12, 31, 23, 30, 0, DateTimeKind.Utc);

        var resultado = ReglasMembresia.CalcularFechaFin(inicio, 1);

        Comprobar(new DateTime(2027, 1, 1, 23, 30, 0, DateTimeKind.Utc), resultado);
    }
}
