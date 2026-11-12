// Dfit/Repositories/Firestore/EvaluacionFisicaRepository.cs
// Implementación de acceso a datos para valoraciones físicas ('EvaluacionesFisicas').
// Facilita la recuperación del historial antropométrico por socio.
// Cumple con la directriz de arquitectura en capas desacoplando el seguimiento antropométrico.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Dfit.Models;
using Dfit.Repositories.Contracts;

namespace Dfit.Repositories.Firestore
{
    // Repositorio Firestore para métricas corporales y antropométricas.
    // Gestiona registros de peso, estatura, IMC, porcentaje graso y masa muscular.
    public class EvaluacionFisicaRepository : IEvaluacionFisicaRepository
    {
        // Instancia del cliente Cloud Firestore inyectada por DI.
        private readonly FirestoreDb _db;

        // Nombre de la colección de evaluaciones físicas en Firestore.
        private const string Coleccion = "EvaluacionesFisicas";

        // Constructor con inyección del cliente Firestore.
        public EvaluacionFisicaRepository(FirestoreDb db)
        {
            _db = db;
        }

        // Obtiene una evaluación física por su identificador.
        // Retorna null si el ID no es válido o no existe en Firestore.
        public async Task<EvaluacionFisica?> GetByIdAsync(string id)
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

            // Conversión del snapshot a la entidad EvaluacionFisica.
            return MapearEvaluacion(doc);
        }

        // Obtiene todas las evaluaciones registradas.
        // Utilizado en auditorías clínicas y seguimiento general del gimnasio.
        public async Task<IReadOnlyList<EvaluacionFisica>> GetAllAsync()
        {
            // Ejecución asíncrona sobre la colección completa de evaluaciones.
            var snap = await _db.Collection(Coleccion).GetSnapshotAsync();
            var lista = new List<EvaluacionFisica>();

            // Iteración para mapeo de cada registro antropométrico.
            foreach (var doc in snap.Documents)
            {
                lista.Add(MapearEvaluacion(doc));
            }

            return lista;
        }

        // Obtiene las evaluaciones de un socio ordenadas descendentemente por fecha.
        // Permite graficar o listar el progreso físico del socio en su perfil.
        public async Task<IReadOnlyList<EvaluacionFisica>> GetByUsuarioIdAsync(string usuarioId)
        {
            // Consulta indexada filtrando por el identificador del socio.
            var snap = await _db.Collection(Coleccion)
                .WhereEqualTo("UsuarioId", usuarioId)
                .GetSnapshotAsync();

            var lista = new List<EvaluacionFisica>();

            // Mapeo de evaluaciones del socio.
            foreach (var doc in snap.Documents)
            {
                lista.Add(MapearEvaluacion(doc));
            }

            // Ordenamiento por fecha de medición más reciente.
            return lista.OrderByDescending(e => e.Fecha).ToList();
        }

        // Registra una nueva evaluación física en Firestore.
        // Almacena las mediciones antropométricas y notas técnicas del entrenador.
        public async Task<string> AddAsync(EvaluacionFisica entity)
        {
            // Creación de una referencia de documento vacía.
            var doc = _db.Collection(Coleccion).Document();
            entity.Id = doc.Id;

            // Construcción del diccionario de campos antropométricos.
            var datos = new Dictionary<string, object>
            {
                { "UsuarioId", entity.UsuarioId },
                { "EntrenadorId", entity.EntrenadorId ?? "" },
                { "EntrenadorNombre", entity.EntrenadorNombre ?? "Entrenador" },
                { "Fecha", entity.Fecha == default ? DateTime.UtcNow : entity.Fecha },
                { "Peso", entity.Peso },
                { "Altura", entity.Altura },
                { "IMC", entity.IMC },
                { "PorcentajeGrasa", entity.PorcentajeGrasa },
                { "MasaMuscular", entity.MasaMuscular },
                { "Notas", entity.Notas ?? "" }
            };

            // Grabación persistente en Firestore.
            await doc.SetAsync(datos);
            return doc.Id;
        }

        // Actualiza una evaluación física existente.
        // Utilizado en correcciones de mediciones antropométricas.
        public async Task UpdateAsync(string id, EvaluacionFisica entity)
        {
            // Diccionario con los parámetros corregidos.
            var datos = new Dictionary<string, object>
            {
                { "Peso", entity.Peso },
                { "Altura", entity.Altura },
                { "IMC", entity.IMC },
                { "PorcentajeGrasa", entity.PorcentajeGrasa },
                { "MasaMuscular", entity.MasaMuscular },
                { "Notas", entity.Notas ?? "" }
            };

            // Grabación de cambios en Firestore.
            await _db.Collection(Coleccion).Document(id).UpdateAsync(datos);
        }

        // Elimina un registro de evaluación física.
        public async Task DeleteAsync(string id)
        {
            await _db.Collection(Coleccion).Document(id).DeleteAsync();
        }

        // Transforma un DocumentSnapshot en la entidad EvaluacionFisica.
        // Realiza conversiones numéricas tolerantes a tipos heterogéneos.
        private static EvaluacionFisica MapearEvaluacion(DocumentSnapshot doc)
        {
            return new EvaluacionFisica
            {
                Id = doc.Id,
                UsuarioId = doc.ContainsField("UsuarioId") ? doc.GetValue<string>("UsuarioId") : "",
                EntrenadorId = doc.ContainsField("EntrenadorId") ? doc.GetValue<string>("EntrenadorId") : "",
                EntrenadorNombre = doc.ContainsField("EntrenadorNombre") ? doc.GetValue<string>("EntrenadorNombre") : "Entrenador",
                Fecha = doc.ContainsField("Fecha") ? doc.GetValue<DateTime>("Fecha") : DateTime.UtcNow,
                Peso = doc.ContainsField("Peso") ? Convert.ToDouble(doc.GetValue<object>("Peso")) : 0,
                Altura = doc.ContainsField("Altura") ? Convert.ToDouble(doc.GetValue<object>("Altura")) : 0,
                IMC = doc.ContainsField("IMC") ? Convert.ToDouble(doc.GetValue<object>("IMC")) : 0,
                PorcentajeGrasa = doc.ContainsField("PorcentajeGrasa") ? Convert.ToDouble(doc.GetValue<object>("PorcentajeGrasa")) : 0,
                MasaMuscular = doc.ContainsField("MasaMuscular") ? Convert.ToDouble(doc.GetValue<object>("MasaMuscular")) : 0,
                Notas = doc.ContainsField("Notas") ? doc.GetValue<string>("Notas") : ""
            };
        }
    }
}
