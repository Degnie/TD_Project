# Auditoría de Corpus Comparativo — Caso 5C

Estado: **documento de auditoría — evalúa evidencia real, no propone ni implementa Capa 2**. Responde
únicamente si el corpus generado por `caso5/campana_corpus/` (`PROPUESTA_CAMPANA_CORPUS_CASO5C_V1.md`,
`ESPECIFICACION_IMPLEMENTACION_CAMPANA_CORPUS_CASO5C_V1.md`) permite diseñar Caso 5C Capa 2. No
determina qué gestor es mejor, no recomienda, no fija ningún criterio de recomendación — esas
preguntas siguen fuera de alcance hasta que exista una decisión D-N que abra Capa 2 formalmente.

**Corpus auditado**: las 6 carpetas escritas por la ejecución real de la campaña
(`EmaCross_15m/1h/1D_20260812T2135*`, `TresMosqueteros_15m/1h/1D_20260812T2135*`), leídas
directamente de `caso5/resultados/`. La carpeta preexistente de pruebas técnicas
(`TresMosqueteros_1D_20260812T204012Z`, generada por `TestsPersistidorComparaciones.cs`) se excluye
de este análisis — no es evidencia de campaña, es un artefacto de verificación de infraestructura
(criterio ya anticipado en `ESPECIFICACION_IMPLEMENTACION_CAMPANA_CORPUS_CASO5C_V1.md` §5).

---

## 1. Qué corpus existe

**6 comparaciones persistidas**, cada una con 3 filas de gestor (18 corridas individuales
representadas):

| Carpeta | Estrategia | Timeframe | Gestores | Estado de las 3 corridas |
|---|---|---|---|---|
| `TresMosqueteros_15m_...530Z` | Tres Mosqueteros | 15m | FixedFractional, FixedRisk, VolatilitySizing | Success, Success, Success |
| `TresMosqueteros_1h_...531Z` | Tres Mosqueteros | 1h | ídem | Success, Success, Success |
| `TresMosqueteros_1D_...531Z` | Tres Mosqueteros | 1D | ídem | Success, Success, Success |
| `EmaCross_15m_...539Z` | Ema Cross | 15m | ídem | Success, Success, Success |
| `EmaCross_1h_...540Z` | Ema Cross | 1h | ídem | Success, Success, Success |
| `EmaCross_1D_...540Z` | Ema Cross | 1D | ídem | Success, Success, Success |

**18/18 corridas internas en estado `Success`** — ninguna comparación tiene una fila con
`Metricas: null`. El corpus no contiene, todavía, ningún caso de corrida fallida sobre el cual
evaluar cómo se comportaría una futura Capa 2 ante evidencia parcial (P7 de Caso 5B ya verificó ese
caso a nivel de componente, pero no está representado en este corpus real).

**Identidad verificada**: los 6 `IDENTIDAD_COMPARACION.json` muestran las mismas 3 identidades de
gestor (`fixed-fractional:v1:riesgo=0.1`, `fixed-risk:v1:monto=50`,
`volatility-sizing:v1:ventana=20:base=0.1:desviacionReferencia=2`) en las 6 comparaciones — confirma
que la campaña usó exactamente los parámetros declarados en la especificación, sin variación
accidental entre corridas.

---

## 2. Qué diversidad contiene

**Estrategias**: 2 — Tres Mosqueteros (con martingala) y Ema Cross (sin martingala). Cubre 2 de las
6 familias congeladas en el laboratorio (`ZScoreReversion`, `EstrategiaNeutral`,
`VolumenBreakout` no están representadas — no linkeadas en `Caso5.csproj`, límite ya declarado en
`PROPUESTA_CAMPANA_CORPUS_CASO5C_V1.md` §1).

**Timeframes**: 3 — `15m` (corto), `1h` (medio), `1D` (largo). De los 13 timeframes disponibles en
el dataset, la campaña cubrió 3, elegidos por separación de escala, no por resultado.

**Datasets**: 1 — `BTCUSDT_2024-01-02_2025-01-02`. Ningún otro instrumento ni rango temporal
disponible en el repositorio (mismo límite ya declarado en la propuesta).

**Gestores**: 3 — los mismos en las 6 comparaciones, cobertura completa de D-110 (Kelly/Masaniello
excluidos, no forman parte del corpus por diseño, no por omisión de la campaña).

**Matriz de cobertura real**: 2×3 = 6 combinaciones estrategia×timeframe, cada una con los 3
gestores — exactamente lo declarado en la propuesta, sin desviación.

---

## 3. Qué comparaciones están representadas

**Cada combinación estrategia×timeframe aparece exactamente una vez** — el corpus no contiene
repeticiones de la misma combinación bajo condiciones idénticas. Esto significa que, con este
corpus, **no es posible evaluar la estabilidad de un resultado ante una re-ejecución** (ej.: si
`TresMosqueteros_1D` se repitiera, ¿el perfil relativo entre gestores se mantendría?) — el
mecanismo ya es reproducible por diseño (D-116/D-117, verificado por P4 de Caso 5C Capa 1), pero
esta campaña no ejecutó esa repetición.

**Observación directa de los datos generados** (lectura de los 6 `COMPARACION_GESTORES_V1.md`, sin
interpretación de cuál gestor es preferible — se reporta como característica del corpus, no como
conclusión comparativa):

