using System;
using Google.Cloud.Firestore;

using System.ComponentModel.DataAnnotations;

namespace Dfit.Models
{
    [FirestoreData]
    public class Horario
    {
        [Key]
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "El día de la semana es obligatorio")]
        [Display(Name = "Día de la Semana")]
        [FirestoreProperty]
        public string DiaSemana { get; set; } // Lunes, Martes, etc.

        [Required(ErrorMessage = "La hora de inicio es obligatoria")]
        [Display(Name = "Hora de Inicio")]
        [FirestoreProperty]
        public TimeSpan HoraInicio { get; set; }

        [Required(ErrorMessage = "La hora de fin es obligatoria")]
        [Display(Name = "Hora de Fin")]
        [FirestoreProperty]
        public TimeSpan HoraFin { get; set; }

        [Required(ErrorMessage = "El nombre de la clase es obligatorio")]
        [Display(Name = "Nombre de la Clase")]
        [FirestoreProperty]
        public string NombreClase { get; set; }

        [Display(Name = "Instructor")]
        [FirestoreProperty]
        public string Instructor { get; set; }

        [Display(Name = "Cupo Máximo")]
        public int CupoMaximo { get; set; } = 20;
    }
}
