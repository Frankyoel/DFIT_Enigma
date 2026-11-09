// Dfit/Repositories/Firestore/RutinaRepository.cs
// Implementación de acceso a datos para rutinas deportivas ('Rutinas') en Firestore.
// Gestiona el catálogo de ejercicios, niveles de dificultad y enlaces multimedia.
// Cumple con el principio de responsabilidad única para el dominio de entrenamiento.

using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Dfit.Models;
using Dfit.Repositories.Contracts;

namespace Dfit.Repositories.Firestore
{
    // Repositorio Firestore para planes y catálogo de rutinas.
    // Proporciona operaciones para catálogo de entrenamientos y baja lógica.
    public class RutinaRepository : IRutinaRepository
    {
        // Instancia del cliente Cloud Firestore inyectada por DI.
        private readonly FirestoreDb _db;

        // Nombre de la colección de rutinas deportivas en Firestore.
        private const string Coleccion = "Rutinas";

        // Constructor con inyección del cliente Firestore.
        public RutinaRepository(FirestoreDb db)
        {
            _db = db;
        }

        // Obtiene una rutina por su identificador de documento.
        // Retorna null si el ID no es válido o la rutina no existe.
        public async Task<Rutina?> GetByIdAsync(string id)
        {
            // Cláusula de guarda para validar el identificador suministrado.
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            // Lectura puntual del documento en Firestore.
            var doc = await _db.Collection(Coleccion).Document(id).GetSnapshotAsync();

            // Verificación de existencia del documento.
            if (!doc.Exists)
            {
                return null;
            }

            // Mapeo seguro del documento a entidad de dominio.
            return MapearRutina(doc);
        }

        // Obtiene todas las rutinas registradas en el catálogo.
        // Utilizado en la consola de administración de entrenadores.
        public async Task<IReadOnlyList<Rutina>> GetAllAsync()
        {
            // Ejecución asíncrona sobre la colección completa de rutinas.
            var snap = await _db.Collection(Coleccion).GetSnapshotAsync();
            var lista = new List<Rutina>();

            // Iteración para transformación de documentos.
            foreach (var doc in snap.Documents)
            {
                lista.Add(MapearRutina(doc));
            }

            return lista;
        }

        // Obtiene únicamente las rutinas marcadas como activas para socios.
        // Optimiza la carga del catálogo visual en el portal web.
        public async Task<IReadOnlyList<Rutina>> GetActivasAsync()
        {
            // Consulta indexada filtrando por Activa = true.
            var snap = await _db.Collection(Coleccion)
                .WhereEqualTo("Activa", true)
                .GetSnapshotAsync();

            var lista = new List<Rutina>();

            // Mapeo de rutinas activas.
            foreach (var doc in snap.Documents)
            {
                lista.Add(MapearRutina(doc));
            }

            return lista;
        }

        // Inserta una nueva rutina en la colección Firestore.
        // Asigna el nivel de dificultad y URL multimedia de demostración.
        public async Task<string> AddAsync(Rutina entity)
        {
            // Creación de una referencia de documento vacía.
            var doc = _db.Collection(Coleccion).Document();
            entity.Id = doc.Id;

            // Mapeo estructurado para almacenamiento NoSQL.
            var datos = new Dictionary<string, object>
            {
                { "Nombre", entity.Nombre },
                { "Descripcion", entity.Descripcion ?? "" },
                { "Nivel", entity.Nivel ?? "Principiante" },
                { "VideoUrl", entity.VideoUrl ?? "" },
                { "Activa", entity.Activa }
            };

            // Grabación persistente en Firestore.
            await doc.SetAsync(datos);
            return doc.Id;
        }

        // Actualiza los datos descriptivos y multimedia de una rutina.
        // Permite a los entrenadores enriquecer guías de ejercicios existentes.
        public async Task UpdateAsync(string id, Rutina entity)
        {
            // Diccionario con los datos actualizados de la rutina.
            var datos = new Dictionary<string, object>
            {
                { "Nombre", entity.Nombre },
                { "Descripcion", entity.Descripcion ?? "" },
                { "Nivel", entity.Nivel ?? "Principiante" },
                { "VideoUrl", entity.VideoUrl ?? "" },
                { "Activa", entity.Activa }
            };

            // Grabación de cambios en Firestore.
            await _db.Collection(Coleccion).Document(id).UpdateAsync(datos);
        }

        // Elimina el documento de rutina físicamente.
        // Operación de limpieza administrativa.
        public async Task DeleteAsync(string id)
        {
            await _db.Collection(Coleccion).Document(id).DeleteAsync();
        }

        // Desactiva lógicamente la rutina sin eliminar sus asignaciones históricas.
        // Evita inconsistencias referenciales en rutinas asignadas a socios.
        public async Task DesactivarAsync(string id)
        {
            await _db.Collection(Coleccion).Document(id).UpdateAsync("Activa", false);
        }

        // Transforma un DocumentSnapshot en la entidad Rutina.
        // Aplica valores predeterminados seguros para nivel y video.
        private static Rutina MapearRutina(DocumentSnapshot doc)
        {
            return new Rutina
            {
                Id = doc.Id,
                Nombre = doc.ContainsField("Nombre") ? doc.GetValue<string>("Nombre") : "",
                Descripcion = doc.ContainsField("Descripcion") ? doc.GetValue<string>("Descripcion") : "",
                Nivel = doc.ContainsField("Nivel") ? doc.GetValue<string>("Nivel") : "Principiante",
                VideoUrl = doc.ContainsField("VideoUrl") ? doc.GetValue<string>("VideoUrl") : "",
                Activa = !doc.ContainsField("Activa") || doc.GetValue<bool>("Activa")
            };
        }
    }
}
