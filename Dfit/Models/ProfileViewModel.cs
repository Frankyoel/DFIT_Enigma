using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace Dfit.Models
{
    [FirestoreData]
    public class ProfileViewModel
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los nombres son requeridos")]
        [StringLength(100)]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son requeridos")]
        [StringLength(100)]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es requerido")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        public string Correo { get; set; } = string.Empty;
        
        [DataType(DataType.Password)]
        [FirestoreProperty]
        public string? NewPassword { get; set; }
    }
}
