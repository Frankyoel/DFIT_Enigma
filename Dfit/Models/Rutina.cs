using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace Dfit.Models
{
    [FirestoreData]
    public class Rutina
    {
        [Key]
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de la rutina es obligatorio")]
        [StringLength(100)]
        [FirestoreProperty]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [FirestoreProperty]
        public string Descripcion { get; set; }

        [Display(Name = "Nivel")]
        [FirestoreProperty]
        public string Nivel { get; set; } // Principiante, Intermedio, Avanzado

        [Display(Name = "URL del Video de YouTube")]
        [FirestoreProperty]
        public string VideoUrl { get; set; }

        public bool Activa { get; set; } = true;
    }
}
