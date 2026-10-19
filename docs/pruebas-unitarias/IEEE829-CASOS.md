# Especificación y resultados de casos de prueba — DFIT

Fecha: 18/10/2026. Rama de trabajo: `diego/pruebas-unitarias`.
Formato adaptado a la ficha IEEE 829 de S07, páginas 28 y 30. Este documento
no constituye una certificación de conformidad con la norma completa.

## Alcance y trazabilidad

Se comprueban reglas de negocio extraídas a métodos reales usados por el backend,
y la utilidad EnvLoader. No se inicia la aplicación web ni se conecta a Firestore.
Las fechas son fijas; las respuestas esperadas no se calculan repitiendo la regla.

No se proporcionó un catálogo formal de historias de usuario. Por ello se identifica
la funcionalidad asociada a cada caso y se deja **HU pendiente de vincular**,
sin inventar códigos de historias existentes.

`PruebaDocumentada.Comprobar` registra los valores y llama a `Assert.AreEqual`.
Los campos **Resultado obtenido** proceden de la salida TestContext de
[evidencias/refactor.trx](evidencias/refactor.trx), no de resultados supuestos.
Las diferencias de formato decimal (210 y 210.00) representan el mismo valor.

## Ejecuciones reales

| Fase | Inicio UTC | Ejecutadas | Aprobadas | Fallidas | Evidencia |
| --- | --- | ---: | ---: | ---: | --- |
| BASELINE | 2026-10-18T22:16:44.6885925Z | 10 | 10 | 0 | [baseline.trx](evidencias/baseline.trx) |
| RED | 2026-10-18T22:30:01.557404Z | 24 | 8 | 16 | [red.trx](evidencias/red.trx) |
| GREEN | 2026-10-18T22:30:57.2005147Z | 24 | 24 | 0 | [green.trx](evidencias/green.trx) |
| REFACTOR | 2026-10-18T22:31:52.4959812Z | 24 | 24 | 0 | [refactor.trx](evidencias/refactor.trx) |

Estas cifras corresponden al **runner oficial MSTest ejecutado directamente**.
El resultado del comando normal `dotnet test` se documenta por separado en
el informe; un bloqueo del ejecutor no cuenta como un Assert fallido.

## Métodos de producción cubiertos

| Casos | Método real | Uso en el backend |
| --- | --- | --- |
| CP-VIG-001 a 006 | ReglasMembresia.CalcularFechaFin | HomeController.AssignMembresia |
| CP-ACC-001 a 006 | ReglasMembresia.PermiteAcceso | HomeController.RegistrarAsistencia, Dashboard, MiMembresia; GymAccessProxy.RegistrarAsistencia |
| CP-PAG-001 a 003 | PaymentContext.ExecutePayment y estrategias | Cálculo utilizado por ExecutePaymentForAmount |
| CP-PAG-004 a 007 | PaymentContext.ExecutePaymentForAmount | HomeController.RegistrarPago |
| CP-ENV-001 a 005 | EnvLoader.LoadFile | Método llamado por EnvLoader.Load; no se prueba aquí la búsqueda de rutas |

Se verifica la regla aislada. Las llamadas de integración con Firestore y el
guardado de membresías, pagos y asistencias no se ejecutan en estos casos.

## Condiciones comunes

- .NET 10, MSTest.Sdk 4.0.2; Visual Studio 2026 confirmado por Diego.
- Compilar Dfit y PruebasUnitarias desde esta rama.
- Los casos de negocio no requieren credenciales ni datos de usuarios.
- Los casos ENV usan claves aleatorias exclusivas del proceso y una ruta temporal.
  TestCleanup restaura las variables y elimina el archivo aun si falla un Assert.
  La clase ENV no se ejecuta en paralelo.
- Los resultados de cada ficha corresponden a la ejecución REFACTOR indicada arriba.

## Índice de casos

