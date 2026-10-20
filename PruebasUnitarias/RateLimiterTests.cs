using System;
using System.Threading.Tasks;
using Dfit.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PruebasUnitarias
{
    [TestClass]
    [TestCategory("Seguridad")]
    public class RateLimiterTests : PruebaDocumentada
    {
        private DateTime _relojSimulado = new(2026, 10, 19, 19, 0, 0, DateTimeKind.Utc);

        [TestMethod]
        [TestProperty("Caso", "CP-SEC-001")]
        public void Verificar_SinIntentosPrevios_PermiteAccesoConMaximosIntentos()
        {
            var limiter = new RateLimiter(maxIntentosPorDefecto: 5, relojUtc: () => _relojSimulado);

            var resultado = limiter.Verificar("login", "usuario@dfit.com", "192.168.1.50");

            Comprobar(false, resultado.EstaBloqueado);
            Comprobar(5, resultado.IntentosRestantes);
            Assert.IsNull(resultado.MensajeBloqueo);
        }

        [TestMethod]
        [TestProperty("Caso", "CP-SEC-002")]
        public void RegistrarFallo_DecrementaIntentosRestantes()
        {
            var limiter = new RateLimiter(maxIntentosPorDefecto: 5, relojUtc: () => _relojSimulado);

            var primero = limiter.RegistrarFallo("login", "socio@dfit.com", "10.0.0.1");
            var segundo = limiter.RegistrarFallo("login", "socio@dfit.com", "10.0.0.1");

            Comprobar(false, primero.EstaBloqueado);
            Comprobar(4, primero.IntentosRestantes);
            Comprobar(false, segundo.EstaBloqueado);
            Comprobar(3, segundo.IntentosRestantes);
        }

        [TestMethod]
        [TestProperty("Caso", "CP-SEC-003")]
        public void RegistrarFallo_AlAlcanzarLimite_BloqueaAccesoPorTiempoConfigurado()
        {
            var tiempoBloqueo = TimeSpan.FromMinutes(15);
            var limiter = new RateLimiter(maxIntentosPorDefecto: 3, tiempoBloqueoPorDefecto: tiempoBloqueo, relojUtc: () => _relojSimulado);

            limiter.RegistrarFallo("login", "victima@dfit.com", "192.168.1.100");
            limiter.RegistrarFallo("login", "victima@dfit.com", "192.168.1.100");
            var resultadoTercero = limiter.RegistrarFallo("login", "victima@dfit.com", "192.168.1.100");

            Comprobar(true, resultadoTercero.EstaBloqueado);
            Comprobar(0, resultadoTercero.IntentosRestantes);
            Comprobar(tiempoBloqueo, resultadoTercero.TiempoRestanteBloqueo);
            Assert.IsNotNull(resultadoTercero.MensajeBloqueo);
        }

        [TestMethod]
        [TestProperty("Caso", "CP-SEC-004")]
        public void Verificar_CuandoEstaBloqueado_RetornaBloqueadoSinModificarEstado()
        {
            var limiter = new RateLimiter(maxIntentosPorDefecto: 2, relojUtc: () => _relojSimulado);

            limiter.RegistrarFallo("login", "admin@dfit.com", "127.0.0.1");
            limiter.RegistrarFallo("login", "admin@dfit.com", "127.0.0.1");

            var consulta = limiter.Verificar("login", "admin@dfit.com", "127.0.0.1");

            Comprobar(true, consulta.EstaBloqueado);
            Comprobar(0, consulta.IntentosRestantes);
            Assert.IsTrue(consulta.TiempoRestanteBloqueo > TimeSpan.Zero);
        }

        [TestMethod]
        [TestProperty("Caso", "CP-SEC-005")]
        public void RegistrarExito_RestableceContadorDeIntentos()
        {
            var limiter = new RateLimiter(maxIntentosPorDefecto: 5, relojUtc: () => _relojSimulado);

            limiter.RegistrarFallo("login", "socio@dfit.com", "192.168.1.20");
            limiter.RegistrarFallo("login", "socio@dfit.com", "192.168.1.20");

            var intermedio = limiter.Verificar("login", "socio@dfit.com", "192.168.1.20");
            Comprobar(3, intermedio.IntentosRestantes);

            limiter.RegistrarExito("login", "socio@dfit.com", "192.168.1.20");

            var restaurado = limiter.Verificar("login", "socio@dfit.com", "192.168.1.20");
            Comprobar(false, restaurado.EstaBloqueado);
            Comprobar(5, restaurado.IntentosRestantes);
        }

        [TestMethod]
        [TestProperty("Caso", "CP-SEC-006")]
        public void RegistrarFallo_BloqueoPorIp_ProtegeContraPasswordSpraying()
        {
            var limiter = new RateLimiter(maxIntentosPorDefecto: 3, relojUtc: () => _relojSimulado);
            var ipAtacante = "203.0.113.195";

            limiter.RegistrarFallo("login", "cuenta1@dfit.com", ipAtacante);
            limiter.RegistrarFallo("login", "cuenta2@dfit.com", ipAtacante);
            var resultadoTercero = limiter.RegistrarFallo("login", "cuenta3@dfit.com", ipAtacante);

            Comprobar(true, resultadoTercero.EstaBloqueado);

            // Cualquier nueva cuenta intentada desde esa IP debe ser bloqueada
            var siguienteVictima = limiter.Verificar("login", "cuentaNueva@dfit.com", ipAtacante);
            Comprobar(true, siguienteVictima.EstaBloqueado);
        }

        [TestMethod]
        [TestProperty("Caso", "CP-SEC-007")]
        public void RegistrarFallo_BloqueoPorCuenta_ProtegeContraAtaqueDistribuido()
        {
            var limiter = new RateLimiter(maxIntentosPorDefecto: 3, relojUtc: () => _relojSimulado);
            var correoObjetivo = "ceo@dfit.com";

            limiter.RegistrarFallo("login", correoObjetivo, "198.51.100.1");
            limiter.RegistrarFallo("login", correoObjetivo, "198.51.100.2");
            var bloqueo = limiter.RegistrarFallo("login", correoObjetivo, "198.51.100.3");

            Comprobar(true, bloqueo.EstaBloqueado);

            // Incluso desde una IP limpia nueva, la cuenta está protegida y bloqueada
            var intentoNuevaIp = limiter.Verificar("login", correoObjetivo, "198.51.100.99");
            Comprobar(true, intentoNuevaIp.EstaBloqueado);
        }

        [TestMethod]
        [TestProperty("Caso", "CP-SEC-008")]
        public void Verificar_TrasExpirarTiempoDeBloqueo_RestauraAccesoAutomaticamente()
        {
            var tiempoBloqueo = TimeSpan.FromMinutes(15);
            var limiter = new RateLimiter(maxIntentosPorDefecto: 2, tiempoBloqueoPorDefecto: tiempoBloqueo, relojUtc: () => _relojSimulado);

            limiter.RegistrarFallo("login", "usuario@dfit.com", "192.168.1.1");
            limiter.RegistrarFallo("login", "usuario@dfit.com", "192.168.1.1");

            var bloqueado = limiter.Verificar("login", "usuario@dfit.com", "192.168.1.1");
            Comprobar(true, bloqueado.EstaBloqueado);

            // Avanzar el reloj simulado 15 minutos y 1 segundo
            _relojSimulado = _relojSimulado.Add(tiempoBloqueo).AddSeconds(1);

            var rehabilitado = limiter.Verificar("login", "usuario@dfit.com", "192.168.1.1");
            Comprobar(false, rehabilitado.EstaBloqueado);
            Comprobar(2, rehabilitado.IntentosRestantes);
        }

        [TestMethod]
        [TestProperty("Caso", "CP-SEC-009")]
        public void Reutilizacion_MultiplesPantallas_AislaLimitesSinInterferirEntreSi()
        {
            // Demuestra la reutilización en distintas pantallas (Login vs Recuperar Contraseña vs Registro)
            var limiter = new RateLimiter(maxIntentosPorDefecto: 3, relojUtc: () => _relojSimulado);
            var usuario = "socio@dfit.com";
            var ip = "192.168.1.10";

            // Bloquear la pantalla de recuperación de contraseña con 3 fallos
            limiter.RegistrarFallo("recuperar-password", usuario, ip);
            limiter.RegistrarFallo("recuperar-password", usuario, ip);
            var estadoRecuperacion = limiter.RegistrarFallo("recuperar-password", usuario, ip);

            Comprobar(true, estadoRecuperacion.EstaBloqueado);

            // La pantalla de login NO debe estar bloqueada para ese mismo usuario
            var estadoLogin = limiter.Verificar("login", usuario, ip);
            Comprobar(false, estadoLogin.EstaBloqueado);
            Comprobar(3, estadoLogin.IntentosRestantes);

            // La pantalla de registro tampoco se ve afectada
            var estadoRegistro = limiter.Verificar("registro", usuario, ip);
            Comprobar(false, estadoRegistro.EstaBloqueado);
        }

        [TestMethod]
        [TestProperty("Caso", "CP-SEC-010")]
        public void Reutilizacion_ParametrosPersonalizadosPorPantalla_RespetaConfiguracionPropia()
        {
            // Una pantalla crítica (ej. canje de QR o PIN) puede definir 2 intentos y 5 min de penalización
            var limiter = new RateLimiter(maxIntentosPorDefecto: 5, relojUtc: () => _relojSimulado);
            var qrToken = "token-qr-invalido";
            var ip = "10.0.0.50";

            limiter.RegistrarFallo("canje-qr", qrToken, ip, maxIntentos: 2, tiempoBloqueo: TimeSpan.FromMinutes(5));
            var estadoQr = limiter.RegistrarFallo("canje-qr", qrToken, ip, maxIntentos: 2, tiempoBloqueo: TimeSpan.FromMinutes(5));

            Comprobar(true, estadoQr.EstaBloqueado);
            Comprobar(TimeSpan.FromMinutes(5), estadoQr.TiempoRestanteBloqueo);
        }

        [TestMethod]
        [TestProperty("Caso", "CP-SEC-011")]
        public void RegistrarFallo_ConcurrenciaMultihilo_MantieneIntegridadDelContador()
        {
            var limiter = new RateLimiter(maxIntentosPorDefecto: 10, relojUtc: () => _relojSimulado);
            var correo = "concurrencia@dfit.com";
            var ip = "10.10.10.10";

            Parallel.For(0, 10, _ =>
            {
                limiter.RegistrarFallo("login", correo, ip);
            });

            var estadoFinal = limiter.Verificar("login", correo, ip);
            Comprobar(true, estadoFinal.EstaBloqueado);
            Comprobar(0, estadoFinal.IntentosRestantes);
        }

        [TestMethod]
        [TestProperty("Caso", "CP-SEC-012")]
        public void Verificar_EstadoBloqueo_CalculaSegundosRestantesPrecisosParaUI()
        {
            var bloqueo = TimeSpan.FromMinutes(10);
            var limiter = new RateLimiter(maxIntentosPorDefecto: 2, tiempoBloqueoPorDefecto: bloqueo, relojUtc: () => _relojSimulado);
            var correo = "bloqueo-ui@dfit.com";
            var ip = "127.0.0.1";

            limiter.RegistrarFallo("login", correo, ip);
            var segundoFallo = limiter.RegistrarFallo("login", correo, ip);

            Comprobar(true, segundoFallo.EstaBloqueado);
            Comprobar(600, (int)segundoFallo.TiempoRestanteBloqueo.TotalSeconds);

            // Simular paso de 3 minutos
            _relojSimulado = _relojSimulado.AddMinutes(3);

            var consultaPosterior = limiter.Verificar("login", correo, ip);
            Comprobar(true, consultaPosterior.EstaBloqueado);
            Comprobar(420, (int)consultaPosterior.TiempoRestanteBloqueo.TotalSeconds);
        }
    }
}
