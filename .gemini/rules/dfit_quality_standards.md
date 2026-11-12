---
description: Estándares de calidad de código, métricas estáticas, seguridad y accesibilidad específicos para el proyecto DFIT Power.
globs: "**/*.{cs,cshtml,js}"
alwaysApply: true
---

# Estándares Locales de Calidad — DFIT Power

Estas directrices aplican **única y exclusivamente** al repositorio `DFIT_Enigma`:

## 1. Métricas Estáticas y Formato de Código (`cloc-2.10.exe`)
- **Líneas en blanco:** Mantener entre el **10 % y el 20 %** del total del archivo para preservar legibilidad.
- **Comentarios:** Superar el **20 %** de comentarios explicativos.
- **Sintaxis de documentación:** Usar exclusivamente comentarios de línea con doble barra **`//`** (prohibido el uso de etiquetas XML Doc `/// <summary>`).
- **Profundidad condicional:** Máximo **3 niveles** (objetivo estricto $\le \mathbf{2}$ niveles) usando cláusulas de guarda (*early return* / *continue*) y descomposición funcional en métodos privados con responsabilidad única.

## 2. Respaldos Preventivos
- Generar siempre una copia con extensión `.bak` antes de modificar archivos de configuración o infraestructura.
- Confirmar que los `.bak` permanezcan ignorados por `.gitignore`.

## 3. Seguridad Defensiva (OWASP ZAP)
- Inyectar cabeceras de seguridad globales en `Program.cs` (`Content-Security-Policy`, `X-Frame-Options: SAMEORIGIN`, `X-Content-Type-Options: nosniff`).
- El bloqueo por Rate Limiting debe aislarse por cuenta de usuario previa consulta en Firestore, sin penalizar si el correo no existe ni bloquear IPs públicas de proxies de Render.
- Validar tokens Anti-CSRF en formularios de acción y consulta.

## 4. Accesibilidad e Inclusión (WCAG 2.1 AA)
- No quemar textos rígidos como "Alt + A".
- Detectar dinámicamente dispositivos táctiles/móviles para ocultar atajos de teclado y adaptar etiquetas a `⌥ Option + A` en macOS y `Alt + A` en Windows/Linux.
