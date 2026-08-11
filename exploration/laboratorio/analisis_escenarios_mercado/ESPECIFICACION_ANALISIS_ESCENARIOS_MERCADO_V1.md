# Especificación del Clasificador de Escenarios de Mercado V1

Estado: **especificación — Fase 1.4 del Caso 1**. Documento de diseño, no implementación. No se
modifica `BacktestRunner`, `IStrategy`, DTOs, estrategias, `AnalizadorOperacional.cs` (Fase 1.2) ni
`PerfilMultiTimeframe.cs` (Fase 1.3).

Resuelve D-006, pendiente desde Fase 1.2 (`ESPECIFICACION_ANALIZADOR_OPERACIONAL_V1.md §4.5`) y
reafirmada en Fase 1.3 (D-013).

---

## 1. Objetivo

Definir un clasificador de régimen de mercado (alcista/bajista/lateral) que sea **independiente de
la estrategia**, para poder responder:

> "Dado este criterio objetivo de mercado, ¿cómo se comportó la estrategia dentro de ese régimen?"

y no:

> "La estrategia funciona porque elegimos este escenario."

Esta distinción es la restricción central de la fase: el orden de trabajo es obligatorio y no se
invierte —

```
Dataset
    ↓
Clasificador de régimen   (no conoce ninguna estrategia)
    ↓
Segmentos de mercado       (fijados ANTES de mirar resultados de ninguna estrategia)
    ↓
Evaluación de estrategia   (las estrategias ya evaluadas en Fase 2C se miden DENTRO de segmentos
                             ya fijados, nunca al revés)
```

---

## 2. Por qué el clasificador no puede depender de la estrategia — riesgo de selección retrospectiva

**Selección retrospectiva** (conocida en la literatura como *data snooping* o *overfitting por
segmentación*): definir o ajustar los límites de un "escenario favorable" **después** de ver qué
tramos le fueron bien a una estrategia, de modo que el escenario termina siendo, por construcción,
el conjunto de tramos donde esa estrategia ganó. El resultado parece validar la estrategia, pero en
realidad no prueba nada — cualquier estrategia parece buena si se le permite elegir su propio
subconjunto de evaluación a posteriori.

**Por qué el proyecto ya es vulnerable a esto sin quererlo**: los generadores sintéticos de Fase 1.5
(`GeneradorTendencia.cs`, `GeneradorLateral.cs`, revisados para este documento) etiquetan el régimen
**por construcción** — el código sabe que generó "tendencia" porque él mismo fijó la pendiente antes
de generar las velas. Esto es válido para Fase 1.5 (el régimen se conoce de antemano porque el
dataset se diseñó así), pero **no es un clasificador**: no puede aplicarse a un dataset real ya
congelado (como `BTCUSDT_2024-01-02_2025-01-02_1m.csv`), donde nadie "programó" la tendencia — hay
que detectarla a partir del precio observado. Fase 1.4 construye ese clasificador que falta.

**Salvaguarda estructural obligatoria**: el clasificador se implementa y se congela (con su propio
hash, igual que un dataset — sección 6) **antes** de ejecutar cualquier comparación entre régimen y
comportamiento de estrategia. Una vez fijados los segmentos de un dataset, no se permiten ajustes al
criterio de clasificación motivados por el resultado de una estrategia específica dentro de esos
segmentos. Si el criterio necesita cambiar, se congela una nueva versión (`v2`) y se re-clasifica el
dataset completo desde cero — nunca se ajustan límites puntuales para favorecer un resultado ya
observado.

---

## 3. Qué datos necesita el clasificador

**Entrada exclusiva**: la serie de velas ya congelada (`VelaCruda`/`Candle`, cualquier timeframe del
dataset base o derivado, `datasets/reales/BTCUSDT/**`). Ningún dato de ninguna estrategia,
`InfoOperacionResuelta`, `PerfilMultiTf` ni `ReporteOperacional` entra al clasificador — esta es la
garantía estructural de independencia (sección 2).

| Dato | Fuente | Ya disponible |
|---|---|---|
| OHLCV por vela | `datasets/reales/BTCUSDT/{tf}/*.csv` | Sí (Fase 2A/2B) |
| Timestamp UTC, calendario de agregación | Metadata del timeframe (`metadata.json`) | Sí (Fase 2A/2B) |
| Hash del dataset/timeframe de origen | `metadata.json` | Sí — el clasificador debe registrar contra qué hash se congeló su salida (sección 6) |

