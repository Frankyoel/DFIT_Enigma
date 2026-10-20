using System;
using System.Collections.Concurrent;

namespace Dfit.Services
{
    /// <summary>
    /// Implementación generalizada y thread-safe del limitador de tasa de intentos.
    /// Diseñado para ser reutilizado en cualquier pantalla, controlador o flujo del sistema DFIT
    /// aislando los contadores por tipo de acción (login, registro, pagos, QR, etc.).
    /// </summary>
    public class RateLimiter : IRateLimiter
    {
        public const int MaxIntentosPorDefecto = 5;
        public static readonly TimeSpan TiempoBloqueoPorDefecto = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan VentanaExpiracionPorDefecto = TimeSpan.FromMinutes(15);

        private readonly int _maxIntentosPredeterminado;
        private readonly TimeSpan _tiempoBloqueoPredeterminado;
        private readonly TimeSpan _ventanaExpiracion;
        private readonly Func<DateTime> _relojUtc;
        private readonly ConcurrentDictionary<string, RegistroIntento> _registros;

        public RateLimiter(
            int maxIntentosPorDefecto = MaxIntentosPorDefecto,
            TimeSpan? tiempoBloqueoPorDefecto = null,
            TimeSpan? ventanaExpiracion = null,
            Func<DateTime>? relojUtc = null)
        {
            _maxIntentosPredeterminado = maxIntentosPorDefecto > 0 ? maxIntentosPorDefecto : MaxIntentosPorDefecto;
            _tiempoBloqueoPredeterminado = tiempoBloqueoPorDefecto ?? TiempoBloqueoPorDefecto;
            _ventanaExpiracion = ventanaExpiracion ?? VentanaExpiracionPorDefecto;
            _relojUtc = relojUtc ?? (() => DateTime.UtcNow);
            _registros = new ConcurrentDictionary<string, RegistroIntento>(StringComparer.OrdinalIgnoreCase);
        }

        // =========================================================================
        // 1. FLUJO PRINCIPAL DE ALTO NIVEL (STEP-DOWN RULE)
        // =========================================================================

        public EstadoRateLimit Verificar(
            string accion,
            string? identificador,
            string? direccionIp = null,
            int? maxIntentos = null,
            TimeSpan? tiempoBloqueo = null)
        {
            var limite = ResolverMaxIntentos(maxIntentos);
            var penalizacion = ResolverTiempoBloqueo(tiempoBloqueo);
            var ahora = _relojUtc();

            var claveId = GenerarClaveIdentificador(accion, identificador);
            var claveIp = GenerarClaveIp(accion, direccionIp);

            var estadoId = EvaluarClave(claveId, limite, penalizacion, ahora);
            if (estadoId.EstaBloqueado)
            {
                return estadoId;
            }

            var estadoIp = EvaluarClave(claveIp, limite, penalizacion, ahora);
            if (estadoIp.EstaBloqueado)
            {
                return estadoIp;
            }

            var restantes = Math.Min(estadoId.IntentosRestantes, estadoIp.IntentosRestantes);
            return new EstadoRateLimit(
                EstaBloqueado: false,
                IntentosRestantes: restantes,
                TiempoRestanteBloqueo: TimeSpan.Zero,
                MensajeBloqueo: null
            );
        }

        public EstadoRateLimit RegistrarFallo(
            string accion,
            string? identificador,
            string? direccionIp = null,
            int? maxIntentos = null,
            TimeSpan? tiempoBloqueo = null)
        {
            var limite = ResolverMaxIntentos(maxIntentos);
            var penalizacion = ResolverTiempoBloqueo(tiempoBloqueo);
            var ahora = _relojUtc();

            var claveId = GenerarClaveIdentificador(accion, identificador);
            var claveIp = GenerarClaveIp(accion, direccionIp);

            var resultadoId = IncrementarFalloClave(claveId, limite, penalizacion, ahora);
            var resultadoIp = IncrementarFalloClave(claveIp, limite, penalizacion, ahora);

            if (resultadoId.EstaBloqueado)
            {
                return resultadoId;
            }

            if (resultadoIp.EstaBloqueado)
            {
                return resultadoIp;
            }

            var restantes = Math.Min(resultadoId.IntentosRestantes, resultadoIp.IntentosRestantes);
            return new EstadoRateLimit(
                EstaBloqueado: false,
                IntentosRestantes: restantes,
                TiempoRestanteBloqueo: TimeSpan.Zero,
                MensajeBloqueo: ConstruirMensajeFallo(restantes)
            );
        }

        public void RegistrarExito(string accion, string? identificador, string? direccionIp = null)
        {
            Restablecer(accion, identificador, direccionIp);
        }

        public void Restablecer(string accion, string? identificador, string? direccionIp = null)
        {
            var claveId = GenerarClaveIdentificador(accion, identificador);
            if (claveId != null)
            {
                _registros.TryRemove(claveId, out _);
            }

            var claveIp = GenerarClaveIp(accion, direccionIp);
            if (claveIp != null)
            {
                _registros.TryRemove(claveIp, out _);
            }
        }

        public void RestablecerTodo()
        {
            _registros.Clear();
        }

        // =========================================================================
        // 2. MÉTODOS AUXILIARES DE EVALUACIÓN Y CONCURRENCIA
        // =========================================================================

