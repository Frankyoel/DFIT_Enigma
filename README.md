# 🏋️ DFIT Power — Portal Web Corporativo y Sistema de Gestión

Sistema web integral para la administración de gimnasios, membresías, asistencias QR, rutinas y finanzas, desarrollado con **ASP.NET Core MVC (.NET 10)** y persistencia distribuida en la nube con **Google Cloud Firestore**.

---

## 📐 Metodología de Desarrollo y Estándares de Calidad

Este proyecto implementa una metodología rigurosa orientada a la calidad de software y control estático de código, fundamentada en los modelos de madurez **CMMI (Niveles 2 al 4 / Competencia OD13)** y en la seguridad defensiva según **OWASP ZAP**.

---

### 1. Control de Complejidad y Anidamiento Condicional
- **Límite de Profundidad:** Máximo **3 niveles** de anidamiento condicional (objetivo estándar: $\le \mathbf{2\text{ niveles}}$).
- **Descomposición Funcional:** División de métodos monolíticos en funciones privadas especializadas de responsabilidad única (*Single Responsibility Principle*).
- **Cláusulas de Guarda (*Early Returns* / *Continue*):** Inversión de condiciones para abortar o continuar de inmediato sin anidar bloques `{ ... }`.

---

### 2. Estándares Métricos Cuantitativos (`cloc-2.10.exe`)
Toda refactorización del código backend en C# se mide contra la herramienta oficial `cloc-2.10.exe`:

- **Líneas en Blanco (*Blank / LineWhite*):** Mantener de forma estricta entre el **10 % y el 20 %** del total del archivo, asegurando un espaciado equilibrado entre firmas de métodos, bloques lógicos y retornos sin código apelmazado.
- **Densidad de Documentación (*Comments*):** Alcanzar más del **20 %** de comentarios explicativos.
- **Sintaxis de Documentación:** Se utiliza exclusivamente sintaxis de doble barra **`//`** para comentarios claros y directos por método, **sin emplear etiquetas XML Doc (`/// <summary>`)**.

---

### 3. Política de Respaldo y Protección en Git
- **Copias de Seguridad Preventivas:** Antes de realizar modificaciones estructurales en archivos de configuración o lógica core, se genera obligatoriamente una copia de respaldo con extensión `.bak` (ej. `FirebaseConfig.cs.bak`).
- **Aislamiento en `.gitignore`:** La regla `*.bak` está registrada activamente en el `.gitignore` del proyecto para garantizar que ningún archivo temporal sea rastreado ni subido al repositorio remoto.

---

### 4. Seguridad Defensiva y Control de Acceso (Auditoría OWASP ZAP)
- **Cabeceras HTTP de Seguridad Globales:** Inyección uniforme en el pipeline de `Program.cs`:
  - `Content-Security-Policy (CSP)`: Restringe la carga de scripts y estilos a fuentes legítimas del propio sitio.
  - `X-Frame-Options: SAMEORIGIN` y directiva `frame-ancestors 'self'`: Mitigación total contra ataques de *Clickjacking* en todas las páginas (incluido `/login`).
  - `X-Content-Type-Options: nosniff`: Protección contra interpretación arbitraria de tipos MIME.
- **Rate Limiting Inteligente por Usuario:**
  - El sistema valida previamente en Firestore si el correo existe en la base de datos **sin penalizar ni consumir intentos**.
  - Si la cuenta existe y la contraseña es incorrecta, se descuentan intentos y se bloquea temporalmente de forma aislada **por cuenta de usuario**, evitando bloqueos masivos por IPs compartidas o proxies de Render.
- **Política Estricta de Contraseñas:**
  - Mínimo de 8 caracteres.
  - Combinación obligatoria de mayúsculas, minúsculas, números y símbolos especiales.
  - Validación personalizada contra secuencias triviales (`123456`, `qwerty`, `password`) y prohibición de que la contraseña incluya el DNI o Celular del usuario.
- **Tokens Anti-CSRF:** Integración de tokens de verificación en formularios de acción y consulta (Dashboard).

---

### 5. Accesibilidad e Inclusión Adaptativa (WCAG 2.1 AA)
- **Atajos de Teclado Multiplataforma:** Detección dinámica de hardware y sistema operativo:
  - **Dispositivos Móviles y Táctiles:** Se ocultan automáticamente los textos de atajos de teclado (`Alt + A`) para no confundir al usuario que no posee teclado físico.
  - **macOS:** Se adapta la etiqueta visual mostrando `(⌥ Option + A)`.
  - **Windows / Linux:** Se muestra `(Alt + A)`.
- **Ajustes Visuales:** Soporte integrado en el panel de asistencia para modo de Alto Contraste (blanco/negro), tipografía de fácil lectura y reducción de animaciones para usuarios con sensibilidad visual.

---

### 6. Modelo de Madurez CMMI y Competencia OD13
El ciclo de ingeniería del proyecto avanza progresivamente a través de las etapas descritas en el marco teórico (*"Introducción a la Calidad de Software"*, López, Cabrera y Valencia, Scientia et Technica, 2008):

| Nivel CMMI | Estado en DFIT Power |
|---|---|
| **Nivel 1 (Inicial)** | Etapa base: Métodos monolíticos, anidamiento de 5 niveles y credenciales en texto plano. |
| **Nivel 2 (Gestionado)** | Gestión de versiones, copias `.bak` en `.gitignore`, políticas de contraseñas y Rate Limiting por usuario. |
| **Nivel 3 (Definido)** | Procesos y estándares de codificación formalizados (Clean Code, límite de 2 niveles de anidamiento, comentarios con `//`). |
| **Nivel 4 (Gestionado Cuantitativamente)** | Control estadístico del código con mediciones formales de `cloc-2.10.exe` y auditorías automatizadas de vulnerabilidades con **OWASP ZAP**. |
| **Nivel 5 (Optimización Continua)** | Refactorizaciones preventivas sistemáticas y mejoras de rendimiento (compresión de respuestas y optimización NoSQL). |

---

## 🚀 Despliegue en Cloud (Render / Docker)
- **Contenedorización:** Imagen ligera multi-stage basada en `mcr.microsoft.com/dotnet/aspnet:10.0`.
- **Variables de Entorno:** Soporte de credenciales desglosadas (`FIREBASE_CLIENT_EMAIL` y `FIREBASE_PRIVATE_KEY`) sin necesidad de versionar archivos JSON sensibles en el repositorio.
