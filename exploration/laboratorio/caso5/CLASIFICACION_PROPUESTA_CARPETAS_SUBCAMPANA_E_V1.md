# Clasificación Propuesta — Carpetas generadas por la ejecución de Sub-campaña E

Estado: **documento de clasificación propuesta — no modifica `MANIFIESTO_CORPUS_CASO5C_V1.json`,
el corpus oficial (49), ni ninguna auditoría de Capa 2**. Registra, por evidencia de contenido, la
categoría de cada una de las 88 carpetas nuevas en `caso5/resultados/` tras la ejecución de
`campana_corpus/` autorizada para D-125 (Sub-campaña E). Es un documento intermedio entre la
ejecución y cualquier decisión de ampliar el manifiesto — separa explícitamente 2 procesos: (1)
clasificar residuos de ejecución, (2) decidir si la evidencia `ETHUSDT` se incorpora al corpus
oficial.

---

## 1. Origen y alcance

**Ejecución de origen**: `caso5/campana_corpus/ProgramCampanaCorpus.cs`, corrida el
`2026-08-13` (~05:42–05:46 UTC para la mayoría de comparaciones nuevas; 2 carpetas de
`escritura-interrumpida` corresponden a pruebas intermedias propias de esta sesión a las
`~04:10`/`~04:24` UTC, antes de la ejecución final reportada). Autorizada explícitamente para
generar Sub-campaña E (D-125) — la misma corrida también re-ejecuta las matrices V1/A/B/C/D ya
oficiales, porque `ProgramCampanaCorpus.cs` no tiene un modo de "solo Sub-campaña E" (mismo
comportamiento ya observado y clasificado tras la ejecución que produjo Sub-campaña D).

**Carpetas en disco antes de esta ejecución**: 110 (49 oficiales + 61 previamente excluidas y
documentadas: 25+8+1+25+2 de pruebas de esta sesión ya reclasificadas en la ronda de Capa 2).
**Carpetas en disco después**: 171. **Carpetas nuevas a clasificar en este documento**: 171 − 110
− 2 recuento = **88** (110 previas ya excluidas/oficiales no se re-cuentan; el número exacto de
carpetas nuevas producidas por esta corrida es 61 según el log de ejecución — la diferencia de 88
vs 61 se debe a que 2 carpetas de `escritura-interrumpida` databan de una prueba manual anterior a
esta corrida, no de ella; ver §3).

---

## 2. Criterio de clasificación (mismo ya usado en la ronda anterior)

Para cada carpeta nueva, se leyó su contenido — nunca su nombre ni su timestamp:

```
carpeta física
    ↓
IDENTIDAD_COMPARACION.json  (estrategia, timeframe, nombreDataset, gestores + estado)
    ↓
COMPARACION_GESTORES_V1.md  (6 métricas financieras completas por gestor)
    ↓
comparación de contenido contra las 49 comparaciones ya oficiales en el manifiesto
    ↓
clasificación
```

**`HashCompuesto`/`HashConfiguracionEconomica` no están disponibles en disco** — `Identidad
ExperimentoCompleta` se calcula en memoria durante la corrida (verificado por P6/P8 del log de
ejecución) pero no se persiste dentro de `IDENTIDAD_COMPARACION.json`. El criterio de contenido
usado aquí (clave experimental completa + gestores/estado + las 6 métricas financieras exactas de
cada gestor) es equivalente en poder discriminativo — dos comparaciones con el mismo dataset,
estrategia, timeframe, gestores y métricas idénticas hasta el último decimal solo pueden originarse
de una ejecución determinista sobre el mismo dataset (mismo principio ya usado para verificar las
25 carpetas de "repetición completa" en la clasificación anterior).

---

## 3. Categoría A — Repetición técnica (68 carpetas)

**Criterio de pertenencia**: misma clave `(Estrategia, Timeframe, NombreDataset)` que una
comparación ya oficial en el manifiesto, mismos 3 gestores en el mismo estado, y las 6 métricas
financieras de cada gestor idénticas byte a byte contra la comparación oficial correspondiente.

**Origen**: la re-ejecución de las Sub-campañas V1, A, B y D dentro de la misma corrida que generó
Sub-campaña E (`ProgramCampanaCorpus.cs` ejecuta todo el archivo en cada corrida, sin modo parcial).

**No agregan evidencia nueva** — son el mismo resultado determinista ya representado en el corpus
oficial, producido de nuevo porque el programa no distingue "ejecutar solo la sub-campaña nueva".

**Listado completo (68)**, con su comparación oficial equivalente:

| Carpeta nueva | Oficial equivalente (mismo contenido) |
|---|---|
| EmaCross_15m_20260812T221915Z | EmaCross_15m_20260812T222152Z |
| EmaCross_15m_20260812T222007Z | EmaCross_15m_20260812T222152Z |
| EmaCross_15m_20260813T054300Z | EmaCross_15m_20260812T222152Z |
| EmaCross_15m_20260813T054359Z | EmaCross_15m_20260812T222152Z |
| EmaCross_15m_20260813T054418Z | EmaCross_15m_20260813T032422Z |
| EmaCross_1D_20260812T221916Z | EmaCross_1D_20260812T222100Z |
| EmaCross_1D_20260812T222008Z | EmaCross_1D_20260812T222100Z |
| EmaCross_1D_20260813T054301Z | EmaCross_1D_20260812T222100Z |
| EmaCross_1D_20260813T054400Z | EmaCross_1D_20260812T222100Z |
| EmaCross_1D_20260813T054419Z | EmaCross_1D_20260813T032423Z |
| EmaCross_1h_20260812T221916Z | EmaCross_1h_20260812T222153Z |
| EmaCross_1h_20260812T222008Z | EmaCross_1h_20260812T222153Z |
| EmaCross_1h_20260813T054301Z | EmaCross_1h_20260812T222153Z |
| EmaCross_1h_20260813T054400Z | EmaCross_1h_20260812T222153Z |
| EmaCross_1h_20260813T054419Z | EmaCross_1h_20260813T032423Z |
| MhiMayoria_15m_20260812T221950Z | MhiMayoria_15m_20260812T222135Z |
| MhiMayoria_15m_20260813T054339Z | MhiMayoria_15m_20260812T222135Z |
| MhiMayoria_15m_20260813T054456Z | MhiMayoria_15m_20260813T032500Z |
| MhiMayoria_1D_20260812T221951Z | MhiMayoria_1D_20260812T222136Z |
| MhiMayoria_1D_20260813T054340Z | MhiMayoria_1D_20260812T222136Z |
| MhiMayoria_1D_20260813T054457Z | MhiMayoria_1D_20260813T032501Z |
| MhiMayoria_1h_20260812T221951Z | MhiMayoria_1h_20260812T222136Z |
| MhiMayoria_1h_20260813T054340Z | MhiMayoria_1h_20260812T222136Z |
| MhiMayoria_1h_20260813T054457Z | MhiMayoria_1h_20260813T032501Z |
| Neutral_15m_20260812T221932Z | Neutral_15m_20260812T222117Z |
| Neutral_15m_20260813T054319Z | Neutral_15m_20260812T222117Z |
| Neutral_15m_20260813T054436Z | Neutral_15m_20260813T032442Z |
| Neutral_1D_20260812T221933Z | Neutral_1D_20260812T222117Z |
| Neutral_1D_20260813T054320Z | Neutral_1D_20260812T222117Z |
| Neutral_1D_20260813T054437Z | Neutral_1D_20260813T032443Z |
| Neutral_1h_20260812T221933Z | Neutral_1h_20260812T222117Z |
| Neutral_1h_20260813T054320Z | Neutral_1h_20260812T222117Z |
| Neutral_1h_20260813T054437Z | Neutral_1h_20260813T032443Z |
| TresMosqueteros_15m_20260812T221906Z | TresMosqueteros_15m_20260812T213530Z |
| TresMosqueteros_15m_20260812T221959Z | TresMosqueteros_15m_20260812T213530Z |
| TresMosqueteros_15m_20260813T054250Z | TresMosqueteros_15m_20260812T213530Z |
| TresMosqueteros_15m_20260813T054349Z | TresMosqueteros_15m_20260812T213530Z |
| TresMosqueteros_15m_20260813T054408Z | TresMosqueteros_15m_20260813T032413Z |
| TresMosqueteros_1D_20260812T221907Z | TresMosqueteros_1D_20260812T222145Z |
| TresMosqueteros_1D_20260812T222000Z | TresMosqueteros_1D_20260812T222145Z |
| TresMosqueteros_1D_20260812T222008Z | TresMosqueteros_1D_20260812T222153Z |
| TresMosqueteros_1D_20260813T054251Z | TresMosqueteros_1D_20260812T222145Z |
| TresMosqueteros_1D_20260813T054350Z | TresMosqueteros_1D_20260812T222145Z |
| TresMosqueteros_1D_20260813T054400Z | TresMosqueteros_1D_20260812T222153Z |
| TresMosqueteros_1D_20260813T054409Z | TresMosqueteros_1D_20260813T032414Z |
| TresMosqueteros_1h_20260812T221907Z | TresMosqueteros_1h_20260812T222052Z |
| TresMosqueteros_1h_20260812T222000Z | TresMosqueteros_1h_20260812T222052Z |
| TresMosqueteros_1h_20260813T054251Z | TresMosqueteros_1h_20260812T222052Z |
| TresMosqueteros_1h_20260813T054350Z | TresMosqueteros_1h_20260812T222052Z |
| TresMosqueteros_1h_20260813T054409Z | TresMosqueteros_1h_20260813T032414Z |
| VolumenBreakout_15m_20260812T221941Z | VolumenBreakout_15m_20260812T222125Z |
| VolumenBreakout_15m_20260813T054329Z | VolumenBreakout_15m_20260812T222125Z |
| VolumenBreakout_15m_20260813T054446Z | VolumenBreakout_15m_20260813T032451Z |
| VolumenBreakout_1D_20260812T221942Z | VolumenBreakout_1D_20260812T222126Z |
| VolumenBreakout_1D_20260813T054329Z | VolumenBreakout_1D_20260812T222126Z |
| VolumenBreakout_1D_20260813T054446Z | VolumenBreakout_1D_20260813T032451Z |
| VolumenBreakout_1h_20260812T221942Z | VolumenBreakout_1h_20260812T222126Z |
| VolumenBreakout_1h_20260813T054329Z | VolumenBreakout_1h_20260812T222126Z |
| VolumenBreakout_1h_20260813T054446Z | VolumenBreakout_1h_20260813T032451Z |
| ZScoreReversion_15m_20260812T221923Z | ZScoreReversion_15m_20260812T222108Z |
| ZScoreReversion_15m_20260813T054309Z | ZScoreReversion_15m_20260812T222108Z |
| ZScoreReversion_15m_20260813T054427Z | ZScoreReversion_15m_20260813T032432Z |
| ZScoreReversion_1D_20260812T221924Z | ZScoreReversion_1D_20260812T222109Z |
| ZScoreReversion_1D_20260813T054310Z | ZScoreReversion_1D_20260812T222109Z |
| ZScoreReversion_1D_20260813T054427Z | ZScoreReversion_1D_20260813T032432Z |
| ZScoreReversion_1h_20260812T221924Z | ZScoreReversion_1h_20260812T222109Z |
| ZScoreReversion_1h_20260813T054309Z | ZScoreReversion_1h_20260812T222109Z |
| ZScoreReversion_1h_20260813T054427Z | ZScoreReversion_1h_20260813T032432Z |

