using System;
using Google.Cloud.Firestore;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dfit.Models
{
    [FirestoreData]
    public class MembresiaUsuario
    {
        [Key]
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [Required]
        [FirestoreProperty]
        public string UsuarioId { get; set; } = string.Empty;
        [ForeignKey("UsuarioId")]
        [FirestoreProperty]
        public Usuario? Usuario { get; set; }

        [Required]
        [FirestoreProperty]
        public string MembresiaId { get; set; } = string.Empty;
        [ForeignKey("MembresiaId")]
        [FirestoreProperty]
        public Membresia? Membresia { get; set; }

        [Required]
        [FirestoreProperty]
        public DateTime FechaInicio { get; set; }

        [Required]
        [FirestoreProperty]
        public DateTime FechaFin { get; set; }

        public bool Activa { get; set; } = true;
    }
}
