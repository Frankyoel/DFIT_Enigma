using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Dfit.Models;
using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;

using Dfit.Models.Patterns.Singleton;
using Dfit.Models.Patterns.Proxy;
using Dfit.Models.Patterns.Strategy;
using Microsoft.AspNetCore.Authorization;
using Dfit.Utils;

namespace Dfit.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly FirestoreDb _db;
        
        private readonly IQRTokenService _qrTokenService;
        private readonly IGymAccess _gymAccess;
        private readonly PaymentContext _paymentContext;

        public HomeController(
            ILogger<HomeController> logger, 
            FirestoreDb db,
            IQRTokenService qrTokenService,
            IGymAccess gymAccess,
            PaymentContext paymentContext)
        {
            _logger = logger;
            _db = db;
            _qrTokenService = qrTokenService;
            _gymAccess = gymAccess;
            _paymentContext = paymentContext;
        }

        public IActionResult Index()
        {
            if (User.EsAdmin())
            {
                return RedirectToAction("Dashboard");
            }

            var currentUserId = User.ObtenerUsuarioId();
            return RedirectToAction("MiMembresia", new { id = currentUserId });
        }

        // =========================================================================
        // REDIRECCIONES DE COMPATIBILIDAD (AUTENTICACIÓN DELEGADA A AUTHCONTROLLER)
        // =========================================================================

        [AllowAnonymous]
        [HttpGet]
        [Route("Home/Login")]
        public IActionResult Login() => RedirectToAction("Login", "Auth");

        [AllowAnonymous]
        [HttpGet]
        [Route("Home/Register")]
        public IActionResult Register() => RedirectToAction("Register", "Auth");

        [AllowAnonymous]
        [HttpGet]
        [Route("Home/Logout")]
        public IActionResult Logout() => RedirectToAction("Logout", "Auth");

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Dashboard(string? search = null)
        {
            var snap = await _db.Collection("Usuarios").GetSnapshotAsync();
            var usuarios = new List<Usuario>();
            
            foreach (var doc in snap.Documents)
            {
                var u = new Usuario {
                    Id = doc.Id,
                    Nombres = doc.ContainsField("Nombres") ? doc.GetValue<string>("Nombres") : "",
                    Apellidos = doc.ContainsField("Apellidos") ? doc.GetValue<string>("Apellidos") : "",
                    Correo = doc.ContainsField("Correo") ? doc.GetValue<string>("Correo") : "",
                    DNI = doc.ContainsField("DNI") ? doc.GetValue<string>("DNI") : "",
                    Celular = doc.ContainsField("Celular") ? doc.GetValue<string>("Celular") : "",
                    Rol = doc.ContainsField("Rol") ? doc.GetValue<string>("Rol") : "",
                    Activo = doc.ContainsField("Activo") ? doc.GetValue<bool>("Activo") : true
                };
                usuarios.Add(u);
            }

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                usuarios = usuarios.Where(u => u.Nombres.ToLower().Contains(search) 
                                      || u.Apellidos.ToLower().Contains(search) 
                                      || u.Correo.ToLower().Contains(search)
                                      || u.DNI.Contains(search)).ToList();
                ViewBag.SearchTerm = search;
            }

            // Membresias activas
            var memSnap = await _db.Collection("MembresiasUsuarios").WhereEqualTo("Activa", true).GetSnapshotAsync();
            var muList = new List<MembresiaUsuario>();
            foreach(var doc in memSnap.Documents) {
                var fechaFin = doc.GetValue<DateTime>("FechaFin");
                if (ReglasMembresia.PermiteAcceso(doc.GetValue<bool>("Activa"), fechaFin, DateTime.UtcNow)) {
                    var mu = new MembresiaUsuario {
                        Id = doc.Id,
                        UsuarioId = doc.GetValue<string>("UsuarioId"),
                        MembresiaId = doc.GetValue<string>("MembresiaId"),
                        FechaFin = fechaFin,
                        Activa = true
                    };
                    var mDoc = await _db.Collection("Membresias").Document(mu.MembresiaId).GetSnapshotAsync();
                    if(mDoc.Exists) {
                        mu.Membresia = new Membresia { Nombre = mDoc.GetValue<string>("Nombre") };
                    }
                    muList.Add(mu);
                }
            }
            
            ViewBag.MembresiasActivas = muList;
            return View(usuarios);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult CreateUser() => View();

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateUser(Usuario model)
        {
            if (ModelState.IsValid)
            {
                var doc = _db.Collection("Usuarios").Document();
                model.Id = doc.Id;
                await doc.SetAsync(new {
                    Nombres = model.Nombres,
                    Apellidos = model.Apellidos,
                    Correo = model.Correo,
                    DNI = model.DNI,
                    Celular = model.Celular,
                    PasswordHash = model.PasswordHash,
                    Rol = model.Rol,
                    FechaRegistro = DateTime.UtcNow,
                    Activo = model.Activo
                });
                return RedirectToAction("Dashboard");
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditUser(string id)
        {
            var doc = await _db.Collection("Usuarios").Document(id).GetSnapshotAsync();
            if (!doc.Exists) return NotFound();

            var model = new EditUserViewModel
            {
                Id = doc.Id,
                Nombres = doc.GetValue<string>("Nombres"),
                Apellidos = doc.GetValue<string>("Apellidos"),
                DNI = doc.ContainsField("DNI") ? doc.GetValue<string>("DNI") : "",
                Celular = doc.ContainsField("Celular") ? doc.GetValue<string>("Celular") : "",
                Correo = doc.GetValue<string>("Correo"),
                Rol = doc.GetValue<string>("Rol"),
                Activo = doc.GetValue<bool>("Activo")
            };
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var docRef = _db.Collection("Usuarios").Document(model.Id);
                await docRef.UpdateAsync(new Dictionary<string, object>
                {
                    { "Nombres", model.Nombres },
                    { "Apellidos", model.Apellidos },
                    { "DNI", model.DNI },
                    { "Celular", model.Celular },
                    { "Correo", model.Correo },
                    { "Rol", model.Rol },
                    { "Activo", model.Activo }
                });
                return RedirectToAction("Dashboard");
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            await _db.Collection("Usuarios").Document(id).DeleteAsync();
            return RedirectToAction("Dashboard");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var doc = await _db.Collection("Usuarios").Document(id).GetSnapshotAsync();
            if (doc.Exists)
            {
                bool activo = doc.GetValue<bool>("Activo");
                await _db.Collection("Usuarios").Document(id).UpdateAsync("Activo", !activo);
            }
            return RedirectToAction("Dashboard");
        }

        public async Task<IActionResult> Profile(string? id) 
        {
            var currentUserId = User.ObtenerUsuarioId();
            if (string.IsNullOrEmpty(id))
            {
                id = currentUserId;
            }

            if (!User.EsAdminOPropietario(id))
            {
                return RedirectToAction("Profile", new { id = currentUserId });
            }

            var doc = await _db.Collection("Usuarios").Document(id!).GetSnapshotAsync();
            if (!doc.Exists) return NotFound();

            var model = new ProfileViewModel
            {
                Id = doc.Id,
                Nombres = doc.GetValue<string>("Nombres"),
                Apellidos = doc.GetValue<string>("Apellidos"),
                Correo = doc.GetValue<string>("Correo")
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!User.EsAdminOPropietario(model.Id))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                var dict = new Dictionary<string, object>
                {
                    { "Nombres", model.Nombres },
                    { "Apellidos", model.Apellidos },
                    { "Correo", model.Correo }
                };
                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    dict["PasswordHash"] = model.NewPassword;
                }
                await _db.Collection("Usuarios").Document(model.Id).UpdateAsync(dict);
                ViewBag.Message = "Perfil actualizado correctamente.";
            }
            return View(model);
        }

        // ==========================================
        // FASE 3: PLANES Y MEMBRESÍAS
        // ==========================================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Membresias()
        {
            var snap = await _db.Collection("Membresias").GetSnapshotAsync();
            var list = new List<Membresia>();
            foreach(var doc in snap.Documents) {
                list.Add(new Membresia {
                    Id = doc.Id,
                    Nombre = doc.GetValue<string>("Nombre"),
                    Descripcion = doc.GetValue<string>("Descripcion"),
                    Precio = Convert.ToDecimal(doc.GetValue<double>("Precio")),
                    DuracionDias = doc.GetValue<int>("DuracionDias"),
                    Activa = doc.GetValue<bool>("Activa")
                });
            }
            return View(list);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult CreateMembresia() => View();

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateMembresia(Membresia model)
        {
            if (ModelState.IsValid)
            {
                var doc = _db.Collection("Membresias").Document();
                await doc.SetAsync(new {
                    Nombre = model.Nombre,
                    Descripcion = model.Descripcion,
                    Precio = (double)model.Precio,
                    DuracionDias = model.DuracionDias,
                    Activa = model.Activa
                });
                return RedirectToAction("Membresias");
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditMembresia(string id)
        {
            var doc = await _db.Collection("Membresias").Document(id).GetSnapshotAsync();
            if (!doc.Exists) return NotFound();
            var m = new Membresia {
                Id = doc.Id,
                Nombre = doc.GetValue<string>("Nombre"),
                Descripcion = doc.GetValue<string>("Descripcion"),
                Precio = Convert.ToDecimal(doc.GetValue<double>("Precio")),
                DuracionDias = doc.GetValue<int>("DuracionDias"),
                Activa = doc.GetValue<bool>("Activa")
            };
            return View(m);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> EditMembresia(Membresia model)
        {
            if (ModelState.IsValid)
            {
                await _db.Collection("Membresias").Document(model.Id).UpdateAsync(new Dictionary<string, object> {
                    { "Nombre", model.Nombre },
                    { "Descripcion", model.Descripcion },
                    { "Precio", (double)model.Precio },
                    { "DuracionDias", model.DuracionDias },
                    { "Activa", model.Activa }
                });
                return RedirectToAction("Membresias");
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteMembresia(string id)
        {
            await _db.Collection("Membresias").Document(id).DeleteAsync();
            return RedirectToAction("Membresias");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignMembresia(string usuarioId)
        {
            ViewBag.UsuarioId = usuarioId;
            var snap = await _db.Collection("Membresias").WhereEqualTo("Activa", true).GetSnapshotAsync();
            var list = new List<Membresia>();
            foreach(var doc in snap.Documents) {
                var n = doc.GetValue<string>("Nombre");
                if (n != "Entrada Libre") {
                    list.Add(new Membresia { 
                        Id = doc.Id, 
                        Nombre = n,
                        Precio = Convert.ToDecimal(doc.GetValue<double>("Precio")),
                        DuracionDias = doc.GetValue<int>("DuracionDias")
                    });
                }
            }
            ViewBag.Membresias = list;
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AssignMembresia(string usuarioId, string membresiaId)
        {
            var doc = await _db.Collection("Membresias").Document(membresiaId).GetSnapshotAsync();
            if (doc.Exists)
            {
                var duracion = doc.GetValue<int>("DuracionDias");
                var fechaInicio = DateTime.UtcNow;
                await _db.Collection("MembresiasUsuarios").AddAsync(new {
                    UsuarioId = usuarioId,
                    MembresiaId = membresiaId,
                    FechaInicio = fechaInicio,
                    FechaFin = ReglasMembresia.CalcularFechaFin(fechaInicio, duracion),
                    Activa = true
                });
            }
            return RedirectToAction("Dashboard");
        }

        public async Task<IActionResult> MiMembresia(string? id) 
        {
            var currentUserId = User.ObtenerUsuarioId();
            if (string.IsNullOrEmpty(id))
            {
                id = currentUserId;
            }

            if (string.IsNullOrEmpty(id) || !User.EsAdminOPropietario(id))
            {
                return Forbid();
            }

            var snap = await _db.Collection("MembresiasUsuarios")
                .WhereEqualTo("UsuarioId", id)
                .WhereEqualTo("Activa", true)
                .GetSnapshotAsync();
            
            foreach(var doc in snap.Documents) {
                var fechaFin = doc.GetValue<DateTime>("FechaFin");
                if (ReglasMembresia.PermiteAcceso(doc.GetValue<bool>("Activa"), fechaFin, DateTime.UtcNow)) {
                    var mu = new MembresiaUsuario {
                        Id = doc.Id,
                        UsuarioId = doc.GetValue<string>("UsuarioId"),
                        MembresiaId = doc.GetValue<string>("MembresiaId"),
                        FechaFin = fechaFin,
                        FechaInicio = doc.GetValue<DateTime>("FechaInicio"),
                        Activa = true
                    };
                    var mDoc = await _db.Collection("Membresias").Document(mu.MembresiaId).GetSnapshotAsync();
                    if(mDoc.Exists) {
                        mu.Membresia = new Membresia { 
                            Nombre = mDoc.GetValue<string>("Nombre"),
                            Descripcion = mDoc.GetValue<string>("Descripcion")
                        };
                    }
                    return View(mu);
                }
            }
            return View(null);
        }

        [HttpPost]
        public async Task<IActionResult> CancelMembresia(string membresiaUsuarioId, string usuarioId)
        {
            if (!User.EsAdminOPropietario(usuarioId))
            {
                return Forbid();
            }

            await _db.Collection("MembresiasUsuarios").Document(membresiaUsuarioId).UpdateAsync("Activa", false);
            return RedirectToAction("MiMembresia", new { id = usuarioId });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AdminRemoveMembresia(string membresiaUsuarioId)
        {
            await _db.Collection("MembresiasUsuarios").Document(membresiaUsuarioId).UpdateAsync("Activa", false);
            return RedirectToAction("Dashboard");
        }

        // ==========================================
        // FASE 4: PAGOS Y REPORTES
        // ==========================================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegistrarPago(string usuarioId)
        {
            var uDoc = await _db.Collection("Usuarios").Document(usuarioId).GetSnapshotAsync();
            ViewBag.Usuario = new Usuario { Id = uDoc.Id, Nombres = uDoc.GetValue<string>("Nombres"), Apellidos = uDoc.GetValue<string>("Apellidos") };
            
            var snap = await _db.Collection("Membresias").WhereEqualTo("Activa", true).GetSnapshotAsync();
            var list = new List<Membresia>();
            foreach(var doc in snap.Documents) {
                list.Add(new Membresia { 
                    Id = doc.Id, 
                    Nombre = doc.GetValue<string>("Nombre"),
                    Precio = Convert.ToDecimal(doc.GetValue<double>("Precio")),
                    DuracionDias = doc.GetValue<int>("DuracionDias")
                });
            }
            ViewBag.Membresias = list;
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> RegistrarPago(Pago model)
        {
            if (ModelState.IsValid)
            {
                model.Monto = _paymentContext.ExecutePaymentForAmount(model.Monto);

                await _db.Collection("Pagos").AddAsync(new {
                    UsuarioId = model.UsuarioId,
                    MembresiaId = model.MembresiaId,
                    Monto = (double)model.Monto,
                    FechaPago = DateTime.UtcNow
                });
                return RedirectToAction("Dashboard");
            }
            return View(model);
        }

        public async Task<IActionResult> PagosUsuario(string? id) 
        {
            var currentUserId = User.ObtenerUsuarioId();
            if (string.IsNullOrEmpty(id))
            {
                id = currentUserId;
            }

            if (string.IsNullOrEmpty(id) || !User.EsAdminOPropietario(id))
            {
                return Forbid();
            }

            var snap = await _db.Collection("Pagos").WhereEqualTo("UsuarioId", id).GetSnapshotAsync();
            var list = new List<Pago>();
            foreach(var doc in snap.Documents) {
                var p = new Pago {
                    Id = doc.Id,
                    UsuarioId = doc.GetValue<string>("UsuarioId"),
                    MembresiaId = doc.GetValue<string>("MembresiaId"),
                    Monto = Convert.ToDecimal(doc.GetValue<double>("Monto")),
                    FechaPago = doc.GetValue<DateTime>("FechaPago")
                };
                var mDoc = await _db.Collection("Membresias").Document(p.MembresiaId).GetSnapshotAsync();
                if(mDoc.Exists) p.Membresia = new Membresia { Nombre = mDoc.GetValue<string>("Nombre") };
                list.Add(p);
            }
            return View(list.OrderByDescending(x => x.FechaPago).ToList());
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReporteFinanciero()
        {
            var snap = await _db.Collection("Pagos").GetSnapshotAsync();
            var pagos = new List<Pago>();
            foreach(var doc in snap.Documents) {
                var p = new Pago {
                    Id = doc.Id,
                    UsuarioId = doc.GetValue<string>("UsuarioId"),
                    MembresiaId = doc.GetValue<string>("MembresiaId"),
                    Monto = Convert.ToDecimal(doc.GetValue<double>("Monto")),
                    FechaPago = doc.GetValue<DateTime>("FechaPago")
                };
                var uDoc = await _db.Collection("Usuarios").Document(p.UsuarioId).GetSnapshotAsync();
                if(uDoc.Exists) p.Usuario = new Usuario { Nombres = uDoc.GetValue<string>("Nombres"), Apellidos = uDoc.GetValue<string>("Apellidos") };
                
                var mDoc = await _db.Collection("Membresias").Document(p.MembresiaId).GetSnapshotAsync();
                if(mDoc.Exists) p.Membresia = new Membresia { Nombre = mDoc.GetValue<string>("Nombre") };
                pagos.Add(p);
            }

            pagos = pagos.OrderByDescending(p => p.FechaPago).ToList();
            ViewBag.IngresosTotales = pagos.Sum(p => p.Monto);
            ViewBag.PagosDelMes = pagos.Where(p => p.FechaPago.Month == DateTime.UtcNow.Month).Sum(p => p.Monto);
            
            return View(pagos);
        }

        // ==========================================
        // FASE 5: OPERACIONES DEL GIMNASIO
        // ==========================================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Asistencia()
        {
            var snap = await _db.Collection("Asistencias").GetSnapshotAsync();
            var asistencias = new List<Asistencia>();
            foreach(var doc in snap.Documents) {
                var fh = doc.GetValue<DateTime>("FechaHoraEntrada");
                if (fh.Date == DateTime.UtcNow.Date) {
                    var a = new Asistencia {
                        Id = doc.Id,
                        UsuarioId = doc.GetValue<string>("UsuarioId"),
                        FechaHoraEntrada = fh
                    };
                    var uDoc = await _db.Collection("Usuarios").Document(a.UsuarioId).GetSnapshotAsync();
                    if(uDoc.Exists) a.Usuario = new Usuario { Nombres = uDoc.GetValue<string>("Nombres"), Apellidos = uDoc.GetValue<string>("Apellidos") };
                    asistencias.Add(a);
                }
            }
            
            var uSnap = await _db.Collection("Usuarios").WhereEqualTo("Activo", true).GetSnapshotAsync();
            var uList = new List<Usuario>();
            foreach(var doc in uSnap.Documents) {
                uList.Add(new Usuario { Id = doc.Id, Nombres = doc.GetValue<string>("Nombres"), Apellidos = doc.GetValue<string>("Apellidos") });
            }
            ViewBag.Usuarios = uList;
            
            return View(asistencias.OrderByDescending(x => x.FechaHoraEntrada).ToList());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> RegistrarAsistencia(string usuarioId)
        {
            var snap = await _db.Collection("MembresiasUsuarios")
                .WhereEqualTo("UsuarioId", usuarioId)
                .WhereEqualTo("Activa", true)
                .GetSnapshotAsync();
                
            bool membresiaActiva = false;
            foreach(var doc in snap.Documents) {
                if (ReglasMembresia.PermiteAcceso(doc.GetValue<bool>("Activa"),
                    doc.GetValue<DateTime>("FechaFin"), DateTime.UtcNow)) {
                    membresiaActiva = true;
                    break;
                }
            }

            if (!membresiaActiva)
            {
                TempData["ErrorAcceso"] = "El usuario no tiene una membresía activa o está vencida.";
                return RedirectToAction("Asistencia");
            }

            await _db.Collection("Asistencias").AddAsync(new {
                UsuarioId = usuarioId,
                FechaHoraEntrada = DateTime.UtcNow
            });

            TempData["ExitoAcceso"] = "Acceso concedido y asistencia registrada.";
            return RedirectToAction("Asistencia");
        }

        [Authorize(Roles = "Admin")]
        public IActionResult PantallaQR() => View();

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult GenerarTokenQR() => Json(new { token = _qrTokenService.GetCurrentToken() });

        [HttpGet]
        public IActionResult CheckIn(string token)
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                               ?? HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Auth");

            if (!_qrTokenService.ValidateToken(token))
            {
                ViewBag.Success = false;
                ViewBag.Message = "El código QR ha expirado o es inválido. Acércate a la pantalla y escanea el nuevo código.";
                return View("CheckInResult");
            }

            bool exito = _gymAccess.RegistrarAsistencia(userIdString);
            ViewBag.Success = exito;
            ViewBag.Message = exito ? _gymAccess.MensajeError : _gymAccess.MensajeError; 
            return View("CheckInResult");
        }

        // ==========================================
        // FASE 6: RUTINAS Y HORARIOS
        // ==========================================

        public async Task<IActionResult> Rutinas()
        {
            var snap = await _db.Collection("Rutinas").WhereEqualTo("Activa", true).GetSnapshotAsync();
            var list = new List<Rutina>();
            foreach(var doc in snap.Documents) {
                list.Add(new Rutina {
                    Id = doc.Id,
                    Nombre = doc.GetValue<string>("Nombre"),
                    Descripcion = doc.GetValue<string>("Descripcion"),
                    Nivel = doc.GetValue<string>("Nivel"),
                    VideoUrl = doc.GetValue<string>("VideoUrl"),
                    Activa = true
                });
            }
            return View(list);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult CreateRutina() => View();

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateRutina(Rutina model)
        {
            if (ModelState.IsValid)
            {
                await _db.Collection("Rutinas").AddAsync(new {
                    Nombre = model.Nombre,
                    Descripcion = model.Descripcion,
                    Nivel = model.Nivel,
                    VideoUrl = model.VideoUrl,
                    Activa = model.Activa
                });
                return RedirectToAction("Rutinas");
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditRutina(string id)
        {
            var doc = await _db.Collection("Rutinas").Document(id).GetSnapshotAsync();
            if (!doc.Exists) return NotFound();
            return View(new Rutina {
                Id = doc.Id,
                Nombre = doc.GetValue<string>("Nombre"),
                Descripcion = doc.GetValue<string>("Descripcion"),
                Nivel = doc.GetValue<string>("Nivel"),
                VideoUrl = doc.GetValue<string>("VideoUrl"),
                Activa = doc.GetValue<bool>("Activa")
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> EditRutina(Rutina model)
        {
            if (ModelState.IsValid)
            {
                await _db.Collection("Rutinas").Document(model.Id).UpdateAsync(new Dictionary<string, object> {
                    { "Nombre", model.Nombre },
                    { "Descripcion", model.Descripcion },
                    { "Nivel", model.Nivel },
                    { "VideoUrl", model.VideoUrl },
                    { "Activa", model.Activa }
                });
                return RedirectToAction("Rutinas");
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteRutina(string id)
        {
            await _db.Collection("Rutinas").Document(id).UpdateAsync("Activa", false);
            return RedirectToAction("Rutinas");
        }

        public async Task<IActionResult> Horarios()
        {
            var snap = await _db.Collection("Horarios").GetSnapshotAsync();
            var list = new List<Horario>();
            foreach(var doc in snap.Documents) {
                list.Add(new Horario {
                    Id = doc.Id,
                    DiaSemana = doc.GetValue<string>("DiaSemana"),
                    HoraInicio = TimeSpan.Parse(doc.GetValue<string>("HoraInicio")),
                    HoraFin = TimeSpan.Parse(doc.GetValue<string>("HoraFin")),
                    NombreClase = doc.GetValue<string>("NombreClase"),
                    Instructor = doc.GetValue<string>("Instructor"),
                    CupoMaximo = doc.GetValue<int>("CupoMaximo")
                });
            }
            return View(list.OrderBy(h => h.HoraInicio).ToList());
        }

        [Authorize(Roles = "Admin")]
        public IActionResult CreateHorario() => View();

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateHorario(Horario model)
        {
            if (ModelState.IsValid)
            {
                await _db.Collection("Horarios").AddAsync(new {
                    DiaSemana = model.DiaSemana,
                    HoraInicio = model.HoraInicio.ToString(),
                    HoraFin = model.HoraFin.ToString(),
                    NombreClase = model.NombreClase,
                    Instructor = model.Instructor,
                    CupoMaximo = model.CupoMaximo
                });
                return RedirectToAction("Horarios");
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditHorario(string id)
        {
            var doc = await _db.Collection("Horarios").Document(id).GetSnapshotAsync();
            if (!doc.Exists) return NotFound();
            return View(new Horario {
                Id = doc.Id,
                DiaSemana = doc.GetValue<string>("DiaSemana"),
                HoraInicio = TimeSpan.Parse(doc.GetValue<string>("HoraInicio")),
                HoraFin = TimeSpan.Parse(doc.GetValue<string>("HoraFin")),
                NombreClase = doc.GetValue<string>("NombreClase"),
                Instructor = doc.GetValue<string>("Instructor"),
                CupoMaximo = doc.GetValue<int>("CupoMaximo")
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> EditHorario(Horario model)
        {
            if (ModelState.IsValid)
            {
                await _db.Collection("Horarios").Document(model.Id).UpdateAsync(new Dictionary<string, object> {
                    { "DiaSemana", model.DiaSemana },
                    { "HoraInicio", model.HoraInicio.ToString() },
                    { "HoraFin", model.HoraFin.ToString() },
                    { "NombreClase", model.NombreClase },
                    { "Instructor", model.Instructor },
                    { "CupoMaximo", model.CupoMaximo }
                });
                return RedirectToAction("Horarios");
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteHorario(string id)
        {
            await _db.Collection("Horarios").Document(id).DeleteAsync();
            return RedirectToAction("Horarios");
        }

        [AllowAnonymous]
        public IActionResult Privacy() => View();

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