        private EstadoRateLimit EvaluarClave(string? clave, int maxIntentos, TimeSpan tiempoBloqueo, DateTime ahora)
        {
            if (string.IsNullOrEmpty(clave))
            {
                return new EstadoRateLimit(false, maxIntentos, TimeSpan.Zero, null);
            }

            if (!_registros.TryGetValue(clave, out var registro))
            {
                return new EstadoRateLimit(false, maxIntentos, TimeSpan.Zero, null);
            }

            lock (registro)
            {
                if (registro.BloqueadoHastaUtc.HasValue)
                {
                    if (ahora < registro.BloqueadoHastaUtc.Value)
                    {
                        var restante = registro.BloqueadoHastaUtc.Value - ahora;
                        return new EstadoRateLimit(
                            EstaBloqueado: true,
                            IntentosRestantes: 0,
                            TiempoRestanteBloqueo: restante,
                            MensajeBloqueo: ConstruirMensajeBloqueo(restante)
                        );
                    }

                    // Bloqueo finalizado
                    registro.ContadorFallos = 0;
                    registro.BloqueadoHastaUtc = null;
                }
                else if (ahora - registro.UltimoFalloUtc > _ventanaExpiracion)
                {
                    registro.ContadorFallos = 0;
                }

                var disponibles = Math.Max(0, maxIntentos - registro.ContadorFallos);
                return new EstadoRateLimit(false, disponibles, TimeSpan.Zero, null);
            }
        }

        private EstadoRateLimit IncrementarFalloClave(string? clave, int maxIntentos, TimeSpan tiempoBloqueo, DateTime ahora)
        {
            if (string.IsNullOrEmpty(clave))
            {
                return new EstadoRateLimit(false, maxIntentos, TimeSpan.Zero, null);
            }

            var registro = _registros.GetOrAdd(clave, _ => new RegistroIntento
            {
                ContadorFallos = 0,
                UltimoFalloUtc = ahora,
                BloqueadoHastaUtc = null
            });

            lock (registro)
            {
                if (registro.BloqueadoHastaUtc.HasValue && ahora < registro.BloqueadoHastaUtc.Value)
                {
                    var restante = registro.BloqueadoHastaUtc.Value - ahora;
                    return new EstadoRateLimit(
                        EstaBloqueado: true,
                        IntentosRestantes: 0,
                        TiempoRestanteBloqueo: restante,
                        MensajeBloqueo: ConstruirMensajeBloqueo(restante)
                    );
                }

                if (registro.BloqueadoHastaUtc.HasValue && ahora >= registro.BloqueadoHastaUtc.Value)
                {
                    registro.ContadorFallos = 0;
                    registro.BloqueadoHastaUtc = null;
                }
                else if (ahora - registro.UltimoFalloUtc > _ventanaExpiracion)
                {
                    registro.ContadorFallos = 0;
                }

                registro.ContadorFallos++;
                registro.UltimoFalloUtc = ahora;

                if (registro.ContadorFallos >= maxIntentos)
                {
                    registro.BloqueadoHastaUtc = ahora.Add(tiempoBloqueo);
                    return new EstadoRateLimit(
                        EstaBloqueado: true,
                        IntentosRestantes: 0,
                        TiempoRestanteBloqueo: tiempoBloqueo,
                        MensajeBloqueo: ConstruirMensajeBloqueo(tiempoBloqueo)
                    );
                }

                var restantes = Math.Max(0, maxIntentos - registro.ContadorFallos);
                return new EstadoRateLimit(
                    EstaBloqueado: false,
                    IntentosRestantes: restantes,
                    TiempoRestanteBloqueo: TimeSpan.Zero,
                    MensajeBloqueo: ConstruirMensajeFallo(restantes)
                );
            }
        }

        // =========================================================================
        // 3. GENERACIÓN DE CLAVES AISLADAS Y FORMATO DE MENSAJES
        // =========================================================================

        private static string? GenerarClaveIdentificador(string accion, string? identificador)
        {
            if (string.IsNullOrWhiteSpace(identificador)) return null;
            var accionNormalizada = accion.Trim().ToLowerInvariant();
            var idNormalizado = identificador.Trim().ToLowerInvariant();
            return $"act:{accionNormalizada}:id:{idNormalizado}";
        }

        private static string? GenerarClaveIp(string accion, string? ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return null;
            var accionNormalizada = accion.Trim().ToLowerInvariant();
            var ipNormalizada = ip.Trim();
            return $"act:{accionNormalizada}:ip:{ipNormalizada}";
        }

        private int ResolverMaxIntentos(int? maxIntentos) =>
            maxIntentos is > 0 ? maxIntentos.Value : _maxIntentosPredeterminado;

        private TimeSpan ResolverTiempoBloqueo(TimeSpan? tiempoBloqueo) =>
            tiempoBloqueo ?? _tiempoBloqueoPredeterminado;

        private static string ConstruirMensajeBloqueo(TimeSpan tiempoRestante)
        {
            var minutos = (int)Math.Ceiling(tiempoRestante.TotalMinutes);
            if (minutos <= 1)
            {
                var segundos = Math.Max(1, (int)tiempoRestante.TotalSeconds);
                return $"Has superado el límite de intentos permitidos. Inténtalo de nuevo en {segundos} segundo{(segundos == 1 ? "" : "s")}.";
            }

            return $"Has superado el límite de intentos permitidos. Inténtalo de nuevo en {minutos} minuto{(minutos == 1 ? "" : "s")}.";
        }

        private static string ConstruirMensajeFallo(int intentosRestantes)
        {
            if (intentosRestantes == 1)
            {
                return "Datos incorrectos. Te queda 1 intento antes de suspender temporalmente el acceso.";
            }

            return $"Datos incorrectos. Te quedan {intentosRestantes} intentos disponibles.";
        }

        // =========================================================================
        // 4. ESTRUCTURA INTERNA
        // =========================================================================

        private sealed class RegistroIntento
        {
            public int ContadorFallos { get; set; }
            public DateTime UltimoFalloUtc { get; set; }
            public DateTime? BloqueadoHastaUtc { get; set; }
        }
    }
}