- En **5 de 6 comparaciones**, al menos un gestor distinto de `VolatilitySizing` produjo
  `DrawdownMaximoPct > 1` (superior al 100%) y `CashFinal` negativo — resultados económicamente
  degenerados, no solo "peores". La única excepción es `EmaCross_1D`, donde los 3 gestores
  terminaron con `CashFinal`/`EquityFinal` positivos.
- `VolatilitySizing` fue el único gestor que, en las 6 comparaciones, mantuvo `CashFinal` positivo
  y `DrawdownMaximoPct` por debajo de 1 — consistente con su diseño de reducir exposición ante
  volatilidad, pero el corpus no permite saber si esa consistencia se sostiene fuera de estos 6
  contextos.
- La dirección del `PnLTotal` de un mismo gestor cambia entre estrategias y timeframes sin patrón
  evidente en este corpus (ej.: `fixed-fractional` es negativo en `TresMosqueteros_1D`/`_15m`/`_1h`
  y en `EmaCross_15m`, pero positivo en `EmaCross_1h`/`_1D`).

Estas observaciones se reportan como **contenido del corpus**, no como hallazgo comparativo
oficial — ninguna es una recomendación ni implica que un gestor sea superior; son la base fáctica
que una futura Capa 2 tendría disponible, y también la razón por la que ese análisis no puede
hacerse todavía con la profundidad que D-119 exige.

---

## 4. Qué limitaciones tiene

- **Sin repetición**: cada combinación aparece una sola vez — no hay evidencia de estabilidad ante
  reejecución (§3).
- **Sin corridas fallidas representadas**: las 18 corridas fueron `Success` — el corpus no cubre
  cómo una futura Capa 2 debería tratar comparaciones con evidencia parcial, aunque el mecanismo ya
  lo soporta (D-114/P7 de Caso 5B).
- **Un solo dataset/instrumento**: toda conclusión posible está condicionada a `BTCUSDT` en el
  rango `2024-01-02`–`2025-01-02` — ninguna diversidad de instrumento, ninguna diversidad de rango
  temporal distinto (dos ventanas de tiempo distintas del mismo instrumento, por ejemplo).
- **Solo 2 de 6 estrategias congeladas**: `ZScoreReversion`, `EstrategiaNeutral`, `VolumenBreakout`
  no participaron — no por decisión de exclusión, sino porque no están linkeadas en
  `Caso5.csproj`/`CampanaCorpus.csproj` (ampliarlo es una decisión de infraestructura fuera de esta
  campaña).
- **Resultados económicamente degenerados sin explicación en el corpus**: `DrawdownMaximoPct > 1`
  y `CashFinal` negativo aparecen en 5 de 6 comparaciones (§3) — el corpus registra el hecho, pero
  no contiene ninguna anotación de causa (posible interacción entre martingala/timeframe
  corto/monto de riesgo fijo). Investigar la causa no es una tarea de Capa 1 ni de esta auditoría —
  se deja registrada como una característica del corpus que cualquier diseño de Capa 2 tendría que
  poder manejar sin ocultarla.
- **Volumen pequeño en términos absolutos**: 6 comparaciones, 18 corridas — muy por debajo de
  cualquier número que D-119 (aún sin valor fijado) probablemente exigiría para una recomendación
  con múltiples condiciones cubiertas declaradas.

---

## 5. ¿La evidencia actual permite o no diseñar Capa 2?

**Evidencia todavía insuficiente.**

Razones, directamente derivadas de §1 a §4, no de un criterio numérico inventado aquí (D-119 sigue
sin fijar valores, y esta auditoría no lo hace):

- No hay repetición de ninguna combinación — no es posible evaluar si un patrón observado es
  estable o casual con un corpus donde cada punto es único.
- No hay diversidad de dataset ni de instrumento — cualquier "consistencia" observada
  (`VolatilitySizing` con mejor perfil de riesgo en las 6 comparaciones) podría ser un efecto
  específico de `BTCUSDT` en esta ventana temporal, no un patrón general.
- Solo 2 de 6 estrategias están representadas — insuficiente para que D-119 hable de "diversidad de
  estrategias cubiertas" con algo más que una muestra mínima.
- El corpus no contiene ningún caso de evidencia parcial (corridas fallidas) — un diseño de Capa 2
  hecho sobre este corpus no tendría cómo verificar su propio comportamiento ante ese caso, que
  D-114/D-117 ya reconocen como válido.

**Esto no invalida el mecanismo de Caso 5C Capa 1** — D-116/D-117 siguen congeladas y funcionando
correctamente (la campaña lo confirmó: 6/6 comparaciones persistidas, formato correcto, sin
regresión). La insuficiencia es de **volumen y diversidad del corpus acumulado**, no de la
capacidad de acumularlo.

---

## Fuera de alcance de este documento

No se determina qué gestor es mejor. No se recomienda ningún gestor. No se define ningún criterio
de recomendación, umbral numérico de D-119, ni estructura de `RecomendacionExperimental` más allá
de lo ya fijado en D-120. No se decide si ampliar la campaña (más estrategias, más timeframes, más
repeticiones) — esa decisión, si se toma, es del auditor, no una consecuencia automática de esta
auditoría.

---

## Conclusión

El sistema ya puede ejecutar, comparar y conservar evidencia de gestores de riesgo. **Todavía no
tiene base suficiente para recomendar configuraciones.** Ampliar el corpus (repeticiones, más
estrategias, más diversidad de dataset/timeframe) es la vía directa para resolver esta
insuficiencia — diseñar Capa 2 sobre el corpus actual repetiría el riesgo que D-030 ya previno: fijar
reglas sin evidencia suficiente sobre la que calibrarlas.
