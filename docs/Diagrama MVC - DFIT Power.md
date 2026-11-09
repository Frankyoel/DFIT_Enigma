---
title: Diagrama de Arquitectura MVC — DFIT Power
date: 2026-11-09
tags:
  - arquitectura
  - mvc
  - dfit-power
  - dotnet10
  - aspnetcore
  - firestore
  - mermaid
sistema: DFIT Power Enigma
framework: ASP.NET Core MVC (.NET 10)
base_de_datos: Google Cloud Firestore (NoSQL)
---

# 🏋️ Diagrama de Arquitectura MVC — DFIT Power

> [!info] Resumen del Sistema
> **DFIT Power** es una aplicación web empresarial orientada a la administración de gimnasios, membresías, asistencias biométricas/QR, rutinas y balances financieros, construida bajo el patrón arquitectónico **MVC (Modelo - Vista - Controlador)** con persistencia distribuida en la nube (**Google Cloud Firestore**).

---

## 🗺️ Mapa Arquitectónico MVC (Mermaid)

```mermaid
flowchart TD
    %% ==========================================
    %% CLIENTE / NAVEGADOR
    %% ==========================================
    subgraph CLIENTE["🌐 Navegador Web / Cliente"]
        User["👤 Usuario / Socio / Admin / Recepcionista / Entrenador"]
    end

    %% ==========================================
    %% CAPA CONTROLADORES (CONTROLLERS)
    %% ==========================================
    subgraph CONTROLLERS["🎮 Capa de Controladores (C - Controllers)"]
        direction TB
        AC["🔐 AuthController\n• Login / Register / Logout\n• RateLimiter & PasswordHasher"]
        ErrC["⚠️ ErrorController\n• Manejo HTTP 401, 403, 404, 500"]
        HC["🏠 HomeController\n• Index & Enrutamiento según Rol\n• Privacy & Error"]
        
        subgraph MODULAR_CTRLS["Módulos Especializados de Negocio"]
            UC["👥 UsuariosController\n• Dashboard de Socios\n• Create / Edit / Profile\n• Evaluaciones Antropométricas"]
            RC["🛎️ RecepcionController\n• Consola de Mostrador\n• Registro Ágil y Cobro Inicial"]
            MC["💳 MembresiasController\n• Catálogo de Planes\n• Mi Membresía & Vigencias"]
            PC["💰 PagosController\n• Registrar Pago en Caja\n• Historial y Reporte Financiero"]
            AsisC["📲 AsistenciaController\n• Check-In QR\n• Pantalla QR & Aforo"]
            EC["🏋️ EntrenamientoController\n• Rutinas y Videos\n• Horarios & Roster de Clases"]
        end
    end

    %% ==========================================
    %% CAPA MODELOS Y SERVICIOS (MODELS)
    %% ==========================================
    subgraph MODELS["🧠 Capa de Modelos y Dominio (M - Models)"]
        direction TB
        subgraph VIEWMODELS["📋 ViewModels (DTOs de Presentación)"]
            VM1["LoginViewModel / RegisterViewModel"]
            VM2["ProfileViewModel / EditUserViewModel"]
            VM3["RecepcionViewModel / ErrorViewModel"]
        end

        subgraph DOMAIN_MODELS["📦 Entidades de Dominio"]
            E_User["Usuario"]
            E_Mem["Membresia / MembresiaUsuario"]
            E_Pago["Pago"]
            E_Asis["Asistencia"]
            E_Rut["Rutina / RutinaUsuario"]
            E_Hor["Horario"]
            E_Eval["EvaluacionFisica"]
        end

        subgraph REGLAS_PATRONES["⚙️ Reglas de Negocio y Patrones GoF"]
            RM["ReglasMembresia (Cálculo de Vigencias)"]
            P_Strat["Strategy: PaymentContext"]
            P_Proxy["Proxy: GymAccessProxy"]
            P_Sing["Singleton: QRTokenManager"]
            P_Obs["Observer: UserRegistrationSubject"]
            P_Build["Builder: UsuarioBuilder"]
        end

        subgraph SERVICES_REPOS["🔌 Capa de Servicios y Repositorios"]
            SRV["Servicios de Negocio\n(Usuario, Pago, Recepcion, etc.)"]
            REPO["Repositorios Firestore\n(Abstracción y Optimización NoSQL)"]
        end
    end

    %% ==========================================
    %% BASE DE DATOS / PERSISTENCIA
    %% ==========================================
    subgraph DB["☁️ Almacén de Persistencia en la Nube"]
        FS[("🔥 Google Cloud Firestore\n• Usuarios\n• Membresias / MembresiasUsuarios\n• Pagos\n• Asistencias\n• Rutinas / RutinasUsuarios\n• Horarios\n• EvaluacionesFisicas")]
    end

    %% ==========================================
    %% CAPA VISTAS (VIEWS)
    %% ==========================================
    subgraph VIEWS["🖥️ Capa de Presentación (V - Razor Views .cshtml)"]
        direction TB
        subgraph V_AUTH["Views/Auth"]
            V_Login["Login.cshtml"]
            V_Reg["Register.cshtml"]
            V_Denied["AccessDenied.cshtml"]
        end

        subgraph V_HOME["Views/Home"]
            V_Dash["Dashboard.cshtml"]
            V_Recep["Recepcion.cshtml"]
            V_Prof["Profile.cshtml"]
            V_Mem["Membresias.cshtml / MiMembresia.cshtml"]
            V_Pago["RegistrarPago.cshtml / ReporteFinanciero.cshtml"]
            V_Asis["Asistencia.cshtml / PantallaQR.cshtml / CheckInResult.cshtml"]
            V_Ent["Rutinas.cshtml / Horarios.cshtml / ClaseRoster.cshtml"]
        end

        subgraph V_SHARED["Views/Shared"]
            V_Layout["_Layout.cshtml (Sidebar reactivo por Rol)"]
            V_Drawer["_AssistanceDrawer.cshtml (Accesibilidad WCAG)"]
            V_Errors["401, 403, 404, 500.cshtml"]
        end
    end

    %% ==========================================
    %% FLUJO Y RELACIONES
    %% ==========================================
    User -->|"1. Petición HTTP (GET / POST / AJAX)"| CONTROLLERS

    AC -->|"Usa"| VM1
    UC -->|"Usa"| VM2
    RC -->|"Usa"| VM3

    CONTROLLERS -->|"2. Invoca Lógica de Negocio"| SERVICES_REPOS
    CONTROLLERS -.->|"Valida Acceso / Tokens"| REGLAS_PATRONES
    SERVICES_REPOS --> DOMAIN_MODELS
    SERVICES_REPOS --> REPO

    REPO <-->|"3. Lectura / Escritura de Documentos"| FS

    AC -->|"4. Renderiza"| V_AUTH
    ErrC -->|"4. Renderiza"| V_Errors
    HC & MODULAR_CTRLS -->|"4. Renderiza con ViewModel"| V_HOME
    V_HOME & V_AUTH -->|"Heredan Maquetado Base"| V_SHARED

    VIEWS -->|"5. Respuesta HTML / CSS / JS"| User
```

