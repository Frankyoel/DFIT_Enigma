// Dfit/Repositories/Firestore/PagoRepository.cs
// Implementación de acceso a datos para transacciones y cobranzas en 'Pagos'.
// Permite consultar pagos diarios y balances financieros sin traer documentos innecesarios.
// Optimiza los cálculos de reportes financieros mediante consultas delimitadas por fecha.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Dfit.Models;
using Dfit.Repositories.Contracts;

namespace Dfit.Repositories.Firestore
{
    // Repositorio Firestore para operaciones de pagos y cobranza.
    // Encapsula las transacciones de recepción y el historial financiero.
    public class PagoRepository : IPagoRepository
    {
        // Instancia de FirestoreDb para acceso a la base de datos distribuida.
        private readonly FirestoreDb _db;

        // Nombre de la colección de pagos en Cloud Firestore.
        private const string Coleccion = "Pagos";

        // Constructor con inyección del cliente Firestore.
        public PagoRepository(FirestoreDb db)
        {
            _db = db;
        }

        // Obtiene un pago específico por su identificador.
        // Retorna null si el id no es válido o no existe en Firestore.
        public async Task<Pago?> GetByIdAsync(string id)
        {
            // Cláusula de guarda para validar el identificador del pago.
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            // Lectura directa del documento de pago en Firestore.
            var doc = await _db.Collection(Coleccion).Document(id).GetSnapshotAsync();

            // Verificación de existencia del documento en la colección.
            if (!doc.Exists)
            {
                return null;
            }

            // Conversión del snapshot a la entidad Pago.
            return MapearPago(doc);
        }

        // Obtiene la totalidad de pagos registrados en la plataforma.
        // Utilizado en reportería consolidada y auditorías contables.
        public async Task<IReadOnlyList<Pago>> GetAllAsync()
        {
            // Ejecución asíncrona sobre la colección de pagos completa.
            var snap = await _db.Collection(Coleccion).GetSnapshotAsync();
            var lista = new List<Pago>();

            // Iteración para mapeo seguro de cada transacción.
            foreach (var doc in snap.Documents)
            {
                lista.Add(MapearPago(doc));
            }

            return lista;
        }

        // Obtiene el historial de pagos de un socio particular ordenado cronológicamente.
        // Permite consultar los recibos emitidos desde el portal del socio.
        public async Task<IReadOnlyList<Pago>> GetByUsuarioIdAsync(string usuarioId)
        {
            // Consulta indexada filtrando por el identificador del usuario.
            var snap = await _db.Collection(Coleccion)
                .WhereEqualTo("UsuarioId", usuarioId)
                .GetSnapshotAsync();

            var lista = new List<Pago>();

            // Mapeo de transacciones del usuario.
            foreach (var doc in snap.Documents)
            {
                lista.Add(MapearPago(doc));
            }

            // Ordenamiento cronológico descendente: transacciones recientes primero.
            return lista.OrderByDescending(p => p.FechaPago).ToList();
        }

        // Obtiene los pagos realizados en un rango de fechas.
        // Soporta la generación de balances diarios, mensuales o anuales.
        public async Task<IReadOnlyList<Pago>> GetByRangoFechaAsync(DateTime inicio, DateTime fin)
        {
            // Consulta delimitada por rango temporal para minimizar la lectura de documentos.
            var snap = await _db.Collection(Coleccion)
                .WhereGreaterThanOrEqualTo("FechaPago", inicio)
                .WhereLessThanOrEqualTo("FechaPago", fin)
                .GetSnapshotAsync();

            var lista = new List<Pago>();

            // Transformación de documentos a modelos de pago.
            foreach (var doc in snap.Documents)
            {
                lista.Add(MapearPago(doc));
            }

            // Retorno ordenado por fecha de pago más reciente.
            return lista.OrderByDescending(p => p.FechaPago).ToList();
        }

        // Registra un nuevo pago en Cloud Firestore.
        // Valida fecha de pago y método de cobro predeterminado.
        public async Task<string> AddAsync(Pago entity)
        {
            // Creación de una referencia vacía para autogenerar el ID.
            var doc = _db.Collection(Coleccion).Document();
            entity.Id = doc.Id;

            // Construcción del documento de pago para Firestore.
            var datos = new Dictionary<string, object>
            {
                { "UsuarioId", entity.UsuarioId },
                { "MembresiaId", entity.MembresiaId },
                { "Monto", (double)entity.Monto },
                { "FechaPago", entity.FechaPago == default ? DateTime.UtcNow : entity.FechaPago },
                { "MetodoPago", string.IsNullOrWhiteSpace(entity.MetodoPago) ? "Efectivo" : entity.MetodoPago }
            };

            // Inserción atómica en Firestore.
            await doc.SetAsync(datos);
            return doc.Id;
        }

        // Actualiza los datos de un pago registrado.
        // Utilizado en correcciones excepcionales de monto o medio de pago.
        public async Task UpdateAsync(string id, Pago entity)
        {
            // Diccionario con los datos actualizados de la transacción.
            var datos = new Dictionary<string, object>
            {
                { "Monto", (double)entity.Monto },
                { "MetodoPago", entity.MetodoPago }
            };

            // Grabación de la modificación en Firestore.
            await _db.Collection(Coleccion).Document(id).UpdateAsync(datos);
        }

        // Elimina un registro de pago en caso de anulación administrativa.
        public async Task DeleteAsync(string id)
        {
            await _db.Collection(Coleccion).Document(id).DeleteAsync();
        }

        // Transforma un DocumentSnapshot en la entidad Pago.
        // Previene excepciones por conversión de tipos numéricos entre double, long y decimal.
        private static Pago MapearPago(DocumentSnapshot doc)
        {
            // Extracción segura del monto para tolerar representaciones numéricas mixtas en Firestore.
            decimal monto = 0m;
            if (doc.ContainsField("Monto"))
            {
                var valorMonto = doc.GetValue<object>("Monto");
                if (valorMonto != null)
                {
                    monto = Convert.ToDecimal(valorMonto);
                }
            }

            return new Pago
            {
                Id = doc.Id,
                UsuarioId = doc.ContainsField("UsuarioId") ? doc.GetValue<string>("UsuarioId") : "",
                MembresiaId = doc.ContainsField("MembresiaId") ? doc.GetValue<string>("MembresiaId") : "",
                Monto = monto,
                FechaPago = doc.ContainsField("FechaPago") ? doc.GetValue<DateTime>("FechaPago") : DateTime.UtcNow,
                MetodoPago = doc.ContainsField("MetodoPago") ? doc.GetValue<string>("MetodoPago") : "Efectivo"
            };
        }
    }
}
