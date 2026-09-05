using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace Dfit.Models
{
    [FirestoreData]
    public class EditUserViewModel
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

        [Required(ErrorMessage = "El DNI es requerido")]
        [StringLength(20)]
        public string DNI { get; set; } = string.Empty;

        [Required(ErrorMessage = "El celular es requerido")]
        [StringLength(20)]
        public string Celular { get; set; } = string.Empty;

        [Required]
        public string Rol { get; set; } = "Usuario";

        [FirestoreProperty]
        public bool Activo { get; set; }
    }
}
