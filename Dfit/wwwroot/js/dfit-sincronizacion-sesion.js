/**
 * dfit-sincronizacion-sesion.js
 * Módulo reutilizable de sincronización de sesión entre pestañas del navegador para DFIT Power.
 * Permite que:
 * 1. Al cerrar sesión en una pestaña, todas las demás pestañas abiertas se redirijan a /login.
 * 2. Si se inicia sesión con otra cuenta en otra pestaña, las pestañas abiertas con la cuenta
 *    anterior se sincronicen automáticamente con la nueva cuenta sin pantallas de error.
 */
(function (ventana, documento) {
    "use strict";

    // Constantes de configuración
    const NOMBRE_CANAL = "dfit_canal_autenticacion";
    const CLAVE_ESTADO_SESION = "dfit_sesion_activa";
    const CLAVE_EVENTO_SESION = "dfit_evento_sesion";
    const CLAVE_USUARIO_ACTUAL = "dfit_usuario_id";
    const RUTA_LOGIN = "/login";

    // Identificador del usuario autenticado que dibujó esta vista
    const idUsuarioRenderizado = documento.body
        ? (documento.body.getAttribute("data-usuario-id") || "")
        : "";

    // Instancia del canal de comunicación entre pestañas
    const canalComunicacion = typeof BroadcastChannel !== "undefined"
        ? new BroadcastChannel(NOMBRE_CANAL)
        : null;

    // =========================================================================
    // 1. FLUJO PRINCIPAL (Step-Down Rule: Alto nivel primero)
    // =========================================================================

    /**
     * Inicia los observadores de sincronización en la pestaña actual.
     */
    function iniciarSincronizacion() {
        // Marcamos la sesión activa y guardamos el identificador de usuario actual
        marcarSesionComoActiva();
        sincronizarIdentidadActual();

        // 1. Escuchar mensajes del canal moderno (BroadcastChannel)
        configurarEscuchadorCanal();

        // 2. Escuchar cambios de almacenamiento (compatibilidad universal entre pestañas)
        configurarEscuchadorAlmacenamiento();

        // 3. Verificar estado al volver a enfocar la pestaña (en caso de haber estado en segundo plano)
        configurarEscuchadorVisibilidad();
    }

    /**
     * Procesa un evento de autenticación recibido desde otra pestaña.
     * @param {string} accion - Tipo de acción ('cerrar_sesion', 'cambio_cuenta', etc.)
     * @param {string} [usuarioId] - ID del usuario remitente si aplica
     */
    function procesarEventoDeSesion(accion, usuarioId) {
        if (accion === "cerrar_sesion") {
            redirigirAlLogin();
        } else if (accion === "cambio_cuenta" && usuarioId) {
            verificarCambioDeCuenta(usuarioId);
        }
    }

    /**
     * Emite la notificación de cierre de sesión a todas las demás pestañas del navegador.
     * Debe invocarse al hacer clic en el botón 'Cerrar sesión'.
     */
    function notificarCierreDeSesion() {
        marcarSesionComoInactiva();
        emitirMensajeACanal("cerrar_sesion");
        emitirEventoAlmacenamiento("cerrar_sesion");
    }

    // =========================================================================
    // 2. MÉTODOS DE SOPORTE Y DETALLE (Funciones auxiliares)
    // =========================================================================

    function redirigirAlLogin() {
        if (ventana.location.pathname.toLowerCase().includes("/login")) {
            return;
        }
        ventana.location.replace(RUTA_LOGIN);
    }

    function sincronizarIdentidadActual() {
        if (idUsuarioRenderizado) {
            try {
                localStorage.setItem(CLAVE_USUARIO_ACTUAL, idUsuarioRenderizado);
                // Notificar a otras pestañas que una cuenta activa tomó el control
                emitirMensajeACanal("cambio_cuenta", idUsuarioRenderizado);
            } catch (error) {
                // Manejo silencioso
            }
        }
    }

    function verificarCambioDeCuenta(nuevoUsuarioId) {
        // Si otra pestaña inició con una cuenta distinta a la que esta pestaña tiene en pantalla,
        // recargar para sincronizar los datos y claims de la nueva sesión
        if (idUsuarioRenderizado && nuevoUsuarioId && nuevoUsuarioId !== idUsuarioRenderizado) {
            ventana.location.reload();
        }
    }

    function configurarEscuchadorCanal() {
        if (!canalComunicacion) return;

        canalComunicacion.onmessage = function (evento) {
            if (evento && evento.data && evento.data.accion) {
                procesarEventoDeSesion(evento.data.accion, evento.data.usuarioId);
            }
        };
    }

    function configurarEscuchadorAlmacenamiento() {
        ventana.addEventListener("storage", function (evento) {
            if (evento.key === CLAVE_EVENTO_SESION && evento.newValue) {
                try {
                    const datos = JSON.parse(evento.newValue);
                    if (datos && datos.accion) {
                        procesarEventoDeSesion(datos.accion, datos.usuarioId);
                    }
                } catch (error) {
                    // Ignorar contenido no válido
                }
            } else if (evento.key === CLAVE_ESTADO_SESION && evento.newValue === "0") {
                redirigirAlLogin();
            } else if (evento.key === CLAVE_USUARIO_ACTUAL && evento.newValue) {
                verificarCambioDeCuenta(evento.newValue);
            }
        });
    }

    function configurarEscuchadorVisibilidad() {
        documento.addEventListener("visibilitychange", function () {
            if (documento.visibilityState === "visible") {
                const sesionActiva = localStorage.getItem(CLAVE_ESTADO_SESION);
                if (sesionActiva === "0") {
                    redirigirAlLogin();
                    return;
                }

                const usuarioEnStorage = localStorage.getItem(CLAVE_USUARIO_ACTUAL);
                if (usuarioEnStorage) {
                    verificarCambioDeCuenta(usuarioEnStorage);
                }
            }
        });
    }

    function marcarSesionComoActiva() {
        try {
            localStorage.setItem(CLAVE_ESTADO_SESION, "1");
        } catch (error) {
            // Manejo silencioso en navegación privada estricta
        }
    }

    function marcarSesionComoInactiva() {
        try {
            localStorage.setItem(CLAVE_ESTADO_SESION, "0");
            localStorage.removeItem(CLAVE_USUARIO_ACTUAL);
        } catch (error) {
            // Manejo silencioso
        }
    }

    function emitirMensajeACanal(accion, usuarioId) {
        if (canalComunicacion) {
            try {
                canalComunicacion.postMessage({
                    accion: accion,
                    usuarioId: usuarioId || null,
                    timestamp: Date.now()
                });
            } catch (error) {
                // Manejo silencioso
            }
        }
    }

    function emitirEventoAlmacenamiento(accion, usuarioId) {
        try {
            localStorage.setItem(
                CLAVE_EVENTO_SESION,
                JSON.stringify({
                    accion: accion,
                    usuarioId: usuarioId || null,
                    timestamp: Date.now()
                })
            );
        } catch (error) {
            // Manejo silencioso
        }
    }

    // =========================================================================
    // 3. INICIALIZACIÓN Y EXPOSICIÓN GLOBAL
    // =========================================================================

    iniciarSincronizacion();

    // Exponer API limpia en español para ser invocada desde las vistas
    ventana.DfitSesion = {
        notificarCierre: notificarCierreDeSesion
    };

})(window, document);
