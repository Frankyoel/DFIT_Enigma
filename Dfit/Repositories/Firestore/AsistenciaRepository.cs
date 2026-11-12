// Dfit/Repositories/Firestore/AsistenciaRepository.cs
// Implementación de acceso a datos para ingresos y asistencias físicas en 'Asistencias'.
// Facilita la verificación de aforos y check-in por clase grupal.
// Desacopla la gestión de asistencia física y acceso QR de la capa web.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Dfit.Models;
using Dfit.Repositories.Contracts;

namespace Dfit.Repositories.Firestore
{
    // Repositorio Firestore para control de ingresos y asistencias.
    // Proporciona consultas de aforo diario y confirmaciones de check-in en tiempo real.
    public class AsistenciaRepository : IAsistenciaRepository
    {
        // Instancia del cliente Cloud Firestore inyectada por DI.
        private readonly FirestoreDb _db;

        // Nombre canónico de la colección de asistencias en Cloud Firestore.
        private const string Coleccion = "Asistencias";

        // Constructor con inyección del cliente Firestore.
        public AsistenciaRepository(FirestoreDb db)
        {
            _db = db;
        }

        // Obtiene un registro de asistencia por su identificador.
        // Retorna null si el id no es válido o no existe en Firestore.
        public async Task<Asistencia?> GetByIdAsync(string id)
        {
            // Cláusula de guarda para validar el identificador de asistencia.
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            // Lectura puntual del documento en Firestore.
            var doc = await _db.Collection(Coleccion).Document(id).GetSnapshotAsync();

            // Verificación de existencia del registro.
            if (!doc.Exists)
            {
                return null;
            }

            // Conversión del snapshot a la entidad Asistencia.
            return MapearAsistencia(doc);
        }

        // Obtiene todas las asistencias registradas en el historial.
        // Utilizado para análisis históricos de aforo y frecuencia de visitas.
        public async Task<IReadOnlyList<Asistencia>> GetAllAsync()
        {
            // Descarga de la colección completa de asistencias.
            var snap = await _db.Collection(Coleccion).GetSnapshotAsync();
            var lista = new List<Asistencia>();

            // Iteración para mapear cada registro de entrada.
            foreach (var doc in snap.Documents)
            {
                lista.Add(MapearAsistencia(doc));
            }

            return lista;
        }

        // Obtiene los ingresos correspondientes a un día específico.
        // Permite monitorear el flujo de socios en la recepción del gimnasio.
        public async Task<IReadOnlyList<Asistencia>> GetByFechaAsync(DateTime fecha)
        {
            // Descarga asíncrona de los registros de asistencia.
            var snap = await _db.Collection(Coleccion).GetSnapshotAsync();
            var lista = new List<Asistencia>();
            var fechaObjetivo = fecha.Date;

            // Filtrado en memoria por la fecha objetivo del día.
            foreach (var doc in snap.Documents)
            {
                var asistencia = MapearAsistencia(doc);
                if (asistencia.FechaHoraEntrada.Date == fechaObjetivo)
                {
                    lista.Add(asistencia);
                }
            }

            // Retorno ordenado por hora de entrada descendente (más recientes primero).
            return lista.OrderByDescending(a => a.FechaHoraEntrada).ToList();
        }

        // Obtiene los IDs de los socios con asistencia confirmada hoy para un horario específico.
        // Utilizado en el roster de clases grupales para controlar asistencia por cupo.
        public async Task<HashSet<string>> GetAsistenciasHoyPorHorarioAsync(string horarioId, DateTime fechaHoy)
        {
            // Consulta indexada filtrando por el identificador de la clase/horario.
            var snap = await _db.Collection(Coleccion)
                .WhereEqualTo("HorarioId", horarioId)
                .GetSnapshotAsync();

            var asistenciasHoy = new HashSet<string>();
            var fechaObjetivo = fechaHoy.Date;

            // Identificación de los socios que ingresaron en la fecha evaluada.
            foreach (var doc in snap.Documents)
            {
                var f = doc.ContainsField("FechaHoraEntrada") ? doc.GetValue<DateTime>("FechaHoraEntrada") : DateTime.MinValue;
                if (f.Date == fechaObjetivo)
                {
                    asistenciasHoy.Add(doc.GetValue<string>("UsuarioId"));
                }
            }

            return asistenciasHoy;
        }

        // Inserta un nuevo registro de acceso en Firestore.
        // Almacena opcionalmente la clase grupal a la que asistió el socio.
        public async Task<string> AddAsync(Asistencia entity)
        {
            // Generación de un nuevo ID de documento en Firestore.
            var doc = _db.Collection(Coleccion).Document();
            entity.Id = doc.Id;

            // Datos base del registro de acceso.
            var datos = new Dictionary<string, object>
            {
                { "UsuarioId", entity.UsuarioId },
                { "FechaHoraEntrada", entity.FechaHoraEntrada == default ? DateTime.UtcNow : entity.FechaHoraEntrada }
            };

            // Asociación opcional a clase grupal en caso de pase de lista.
            if (!string.IsNullOrWhiteSpace(entity.HorarioId))
            {
                datos["HorarioId"] = entity.HorarioId;
            }

            if (!string.IsNullOrWhiteSpace(entity.NombreClase))
            {
                datos["NombreClase"] = entity.NombreClase;
            }

            // Inserción persistente en Cloud Firestore.
            await doc.SetAsync(datos);
            return doc.Id;
        }

        // Actualiza un registro de asistencia.
        // Utilizado en correcciones de hora de entrada.
        public async Task UpdateAsync(string id, Asistencia entity)
        {
            var datos = new Dictionary<string, object>
            {
                { "FechaHoraEntrada", entity.FechaHoraEntrada }
            };

            await _db.Collection(Coleccion).Document(id).UpdateAsync(datos);
        }

        // Elimina un registro de asistencia por su ID.
        public async Task DeleteAsync(string id)
        {
            await _db.Collection(Coleccion).Document(id).DeleteAsync();
        }

        // Transforma un DocumentSnapshot en la entidad Asistencia.
        // Controla campos opcionales como HorarioId y NombreClase.
        private static Asistencia MapearAsistencia(DocumentSnapshot doc)
        {
            return new Asistencia
            {
                Id = doc.Id,
                UsuarioId = doc.ContainsField("UsuarioId") ? doc.GetValue<string>("UsuarioId") : "",
                HorarioId = doc.ContainsField("HorarioId") ? doc.GetValue<string>("HorarioId") : null,
                NombreClase = doc.ContainsField("NombreClase") ? doc.GetValue<string>("NombreClase") : null,
                FechaHoraEntrada = doc.ContainsField("FechaHoraEntrada") ? doc.GetValue<DateTime>("FechaHoraEntrada") : DateTime.UtcNow
            };
        }
    }
}
