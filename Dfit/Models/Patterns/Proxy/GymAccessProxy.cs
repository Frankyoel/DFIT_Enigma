using System;
using Google.Cloud.Firestore;

namespace Dfit.Models.Patterns.Proxy
{
    public interface IGymAccess
    {
        bool RegistrarAsistencia(string usuarioId);
        string MensajeError { get; }
    }

    public class RealGymAccess : IGymAccess
    {
        private readonly FirestoreDb _db;
        public string MensajeError { get; private set; } = string.Empty;

        public RealGymAccess(FirestoreDb db)
        {
            _db = db;
        }

        public bool RegistrarAsistencia(string usuarioId)
        {
            _db.Collection("Asistencias").AddAsync(new {
                UsuarioId = usuarioId,
                FechaHoraEntrada = DateTime.UtcNow
            }).Wait();
            MensajeError = "Asistencia registrada en Firebase.";
            return true;
        }
    }

    public class GymAccessProxy : IGymAccess
    {
        private readonly RealGymAccess _realAccess;
        private readonly FirestoreDb _db;
        public string MensajeError { get; private set; } = string.Empty;

        public GymAccessProxy(RealGymAccess realAccess, FirestoreDb db)
        {
            _realAccess = realAccess;
            _db = db;
        }

        public bool RegistrarAsistencia(string usuarioId)
        {
            var snap = _db.Collection("MembresiasUsuarios")
                .WhereEqualTo("UsuarioId", usuarioId)
                .WhereEqualTo("Activa", true)
                .GetSnapshotAsync().Result;
                
            bool tieneAcceso = false;
            foreach(var doc in snap.Documents) {
                if (doc.GetValue<DateTime>("FechaFin") > DateTime.UtcNow) {
                    tieneAcceso = true;
                    break;
                }
            }

            if (tieneAcceso)
            {
                bool resultado = _realAccess.RegistrarAsistencia(usuarioId);
                MensajeError = _realAccess.MensajeError;
                return resultado;
            }
            else
            {
                MensajeError = "Acceso denegado: No tienes una membresía activa.";
                return false;
            }
        }
    }
}
