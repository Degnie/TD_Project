# Documento de diseño — Fase 2D: Normalización de modelo financiero

Estado: **cerrado como hoja de ruta (2026-08-11). Sin implementación de código.** Responde a la
pregunta que Fase 2C dejó explícitamente abierta: el motor funciona (validado), pero los
retornos que produce no son comparables entre sí sin definir primero qué representa cada pieza
del modelo actual. Decisión final: el sistema persigue **Caso 1 — Laboratorio de estrategias**
(sección 3.4) bajo **Modelo A — la estrategia controla la cantidad** (sección 3.3), sin cambios
en `src/`, `SPEC.md`, `IStrategy`, ni contratos. Caso 2/3 y Modelo B/C quedan como evolución
prevista, documentados pero no implementados.

**Prioridad de esta versión del documento**: antes de elegir un modelo de sizing (sección 3, la
prioridad original), hay que separar qué supuestos financieros son parte deliberada del modelo y
cuáles son simples valores ocultos del código. El motor calculó correctamente bajo sus supuestos
actuales durante Fase 1-2C (lógica, reconciliación y determinismo validados, sin invalidarse por
este hallazgo) — pero uno de esos supuestos (`TasaMargen`) nunca estuvo formalizado ni declarado
como parte del experimento. Elegir un modelo de sizing sin resolver esto primero construiría
métricas sobre un modelo financiero parcialmente implícito.

## 1. Supuestos financieros ocultos detectados

Clasificados como **`[SUPUESTO FINANCIERO NO EXPLICITADO]`**, no como `[BUG]` — no se sabe
todavía si el valor actual es incorrecto; el problema es que no está declarado ni parametrizado.

| Supuesto | Ubicación | Estado |
|---|---|---|
| `TasaMargen = 0.1` (10%) | `src/Domain/Portfolio/AplicadorFill.cs:13`, default de parámetro | Pendiente de formalización |
| `CostoFriccionReal = 0` (siempre cero) | `src/Domain/Matching/MatchingEngine.cs:30,51`, literal fijo en cada `Fill` | Pendiente de formalización (ya documentado como gap del Friction Model en `docs/PENDIENTES.md`, mismo hallazgo visto desde otro ángulo) |
| Tamaño fijo de posición (`Cantidad` sin unidad definida) | Decisión de cada `IStrategy`, sin regla del motor | Pendiente de decisión (sección 3) |
| Estrategia sin acceso a capital/portfolio | `src/Domain/Strategy/IStrategy.cs`, contrato de `Observar(DataSlice)` | Impacto arquitectónico a evaluar (sección 4) |
| `ValidadorCapacidad` desconectado del flujo real | `src/Application/BacktestRunner.cs`, no invoca `ValidadorCapacidad` (RN-12/CU-15) | Ya documentado en `docs/PENDIENTES.md`, relevante para cualquier modelo de sizing basado en capital |

### `TasaMargen` — detalle

`SPEC.md` (RN-08) define `Margin_k = Q_k × PrecioFill_k × TasaMargen`, pero **no fija el valor de
`TasaMargen` ni dice de dónde proviene** — ni siquiera está en el glosario como concepto con
definición propia. En código, `TasaMargen` no es parte de `ConfiguracionExperimento`
(`src/Application/ConfiguracionExperimento.cs` — solo tiene `CapitalInicial`, `Velas`, `Warmup`)
ni de ningún contrato público. Es un valor por defecto hardcodeado —
`AplicadorFill.Aplicar(..., decimal tasaMargen = 0.1m)` — 10% fijo, invisible desde afuera del
motor, imposible de configurar por experimento o por estrategia sin recompilar.

**Pregunta abierta, no respondida en este documento**: ¿`TasaMargen` es una propiedad del
mercado (distinta por instrumento), una condición del broker (apalancamiento contratado), o un
parámetro del experimento (elegido por quien corre el backtest)? La respuesta cambia dónde debe
vivir el valor — hoy no vive en ningún lado explícito, solo en el default de una función interna.

### `CostoFriccionReal` — detalle

