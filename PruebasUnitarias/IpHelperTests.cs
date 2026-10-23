using System.Net;
using Dfit.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PruebasUnitarias
{
    [TestClass]
    [TestCategory("Seguridad")]
    public class IpHelperTests : PruebaDocumentada
    {
        [TestMethod]
        [TestProperty("Caso", "CP-IP-001")]
        public void ObtenerIpCliente_ConCabeceraCloudflare_RetornaIpRealDelCliente()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["CF-Connecting-IP"] = "190.235.10.50";

            var ip = IpHelper.ObtenerIpCliente(context);

            Comprobar("190.235.10.50", ip);
        }

        [TestMethod]
        [TestProperty("Caso", "CP-IP-002")]
        public void ObtenerIpCliente_ConXForwardedForMultiple_RetornaPrimeraIpPublica()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["X-Forwarded-For"] = "200.48.12.34, 10.0.0.1, 172.16.0.5";

            var ip = IpHelper.ObtenerIpCliente(context);

            Comprobar("200.48.12.34", ip);
        }

        [TestMethod]
        [TestProperty("Caso", "CP-IP-003")]
        public void ObtenerIpCliente_ConPuertoEnIp_LimpiaPuertoCorrectamente()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["X-Real-IP"] = "181.65.22.90:54321";

            var ip = IpHelper.ObtenerIpCliente(context);

            Comprobar("181.65.22.90", ip);
        }

        [TestMethod]
        [TestProperty("Caso", "CP-IP-004")]
        public void EsIpLoopbackOPrivada_IdentificaLocalhostYRedesInternas()
        {
            Comprobar(true, IpHelper.EsIpLoopbackOPrivada("127.0.0.1"));
            Comprobar(true, IpHelper.EsIpLoopbackOPrivada("::1"));
            Comprobar(true, IpHelper.EsIpLoopbackOPrivada("10.0.15.2"));
            Comprobar(true, IpHelper.EsIpLoopbackOPrivada("192.168.1.100"));
            Comprobar(true, IpHelper.EsIpLoopbackOPrivada("172.20.10.1"));
        }

        [TestMethod]
        [TestProperty("Caso", "CP-IP-005")]
        public void EsIpPublica_IdentificaIpsPublicasDeInternet()
        {
            Comprobar(true, IpHelper.EsIpPublica("200.48.12.34"));
            Comprobar(true, IpHelper.EsIpPublica("8.8.8.8"));
            Comprobar(false, IpHelper.EsIpPublica("127.0.0.1"));
            Comprobar(false, IpHelper.EsIpPublica("10.1.2.3"));
        }
    }
}
