using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dfit.Utils
{
    /// <summary>
    /// Cargador de variables de entorno desde archivos .env para desarrollo local.
    /// No sobreescribe variables existentes en el sistema/contenedor (prioridad para Render/Docker).
    /// </summary>
    public static class EnvLoader
    {
        public static void Load(string? contentRootPath = null)
        {
            var searchPaths = new List<string>();

            if (!string.IsNullOrEmpty(contentRootPath))
            {
                searchPaths.Add(Path.Combine(contentRootPath, ".env"));
                var parent = Directory.GetParent(contentRootPath)?.FullName;
                if (parent != null)
                {
                    searchPaths.Add(Path.Combine(parent, ".env"));
                    var grandParent = Directory.GetParent(parent)?.FullName;
                    if (grandParent != null)
                    {
                        searchPaths.Add(Path.Combine(grandParent, ".env"));
                    }
                }
            }

            searchPaths.Add(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
            searchPaths.Add(Path.Combine(AppContext.BaseDirectory, ".env"));

            foreach (var path in searchPaths.Distinct())
            {
                if (File.Exists(path))
                {
                    LoadFile(path);
                    Console.WriteLine($"[EnvLoader] Variables cargadas exitosamente desde: {path}");
                    return;
                }
            }
        }

        public static void LoadFile(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            var lines = File.ReadAllLines(filePath);
            string? currentKey = null;
            string currentValue = "";
            char quoteChar = '\0';
            bool inMultiLine = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var rawLine = lines[i];

                if (inMultiLine)
                {
                    // Buscamos si la línea cierra las comillas
                    int closingQuoteIndex = rawLine.IndexOf(quoteChar);
                    if (closingQuoteIndex >= 0)
                    {
                        currentValue += "\n" + rawLine.Substring(0, closingQuoteIndex);
                        SetEnvironmentVariableIfEmpty(currentKey!, currentValue);
                        inMultiLine = false;
                        currentKey = null;
                        currentValue = "";
                        quoteChar = '\0';
                    }
                    else
                    {
                        currentValue += "\n" + rawLine;
                    }
                    continue;
                }

                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                int separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;

                var key = line.Substring(0, separatorIndex).Trim();
                var value = line.Substring(separatorIndex + 1).Trim();

                // Detección de cadenas con comillas
                if (value.Length > 0 && (value[0] == '"' || value[0] == '\''))
                {
                    quoteChar = value[0];
                    // Si termina con la misma comilla en la misma línea
                    if (value.Length > 1 && value[value.Length - 1] == quoteChar)
                    {
                        var unquoted = value.Substring(1, value.Length - 2);
                        SetEnvironmentVariableIfEmpty(key, unquoted);
                    }
                    else
                    {
                        // Inicia valor multilínea
                        inMultiLine = true;
                        currentKey = key;
                        currentValue = value.Substring(1);
                    }
                }
                else
                {
                    SetEnvironmentVariableIfEmpty(key, value);
                }
            }
        }

        private static void SetEnvironmentVariableIfEmpty(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                return;

            // Las variables del sistema operativo / contenedor tienen precedencia (por ejemplo en Render)
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
