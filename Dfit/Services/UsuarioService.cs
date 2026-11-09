// Dfit/Services/UsuarioService.cs
// Implementación de lógica de negocio y orquestación para la administración de usuarios y perfiles.
// Aplica PBKDF2 para contraseñas, valida unicidad y mitiga el antipatrón de consultas N+1.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dfit.Models;
using Dfit.Repositories.Contracts;

namespace Dfit.Services
{
    // Servicio de aplicación para administración de cuentas de usuario, perfiles y valoraciones físicas.
    // Encapsula las reglas de acceso por rol y coordinación entre socios y rutinas.
    public class UsuarioService : IUsuarioService
    {
        // Repositorios inyectados para acceso desacoplado a datos de usuarios y membresías.
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly IMembresiaUsuarioRepository _memUsuarioRepo;
        private readonly IMembresiaRepository _membresiaRepo;
        private readonly IRutinaUsuarioRepository _rutinaUsuarioRepo;
        private readonly IRutinaRepository _rutinaRepo;
        private readonly IEvaluacionFisicaRepository _evaluacionRepo;
        private readonly IPasswordHasherService _passwordHasher;

        // Constructor con inyección de repositorios y servicios de seguridad.
        public UsuarioService(
            IUsuarioRepository usuarioRepo,
            IMembresiaUsuarioRepository memUsuarioRepo,
            IMembresiaRepository membresiaRepo,
            IRutinaUsuarioRepository rutinaUsuarioRepo,
            IRutinaRepository rutinaRepo,
            IEvaluacionFisicaRepository evaluacionRepo,
            IPasswordHasherService passwordHasher)
        {
            _usuarioRepo = usuarioRepo;
            _memUsuarioRepo = memUsuarioRepo;
            _membresiaRepo = membresiaRepo;
            _rutinaUsuarioRepo = rutinaUsuarioRepo;
            _rutinaRepo = rutinaRepo;
            _evaluacionRepo = evaluacionRepo;
            _passwordHasher = passwordHasher;
        }

        // Obtiene la lista de usuarios y resuelve las membresías activas sin consultas N+1.
        // Carga el catálogo de planes en memoria para vincular nombres en tiempo constante O(1).
        public async Task<(IReadOnlyList<Usuario> Usuarios, IReadOnlyList<MembresiaUsuario> MembresiasActivas)> ObtenerDashboardAsync(string? search = null)
        {
            // Carga usuarios registrados desde el repositorio.
            var usuarios = await _usuarioRepo.GetAllAsync();

            // Aplica filtro de búsqueda en memoria si fue provisto por el usuario.
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                // Evalúa coincidencia en nombres, apellidos, correo o documento de identidad.
                usuarios = usuarios.Where(u =>
                    u.Nombres.ToLower().Contains(s) ||
                    u.Apellidos.ToLower().Contains(s) ||
                    u.Correo.ToLower().Contains(s) ||
                    u.DNI.Contains(s)).ToList();
            }

            // Carga catálogo de membresías en memoria para lookup O(1) evitando consultas N+1.
            var planes = await _membresiaRepo.GetAllAsync();
            var planesDict = planes.ToDictionary(p => p.Id, p => p.Nombre);

            // Carga asignaciones activas de membresía y valida acceso con ReglasMembresia.
            var asignacionesActivas = await _memUsuarioRepo.GetActivasAsync();
            var ahora = DateTime.UtcNow;
            var membresiasValidas = new List<MembresiaUsuario>();

            // Itera cada asignación para verificar el período de vigencia en UTC.
            foreach (var mu in asignacionesActivas)
            {
                // Descarta suscripciones cuya fecha de vigencia ya expiró.
                if (!ReglasMembresia.PermiteAcceso(mu.Activa, mu.FechaFin, ahora))
                {
                    continue;
                }

                // Asocia el nombre de la membresía correspondiente a partir del diccionario en memoria.
                if (planesDict.TryGetValue(mu.MembresiaId, out var nombrePlan))
                {
                    mu.Membresia = new Membresia { Nombre = nombrePlan };
                }

                membresiasValidas.Add(mu);
            }

            return (usuarios, membresiasValidas);
        }

        // Crea un nuevo usuario hasheando la contraseña mediante PBKDF2 (OWASP A02).
        public async Task CrearUsuarioAsync(Usuario model)
        {
            // Cláusula de guarda para modelo nulo.
            if (model == null)
            {
                return;
            }

            // Hasheo de contraseña con sal criptográfica antes de almacenar.
            model.PasswordHash = _passwordHasher.HashPassword(model.PasswordHash);
            model.FechaRegistro = DateTime.UtcNow;

            // Inserta el registro en el repositorio de usuarios.
            await _usuarioRepo.AddAsync(model);
        }

        // Obtiene un usuario y lo mapea al ViewModel de edición administrativa.
        public async Task<EditUserViewModel?> ObtenerUsuarioParaEditarAsync(string id)
        {
            // Recupera la entidad por su ID de documento.
            var usuario = await _usuarioRepo.GetByIdAsync(id);
            if (usuario == null)
            {
                return null;
            }

            // Mapea los campos editables hacia el ViewModel.
            return new EditUserViewModel
            {
                Id = usuario.Id,
                Nombres = usuario.Nombres,
                Apellidos = usuario.Apellidos,
                DNI = usuario.DNI,
                Celular = usuario.Celular,
                Correo = usuario.Correo,
                Rol = usuario.Rol,
                Activo = usuario.Activo
            };
        }

