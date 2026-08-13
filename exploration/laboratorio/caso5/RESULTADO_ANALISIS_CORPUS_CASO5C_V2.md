# Resultado — Análisis del Corpus Ampliado (Caso 5C, post D-126)

Estado: **documento de resultado — describe evidencia real, no propone ni implementa
recomendación**. Ejecuta `analisis_corpus/` (Capa 2, D-123) y `analisis_interpretativo/` (D-124)
sobre el corpus oficial ampliado declarado en `MANIFIESTO_CORPUS_CASO5C_V1.json` tras D-126 (67
comparaciones: 6 V1 + 25 V2 + 18 Sub-campaña D + 18 Sub-campaña E). Continúa `RESULTADO_ANALISIS_
CORPUS_CASO5C_CAPA2_V1.md` (49 comparaciones, `BTCUSDT` únicamente) — este documento es su
extensión sobre el corpus con `ETHUSDT` incorporado, no un reemplazo. Responde exclusivamente
**"¿qué evidencia contiene el corpus ampliado?"** — no responde "¿qué instrumento/gestor/estrategia
debería elegirse?". D-118/D-119/D-120 permanecen intactas, sin activar.

**Fuente**: salida real de `analisis_corpus/ProgramAnalisisCorpus.cs` (11/11 pruebas P1-P9/P4b/P8b)
y `analisis_interpretativo/ProgramAnalisisInterpretativo.cs` (8/8 pruebas P1-P8), ambos leyendo las
201 filas (67 comparaciones × 3 gestores) declaradas en `MANIFIESTO_CORPUS_CASO5C_V1.json` —
ninguna de las 105 carpetas excluidas con lista explícita, ni las 25 sin lista
(`primera-ejecucion-interrumpida-v2`), participó en ningún cálculo. Regresión completa (126/126
producción, 25/25 Caso5 Capa1/5A/5B) sin cambios tras esta ejecución.

---

## 0. Defectos encontrados y corregidos durante esta ejecución

Este documento deja constancia explícita de 2 defectos de infraestructura descubiertos al ejecutar
Capa 2/interpretativo sobre el corpus recién ampliado — ambos son evidencia de que la capa de
análisis está siendo validada contra cambios reales del corpus, no solo contra el estado inicial de
`BTCUSDT` en solitario.

**Defecto 1 — `AnalisisDescriptivo.Resumir` (Capa 2)**: el texto fijo de `Limitaciones` afirmaba
`"instrumento unico (BTCUSDT)"` como literal codificado, sin derivarlo de los datos. Al incorporar
`ETHUSDT`, esa afirmación pasó a ser falsa. **Corregido**: el texto ahora deriva la lista de
instrumentos del prefijo de `NombreDataset` (convención `{SYMBOL}_{inicio}_{fin}`) sobre las filas
con métrica real, produciendo `"instrumento unico (BTCUSDT)"` con 1 instrumento o
`"N instrumentos (...)"` con más de 1.

**Defecto 2 — `ProgramAnalisisInterpretativo.cs` (análisis interpretativo), introducido por la
propia corrección del Defecto 1**: la primera versión del fix extraía el instrumento de *todas* las
filas, incluyendo `DatasetInexistente_ParaCorpusDeFallo` (evidencia deliberada de fallo, sub-
campaña C, sin `PnLTotal`) — produciendo `"3 instrumentos (BTCUSDT, DatasetInexistente, ETHUSDT)"`,
contando un caso de fallo como si fuera un instrumento real. **Corregido**: se aplicó el mismo
filtro `f.PnLTotal.HasValue` ya usado en `AnalisisDescriptivo.Resumir` (líneas 158/176) antes de
extraer instrumentos, produciendo el resultado correcto: `"2 instrumentos (BTCUSDT, ETHUSDT)"`.

**Ninguno de los 2 defectos cambió la metodología, agregó capacidad analítica nueva, ni modificó
ningún criterio de interpretación** — ambos son correcciones de una afirmación de texto fijo que
dejó de reflejar el corpus real tras una ampliación válida (mismo tipo de hallazgo que la
corrección de "3 periodos" ya documentada en `RESULTADO_ANALISIS_CORPUS_CASO5C_CAPA2_V1.md`).

