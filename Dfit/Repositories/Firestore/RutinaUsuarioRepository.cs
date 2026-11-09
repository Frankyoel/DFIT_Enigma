// Dfit/Repositories/Firestore/RutinaUsuarioRepository.cs
// Implementación de acceso a datos para asignaciones de rutinas a socios ('RutinasUsuarios').
// Controla el estado activo de asignaciones y la desactivación de rutinas previas.
// Aplica cláusulas de guarda para garantizar profundidad condicional <= 2 niveles.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Dfit.Models;
using Dfit.Repositories.Contracts;

namespace Dfit.Repositories.Firestore
{
    // Repositorio Firestore para asignación de rutinas a socios.
    // Encapsula las transiciones de estado de rutinas activas, canceladas o reemplazadas.
    public class RutinaUsuarioRepository : IRutinaUsuarioRepository
    {
        // Instancia del cliente Cloud Firestore inyectada por DI.
        private readonly FirestoreDb _db;

        // Nombre canónico de la colección de rutinas asignadas en Firestore.
        private const string Coleccion = "RutinasUsuarios";

        // Constructor con inyección del cliente Firestore.
        public RutinaUsuarioRepository(FirestoreDb db)
        {
            _db = db;
        }

        // Obtiene una asignación de rutina por su identificador.
        // Retorna null si el ID suministrado es inválido o no existe.
        public async Task<RutinaUsuario?> GetByIdAsync(string id)
        {
            // Cláusula de guarda para validar el identificador de asignación.
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            // Lectura directa del documento de asignación.
            var doc = await _db.Collection(Coleccion).Document(id).GetSnapshotAsync();

            // Verificación de existencia del documento consultado.
            if (!doc.Exists)
            {
                return null;
            }

            // Mapeo seguro a la entidad RutinaUsuario.
            return MapearRutinaUsuario(doc);
        }

        // Obtiene todas las asignaciones de rutina registradas.
        // Utilizado en reportes analíticos de entrenamiento deportivo.
        public async Task<IReadOnlyList<RutinaUsuario>> GetAllAsync()
        {
            // Descarga de la colección completa de asignaciones.
            var snap = await _db.Collection(Coleccion).GetSnapshotAsync();
            var lista = new List<RutinaUsuario>();

            // Iteración para mapeo de cada registro de asignación.
            foreach (var doc in snap.Documents)
            {
                lista.Add(MapearRutinaUsuario(doc));
            }

            return lista;
        }

        // Obtiene la rutina asignada actualmente activa para un usuario específico.
        // Retorna null si el socio no cuenta con entrenamiento vigente.
        public async Task<RutinaUsuario?> GetRutinaActivaPorUsuarioAsync(string usuarioId)
        {
            // Consulta indexada filtrando por socio y estado Activa.
            var snap = await _db.Collection(Coleccion)
                .WhereEqualTo("UsuarioId", usuarioId)
                .WhereEqualTo("Estado", "Activa")
                .GetSnapshotAsync();

            // Cláusula de guarda si no existen asignaciones activas.
            if (snap.Documents.Count == 0)
            {
                return null;
            }

            // Mapea y retorna la asignación activa actual.
            return MapearRutinaUsuario(snap.Documents[0]);
        }

        // Desactiva o reemplaza las asignaciones activas anteriores de un socio.
        // Evita que un socio mantenga múltiples rutinas activas concurrentes.
        public async Task DesactivarRutinasPreviasAsync(string usuarioId)
        {
            // Búsqueda de todas las rutinas marcadas como Activa para el socio.
            var snap = await _db.Collection(Coleccion)
                .WhereEqualTo("UsuarioId", usuarioId)
                .WhereEqualTo("Estado", "Activa")
                .GetSnapshotAsync();

            // Actualización en lote al estado Reemplazada.
            foreach (var doc in snap.Documents)
            {
                await _db.Collection(Coleccion).Document(doc.Id).UpdateAsync("Estado", "Reemplazada");
            }
        }

        // Inserta una nueva asignación de rutina en Firestore.
        // Establece fecha de asignación y fecha de vencimiento calculada.
        public async Task<string> AddAsync(RutinaUsuario entity)
        {
            // Creación de una referencia vacía con ID autogenerado.
            var doc = _db.Collection(Coleccion).Document();
            entity.Id = doc.Id;

            // Mapeo estructurado para almacenamiento en Firestore.
            var datos = new Dictionary<string, object>
            {
                { "UsuarioId", entity.UsuarioId },
                { "RutinaId", entity.RutinaId },
                { "FechaAsignacion", entity.FechaAsignacion == default ? DateTime.UtcNow : entity.FechaAsignacion },
                { "Estado", string.IsNullOrWhiteSpace(entity.Estado) ? "Activa" : entity.Estado }
            };

            // Inclusión de la fecha límite en caso de haber sido definida.
            if (entity.FechaFin.HasValue)
            {
                datos["FechaFin"] = entity.FechaFin.Value;
            }

            // Grabación persistente en Firestore.
            await doc.SetAsync(datos);
            return doc.Id;
        }

        // Actualiza el estado de una asignación existente.
        // Utilizado al cancelar o completar la rutina.
        public async Task UpdateAsync(string id, RutinaUsuario entity)
        {
            // Diccionario con los datos a actualizar en la asignación.
            var datos = new Dictionary<string, object>
            {
                { "Estado", entity.Estado }
            };

            // Grabación de cambios en Firestore.
            await _db.Collection(Coleccion).Document(id).UpdateAsync(datos);
        }

        // Elimina una asignación de rutina físicamente.
        public async Task DeleteAsync(string id)
        {
            await _db.Collection(Coleccion).Document(id).DeleteAsync();
        }

        // Transforma un DocumentSnapshot en la entidad RutinaUsuario.
        // Controla la deserialización opcional del campo FechaFin.
        private static RutinaUsuario MapearRutinaUsuario(DocumentSnapshot doc)
        {
            return new RutinaUsuario
            {
                Id = doc.Id,
                UsuarioId = doc.ContainsField("UsuarioId") ? doc.GetValue<string>("UsuarioId") : "",
                RutinaId = doc.ContainsField("RutinaId") ? doc.GetValue<string>("RutinaId") : "",
                FechaAsignacion = doc.ContainsField("FechaAsignacion") ? doc.GetValue<DateTime>("FechaAsignacion") : DateTime.UtcNow,
                FechaFin = doc.ContainsField("FechaFin") ? doc.GetValue<DateTime?>("FechaFin") : null,
                Estado = doc.ContainsField("Estado") ? doc.GetValue<string>("Estado") : "Activa"
            };
        }
    }
}
