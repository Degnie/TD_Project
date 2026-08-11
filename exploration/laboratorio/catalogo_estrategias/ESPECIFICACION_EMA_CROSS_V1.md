# Especificación de la Estrategia EMA Cross V1

Estado: **especificación — Fase 1.6-D del Caso 1**. Documento de diseño, no implementación.
Propósito único: validar que el laboratorio (protocolo, pipeline, catálogo de métricas, análisis
por régimen) generaliza a una estrategia estructuralmente distinta de Tres Mosqueteros/MHI Mayoría
(D-054) — no evaluar si EMA Cross es rentable o "buena". No se modifica `IStrategy`,
`BacktestRunner`, `InfoOperacionResuelta`, `ClasificadorRegimenV1.cs`, `PerfilMultiTf.cs`,
`MetricasPorEscenario.cs` ni ningún módulo congelado de Fases 1.0-1.6-C.

---

## 1. Tipo de estrategia (categoría A, plantilla Fase 1.1)

**Tipo: Tendencia** (a diferencia de Tres Mosqueteros/MHI Mayoría, clasificadas como "Patrón" en
D-003) — genera su señal a partir de un indicador que resume el comportamiento acumulado de varias
velas (media móvil exponencial), no de una regla determinística sobre una posición fija en el
dataset. Esta distinción de tipo, por sí sola, ya es parte de la diversidad estructural que D-054
buscaba (columna "Tendencia" de la tabla comparativa de la decisión).

---

## 2. Reglas exactas

### 2.1 Indicador

Dos EMA (media móvil exponencial) calculadas sobre `Candle.Close`:

```
EMA_corta: periodo PeriodoEmaCorta
EMA_larga: periodo PeriodoEmaLarga  (PeriodoEmaLarga > PeriodoEmaCorta)

EMA[i] = Close[i] * k + EMA[i-1] * (1-k),  k = 2 / (periodo + 1)
Semilla: EMA[periodo-1] = promedio simple de Close[0..periodo-1]
```

Mismo tipo de suavizado exponencial ya usado en el proyecto para Wilder (`ClasificadorAdxExperimental.cs`/`CalibradorUmbralSesgoDI.cs`), pero con la constante de suavizado estándar de EMA (`k = 2/(n+1)`), no la de Wilder (`k = 1/n`) — son fórmulas relacionadas pero distintas, no se reutiliza el código de Wilder tal cual.

### 2.2 Señal de entrada

```
Si EMA_corta[i-1] <= EMA_larga[i-1]  Y  EMA_corta[i] > EMA_larga[i]  → cruce hacia arriba → Buy
Si EMA_corta[i-1] >= EMA_larga[i-1]  Y  EMA_corta[i] < EMA_larga[i]  → cruce hacia abajo  → Sell
En cualquier otro caso → sin señal
```

Se evalúa en cada vela (`Observar`, sin cuadrantes `N%5` — diferencia estructural explícita frente
a las estrategias existentes, D-054). Igual que Tres Mosqueteros/MHI Mayoría, respeta el desfase
RN-13: la señal calculada con `DataSlice` hasta N se ejecuta contra `Velas[N+1]`, sin excepción —
esta restricción es del motor (`BacktestRunner`), no de la estrategia, y no se reinterpreta aquí.

### 2.3 Señal de salida — Opción "cruce contrario" (aprobada por el auditor)

```
Con una posición Buy abierta: cierra en el primer cruce hacia abajo (EMA_corta cruza por debajo de EMA_larga).
Con una posición Sell abierta: cierra en el primer cruce hacia arriba.
```

No hay reintentos, no hay martingala, no hay número máximo de intentos — una posición permanece
abierta hasta que aparece la señal contraria, sin límite de velas. **Consecuencia explícita**: a
diferencia de Tres Mosqueteros/MHI Mayoría (que garantizan resolución en como máximo
`2 + maxMartingalas` ciclos), EMA Cross puede mantener una posición abierta por un número
arbitrario de velas — incluida la posibilidad de que quede `OperacionAbiertaAlCierre = true` con
mucha mayor frecuencia relativa que las estrategias existentes. Esto no es un defecto de la
estrategia, es una propiedad estructural distinta que el pipeline ya sabe manejar (campo existente
desde Fase 1.2), y es justamente el tipo de caso que D-054 busca ejercitar.

### 2.4 Sin señal simultánea de entrada y cierre

