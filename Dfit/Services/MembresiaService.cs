// Dfit/Services/MembresiaService.cs
// Implementación de lógica de negocio para planes, membresías y suscripciones.
// Aplica las reglas puras de vigencia de ReglasMembresia y elimina consultas N+1.
// Desacopla la persistencia de suscripciones de los controladores web.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dfit.Models;
using Dfit.Repositories.Contracts;

namespace Dfit.Services
{
    // Servicio de aplicación para administración del catálogo de membresías y suscripciones de socios.
    public class MembresiaService : IMembresiaService
    {
        // Repositorios inyectados para acceso a planes y asignaciones de socios.
        private readonly IMembresiaRepository _membresiaRepo;
        private readonly IMembresiaUsuarioRepository _memUsuarioRepo;

        // Constructor con inyección de dependencias.
        public MembresiaService(
            IMembresiaRepository membresiaRepo,
            IMembresiaUsuarioRepository memUsuarioRepo)
        {
            _membresiaRepo = membresiaRepo;
            _memUsuarioRepo = memUsuarioRepo;
        }

        // Obtiene todos los planes de membresía registrados en el catálogo.
        public async Task<IReadOnlyList<Membresia>> ObtenerCatalogoMembresiasAsync()
        {
            // Recupera la totalidad de planes desde el repositorio.
            return await _membresiaRepo.GetAllAsync();
        }

        // Registra un nuevo plan de membresía en el catálogo.
        public async Task CrearMembresiaAsync(Membresia model)
        {
            // Valida que el modelo no sea nulo antes de persistir.
            if (model == null)
            {
                return;
            }

            // Inserción en el almacén de datos.
            await _membresiaRepo.AddAsync(model);
        }

        // Obtiene un plan de membresía para su edición.
        public async Task<Membresia?> ObtenerMembresiaParaEditarAsync(string id)
        {
            // Cláusula de guarda para evitar consultas con identificador inválido.
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            // Búsqueda directa del plan.
            return await _membresiaRepo.GetByIdAsync(id);
        }

        // Actualiza las características de un plan existente.
        public async Task ActualizarMembresiaAsync(Membresia model)
        {
            // Validación de existencia del modelo y su clave.
            if (model == null || string.IsNullOrWhiteSpace(model.Id))
            {
                return;
            }

            // Actualización persistente del plan.
            await _membresiaRepo.UpdateAsync(model.Id, model);
        }

        // Elimina un plan de membresía del catálogo.
        public async Task EliminarMembresiaAsync(string id)
        {
            // Validación de identificador antes de invocar borrado.
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            // Borrado del plan en el repositorio.
            await _membresiaRepo.DeleteAsync(id);
        }

        // Obtiene la lista de planes disponibles para asignación a socios.
        // Filtra planes inactivos o exclusivos para evitar asignaciones incorrectas.
        public async Task<IReadOnlyList<Membresia>> ObtenerPlanesParaAsignarAsync()
        {
            // Consulta de planes marcados como activos.
            var planes = await _membresiaRepo.GetActivasAsync();

            // Excluye planes especiales como "Entrada Libre" para suscripciones regulares.
            return planes.Where(p => p.Nombre != "Entrada Libre").ToList();
        }

        // Asigna un plan de membresía a un socio calculando su vigencia de acceso.
        public async Task AsignarMembresiaASocioAsync(string usuarioId, string membresiaId)
        {
            // Cláusula de guarda para validar datos de entrada requeridos.
            if (string.IsNullOrWhiteSpace(usuarioId) || string.IsNullOrWhiteSpace(membresiaId))
            {
                return;
            }

            // Recupera la información del plan para conocer su duración en días.
            var plan = await _membresiaRepo.GetByIdAsync(membresiaId);
            if (plan == null)
            {
                return;
            }

            // Cálculo puro de la fecha de inicio y vencimiento.
            var fechaInicio = DateTime.UtcNow;
            var fechaFin = ReglasMembresia.CalcularFechaFin(fechaInicio, plan.DuracionDias);

            // Desactiva cualquier membresía activa previa del socio.
            await _memUsuarioRepo.DesactivarMembresiasPreviasAsync(usuarioId);

            // Registro de la nueva suscripción activa.
            var asignacion = new MembresiaUsuario
            {
                UsuarioId = usuarioId,
                MembresiaId = membresiaId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                Activa = true
            };

            await _memUsuarioRepo.AddAsync(asignacion);
        }

        // Obtiene la membresía activa de un socio para su portal personal.
        // Resuelve el nombre y descripción del plan sin incurrir en N+1 queries.
        public async Task<MembresiaUsuario?> ObtenerMiMembresiaAsync(string usuarioId)
        {
            // Cláusula de guarda para ID de usuario.
            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return null;
            }

            // Consulta las asignaciones del socio en el repositorio.
            var asignaciones = await _memUsuarioRepo.GetByUsuarioIdAsync(usuarioId, soloActivas: true);
            var ahora = DateTime.UtcNow;

            // Busca la suscripción vigente según las reglas de acceso.
            foreach (var mu in asignaciones)
            {
                if (ReglasMembresia.PermiteAcceso(mu.Activa, mu.FechaFin, ahora))
                {
                    // Enlaza la información del plan asociado.
                    var plan = await _membresiaRepo.GetByIdAsync(mu.MembresiaId);
                    if (plan != null)
                    {
                        mu.Membresia = new Membresia
                        {
                            Nombre = plan.Nombre,
                            Descripcion = plan.Descripcion
                        };
                    }

                    return mu;
                }
            }

            return null;
        }

        // Cancela una suscripción de membresía activa.
        public async Task CancelarMembresiaAsync(string membresiaUsuarioId)
        {
            // Cláusula de guarda para validar el identificador.
            if (string.IsNullOrWhiteSpace(membresiaUsuarioId))
            {
                return;
            }

            // Desactiva la asignación mediante actualización de estado.
            var asignacion = await _memUsuarioRepo.GetByIdAsync(membresiaUsuarioId);
            if (asignacion == null)
            {
                return;
            }

            asignacion.Activa = false;
            await _memUsuarioRepo.UpdateAsync(membresiaUsuarioId, asignacion);
        }
    }
}