| ID | Objetivo | Resultado final |
| --- | --- | --- |
| CP-VIG-001 | Plan mensual | PASS |
| CP-VIG-002 | Plan anual de 365 días | PASS |
| CP-VIG-003 | Plan trimestral | PASS |
| CP-VIG-004 | Sumar días, conservando hora y UTC | PASS |
| CP-VIG-005 | Incluir el 29 de febrero | PASS |
| CP-VIG-006 | Cruzar de año manteniendo hora | PASS |
| CP-ACC-001 | Vigente y activa | PASS |
| CP-ACC-002 | Vencida | PASS |
| CP-ACC-003 | Inactiva con fecha futura | PASS |
| CP-ACC-004 | Vencimiento exacto | PASS |
| CP-ACC-005 | Un tick antes de vencer | PASS |
| CP-ACC-006 | Un tick después de vencer | PASS |
| CP-PAG-001 | Recargo de tarjeta | PASS |
| CP-PAG-002 | Efectivo sin recargo | PASS |
| CP-PAG-003 | Estrategia predeterminada | PASS |
| CP-PAG-004 | Selección por debajo del umbral | PASS |
| CP-PAG-005 | Selección exactamente en el umbral | PASS |
| CP-PAG-006 | Selección por encima del umbral; conservar precisión actual | PASS |
| CP-PAG-007 | Evitar conservar tarjeta para el siguiente monto bajo | PASS |
| CP-ENV-001 | Interpretar valores y comillas | PASS |
| CP-ENV-002 | Respetar prioridad del entorno | PASS |
| CP-ENV-003 | Conservar valor multilínea | PASS |
| CP-ENV-004 | Archivo ausente sin efectos | PASS |
| CP-ENV-005 | Ignorar líneas inválidas y conservar '=' interno | PASS |

## CP-VIG-001 — Plan mensual

| Campo | Especificación |
| --- | --- |
| Identificador | CP-VIG-001 |
| Funcionalidad / HU | Vigencia; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Plan mensual |
| Nombre MSTest | `CalcularFechaFin_MembresiaMensual_SumaExactamente30Dias` |
| Método real | `ReglasMembresia.CalcularFechaFin` |
| Prioridad | Alta |
| Precondiciones | Condiciones comunes; fecha inicial explícita en UTC |
| Datos de entrada | Inicio UTC (2026, 1, 1, 0, 0, 0); duración=30 días |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `CalcularFechaFin_MembresiaMensual_SumaExactamente30Dias`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. |
| Resultado esperado | 2026-01-31T00:00:00Z |
| Resultado obtenido | 2026-01-31T00:00:00.0000000Z |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | FAIL: NotImplementedException en el método extraído pendiente |

## CP-VIG-002 — Plan anual de 365 días

| Campo | Especificación |
| --- | --- |
| Identificador | CP-VIG-002 |
| Funcionalidad / HU | Vigencia; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Plan anual de 365 días |
| Nombre MSTest | `CalcularFechaFin_MembresiaAnual_SumaExactamente365Dias` |
| Método real | `ReglasMembresia.CalcularFechaFin` |
| Prioridad | Alta |
| Precondiciones | Condiciones comunes; fecha inicial explícita en UTC |
| Datos de entrada | Inicio UTC (2026, 1, 1, 0, 0, 0); duración=365 días |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `CalcularFechaFin_MembresiaAnual_SumaExactamente365Dias`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. |
| Resultado esperado | 2027-01-01T00:00:00Z |
| Resultado obtenido | 2027-01-01T00:00:00.0000000Z |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | FAIL: NotImplementedException en el método extraído pendiente |

## CP-VIG-003 — Plan trimestral

| Campo | Especificación |
| --- | --- |
| Identificador | CP-VIG-003 |
| Funcionalidad / HU | Vigencia; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Plan trimestral |
| Nombre MSTest | `CalcularFechaFin_MembresiaTrimestral_SumaExactamente90Dias` |
| Método real | `ReglasMembresia.CalcularFechaFin` |
| Prioridad | Alta |
| Precondiciones | Condiciones comunes; fecha inicial explícita en UTC |
| Datos de entrada | Inicio UTC (2026, 1, 1, 0, 0, 0); duración=90 días |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `CalcularFechaFin_MembresiaTrimestral_SumaExactamente90Dias`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. |
| Resultado esperado | 2026-04-01T00:00:00Z |
| Resultado obtenido | 2026-04-01T00:00:00.0000000Z |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | FAIL: NotImplementedException en el método extraído pendiente |

