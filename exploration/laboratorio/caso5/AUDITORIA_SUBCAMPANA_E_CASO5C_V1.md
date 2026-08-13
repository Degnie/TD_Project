# Auditoría — Sub-campaña E (Diversidad de Instrumento, ETHUSDT)

Estado: **documento de auditoría — evalúa exclusivamente las 18 comparaciones `ETHUSDT` generadas
por Sub-campaña E, no propone ni aplica ninguna modificación al manifiesto**. Continúa
`CLASIFICACION_PROPUESTA_CARPETAS_SUBCAMPANA_E_V1.md` §8. No evalúa las 68 carpetas de repetición
técnica ni las 2 de escritura interrumpida (ya clasificadas en ese documento). No decide si
`ETHUSDT` se incorpora al corpus oficial — eso queda para una decisión posterior explícita,
distinta de esta auditoría.

---

## 1. ¿La cobertura es completa?

**Sí, verificado por inspección directa de las 18 carpetas** (no por el conteo reportado en el log
de ejecución, para no repetir el error histórico de confiar en un número sin inspeccionar el
contenido):

| Estrategia | 15m | 1h | 1D |
|---|---|---|---|
| Tres Mosqueteros | ✅ | ✅ | ✅ |
| Ema Cross | ✅ | ✅ | ✅ |
| ZScore Reversion | ✅ | ✅ | ✅ |
| Neutral | ✅ | ✅ | ✅ |
| Volumen Breakout | ✅ | ✅ | ✅ |
| Mhi Mayoria | ✅ | ✅ | ✅ |

**6 estrategias × 3 timeframes = 18 combinaciones únicas, sin huecos ni duplicados** (verificado:
`len(claves) == 18` sobre el conjunto de pares `(Estrategia, Timeframe)` leído de
`IDENTIDAD_COMPARACION.json` en cada una de las 18 carpetas).

**Gestores**: las 18 comparaciones tienen exactamente 3/3 gestores en estado `Success`
(`fixed-fractional`, `fixed-risk`, `volatility-sizing`) — ninguna quedó con escritura incompleta,
a diferencia de las 2 carpetas de `BTCUSDT` clasificadas aparte en el documento anterior. 18 × 3 =
**54 filas de evidencia** con métricas completas.

---

## 2. ¿La identidad experimental es correcta?

**Sí, verificado por prueba sobre el código real (P8, log de ejecución de
`ProgramCampanaCorpus.cs`), no solo por inspección de archivos**:

- **`HashCompuesto` distingue el instrumento**: la comparación de `IdentidadExperimentoCompleta`
  entre `entrada2024` (`BTCUSDT`) y `entradaEth` (`ETHUSDT`), ambas con el mismo rango temporal
  (`2024-01-02`–`2025-01-02`), misma estrategia (`Tres Mosqueteros`), mismo timeframe (`15m`),
  produjo hashes distintos — confirma que el sistema no confunde instrumentos con el mismo período.
- **`HashConfiguracionEconomica` es idéntico entre instrumentos**: mismo hash para `BTCUSDT` y
  `ETHUSDT` — confirma que `TasaMargen=0.1m`/costes `0.001m`/`0.001m` no cambiaron entre ambas
  ejecuciones (D-030/D-125 respetados; ningún ajuste de escala de precio/volumen fue aplicado,
  deliberada o accidentalmente).
- **Única dimensión variada**: `dirDatasetsEth`/`nombreDatasetEth` apuntan a
  `datasets/reales/ETHUSDT/` con `NombreDataset: "ETHUSDT_2024-01-02_2025-01-02"` — mismo rango
  exacto que el dataset `BTCUSDT` ya congelado, mismas 6 estrategias, mismos 3 gestores, mismo
  capital inicial (`1000m`). El instrumento es la única variable que cambia entre la matriz oficial
  `BTCUSDT` 2024-2025 y esta Sub-campaña E — condición exigida por D-121/D-125 para que cualquier
  diferencia observada sea atribuible a una sola dimensión.

