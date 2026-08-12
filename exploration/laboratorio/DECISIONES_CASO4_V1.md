# Decisiones — Caso 4: Evolución Financiera

Estado: **D-091 y D-092 resueltas por auditoría**. Misma estructura usada en D-001 a D-090
(decisión, opciones, criterio, evidencia). Ningún código se modifica en este documento — las
resoluciones aquí registradas habilitan la especificación de implementación siguiente
(`ESPECIFICACION_CLASIFICADOR_INTENCION_ORDEN_V1.md`), no la reemplazan.

Contexto completo en `PROPUESTA_CASO4_V1.md`. Verificación contra código existente, no
reconstruida de memoria (mismo criterio que abrió Caso 2/Caso 3, D-057).

---

## D-091 — Alcance de modificación del motor

**Estado**: 🟢 Aprobada. **Selección: C — corrección en motor (`src/`) con activación experimental
explícita, comportamiento histórico preservado por default.**

**Decisión**: ¿Caso 4 corrige D-084/D-085 modificando `src/` directamente, mantiene la corrección
enteramente en `exploration/`, o adopta un diseño híbrido?

### Hallazgo previo a las opciones — D-084 ya vive en `src/`

A diferencia de como se presentó la pregunta en `PROPUESTA_CASO4_V1.md` §8 ("si Caso 4 debe tocar
`src/`"), la verificación de código muestra que **la pregunta no es si tocar `src/`, sino si
corregir el código ya existente ahí**:

- `src/Domain/Portfolio/GestorCapital.cs` — el archivo con el defecto de D-084 — no es un
  componente del laboratorio, es parte del motor de producción (`Domain/Portfolio/`, mismo
  namespace que `AplicadorFill`/`ConsumidorFifo`/`ResolutorCrossZero`).
- `src/Application/BacktestRunner.cs:52` invoca `GestorCapital.Ajustar(requests, portfolio,
  config.Sizing)` directamente en el orquestador central del backtest, antes de
  `ValidadorBolsaRequests.Evaluar` (línea 57) — no hay ninguna capa intermedia del laboratorio
  entre la estrategia y este código.
- `ValidadorCapacidad.Validar` (línea 65) y `CalculadoraReservaPreventiva.Calcular` (línea 67) se
  invocan en la misma función, sobre el mismo `request.Cantidad` ya transformado por
  `GestorCapital` — confirmando el acoplamiento D-084↔D-085 documentado en
  `PROPUESTA_CASO4_V1.md` §2 dentro del propio motor, no en una capa externa.

**Consecuencia**: `config.Sizing=null` en todo baseline congelado (Caso 1, Caso 2, Caso 3A) es lo
que mantiene D-084 inactiva hoy — no una separación arquitectónica entre motor y defecto. El
defecto ya está en producción, simplemente no se activa con la configuración congelada. Cualquier
opción que "mantenga todo en `exploration/`" debe partir de este hecho, no de la premisa de que
`GestorCapital` es código de laboratorio.

**Evidencia**: `DECISIONES_MODELO_ECONOMICO_V1.md:671-672` ya documentó la causa raíz exacta
(`GestorCapital.Ajustar` recalcula `Cantidad` en toda `OrderRequest`) al cerrar D-084 en Caso 2 —
Caso 4 no descubre el hallazgo, lo hereda con ubicación exacta ya conocida.

### Opciones

- **A — Modificar `src/` directamente**: corregir `GestorCapital.cs`/`OrderRequest.cs` (o el
  contrato que 4.1 determine) en su ubicación actual.
  - Ventajas: resuelve la causa raíz donde realmente vive; un único `GestorCapital` para todos los
    consumidores (`BacktestRunner`, y transitivamente cualquier corrida de `Application`, no solo
    el laboratorio); no dejar dos implementaciones divergentes del mismo cálculo.
  - Riesgos: cualquier cambio de contrato en `OrderRequest` (ej. campo de intención
    apertura/cierre) es visible a todo `src/Domain`/`src/Application`/`src/Presentation` — requiere
    verificar los 107 tests de producción y los 3 baselines congelados no solo no cambian de
    resultado, sino que compilan sin romper ningún consumidor existente
    (`EstrategiaDemo.cs`, `tests/`).
- **B — Mantener la corrección en `exploration/`**: envolver o interceptar `GestorCapital.Ajustar`
  desde una capa nueva en el laboratorio, sin modificar `src/Domain/Portfolio/GestorCapital.cs`.
  - Ventajas: cero riesgo de regresión sobre `src/`/`tests/`, mismo perfil de riesgo que Caso 3A.
  - Riesgos (agravados por el hallazgo anterior): el defecto real permanece en `src/` sin corregir
    — `BacktestRunner.cs:52` seguiría invocando la versión defectuosa para cualquier consumidor de
    producción que no pase por la capa nueva del laboratorio. Duplicar la lógica correcta en
    `exploration/` sin corregir `src/` no resuelve D-084, la esconde detrás de una segunda
    implementación — contradice el criterio de éxito de `PROPUESTA_CASO4_V1.md` §5 ("D-084
    resuelta con evidencia, no con parche").
- **C — Diseño híbrido**: nuevo(s) tipo(s)/método(s) en `src/Domain` (ej. un campo de intención en
  `OrderRequest`, o un tipo `IntencionOrden` separado) que resuelvan D-084/D-085 en su ubicación
  real, pero cuya **activación** (uso efectivo por `GestorCapital`/`BacktestRunner` con sizing no
  nulo) quede condicionada a configuración explícita desde el laboratorio — mismo patrón D-061 ya
  usado en Caso 2 (parámetro opcional con default histórico que preserva compatibilidad).
  - Ventajas: corrige la causa raíz en su ubicación real (a diferencia de B), pero cualquier corrida
    congelada existente sigue produciendo exactamente el mismo resultado sin ningún cambio de
    comportamiento por default (a diferencia de A sin resguardo) — mismo principio que ya probó su
    viabilidad en D-079 (extender `EntradaProtocolo` con campos opcionales sin afectar Caso 1).
  - Riesgos: mayor superficie de diseño en 4.1 (definir el contrato nuevo con cuidado) que A o B;
    requiere disciplina explícita para no dejar "dos caminos" (con/sin corrección) como deuda
    técnica permanente en vez de una transición hacia una única versión correcta.

### Resolución adoptada

**Selección: C.** La pregunta real no era "tocar o no `src/`" (ya estaba tocado — `GestorCapital`
vive en `src/Domain/Portfolio/` desde Caso 2) sino si el comportamiento por default de todo
consumidor existente cambia con esta fase. Respuesta: **no debe cambiar** — mismo patrón que
D-061/D-069/D-079/D-082 ya probaron: `null`/default preserva el comportamiento histórico, la nueva
capacidad entra solo mediante configuración explícita.

**Reglas fijadas**:
- Las mejoras de semántica económica (corrección de D-084, definición de D-085) pueden vivir en
  `src/` — no se descarta modificar `GestorCapital.cs`/`OrderRequest.cs` en su ubicación real.
- El comportamiento histórico de Caso 1 (`Sizing=null`, `Cantidad=1` fija) y Caso 2 (baseline
  financiero, `Sizing=null` por D-084 original) debe permanecer bit-a-bit idéntico cuando la nueva
  capacidad no esté activada.
- Toda nueva capacidad entra mediante configuración explícita/versionado experimental — nunca
  activación implícita ni cambio de default.

**Explícitamente rechazado**: duplicar la lógica corregida en `exploration/` dejando el original
defectuoso en `src/` sin corregir (Opción B) — esconder D-084 detrás de una segunda
implementación no es resolverla.

**Consecuencia para D-084**: deja de tratarse como deuda "externa" al motor — pasa a ser una
corrección de motor con protección de regresión. El objetivo no es cambiar el comportamiento de
Caso 1 (`Sizing=null`), sino evitar que, cuando alguien active `Sizing != null`, el motor tenga una
semántica incorrecta de cierre.

**Consecuencia para D-085**: queda dentro del alcance de Caso 4 — la relación
`Cantidad × Precio × TasaMargen` debe tener semántica económica explícita, pero sin cambiar
silenciosamente estrategias congeladas, `CapitalInicial` histórico, ni ningún baseline existente.

---

---

## D-092 — Clasificación previa de intención de orden

**Estado**: 🟢 Aprobada. **Selección: Opción 2 — componente clasificador separado, ejecutado antes
de `GestorCapital`.**

**Decisión**: ¿cómo determina el motor, en el punto donde `GestorCapital.Ajustar` necesita saberlo
(antes del `Fill`), si una `OrderRequest` abre, aumenta, reduce o invierte una posición? Resuelve
la pregunta abierta por `ESPECIFICACION_SEMANTICA_ORDEN_V1.md`.

**Motivo de la selección**: el hallazgo de la especificación cambia la naturaleza del problema — la
regla de clasificación **ya existe** en `AplicadorFill.Aplicar`
(`src/Domain/Portfolio/AplicadorFill.cs:17`, comparación de signo de `Fill` contra
`PosicionActual.De(portfolio)`), el problema es puramente de ubicación temporal: esa regla corre
después del `Fill`, pero D-084 la necesita antes, en el punto de `GestorCapital`. No se crea
semántica nueva de estrategia — se reubica una regla de dominio ya validada.

**Evaluación de las 3 opciones**:
- **Opción 1 — Extender `OrderRequest`**: ❌ rechazada. Obliga a la estrategia a conocer intención
  económica (qué representa "abrir" o "cerrar" en términos de `PortfolioState`), mezclando señal
  con gestión de posición — contradice P-002 (separación estrategia/economía) y el criterio de
  éxito de `PROPUESTA_CASO4_V1.md` §5. Modificaría un contrato consumido por las 5 estrategias
  existentes.
- **Opción 2 — Componente separado (`ClasificadorIntencionOrden` o equivalente)**: ✅
  seleccionada. Una única definición de intención, reutilizable por `GestorCapital`, sin modificar
  `IStrategy` ni ninguna estrategia existente, manteniendo separación de responsabilidades — mismo
  patrón de componente de responsabilidad única que `ConsumidorFifo`/`ResolutorCrossZero` ya
  establecieron en `Domain/Portfolio/`.
- **Opción 3 — Inferencia inline dentro de `GestorCapital`**: ❌ rechazada. Aunque técnicamente
  posible, mezcla dos responsabilidades (sizing + interpretación de posición) dentro de un mismo
  componente — reproduce el mismo patrón de acoplamiento que originó D-084 (una capa que empieza a
  conocer demasiado del dominio), solo que en un lugar distinto.

**Arquitectura objetivo**:

```
Strategy
   |
OrderRequest
   |
ClasificadorIntencionOrden
   |
GestorCapital
   |
ValidadorCapacidad
   |
Fill
   |
AplicadorFill
```

**Restricción de fuente de verdad**: la clasificación debe derivarse exclusivamente de
`PortfolioState`/`LotesVivos` (posición real) — nunca de nombre de estrategia, tipo de orden, ni
metadata manual declarada por la estrategia. Mismo criterio que ya usa `AplicadorFill`.

**Restricciones de no-modificación**: `IStrategy` intacto; las 5 estrategias existentes intactas;
lógica FIFO (`ConsumidorFifo`) intacta; Cross-Zero (`ResolutorCrossZero`) intacto — el componente
nuevo reutiliza el mismo criterio de clasificación que esos componentes ya usan, no lo reemplaza.

**Impacto sobre D-084 (redefinido)**: antes, `GestorCapital` recalculaba `Cantidad` en toda
`OrderRequest` sin distinción. Después de D-092, `GestorCapital` ajusta únicamente las órdenes
compatibles con sizing activo (apertura/aumento) — las órdenes de reducción/cierre conservan la
`Cantidad` necesaria para cerrar exactamente la posición existente, eliminando la causa raíz de los
residuos de lotes documentados en el hallazgo original de D-084.

**Impacto sobre D-085**: sin resolver todavía — depende de que la semántica de orden (D-092) esté
implementada primero. Orden de dependencia: (1) saber qué cantidad se está modificando y cuándo
corresponde aplicar sizing, (2) definir unidades/exposición.

**Evidencia**: `ESPECIFICACION_SEMANTICA_ORDEN_V1.md` §1 (hallazgo verificado en código), §2-3
(comparación de opciones y recomendación técnica, adoptada sin cambios).

---

## Resumen de decisiones

| Decisión | Selección | Estado |
|---|---|---|
| D-091 | Corrección en `src/` con activación experimental explícita, default histórico preservado (Opción C) | 🟢 Aprobada |
| D-092 | Componente clasificador de intención, previo a `GestorCapital`, fuente de verdad = `PortfolioState`/`LotesVivos` (Opción 2) | 🟢 Aprobada |

---

## Fuera de alcance de este documento

No se modifica código en este documento. No se define la firma exacta del componente
(`ClasificadorIntencionOrden` es un nombre de trabajo, no definitivo) — es el próximo documento, no
parte de este.

---

## Cierre de sub-fase 4.1 — `ClasificadorIntencionOrden`

**Estado**: ✅ Aprobado por auditoría. Implementación: `src/Domain/Portfolio/
ClasificadorIntencionOrden.cs` (enum `IntencionOrden` + `Clasificar(PortfolioState,
OrderRequest)`), pruebas: `tests/Domain.Tests/Portfolio/ClasificadorIntencionOrdenTests.cs`
(11 pruebas, P1-P7 de `ESPECIFICACION_CLASIFICADOR_INTENCION_ORDEN_V1.md`).

**Verificación**: 118/118 tests de producción (incluyendo las 11 nuevas). P6 confirmado con
evidencia real — 4 pruebas clasifican antes del `Fill` y luego invocan `AplicadorFill.Aplicar` real
sobre el mismo escenario, confirmando que la predicción coincide con la rama efectivamente
ejecutada, incluyendo el caso simétrico de posición corta. P7 (pureza) verificado explícitamente.
`GestorCapital.cs`/`AplicadorFill.cs`/`ConsumidorFifo.cs`/`ResolutorCrossZero.cs`/
`OrderRequest.cs` sin cambios. Ambos baselines congelados intactos.

**D-084 tras 4.1**: 🟡 en resolución — falta la integración con `GestorCapital` (sub-fase 4.2).

---

## Próximo paso

`ESPECIFICACION_CLASIFICADOR_INTENCION_ORDEN_V1.md` — nombre, ubicación (`src/Domain/Portfolio/`
probablemente), algoritmo exacto (posición actual, lado, cantidad solicitada → resultado), casos
(apertura, aumento, reducción parcial, cierre total, Cross-Zero), compatibilidad con
`Sizing=null`, pruebas obligatorias. Completado — ver "Cierre de sub-fase 4.1" arriba.

`ESPECIFICACION_INTEGRACION_GESTOR_CAPITAL_V1.md` (sub-fase 4.2) — integración de
`ClasificadorIntencionOrden` en `GestorCapital.Ajustar`, qué intenciones reciben sizing,
preservación de `Sizing=null`, interacción con `ValidadorCapacidad`, pruebas, no-regresión.
Completado — ver "Cierre de sub-fase 4.2" abajo.

---

## Cierre de sub-fase 4.2 — Integración en `GestorCapital`

**Estado**: ✅ Implementado y verificado. `src/Domain/Portfolio/GestorCapital.cs` modificado:
clasifica secuencialmente cada `OrderRequest` de la bolsa contra una posición **proyectada** local
(nunca contra `PortfolioState.LotesVivos` real, que sigue mutando exclusivamente vía
`AplicadorFill`, D-071 vigente) — `Apertura`/`Aumento` reciben `cantidadCalculada` por sizing;
`ReduccionParcial`/`CierreTotal`/`CrossZero` conservan la `Cantidad` original de la orden.

**Pruebas**: P1-P6 (`GestorCapitalTests.cs`, congeladas desde Caso 2, `ESPECIFICACION_GESTOR_
CAPITAL_PORCENTAJE_V1.md`) pasan sin modificación — ningún escenario ahí cruza cero. P7-P10
nuevas: `CierreTotalConservaLaCantidadOriginalNoLaDeSizing`, `CrossZeroConservaLaCantidadOriginal
SinAplicarSizing`, `SegundaOrdenDeUnaBolsaDeReversionSeClasificaContraLaPosicionProyectada` (el
hallazgo crítico de la especificación §2, probado directamente sobre `GestorCapital.Ajustar` con
un `PortfolioState` construido explícitamente), `AjustarNoMutaElPortfolioStateReal`.

**Hallazgo durante implementación, no una decisión nueva**: el primer diseño de prueba de P9
(`EstrategiaReversionEnUnaBolsa`, una `IStrategy` fake que abría con `Cantidad=1m` y luego
intentaba "cerrar" con `Cantidad=1m` fija) reveló que, bajo sizing activo, la posición real que
deja una apertura es la cantidad *calculada* (`cantidadCalculada`), no la *pedida* por la
estrategia — una orden de cierre con `Cantidad=1m` fija se clasifica como `ReduccionParcial` (no
`CierreTotal`) frente a esa posición mayor. Esto no es una limitación nueva del sistema: es
exactamente el mismo síntoma que D-085 ya documentó (`Cantidad` fija sin relación dimensional),
aplicado aquí al diseño del test, no al comportamiento del motor — ninguna `IStrategy` real puede
conocer `cantidadCalculada` de antemano (P-002, no conoce `Cash`/`Margin`). Corregido rediseñando
P9 para probar `GestorCapital.Ajustar` directamente sobre un `PortfolioState` construido
explícitamente (posición Long=1 ya viva), aislando la clasificación secuencial dentro de la bolsa
del cálculo de sizing de apertura — mismo patrón de diagnóstico que D-062/D-083/D-084/el hallazgo
de arnés P5 de Caso 3A: verificar con evidencia antes de corregir, distinguir defecto de test de
limitación real. **No se registra como decisión nueva** — no reveló ninguna limitación del
pipeline ni del diseño de 4.2, solo un defecto del fake de prueba escrito en este mismo ciclo.

**P8 — corrida larga verificada**: reproducido el escenario exacto que originó D-084
(`EstrategiaTresMosqueteros`, dataset 1m real, `Sizing(PorcentajeRiesgo: 0.000002m)` de D-083, vía
un proyecto satélite temporal, eliminado tras la verificación, sin tocar `baseline_financiero/`
congelado). Resultado: `Estado=Success` en **~10 segundos** (antes: colgaba 25+ minutos, killeado
sin terminar). `CashFinal`/`EquityFinal` extremadamente negativos — **esperado y fuera de alcance
de 4.2**, es D-085 (`Cantidad=1` fija sin relación dimensional con `CapitalInicial`), no un fallo
de esta sub-fase.

**Regresión**: 122/122 tests de producción (62 Domain.Tests + 4 Contracts + 2 Infrastructure + 18
Api + 36 Application, incluyendo las 6 P1-P6 congeladas de Caso 2 + las 4 nuevas P7-P10). 3
baselines congelados (Caso 1, Caso 2, Caso 3A) sin regenerar ni alterar —
`git status --porcelain` vacío sobre esas rutas.

**D-084 tras 4.2**: ✅ resuelta — causa raíz corregida (`GestorCapital` ya no recalcula `Cantidad`
en órdenes de cierre/reducción/Cross-Zero), verificado con la misma corrida que originó el
hallazgo.

---

## Próximo paso

Auditoría de cierre de 4.2 por el auditor. Tras eso: sub-fase 4.3 (D-085 — unidades y exposición),
único punto de `PROPUESTA_CASO4_V1.md` §6 que sigue sin resolver dentro del alcance de Caso 4.
