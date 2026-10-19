using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PruebasUnitarias;

/// <summary>Registra el esperado y el resultado real en MSTest y en el informe TRX.</summary>
public abstract class PruebaDocumentada
{
    public TestContext TestContext { get; set; } = null!;

    protected void Comprobar<T>(T esperado, T obtenido)
    {
        TestContext.WriteLine($"Esperado: {Formatear(esperado)} | Obtenido: {Formatear(obtenido)}");
        Assert.AreEqual(esperado, obtenido);
    }

    private static string Formatear<T>(T valor) => valor switch
    {
        null => "<null>",
        DateTime fecha => fecha.ToString("O", CultureInfo.InvariantCulture),
        IFormattable numero => numero.ToString(null, CultureInfo.InvariantCulture),
        _ => valor.ToString()!.Replace("\r", "\\r").Replace("\n", "\\n")
    };
}
