# Informe de corrección de pruebas unitarias DFIT

Fecha: 18 de octubre de 2026. Rama: `diego/pruebas-unitarias`.

## Resultado

**24 pruebas ejecutadas, 24 aprobadas, 0 fallidas y 0 omitidas** mediante
el runner oficial de MSTest en ejecución directa. Se conservaron la versión
MSTest.Sdk 4.0.2, .NET 10 y la configuración UseVSTest del proyecto.
Diego eligió Visual Studio 2026, instalado en su equipo, en lugar de migrar
el proyecto para Visual Studio 2022.

El comando solicitado:

```powershell
dotnet test .\PruebasUnitarias\PruebasUnitarias.csproj
```

compiló ambos proyectos, pero terminó con código 1 y **serie de pruebas anulada**:
VSTest no pudo comunicarse con testhost en el entorno restringido.
En este intento se estableció VSTEST_CONNECTION_TIMEOUT=30.
No se contabiliza como una ejecución con 24 aprobadas ni como 24 pruebas fallidas.
La salida está en [dotnet-test-salida.txt](evidencias/dotnet-test-salida.txt)
y el comando, fechas y código de salida en
[dotnet-test-ejecucion.json](evidencias/dotnet-test-ejecucion.json).

La ejecución directa evita esa comunicación entre procesos, sin sustituir
MSTest ni inventar resultados. El script y los TRX permiten reproducir y
examinar los resultados.

## Estado antes de la interrupción y al retomar

- Se había analizado el backend y las diez pruebas originales.
- Se comprobó la instalación de Visual Studio Community 2026 18.11.
- Se investigó el bloqueo del ejecutor y se consiguió ejecutar directamente
  las diez pruebas originales: 10 aprobadas, 0 fallidas.
- Se conservó [baseline.trx](evidencias/baseline.trx).
- Las correcciones de código estaban preparadas, pero todavía no escritas.
- Al retomar se ejecutaron git branch, git status y git diff: la rama era
  diego/pruebas-unitarias; no había modificaciones de archivos versionados.
  Solo docs/ figuraba como nuevo, con baseline.trx. No se descartó esa evidencia.

## Trabajo terminado

1. Extraer CalcularFechaFin y PermiteAcceso a ReglasMembresia, dentro de Dfit.
2. Conectar las reglas a los flujos reales de membresía y asistencia.
3. Reemplazar los seis tests que simulaban reglas por llamadas a esos métodos.
4. Mantener las dos comprobaciones reales de tarjeta y efectivo, corrigiendo
   sus nombres para describir lo que realmente prueban.
5. Extraer la selección por monto a PaymentContext.ExecutePaymentForAmount
   y utilizarla desde RegistrarPago; añadir pruebas del umbral y de reutilización.
6. Mejorar limpieza y aislamiento de EnvLoaderTests; añadir tres casos.
7. Registrar valores reales con TestContext y un identificador único por caso.
8. Conservar evidencia RED, GREEN y REFACTOR y documentar los 24 casos.

## Evidencia TDD

| Fase | Ejecutadas | Aprobadas | Fallidas | Interpretación |
| --- | ---: | ---: | ---: | --- |
| BASELINE | 10 | 10 | 0 | Pruebas originales, incluidas seis simulaciones |
| RED | 24 | 8 | 16 | Nuevos métodos extraídos pendientes: NotImplementedException |
| GREEN | 24 | 24 | 0 | Implementación mínima y conexión con los flujos reales |
| REFACTOR | 24 | 24 | 0 | Regla compartida también en Dashboard/MiMembresia; simplificación de PaymentContext |

La evidencia se limita a esta **extracción guiada por pruebas**. Las reglas
de negocio ya existían dentro del controlador y el proxy; no se afirma que
el proyecto original se haya construido con TDD desde cero.

En RED se añadieron las pruebas y las firmas de los métodos con
NotImplementedException, todavía sin reemplazar los flujos existentes.
Los 16 fallos registrados corresponden exactamente a esos métodos pendientes.
Las ocho pruebas de comportamientos ya implementados siguieron pasando.
Los resultados esperados de los tests se mantuvieron en GREEN y REFACTOR.

Para cada fase se conservan:

- `fase.trx`: informe original emitido por MSTest.
- `fase-salida.txt`: salida de compilación y pruebas.
- `fase-ejecucion.json`: comando real, fechas UTC y código de salida.
- `fase-fuentes.json`: contenido y SHA-256 de los 12 archivos de código/configuración relevantes.

La fuente actual se comparó con la instantánea REFACTOR: 12 archivos
verificados y 0 diferencias. Esto relaciona el resultado con el código entregado.

## Archivos de código modificados o nuevos

