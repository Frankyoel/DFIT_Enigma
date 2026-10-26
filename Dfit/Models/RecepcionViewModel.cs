using System;
using System.Collections.Generic;

namespace Dfit.Models
{
    public class SocioRecepcionItem
    {
        public string Id { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string DNI { get; set; } = string.Empty;
        public string Celular { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;

        public string? MembresiaUsuarioId { get; set; }
        public string? MembresiaNombre { get; set; }
        public DateTime? FechaFin { get; set; }
        public bool TieneMembresiaActiva { get; set; }
        public int DiasRestantes { get; set; }
        public string EstadoVigencia { get; set; } = "Sin Plan"; // "Vigente", "Por Vencer", "Vencida", "Sin Plan"
    }

    public class RecepcionViewModel
    {
        public List<SocioRecepcionItem> Socios { get; set; } = new();
        public List<Membresia> PlanesDisponibles { get; set; } = new();
        public List<Pago> UltimosPagosHoy { get; set; } = new();

        public int TotalSocios { get; set; }
        public int SociosConMembresiaActiva { get; set; }
        public int MembresiasPorVencer { get; set; }
        public int MembresiasVencidas { get; set; }
        public decimal TotalCobradoHoy { get; set; }
        public int PagosRealizadosHoy { get; set; }

        public string? SearchTerm { get; set; }
        public string? FiltroEstado { get; set; }
    }
}
