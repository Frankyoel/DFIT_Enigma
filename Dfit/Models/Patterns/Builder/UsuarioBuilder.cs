using System;
using Google.Cloud.Firestore;


namespace Dfit.Models.Patterns.Builder
{
    [FirestoreData]
    public class UsuarioBuilder
    {
        private Usuario _usuario = new Usuario();

        public UsuarioBuilder SetDatosBasicos(string nombres, string apellidos, string correo)
        {
            _usuario.Nombres = nombres;
            _usuario.Apellidos = apellidos;
            _usuario.Correo = correo;
            return this;
        }

        public UsuarioBuilder SetPassword(string password)
        {
            _usuario.PasswordHash = password; // En producción se hashearía
            return this;
        }

        public UsuarioBuilder SetRol(string rol)
        {
            _usuario.Rol = rol;
            return this;
        }

        public UsuarioBuilder SetDefaults()
        {
            _usuario.FechaRegistro = DateTime.Now;
            _usuario.Activo = true;
            return this;
        }

        public Usuario Build()
        {
            var result = _usuario;
            _usuario = new Usuario(); // Reset para uso futuro
            return result;
        }
    }
}