Si en la misma vela se cumple a la vez "cerrar la posición actual" y "abrir una posición nueva en
sentido contrario" (cruce hacia abajo mientras hay una posición Buy abierta: cierra Buy Y abre
Sell), ambas órdenes se emiten en la misma bolsa de `Observar()` — mismo tratamiento que ya usan
Tres Mosqueteros/MHI Mayoría para su cierre+reapertura (RN-14: la bolsa completa del ciclo se
evalúa junta), no se inventa una regla nueva.

---

## 3. Parámetros

| Parámetro | Valor propuesto | Origen | Estado |
|---|---|---|---|
| `PeriodoEmaCorta` | 12 | Convención de literatura técnica (EMA 12/26 es el par más citado en cruces de medias) | Propuesto |
| `PeriodoEmaLarga` | 26 | Misma convención | Propuesto |

**Nota metodológica, mismo criterio que D-030 (parámetros del clasificador de régimen)**: 12/26 es
una convención externa (no se ajustó mirando el resultado de esta estrategia sobre el dataset
BTC/USDT) — se marca "Propuesto" y no "congelado", consistente con cómo el proyecto ha tratado
siempre los parámetros no calibrados internamente. A diferencia de `UmbralSesgoDI` (Fase 1.4-B),
esta estrategia no requiere ningún procedimiento de calibración interna — los valores son
literatura pura, sin dato del proyecto involucrado.

`maxMartingalas` **no existe como parámetro de esta estrategia** — se declara explícitamente su
ausencia porque D-054 lo exige como restricción ("no usar martingala, reintentos").

---

## 4. Supuestos — incluyendo un hallazgo verificado contra el código real

**Supuesto heredado, no nuevo**: mismo modelo de posición fija (1 unidad por operación) y capital
inicial ya usado por las estrategias existentes — no se introduce apalancamiento ni tamaño variable.

**Hallazgo nuevo de esta fase — el vocabulario de `InfoOperacionResuelta` asume martingala**: se
verificó contra el código real (`PerfilMultiTf.Medir`, `MetricasPorEscenario.CalcularFila`) que
`GanoInicial`/`GanoM1`/`GanoM2`/`PctResueltasPorMartingala` se calculan a partir de
`MartingalasUsadas == 0/1/2`. EMA Cross no tiene reintentos, por lo que **cada operación ganada
tendrá `MartingalasUsadas = 0` siempre** — no porque "nunca necesitó escalar" (interpretación
válida para Tres Mosqueteros/MHI Mayoría) sino porque el concepto de "escalar" no existe en esta
estrategia. Consecuencia verificable, no evitable sin tocar módulos congelados:

```
GanoInicial == OperacionesGanadas  (siempre, el 100% de las victorias caen aquí)
GanoM1 == GanoM2 == 0               (siempre)
PctResueltasPorMartingala == 0%     (siempre)
```

**Esto no es un error del pipeline ni de esta estrategia** — es exactamente el "supuesto oculto
específico de las estrategias actuales" que la auditoría pidió detectar (criterio de validación de
Fase 1.6-D, punto 5: "no aparecen supuestos ocultos específicos de las estrategias actuales"). Se
documenta aquí como hallazgo esperado, no como fallo: el reporte de Fase 1.6-B (§3) para EMA Cross
mostrará `Recuperación M1/M2: 0%` de forma constante y matemáticamente vacía — no se debe
interpretar como una propiedad interesante de la estrategia. Se propone que el reporte generado
para esta estrategia incluya una nota aclaratoria en la sección "Limitaciones" (heredada, Fase
1.6-B §3 bloque 6) señalando explícitamente que "Resolución de intentos" no aplica a estrategias
sin reintentos — **este documento no modifica `ReporteConsolidadoGenerador.cs` para agregar esa
nota automáticamente**, señala la necesidad, la implementación de cómo detectarlo (¿un campo
booleano `SoportaReintentos`? ¿inferencia por `PctResueltasPorMartingala == 0` en todas las
corridas?) queda para decisión posterior si el auditor lo considera necesario — no se resuelve
en este documento porque tocaría un módulo ya congelado (D-051/Fase 1.6-B) fuera del alcance de
simplemente agregar una estrategia nueva.

**Ninguna lógica específica de BTC/USDT**: el cálculo de EMA es genérico sobre cualquier serie de
`Close` — mismo dataset ya congelado se usa por consistencia experimental, no porque la estrategia
lo requiera.

**Ningún parámetro ajustado mirando resultados**: 12/26 es convención externa (sección 3) — D-016
(no selección retrospectiva) se respeta de la misma forma que para el clasificador de régimen.

