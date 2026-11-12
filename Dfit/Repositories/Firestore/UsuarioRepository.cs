// Dfit/Repositories/Firestore/UsuarioRepository.cs
// Implementación de acceso a datos para la colección 'Usuarios' en Google Cloud Firestore.
// Encapsula las operaciones CRUD y búsquedas para desacoplar el resto de la aplicación.
// Cumple con la directriz de arquitectura hexagonal aislando la persistencia NoSQL.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Dfit.Models;
using Dfit.Repositories.Contracts;

namespace Dfit.Repositories.Firestore
{
    // Repositorio Firestore para la administración y consulta de usuarios y socios.
    // Proporciona métodos optimizados para evitar descargas innecesarias y reducir costos de lectura.
    public class UsuarioRepository : IUsuarioRepository
    {
        // Instancia del cliente Cloud Firestore inyectada a través del contenedor de dependencias.
        private readonly FirestoreDb _db;

        // Nombre canónico de la colección de usuarios en Cloud Firestore.
        private const string Coleccion = "Usuarios";

        // Constructor con inyección del cliente de Cloud Firestore configurado.
        public UsuarioRepository(FirestoreDb db)
        {
            _db = db;
        }

        // Obtiene un usuario a partir de su ID de documento en Firestore.
        // Retorna null si el identificador es nulo, vacío o si el documento no existe.
        public async Task<Usuario?> GetByIdAsync(string id)
        {
            // Cláusula de guarda: si el ID no es válido se aborta inmediatamente.
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            // Consulta puntual de lectura directa por clave primaria en Firestore.
            var doc = await _db.Collection(Coleccion).Document(id).GetSnapshotAsync();

            // Si el documento no existe en la colección, se retorna referencia nula controlada.
            if (!doc.Exists)
            {
                return null;
            }

            // Transforma el snapshot de Firestore en la entidad fuertemente tipada.
            return MapearUsuario(doc);
        }

        // Obtiene todos los usuarios registrados en el sistema.
        // Utilizado principalmente en vistas de reportería global o consola administrativa.
        public async Task<IReadOnlyList<Usuario>> GetAllAsync()
        {
            // Ejecución de la consulta asíncrona sobre la totalidad de la colección.
            var snap = await _db.Collection(Coleccion).GetSnapshotAsync();
            var lista = new List<Usuario>();

            // Iteración de documentos para realizar la deserialización controlada.
            foreach (var doc in snap.Documents)
            {
                lista.Add(MapearUsuario(doc));
            }

            return lista;
        }

        // Busca un usuario por su correo electrónico exacto.
        // Utilizado en autenticación, validación de unicidad y registro ágil de recepción.
        public async Task<Usuario?> GetByEmailAsync(string email)
        {
            // Cláusula de guarda para evitar consultas innecesarias con cadenas vacías.
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            // Normalización del correo para evitar fallos por espacios accidentales.
            var correoNormalizado = email.Trim();

            // Consulta indexada en Firestore utilizando filtro de igualdad.
            var snap = await _db.Collection(Coleccion)
                .WhereEqualTo("Correo", correoNormalizado)
                .GetSnapshotAsync();

            // Si no se encontraron coincidencias se retorna null de forma segura.
            if (snap.Documents.Count == 0)
            {
                return null;
            }

            // Se retorna el primer documento coincidente como entidad de usuario.
            return MapearUsuario(snap.Documents[0]);
        }

        // Obtiene los socios con rol 'Usuario' filtrando opcionalmente por término de búsqueda.
        // Discrimina administradores y entrenadores para optimizar el listado de recepción.
        public async Task<IReadOnlyList<Usuario>> GetSociosAsync(string? search = null)
        {
            // Recupera la instantánea de usuarios para evaluar roles y datos de contacto.
            var snap = await _db.Collection(Coleccion).GetSnapshotAsync();
            var socios = new List<Usuario>();

            // Filtrado primario para incluir únicamente miembros con rol de socio.
            foreach (var doc in snap.Documents)
            {
                var usuario = MapearUsuario(doc);
                if (usuario.Rol == "Usuario")
                {
                    socios.Add(usuario);
                }
            }

            // Si no se proporcionó término de búsqueda, retorna la lista completa de socios.
            if (string.IsNullOrWhiteSpace(search))
            {
                return socios;
            }

            // Búsqueda multi-campo normalizada por nombres, apellidos, DNI y correo electrónico.
            var termino = search.Trim().ToLower();
            return socios.Where(u =>
                u.Nombres.ToLower().Contains(termino) ||
                u.Apellidos.ToLower().Contains(termino) ||
                u.DNI.Contains(termino) ||
                u.Correo.ToLower().Contains(termino)).ToList();
        }