**No requiere**: indicadores técnicos de terceros, datos de volumen fuera del ya incluido en el CSV,
ni ninguna fuente externa al dataset ya congelado en Fase 2A/2B.

---

## 4. Definición matemática de cada escenario

**Decisión de diseño propuesta** (requiere aprobación explícita antes de implementar — ver sección
"Decisiones pendientes"): clasificación por **pendiente normalizada de una ventana deslizante**,
el criterio más simple que cumple "objetivo, medible, sin conocer la estrategia".

Para una ventana de `N` velas consecutivas de un timeframe dado:

```
PendienteNormalizada = (Close_final - Close_inicial) / Close_inicial
RangoRelativo         = (Max(High) - Min(Low)) / Close_inicial   (dispersión dentro de la ventana)
```

Clasificación propuesta por umbrales sobre `PendienteNormalizada`:

| Condición | Escenario |
|---|---|
| `PendienteNormalizada > +Umbral` | Alcista |
| `PendienteNormalizada < -Umbral` | Bajista |
| `-Umbral ≤ PendienteNormalizada ≤ +Umbral` | Lateral |

**El valor de `Umbral` y el tamaño de ventana `N` NO se definen en este documento** — son, igual que
D-005 (dependencia de martingala) y D-012 (muestra reducida), parámetros que si se fijan mirando el
dataset de BTC/USDT ya conocido, arriesgan ajustarse al comportamiento ya observado en vez de ser un
criterio objetivo. Ver "Decisiones pendientes".

**Alternativas descartadas para esta fase** (registradas para referencia futura, no elegidas ahora
por mayor complejidad sin beneficio claro sobre el criterio simple): regresión lineal con R² sobre
la ventana, medias móviles cruzadas, ADX. Quedan como posible evolución si el criterio de pendiente
normalizada resulta insuficiente en la práctica.

---

## 5. Cómo tratar zonas ambiguas

Una ventana puede caer justo en el borde del umbral, o el `RangoRelativo` puede ser alto mientras la
`PendienteNormalizada` es baja (mercado volátil pero sin dirección neta — no es lo mismo que
"lateral tranquilo").

**Regla obligatoria**: el clasificador expone una categoría explícita **"Ambiguo"**, distinta de
"Lateral". No se fuerza toda zona sin tendencia clara a cabalgar en "Lateral" — mismo principio que
ya rige en el motor de dominio: una vela doji sin color claro no se fuerza a Buy o Sell (ver
`EstrategiaTresMosqueteros.cs`, "vela doji → sin señal"), y una zona sin régimen claro no se fuerza
a un escenario.

Criterio propuesto (requiere aprobación, mismo motivo que sección 4): si `RangoRelativo` supera un
segundo umbral independiente mientras `PendienteNormalizada` está cerca de cero, la ventana se
clasifica "Ambiguo" en vez de "Lateral" — evita mezclar "sin tendencia y tranquilo" con "sin
tendencia y muy volátil" bajo la misma etiqueta.

**Ninguna ventana se descarta silenciosamente**: toda vela pertenece a exactamente una categoría
(Alcista/Bajista/Lateral/Ambiguo), igual que la política de velas parciales en Fase 2B (incluir y
marcar, nunca descartar sin registro).

---

## 6. Cómo evitar selección retrospectiva — proceso obligatorio

Extiende la salvaguarda de la sección 2 en un proceso concreto y verificable:

1. **Congelar el criterio primero**: umbral, tamaño de ventana y regla de ambigüedad se fijan y se
   documentan (versión `v1`, análoga a `aggregationVersion` de Fase 2B) **antes** de ejecutar la
   clasificación sobre el dataset real.
2. **Clasificar el dataset completo, no un subconjunto**: la clasificación corre sobre el dataset
   íntegro (todo el rango 2024-01-02 a 2025-01-02), nunca sobre un tramo elegido a mano.
3. **Registrar hash del criterio + hash del resultado**: igual que un dataset derivado (Fase 2B), la
   salida del clasificador (qué vela pertenece a qué escenario) se congela con su propio
   `metadata.json` (hash del dataset de origen, versión del criterio, hash de la salida).
