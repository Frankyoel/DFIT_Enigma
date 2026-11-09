// Dfit/Models/RutinaUsuario.cs
using System;
using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace Dfit.Models
{
    /// <summary>
    /// Modelo que representa la asignación de una rutina de entrenamiento a un socio del gimnasio.
    /// Almacenado en la colección 'RutinasUsuarios' en Cloud Firestore.
    /// </summary>
    [FirestoreData]
    public class RutinaUsuario
    {
        [Key]
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [Required]
        [FirestoreProperty]
        public string UsuarioId { get; set; } = string.Empty;

        [Required]
        [FirestoreProperty]
        public string RutinaId { get; set; } = string.Empty;

        [FirestoreProperty]
        public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;

        [FirestoreProperty]
        public DateTime? FechaFin { get; set; }

        /// <summary>
        /// Estado de la asignación: "Activa", "Completada" o "Cancelada".
        /// </summary>
        [FirestoreProperty]
        public string Estado { get; set; } = "Activa";

        // Propiedades de navegación / soporte para la vista
        public Rutina? Rutina { get; set; }
        public Usuario? Usuario { get; set; }
    }
}