## CP-VIG-004 — Sumar días, conservando hora y UTC

| Campo | Especificación |
| --- | --- |
| Identificador | CP-VIG-004 |
| Funcionalidad / HU | Vigencia; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Sumar días, conservando hora y UTC |
| Nombre MSTest | `CalcularFechaFin_FinDeMes_SumaDiasSinUsarMesCalendario` |
| Método real | `ReglasMembresia.CalcularFechaFin` |
| Prioridad | Alta |
| Precondiciones | Condiciones comunes; fecha inicial explícita en UTC |
| Datos de entrada | Inicio UTC (2026, 1, 31, 14, 30, 0); duración=30 días |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `CalcularFechaFin_FinDeMes_SumaDiasSinUsarMesCalendario`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. |
| Resultado esperado | 2026-03-02T14:30:00Z; Kind=Utc |
| Resultado obtenido | 2026-03-02T14:30:00.0000000Z; Utc |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | FAIL: NotImplementedException en el método extraído pendiente |

## CP-VIG-005 — Incluir el 29 de febrero

| Campo | Especificación |
| --- | --- |
| Identificador | CP-VIG-005 |
| Funcionalidad / HU | Vigencia; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Incluir el 29 de febrero |
| Nombre MSTest | `CalcularFechaFin_AnioBisiesto_Incluye29DeFebrero` |
| Método real | `ReglasMembresia.CalcularFechaFin` |
| Prioridad | Alta |
| Precondiciones | Condiciones comunes; fecha inicial explícita en UTC |
| Datos de entrada | Inicio UTC (2028, 2, 28, 0, 0, 0); duración=1 días |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `CalcularFechaFin_AnioBisiesto_Incluye29DeFebrero`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. |
| Resultado esperado | 2028-02-29T00:00:00Z |
| Resultado obtenido | 2028-02-29T00:00:00.0000000Z |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | FAIL: NotImplementedException en el método extraído pendiente |

## CP-VIG-006 — Cruzar de año manteniendo hora

| Campo | Especificación |
| --- | --- |
| Identificador | CP-VIG-006 |
| Funcionalidad / HU | Vigencia; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Cruzar de año manteniendo hora |
| Nombre MSTest | `CalcularFechaFin_CambioDeAnio_ConservaHora` |
| Método real | `ReglasMembresia.CalcularFechaFin` |
| Prioridad | Alta |
| Precondiciones | Condiciones comunes; fecha inicial explícita en UTC |
| Datos de entrada | Inicio UTC (2026, 12, 31, 23, 30, 0); duración=1 días |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `CalcularFechaFin_CambioDeAnio_ConservaHora`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. |
| Resultado esperado | 2027-01-01T23:30:00Z |
| Resultado obtenido | 2027-01-01T23:30:00.0000000Z |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | FAIL: NotImplementedException en el método extraído pendiente |

## CP-ACC-001 — Vigente y activa

| Campo | Especificación |
| --- | --- |
| Identificador | CP-ACC-001 |
| Funcionalidad / HU | Acceso; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Vigente y activa |
| Nombre MSTest | `ValidarAcceso_ConMembresiaVigenteYActiva_DebePermitirAcceso` |
| Método real | `ReglasMembresia.PermiteAcceso` |
| Prioridad | Alta |
| Precondiciones | Condiciones comunes; ahora fijo: 2026-10-18 12:00:00 UTC |
| Datos de entrada | Activa=true; fin=new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc); ahora=2026-10-18T12:00:00Z |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `ValidarAcceso_ConMembresiaVigenteYActiva_DebePermitirAcceso`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. |
| Resultado esperado | true |
| Resultado obtenido | True |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | FAIL: NotImplementedException en el método extraído pendiente |

## CP-ACC-002 — Vencida

