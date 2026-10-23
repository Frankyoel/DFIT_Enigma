using System;
using System.Net;
using Microsoft.AspNetCore.Http;

namespace Dfit.Utils
{
    /// <summary>
    /// Utilidad para la resolución y validación de direcciones IP en entornos con proxies inversos (Render, Cloudflare, Nginx).
    /// </summary>
    public static class IpHelper
    {
        public const string IpLoopbackPorDefecto = "127.0.0.1";

        /// <summary>
        /// Obtiene la dirección IP real del cliente evaluando las cabeceras emitidas por proxies de confianza.
        /// </summary>
        public static string ObtenerIpCliente(HttpContext? context)
        {
            if (context == null)
            {
                return IpLoopbackPorDefecto;
            }

            var headers = context.Request.Headers;

            // 1. Cloudflare header (Render utiliza la red de Cloudflare Edge)
            if (headers.TryGetValue("CF-Connecting-IP", out var cfIp) && !string.IsNullOrWhiteSpace(cfIp))
            {
                var limpia = LimpiarIp(cfIp.ToString());
                if (EsIpValida(limpia))
                {
                    return limpia;
                }
            }

            // 2. True-Client-IP (Cloudflare Enterprise / Akamai)
            if (headers.TryGetValue("True-Client-IP", out var trueIp) && !string.IsNullOrWhiteSpace(trueIp))
            {
                var limpia = LimpiarIp(trueIp.ToString());
                if (EsIpValida(limpia))
                {
                    return limpia;
                }
            }

            // 3. X-Real-IP (Nginx / Traefik / Render)
            if (headers.TryGetValue("X-Real-IP", out var realIp) && !string.IsNullOrWhiteSpace(realIp))
            {
                var limpia = LimpiarIp(realIp.ToString());
                if (EsIpValida(limpia))
                {
                    return limpia;
                }
            }

            // 4. X-Forwarded-For (Cadena: cliente, proxy1, proxy2...)
            if (headers.TryGetValue("X-Forwarded-For", out var fwdIp) && !string.IsNullOrWhiteSpace(fwdIp))
            {
                var ips = fwdIp.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                
                // Priorizar la primera IP pública de la lista (de izquierda a derecha representa al cliente original)
                foreach (var candidata in ips)
                {
                    var limpia = LimpiarIp(candidata);
                    if (EsIpPublica(limpia))
                    {
                        return limpia;
                    }
                }

                // Si no se detectó como pública estricta pero es válida, tomar la primera de la cadena
                if (ips.Length > 0)
                {
                    var primera = LimpiarIp(ips[0]);
                    if (EsIpValida(primera))
                    {
                        return primera;
                    }
                }
            }

            // 5. RemoteIpAddress directo de la conexión Kestrel (propagado por UseForwardedHeaders)
            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(remoteIp))
            {
                var limpia = LimpiarIp(remoteIp);
                if (EsIpValida(limpia))
                {
                    return limpia;
                }
            }

            return IpLoopbackPorDefecto;
        }

        /// <summary>
        /// Determina si la dirección IP corresponde a un loopback, localhost o red privada interna.
        /// </summary>
        public static bool EsIpLoopbackOPrivada(string? ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                return true;
            }

            var ipTrim = ip.Trim();
            if (ipTrim == "127.0.0.1" || ipTrim == "::1" || ipTrim.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (IPAddress.TryParse(ipTrim, out var address))
            {
                if (IPAddress.IsLoopback(address))
                {
                    return true;
                }

                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    var bytes = address.GetAddressBytes();
                    // 10.0.0.0/8 (Red interna típica de Docker/Render)
                    if (bytes[0] == 10) return true;
                    // 172.16.0.0/12 (Redes privadas)
                    if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                    // 192.168.0.0/16
                    if (bytes[0] == 192 && bytes[1] == 168) return true;
                    // 169.254.0.0/16 (Link-local)
                    if (bytes[0] == 169 && bytes[1] == 254) return true;
                }
                else if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool EsIpPublica(string? ip)
        {
            return EsIpValida(ip) && !EsIpLoopbackOPrivada(ip);
        }

        public static bool EsIpValida(string? ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                return false;
            }

            return IPAddress.TryParse(ip.Trim(), out _);
        }

        private static string LimpiarIp(string ipRaw)
        {
            if (string.IsNullOrWhiteSpace(ipRaw))
            {
                return string.Empty;
            }

            var ip = ipRaw.Trim();

            // Despojar prefijo de mapeo IPv6 a IPv4 si existe (ej. ::ffff:192.168.1.1)
            if (ip.StartsWith("::ffff:", StringComparison.OrdinalIgnoreCase))
            {
                ip = ip.Substring(7);
            }

            // Quitar puerto si viene formato IPv4:puerto (ej: 190.235.10.5:54321)
            if (ip.Contains(':') && !ip.Contains('['))
            {
                var partes = ip.Split(':');
                if (partes.Length == 2 && IPAddress.TryParse(partes[0], out _))
                {
                    return partes[0];
                }
            }

            // Quitar formato IPv6 con corchetes [::1]:port
            if (ip.StartsWith('[') && ip.Contains(']'))
            {
                var fin = ip.IndexOf(']');
                return ip.Substring(1, fin - 1);
            }

            return ip;
        }
    }
}
