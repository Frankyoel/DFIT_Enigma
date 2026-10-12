using System;
using System.IO;
using System.Text;
using System.Text.Json;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Dfit.Utils
{
    /// <summary>
    /// Configura la autenticación y conexión con Firebase Admin SDK y Google Cloud Firestore
    /// a partir de variables de entorno (.env / Render) o archivos locales.
    /// </summary>
    public static class FirebaseConfig
    {
        public static void Configure(WebApplicationBuilder builder)
        {
            GoogleCredential? credential = null;
            string? projectId = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID");

            // =========================================================================
            // OPCIÓN A: Variables individuales desglosadas (Ideal para Render / Cloud)
            // Requiere FIREBASE_CLIENT_EMAIL y FIREBASE_PRIVATE_KEY
            // =========================================================================
            var clientEmail = Environment.GetEnvironmentVariable("FIREBASE_CLIENT_EMAIL");
            var privateKey = Environment.GetEnvironmentVariable("FIREBASE_PRIVATE_KEY");

            if (!string.IsNullOrWhiteSpace(clientEmail) && !string.IsNullOrWhiteSpace(privateKey))
            {
                try
                {
                    // Asegurar saltos de línea correctos en la clave RSA
                    var normalizedKey = privateKey.Replace("\\n", "\n");
                    var effectiveProjectId = !string.IsNullOrWhiteSpace(projectId) ? projectId : "dfit-gym-cd550";

                    var serviceAccountObj = new
                    {
                        type = "service_account",
                        project_id = effectiveProjectId,
                        private_key = normalizedKey,
                        client_email = clientEmail
                    };

                    var generatedJson = JsonSerializer.Serialize(serviceAccountObj);
                    credential = GoogleCredential.FromJson(generatedJson);
                    projectId = effectiveProjectId;
                    Console.WriteLine("[Firebase] Credenciales cargadas exitosamente (OPCIÓN A: CLIENT_EMAIL y PRIVATE_KEY).");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Firebase] Advertencia: Error al crear credenciales desde variables individuales (OPCIÓN A): {ex.Message}");
                }
            }

            // =========================================================================
            // OPCIÓN B: Ruta a archivo local (Ideal para desarrollo local)
            // Lee FIREBASE_KEY_PATH o busca firebase-key.json en el proyecto
            // =========================================================================
            if (credential == null)
            {
                var candidatePaths = new[]
                {
                    Environment.GetEnvironmentVariable("FIREBASE_KEY_PATH"),
                    Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS"),
                    Path.Combine(builder.Environment.ContentRootPath, "firebase-key.json")
                };

                foreach (var path in candidatePaths)
                {
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        try
                        {
                            credential = GoogleCredential.FromFile(path);
                            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);

                            var json = File.ReadAllText(path);
                            var certProjectId = ExtractProjectId(json);
                            if (!string.IsNullOrEmpty(certProjectId))
                            {
                                if (!string.IsNullOrEmpty(projectId) && projectId != certProjectId)
                                {
                                    Console.WriteLine($"[Firebase] Sincronización automática: El archivo '{path}' pertenece al proyecto '{certProjectId}' (se reemplaza '{projectId}').");
                                }
                                projectId = certProjectId;
                            }
                            Console.WriteLine($"[Firebase] Credenciales cargadas exitosamente (OPCIÓN B: Archivo {path}).");
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Firebase] Advertencia al leer archivo {path}: {ex.Message}");
                        }
                    }
                }
            }

            // Fallback de project ID predeterminado si no se determinó
            projectId = !string.IsNullOrWhiteSpace(projectId) ? projectId : "dfit-gym-cd550";

            // Validar que se haya obtenido una credencial válida
            if (credential == null)
            {
                throw new InvalidOperationException(
                    "❌ ERROR DE CONFIGURACIÓN FIREBASE:\n" +
                    "No se encontraron credenciales válidas de Firebase.\n" +
                    "Por favor, configure una de las siguientes opciones en su archivo .env o en el panel de Render:\n" +
                    "  - OPCIÓN A: FIREBASE_CLIENT_EMAIL y FIREBASE_PRIVATE_KEY\n" +
                    "  - OPCIÓN B: FIREBASE_KEY_PATH indicando la ruta al archivo json local (ej: ./Dfit/firebase-key.json)\n" +
                    "Consulte el archivo .env.example para más detalles.");
            }

            // 5. Inicializar Firebase Admin SDK (si no está inicializado)
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = credential,
                    ProjectId = projectId
                });
            }

            // 6. Inicializar FirestoreDb con credenciales explícitas e inyectarlo en DI
            var firestoreDb = new FirestoreDbBuilder
            {
                ProjectId = projectId,
                Credential = credential
            }.Build();

            builder.Services.AddSingleton(firestoreDb);
            Console.WriteLine($"[Firebase] FirestoreDb inicializado correctamente para el proyecto '{projectId}'.");
        }

        private static string? ExtractProjectId(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("project_id", out var elem))
                {
                    return elem.GetString();
                }
            }
            catch
            {
                // Ignorar errores de análisis JSON
            }
            return null;
        }
    }
}