| Campo | Especificación |
| --- | --- |
| Identificador | CP-ACC-002 |
| Funcionalidad / HU | Acceso; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Vencida |
| Nombre MSTest | `ValidarAcceso_ConMembresiaVencida_DebeDenegarAcceso` |
| Método real | `ReglasMembresia.PermiteAcceso` |
| Prioridad | Alta |
| Precondiciones | Condiciones comunes; ahora fijo: 2026-10-18 12:00:00 UTC |
| Datos de entrada | Activa=true; fin=new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc); ahora=2026-10-18T12:00:00Z |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `ValidarAcceso_ConMembresiaVencida_DebeDenegarAcceso`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. |
| Resultado esperado | false |
| Resultado obtenido | False |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | FAIL: NotImplementedException en el método extraído pendiente |

## CP-ACC-003 — Inactiva con fecha futura

| Campo | Especificación |
| --- | --- |
| Identificador | CP-ACC-003 |
| Funcionalidad / HU | Acceso; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Inactiva con fecha futura |
| Nombre MSTest | `ValidarAcceso_ConMembresiaInactiva_DebeDenegarAcceso` |
| Método real | `ReglasMembresia.PermiteAcceso` |
| Prioridad | Alta |
| Precondiciones | Condiciones comunes; ahora fijo: 2026-10-18 12:00:00 UTC |
| Datos de entrada | Activa=false; fin=new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc); ahora=2026-10-18T12:00:00Z |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `ValidarAcceso_ConMembresiaInactiva_DebeDenegarAcceso`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. |
| Resultado esperado | false |
| Resultado obtenido | False |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | FAIL: NotImplementedException en el método extraído pendiente |

## CP-ACC-004 — Vencimiento exacto

| Campo | Especificación |
| --- | --- |
| Identificador | CP-ACC-004 |
| Funcionalidad / HU | Acceso; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Vencimiento exacto |
| Nombre MSTest | `ValidarAcceso_EnInstanteExactoDeVencimiento_DebeDenegarAcceso` |
| Método real | `ReglasMembresia.PermiteAcceso` |
| Prioridad | Alta |
| Precondiciones | Condiciones comunes; ahora fijo: 2026-10-18 12:00:00 UTC |
| Datos de entrada | Activa=true; fin=Ahora; ahora=2026-10-18T12:00:00Z |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `ValidarAcceso_EnInstanteExactoDeVencimiento_DebeDenegarAcceso`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. |
| Resultado esperado | false |
| Resultado obtenido | False |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | FAIL: NotImplementedException en el método extraído pendiente |

## CP-ACC-005 — Un tick antes de vencer

| Campo | Especificación |
| --- | --- |
| Identificador | CP-ACC-005 |
| Funcionalidad / HU | Acceso; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Un tick antes de vencer |
| Nombre MSTest | `ValidarAcceso_UnTickAntesDeVencer_DebePermitirAcceso` |
| Método real | `ReglasMembresia.PermiteAcceso` |
| Prioridad | Alta |
| Precondiciones | Condiciones comunes; ahora fijo: 2026-10-18 12:00:00 UTC |
| Datos de entrada | Activa=true; fin=Ahora.AddTicks(1); ahora=2026-10-18T12:00:00Z |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `ValidarAcceso_UnTickAntesDeVencer_DebePermitirAcceso`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. |
| Resultado esperado | true |
| Resultado obtenido | True |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | FAIL: NotImplementedException en el método extraído pendiente |

## CP-ACC-006 — Un tick después de vencer

| Campo | Especificación |
| --- | --- |
| Identificador | CP-ACC-006 |
| Funcionalidad / HU | Acceso; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Un tick después de vencer |
| Nombre MSTest | `ValidarAcceso_UnTickDespuesDeVencer_DebeDenegarAcceso` |
| Método real | `ReglasMembresia.PermiteAcceso` |
| Prioridad | Alta |
| Precondiciones | Condiciones comunes; ahora fijo: 2026-10-18 12:00:00 UTC |
| Datos de entrada | Activa=true; fin=Ahora.AddTicks(-1); ahora=2026-10-18T12:00:00Z |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `ValidarAcceso_UnTickDespuesDeVencer_DebeDenegarAcceso`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. |
| Resultado esperado | false |
| Resultado obtenido | False |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | FAIL: NotImplementedException en el método extraído pendiente |