---

## 5. Configuración experimental

Misma `ConfiguracionExperimento` ya usada por las estrategias existentes: `CapitalInicial = 1000m`,
mismos 6 timeframes del dataset congelado (`1m, 5m, 15m, 1h, 4h, 1D`), mismo protocolo de
determinismo (2 corridas, comparación campo por campo) ya implementado en `EjecutorProtocolo`.

`Warmup`: a diferencia de las estrategias existentes (que no requieren historial previo para
generar su primera señal, solo esperan `N%5`), EMA Cross necesita al menos `PeriodoEmaLarga` velas
de historial antes de que la EMA larga tenga un valor válido. Esto no requiere ningún parámetro
nuevo en `ConfiguracionExperimento` (que ya tiene `Warmup`, sin usar hasta ahora) — se propone
`Warmup = PeriodoEmaLarga` (26), dejando que las primeras 26 velas no generen señal (EMA calculada
pero sin suficiente historial para comparar cruces de forma confiable) en vez de forzar una
comparación con datos insuficientes.

---

## 6. Métricas esperadas

Mismo catálogo heredado de Fase 1.2 (§4.1-4.3) — ninguna métrica nueva. Expectativa **estructural**
(no de resultado, D-054 lo prohíbe explícitamente): dado que no hay reintentos,
`OperacionesCompletadas` para EMA Cross en un timeframe dado será menor que para Tres
Mosqueteros/MHI Mayoría en el mismo timeframe (las señales de cruce EMA son mucho menos frecuentes
que una señal evaluada cada 5 velas) — esto valida indirectamente que Fase 1.3 (D-010, tamaño de
muestra obligatorio) es necesaria: comparar "eficiencia operacional" entre estrategias con órdenes
de magnitud distintos de operaciones sería exactamente el tipo de comparación sin contexto que el
proyecto lleva prohibiendo desde D-010.

---

## 7. Escenarios de fallo

- **Cero cruces en un timeframe** (ej. mercado sin tendencia clara en 1D, con pocas velas): mismo
  tratamiento ya definido en Fase 1.2 §4.4 para "cero operaciones completadas" — no es un error del
  pipeline, se reporta como tal.
- **Posición abierta al cierre del dataset con alta probabilidad** (sección 2.3): ya cubierto por
  `OperacionAbiertaAlCierre`, sin cambios necesarios.
- **`EstadoBacktest != Success`**: mismo tratamiento ya implementado en `EjecutorProtocolo` (Fase
  1.6-C) — no requiere ningún caso especial para esta estrategia.

---

## Fuera de alcance (respetado)

No se implementa código en este documento. No se modifica ningún módulo de Fases 1.0-1.6-C. No se
evalúa si EMA Cross es rentable, competitiva o recomendable — únicamente si el pipeline la acepta
sin cambios. No se resuelve cómo señalar automáticamente en el reporte que "Resolución de
intentos" no aplica a estrategias sin martingala (sección 4) — se documenta como hallazgo,
decisión de implementación diferida.

---

## Criterio de cierre de la especificación EMA Cross V1

- ✓ Tipo de estrategia clasificado (Tendencia, D-003), explícitamente distinto de "Patrón" (sección
  1).
- ✓ Reglas exactas de entrada/salida definidas, con la Opción "cruce contrario" ya aprobada por el
  auditor (sección 2), respetando RN-13/RN-14 sin reinterpretarlas.
- ✓ Parámetros definidos como "Propuesto" (convención externa, no calibrado internamente) — mismo
  criterio que D-030 (sección 3).
- ✓ Supuestos documentados, incluyendo el hallazgo verificado de que el vocabulario de martingala
  en `InfoOperacionResuelta`/`PerfilMultiTf` no aplica a esta estrategia — señalado, no ocultado
  (sección 4).
- ✓ Configuración experimental definida, incluyendo el primer uso real de `Warmup` (ya existente en
  `ConfiguracionExperimento`, sin usar hasta ahora) — sección 5.
- ✓ Expectativa estructural de métricas (menor volumen de operaciones) documentada sin declarar
  ninguna expectativa de resultado financiero (sección 6).
- ✓ Escenarios de fallo cubiertos por mecanismos ya existentes, sin necesidad de casos especiales
  (sección 7).
- ⏳ Auditoría aprueba la especificación — pendiente de confirmación explícita antes de implementar
  `EstrategiaEmaCross.cs` y `catalogo_estrategias/EMA_CROSS.md`.