| Archivo | Cambio |
| --- | --- |
| Dfit/Models/ReglasMembresia.cs | Nuevo: cálculo de fecha y regla de acceso |
| Dfit/Controllers/HomeController.cs | Utiliza las reglas compartidas y la selección de pago extraída |
| Dfit/Models/Patterns/Proxy/GymAccessProxy.cs | Utiliza PermiteAcceso |
| Dfit/Models/Patterns/Strategy/PaymentStrategy.cs | Selección por monto, umbral con nombre, estrategia nullable y predeterminada |
| PruebasUnitarias/CalculoVigenciaTests.cs | 6 casos reales de fechas |
| PruebasUnitarias/MembresiaAccesoTests.cs | 6 casos reales de acceso |
| PruebasUnitarias/Test1.cs | 7 casos de pagos |
| PruebasUnitarias/EnvLoaderTests.cs | 5 casos, limpieza garantizada y ejecución sin paralelismo |
| PruebasUnitarias/PruebaDocumentada.cs | Nuevo: registro de esperado/obtenido y Assert.AreEqual |

No se modificaron los archivos csproj, Program.cs, la solución ni las
dependencias. No se hicieron commits, push, merge ni cambios de rama.

[cambios.patch](cambios.patch) contiene el diff de los siete archivos
preexistentes modificados. Los dos archivos nuevos se encuentran en las
rutas indicadas y también están incluidos en las instantáneas de fuentes.
La documentación y las evidencias son archivos nuevos bajo docs/pruebas-unitarias.

## Métodos reales comprobados

| Grupo / casos | Método |
| --- | --- |
| CP-VIG-001 a 006 | ReglasMembresia.CalcularFechaFin |
| CP-ACC-001 a 006 | ReglasMembresia.PermiteAcceso |
| CP-PAG-001 | PaymentContext.ExecutePayment y CardStrategy.CalculateFinalAmount |
| CP-PAG-002 y 003 | PaymentContext.ExecutePayment y CashStrategy.CalculateFinalAmount |
| CP-PAG-004 a 007 | PaymentContext.ExecutePaymentForAmount, ExecutePayment y estrategia seleccionada |
| CP-ENV-001 a 005 | EnvLoader.LoadFile |

La ficha individual enumera entradas, objetivo y salida de cada caso:
[IEEE829-CASOS.md](IEEE829-CASOS.md).

## Reglas preservadas y ajuste explicado

- Vigencia: sumar DuracionDias; no convertir los planes en meses calendario.
- Acceso: Activa y FechaFin estrictamente posterior al instante evaluado.
- Pagos: monto mayor a 100 selecciona tarjeta con 5 %; los demás, efectivo.
- Sin redondeo nuevo y sin nuevas validaciones de negativos o duración.
- EnvLoader: misma implementación de producción.

Antes de cambiar la asignación se explicó que el controlador leía UtcNow dos
veces. Ahora captura una fecha inicial única y calcula el fin a partir de ella.
Esto elimina el desfase mínimo del reloj sin cambiar la duración del plan.

## Aislamiento y límites

Ninguna prueba inicia el sitio ni conecta con una base real. Los métodos
de reglas no usan Firestore. EnvLoaderTests crea únicamente archivos y claves
de prueba; la clase se marca DoNotParallelize y TestCleanup restaura las
variables y elimina el archivo aunque un Assert falle.

No se valida aquí la persistencia, la autenticación, los permisos, HTTP,
Render ni el funcionamiento integral. Los cálculos extraídos están conectados
al backend, pero las acciones de los controladores no se ejecutan con Firestore.
No se midió cobertura porcentual.

## Advertencias y pendientes

- Ejecutar la suite en el Explorador de pruebas del Visual Studio del usuario
  para confirmar también ese canal; la prueba automatizada aquí utilizó el runner directo.
- Vincular los casos con los IDs de historias de usuario del equipo: no fueron proporcionados.
- Se mantienen advertencias preexistentes de nulabilidad y API obsoletas.
  El intento normal mostró NU1900 por falta de acceso a NuGet para auditar.
  No se actualizaron paquetes ni se declara completada una auditoría de seguridad.
- No se corrigieron el reporte mensual que ignora el año ni los problemas de
  sesión/autorización detectados en el análisis previo: quedan fuera del alcance.
- No quedan métodos NotImplementedException ni pruebas pendientes de implementación
  dentro de los cuatro grupos solicitados.

## Demostración

Abrir Dfit.sln en Visual Studio 2026, compilar, abrir **Prueba > Explorador de
pruebas** y ejecutar todas. Para exponer RED sin alterar el código final,
abrir red.trx como resultado histórico y compararlo con green.trx y refactor.trx.

Guion y comandos: [GUIA_DEMOSTRACION.md](GUIA_DEMOSTRACION.md).

Fuentes del enfoque: clase S07, páginas 7, 20–21 y 28–30.
La documentación de Microsoft sobre MSTest.Sdk explica los modos de ejecución:
https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-sdk
