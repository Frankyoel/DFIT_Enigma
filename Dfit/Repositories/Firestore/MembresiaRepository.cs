// Dfit/Repositories/Firestore/MembresiaRepository.cs
// Implementación de acceso a datos para la colección 'Membresias' en Cloud Firestore.
// Encapsula las operaciones del catálogo de planes del gimnasio.
// Cumple con la directriz de arquitectura hexagonal aislando la persistencia NoSQL.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Dfit.Models;
using Dfit.Repositories.Contracts;

namespace Dfit.Repositories.Firestore
{
    // Repositorio Firestore para planes y membresías disponibles.
    // Proporciona consultas filtradas para planes activos y operaciones CRUD.
    public class MembresiaRepository : IMembresiaRepository
    {
        // Instancia del cliente Cloud Firestore inyectada por DI.
        private readonly FirestoreDb _db;

        // Nombre canónico de la colección de membresías en Firestore.
        private const string Coleccion = "Membresias";

        // Constructor con inyección del cliente Firestore.
        public MembresiaRepository(FirestoreDb db)
        {
            _db = db;
        }

        // Obtiene un plan de membresía por su ID de documento.
        // Retorna null si el identificador es nulo, vacío o si el documento no existe.
        public async Task<Membresia?> GetByIdAsync(string id)
        {
            // Cláusula de guarda para validar el parámetro de entrada.
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            // Lectura directa del documento de membresía en Firestore.
            var doc = await _db.Collection(Coleccion).Document(id).GetSnapshotAsync();

            // Validación de existencia en el almacén de datos.
            if (!doc.Exists)
            {
                return null;
            }

            // Deserializa el documento a la entidad Membresia.
            return MapearMembresia(doc);
        }

        // Obtiene la totalidad de planes registrados en la base de datos.
        // Utilizado en la administración de planes para visualización integral.
        public async Task<IReadOnlyList<Membresia>> GetAllAsync()
        {
            // Consulta asíncrona sobre la colección completa de membresías.
            var snap = await _db.Collection(Coleccion).GetSnapshotAsync();
            var lista = new List<Membresia>();

            // Iteración y transformación a entidades fuertemente tipadas.
            foreach (var doc in snap.Documents)
            {
                lista.Add(MapearMembresia(doc));
            }

            return lista;
        }

        // Obtiene únicamente los planes marcados como activos para ventas y suscripciones.
        // Evita ofrecer planes dados de baja en el mostrador de recepción.
        public async Task<IReadOnlyList<Membresia>> GetActivasAsync()
        {
            // Consulta indexada filtrando exclusivamente membresías con Activa = true.
            var snap = await _db.Collection(Coleccion)
                .WhereEqualTo("Activa", true)
                .GetSnapshotAsync();

            var lista = new List<Membresia>();

            // Mapeo seguro de cada documento recuperado.
            foreach (var doc in snap.Documents)
            {
                lista.Add(MapearMembresia(doc));
            }

            return lista;
        }

        // Inserta un nuevo plan de membresía en Firestore.
        // Genera un ID automático asignado por Firestore.
        public async Task<string> AddAsync(Membresia entity)
        {
            // Creación de una referencia de documento vacía para generar el ID.
            var doc = _db.Collection(Coleccion).Document();
            entity.Id = doc.Id;

            // Preparación del diccionario estructurado para almacenamiento.
            var datos = new Dictionary<string, object>
            {
                { "Nombre", entity.Nombre },
                { "Descripcion", entity.Descripcion ?? "" },
                { "Precio", (double)entity.Precio },
                { "DuracionDias", entity.DuracionDias },
                { "Activa", entity.Activa }
            };

            // Grabación persistente en Firestore.
            await doc.SetAsync(datos);
            return doc.Id;
        }

        // Actualiza las propiedades de un plan existente.
        // Sobrescribe descripción, precio, duración y estado activo.
        public async Task UpdateAsync(string id, Membresia entity)
        {
            // Diccionario con los campos actualizados de la membresía.
            var datos = new Dictionary<string, object>
            {
                { "Nombre", entity.Nombre },
                { "Descripcion", entity.Descripcion ?? "" },
                { "Precio", (double)entity.Precio },
                { "DuracionDias", entity.DuracionDias },
                { "Activa", entity.Activa }
            };

            // Modificación puntual del documento en Firestore.
            await _db.Collection(Coleccion).Document(id).UpdateAsync(datos);
        }

        // Elimina permanentemente un plan de la base de datos.
        // Operación administrativa de alto impacto.
        public async Task DeleteAsync(string id)
        {
            await _db.Collection(Coleccion).Document(id).DeleteAsync();
        }

        // Transforma un DocumentSnapshot de Firestore en la entidad Membresia.
        // Provee valores predeterminados seguros ante omisión de campos.
        private static Membresia MapearMembresia(DocumentSnapshot doc)
        {
            return new Membresia
            {
                Id = doc.Id,
                Nombre = doc.ContainsField("Nombre") ? doc.GetValue<string>("Nombre") : "",
                Descripcion = doc.ContainsField("Descripcion") ? doc.GetValue<string>("Descripcion") : "",
                Precio = doc.ContainsField("Precio") ? Convert.ToDecimal(doc.GetValue<double>("Precio")) : 0m,
                DuracionDias = doc.ContainsField("DuracionDias") ? doc.GetValue<int>("DuracionDias") : 30,
                Activa = !doc.ContainsField("Activa") || doc.GetValue<bool>("Activa")
            };
        }
    }
}
