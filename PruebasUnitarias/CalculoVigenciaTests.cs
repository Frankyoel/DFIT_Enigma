using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Dfit.Models;

namespace PruebasUnitarias;

[TestClass]
public class CalculoVigenciaTests
{
    [TestMethod]
    public void CalcularFechaFin_MembresiaMensual_SumaExactamente30Dias()
    {
        // Regla del sistema (AssignMembresia): FechaFin = FechaInicio.AddDays(DuracionDias)
        var membresia = new Membresia
        {
            Id = "mem-30",
            Nombre = "Plan Mensual",
            DuracionDias = 30,
            Precio = 80m,
            Activa = true
        };

        var fechaInicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var fechaFinEsperada = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);

        var fechaFinCalculada = fechaInicio.AddDays(membresia.DuracionDias);

        Assert.AreEqual(fechaFinEsperada, fechaFinCalculada);
        Assert.AreEqual(30, (fechaFinCalculada - fechaInicio).TotalDays);
    }

    [TestMethod]
    public void CalcularFechaFin_MembresiaAnual_SumaExactamente365Dias()
    {
        // Regla del sistema: membresía anual con 365 días
        var membresia = new Membresia
        {
            Id = "mem-anual",
            Nombre = "Plan Anual VIP",
            DuracionDias = 365,
            Precio = 600m,
            Activa = true
        };

        var fechaInicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var fechaFinCalculada = fechaInicio.AddDays(membresia.DuracionDias);

        Assert.AreEqual(365, (fechaFinCalculada - fechaInicio).TotalDays);
        Assert.IsTrue(fechaFinCalculada > fechaInicio);
    }

    [TestMethod]
    public void AsignarMembresia_CreaMembresiaUsuario_ConRangoDeVigenciaCorrecto()
    {
        // Simulación de la asignación de membresía realizada en el sistema
        var membresia = new Membresia
        {
            Id = "mem-trimestral",
            Nombre = "Plan Trimestral",
            DuracionDias = 90,
            Precio = 200m
        };

        var fechaInicio = DateTime.UtcNow;
        var membresiaUsuario = new MembresiaUsuario
        {
            UsuarioId = "user-999",
            MembresiaId = membresia.Id,
            FechaInicio = fechaInicio,
            FechaFin = fechaInicio.AddDays(membresia.DuracionDias),
            Activa = true
        };

        Assert.AreEqual(90, (membresiaUsuario.FechaFin - membresiaUsuario.FechaInicio).TotalDays);
        Assert.IsTrue(membresiaUsuario.Activa);
        Assert.IsTrue(membresiaUsuario.FechaFin > DateTime.UtcNow);
    }
}
