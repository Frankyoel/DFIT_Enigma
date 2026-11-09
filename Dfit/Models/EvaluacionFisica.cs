// Dfit/Models/EvaluacionFisica.cs
using System;
using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace Dfit.Models
{
    /// <summary>
    /// Modelo que almacena el seguimiento y las métricas antropométricas de un usuario realizadas por un Entrenador.
    /// Almacenado en la colección 'EvaluacionesFisicas' en Cloud Firestore.
    /// </summary>
    [FirestoreData]
    public class EvaluacionFisica
    {
        [Key]
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [Required]
        [FirestoreProperty]
        public string UsuarioId { get; set; } = string.Empty;

        [Required]
        [FirestoreProperty]
        public string EntrenadorId { get; set; } = string.Empty;

        [FirestoreProperty]
        public string EntrenadorNombre { get; set; } = string.Empty;

        [FirestoreProperty]
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        [Range(20, 300, ErrorMessage = "El peso debe estar entre 20 y 300 kg")]
        [FirestoreProperty]
        public double Peso { get; set; }

        [Range(0.5, 2.5, ErrorMessage = "La altura debe estar en metros (ej. 1.75)")]
        [FirestoreProperty]
        public double Altura { get; set; }

        [FirestoreProperty]
        public double IMC { get; set; }

        [Range(0, 100)]
        [FirestoreProperty]
        public double PorcentajeGrasa { get; set; }

        [Range(0, 100)]
        [FirestoreProperty]
        public double MasaMuscular { get; set; }

        [FirestoreProperty]
        public string Notas { get; set; } = string.Empty;

        /// <summary>
        /// Calcula el IMC automáticamente a partir del Peso (kg) y la Altura (m).
        /// </summary>
        public void CalcularImc()
        {
            if (Peso > 0 && Altura > 0)
            {
                IMC = Math.Round(Peso / (Altura * Altura), 2);
            }
        }
    }
}
