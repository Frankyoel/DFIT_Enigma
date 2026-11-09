// Dfit/Utils/MensajesNotificacion.cs
// Catálogo centralizado de mensajes informativos, de éxito y error para la interfaz web.
// Cumple con la directriz de internacionalización (i18n) eliminando textos hardcodeados en controladores.

namespace Dfit.Utils
{
    // Cadenas canónicas de notificación consumidas por TempData y ViewBag.
    public static class MensajesNotificacion
    {
        // Mensajes de Validación y Errores Operativos
        public const string CamposObligatoriosIncompletos = "Los campos Nombre, Apellidos y Correo son requeridos.";
        public const string CorreoYaRegistrado = "El correo electrónico '{0}' ya se encuentra registrado.";
        public const string DatosAsignacionIncompletos = "Datos de asignación incompletos.";
        public const string UsuarioNoEncontrado = "El usuario especificado no fue encontrado en el sistema.";
        public const string SinMembresiaActivaOAccesoVencido = "El usuario no tiene una membresía activa o está vencida.";
        public const string QRExpiradoOInvalido = "El código QR ha expirado o es inválido. Acércate a la pantalla y escanea el nuevo código.";

        // Mensajes de Éxito en Operaciones
        public const string PerfilActualizadoExito = "Perfil actualizado correctamente.";
        public const string RutinaAsignadaExito = "Rutina asignada exitosamente al usuario.";
        public const string RutinaDesasignadaExito = "Rutina desasignada correctamente.";
        public const string EvaluacionRegistradaExito = "Evaluación física registrada correctamente.";
        public const string NotasEntrenadorActualizadasExito = "Notas del entrenador actualizadas correctamente.";
        public const string AsistenciaClaseRegistradaExito = "Asistencia registrada para la clase '{0}'.";
        public const string AccesoConcedidoAsistenciaExito = "Acceso concedido y asistencia registrada.";
        public const string SocioRegistradoExito = "Socio {0} {1} registrado con éxito.";
        public const string SocioYPlanRegistradoExito = "Socio {0} registrado y membresía '{1}' activada hasta el {2:dd/MM/yyyy}.";
        public const string PagoRegistradoExito = "Pago de S/ {0:N2} registrado con éxito. Membresía '{1}' actualizada hasta el {2:dd/MM/yyyy}.";
    }
}