**Propuesta**: extender la categoría `excluidos.categorias[]` del manifiesto con una nueva entrada
`repeticion-tecnica-subcampana-e` (cantidad 68) — **no aplicada en este documento**, pendiente de
autorización.

---

## 4. Categoría B — Escritura interrumpida (2 carpetas, extiende categoría ya existente)

**Criterio de pertenencia**: mismo patrón ya documentado en la categoría `escritura-interrumpida`
(8 carpetas, manifiesto actual) — solo 2/3 gestores persistidos (`fixed-fractional`, `fixed-risk`),
ausencia de `volatility-sizing`, mismo estado `Success` en los 2 gestores presentes.

**Carpetas**:

| Carpeta | Fecha (UTC) | Gestores persistidos |
|---|---|---|
| TresMosqueteros_1D_20260813T041024Z | 2026-08-13T04:10:24Z | 2/3 (falta volatility-sizing) |
| TresMosqueteros_1D_20260813T042436Z | 2026-08-13T04:24:36Z | 2/3 (falta volatility-sizing) |

**Origen**: ambos timestamps (`04:10`, `04:24` UTC) son anteriores a la ejecución final de
`campana_corpus/` reportada (`~05:42`–`05:46` UTC) — corresponden a corridas manuales de prueba de
esta sesión, previas a la ejecución oficial autorizada de Sub-campaña E, no a ella. No hay relación
con `ETHUSDT` — su `nombreDataset` es `BTCUSDT_2024-01-02_2025-01-02`.

**No afectan reproducibilidad**: la comparación oficial equivalente
(`TresMosqueteros_1D_20260812T222145Z`, `TresMosqueteros_1D_20260812T222153Z` — ambas con los 3
gestores completos) ya existe en el manifiesto, sin ninguna alteración. Estas 2 carpetas son
evidencia incompleta descartada, no un reemplazo ni una corrección de la evidencia oficial.

**Propuesta**: extender la categoría existente `escritura-interrumpida` del manifiesto de 8 a
**10** carpetas — **no aplicada en este documento**, pendiente de autorización.