Cada `Fill` se crea con `CostoFriccionReal: 0m` fijo en `MatchingEngine.cs`, sin importar el
tipo de orden ni el instrumento. `SPEC.md` sí define el concepto (glosario: "Friction Model —
modelo determinista que proyecta costos preventivamente... y calcula costos reales definitivos")
pero la implementación actual no lo calcula — devuelve la ausencia de fricción como si fuera un
resultado válido, no un placeholder. Mismo patrón que `TasaMargen`: un supuesto financiero
(mercado sin comisiones ni slippage) tomado por default sin declararlo como tal.

## 2. Exposición de parámetros — clasificación y respuestas

### Clasificación por tipo

| Tipo | Definición | Ejemplos del catálogo propuesto |
|---|---|---|
| **A — Instrumento/mercado** | Varía según el activo operado; no depende de quién corre el experimento | Margen requerido, apalancamiento, comisión del exchange, tick size |
| **B — Experimento** | Elegido por quien corre el backtest, constante durante toda la corrida | Capital inicial, período, timeframe, estrategia utilizada |
| **C — Ejecución** | Depende de las condiciones del mercado en el momento exacto de cada Fill | Slippage, impacto de mercado, latencia |
| **D — Estrategia** | Decisión interna de la lógica de trading, no del motor ni del mercado | Señal, martingala, tamaño deseado |

### Respuestas a la tabla de preguntas

| Parámetro | Pregunta | Respuesta (evidencia disponible) | Clasificación |
|---|---|---|---|
| `TasaMargen` | ¿Representa broker, instrumento, mercado o experimento? | **No se puede responder con la evidencia actual.** `SPEC.md` define la fórmula pero no el origen del valor; el código lo trata como constante universal (un único `0.1m` para cualquier instrumento, sin distinción). Candidato más probable por cómo funciona el margen en mercados reales: **Tipo A** (varía por instrumento/exchange — BTC spot no tiene el mismo apalancamiento disponible que un futuro), pero también podría diseñarse como **Tipo B** si se quiere que cada experimento simule un apalancamiento distinto a propósito. Esta ambigüedad es exactamente lo que hay que decidir, no algo que el código actual ya responda. | A o B — pendiente de decisión explícita |
| `CostoFriccionReal` | ¿Representa mercado, liquidez, tamaño de orden o placeholder? | Hoy es un **placeholder puro** (`0m` literal, sin lógica detrás) — no representa ninguna de las otras tres opciones todavía, aunque `SPEC.md` (glosario "Friction Model") deja espacio conceptual para que combine instrumento (comisión fija) y ejecución (slippage/impacto, que depende del tamaño de la orden y la liquidez del momento). Es decir: el destino final probablemente mezcla **Tipo A** (comisión del exchange) y **Tipo C** (slippage/impacto), no un solo tipo. | A + C (mixto) — pendiente de diseño del Friction Model, no solo de un valor |
| Tamaño fijo de posición | ¿Es una decisión del motor o una simplificación temporal? | Es una **decisión de la estrategia** (Tipo D), no del motor — confirmado en código: `Cantidad` la fija cada `IStrategy.Observar` sin que el motor imponga ni valide ningún límite. No es "simplificación temporal" en el sentido de un default provisional del motor — es, literalmente, cómo está diseñado el contrato hoy (el motor es agnóstico al tamaño, delega 100% en quien implementa `IStrategy`). Si eso debe seguir siendo así, o si el motor debería participar (sección 4), es la pregunta abierta — pero la situación actual sí tiene una respuesta clara. | D (hoy) — su ubicación futura depende de la sección 4 |
| Capital inicial | ¿Es una condición de prueba o parte del modelo? | Ya es **Tipo B por diseño y por evidencia**: vive en `ConfiguracionExperimento.CapitalInicial`, es el único parámetro financiero que el sistema ya trata explícitamente como decisión de quien corre el experimento, con contrato público y sin ambigüedad. Este es el único de los cuatro que no tiene pendiente de clasificación — ya está correctamente ubicado. | B — ya resuelto, sin acción pendiente |

### Consecuencia para dónde vive cada parámetro

- `TasaMargen`: si termina siendo Tipo A, necesitaría vivir junto a un concepto de instrumento
  que hoy no existe en `src/` (no hay ningún `Instrumento`/`PerfilMercado`); si es Tipo B, un
  campo nuevo en `ConfiguracionExperimento` alcanza. La decisión de tipo antecede a la decisión
  de contrato.
- `CostoFriccionReal`: al ser mixto (A+C), probablemente no resuelve con un solo valor —
  necesitaría un modelo (función de tamaño de orden × liquidez × comisión fija), no un campo
  escalar. Esto es consistente con que `SPEC.md` ya lo llama "Friction Model" y no "Costo de
  Fricción" a secas.
- Tamaño de posición (Tipo D hoy): permanece en la estrategia salvo que la sección 4 decida
  trasladar parte de esa responsabilidad al motor.
- Capital inicial (Tipo B, resuelto): sin cambios necesarios.

## 3. Definir el modelo financiero objetivo

Cuatro preguntas, en orden, cada una condiciona a la siguiente — no se puede saltar a "qué modelo
de sizing elegimos" sin antes responder qué es
una unidad de posición y qué se quiere medir.

### 3.1 — ¿Qué representa una unidad de posición?

Hoy, `Cantidad: 1` significa "1 unidad del activo" — confirmado en código (sección 1: decisión
de la estrategia, sin normalización del motor). El problema: **esa unidad no es equivalente
entre mercados**. Ejemplos ilustrativos del tipo de distorsión, no una lista exhaustiva de
mercados a soportar:

- BTC: 1 unidad puede representar decenas de miles de dólares (confirmado en Fase 2C: retornos
  de hasta 9896% con `Cantidad: 1` sobre `CapitalInicial: 1000`).
- Una acción individual: 1 unidad puede representar pocos dólares — el mismo `Cantidad: 1`
  significaría una exposición radicalmente menor.
- Forex: la convención de "1 lote" tiene una interpretación propia del mercado (tamaño estándar
  distinto a "1 unidad de la divisa"), que no existe hoy en el sistema.

No se decide en este documento cuál convención adoptar — se deja registrado que "unidad de
posición" no puede seguir siendo un `decimal` sin contexto si el sistema aspira a operar sobre
más de un tipo de mercado.

### 3.2 — ¿Qué queremos medir?

Separar explícitamente dos preguntas que hoy se responden con las mismas métricas, sin
distinguirlas:

- **Resultado operativo** (ya disponible, no depende de resolver el modelo financiero):
  porcentaje de operaciones ganadoras, drawdown, rachas, martingala utilizada — todo esto ya
  quedó validado y es interpretable tal como está en Fase 1.5/2C, porque son proporciones y
  conteos, no montos absolutos.
- **Resultado financiero comparable** (bloqueado hasta resolver 3.1 y el modelo de sizing):
  retorno sobre capital, riesgo asumido, exposición máxima, pérdida máxima posible — estos sí
  requieren que la unidad de posición esté normalizada, porque hoy dependen directamente de
  cuánto vale "1 unidad" en cada mercado/dataset.

Esta separación ya estaba implícita en cómo se cerró Fase 2C (retornos etiquetados como "no
comparables" mientras que la distribución de rachas/martingala sí se interpretó) — aquí queda
formalizada como principio explícito del documento, no solo como práctica de esa fase.

### 3.3 — ¿Quién controla el riesgo? **[DECIDIDO]**

Tres filosofías posibles:

| Modelo | Mecanismo | Ventaja | Desventaja |
|---|---|---|---|
| **A — Estrategia decide cantidad** ✅ elegido | La estrategia dice "comprar 1 unidad"; el motor ejecuta tal cual (situación actual) | Mantiene estrategias simples, sin cambio de contrato | Difícil comparar entre mercados — es la causa raíz del problema de Fase 2C |
| B — Motor administra sizing | La estrategia dice "comprar"; el motor calcula cantidad/margen/riesgo según el modelo de la sección 3.1-3.2 | Comparable financieramente entre mercados y experimentos | Cambia el contrato de `IStrategy` |
| C — Híbrido | La estrategia propone dirección + convicción + riesgo máximo; el motor transforma eso en cantidad concreta | Deja la decisión de "qué tan fuerte es la señal" a la estrategia, pero centraliza el cálculo de riesgo en el motor | Requiere definir qué significa "convicción" como concepto nuevo del contrato — ni `SPEC.md` ni el código actual tienen ese concepto |

**Decisión**: Modelo A, como consecuencia directa de la sección 3.4 (Caso 1 — Laboratorio de
estrategias). No se cambia sizing dinámico, riesgo por capital, margen real ni costos reales
todavía — mezclaría dos preguntas distintas ("¿la estrategia tiene ventaja?" vs. "¿el modelo
financiero está bien construido?") que deben resolverse en orden, no simultáneamente. Modelo B/C
quedan como evolución prevista para cuando el sistema pase a Caso 2 (sección 3.4), no descartados
por principio.

### 3.4 — Casos de uso objetivo del sistema **[DECIDIDO — hoja de ruta]**

Tres casos de uso posibles, cada uno implica un modelo financiero distinto:

| Caso | Objetivo | Implica |
|---|---|---|
| **1 — Laboratorio de estrategias** ✅ objetivo actual | "¿Esta lógica genera mejores señales que otra?" | Estrategia controla más, sizing puede ser fijo, importa comportamiento operativo (sección 3.2) |
| 2 — Simulador financiero realista | "¿Qué habría ocurrido con capital real?" | Modelo de riesgo, costos, margen, sizing, restricciones de mercado — requiere resolver la sección 1 (`TasaMargen`/`CostoFriccionReal`) y el Modelo B/C de 3.3 |
| 3 — Plataforma multi-mercado | "Comparar BTC, acciones y forex bajo un mismo marco" | Unidad de posición normalizada (sección 3.1), concepto de instrumento/mercado que hoy no existe en `src/` |

**Decisión**: el sistema persigue **Caso 1 ahora**, con Caso 2 y Caso 3 como evolución prevista,
no como alcance actual. Motivo: el objetivo vigente del proyecto sigue siendo validar que el
motor ejecuta correctamente una estrategia y permite comparar comportamientos bajo distintas
condiciones de mercado — exactamente lo que Fases 1/1.5/2A/2B/2C ya vinieron construyendo.
Responder "cuánto dinero habría generado realmente" (Caso 2) es una pregunta posterior, no
simultánea.

**Consecuencia para el resto de este documento**: la sección 1 (supuestos financieros ocultos)
permanece documentada — `TasaMargen`/`CostoFriccionReal` no se resuelven todavía porque
pertenecen al Caso 2, no al Caso 1. No bloquean el trabajo bajo Caso 1 (Modelo A no depende de
formalizar esos valores), pero **si se mantienen sin resolver** para preservar la posibilidad de
evolucionar a Caso 2 sin rehacer el modelo desde cero — la documentación de la sección 1-2 es,
en sí misma, la preparación para esa evolución futura.

## 4. Impacto sobre `IStrategy` — resuelto para Caso 1, sin cambios ahora

Consecuencia directa de 3.3 (Modelo A elegido): **no se modifica `IStrategy.Observar(DataSlice)`
en esta etapa.** La estrategia sigue emitiendo `Cantidad` tal cual, sin acceso a
`CapitalInicial`/`Cash` del portfolio.

El problema de comparabilidad de retornos entre mercados (Fase 2C) queda explícitamente sin
resolver bajo Caso 1 — es correcto que así sea, porque bajo este caso de uso lo que importa es el
comportamiento operativo (sección 3.2), no el retorno financiero comparable. Cuando el sistema
evolucione a Caso 2, esta sección deberá reabrirse y decidir entre Modelo B (`Observar` recibe
estado del portfolio, rompe la firma actual en todas las implementaciones existentes, incluidas
`EstrategiaTresMosqueteros`/`EstrategiaMhiMayoria` y las fakes de test) o Modelo C (requiere
definir primero campos nuevos de "convicción"/"riesgo máximo" en `OrderRequest`).

## 5. Métricas válidas — oficiales bajo Caso 1, resto diferido

Consecuencia de 3.2 (ya decidida) + 3.4 (Caso 1 elegido): las métricas de **resultado
operativo** quedan confirmadas como las métricas oficiales de esta etapa — ya implementadas y
validadas en Fase 1.5/2C, sin cambios necesarios:

- Cantidad de operaciones, winrate, rachas negativas (con la corrección ya señalada en el cierre
  de Fase 2C: expresar magnitud relativa a la cantidad de operaciones, no solo el máximo
  absoluto), uso de martingala (inicial/M1/M2), exposición máxima, operaciones abiertas al
  cierre — todo esto es información operativa válida bajo Caso 1, no bloqueada.

Las métricas de **resultado financiero comparable** permanecen bloqueadas — no tienen sentido
formalizado bajo Caso 1 (no hay modelo de riesgo/capital normalizado todavía) y pertenecen a
cuando el sistema evolucione a Caso 2. Catálogo mantenido sin decidir cuáles serán oficiales en
ese momento:

- Drawdown máximo (sobre `EquityCurve`, ya disponible en `ResultadoBacktest`).
- Pérdida máxima por operación completa (parcialmente cubierto por `InfoOperacionResuelta`/
  `PerfilEstrategia`, Fase 1.5/2C — falta expresarlo en unidades comparables).
- Racha negativa relativa a la cantidad total de operaciones (no solo el máximo absoluto — 1m
  con ~80.000 operaciones no es comparable a 1D con ~60, señalado en el cierre de Fase 2C).
- Exposición máxima / capital comprometido (ya expuesto en `PerfilMultiTf`, Fase 2C).
- Recuperación después de pérdida — no implementado, candidato nuevo.
- Factor de beneficio (`suma ganancias / suma pérdidas`) — no implementado, cálculo simple sobre
  `Trades` ya disponibles.
- Sharpe/Sortino — requiere tasa libre de riesgo y frecuencia de muestreo definidas; menor
  prioridad dado el volumen de decisiones ya pendientes.

## 6. Impacto esperado bajo Caso 1 (actual) vs. Caso 2/3 (futuro)

- **Estrategias**: sin impacto bajo Caso 1/Modelo A — `EstrategiaTresMosqueteros`/
  `EstrategiaMhiMayoria` y todas las fakes de test siguen funcionando sin cambios. Bajo Caso 2,
  Modelo B/C romperían la firma de `IStrategy.Observar` en todas ellas — costo diferido, no
  eliminado.
- **Contratos**: sin cambios bajo Caso 1. Bajo Caso 2, `TasaMargen`/`CostoFriccionReal` dejarían
  de ser defaults ocultos y `ConfiguracionExperimento` (o un concepto nuevo tipo
  `PerfilInstrumento`/`PerfilBroker`) necesitaría campos nuevos.
- **Métricas/reportes**: las operativas (sección 5) ya están disponibles hoy sin cambio de
  contrato. Las financieras comparables quedarían para cuando se decida si son parte del
  contrato HTTP (`MetricsDto`) o solo de `exploration/laboratorio`.
- **RN-12/CU-15 (`ValidadorCapacidad` desconectado)**: sin relevancia inmediata bajo Caso 1 (el
  capital no es limitante por diseño en esta etapa, consistente con la decisión ya tomada en la
  Ronda 1 de auditoría, documentada en `docs/PENDIENTES.md`). Se vuelve relevante recién bajo
  Caso 2, si el modelo de sizing depende de capital disponible.

## Fuera de alcance de este documento

- Formalizar el valor final de `TasaMargen`/`CostoFriccionReal` — queda documentado (sección 1)
  pero no se resuelve; pertenece a cuando el sistema evolucione a Caso 2.
- Cualquier cambio a `IStrategy`, `ConfiguracionExperimento`, `OrderRequest`, `SPEC.md`, o
  cualquier archivo de `src/` — Caso 1/Modelo A no los requiere.
- Recalcular los resultados de Fase 2C — permanecen etiquetados como "no comparables"; bajo
  Caso 1 esto es aceptable (no es el tipo de resultado que este caso de uso persigue), no un
  defecto pendiente de arreglar ahora.
- Reabrir RN-12/CU-15 (`ValidadorCapacidad`) como fix independiente — relevante solo para Caso 2.
- Diseño técnico detallado de Caso 2/Modelo B/C — corresponde a un documento posterior, cuando
  se decida iniciar esa evolución.

## Estado final de este documento

**Cerrado como hoja de ruta.** Decisiones tomadas: Caso 1 (Laboratorio de estrategias) es el
objetivo actual; Modelo A (estrategia controla cantidad) es la filosofía de riesgo vigente;
métricas operativas (sección 5) son las oficiales de esta etapa. Caso 2 (Simulador financiero
realista) y Caso 3 (Plataforma multi-mercado) quedan como evolución prevista, no como alcance
actual — la sección 1 (supuestos financieros ocultos) permanece documentada precisamente para
que esa evolución futura no tenga que empezar desde cero.

Sin cambios en `src/`, `IStrategy`, contratos ni `SPEC.md`. El trabajo de laboratorio
(`exploration/laboratorio`) continúa bajo el modelo actual (Modelo A), con los resultados
financieros de Fase 2C permaneciendo etiquetados como "no comparables" hasta que el sistema
evolucione a Caso 2.