        // Inserta un nuevo usuario en la colección Firestore.
        // Asigna automáticamente un ID alfanumérico generado por Firestore.
        public async Task<string> AddAsync(Usuario entity)
        {
            // Generación de una nueva referencia de documento con ID único.
            var doc = _db.Collection(Coleccion).Document();
            entity.Id = doc.Id;

            // Construcción del diccionario estructurado para almacenamiento NoSQL.
            var datos = new Dictionary<string, object>
            {
                { "Nombres", entity.Nombres },
                { "Apellidos", entity.Apellidos },
                { "Correo", entity.Correo },
                { "DNI", entity.DNI },
                { "Celular", entity.Celular },
                { "PasswordHash", entity.PasswordHash },
                { "Rol", entity.Rol },
                { "FechaRegistro", DateTime.UtcNow },
                { "Activo", entity.Activo }
            };

            // Escritura atómica en Cloud Firestore.
            await doc.SetAsync(datos);
            return doc.Id;
        }

        // Actualiza los datos de perfil y administración de un usuario.
        // No sobrescribe la contraseña a menos que se provea un nuevo hash explícito.
        public async Task UpdateAsync(string id, Usuario entity)
        {
            // Mapeo de campos editables del perfil de usuario.
            var datos = new Dictionary<string, object>
            {
                { "Nombres", entity.Nombres },
                { "Apellidos", entity.Apellidos },
                { "Correo", entity.Correo },
                { "DNI", entity.DNI },
                { "Celular", entity.Celular },
                { "Rol", entity.Rol },
                { "Activo", entity.Activo }
            };

            // Si se suministró una contraseña renovada se incluye en el diccionario de actualización.
            if (!string.IsNullOrWhiteSpace(entity.PasswordHash))
            {
                datos["PasswordHash"] = entity.PasswordHash;
            }

            // Aplicación de actualización en el documento Firestore.
            await _db.Collection(Coleccion).Document(id).UpdateAsync(datos);
        }

        // Elimina permanentemente un documento de usuario por su identificador.
        // Utilizado exclusivamente en operaciones administrativas con supervisión.
        public async Task DeleteAsync(string id)
        {
            await _db.Collection(Coleccion).Document(id).DeleteAsync();
        }

        // Alterna el estado activo/inactivo del usuario en Firestore.
        // Permite suspender accesos sin destruir el registro ni su historial.
        public async Task ToggleStatusAsync(string id, bool nuevoEstado)
        {
            await _db.Collection(Coleccion).Document(id).UpdateAsync("Activo", nuevoEstado);
        }

        // Actualiza el campo de notas internas del entrenador sobre el socio.
        // Permite registrar observaciones privadas sobre progresos o lesiones.
        public async Task ActualizarNotasEntrenadorAsync(string id, string notas)
        {
            await _db.Collection(Coleccion).Document(id).UpdateAsync("NotasEntrenador", notas ?? "");
        }

        // Método auxiliar privado para transformar un DocumentSnapshot en la entidad Usuario.
        // Valida la presencia de cada campo para evitar excepciones por datos heterogéneos.
        private static Usuario MapearUsuario(DocumentSnapshot doc)
        {
            return new Usuario
            {
                Id = doc.Id,
                Nombres = doc.ContainsField("Nombres") ? doc.GetValue<string>("Nombres") : "",
                Apellidos = doc.ContainsField("Apellidos") ? doc.GetValue<string>("Apellidos") : "",
                Correo = doc.ContainsField("Correo") ? doc.GetValue<string>("Correo") : "",
                DNI = doc.ContainsField("DNI") ? doc.GetValue<string>("DNI") : "",
                Celular = doc.ContainsField("Celular") ? doc.GetValue<string>("Celular") : "",
                Rol = doc.ContainsField("Rol") ? doc.GetValue<string>("Rol") : "Usuario",
                PasswordHash = doc.ContainsField("PasswordHash") ? doc.GetValue<string>("PasswordHash") : "",
                NotasEntrenador = doc.ContainsField("NotasEntrenador") ? doc.GetValue<string>("NotasEntrenador") : "",
                Activo = !doc.ContainsField("Activo") || doc.GetValue<bool>("Activo"),
                FechaRegistro = doc.ContainsField("FechaRegistro") ? doc.GetValue<DateTime>("FechaRegistro") : DateTime.UtcNow
            };
        }
    }
}