## CP-PAG-001 — Recargo de tarjeta

| Campo | Especificación |
| --- | --- |
| Identificador | CP-PAG-001 |
| Funcionalidad / HU | Pagos; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Recargo de tarjeta |
| Nombre MSTest | `ExecutePayment_ConTarjeta_AplicaCincoPorCiento` |
| Método real | `PaymentContext.ExecutePayment → CardStrategy.CalculateFinalAmount` |
| Prioridad | Alta |
| Precondiciones | Condiciones comunes; PaymentContext nuevo por caso |
| Datos de entrada | 200; estrategia tarjeta |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `ExecutePayment_ConTarjeta_AplicaCincoPorCiento`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. |
| Resultado esperado | 210 |
| Resultado obtenido | 210.00 |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | PASS: comportamiento ya implementado |

## CP-PAG-002 — Efectivo sin recargo

| Campo | Especificación |
| --- | --- |
| Identificador | CP-PAG-002 |
| Funcionalidad / HU | Pagos; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Efectivo sin recargo |
| Nombre MSTest | `ExecutePayment_ConEfectivo_NoAplicaRecargo` |
| Método real | `PaymentContext.ExecutePayment → CashStrategy.CalculateFinalAmount` |
| Prioridad | Alta |
| Precondiciones | Condiciones comunes; PaymentContext nuevo por caso |
| Datos de entrada | 100; estrategia efectivo |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `ExecutePayment_ConEfectivo_NoAplicaRecargo`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. |
| Resultado esperado | 100 |
| Resultado obtenido | 100 |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | PASS: comportamiento ya implementado |

## CP-PAG-003 — Estrategia predeterminada

| Campo | Especificación |
| --- | --- |
| Identificador | CP-PAG-003 |
| Funcionalidad / HU | Pagos; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Estrategia predeterminada |
| Nombre MSTest | `ExecutePayment_SinEstrategia_UsaEfectivo` |
| Método real | `PaymentContext.ExecutePayment → CashStrategy.CalculateFinalAmount` |
| Prioridad | Alta |
| Precondiciones | Condiciones comunes; PaymentContext nuevo por caso |
| Datos de entrada | 200; sin configurar estrategia |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `ExecutePayment_SinEstrategia_UsaEfectivo`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. |
| Resultado esperado | 200 |
| Resultado obtenido | 200 |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | PASS: comportamiento ya implementado |

## CP-PAG-004 — Selección por debajo del umbral

| Campo | Especificación |
| --- | --- |
| Identificador | CP-PAG-004 |
| Funcionalidad / HU | Pagos; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Selección por debajo del umbral |
| Nombre MSTest | `ExecutePaymentForAmount_MenorA100_UsaEfectivo` |
| Método real | `PaymentContext.ExecutePaymentForAmount → ExecutePayment → estrategia` |
| Prioridad | Alta |
| Precondiciones | Condiciones comunes; PaymentContext nuevo por caso |
| Datos de entrada | 99.99 |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `ExecutePaymentForAmount_MenorA100_UsaEfectivo`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. |
| Resultado esperado | 99.99 |
| Resultado obtenido | 99.99 |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | FAIL: NotImplementedException en el método extraído pendiente |

## CP-PAG-005 — Selección exactamente en el umbral

| Campo | Especificación |
| --- | --- |
| Identificador | CP-PAG-005 |
| Funcionalidad / HU | Pagos; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Selección exactamente en el umbral |
| Nombre MSTest | `ExecutePaymentForAmount_IgualA100_UsaEfectivo` |
| Método real | `PaymentContext.ExecutePaymentForAmount → ExecutePayment → estrategia` |
| Prioridad | Alta |
| Precondiciones | Condiciones comunes; PaymentContext nuevo por caso |
| Datos de entrada | 100 |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `ExecutePaymentForAmount_IgualA100_UsaEfectivo`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. |
| Resultado esperado | 100 |
| Resultado obtenido | 100 |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | FAIL: NotImplementedException en el método extraído pendiente |

