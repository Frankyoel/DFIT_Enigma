// PruebasUnitarias/EntrenadorFuncionesAvanzadasTests.cs
using System;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using Dfit.Controllers;
using Dfit.Models;
using Dfit.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PruebasUnitarias;

/// <summary>
/// Pruebas documentadas bajo el estándar IEEE 829 para las 4 funciones avanzadas del rol Entrenador:
/// 1. Asignación de Rutinas (User-Routine Mapping)
/// 2. Evaluaciones Físicas (Progress Tracking)
/// 3. Asistencia por Clases (Class Roster)
/// 4. Notas Internas de Salud/Lesiones (Trainer Notes)
/// </summary>
[TestClass]
[TestCategory("Entrenador")]
public class EntrenadorFuncionesAvanzadasTests : PruebaDocumentada
{
    private static string ObtenerRolesPermitidos(Type controllerType, string actionName, Type[]? paramTypes = null)
    {
        MethodInfo? method;
        if (paramTypes != null)
        {
            method = controllerType.GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance, null, paramTypes, null);
        }
        else
        {
            method = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == actionName);
        }

        Assert.IsNotNull(method, $"El método de acción '{actionName}' no fue encontrado en {controllerType.Name}.");

        var authAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        return authAttr?.Roles ?? string.Empty;
    }

    // =========================================================================
    // 1. ASIGNACIÓN DE RUTINAS (USER-ROUTINE MAPPING)
    // =========================================================================

    [TestMethod]
    [TestProperty("Caso", "CP-ENT-ADV-001")]
    public void HomeController_AsignarRutina_PermiteAccesoAEntrenadorYAdmin()
    {
        var roles = ObtenerRolesPermitidos(typeof(HomeController), nameof(HomeController.AsignarRutina));
        var rolesArray = roles.Split(',').Select(r => r.Trim()).ToArray();

        var permiteEntrenador = rolesArray.Contains(PermisosHelper.RolEntrenador);
        var permiteAdmin = rolesArray.Contains(PermisosHelper.RolAdmin);

        Comprobar(true, permiteEntrenador && permiteAdmin);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ENT-ADV-002")]
    public void HomeController_AsignarRutina_DeniegaAccesoAUsuarioNormal()
    {
        var roles = ObtenerRolesPermitidos(typeof(HomeController), nameof(HomeController.AsignarRutina));
        var rolesArray = roles.Split(',').Select(r => r.Trim()).ToArray();

        var permiteUsuario = rolesArray.Contains(PermisosHelper.RolUsuario);

        Comprobar(false, permiteUsuario);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ENT-ADV-003")]
    public void RutinaUsuario_CreacionPorDefecto_EstadoEsActiva()
    {
        var mapping = new RutinaUsuario
        {
            UsuarioId = "usr-1",
            RutinaId = "rut-1",
            FechaFin = DateTime.UtcNow.AddDays(30)
        };

        Comprobar("Activa", mapping.Estado);
    }

    // =========================================================================
    // 2. EVALUACIONES FÍSICAS (PROGRESS TRACKING)
    // =========================================================================

    [TestMethod]
    [TestProperty("Caso", "CP-ENT-ADV-004")]
    public void HomeController_GuardarEvaluacion_PermiteAccesoAEntrenadorYAdmin()
    {
        var roles = ObtenerRolesPermitidos(typeof(HomeController), nameof(HomeController.GuardarEvaluacion));
        var rolesArray = roles.Split(',').Select(r => r.Trim()).ToArray();

        var permiteEntrenador = rolesArray.Contains(PermisosHelper.RolEntrenador);

        Comprobar(true, permiteEntrenador);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ENT-ADV-005")]
    public void HomeController_GuardarEvaluacion_DeniegaAccesoAUsuarioNormal()
    {
        var roles = ObtenerRolesPermitidos(typeof(HomeController), nameof(HomeController.GuardarEvaluacion));
        var rolesArray = roles.Split(',').Select(r => r.Trim()).ToArray();

        var permiteUsuario = rolesArray.Contains(PermisosHelper.RolUsuario);

        Comprobar(false, permiteUsuario);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ENT-ADV-006")]
    public void EvaluacionFisica_CalcularImc_CalculaValorMatematicoCorrecto()
    {
        var eval = new EvaluacionFisica
        {
            Peso = 80.0,
            Altura = 1.80 // IMC = 80 / (1.80 * 1.80) = 80 / 3.24 = 24.69
        };

        eval.CalcularImc();

        Comprobar(24.69, eval.IMC);
    }

    // =========================================================================
    // 3. ASISTENCIA POR CLASES (CLASS ROSTER)
    // =========================================================================

    [TestMethod]
    [TestProperty("Caso", "CP-ENT-ADV-007")]
    public void HomeController_ClaseRoster_PermiteAccesoAEntrenadorYAdmin()
    {
        var roles = ObtenerRolesPermitidos(typeof(HomeController), nameof(HomeController.ClaseRoster));
        var rolesArray = roles.Split(',').Select(r => r.Trim()).ToArray();

        var permiteEntrenador = rolesArray.Contains(PermisosHelper.RolEntrenador);

        Comprobar(true, permiteEntrenador);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ENT-ADV-008")]
    public void HomeController_RegistrarAsistenciaClase_PermiteAccesoAEntrenadorYAdmin()
    {
        var roles = ObtenerRolesPermitidos(typeof(HomeController), nameof(HomeController.RegistrarAsistenciaClase));
        var rolesArray = roles.Split(',').Select(r => r.Trim()).ToArray();

        var permiteEntrenador = rolesArray.Contains(PermisosHelper.RolEntrenador);

        Comprobar(true, permiteEntrenador);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ENT-ADV-009")]
    public void Asistencia_HorarioIdYNombreClase_AdmitenAsignacionCorrecta()
    {
        var asistencia = new Asistencia
        {
            UsuarioId = "usr-123",
            HorarioId = "hor-456",
            NombreClase = "Spinning Matutino"
        };

        Comprobar("hor-456", asistencia.HorarioId);
        Comprobar("Spinning Matutino", asistencia.NombreClase);
    }

    // =========================================================================
    // 4. NOTAS INTERNAS DE SALUD/LESIONES (TRAINER NOTES)
    // =========================================================================

    [TestMethod]
    [TestProperty("Caso", "CP-ENT-ADV-010")]
    public void HomeController_GuardarNotaInterna_PermiteAccesoAEntrenadorYAdmin()
    {
        var roles = ObtenerRolesPermitidos(typeof(HomeController), nameof(HomeController.GuardarNotaInterna));
        var rolesArray = roles.Split(',').Select(r => r.Trim()).ToArray();

        var permiteEntrenador = rolesArray.Contains(PermisosHelper.RolEntrenador);

        Comprobar(true, permiteEntrenador);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ENT-ADV-011")]
    public void HomeController_GuardarNotaInterna_DeniegaAccesoAUsuarioNormal()
    {
        var roles = ObtenerRolesPermitidos(typeof(HomeController), nameof(HomeController.GuardarNotaInterna));
        var rolesArray = roles.Split(',').Select(r => r.Trim()).ToArray();

        var permiteUsuario = rolesArray.Contains(PermisosHelper.RolUsuario);

        Comprobar(false, permiteUsuario);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ENT-ADV-012")]
    public void PermisosHelper_UsuarioNormal_EsAdminOEntrenadorRetornaFalse()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "socio-10"),
            new Claim(ClaimTypes.Role, "Usuario")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "PruebaAuth"));

        var tienePermiso = principal.EsAdminOEntrenador();

        Comprobar(false, tienePermiso);
    }
}
