// Dfit/Models/Common/MetodosPago.cs
// Formas de pago aceptadas en el módulo de recepción y finanzas de DFIT Power.
// Centraliza las modalidades de cobro para evitar inconsistencias en reportes.

namespace Dfit.Models.Common
{
    // Métodos de pago oficiales soportados por la pasarela y caja física.
    public static class MetodosPago
    {
        // Pago en moneda física en la recepción del gimnasio.
        public const string Efectivo = "Efectivo";

        // Pago digital mediante billetera móvil Yape (Perú).
        public const string Yape = "Yape";

        // Pago digital mediante billetera móvil Plin (Perú).
        public const string Plin = "Plin";

        // Pago mediante tarjeta de débito o crédito a través de POS.
        public const string Tarjeta = "Tarjeta";

        // Pago mediante transferencia bancaria directa a cuentas del gimnasio.
        public const string Transferencia = "Transferencia";
    }
}
