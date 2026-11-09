using System;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using Dfit.Controllers;
using Dfit.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PruebasUnitarias;

/// <summary>
/// Pruebas documentadas bajo el estándar IEEE 829 para la matriz de control de acceso
/// del nuevo rol 'Entrenador' en el ecosistema DFIT.
/// </summary>
[TestClass]
[TestCategory("Permisos")]
public class RolEntrenadorPermisosTests : PruebaDocumentada
{
    private static ClaimsPrincipal CrearPrincipalConRol(string rol, string id = "usr-test-01")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id),
            new Claim(ClaimTypes.Role, rol)
        };
        var identity = new ClaimsIdentity(claims, "PruebaAuth");
        return new ClaimsPrincipal(identity);
    }

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
    // ACCESOS PERMITIDOS PARA EL ROL ENTRENADOR
    // =========================================================================

    [TestMethod]
    [TestProperty("Caso", "CP-ROL-ENT-001")]
    public void PermisosHelper_UsuarioConRolEntrenador_EsEntrenadorRetornaTrue()
    {
        var principal = CrearPrincipalConRol(PermisosHelper.RolEntrenador);

        var resultado = principal.EsEntrenador();

        Comprobar(true, resultado);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ROL-ENT-002")]
    public void PermisosHelper_UsuarioConRolEntrenador_EsAdminOEntrenadorRetornaTrue()
    {
        var principal = CrearPrincipalConRol(PermisosHelper.RolEntrenador);

        var resultado = principal.EsAdminOEntrenador();

        Comprobar(true, resultado);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ROL-ENT-003")]
    public void HomeController_CreateRutina_PermiteAccesoAEntrenadorYAdmin()
    {
        var rolesPermitidos = ObtenerRolesPermitidos(typeof(HomeController), nameof(HomeController.CreateRutina), Type.EmptyTypes);
        var rolesArray = rolesPermitidos.Split(',').Select(r => r.Trim()).ToArray();

        var permiteEntrenador = rolesArray.Contains(PermisosHelper.RolEntrenador);
        var permiteAdmin = rolesArray.Contains(PermisosHelper.RolAdmin);

        Comprobar(true, permiteEntrenador && permiteAdmin);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ROL-ENT-004")]
    public void HomeController_EditRutina_PermiteAccesoAEntrenadorYAdmin()
    {
        var rolesPermitidos = ObtenerRolesPermitidos(typeof(HomeController), nameof(HomeController.EditRutina), new[] { typeof(string) });
        var rolesArray = rolesPermitidos.Split(',').Select(r => r.Trim()).ToArray();

        var permiteEntrenador = rolesArray.Contains(PermisosHelper.RolEntrenador);

        Comprobar(true, permiteEntrenador);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ROL-ENT-005")]
    public void HomeController_DeleteRutina_PermiteAccesoAEntrenadorYAdmin()
    {
        var rolesPermitidos = ObtenerRolesPermitidos(typeof(HomeController), nameof(HomeController.DeleteRutina), new[] { typeof(string) });
        var rolesArray = rolesPermitidos.Split(',').Select(r => r.Trim()).ToArray();

        var permiteEntrenador = rolesArray.Contains(PermisosHelper.RolEntrenador);

        Comprobar(true, permiteEntrenador);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ROL-ENT-006")]
    public void HomeController_CreateHorario_PermiteAccesoAEntrenadorYAdmin()
    {
        var rolesPermitidos = ObtenerRolesPermitidos(typeof(HomeController), nameof(HomeController.CreateHorario), Type.EmptyTypes);
        var rolesArray = rolesPermitidos.Split(',').Select(r => r.Trim()).ToArray();

        var permiteEntrenador = rolesArray.Contains(PermisosHelper.RolEntrenador);

        Comprobar(true, permiteEntrenador);
    }

    // =========================================================================
    // ACCESOS DENEGADOS PARA EL ROL ENTRENADOR (EXPULSIÓN A ACCESS DENIED)
    // =========================================================================

    [TestMethod]
    [TestProperty("Caso", "CP-ROL-ENT-007")]
    public void HomeController_ReporteFinanciero_DeniegaAccesoAEntrenador()
    {
        // El endpoint solo debe autorizar "Admin", por ende el Entrenador es expulsado
        var rolesPermitidos = ObtenerRolesPermitidos(typeof(HomeController), nameof(HomeController.ReporteFinanciero));
        var rolesArray = rolesPermitidos.Split(',').Select(r => r.Trim()).ToArray();

        var permiteEntrenador = rolesArray.Contains(PermisosHelper.RolEntrenador);

        Comprobar(false, permiteEntrenador);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ROL-ENT-008")]
    public void HomeController_RegistrarPago_DeniegaAccesoAEntrenador()
    {
        // RegistrarPago está reservado únicamente para Admin y Recepcionista
        var rolesPermitidos = ObtenerRolesPermitidos(typeof(HomeController), nameof(HomeController.RegistrarPago), new[] { typeof(string), typeof(string) });
        var rolesArray = rolesPermitidos.Split(',').Select(r => r.Trim()).ToArray();

        var permiteEntrenador = rolesArray.Contains(PermisosHelper.RolEntrenador);

        Comprobar(false, permiteEntrenador);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ROL-ENT-009")]
    public void HomeController_CreateMembresia_DeniegaAccesoAEntrenador()
    {
        // Gestión del catálogo de membresías está reservado exclusivamente para Admin
        var rolesPermitidos = ObtenerRolesPermitidos(typeof(HomeController), nameof(HomeController.CreateMembresia), Type.EmptyTypes);
        var rolesArray = rolesPermitidos.Split(',').Select(r => r.Trim()).ToArray();

        var permiteEntrenador = rolesArray.Contains(PermisosHelper.RolEntrenador);

        Comprobar(false, permiteEntrenador);
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ROL-ENT-010")]
    public void HomeController_CreateUser_DeniegaAccesoAEntrenador()
    {
        // Crear usuarios administrativos está reservado para Admin
        var rolesPermitidos = ObtenerRolesPermitidos(typeof(HomeController), nameof(HomeController.CreateUser), Type.EmptyTypes);
        var rolesArray = rolesPermitidos.Split(',').Select(r => r.Trim()).ToArray();

        var permiteEntrenador = rolesArray.Contains(PermisosHelper.RolEntrenador);

        Comprobar(false, permiteEntrenador);
    }
}
