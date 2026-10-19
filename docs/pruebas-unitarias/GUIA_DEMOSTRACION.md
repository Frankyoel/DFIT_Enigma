# Demostración de pruebas unitarias DFIT

## Entorno

Se conserva .NET 10 y MSTest.Sdk 4.0.2. Diego confirmó usar Visual Studio Community
2026 (18.11), instalado en su equipo. Visual Studio 2022 no es el entorno de
esta entrega. No se migró el backend ni se actualizaron paquetes.

## Ejecutar en Visual Studio

1. Abrir Visual Studio 2026 y la solución `Dfit.sln` de este repositorio.
2. Confirmar en el indicador de Git la rama `diego/pruebas-unitarias`.
3. Seleccionar **Compilar > Compilar solución**.
4. Abrir **Prueba > Explorador de pruebas**.
5. Agrupar por **Proyecto / Clase** o por **Rasgos** para ver Vigencia, Acceso,
   Pagos y Configuracion.
6. Pulsar **Ejecutar todas las pruebas**. La suite contiene 24 casos.
7. Seleccionar un caso y abrir su salida: se registra **Esperado / Obtenido**.
   En Propiedades/Rasgos aparece el identificador `Caso`, por ejemplo CP-VIG-001.
8. Para demostrar una prueba, hacer clic derecho sobre ella y elegir
   **Depurar**. Colocar un punto de interrupción dentro de
   `ReglasMembresia.CalcularFechaFin` o `PermiteAcceso`: la ejecución
   entra en código de producción.

No es necesario iniciar la web, configurar Firebase ni disponer de datos reales.
Las pruebas de configuración crean exclusivamente archivos temporales propios.

## Guion breve para la exposición

1. **Problema original:** seis pruebas calculaban la respuesta en el propio test.
   Una modificación en el backend podía no ser detectada.
2. **Corrección:** el controlador y el proxy utilizan las mismas funciones
   de producción que se invocan desde MSTest.
3. **Ejemplo de vigencia:** CP-VIG-004 usa inicio 31/01/2026 14:30 UTC y
   30 días. El resultado esperado es 02/03/2026 14:30 UTC.
   Se suman días, no meses calendario.
4. **Ejemplo de acceso:** CP-ACC-004 rechaza acceso exactamente al vencimiento.
   CP-ACC-005 lo permite un tick antes. Un tick de DateTime equivale a 100 ns.
5. **Ejemplo de pago:** CP-PAG-005 conserva 100 y CP-PAG-006 convierte 100,01
   en 105,0105, sin añadir redondeo que no existe en la regla actual.
6. **Evidencia:** consultar los TRX y el informe IEEE 829. Explicar que RED
   corresponde a métodos extraídos pendientes, no a una regla de negocio
   inédita ni a alterar resultados esperados para provocar fallos.

## Mostrar RED sin romper la versión final

Abrir `evidencias/red.trx` con Visual Studio (**Archivo > Abrir > Archivo**)
para consultar la ejecución histórica. Mostrar el mensaje
`NotImplementedException` y el método afectado. Después consultar
`green.trx` y `refactor.trx`. Estos archivos son registros históricos;
no equivalen a volver a ejecutar hoy esa versión.

Las instantáneas `*-fuentes.json` conservan el contenido y SHA-256 de los
archivos que se ejecutaron en cada fase. No reemplazar el backend con esas
instantáneas durante la presentación.

## Terminal

Desde la raíz del repositorio:

```powershell
dotnet test .\PruebasUnitarias\PruebasUnitarias.csproj
```

Para conservar un TRX con el ejecutor normal:

```powershell
dotnet test .\PruebasUnitarias\PruebasUnitarias.csproj --logger "trx;LogFileName=presentacion.trx" --results-directory .\TestResults
```

El entorno restringido de ejecución del asistente bloquea la comunicación
entre VSTest y testhost. Eso debe distinguirse de un Assert que falla.
La alternativa utilizada para obtener evidencia es el runner oficial MSTest
en ejecución directa:

```powershell
.\docs\pruebas-unitarias\Ejecutar-Evidencia.ps1 -Fase presentacion
```

El script no reemplaza el framework ni simula los resultados. Activa MTP
solo para esa invocación mediante propiedades de MSBuild. Mantiene
`UseVSTest=true` en el proyecto para el comando normal y Visual Studio.
Rechaza sobrescribir un TRX existente: elegir otro nombre de fase en ese caso.
La opción temporal `NuGetAudit=false` evita la consulta de red en la
ejecución restringida; no declara seguras las dependencias.

## Límites que conviene explicar

- Las pruebas validan reglas aisladas, no el guardado real en Firestore.
- No certifican login, permisos, HTTP, despliegue ni el funcionamiento integral
  del gimnasio.
- Las pruebas de EnvLoader ejercitan archivos temporales y variables del
  proceso: son pruebas de utilidad técnica con E/S local.
- No se añadieron reglas para montos negativos, duración no positiva, hora
  inicial de acceso o redondeo monetario. Requieren una decisión del negocio.
- Los errores de seguridad y el filtrado por año del reporte financiero
  quedan fuera de esta corrección.
