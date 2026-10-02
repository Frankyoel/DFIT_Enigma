using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Dfit.Models;

namespace PruebasUnitarias;

[TestClass]
public class MembresiaAccesoTests
{
    [TestMethod]
    public void ValidarAcceso_ConMembresiaVigenteYActiva_DebePermitirAcceso()
    {
        // Regla del sistema: un usuario tiene acceso si Activa es true y FechaFin > UtcNow
        var membresiaUsuario = new MembresiaUsuario
        {
            UsuarioId = "user-001",
            MembresiaId = "plan-mensual",
            FechaInicio = DateTime.UtcNow.AddDays(-5),
            FechaFin = DateTime.UtcNow.AddDays(25),
            Activa = true
        };

        bool tieneAcceso = membresiaUsuario.Activa && membresiaUsuario.FechaFin > DateTime.UtcNow;

        Assert.IsTrue(tieneAcceso, "El usuario con membresía activa y fecha fin futura debe tener acceso.");
    }

    [TestMethod]
    public void ValidarAcceso_ConMembresiaVencida_DebeDenegarAcceso()
    {
        // Regla del sistema: si la fecha fin es menor o igual a UtcNow, el acceso debe ser denegado
        var membresiaUsuario = new MembresiaUsuario
        {
            UsuarioId = "user-002",
            MembresiaId = "plan-mensual",
            FechaInicio = DateTime.UtcNow.AddDays(-35),
            FechaFin = DateTime.UtcNow.AddDays(-5), // Vencida
            Activa = true
        };

        bool tieneAcceso = membresiaUsuario.Activa && membresiaUsuario.FechaFin > DateTime.UtcNow;

        Assert.IsFalse(tieneAcceso, "El usuario con membresía vencida no debe tener acceso.");
    }

    [TestMethod]
    public void ValidarAcceso_ConMembresiaInactiva_DebeDenegarAcceso()
    {
        // Regla del sistema: si Activa es false (cancelada), se deniega aunque la fecha no haya expirado
        var membresiaUsuario = new MembresiaUsuario
        {
            UsuarioId = "user-003",
            MembresiaId = "plan-mensual",
            FechaInicio = DateTime.UtcNow.AddDays(-5),
            FechaFin = DateTime.UtcNow.AddDays(25),
            Activa = false // Cancelada/Inactiva
        };

        bool tieneAcceso = membresiaUsuario.Activa && membresiaUsuario.FechaFin > DateTime.UtcNow;

        Assert.IsFalse(tieneAcceso, "El usuario con membresía inactiva/cancelada debe tener el acceso denegado.");
    }
}
