using System.Security.Claims;

namespace Dfit.Utils
{
    /// <summary>
    /// Métodos de utilidad y extensiones para validación de identidad, roles y permisos de acceso.
    /// Diseñado con funciones puras y reutilizables en cualquier capa del sistema.
    /// </summary>
    public static class PermisosHelper
    {
        public const string RolAdmin = "Admin";
        public const string RolUsuario = "Usuario";

        /// <summary>
        /// Comprueba si el usuario autenticado tiene rol de Administrador o es el propietario del recurso.
        /// </summary>
        public static bool EsAdminOPropietario(this ClaimsPrincipal? usuario, string? usuarioIdObjetivo)
        {
            if (usuario == null)
            {
                return false;
            }

            if (usuario.EsAdmin())
            {
                return true;
            }

            var idActual = usuario.ObtenerUsuarioId();
            return !string.IsNullOrEmpty(idActual) && idActual == usuarioIdObjetivo;
        }

        /// <summary>
        /// Comprueba si el usuario autenticado tiene el rol de Administrador.
        /// </summary>
        public static bool EsAdmin(this ClaimsPrincipal? usuario)
        {
            return usuario?.IsInRole(RolAdmin) ?? false;
        }

        /// <summary>
        /// Obtiene el identificador del usuario autenticado a partir de las claims de identidad.
        /// </summary>
        public static string? ObtenerUsuarioId(this ClaimsPrincipal? usuario)
        {
            return usuario?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
