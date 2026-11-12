// Dfit/Models/Common/RolesSistema.cs
// Definición centralizada de roles del sistema DFIT Power para control de acceso y autorización.
// Evita el uso de cadenas mágicas y facilita la coherencia de permisos según OWASP A01.

namespace Dfit.Models.Common
{
    // Roles de usuario disponibles en el sistema con permisos diferenciados.
    public static class RolesSistema
    {
        // Administrador con acceso global a todos los módulos y configuraciones.
        public const string Admin = "Admin";

        // Recepcionista encargado de cobros, registro de socios y control de acceso.
        public const string Recepcionista = "Recepcionista";

        // Entrenador enfocado en rutinas, horarios de clases y evaluaciones físicas.
        public const string Entrenador = "Entrenador";

        // Socio regular del gimnasio con acceso a su portal personal.
        public const string Usuario = "Usuario";

        // Combinación de roles con privilegios de gestión de entrenamiento.
        public const string AdminOEntrenador = "Admin,Entrenador";

        // Combinación de roles con privilegios operativos de recepción.
        public const string AdminORecepcionista = "Admin,Recepcionista";
    }
}