4. **Ninguna estrategia se evalúa antes de que el paso 3 esté completo y congelado.** Evaluar una
   estrategia dentro de un escenario antes de congelar el criterio de ese escenario invalidaría la
   independencia — sería exactamente la selección retrospectiva que la sección 2 prohíbe.
5. **Cambios al criterio = nueva versión, no ajuste in-place**: si en el futuro se decide que el
   umbral necesita cambiar, se congela `v2` con su propio hash y se re-clasifica desde cero — nunca
   se edita `v1` después de haberla usado para evaluar una estrategia.

---

## 7. Métricas heredadas del analizador operacional

Ninguna métrica nueva. Una vez que el dataset está segmentado por escenario (pasos 1-3 de la
sección 6) y una estrategia ya fue evaluada por backtest en Fase 2C sobre ese mismo timeframe, el
análisis por escenario **agrupa** las operaciones ya resueltas (`InfoOperacionResuelta`) según en
qué segmento de mercado cayó cada una, y aplica exactamente el mismo catálogo de Fase 1.2:

| Métrica heredada | Fuente (Fase 1.2) |
|---|---|
| Eficiencia operacional | `ResultadoGeneral.EficienciaOperacionalPct` |
| Resolución de intentos | `ResolucionDeIntentos.*` |
| Peores escenarios (rachas, exposición) | `PeoresEscenarios.*` |
| Tamaño de muestra obligatorio (D-010, Fase 1.3) | Igual regla: toda comparación por escenario debe mostrar cuántas operaciones cayeron en ese escenario |

**Diferencia respecto a Fase 1.3**: en vez de agrupar por timeframe, se agrupa por escenario de
mercado — mismo catálogo de métricas, distinto criterio de agrupación (exactamente lo que
`ESPECIFICACION_ANALIZADOR_OPERACIONAL_V1.md §4.5` ya anticipaba).

**Punto de atención heredado de D-012**: los escenarios "Bajista" o "Ambiguo" pueden tener muy pocas
operaciones dentro de un timeframe corto si el dataset tuvo, por ejemplo, mayoría de tramos
alcistas — la regla de D-010 (mostrar tamaño de muestra siempre) es aquí más crítica que en Fase
1.3, porque la cantidad de velas por escenario no es controlada por el usuario, es una propiedad del
dataset real ya congelado.

---

## 8. Cómo presentar resultados sin ranking financiero

Misma regla que Fase 1.3 (sección 5 de esa especificación), extendida a escenarios: ninguna
comparación entre "Estrategia en régimen Alcista" vs. "Estrategia en régimen Bajista" ordena por
`RetornoPct`/`EquityFinal`, ni concluye "la estrategia es mejor en mercado alcista" en términos
financieros. Se aplican las mismas categorías de interpretación permitida/prohibida del catálogo de
Fase 1.2 (sección 6 de esa especificación):

Interpretación permitida:
```
La estrategia completó 1,200 operaciones en tramos clasificados como Alcistas (eficiencia
operacional 88%) y 300 en tramos Bajistas (eficiencia operacional 85%).
```

Interpretación prohibida:
```
La estrategia es mejor en mercados alcistas.          [PROHIBIDO — no declara ganador, D-014
                                                         extendido a escenarios]
La estrategia genera más dinero en tendencia.          [PROHIBIDO — ranking financiero]
```

---

## Decisiones registradas por auditoría (2026-08-11)

**D-016 — Prohibición de clasificación por conocimiento de construcción**: ✅ Aprobado. Formaliza
el hallazgo de la sección 2: un clasificador de mercado no puede utilizar información que solo
existe porque el dataset fue generado sintéticamente. Debe funcionar exclusivamente con OHLC(V) y
la información disponible en el momento del análisis — nunca con etiquetas internas, parámetros del
generador, ni conocimiento posterior del futuro. Regla permanente para cualquier clasificador del
laboratorio, no solo el de esta fase.

**D-017 — Versionado del clasificador**: ✅ Aprobado. Un cambio en indicador, ventana, umbral,
fórmula o tratamiento de zonas ambiguas crea una nueva versión (`ClasificadorMercado v1` →
`ClasificadorMercado v2`). Extiende el paso 5 de la sección 6 a regla formal: nunca se modifica un
criterio ya utilizado para evaluar una estrategia.

