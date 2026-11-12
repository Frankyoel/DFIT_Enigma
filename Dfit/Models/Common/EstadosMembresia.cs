// Dfit/Models/Common/EstadosMembresia.cs
// Estados de vigencia y ciclo de vida de membresías y asignaciones en DFIT Power.
// Utilizado para evaluar el acceso al gimnasio y los indicadores de recepción.

namespace Dfit.Models.Common
{
    // Constantes canónicas para estados de membresía y suscripciones.
    public static class EstadosMembresia
    {
        // Membresía vigente con fecha de vencimiento mayor al momento actual.
        public const string Activa = "Activa";

        // Membresía cuya fecha de vigencia ha expirado.
        public const string Vencida = "Vencida";

        // Membresía activa próxima a expirar en un umbral menor o igual a 5 días.
        public const string PorVencer = "Por Vencer";

        // Membresía cancelada manualmente por el socio o administración.
        public const string Cancelada = "Cancelada";

        // Membresía que fue reemplazada tras adquirir un nuevo plan.
        public const string Reemplazada = "Reemplazada";

        // Usuario registrado que aún no cuenta con ninguna membresía asignada.
        public const string SinPlan = "Sin Plan";
    }
}
