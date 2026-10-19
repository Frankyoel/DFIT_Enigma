using System.Security.Claims;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Dfit.Utils;

namespace PruebasUnitarias
{
    [TestClass]
    public class PermisosHelperTests
    {
        [TestMethod]
        public void EsAdmin_UsuarioConRolAdmin_RetornaTrue()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-123"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var resultado = principal.EsAdmin();

            Assert.IsTrue(resultado);
        }

        [TestMethod]
        public void EsAdmin_UsuarioConRolSocio_RetornaFalse()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "socio-123"),
                new Claim(ClaimTypes.Role, "Usuario")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var resultado = principal.EsAdmin();

            Assert.IsFalse(resultado);
        }

        [TestMethod]
        public void EsAdminOPropietario_AdminAccediendoAOtroUsuario_RetornaTrue()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-001"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var resultado = principal.EsAdminOPropietario("socio-456");

            Assert.IsTrue(resultado);
        }

        [TestMethod]
        public void EsAdminOPropietario_SocioAccediendoASuPropioRecurso_RetornaTrue()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "socio-123"),
                new Claim(ClaimTypes.Role, "Usuario")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var resultado = principal.EsAdminOPropietario("socio-123");

            Assert.IsTrue(resultado);
        }

        [TestMethod]
        public void EsAdminOPropietario_SocioIntentandoAccederARecursoAjeno_RetornaFalse()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "socio-123"),
                new Claim(ClaimTypes.Role, "Usuario")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var resultado = principal.EsAdminOPropietario("socio-999");

            Assert.IsFalse(resultado);
        }

        [TestMethod]
        public void EsAdminOPropietario_UsuarioNulo_RetornaFalse()
        {
            ClaimsPrincipal? principal = null;

            var resultado = principal.EsAdminOPropietario("socio-123");

            Assert.IsFalse(resultado);
        }

        [TestMethod]
        public void ObtenerUsuarioId_RetornaIdentificadorCorrecto()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "usuario-abc")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var id = principal.ObtenerUsuarioId();

            Assert.AreEqual("usuario-abc", id);
        }
    }
}
