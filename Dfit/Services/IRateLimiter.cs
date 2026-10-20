using System;

namespace Dfit.Services
{
    /// <summary>
    /// Resultado generalizado de la evaluación de tasa de intentos para cualquier acción o pantalla del sistema.
    /// </summary>
    /// <param name="EstaBloqueado">Indica si la solicitud debe ser rechazada de inmediato por exceso de fallos.</param>
    /// <param name="IntentosRestantes">Cantidad de intentos disponibles antes de que se active la penalización.</param>
    /// <param name="TiempoRestanteBloqueo">Duración restante del bloqueo antes de restablecer el acceso.</param>
    /// <param name="MensajeBloqueo">Mensaje descriptivo adecuado para mostrar al usuario en la interfaz.</param>
    public record EstadoRateLimit(
        bool EstaBloqueado,
        int IntentosRestantes,
        TimeSpan TiempoRestanteBloqueo,
        string? MensajeBloqueo
    );

    /// <summary>
    /// Contrato generalizado de limitación de tasa y control de intentos para proteger cualquier pantalla o acción
    /// (ej. Login, Registro, Recuperación de contraseña, Canje de QR, Validaciones de pago) contra bots y fuerza bruta.
    /// </summary>
    public interface IRateLimiter
    {
        /// <summary>
        /// Comprueba si la acción solicitada se encuentra actualmente bloqueada para el identificador o la IP dada.
        /// No incrementa contadores, solo consulta el estado.
        /// </summary>
        EstadoRateLimit Verificar(
            string accion,
            string? identificador,
            string? direccionIp = null,
            int? maxIntentos = null,
            TimeSpan? tiempoBloqueo = null);

        /// <summary>
        /// Registra un intento fallido para la acción, identificador e IP especificados.
        /// Si se alcanza el límite de intentos, activa el bloqueo temporal.
        /// </summary>
        EstadoRateLimit RegistrarFallo(
            string accion,
            string? identificador,
            string? direccionIp = null,
            int? maxIntentos = null,
            TimeSpan? tiempoBloqueo = null);

        /// <summary>
        /// Restablece el contador de intentos y remueve cualquier bloqueo activo tras una operación exitosa.
        /// </summary>
        void RegistrarExito(string accion, string? identificador, string? direccionIp = null);

        /// <summary>
        /// Restablece un ámbito específico manualmente.
        /// </summary>
        void Restablecer(string accion, string? identificador, string? direccionIp = null);

        /// <summary>
        /// Limpia todos los registros en memoria del servicio.
        /// </summary>
        void RestablecerTodo();
    }
}
