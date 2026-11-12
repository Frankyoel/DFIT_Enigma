// Dfit/Repositories/Firestore/MembresiaUsuarioRepository.cs
// Implementación de acceso a datos para las asignaciones de planes ('MembresiasUsuarios').
// Resuelve las consultas de suscripciones activas y permite vigencias sin incurrir en N+1 queries.
// Desacopla la lógica de suscripciones de la capa de presentación web.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Dfit.Models;
using Dfit.Repositories.Contracts;

namespace Dfit.Repositories.Firestore
{
    // Repositorio Firestore para asignaciones de membresía por socio.
    // Facilita la consulta de membresías vigentes y la desactivación en cascada.
    public class MembresiaUsuarioRepository : IMembresiaUsuarioRepository
    {
        // Cliente Firestore inyectado mediante inyección de dependencias.
        private readonly FirestoreDb _db;

        // Nombre canónico de la colección relacional de membresías por usuario.
        private const string Coleccion = "MembresiasUsuarios";

        // Constructor con inyección del cliente Firestore.
        public MembresiaUsuarioRepository(FirestoreDb db)
        {
            _db = db;
        }

        // Obtiene una asignación de membresía por su identificador único.
        // Retorna null si el id es inválido o no existe en Firestore.
        public async Task<MembresiaUsuario?> GetByIdAsync(string id)
        {
            // Cláusula de guarda para validar el identificador suministrado.
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            // Lectura directa del documento de asignación.
            var doc = await _db.Collection(Coleccion).Document(id).GetSnapshotAsync();

            // Validación de existencia del documento consultado.
            if (!doc.Exists)
            {
                return null;
            }

            // Deserializa el documento en la entidad MembresiaUsuario.
            return MapearMembresiaUsuario(doc);
        }

        // Obtiene la totalidad de asignaciones de membresía registradas.
        // Permite auditorías globales de suscripciones históricas.
        public async Task<IReadOnlyList<MembresiaUsuario>> GetAllAsync()
        {
            // Descarga de la colección completa para procesamiento analítico.
            var snap = await _db.Collection(Coleccion).GetSnapshotAsync();
            var lista = new List<MembresiaUsuario>();

            // Mapeo seguro de cada documento recuperado.
            foreach (var doc in snap.Documents)
            {
                lista.Add(MapearMembresiaUsuario(doc));
            }

            return lista;
        }

        // Obtiene todas las asignaciones activas en Firestore.
        // Consulta indexada que optimiza la carga inicial del panel de recepción.
        public async Task<IReadOnlyList<MembresiaUsuario>> GetActivasAsync()
        {
            // Filtrado a nivel base de datos por Activa = true.
            var snap = await _db.Collection(Coleccion)
                .WhereEqualTo("Activa", true)
                .GetSnapshotAsync();

            var lista = new List<MembresiaUsuario>();

            // Construcción de la lista de suscripciones vigentes.
            foreach (var doc in snap.Documents)
            {
                lista.Add(MapearMembresiaUsuario(doc));
            }

            return lista;
        }

        // Obtiene las membresías de un socio particular con opción de filtrar solo activas.
        // Soporta la visualización del portal del socio y la renovación de planes.
        public async Task<IReadOnlyList<MembresiaUsuario>> GetByUsuarioIdAsync(string usuarioId, bool soloActivas = true)
        {
            // Consulta base filtrando por el identificador del socio.
            var query = _db.Collection(Coleccion).WhereEqualTo("UsuarioId", usuarioId);

            // Si se solicita únicamente planes vigentes, se añade el predicado.
            if (soloActivas)
            {
                query = query.WhereEqualTo("Activa", true);
            }

            // Ejecución asíncrona de la consulta optimizada.
            var snap = await query.GetSnapshotAsync();
            var lista = new List<MembresiaUsuario>();

            // Mapeo de asignaciones recuperadas.
            foreach (var doc in snap.Documents)
            {
                lista.Add(MapearMembresiaUsuario(doc));
            }

            return lista;
        }

        // Inserta una nueva suscripción de socio en Firestore.
        // Establece fecha de inicio, fecha de fin y estado activo.
        public async Task<string> AddAsync(MembresiaUsuario entity)
        {
            // Generación de un nuevo documento con ID único en Firestore.
            var doc = _db.Collection(Coleccion).Document();
            entity.Id = doc.Id;

            // Datos de vigencia y relaciones con el socio y el plan.
            var datos = new Dictionary<string, object>
            {
                { "UsuarioId", entity.UsuarioId },
                { "MembresiaId", entity.MembresiaId },
                { "FechaInicio", entity.FechaInicio },
                { "FechaFin", entity.FechaFin },
                { "Activa", entity.Activa }
            };

            // Grabación persistente de la asignación.
            await doc.SetAsync(datos);
            return doc.Id;
        }

        // Actualiza el estado o vigencia de una asignación.
        // Utilizado al cancelar una suscripción o extender su fecha de corte.
        public async Task UpdateAsync(string id, MembresiaUsuario entity)
        {
            // Diccionario con los atributos sujetos a modificación.
            var datos = new Dictionary<string, object>
            {
                { "Activa", entity.Activa },
                { "FechaFin", entity.FechaFin }
            };

            // Aplicación de la actualización en Firestore.
            await _db.Collection(Coleccion).Document(id).UpdateAsync(datos);
        }

        // Elimina el registro de suscripción por su identificador.
        // Operación administrativa de depuración física.
        public async Task DeleteAsync(string id)
        {
            await _db.Collection(Coleccion).Document(id).DeleteAsync();
        }

        // Desactiva todas las suscripciones activas anteriores de un socio para evitar duplicidades.
        // Garantiza que un usuario solo mantenga una membresía activa concurrente.
        public async Task DesactivarMembresiasPreviasAsync(string usuarioId)
        {
            // Búsqueda de todas las suscripciones activas registradas para el socio.
            var snap = await _db.Collection(Coleccion)
                .WhereEqualTo("UsuarioId", usuarioId)
                .WhereEqualTo("Activa", true)
                .GetSnapshotAsync();

            // Marcado en lote como inactivas para dar paso al nuevo plan.
            foreach (var doc in snap.Documents)
            {
                await _db.Collection(Coleccion).Document(doc.Id).UpdateAsync("Activa", false);
            }
        }

        // Transforma un DocumentSnapshot en la entidad MembresiaUsuario.
        // Provee fechas por defecto en caso de inconsistencia en documentos legados.
        private static MembresiaUsuario MapearMembresiaUsuario(DocumentSnapshot doc)
        {
            return new MembresiaUsuario
            {
                Id = doc.Id,
                UsuarioId = doc.ContainsField("UsuarioId") ? doc.GetValue<string>("UsuarioId") : "",
                MembresiaId = doc.ContainsField("MembresiaId") ? doc.GetValue<string>("MembresiaId") : "",
                FechaInicio = doc.ContainsField("FechaInicio") ? doc.GetValue<DateTime>("FechaInicio") : DateTime.UtcNow,
                FechaFin = doc.ContainsField("FechaFin") ? doc.GetValue<DateTime>("FechaFin") : DateTime.UtcNow,
                Activa = !doc.ContainsField("Activa") || doc.GetValue<bool>("Activa")
            };
        }
    }
}