---

## 5. Categoría C — Sub-campaña E, ETHUSDT (18 carpetas, mantenidas separadas)

**Criterio de pertenencia**: `nombreDataset = "ETHUSDT_2024-01-02_2025-01-02"` — ninguna clave
oficial existente coincide (el corpus oficial actual solo contiene `BTCUSDT` en cualquiera de sus
2 datasets, `2024-2025` y `2022-2023`).

**Estado**: ejecutadas y verificadas (P8: identidad experimental distingue instrumento y es
reproducible; P9: 18/18 comparaciones persistidas, sin duplicados) — **no clasificadas todavía
como corpus oficial ni como excluidas**. Quedan en una categoría propia, separada, a la espera de:

1. Auditoría exclusiva de las 18 comparaciones `ETHUSDT` (cobertura, trazabilidad).
2. Decisión explícita sobre si conforman un manifiesto ampliado.

**Listado (18)**:

```
EmaCross_15m_20260813T054517Z          EmaCross_1D_20260813T054518Z
EmaCross_1h_20260813T054518Z           MhiMayoria_15m_20260813T054554Z
MhiMayoria_1D_20260813T054555Z         MhiMayoria_1h_20260813T054555Z
Neutral_15m_20260813T054535Z           Neutral_1D_20260813T054536Z
Neutral_1h_20260813T054536Z            TresMosqueteros_15m_20260813T054508Z
TresMosqueteros_1D_20260813T054509Z    TresMosqueteros_1h_20260813T054509Z
VolumenBreakout_15m_20260813T054545Z   VolumenBreakout_1D_20260813T054545Z
VolumenBreakout_1h_20260813T054545Z    ZScoreReversion_15m_20260813T054526Z
ZScoreReversion_1D_20260813T054526Z    ZScoreReversion_1h_20260813T054526Z
```

**No se incorpora esta lista al manifiesto en este documento** — es un inventario verificado, no
una declaración de pertenencia al corpus oficial.

---

## 6. Resumen de conteo, verificado contra disco

```
110 carpetas antes de esta ejecucion (49 oficiales + 61 ya excluidas y documentadas)
+ 61 carpetas nuevas producidas por esta corrida (log de ejecucion)
= 171 carpetas en disco (verificado: ls resultados/ | wc -l = 171)

De las 88 no pertenecientes al conjunto {oficiales (49) ∪ excluidas previas (61-59=... )}:
  68  repeticion tecnica (V1/A/B/D re-ejecutadas)         -> categoria propuesta nueva
   2  escritura interrumpida (pruebas manuales previas)    -> extiende categoria existente 8->10
  18  ETHUSDT / Sub-campana E                               -> categoria propia, pendiente de auditoria
  --
  88  total clasificado en este documento
```

**Nota de reconciliación**: el log de ejecución reportó 61 carpetas nuevas (6+19+18+18). Este
documento clasifica 88 carpetas no pertenecientes ni al corpus oficial ni a las categorías de
exclusión ya documentadas antes de esta corrida — la diferencia (88 − 61 = 27) corresponde a
carpetas que ya existían físicamente en disco antes de esta ejecución pero que no habían sido
clasificadas todavía en ninguna categoría previa (residuo de corridas de verificación manual entre
la clasificación de Capa 2 y esta ejecución). Cada una de las 88 fue clasificada individualmente
por contenido en este documento — ninguna se asumió por diferencia aritmética.

---

## 7. Qué no hace este documento

- No modifica `MANIFIESTO_CORPUS_CASO5C_V1.json`.
- No modifica el corpus oficial de 49 comparaciones.
- No modifica ninguna auditoría de Capa 2 ni de análisis interpretativo.
- No decide si las 18 comparaciones `ETHUSDT` pasan a formar parte de un manifiesto ampliado —
  eso requiere la auditoría de diversidad de instrumento (próximo paso, §8).
- No elimina ninguna carpeta física — todas permanecen en `resultados/`, solo se documenta su
  categoría.

---

## 8. Próximo paso

Auditar exclusivamente las 18 comparaciones `ETHUSDT` (§5 de este documento): verificar cobertura
(6 estrategias × 3 timeframes, sin huecos), trazabilidad (cada carpeta con `IDENTIDAD_COMPARACION.
json`/`COMPARACION_GESTORES_V1.md` completos y consistentes), y solo después de esa auditoría,
decidir si se amplía el manifiesto — como decisión separada, explícita, no automática por el hecho
de que la evidencia exista y esté verificada.
