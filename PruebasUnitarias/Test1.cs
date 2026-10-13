using Microsoft.VisualStudio.TestTools.UnitTesting;
using Dfit.Models.Patterns.Strategy;

namespace PruebasUnitarias;

[TestClass]
public class PaymentContextTests
{
    [TestMethod]
    public void RegistrarPago_AplicaEstrategiaTarjeta_CuandoMontoMayorA100()
    {
        var Paymentctx = new PaymentContext();
        Paymentctx.SetStrategy(new CardStrategy());
        var result = Paymentctx.ExecutePayment(200m);
        Assert.AreEqual(200m * 1.05m, result);
    }
    
    [TestMethod]
    public void RegistrarPago_AplicaEstrategiaEfectivo_CuandoMontoMenorOIgual100()
    {
        var Paymentctx = new PaymentContext();
        Paymentctx.SetStrategy(new CashStrategy());
        var result = Paymentctx.ExecutePayment(100m);
        Assert.AreEqual(100m, result);
    }
}