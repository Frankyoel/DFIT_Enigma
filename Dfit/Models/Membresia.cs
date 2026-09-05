using System;
using Google.Cloud.Firestore;

using System.ComponentModel.DataAnnotations;

namespace Dfit.Models
{
    [FirestoreData]
    public class Membresia
    {
        [Key]
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [FirestoreProperty]
        public decimal Precio { get; set; }

        [Required]
        [FirestoreProperty]
        public int DuracionDias { get; set; } // Ej: 30, 90, 365

        public bool Activa { get; set; } = true;
    }
}
