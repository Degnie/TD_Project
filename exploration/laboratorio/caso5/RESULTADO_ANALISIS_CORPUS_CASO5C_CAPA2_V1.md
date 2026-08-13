# Resultado — Análisis Descriptivo del Corpus (Caso 5C Capa 2)

Estado: **documento de resultado — describe evidencia real, no propone ni implementa recomendación**.
Ejecuta `analisis_corpus/` (D-123, `ESPECIFICACION_IMPLEMENTACION_CASO5C_CAPA2_V1.md`) sobre el
corpus oficial declarado en `MANIFIESTO_CORPUS_CASO5C_V1.json` (49 comparaciones: 6 V1 + 25 V2 + 18
Sub-campaña D). Responde exclusivamente **"¿qué evidencia contiene el corpus?"** — no responde
"¿qué debería elegir el usuario?". D-118/D-119/D-120 permanecen intactas, sin activar.

**Fuente**: salida real de `analisis_corpus/ProgramAnalisisCorpus.cs` (11/11 pruebas P1-P9/P4b/P8b
pasaron antes de calcular este resumen), leyendo únicamente las 147 filas (49 comparaciones × 3
gestores) declaradas en el manifiesto — ninguna carpeta de las 59 excluidas (desarrollo/reintentos)
participó en ningún cálculo.

**Corrección aplicada durante esta ejecución**: `AnalisisDescriptivo.Resumir` contaba
`DatasetInexistente_ParaCorpusDeFallo` (la comparación deliberada de evidencia parcial, sub-campaña
C) como un tercer "período temporal" en `Limitaciones`. Corregido para contar solo datasets con al
menos una fila con métrica numérica real — el corpus tiene 2 períodos reales (`BTCUSDT_2024-01-02_
2025-01-02`, `BTCUSDT_2022-01-01_2023-01-01`), no 3. Nueva prueba P8b cubre este caso. La cobertura
cruda (`ComparacionesPorDataset`) sigue mostrando los 3 valores de `NombreDataset` sin filtrar —
solo `Limitaciones` distingue "período real" de "caso de fallo deliberado".

---

## 1. Cobertura del corpus

**147 filas** (49 comparaciones × 3 gestores), **0 carpetas ignoradas** — el manifiesto resolvió
correctamente las 49 combinaciones declaradas.

**Por estrategia**:

| Estrategia | Filas |
|---|---|
| Tres Mosqueteros | 39 |
| Ema Cross | 36 |
| ZScore Reversion | 18 |
| Neutral | 18 |
| Volumen Breakout | 18 |
| Mhi Mayoria | 18 |

Tres Mosqueteros/Ema Cross tienen más filas que el resto porque el corpus incluye, además de su
comparación original (V1), dos repeticiones deliberadas de la misma matriz dentro de V2 (una
identificada como "V1 repetida internamente", otra como "sub-campaña B" — ambas documentadas en
`AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V2.md` §1 como evidencia de reproducibilidad, no como error de
conteo) — 3 comparaciones × 3 timeframes × 3 gestores = 27, más las 12 de Sub-campaña D = 39.
ZScore Reversion/Neutral/Volumen Breakout/Mhi Mayoria solo aparecen una vez por período (V2 + Sub-
campaña D): 2 × 3 × 3 = 18.

**Por timeframe**: `15m`: 48 · `1h`: 48 · `1D`: 51 (el `1D` extra corresponde a la comparación de
evidencia parcial de la sub-campaña C, que solo existe en `1D`).

**Por gestor**: los 3 gestores (`fixed-fractional:v1:riesgo=0.1`, `fixed-risk:v1:monto=50`,
`volatility-sizing:v1:ventana=20:base=0.1:desviacionReferencia=2`) tienen exactamente 49 filas cada
uno — cobertura simétrica, ningún gestor sobre- o sub-representado.

**Por dataset (período)**:

| Dataset | Filas |
|---|---|
| `BTCUSDT_2024-01-02_2025-01-02` | 90 |
| `BTCUSDT_2022-01-01_2023-01-01` | 54 |
| `DatasetInexistente_ParaCorpusDeFallo` | 3 |

El tercer valor no es un período — es la evidencia parcial deliberada de la sub-campaña C (3 filas
= 1 comparación × 3 gestores, todas `Estado: Incomplete`, sin métricas).

---

## 2. Distribuciones (agrupadas por gestor, todos los períodos)

Estadística descriptiva simple (n, mínimo, máximo, media, mediana) por gestor, sobre las filas con
métrica disponible (`Success`). n=48 para la mayoría de métricas (147 filas − 3 sin métrica de la
sub-campaña C − 96 de `ProfitFactor` con `null` en corridas sin actividad de ZScore Reversion, ver
§4).