---

## 🧩 Desglose de Componentes

### 1. Controladores (`Controllers/`)
> [!note] Responsabilidad
> Atienden las peticiones HTTP, validan la autenticación y autorización basada en Roles (`Admin`, `Recepcionista`, `Entrenador`, `Usuario`), interactúan con los servicios de aplicación y seleccionan la vista adecuada.

* **`AuthController`:** Login, registro de socios, cierre de sesión, rate limiting anti-fuerza bruta e integración con PBKDF2.
* **`ErrorController`:** Renderizado amigable de errores HTTP (401 No Autorizado, 403 Acceso Denegado, 404 No Encontrado, 500 Error de Servidor).
* **`HomeController`:** Enrutamiento inicial del usuario según su rol hacia el módulo correspondiente.
* **Módulos de Negocio Refactorizados:**
  * **`UsuariosController`:** Administración de socios, perfiles y valoraciones antropométricas.
  * **`RecepcionController`:** Consola operativa ágil del recepcionista (monitoreo de vencimientos y ventas).
  * **`MembresiasController`:** Gestión del catálogo de suscripciones y portal del socio.
  * **`PagosController`:** Registro de cobros, transacciones y balance financiero mensual.
  * **`AsistenciaController`:** Registro de check-in mediante códigos QR dinámicos y control de aforo.
  * **`EntrenamientoController`:** Catálogo de rutinas con video de YouTube, horarios semanales y asistencia a clases grupales.

---

### 2. Modelos (`Models/`)
> [!tip] Responsabilidad
> Representan las estructuras de datos, las reglas de negocio puras y la interacción con la persistencia.

* **`ViewModels`:** Modelos especializados para transferir datos hacia y desde las vistas Razor (`ProfileViewModel`, `RecepcionViewModel`, `LoginViewModel`), aplicando validaciones declarativas mediante `DataAnnotations`.
* **Entidades de Dominio:** Estructuras que reflejan los documentos de Cloud Firestore (`Usuario`, `Membresia`, `Pago`, `Asistencia`, `Rutina`, `Horario`, `EvaluacionFisica`).
* **Patrones de Diseño Implementados:**
  * **Builder (`UsuarioBuilder`):** Construcción fluida de entidades `Usuario` con inyección segura de hash.
  * **Strategy (`PaymentContext`):** Estrategias de cálculo y procesamiento de pagos.
  * **Proxy (`GymAccessProxy`):** Control y validación de reglas de acceso previo a conceder ingreso.
  * **Singleton (`QRTokenManager`):** Generador de tokens QR con expiración a 60 segundos.
  * **Observer (`UserRegistrationSubject`):** Notificaciones desacopladas tras el registro de socios.
* **Capa de Repositorios:** Abstracción que elimina consultas directas de Firestore en controladores, resolviendo el problema de consultas $N+1$.

---

### 3. Vistas (`Views/`)
> [!architecture] Responsabilidad
> Renderizado del lado del servidor (*Server-Side Rendering*) mediante plantillas Razor (`.cshtml`), enriquecidas con componentes interactivos y accesibilidad adaptativa.

* **`Views/Auth/`:** Pantallas de acceso al sistema con estilos dedicados (`dfit-login.css`).
* **`Views/Home/`:** Vistas de gestión de datos, tablas reactivas, formularios y tableros de control.
* **`Views/Shared/`:**
  * **`_Layout.cshtml`:** Menú de navegación dinámico adaptado a los permisos del rol activo.
  * **`_AssistanceDrawer.cshtml`:** Panel lateral de ayuda y accesibilidad adaptativo a dispositivos móviles/táctiles y atajos de teclado (`Option + A` en Mac, `Alt + A` en Windows/Linux).
  * **`dfit-sincronizacion-sesion.js`:** Sincronización multi-pestaña mediante `BroadcastChannel`.
