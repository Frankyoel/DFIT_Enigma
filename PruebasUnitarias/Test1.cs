using Dfit.Models.Patterns.Strategy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PruebasUnitarias;

[TestClass]
[TestCategory("Pagos")]
public class PaymentContextTests : PruebaDocumentada
{

    [TestMethod]
    [TestProperty("Caso", "CP-PAG-001")]
    public void ExecutePayment_ConTarjeta_AplicaCincoPorCiento()
    {
        var contexto = new PaymentContext();
        contexto.SetStrategy(new CardStrategy());

        var resultado = contexto.ExecutePayment(200m);

        Comprobar(210m, resultado);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-PAG-002")]
    public void ExecutePayment_ConEfectivo_NoAplicaRecargo()
    {
        var contexto = new PaymentContext();
        contexto.SetStrategy(new CashStrategy());

        var resultado = contexto.ExecutePayment(100m);

        Comprobar(100m, resultado);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-PAG-003")]
    public void ExecutePayment_SinEstrategia_UsaEfectivo()
    {
        var contexto = new PaymentContext();

        var resultado = contexto.ExecutePayment(200m);

        Comprobar(200m, resultado);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-PAG-004")]
    public void ExecutePaymentForAmount_MenorA100_UsaEfectivo()
    {
        var contexto = new PaymentContext();

        var resultado = contexto.ExecutePaymentForAmount(99.99m);

        Comprobar(99.99m, resultado);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-PAG-005")]
    public void ExecutePaymentForAmount_IgualA100_UsaEfectivo()
    {
        var contexto = new PaymentContext();

        var resultado = contexto.ExecutePaymentForAmount(100m);

        Comprobar(100m, resultado);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-PAG-006")]
    public void ExecutePaymentForAmount_MayorA100_UsaTarjetaSinRedondeoAdicional()
    {
        var contexto = new PaymentContext();

        var resultado = contexto.ExecutePaymentForAmount(100.01m);

        Comprobar(105.0105m, resultado);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-PAG-007")]
    public void ExecutePaymentForAmount_PagosConsecutivos_ReevaluaEstrategia()
    {
        var contexto = new PaymentContext();

        var primero = contexto.ExecutePaymentForAmount(200m);
        var segundo = contexto.ExecutePaymentForAmount(100m);

        Comprobar(210m, primero);
        Comprobar(100m, segundo);
    }
}