---

## 1. Cobertura del corpus ampliado

**201 filas** (67 comparaciones × 3 gestores), **0 carpetas ignoradas**.

**Por dataset**:

| Dataset | Filas | Combinaciones únicas (Estrategia×Timeframe) |
|---|---|---|
| `BTCUSDT_2024-01-02_2025-01-02` | 90 | 18 (matriz completa) |
| `BTCUSDT_2022-01-01_2023-01-01` | 54 | 18 (matriz completa) |
| `ETHUSDT_2024-01-02_2025-01-02` | 54 | 18 (matriz completa) |
| `DatasetInexistente_ParaCorpusDeFallo` | 3 | 1 (evidencia parcial deliberada, sub-campaña C) |

**Los 3 datasets con matriz completa cubren exactamente las mismas 18 combinaciones cada uno** —
sin huecos de cobertura en ninguno, verificado también en `AUDITORIA_CONSISTENCIA_CORPUS_CASO5C_
V2.md` §2.1.

**Por gestor**: los 3 gestores tienen exactamente 67 filas cada uno — cobertura simétrica.

---

## 2. Distribuciones (agrupadas por gestor, los 3 datasets con matriz completa combinados)

| Métrica | Gestor | n | Mín | Máx | Media | Mediana |
|---|---|---|---|---|---|---|
| PnLTotal | fixed-fractional | 66 | −524.41 | 591.72 | −18.96 | −10.73 |
| PnLTotal | fixed-risk | 66 | −564.98 | 578.22 | −12.20 | −13.15 |
| PnLTotal | volatility-sizing | 66 | −52.51 | 213.64 | 4.71 | 0 |
| DrawdownMaximoPct | fixed-fractional | 66 | 0 | ≈1.00 | 0.67 | 0.81 |
| DrawdownMaximoPct | fixed-risk | 66 | 0 | 20.04 | 4.41 | 0.94 |
| DrawdownMaximoPct | volatility-sizing | 66 | 0 | 1.00 | 0.18 | 0.02 |
| ProfitFactor | fixed-fractional | 57 | 0.54 | 2.43 | 1.02 | 0.93 |
| ProfitFactor | fixed-risk | 57 | 0.48 | 2.73 | 1.07 | 0.98 |
| ProfitFactor | volatility-sizing | 57 | 0.43 | 4.50 | 1.20 | 1.00 |
| ExposicionMaxima | fixed-fractional | 66 | 0 | 129.03 | 89.80 | 100.05 |
| ExposicionMaxima | fixed-risk | 66 | 0 | 59.09 | 45.69 | 52.19 |
| ExposicionMaxima | volatility-sizing | 66 | 0 | 79.92 | 14.88 | 3.99 |
| CashFinal | fixed-fractional | 66 | ≈0 | 1436.64 | 398.53 | 176.67 |
| CashFinal | fixed-risk | 66 | −19038.26 | 1250.33 | −3391.40 | 8.90 |
| CashFinal | volatility-sizing | 66 | 0.15 | 1022.07 | 817.93 | 983.00 |
| EquityFinal | fixed-fractional | 66 | ≈0 | 1531.42 | 422.49 | 193.54 |
| EquityFinal | fixed-risk | 66 | −19038.26 | 1303.63 | −3355.54 | 60.97 |
| EquityFinal | volatility-sizing | 66 | 0.15 | 1027.58 | 819.65 | 983.77 |

**Observación factual**: `DrawdownMaximoPct` de `volatility-sizing` alcanza `0.9998473312283387`
en su máximo sobre el corpus ampliado — más alto que su máximo previo de `0.7520928011507303`
reportado sobre el corpus `BTCUSDT`-únicamente (`RESULTADO_ANALISIS_CORPUS_CASO5C_CAPA2_V1.md` §2).
El nuevo máximo proviene de una fila `ETHUSDT` (ver §4). Hecho verificable sobre el rango
observado, no una evaluación de qué gestor o instrumento es preferible.