## CP-PAG-006 — Selección por encima del umbral; conservar precisión actual

| Campo | Especificación |
| --- | --- |
| Identificador | CP-PAG-006 |
| Funcionalidad / HU | Pagos; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Selección por encima del umbral; conservar precisión actual |
| Nombre MSTest | `ExecutePaymentForAmount_MayorA100_UsaTarjetaSinRedondeoAdicional` |
| Método real | `PaymentContext.ExecutePaymentForAmount → ExecutePayment → estrategia` |
| Prioridad | Alta |
| Precondiciones | Condiciones comunes; PaymentContext nuevo por caso |
| Datos de entrada | 100.01 |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `ExecutePaymentForAmount_MayorA100_UsaTarjetaSinRedondeoAdicional`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. |
| Resultado esperado | 105.0105 |
| Resultado obtenido | 105.0105 |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | FAIL: NotImplementedException en el método extraído pendiente |

## CP-PAG-007 — Evitar conservar tarjeta para el siguiente monto bajo

| Campo | Especificación |
| --- | --- |
| Identificador | CP-PAG-007 |
| Funcionalidad / HU | Pagos; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Evitar conservar tarjeta para el siguiente monto bajo |
| Nombre MSTest | `ExecutePaymentForAmount_PagosConsecutivos_ReevaluaEstrategia` |
| Método real | `PaymentContext.ExecutePaymentForAmount → ExecutePayment → estrategia` |
| Prioridad | Alta |
| Precondiciones | Condiciones comunes; PaymentContext nuevo por caso |
| Datos de entrada | 200 y después 100, mismo contexto |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `ExecutePaymentForAmount_PagosConsecutivos_ReevaluaEstrategia`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. |
| Resultado esperado | 210 y 100 |
| Resultado obtenido | 210.00; 100 |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | FAIL: NotImplementedException en el método extraído pendiente |

## CP-ENV-001 — Interpretar valores y comillas

| Campo | Especificación |
| --- | --- |
| Identificador | CP-ENV-001 |
| Funcionalidad / HU | Configuracion; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Interpretar valores y comillas |
| Nombre MSTest | `LoadFile_CargaVariablesSimplesYConComillas` |
| Método real | `EnvLoader.LoadFile` |
| Prioridad | Media |
| Precondiciones | Condiciones comunes; Setup crea una ruta única y registra el estado previo de las variables |
| Datos de entrada | Comentario, línea vacía, valor simple y entre comillas |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `LoadFile_CargaVariablesSimplesYConComillas`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. 4. Ejecutar TestCleanup. |
| Resultado esperado | valor_simple_123; valor con espacios y comillas |
| Resultado obtenido | valor_simple_123; valor con espacios y comillas |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | PASS: comportamiento ya implementado |

## CP-ENV-002 — Respetar prioridad del entorno

| Campo | Especificación |
| --- | --- |
| Identificador | CP-ENV-002 |
| Funcionalidad / HU | Configuracion; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Respetar prioridad del entorno |
| Nombre MSTest | `LoadFile_NoSobrescribeVariableExistenteDelSistema` |
| Método real | `EnvLoader.LoadFile` |
| Prioridad | Media |
| Precondiciones | Condiciones comunes; Setup crea una ruta única y registra el estado previo de las variables |
| Datos de entrada | Entorno=valor_del_sistema; archivo=valor_del_archivo_env |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `LoadFile_NoSobrescribeVariableExistenteDelSistema`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. 4. Ejecutar TestCleanup. |
| Resultado esperado | valor_del_sistema |
| Resultado obtenido | valor_del_sistema |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | PASS: comportamiento ya implementado |

## CP-ENV-003 — Conservar valor multilínea