**Ningún parámetro de estrategia, gestor, ni configuración de costes fue modificado** para
`ETHUSDT` — confirmado por inspección de `ProgramCampanaCorpus.cs`: `entradaEth` se construye
como `entrada2024 with { DirDatasets = ..., NombreDataset = ... }`, únicos 2 campos alterados.

---

## 3. ¿La evidencia es reproducible?

**Sí, verificado por prueba (mismo criterio P8)**: `EjecutorProtocolo.Ejecutar(entradaEth)`
ejecutado dos veces consecutivas sobre el mismo dataset `ETHUSDT` produjo el mismo `HashCompuesto`
en ambas corridas — mismo criterio de reproducibilidad ya exigido y verificado para el dataset
2022-2023 (P6, Sub-campaña D).

**Reproducibilidad adicional confirmada por evidencia física**: durante la clasificación de las 88
carpetas nuevas (`CLASIFICACION_PROPUESTA_CARPETAS_SUBCAMPANA_E_V1.md` §3), ninguna de las 18
carpetas `ETHUSDT` apareció como "repetición técnica" de otra `ETHUSDT` — la ejecución de Sub-
campaña E ocurrió una sola vez en esta corrida, sin duplicados internos, a diferencia de V1/A/B/D
que sí se repitieron por ser parte del mismo archivo ejecutable.

---

## 4. ¿Qué observaciones descriptivas aporta frente al corpus BTCUSDT?

**Comparación estrictamente factual, sobre las mismas 18 combinaciones (54 filas) en ambos
instrumentos, mismo rango temporal — sin ranking, sin "ganador", sin recomendación, sin selección
de instrumento.**

### 4.1 — Patrón `SinActividad` (ZScore Reversion, PnLTotal=0)

**Idéntico entre instrumentos**: presente en las 3 combinaciones de ZScore Reversion (15m/1h/1D),
en los 3 gestores de cada una — **9/54 filas en ambos instrumentos** (9 en `BTCUSDT`, 9 en
`ETHUSDT`). Mismo patrón ya documentado en `RESULTADO_ANALISIS_CORPUS_CASO5C_CAPA2_V1.md` §4 y en
`AUDITORIA_ANALISIS_INTERPRETATIVO_CASO5C_V1.md` §3 para el corpus `BTCUSDT` (ambos períodos).
**Observación descriptiva**: la ausencia de actividad de ZScore Reversion con estos parámetros
(`ventana=5, umbralEntrada=2.0, umbralSalida=0.5`) no depende del instrumento evaluado, en la
única comparación instrumento-instrumento disponible hasta ahora.

### 4.2 — Patrón `DrawdownMaximoPct >= 99%`

**Presente en ambos instrumentos, con distinta frecuencia**: **15/54 filas en `BTCUSDT`** vs
**18/54 filas en `ETHUSDT`** (mismas 18 combinaciones, mismo rango temporal). La diferencia (3
filas) no se concentra en una sola estrategia o timeframe — aparece distribuida: ejemplos
verificados incluyen `Ema Cross/15m/fixed-fractional` y `Ema Cross/15m/fixed-risk` (ambos con
DD≥99% en `ETHUSDT`, no presentes con ese nivel en la comparación `BTCUSDT` equivalente) y
`Ema Cross/1h/fixed-risk` (DD≥99% en `ETHUSDT`). **Observación descriptiva, no evaluativa**: el
patrón de drawdown extremo aparece con más frecuencia en la evidencia `ETHUSDT` que en la
`BTCUSDT` equivalente, bajo las mismas condiciones económicas y el mismo rango temporal — esto no
determina si el patrón es "peor" ni "mejor" para ningún instrumento, solo que su frecuencia de
aparición difiere entre los dos conjuntos de evidencia disponibles.

### 4.3 — Lo que esta única comparación NO establece

- No establece si el patrón de drawdown es "propio de `ETHUSDT`" en general — es una única
  observación sobre un rango temporal, no una muestra repetida de instrumentos.
