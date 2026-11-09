// Dfit/Repositories/Firestore/HorarioRepository.cs
// Implementación de acceso a datos para horarios semanales de clases ('Horarios').
// Resuelve la ordenación cronológica de sesiones de entrenamiento dirigidas.
// Aísla la programación de clases en el gimnasio de la lógica de controladores.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Dfit.Models;
using Dfit.Repositories.Contracts;

namespace Dfit.Repositories.Firestore
{
    // Repositorio Firestore para programación de clases colectivas.
    // Encapsula la gestión de cupos máximos, instructores y horarios semanales.
    public class HorarioRepository : IHorarioRepository
    {
        // Instancia del cliente Cloud Firestore inyectada por DI.
        private readonly FirestoreDb _db;

        // Nombre canónico de la colección de horarios en Cloud Firestore.
        private const string Coleccion = "Horarios";

        // Constructor con inyección del cliente Firestore.
        public HorarioRepository(FirestoreDb db)
        {
            _db = db;
        }

        // Obtiene una sesión de horario por su identificador.
        // Retorna null si el ID no es válido o no existe en Firestore.
        public async Task<Horario?> GetByIdAsync(string id)
        {
            // Cláusula de guarda para validar el identificador de clase.
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

            // Conversión del snapshot a la entidad Horario.
            return MapearHorario(doc);
        }

        // Obtiene todos los horarios registrados en Firestore.
        // Utilizado en la administración de clases y pase de lista.
        public async Task<IReadOnlyList<Horario>> GetAllAsync()
        {
            // Descarga de la colección completa de horarios.
            var snap = await _db.Collection(Coleccion).GetSnapshotAsync();
            var lista = new List<Horario>();

            // Iteración para mapeo de cada clase programada.
            foreach (var doc in snap.Documents)
            {
                lista.Add(MapearHorario(doc));
            }

            return lista;
        }

        // Obtiene todas las clases ordenadas cronológicamente por hora de inicio.
        // Facilita la visualización limpia del cronograma semanal en la web.
        public async Task<IReadOnlyList<Horario>> GetHorariosOrdenadosAsync()
        {
            // Recupera la totalidad de horarios registrados.
            var horarios = await GetAllAsync();

            // Ordena ascendentemente por hora de inicio de la sesión.
            return horarios.OrderBy(h => h.HoraInicio).ToList();
        }

        // Inserta un nuevo horario de clase en Firestore.
        // Almacena horas como cadenas formateadas para compatibilidad NoSQL.
        public async Task<string> AddAsync(Horario entity)
        {
            // Generación de un nuevo documento con ID autogenerado.
            var doc = _db.Collection(Coleccion).Document();
            entity.Id = doc.Id;

            // Datos estructurados de la clase colectiva.
            var datos = new Dictionary<string, object>
            {
                { "DiaSemana", entity.DiaSemana },
                { "HoraInicio", entity.HoraInicio.ToString() },
                { "HoraFin", entity.HoraFin.ToString() },
                { "NombreClase", entity.NombreClase },
                { "Instructor", entity.Instructor ?? "" },
                { "CupoMaximo", entity.CupoMaximo }
            };

            // Grabación persistente en Firestore.
            await doc.SetAsync(datos);
            return doc.Id;
        }

        // Actualiza los parámetros de una clase existente.
        // Modifica días, horarios, instructores o aforos máximos.
        public async Task UpdateAsync(string id, Horario entity)
        {
            // Diccionario con los parámetros actualizados de la clase.
            var datos = new Dictionary<string, object>
            {
                { "DiaSemana", entity.DiaSemana },
                { "HoraInicio", entity.HoraInicio.ToString() },
                { "HoraFin", entity.HoraFin.ToString() },
                { "NombreClase", entity.NombreClase },
                { "Instructor", entity.Instructor ?? "" },
                { "CupoMaximo", entity.CupoMaximo }
            };

            // Grabación de la modificación en Firestore.
            await _db.Collection(Coleccion).Document(id).UpdateAsync(datos);
        }

        // Elimina el horario de clase indicado.
        // Operación administrativa con impacto en el cronograma.
        public async Task DeleteAsync(string id)
        {
            await _db.Collection(Coleccion).Document(id).DeleteAsync();
        }

        // Transforma un DocumentSnapshot en la entidad Horario.
        // Realiza el parseo seguro de TimeSpan a partir de cadenas de texto.
        private static Horario MapearHorario(DocumentSnapshot doc)
        {
            return new Horario
            {
                Id = doc.Id,
                DiaSemana = doc.ContainsField("DiaSemana") ? doc.GetValue<string>("DiaSemana") : "",
                HoraInicio = doc.ContainsField("HoraInicio") ? TimeSpan.Parse(doc.GetValue<string>("HoraInicio")) : TimeSpan.Zero,
                HoraFin = doc.ContainsField("HoraFin") ? TimeSpan.Parse(doc.GetValue<string>("HoraFin")) : TimeSpan.Zero,
                NombreClase = doc.ContainsField("NombreClase") ? doc.GetValue<string>("NombreClase") : "",
                Instructor = doc.ContainsField("Instructor") ? doc.GetValue<string>("Instructor") : "",
                CupoMaximo = doc.ContainsField("CupoMaximo") ? doc.GetValue<int>("CupoMaximo") : 20
            };
        }
    }
}