| Campo | Especificación |
| --- | --- |
| Identificador | CP-ENV-003 |
| Funcionalidad / HU | Configuracion; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Conservar valor multilínea |
| Nombre MSTest | `LoadFile_ValorMultilinea_ConservaSaltosDeLinea` |
| Método real | `EnvLoader.LoadFile` |
| Prioridad | Media |
| Precondiciones | Condiciones comunes; Setup crea una ruta única y registra el estado previo de las variables |
| Datos de entrada | Valor entre comillas con dos líneas |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `LoadFile_ValorMultilinea_ConservaSaltosDeLinea`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. 4. Ejecutar TestCleanup. |
| Resultado esperado | linea1\\nlinea2 (salto LF) |
| Resultado obtenido | linea1\nlinea2 |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | PASS: comportamiento ya implementado |

## CP-ENV-004 — Archivo ausente sin efectos

| Campo | Especificación |
| --- | --- |
| Identificador | CP-ENV-004 |
| Funcionalidad / HU | Configuracion; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Archivo ausente sin efectos |
| Nombre MSTest | `LoadFile_ArchivoInexistente_NoCreaArchivoNiAlteraVariable` |
| Método real | `EnvLoader.LoadFile` |
| Prioridad | Media |
| Precondiciones | Condiciones comunes; Setup crea una ruta única y registra el estado previo de las variables |
| Datos de entrada | Ruta temporal inexistente; variable=original |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `LoadFile_ArchivoInexistente_NoCreaArchivoNiAlteraVariable`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. 4. Ejecutar TestCleanup. |
| Resultado esperado | original; archivo no creado |
| Resultado obtenido | original; False |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | PASS: comportamiento ya implementado |

## CP-ENV-005 — Ignorar líneas inválidas y conservar '=' interno

| Campo | Especificación |
| --- | --- |
| Identificador | CP-ENV-005 |
| Funcionalidad / HU | Configuracion; HU pendiente de vincular con el catálogo del equipo |
| Objetivo | Ignorar líneas inválidas y conservar '=' interno |
| Nombre MSTest | `LoadFile_LineasInvalidas_IgnoraYConservaIgualesDentroDelValor` |
| Método real | `EnvLoader.LoadFile` |
| Prioridad | Media |
| Precondiciones | Condiciones comunes; Setup crea una ruta única y registra el estado previo de las variables |
| Datos de entrada | Comentario; línea sin '='; clave vacía; CLAVE=uno=dos |
| Pasos | 1. Preparar las entradas indicadas. 2. Ejecutar el método real desde `LoadFile_LineasInvalidas_IgnoraYConservaIgualesDentroDelValor`. 3. Comparar el resultado con Assert.AreEqual y registrar Esperado/Obtenido. 4. Ejecutar TestCleanup. |
| Resultado esperado | uno=dos |
| Resultado obtenido | uno=dos |
| Estado final | PASS |
| Evidencia | `refactor.trx`, resultado con el nombre MSTest de esta ficha |
| Estado en RED | PASS: comportamiento ya implementado |

## Interpretación y límites

- CP-VIG-003 reemplaza la simulación de creación de MembresiaUsuario por una prueba
  real del cálculo de 90 días. No afirma probar persistencia ni activación en Firestore.
- Los casos de acceso verifican la regla `activa && fechaFin > ahora`.
  La búsqueda por usuario y el filtrado Firestore permanecen en producción.
- CP-PAG-003 prueba el valor predeterminado sin estrategia. Es distinto del flujo
  automático del controlador, que utiliza ExecutePaymentForAmount.
- CP-PAG-006 preserva 105.0105. No se introdujo un redondeo a dos decimales.
- No se añadieron rechazos de importes negativos, duraciones no positivas ni
  comprobación de FechaInicio: esas serían nuevas reglas pendientes de decisión.
- EnvLoader mantiene su implementación de producción; se mejoró la prueba.
- La cobertura listada es funcional por casos. No se midió un porcentaje de líneas.
- Para presentar el resultado, consultar [GUIA_DEMOSTRACION.md](GUIA_DEMOSTRACION.md).