- No establece causalidad (volatilidad relativa, liquidez, comportamiento de precio) — esta
  auditoría no calcula ninguna métrica de mercado más allá de las ya producidas por
  `ComparadorGestores`.
- No sugiere qué instrumento "conviene" — D-125 prohíbe explícitamente esa lectura, y ninguna cifra
  de esta sección se convierte aquí en un criterio de selección.

---

## 5. Qué no se hizo (restricción respetada)

- No se generó ningún ranking, puntuación compuesta, ni comparación reducida a un solo número.
- No se declaró ningún instrumento "mejor" ni "peor".
- No se modificó `ComparadorGestores.cs`, `PersistidorComparaciones.cs`, `EjecutorProtocolo.cs`, ni
  ningún componente de Capa 1/Capa 2/análisis interpretativo.
- No se modificó `MANIFIESTO_CORPUS_CASO5C_V1.json` — las 18 comparaciones `ETHUSDT` permanecen
  fuera del corpus oficial declarado.
- No se recalibró ningún parámetro económico ni de estrategia.
- No se usó ninguna herramienta de Capa 2/análisis interpretativo (`LectorCorpus`,
  `AnalisisDescriptivo`, `DetectorRelaciones`) sobre este corpus — esas herramientas leen
  exclusivamente por manifiesto declarado, y el manifiesto no incluye `ETHUSDT` todavía; las
  observaciones de §4 se calcularon directamente sobre las 18 carpetas, fuera de esa
  infraestructura, precisamente para no alterarla ni presuponer su inclusión.

---

## 6. Estado consolidado tras esta auditoría

```
Exploracion ETHUSDT                    ✅ viable (12/12 bloques continuos)
Descarga ETHUSDT 1m                    ✅ completada (527040/527040 velas, 0 huecos)
Congelacion ETHUSDT 1m                 ✅ completada (SHA-256 verificado)
Agregacion 12 timeframes derivados     ✅ completada (sourceSha256 verificado en los 12)
Sub-campana E (18 comparaciones)       ✅ ejecutada, cobertura completa, 3/3 gestores
Identidad experimental                 ✅ verificada (instrumento aislado, config. economica identica)
Reproducibilidad                       ✅ verificada
Auditoria de Sub-campana E             ✅ este documento
Incorporacion al manifiesto oficial    ⏳ pendiente — decision separada, no automatica
```

---

## 7. Fuera de alcance de este documento

No se decide si las 18 comparaciones `ETHUSDT` se incorporan al manifiesto (corpus 49→67). No se
evalúa si la diferencia observada en §4.2 (15 vs 18 filas con drawdown extremo) es relevante o
suficiente para ninguna conclusión — solo se documenta como observación factual disponible. No se
activa ninguna forma de recomendación (D-118/D-119/D-120 siguen en estado de principio). No se
analizan las 68 carpetas de repetición técnica ni las 2 de escritura interrumpida (ya resueltas en
`CLASIFICACION_PROPUESTA_CARPETAS_SUBCAMPANA_E_V1.md`).

---

## Conclusión

Las 18 comparaciones de Sub-campaña E tienen cobertura completa (6×3, sin huecos, 3/3 gestores en
todas), identidad experimental correcta (instrumento como única dimensión variada, configuración
económica idéntica a `BTCUSDT`, verificado por hash) y son reproducibles. La evidencia disponible
muestra que el patrón `SinActividad` de ZScore Reversion es idéntico entre instrumentos (9/54 en
ambos) y que el patrón de drawdown extremo (`>=99%`) aparece con mayor frecuencia en `ETHUSDT`
(18/54) que en `BTCUSDT` (15/54) bajo el mismo rango temporal y la misma configuración económica —
observación estrictamente descriptiva, sin ranking, sin selección, sin recomendación. La decisión
de si esta evidencia se incorpora al corpus oficial (manifiesto ampliado) permanece pendiente, como
un paso separado y explícito.
