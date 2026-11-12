// Dfit/Services/IMembresiaService.cs
// Contrato de servicio de aplicación para la gestión de membresías y suscripciones.
// Encapsula las reglas de negocio de cálculo de vigencias y asignación de planes.
using System.Collections.Generic;
using System.Threading.Tasks;
using Dfit.Models;

namespace Dfit.Services
{
    // Servicio de orquestación para catálogo de membresías y vigencias de socios.
    public interface IMembresiaService
    {
        // Obtiene todos los planes de membresía registrados.
        Task<IReadOnlyList<Membresia>> ObtenerCatalogoMembresiasAsync();
        // Registra un nuevo plan de membresía en el catálogo.
        Task CrearMembresiaAsync(Membresia model);
        // Obtiene un plan de membresía para su edición.
        Task<Membresia?> ObtenerMembresiaParaEditarAsync(string id);
        // Actualiza las características de un plan existente.
        Task ActualizarMembresiaAsync(Membresia model);
        // Elimina un plan de membresía del catálogo.
        Task EliminarMembresiaAsync(string id);
        // Obtiene la lista de planes disponibles para asignación a socios.
        Task<IReadOnlyList<Membresia>> ObtenerPlanesParaAsignarAsync();
        // Asigna un plan de membresía a un socio calculando su vigencia de acceso.
        Task AsignarMembresiaASocioAsync(string usuarioId, string membresiaId);
        // Obtiene la membresía activa de un socio para su portal personal.
        Task<MembresiaUsuario?> ObtenerMiMembresiaAsync(string usuarioId);
        // Cancela una suscripción de membresía activa.
        Task CancelarMembresiaAsync(string membresiaUsuarioId);
    }
}
