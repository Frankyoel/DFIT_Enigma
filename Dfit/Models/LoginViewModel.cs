using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace Dfit.Models
{
    [FirestoreData]
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El correo electrónico es requerido")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