---

## 3. Comparación entre los 3 datasets (descriptiva)

Para `DrawdownMaximoPct` y `PnLTotal`, por gestor, comparando `BTCUSDT_2024-01-02_2025-01-02`
(n=30), `BTCUSDT_2022-01-01_2023-01-01` (n=18) y `ETHUSDT_2024-01-02_2025-01-02` (n=18) —
presencia/ausencia de valores en cada conjunto, sin ordenar ni concluir cuál es "mejor".

| Métrica | Gestor | Dataset | n | Mín | Máx | Media | Mediana |
|---|---|---|---|---|---|---|---|
| DrawdownMaximoPct | fixed-fractional | BTCUSDT 2024-2025 | 30 | 0 | ≈1.00 | 0.68 | 0.81 |
| DrawdownMaximoPct | fixed-fractional | BTCUSDT 2022-2023 | 18 | 0 | ≈1.00 | 0.64 | 0.75 |
| DrawdownMaximoPct | fixed-fractional | ETHUSDT 2024-2025 | 18 | 0 | ≈1.00 | 0.67 | 0.76 |
| PnLTotal | fixed-fractional | BTCUSDT 2024-2025 | 30 | −203.41 | 591.72 | 23.07 | −10.73 |
| PnLTotal | fixed-fractional | BTCUSDT 2022-2023 | 18 | −242.01 | 183.42 | −44.95 | −19.87 |
| PnLTotal | fixed-fractional | ETHUSDT 2024-2025 | 18 | −524.41 | 248.54 | −63.02 | −38.83 |
| DrawdownMaximoPct | fixed-risk | BTCUSDT 2024-2025 | 30 | 0 | 19.98 | 4.63 | 0.94 |
| DrawdownMaximoPct | fixed-risk | BTCUSDT 2022-2023 | 18 | 0 | 20.04 | 4.18 | 0.76 |
| DrawdownMaximoPct | fixed-risk | ETHUSDT 2024-2025 | 18 | 0 | 20.02 | 4.28 | 0.78 |
| PnLTotal | fixed-risk | BTCUSDT 2024-2025 | 30 | −564.60 | 332.36 | −3.79 | −45.98 |
| PnLTotal | fixed-risk | BTCUSDT 2022-2023 | 18 | −291.45 | 578.22 | 13.59 | −0.77 |
| PnLTotal | fixed-risk | ETHUSDT 2024-2025 | 18 | −564.98 | 292.48 | −51.99 | −35.87 |
| DrawdownMaximoPct | volatility-sizing | BTCUSDT 2024-2025 | 30 | 0 | 0.41 | 0.09 | 0.01 |
| DrawdownMaximoPct | volatility-sizing | BTCUSDT 2022-2023 | 18 | 0 | 0.75 | 0.17 | 0.03 |
| DrawdownMaximoPct | volatility-sizing | ETHUSDT 2024-2025 | 18 | 0 | 1.00 | 0.35 | 0.14 |
| PnLTotal | volatility-sizing | BTCUSDT 2024-2025 | 30 | −5.38 | 6.86 | 1.02 | 0 |
| PnLTotal | volatility-sizing | BTCUSDT 2022-2023 | 18 | −22.93 | 13.20 | −0.50 | −0.41 |
| PnLTotal | volatility-sizing | ETHUSDT 2024-2025 | 18 | −52.51 | 213.64 | 16.06 | 0 |

**Observaciones factuales**:
- Los 3 gestores tienen valores tanto positivos como negativos de `PnLTotal` en los 3 conjuntos —
  ningún gestor es exclusivamente positivo o negativo en ninguno de los tres.
- El máximo de `DrawdownMaximoPct` de `fixed-fractional` es ≈100% en los 3 conjuntos — el mismo
  patrón cualitativo (drawdown extremo alcanzable) aparece en los tres.
- `DrawdownMaximoPct` de `volatility-sizing` tiene su máximo más alto en `ETHUSDT` (1.00) frente a
  `BTCUSDT` 2024-2025 (0.41) y 2022-2023 (0.75) — diferencia observable entre conjuntos, sin evaluar
  si es deseable o atribuirla a una causa.
