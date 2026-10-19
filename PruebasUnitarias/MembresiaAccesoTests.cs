using Dfit.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PruebasUnitarias;

[TestClass]
[TestCategory("Acceso")]
public class MembresiaAccesoTests : PruebaDocumentada
{
    private static readonly DateTime Ahora = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    [TestProperty("Caso", "CP-ACC-001")]
    public void ValidarAcceso_ConMembresiaVigenteYActiva_DebePermitirAcceso()
    {
        var fin = new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);

        var resultado = ReglasMembresia.PermiteAcceso(true, fin, Ahora);

        Comprobar(true, resultado);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ACC-002")]
    public void ValidarAcceso_ConMembresiaVencida_DebeDenegarAcceso()
    {
        var fin = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);

        var resultado = ReglasMembresia.PermiteAcceso(true, fin, Ahora);

        Comprobar(false, resultado);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ACC-003")]
    public void ValidarAcceso_ConMembresiaInactiva_DebeDenegarAcceso()
    {
        var fin = new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);

        var resultado = ReglasMembresia.PermiteAcceso(false, fin, Ahora);

        Comprobar(false, resultado);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ACC-004")]
    public void ValidarAcceso_EnInstanteExactoDeVencimiento_DebeDenegarAcceso()
    {
        var fin = Ahora;

        var resultado = ReglasMembresia.PermiteAcceso(true, fin, Ahora);

        Comprobar(false, resultado);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ACC-005")]
    public void ValidarAcceso_UnTickAntesDeVencer_DebePermitirAcceso()
    {
        var fin = Ahora.AddTicks(1);

        var resultado = ReglasMembresia.PermiteAcceso(true, fin, Ahora);

        Comprobar(true, resultado);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ACC-006")]
    public void ValidarAcceso_UnTickDespuesDeVencer_DebeDenegarAcceso()
    {
        var fin = Ahora.AddTicks(-1);

        var resultado = ReglasMembresia.PermiteAcceso(true, fin, Ahora);

        Comprobar(false, resultado);
    }
}