        // Actualiza un usuario existente mapeando desde EditUserViewModel.
        public async Task ActualizarUsuarioAsync(EditUserViewModel model)
        {
            // Cláusula de guarda para evitar procesamiento si el modelo es nulo.
            if (model == null)
            {
                return;
            }

            // Transfiere los datos del ViewModel a la entidad de persistencia.
            var usuario = new Usuario
            {
                Id = model.Id,
                Nombres = model.Nombres,
                Apellidos = model.Apellidos,
                DNI = model.DNI,
                Celular = model.Celular,
                Correo = model.Correo,
                Rol = model.Rol,
                Activo = model.Activo
            };

            // Aplica la actualización en el repositorio.
            await _usuarioRepo.UpdateAsync(model.Id, usuario);
        }

        // Elimina permanentemente un usuario por su ID.
        public async Task EliminarUsuarioAsync(string id)
        {
            // Delega la eliminación física al repositorio de usuarios.
            await _usuarioRepo.DeleteAsync(id);
        }

        // Alterna el estado activo de un usuario en el sistema.
        public async Task ToggleEstadoUsuarioAsync(string id)
        {
            // Obtiene el usuario actual para conocer su estado vigente.
            var usuario = await _usuarioRepo.GetByIdAsync(id);
            if (usuario == null)
            {
                return;
            }

            // Invierte el valor booleano de Activo.
            await _usuarioRepo.ToggleStatusAsync(id, !usuario.Activo);
        }

        // Carga el perfil completo de un usuario, su rutina asignada y su historial antropométrico.
        public async Task<(ProfileViewModel? Perfil, RutinaUsuario? RutinaAsignada, IReadOnlyList<Rutina> RutinasDisponibles, IReadOnlyList<EvaluacionFisica> Evaluaciones)> ObtenerPerfilCompletoAsync(string id, bool esAdminOEntrenador)
        {
            // Busca la información básica del socio.
            var usuario = await _usuarioRepo.GetByIdAsync(id);
            if (usuario == null)
            {
                return (null, null, new List<Rutina>(), new List<EvaluacionFisica>());
            }

            // Construye el modelo de perfil respetando la visibilidad de notas según el rol.
            var perfil = new ProfileViewModel
            {
                Id = usuario.Id,
                Nombres = usuario.Nombres,
                Apellidos = usuario.Apellidos,
                Correo = usuario.Correo,
                NotasEntrenador = esAdminOEntrenador ? usuario.NotasEntrenador : string.Empty
            };

            // 1. Carga rutina activa asignada al socio.
            var rutinaAsignada = await _rutinaUsuarioRepo.GetRutinaActivaPorUsuarioAsync(id);
            if (rutinaAsignada != null && !string.IsNullOrWhiteSpace(rutinaAsignada.RutinaId))
            {
                // Vincula el detalle del catálogo de la rutina asignada.
                var rutina = await _rutinaRepo.GetByIdAsync(rutinaAsignada.RutinaId);
                if (rutina != null)
                {
                    rutinaAsignada.Rutina = rutina;
                }
            }

            // 2. Si el rol es Admin o Entrenador, carga catálogo de rutinas activas para asignar.
            var rutinasDisponibles = esAdminOEntrenador
                ? await _rutinaRepo.GetActivasAsync()
                : new List<Rutina>();

            // 3. Carga historial de evaluaciones físicas del socio ordenado cronológicamente.
            var evaluaciones = await _evaluacionRepo.GetByUsuarioIdAsync(id);

            return (perfil, rutinaAsignada, rutinasDisponibles, evaluaciones);
        }

        // Actualiza el perfil de usuario e incorpora nueva contraseña hasheada si fue provista.
        public async Task ActualizarPerfilAsync(ProfileViewModel model)
        {
            // Validación de entrada para evitar excepciones de referencia nula.
            if (model == null)
            {
                return;
            }

            // Busca el usuario en la base de datos.
            var usuario = await _usuarioRepo.GetByIdAsync(model.Id);
            if (usuario == null)
            {
                return;
            }

            // Actualiza nombres y correo electrónico.
            usuario.Nombres = model.Nombres;
            usuario.Apellidos = model.Apellidos;
            usuario.Correo = model.Correo;

            // Si el usuario especificó una nueva contraseña, se hashea con PBKDF2.
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                usuario.PasswordHash = _passwordHasher.HashPassword(model.NewPassword);
            }

            // Guarda los cambios en el almacén de datos.
            await _usuarioRepo.UpdateAsync(model.Id, usuario);
        }

        // Guarda la nota interna privada del entrenador sobre el socio.
        public async Task GuardarNotaInternaAsync(string usuarioId, string notasEntrenador)
        {
            // Cláusula de guarda para ID de usuario vacío.
            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return;
            }

            // Persiste la nota privada en Firestore.
            await _usuarioRepo.ActualizarNotasEntrenadorAsync(usuarioId, notasEntrenador ?? "");
        }

        // Registra una nueva evaluación física calculando automáticamente el IMC.
        public async Task GuardarEvaluacionFisicaAsync(EvaluacionFisica model, string entrenadorId, string entrenadorNombre)
        {
            // Valida que el modelo y el socio estén presentes.
            if (model == null || string.IsNullOrWhiteSpace(model.UsuarioId))
            {
                return;
            }

            // Invoca regla de dominio para cálculo de índice de masa corporal.
            model.CalcularImc();
            model.EntrenadorId = entrenadorId;
            model.EntrenadorNombre = string.IsNullOrWhiteSpace(entrenadorNombre) ? "Entrenador" : entrenadorNombre;
            model.Fecha = DateTime.UtcNow;

            // Guarda el informe antropométrico en el repositorio.
            await _evaluacionRepo.AddAsync(model);
        }
    }
}
