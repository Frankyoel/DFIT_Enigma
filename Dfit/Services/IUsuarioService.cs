// Dfit/Services/IUsuarioService.cs
// Contrato de servicio de aplicación para la gestión de usuarios, perfiles y antropometría.
// Desacopla la lógica de negocio y presentación de la persistencia de datos.
using System.Collections.Generic;
using System.Threading.Tasks;
using Dfit.Models;

namespace Dfit.Services
{
    // Servicio de orquestación para administración de socios y perfiles.
    public interface IUsuarioService
    {
        // Obtiene la lista de usuarios y membresías activas para la vista de Dashboard.
        Task<(IReadOnlyList<Usuario> Usuarios, IReadOnlyList<MembresiaUsuario> MembresiasActivas)> ObtenerDashboardAsync(string? search = null);
        // Registra un nuevo usuario con hashing criptográfico de contraseña.
        Task CrearUsuarioAsync(Usuario model);
        // Obtiene un usuario transformado a EditUserViewModel para su edición administrativa.
        Task<EditUserViewModel?> ObtenerUsuarioParaEditarAsync(string id);
        // Actualiza los datos de un usuario existente a partir del ViewModel de edición.
        Task ActualizarUsuarioAsync(EditUserViewModel model);
        // Elimina un usuario por su identificador.
        Task EliminarUsuarioAsync(string id);
        // Alterna el estado activo de un usuario.
        Task ToggleEstadoUsuarioAsync(string id);
        // Carga el perfil completo de un usuario, su rutina asignada y su historial de evaluaciones físicas.
        Task<(ProfileViewModel? Perfil, RutinaUsuario? RutinaAsignada, IReadOnlyList<Rutina> RutinasDisponibles, IReadOnlyList<EvaluacionFisica> Evaluaciones)> ObtenerPerfilCompletoAsync(string id, bool esAdminOEntrenador);
        // Actualiza el perfil de usuario verificando la propiedad o rol de administrador.
        Task ActualizarPerfilAsync(ProfileViewModel model);
        // Guarda la nota interna privada escrita por el entrenador sobre el socio.
        Task GuardarNotaInternaAsync(string usuarioId, string notasEntrenador);
        // Registra una nueva evaluación antropométrica física para un socio.
        Task GuardarEvaluacionFisicaAsync(EvaluacionFisica model, string entrenadorId, string entrenadorNombre);
    }
}