**D-018 — Umbral numérico del régimen**: ⏳ Pendiente. Correctamente no fijado (sección 4). No debe
elegirse mirando BTC/USDT, resultados conocidos ni estrategias existentes. Orden obligatorio:
Definir método → Aplicar al dataset → Observar resultados (nunca Observar resultados → Ajustar
método).

**D-019 — Tamaño de ventana**: ⏳ Pendiente. Correctamente abierto (sección 4, punto 3 de
"Decisiones pendientes" original). La ventana afecta sensibilidad, cantidad de cambios de régimen,
clasificación lateral y retraso de detección — debe definirse experimentalmente, no a priori.

**D-020 — Zona no clasificable ("Indeterminado")**: registrada como decisión futura, no
implementada ahora. Extiende la categoría "Ambiguo" (sección 5): en mercados reales pueden existir
periodos donde la evidencia no es suficiente para clasificar con confianza (baja volatilidad,
transición, ruido) — distinto de "Ambiguo" (que sí tiene una definición matemática propuesta en
sección 5, aunque con umbral pendiente). "Indeterminado" cubriría el caso de evidencia insuficiente,
no solo de señal contradictoria. No se define su criterio en esta fase.

---

## Fuera de alcance (respetado)

No se implementa el clasificador ni ningún componente de código en esta fase. No se modifica
`BacktestRunner`, `IStrategy`, DTOs, estrategias, `AnalizadorOperacional.cs` ni
`PerfilMultiTimeframe.cs`. No se ejecuta clasificación sobre el dataset real. No se calcula ranking
financiero entre escenarios. Este documento es la única salida de esta fase.

---

## Criterio de cierre de Fase 1.4

- ✓ Definido por qué el clasificador debe ser independiente de la estrategia y qué riesgo concreto
  (selección retrospectiva) previene (sección 2), con referencia a una vulnerabilidad real ya
  presente en el proyecto (generadores sintéticos de Fase 1.5, que clasifican por construcción y no
  sirven para datos reales).
- ✓ Datos de entrada del clasificador identificados, con garantía estructural de que ningún dato de
  estrategia entra a él (sección 3).
- ✓ Definición matemática propuesta para cada escenario, con umbrales explícitamente no fijados
  para evitar ajuste retrospectivo (sección 4).
- ✓ Tratamiento de zonas ambiguas definido como categoría propia, no forzada a Lateral (sección 5).
- ✓ Proceso obligatorio de 5 pasos para evitar selección retrospectiva, con congelamiento y
  versionado del criterio (sección 6).
- ✓ Métricas heredadas del analizador operacional identificadas, sin cálculos nuevos fuera del
  criterio de clasificación (sección 7).
- ✓ Regla de presentación sin ranking financiero, extendida de Fase 1.3 (sección 8).
- ✅ Auditoría aprueba la especificación — **Diseño aprobado** (2026-08-11). D-016 y D-017
  incorporadas como decisiones de diseño aprobadas. D-018, D-019 y D-020 quedan pendientes para una
  fase posterior sin bloquear el cierre. Implementación de código **no autorizada todavía**: antes
  de programar queda pendiente una decisión metodológica adicional — seleccionar la familia de
  clasificador candidata (ver documento de selección de enfoque, siguiente entregable requerido
  antes de tocar código).

**D-021 — Selección de familia de clasificador** (2026-08-11): ✅ Aprobado. La selección del
clasificador oficial será posterior a una comparación experimental de candidatos, no una elección
directa entre las opciones A/B/C descritas en la sección 4. Motivo: evitar introducir sesgo
retrospectivo y garantizar independencia respecto a resultados conocidos — elegir un candidato
directamente introduciría una decisión irreversible antes de tener evidencia comparativa. Se abre
una subfase previa, **Fase 1.4-A — Evaluación de clasificadores candidatos**, ver
`EVALUACION_CLASIFICADORES_REGIMEN_V1.md`. Candidatos mantenidos para esa evaluación: A (Medias
móviles/EMA, 🟡 candidato secundario — riesgo de fuerte dependencia del timeframe), B (ADX+DI, 🟢
candidato prioritario), C (Retorno + Volatilidad, 🟢 candidato prioritario, ya esbozado en la
sección 4 de este documento como `PendienteNormalizada`/`RangoRelativo`).

**Estado de Fase 1.4**: ⏳ En diseño comparativo — el diseño conceptual de esta especificación
queda aprobado, pero la implementación de código no procede hasta que Fase 1.4-A congele un
clasificador oficial.
