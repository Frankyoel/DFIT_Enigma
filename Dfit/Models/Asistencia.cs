using System;
using Google.Cloud.Firestore;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dfit.Models
{
    [FirestoreData]
    public class Asistencia
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
        public DateTime FechaHoraEntrada { get; set; } = DateTime.Now;
    }
}
