using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Dfit.Models
{
    [FirestoreData]
    public class RegisterViewModel : IValidatableObject
    {
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
        [StringLength(8, MinimumLength = 8, ErrorMessage = "El DNI debe contener 8 dígitos")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI solo debe contener números")]
        public string DNI { get; set; } = string.Empty;

        [Required(ErrorMessage = "El celular es requerido")]
        [StringLength(9, MinimumLength = 9, ErrorMessage = "El celular debe contener 9 dígitos")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "El celular solo debe contener números")]
        public string Celular { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$",
            ErrorMessage = "La contraseña debe tener al menos 8 caracteres e incluir mayúsculas, minúsculas, números y al menos un símbolo")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirmar la contraseña es requerido")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Password))
                yield break;

            var lowerPwd = Password.ToLowerInvariant();

            // Bloquear secuencias numéricas y alfabéticas triviales (OWASP)
            string[] trivialSequences = { "123456", "234567", "345678", "456789", "567890", "abcdef", "qwerty", "password", "contrasena" };
            if (trivialSequences.Any(seq => lowerPwd.Contains(seq)))
            {
                yield return new ValidationResult(
                    "La contraseña no debe contener secuencias simples ni palabras comunes.",
                    new[] { nameof(Password) });
            }

            // Prohibir que la contraseña contenga el DNI o el Celular
            if (!string.IsNullOrWhiteSpace(DNI) && DNI.Length >= 6 && Password.Contains(DNI))
            {
                yield return new ValidationResult(
                    "La contraseña no puede incluir tu número de DNI.",
                    new[] { nameof(Password) });
            }

            if (!string.IsNullOrWhiteSpace(Celular) && Celular.Length >= 6 && Password.Contains(Celular))
            {
                yield return new ValidationResult(
                    "La contraseña no puede incluir tu número de celular.",
                    new[] { nameof(Password) });
            }
        }
    }
}