| Métrica | Gestor | n | Mín | Máx | Media | Mediana |
|---|---|---|---|---|---|---|
| PnLTotal | fixed-fractional | 48 | −242.01 | 591.72 | −2.44 | −10.73 |
| PnLTotal | fixed-risk | 48 | −564.60 | 578.22 | 2.72 | −13.15 |
| PnLTotal | volatility-sizing | 48 | −22.93 | 13.20 | 0.45 | −0.02 |
| DrawdownMaximoPct | fixed-fractional | 48 | 0 | 0.9999999999999999965796659887 | 0.67 | 0.81 |
| DrawdownMaximoPct | fixed-risk | 48 | 0 | 20.04 | 4.46 | 0.94 |
| DrawdownMaximoPct | volatility-sizing | 48 | 0 | 0.75 | 0.12 | 0.01 |
| ProfitFactor | fixed-fractional | 42 | 0.54 | 2.43 | 1.04 | 0.94 |
| ProfitFactor | fixed-risk | 42 | 0.48 | 2.73 | 1.09 | 0.98 |
| ProfitFactor | volatility-sizing | 42 | 0.43 | 4.50 | 1.21 | 1.00 |
| ExposicionMaxima | fixed-fractional | 48 | 0 | 129.03 | 91.13 | 100.05 |
| ExposicionMaxima | fixed-risk | 48 | 0 | 59.09 | 46.26 | 52.19 |
| ExposicionMaxima | volatility-sizing | 48 | 0 | 36.03 | 7.43 | 3.68 |
| CashFinal | fixed-fractional | 48 | ≈0 | 1436.64 | 403.77 | 176.67 |
| CashFinal | fixed-risk | 48 | −19038.26 | 1250.33 | −3440.50 | 8.90 |
| CashFinal | volatility-sizing | 48 | 247.91 | 1001.07 | 879.89 | 987.09 |
| EquityFinal | fixed-fractional | 48 | ≈0 | 1531.42 | 430.27 | 193.54 |
| EquityFinal | fixed-risk | 48 | −19038.26 | 1303.63 | −3402.72 | 60.97 |
| EquityFinal | volatility-sizing | 48 | 247.91 | 1001.14 | 881.10 | 987.79 |

**Observación factual sobre el rango**: `DrawdownMaximoPct` de `fixed-fractional` alcanza
`0.9999999999999999965796659887` (≈100%) en su máximo, mientras `volatility-sizing` no supera
`0.7520928011507303088120610203` en ninguna fila del corpus — un hecho verificable sobre el rango
observado, no una evaluación de qué gestor es preferible.

**Valores completos con precisión decimal íntegra** quedan en la salida cruda del programa
(`analisis_corpus/`, ejecutable, reproducible bajo el mismo manifiesto) — esta tabla los trunca a 2
decimales solo por legibilidad del documento.

---

## 3. Comparación entre períodos (descriptiva)

Para `DrawdownMaximoPct` y `PnLTotal`, por gestor, comparando `BTCUSDT_2024-01-02_2025-01-02`
(n=30) contra `BTCUSDT_2022-01-01_2023-01-01` (n=18) — presencia/ausencia de valores en cada
período, sin ordenar ni concluir cuál es "mejor".

| Métrica | Gestor | Período | n | Mín | Máx | Media | Mediana |
|---|---|---|---|---|---|---|---|
| DrawdownMaximoPct | fixed-fractional | 2024-2025 | 30 | 0 | 0.9999999999999999961370391383 | 0.68 | 0.81 |
| DrawdownMaximoPct | fixed-fractional | 2022-2023 | 18 | 0 | 0.9999999999999999965796659887 | 0.64 | 0.75 |
| PnLTotal | fixed-fractional | 2024-2025 | 30 | −203.41 | 591.72 | 23.07 | −10.73 |
| PnLTotal | fixed-fractional | 2022-2023 | 18 | −242.01 | 183.42 | −44.95 | −19.87 |
| DrawdownMaximoPct | fixed-risk | 2024-2025 | 30 | 0 | 19.98 | 4.63 | 0.94 |
| DrawdownMaximoPct | fixed-risk | 2022-2023 | 18 | 0 | 20.04 | 4.18 | 0.76 |
| PnLTotal | fixed-risk | 2024-2025 | 30 | −564.60 | 332.36 | −3.79 | −45.98 |
| PnLTotal | fixed-risk | 2022-2023 | 18 | −291.45 | 578.22 | 13.59 | −0.77 |
| DrawdownMaximoPct | volatility-sizing | 2024-2025 | 30 | 0 | 0.41 | 0.09 | 0.01 |
| DrawdownMaximoPct | volatility-sizing | 2022-2023 | 18 | 0 | 0.75 | 0.17 | 0.03 |
| PnLTotal | volatility-sizing | 2024-2025 | 30 | −5.38 | 6.86 | 1.02 | 0 |
| PnLTotal | volatility-sizing | 2022-2023 | 18 | −22.93 | 13.20 | −0.50 | −0.41 |

**Observaciones factuales**:
- Los 3 gestores tienen valores tanto positivos como negativos de `PnLTotal` en **ambos** períodos —
  ningún gestor es exclusivamente positivo o negativo en ninguno de los dos rangos.
- El máximo de `DrawdownMaximoPct` de `fixed-fractional` es ≈100% en ambos períodos (0.99999...96 vs
  0.99999...96) — el mismo patrón cualitativo (drawdown extremo alcanzable) aparece en los dos
  rangos temporales.