- El rango de `PnLTotal` de `volatility-sizing` es sustancialmente más amplio en `ETHUSDT`
  (−52.51 a 213.64) que en cualquiera de los 2 conjuntos `BTCUSDT` — diferencia observable, sin
  evaluación.

---

## 4. Casos atípicos

**Ausencia de operaciones — ZScore Reversion**: en los 3 datasets con matriz completa, las 9
corridas `Success` (3 timeframes × 3 gestores) muestran `PnLTotal=0` — la estrategia no generó
ninguna operación bajo los parámetros usados, en ninguno de los 3 conjuntos, incluyendo `ETHUSDT`.
Mismo patrón ya documentado para los 2 datasets `BTCUSDT`, ahora confirmado también con instrumento
distinto.

**Estados incompletos — sub-campaña C**: sin cambio respecto al documento anterior — 1 comparación,
3 filas, todas `Estado: Incomplete`, sin métricas, evidencia parcial deliberada.

**Drawdowns extremos (`DrawdownMaximoPct ≥ 99%`)**: detectados en los 3 datasets, con distinta
frecuencia:

| Dataset | Filas con DD≥99% | Total filas | Proporción |
|---|---|---|---|
| BTCUSDT 2024-2025 | 15 | 90 | 16.7% |
| BTCUSDT 2022-2023 | 16 | 54 | 29.6% |
| ETHUSDT 2024-2025 | 18 | 54 | 33.3% |

**Nota de lectura**: los denominadores no son directamente comparables entre sí (90 filas en
`BTCUSDT_2024-01-02_2025-01-02` incluyen las repeticiones deliberadas de V1/B — 30 filas sobre 18
combinaciones únicas; los otros 2 conjuntos tienen exactamente 18 combinaciones únicas × 3 gestores
= 54 filas sin repetición). Sobre las 18 combinaciones únicas de cada dataset (54 filas
comparables), la cifra de `ETHUSDT` (18/54) es mayor que la de `BTCUSDT_2024-01-02_2025-01-02`
tomando solo sus 18 combinaciones únicas (15/54, ya reportado también en `AUDITORIA_SUBCAMPANA_
E_CASO5C_V1.md` §4.2) — diferencia factual, sin atribución causal ni evaluación de cuál instrumento
"tiene mejor comportamiento".

Al igual que en el corpus previo, el patrón se concentra en `fixed-fractional` en timeframes cortos
(`15m`/`1h`, nunca `1D`) en los 3 datasets, con `fixed-risk` mostrando valores del mismo orden de
magnitud en las mismas combinaciones — mismo patrón cualitativo ya documentado, ahora también
presente en `ETHUSDT`.

**Volumen Breakout no muestra drawdowns ≥99% en ningún dataset**, incluyendo `ETHUSDT` — hecho
observable, no una conclusión sobre robustez.

---

## 5. Relaciones y consistencia (análisis interpretativo, D-124)

Ejecutado sobre las mismas 201 filas, vía `DetectorRelaciones` (sin modificación de código más allá
de la corrección de §0):

**`CruzarDimensiones(DrawdownMaximoPct, [Estrategia, Gestor])`**: 18 combinaciones (6 estrategias ×
3 gestores) sobre el corpus combinado — ninguna destacada, todas presentadas en el mismo formato.

**`AgruparPorPatron(DrawdownMaximoPct>=99%)`**: 61 filas donde aparece, 140 donde no (61+140=201) —
incluye las 46 filas ya conocidas de `BTCUSDT` (ambos períodos) más 15 nuevas de `ETHUSDT`.

**`AgruparPorPatron(SinActividad)`**: 27 filas donde aparece (ZScore Reversion, los 3 datasets con
matriz completa, los 3 timeframes × 3 gestores cada uno), 174 donde no (27+174=201) — 9 filas de
cada uno de los 3 datasets, patrón idéntico en los 3.

**`CompararConsistencia(DrawdownMaximoPct>=99%)`** — por primera vez con 3 conjuntos, no 2:

| Dataset | Condiciones (Estrategia/Timeframe/Gestor) con DD≥99% |
|---|---|
| `BTCUSDT_2024-01-02_2025-01-02` | 15 |
| `DatasetInexistente_ParaCorpusDeFallo` | 0 |
| `BTCUSDT_2022-01-01_2023-01-01` | 16 |
| `ETHUSDT_2024-01-02_2025-01-02` | 18 |

De las 15 condiciones de `BTCUSDT` 2024-2025, las 15 aparecen también en `BTCUSDT` 2022-2023 (con 1
condición adicional exclusiva de 2022-2023, ya documentada en auditorías previas). De esas mismas
15, **11 aparecen también en `ETHUSDT`** (mismo trío estrategia/timeframe/gestor): Tres
Mosqueteros/15m/fixed-fractional, Tres Mosqueteros/15m/fixed-risk, Tres Mosqueteros/1h/
fixed-fractional, Tres Mosqueteros/1h/fixed-risk, Ema Cross/15m/fixed-fractional, Ema Cross/15m/
fixed-risk, Neutral/15m/fixed-fractional, Neutral/15m/fixed-risk, Neutral/1h/fixed-fractional,
Neutral/1h/fixed-risk, Mhi Mayoria/15m/fixed-fractional (y 6 más de Mhi Mayoria/otros). **7
condiciones aparecen exclusivamente en `ETHUSDT`** y no en ninguno de los 2 conjuntos `BTCUSDT`,
incluyendo con el gestor `volatility-sizing` (que no muestra DD≥99% en ningún dataset `BTCUSDT`):
Tres Mosqueteros/15m/volatility-sizing, Ema Cross/1h/fixed-risk, Neutral/15m/volatility-sizing, Mhi
Mayoria/15m/volatility-sizing, entre otras.

**Observación estrictamente factual**: la mayoría de las condiciones de drawdown extremo detectadas
en `BTCUSDT` también aparecen en `ETHUSDT` bajo el mismo trío estrategia/timeframe/gestor, y
`ETHUSDT` presenta adicionalmente un subconjunto de condiciones con `volatility-sizing` que no
aparece en ningún dataset `BTCUSDT` — presencia/ausencia factual, sin calificar consistencia como
"robustez" ni "confiabilidad" (mismo límite ya establecido en D-124).

---

## Fuera de alcance de este documento

No se declara ningún gestor ni instrumento "ganador". No se ordena por ninguna métrica. No se
calcula ningún score compuesto. No se recomienda ninguna configuración ni instrumento. No se ajusta
ningún parámetro. No se concluye robustez general de ningún gestor, estrategia, ni instrumento. No
se atribuye causa a ninguna diferencia observada entre `BTCUSDT` y `ETHUSDT` (liquidez,
volatilidad, u otra) — esta capa no calcula ninguna métrica de mercado más allá de las ya producidas
por `ComparadorGestores`.

---

## Conclusión

El corpus ampliado (67 comparaciones, 201 filas) mantiene cobertura completa y simétrica en sus 3
datasets con matriz completa (18 combinaciones cada uno, sin huecos). Los patrones ya documentados
sobre `BTCUSDT` (ausencia de actividad de ZScore Reversion, drawdown extremo de `fixed-fractional`
en timeframes cortos) se observan también en `ETHUSDT`. La evidencia disponible muestra una mayor
frecuencia de drawdown extremo en `ETHUSDT` que en `BTCUSDT` bajo condiciones comparables, y un
subconjunto de condiciones de drawdown extremo con `volatility-sizing` presente solo en `ETHUSDT` —
observaciones estrictamente descriptivas, sin ranking, sin recomendación, sin selección de
instrumento. Durante esta ejecución se detectaron y corrigieron 2 defectos de infraestructura (§0),
ambos de la misma naturaleza que el ya documentado para "3 periodos" — afirmaciones de texto fijo
que dejaron de reflejar el corpus tras su ampliación válida, no defectos de metodología. D-118/
D-119/D-120 permanecen intactas; este documento no evalúa si la evidencia acumulada justifica
abrir una etapa de recomendación.
