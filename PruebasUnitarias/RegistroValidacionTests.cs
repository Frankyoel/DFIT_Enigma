using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dfit.Models;
using Dfit.Models.Patterns.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PruebasUnitarias;

[TestClass]
[TestCategory("RegistroSeguridad")]
public class RegistroValidacionTests : PruebaDocumentada
{
    private static (bool IsValid, IList<ValidationResult> Results) ValidarModelo(object modelo)
    {
        var resultados = new List<ValidationResult>();
        var contexto = new ValidationContext(modelo, serviceProvider: null, items: null);
        bool isValid = Validator.TryValidateObject(modelo, contexto, resultados, validateAllProperties: true);
        return (isValid, resultados);
    }

    private static RegisterViewModel CrearModeloBase()
    {
        return new RegisterViewModel
        {
            Nombres = "Carlos",
            Apellidos = "Mendoza",
            Correo = "carlos@dfitpower.com",
            DNI = "72345678",
            Celular = "987654321",
            Password = "Password#2026",
            ConfirmPassword = "Password#2026"
        };
    }

    [TestMethod]
    [TestProperty("Caso", "CP-SEC-001")]
    public void Password_ConSecuenciaNumerica123456789_RechazaPorNoCumplirOWASP()
    {
        var modelo = CrearModeloBase();
        modelo.Password = "123456789";
        modelo.ConfirmPassword = "123456789";

        var (isValid, resultados) = ValidarModelo(modelo);

        Assert.IsFalse(isValid, "La clave '123456789' debió ser rechazada por estándares OWASP.");
        bool mensajeEsperado = resultados.Any(r => r.MemberNames.Contains("Password"));
        Comprobar(true, mensajeEsperado);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-SEC-002")]
    public void Password_SinSimbolosOCombinacion_RechazaPorFaltaDeEntropia()
    {
        var modelo = CrearModeloBase();
        modelo.Password = "Password1234";
        modelo.ConfirmPassword = "Password1234";

        var (isValid, resultados) = ValidarModelo(modelo);

        Assert.IsFalse(isValid, "La clave sin símbolos debió ser rechazada.");
        bool mensajePassword = resultados.Any(r => r.MemberNames.Contains("Password"));
        Comprobar(true, mensajePassword);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-SEC-003")]
    public void Password_QueIncluyeDNI_RechazaPorContenerIdentificador()
    {
        var modelo = CrearModeloBase();
        modelo.DNI = "72345678";
        modelo.Password = "Dfit72345678#";
        modelo.ConfirmPassword = "Dfit72345678#";

        var (isValid, resultados) = ValidarModelo(modelo);

        Assert.IsFalse(isValid, "La contraseña no debe poder contener el DNI del usuario.");
        bool mensajeDni = resultados.Any(r => r.ErrorMessage != null && r.ErrorMessage.Contains("DNI"));
        Comprobar(true, mensajeDni);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-SEC-004")]
    public void Password_RobustaOWASP_PasaValidacionConExito()
    {
        var modelo = CrearModeloBase();
        modelo.Password = "DfitPower#2026Seguro";
        modelo.ConfirmPassword = "DfitPower#2026Seguro";

        var (isValid, resultados) = ValidarModelo(modelo);

        Comprobar(true, isValid);
        Comprobar(0, resultados.Count);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-SEC-005")]
    public void UsuarioBuilder_SetDatosIdentidad_AsignaDniYCelularCorrectamente()
    {
        var builder = new UsuarioBuilder();
        var usuario = builder
            .SetDatosBasicos("Ana", "Gomez", "ana@dfitpower.com")
            .SetDatosIdentidad("87654321", "912345678")
            .SetPassword("Dfit#2026Secure")
            .SetRol("Usuario")
            .SetDefaults()
            .Build();

        Comprobar("87654321", usuario.DNI);
        Comprobar("912345678", usuario.Celular);
        Comprobar("ana@dfitpower.com", usuario.Correo);
    }
}