- El rango de `PnLTotal` de `volatility-sizing` es más amplio en 2022-2023 (−22.93 a 13.20) que en
  2024-2025 (−5.38 a 6.86) — una diferencia observable entre períodos, sin evaluar si es deseable.

---

## 4. Casos atípicos

**Ausencia de operaciones — ZScore Reversion**: en ambos períodos, las 9 corridas `Success` (3
timeframes × 3 gestores) muestran `PnLTotal=0` — la estrategia no generó ninguna operación bajo los
parámetros usados (`ventana=5, umbralEntrada=2.0, umbralSalida=0.5`), ni en 2024-2025 ni en
2022-2023. Hecho ya documentado en auditorías previas (V2, diversidad temporal), confirmado aquí de
forma automatizada sobre el corpus completo.

**Estados incompletos — sub-campaña C**: 1 comparación (`Tres Mosqueteros / 1D /
DatasetInexistente_ParaCorpusDeFallo`), 3 filas, todas `Estado: Incomplete`, sin métricas —
evidencia parcial deliberada (dataset inexistente, generada intencionalmente para representar este
tipo de evidencia en el corpus), no una corrida degradada.

**Drawdowns extremos (`DrawdownMaximoPct ≥ 99%`)**: detectados en **ambos períodos**, siempre en
timeframes cortos (`15m`/`1h`, nunca `1D`), y siempre con el gestor `fixed-fractional`, con
`fixed-risk` mostrando valores igualmente altos (aunque no ≥99%, sí de un orden de magnitud
similar o mayor) en las mismas combinaciones estrategia/timeframe:

| Estrategia | Timeframe | Período | Gestor con Drawdown≥99% |
|---|---|---|---|
| Tres Mosqueteros | 15m | 2024-2025 | fixed-fractional |
| Tres Mosqueteros | 1h | 2024-2025 | fixed-fractional |
| Ema Cross | 15m | 2024-2025 | fixed-fractional |
| Neutral | 15m | 2024-2025 | fixed-fractional |
| Neutral | 1h | 2024-2025 | fixed-fractional |
| Mhi Mayoria | 15m | 2024-2025 | fixed-fractional |
| Mhi Mayoria | 1h | 2024-2025 | fixed-fractional |
| Tres Mosqueteros | 15m | 2022-2023 | fixed-fractional |
| Tres Mosqueteros | 1h | 2022-2023 | fixed-fractional |
| Ema Cross | 15m | 2022-2023 | fixed-fractional |
| Neutral | 15m | 2022-2023 | fixed-fractional |
| Neutral | 1h | 2022-2023 | fixed-fractional |
| Mhi Mayoria | 15m | 2022-2023 | fixed-fractional |
| Mhi Mayoria | 1h | 2022-2023 | fixed-fractional |

El patrón (drawdown extremo en timeframes cortos con `fixed-fractional`, replicado en ambos
períodos y en 5 de las 6 estrategias) coincide con lo ya señalado en `AUDITORIA_CORPUS_
COMPARATIVO_CASO5C_V2.md` §3 y `AUDITORIA_DIVERSIDAD_TEMPORAL_CASO5C_V1.md` §3 — aquí queda
confirmado de forma automatizada y exhaustiva sobre el corpus completo (147 filas), no solo sobre
las muestras revisadas manualmente en esas auditorías.

**Volumen Breakout y ZScore Reversion no muestran drawdowns ≥99%** en ninguna de sus filas del
corpus — hecho observable, no una conclusión sobre robustez.

---

## Fuera de alcance de este documento

No se declara ningún gestor "ganador". No se ordena a los gestores por ninguna métrica. No se
calcula ningún score compuesto. No se recomienda ninguna configuración. No se ajusta ningún
parámetro. No se concluye robustez general de ningún gestor o estrategia. No se extrapola ningún
patrón a instrumentos distintos de `BTCUSDT` — el corpus contiene un único instrumento (§2 de
`PROPUESTA_CASO5C_CAPA2_V1.md`), limitación que este documento no resuelve ni intenta resolver.

---

## Conclusión

El corpus de 49 comparaciones (147 filas) tiene cobertura completa y simétrica entre estrategias,
timeframes y gestores (con la excepción esperada de Tres Mosqueteros/Ema Cross, que tienen más
evidencia por incluir repeticiones deliberadas de reproducibilidad). Dos patrones observados por
primera vez en auditorías anteriores —ausencia de actividad de ZScore Reversion y drawdown extremo
de `fixed-fractional` en timeframes cortos— se confirman aquí de forma automatizada sobre la
totalidad del corpus, en ambos períodos temporales disponibles. El corpus sigue limitado a un único
instrumento (`BTCUSDT`); ningún patrón aquí descrito puede extrapolarse fuera de ese instrumento.
Este documento no evalúa si la evidencia acumulada es suficiente para abrir una etapa de
recomendación — esa evaluación, junto con D-118/D-119/D-120 (que permanecen intactas), queda para
una decisión posterior explícita.
