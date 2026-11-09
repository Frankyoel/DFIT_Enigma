using System;
using System.IO;
using System.Text.Json;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Dfit.Utils
{
    // Configura y centraliza la autenticación con Firebase Admin SDK y Google Cloud Firestore.
    // Soporta variables de entorno para Render/Cloud y rutas locales para desarrollo.
    public static class FirebaseConfig
    {
        private const string DefaultProjectId = "dfit-gym-cd550";

        // Orquesta el flujo de configuración general y el registro en el contenedor DI.
        public static void Configure(WebApplicationBuilder builder)
        {
            string? projectId = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID");

            // Intento 1: Carga desde variables individuales de entorno (ideal para Render)
            var credential = CargarCredencialDesdeVariables(ref projectId);

            // Intento 2: Carga desde archivos locales JSON si no se obtuvieron variables
            if (credential == null)
            {
                credential = CargarCredencialDesdeArchivosLocales(builder.Environment.ContentRootPath, ref projectId);
            }

            // Asignar Project ID predeterminado como fallback
            projectId = !string.IsNullOrWhiteSpace(projectId) ? projectId : DefaultProjectId;

            // Validación: abortar si no se pudo construir ninguna credencial válida
            if (credential == null)
            {
                throw new InvalidOperationException(
                    "❌ ERROR DE CONFIGURACIÓN FIREBASE:\n" +
                    "No se encontraron credenciales válidas de Firebase.\n" +
                    "Por favor configure una de las siguientes opciones en .env o en el panel de Render:\n" +
                    "  - OPCIÓN A: FIREBASE_CLIENT_EMAIL y FIREBASE_PRIVATE_KEY\n" +
                    "  - OPCIÓN B: FIREBASE_KEY_PATH indicando la ruta al archivo json local (ej: ./Dfit/firebase-key.json)\n" +
                    "Consulte el archivo .env.example para más detalles.");
            }

            InicializarServicios(credential, projectId, builder);
        }

        // Carga credenciales desde variables individuales FIREBASE_CLIENT_EMAIL y FIREBASE_PRIVATE_KEY.
        // Nivel de profundidad: 1 (cláusula de guarda inicial).
        private static GoogleCredential? CargarCredencialDesdeVariables(ref string? projectId)
        {
            var clientEmail = Environment.GetEnvironmentVariable("FIREBASE_CLIENT_EMAIL");
            var privateKey = Environment.GetEnvironmentVariable("FIREBASE_PRIVATE_KEY");

            // Cláusula de guarda: si falta alguna variable requerida, salir inmediatamente
            if (string.IsNullOrWhiteSpace(clientEmail) || string.IsNullOrWhiteSpace(privateKey))
            {
                return null;
            }

            try
            {
                var normalizedKey = privateKey.Replace("\\n", "\n");
                var effectiveProjectId = !string.IsNullOrWhiteSpace(projectId) ? projectId : DefaultProjectId;

                var serviceAccountObj = new
                {
                    type = "service_account",
                    project_id = effectiveProjectId,
                    private_key = normalizedKey,
                    client_email = clientEmail
                };

                var generatedJson = JsonSerializer.Serialize(serviceAccountObj);
#pragma warning disable CS0618
                var credential = GoogleCredential.FromJson(generatedJson);
#pragma warning restore CS0618
                projectId = effectiveProjectId;

                Console.WriteLine("[Firebase] Credenciales cargadas exitosamente (OPCIÓN A: CLIENT_EMAIL y PRIVATE_KEY).");
                return credential;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Firebase] Advertencia: Error al crear credenciales desde variables individuales: {ex.Message}");
                return null;
            }
        }

        // Itera sobre las rutas candidatas para cargar credenciales desde un archivo JSON.
        // Nivel de profundidad máximo: 2 (foreach + if de guarda).
        private static GoogleCredential? CargarCredencialDesdeArchivosLocales(string contentRootPath, ref string? projectId)
        {
            var candidatePaths = new[]
            {
                Environment.GetEnvironmentVariable("FIREBASE_KEY_PATH"),
                Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS"),
                Path.Combine(contentRootPath, "firebase-key.json")
            };

            foreach (var path in candidatePaths)
            {
                // Cláusula de guarda: omitir rutas no configuradas o archivos inexistentes
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    continue;
                }

                var credential = IntentarCargarDesdeArchivo(path, ref projectId);
                if (credential != null)
                {
                    return credential;
                }
            }

            return null;
        }

        // Intenta instanciar la credencial desde una ruta de archivo específica.
        // Nivel de profundidad: 1 (bloque try / catch).
        private static GoogleCredential? IntentarCargarDesdeArchivo(string path, ref string? projectId)
        {
            try
            {
#pragma warning disable CS0618
                var credential = GoogleCredential.FromFile(path);
#pragma warning restore CS0618
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);

                SincronizarProjectIdDesdeArchivo(path, ref projectId);

                Console.WriteLine($"[Firebase] Credenciales cargadas exitosamente (OPCIÓN B: Archivo {path}).");
                return credential;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Firebase] Advertencia al leer archivo {path}: {ex.Message}");
                return null;
            }
        }

        // Lee el JSON de la cuenta de servicio y actualiza el projectId con retorno temprano.
        // Nivel de profundidad: 1 (retorno anticipado).
        private static void SincronizarProjectIdDesdeArchivo(string path, ref string? projectId)
        {
            var json = File.ReadAllText(path);
            var certProjectId = ExtractProjectId(json);

            // Cláusula de guarda: salir si no contiene project_id
            if (string.IsNullOrEmpty(certProjectId))
            {
                return;
            }

            if (!string.IsNullOrEmpty(projectId) && projectId != certProjectId)
            {
                Console.WriteLine($"[Firebase] Sincronización automática: El archivo '{path}' pertenece a '{certProjectId}' (reemplaza '{projectId}').");
            }

            projectId = certProjectId;
        }

        // Inicializa FirebaseApp y FirestoreDb registrándolo en el contenedor de dependencias.
        private static void InicializarServicios(GoogleCredential credential, string projectId, WebApplicationBuilder builder)
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = credential,
                    ProjectId = projectId
                });
            }

            var firestoreDb = new FirestoreDbBuilder
            {
                ProjectId = projectId,
                Credential = credential
            }.Build();

            builder.Services.AddSingleton(firestoreDb);
            Console.WriteLine($"[Firebase] FirestoreDb inicializado correctamente para el proyecto '{projectId}'.");
        }

        // Extrae el campo 'project_id' del documento JSON.
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
                // Ignorar errores al parsear el JSON
            }

            return null;
        }
    }
}
