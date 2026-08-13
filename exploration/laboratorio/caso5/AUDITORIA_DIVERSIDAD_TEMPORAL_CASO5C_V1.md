# Auditoría de Diversidad Temporal — Caso 5C

Estado: **documento de auditoría — evalúa evidencia real, no propone ni implementa Capa 2**.
Evalúa exclusivamente el resultado de la Sub-campaña D (`ESPECIFICACION_IMPLEMENTACION_
DIVERSIDAD_TEMPORAL_CASO5C_V1.md` §5), ejecutada tras la incorporación del dataset
`BTCUSDT_2022-01-01_2023-01-01` (`HALLAZGO_DATASET_TEMPORAL_2022_CASO5C_V1.md`, D-121, D-122). No
audita de nuevo el corpus V1/V2 (`AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V1.md`/`_V2.md`) más que para
compararlo estructuralmente con la evidencia nueva.

**Pregunta que responde este documento**: ¿qué evidencia nueva aporta el segundo período temporal,
y la infraestructura soporta comparar períodos manteniendo constantes los demás ejes?

**Preguntas que NO responde** (fuera de alcance, explícito): qué período es mejor; qué gestor es
mejor; si algún gestor debe recomendarse; si existe evidencia suficiente para diseñar Capa 2.

**Corpus auditado en este documento**: las 18 comparaciones generadas por la Sub-campaña D
(`caso5/resultados/`, rango de timestamps `2026-08-13T03:24:13Z`–`03:25:01Z`), contra el dataset
`BTCUSDT_2022-01-01_2023-01-01` vía la vista de compatibilidad `datasets/reales/BTCUSDT_2022/`.

---

## 1. Qué nueva evidencia aporta el segundo período

**18 comparaciones nuevas, 54 corridas individuales** (6 estrategias × 3 timeframes × 3 gestores),
todas en `Estado: Success` (54/54) — ninguna corrida falló ni quedó incompleta.

| Eje | Sub-campaña D (2022-2023) | Corpus V1+V2 (2024-2025) | Coincide |
|---|---|---|---|
| Instrumento | BTCUSDT | BTCUSDT | Sí — eje no varía (D-121) |
| Estrategias | 6/6 | 6/6 | Sí |
| Timeframes | 15m, 1h, 1D | 15m, 1h, 1D | Sí |
| Gestores | 3/3 (mismas identidades) | 3/3 | Sí |
| Período | 2022-01-01–2023-01-01 | 2024-01-02–2025-01-02 | No — único eje que varía |

**Identidad de gestor verificada sin cambio**: las 3 identidades (`fixed-fractional:v1:riesgo=0.1`,
`fixed-risk:v1:monto=50`, `volatility-sizing:v1:ventana=20:base=0.1:desviacionReferencia=2`) son
idénticas a las usadas en V1/V2 — ningún parámetro de gestor cambió entre corpus.

**El corpus acumulado pasa de 31 a 49 comparaciones** (147 corridas individuales), ahora
distribuidas en 2 períodos temporales distintos del mismo instrumento.

---

## 2. La infraestructura sí soporta comparar períodos manteniendo constantes los demás ejes

Esta es la pregunta estructural central de la Sub-campaña D. Se verificó por mecanismo, no por
inspección de código, ejecutando `EjecutorProtocolo.Ejecutar` directamente (fuera de
`ComparadorGestores`, sin modificarlo) sobre la misma estrategia/parámetros/timeframe con cada
dataset (P6, `ProgramCampanaCorpus.cs`):

- **`IdentidadExperimentoCompleta.HashCompuesto` distingue el período**: el hash calculado sobre el
  dataset 2024-2025 y el calculado sobre el dataset 2022-2023 son distintos entre sí, con
  estrategia/parámetros/timeframe idénticos. Esto confirma que el dataset es, en efecto, un eje que
  el sistema de identidad reconoce y distingue — no una variable oculta que pudiera confundirse con
  otra.
- **`IdentidadExperimentoCompleta.HashConfiguracionEconomica` permanece invariante entre períodos**:
  el hash económico (instrumento/costes/sizing) es idéntico entre la corrida 2024-2025 y la corrida
  2022-2023 para el mismo gestor. Confirma que la comparación temporal no arrastra consigo ningún
  cambio accidental de configuración económica — el único eje que varió fue el período, tal como
  exigía el criterio de atribución causal de D-121.
- **Reproducibilidad confirmada sobre el dataset 2022-2023**: dos ejecuciones idénticas de
  `EjecutorProtocolo.Ejecutar` sobre el mismo dataset/estrategia/parámetros producen el mismo
  `HashCompuesto` — el dataset nuevo no introduce ninguna fuente de no-determinismo que no existiera
  ya en el dataset original.

**Separación de la vista de compatibilidad respecto a la fuente de verdad**: las 13 copias en
`datasets/reales/BTCUSDT_2022/{tf}/` fueron verificadas SHA-256-idénticas a los archivos congelados
en `datasets/reales/BTCUSDT/{tf}_2022/` antes de ejecutar la campaña. La vista existe únicamente
porque `EjecutorProtocolo` (Caso 1, congelado) usa el mismo token de timeframe para la subcarpeta y
el sufijo del archivo — es una adaptación técnica de acceso, no un segundo dataset experimental. El
dataset congelado con sufijo (`*_2022/`) sigue siendo la única fuente de verdad.

**Ningún componente congelado fue modificado**: `ComparadorGestores.cs` y
`PersistidorComparaciones.cs` se usaron sin cambios — la Sub-campaña D reutilizó `EjecutorMatriz`
(ya existente en `campana_corpus/ProgramCampanaCorpus.cs` desde V1/V2), extendido únicamente para
aceptar `DirDatasets`/`NombreDataset` como parámetros en vez de constantes fijas.

**Conclusión de esta sección**: sí, la infraestructura soporta comparar períodos distintos
manteniendo constantes instrumento/estrategias/timeframes/gestores/configuración económica, con
verificación mecánica (no solo por diseño) de que el período es el único eje que cambió.

---

## 3. Qué patrones ya observados en V1/V2 se replican o no en el nuevo período

Sección puramente descriptiva — documenta qué aparece en los datos, sin ordenar ni calificar
gestores o períodos.

**El patrón de `DrawdownMaximoPct`/`CashFinal` extremos en timeframes cortos aparece también en
2022-2023**: en `TresMosqueteros/15m`, `fixed-fractional` produce `DrawdownMaximoPct≈100%` y
`CashFinal≈0`; `fixed-risk` produce `CashFinal` negativo (`-18619.89...`). En `TresMosqueteros/1D`
del mismo dataset, ningún gestor alcanza `DrawdownMaximoPct≈100%`. Esta relación entre timeframe
corto y valores extremos, señalada por primera vez en la auditoría V1 sobre 2024-2025, se repite de
forma consistente en el dataset 2022-2023 — no era exclusiva del período original.

**`ZScoreReversion` (ventana=5, umbralEntrada=2.0, umbralSalida=0.5) tampoco genera operaciones en
2022-2023**: las 9 corridas (3 timeframes × 3 gestores) muestran `PnLTotal: 0`,
`DrawdownMaximoPct: 0`, `CashFinal: 1000`, `ProfitFactor: null` — mismo resultado trivial ya
observado en el corpus V2 sobre 2024-2025. Las 9 corridas están en `Success`; la estrategia se
ejecutó correctamente, simplemente no encontró condiciones de entrada bajo estos parámetros en
ninguno de los dos períodos. No se investiga aquí si otros valores de parámetro producirían
actividad (calibración observando resultados, D-030, fuera de alcance).

**Ninguna corrida de la Sub-campaña D quedó en `Incomplete`/`Failed`**: a diferencia de la
sub-campaña C de V2 (que incluyó deliberadamente un caso de dataset inexistente), la Sub-campaña D
no contiene evidencia de corrida no exitosa — todas las 54 filas son `Success`, porque la vista de
compatibilidad resolvió correctamente todas las combinaciones declaradas.

---

## 4. Qué limitaciones permanecen

**Sin diversidad de instrumento**: la incorporación de `BTCUSDT 2022-2023` mejora la dimensión
temporal, pero **no resuelve la diversidad de instrumento** — las 49 comparaciones acumuladas siguen
siendo, en su totalidad, sobre `BTCUSDT`. Cualquier conclusión futura basada en este corpus seguirá
limitada a un único activo, independientemente de cuántos períodos temporales se agreguen.

**Solo 2 períodos temporales representados**: la Sub-campaña D aporta un segundo punto, no una
serie — no permite distinguir si un patrón observado es estable a través del tiempo o específico de
estos dos rangos particulares (2022-2023, 2024-2025). Un tercer período sería necesario para
empezar a distinguir "constante entre períodos" de "coincidencia entre dos muestras".

**Ningún caso de evidencia parcial en el nuevo período**: a diferencia del corpus V2 (que incluye 1
comparación con las 3 filas en `Incomplete`), la Sub-campaña D no generó ningún caso de corrida no
exitosa — no hay evidencia todavía de cómo se comporta la comparación de gestores ante un fallo
sobre el dataset 2022-2023 específicamente (aunque el mecanismo ya está probado sobre 2024-2025 en
V2, y no depende del dataset).

**Sin repetición dentro de la Sub-campaña D**: cada una de las 18 combinaciones se ejecutó una sola
vez — a diferencia de V2 (que repitió explícitamente la matriz V1 en su sub-campaña B para verificar
reproducibilidad con datos reales), la Sub-campaña D confía en la verificación de reproducibilidad
hecha por P6 sobre una sola combinación (Tres Mosqueteros/15m), no sobre las 18.

**Volumen del corpus acumulado**: 49 comparaciones, 147 corridas — mayor que V1+V2 (31/93), pero
sigue siendo una muestra de un único instrumento en dos ventanas temporales, no un corpus con
diversidad de mercado.

---

## Fuera de alcance de este documento

No se determina qué período produce mejores resultados. No se determina qué gestor es preferible en
ninguno de los dos períodos. No se recomienda ningún gestor. No se evalúa si el corpus acumulado
(49 comparaciones) es suficiente para diseñar Capa 2 — esa evaluación queda para una decisión
posterior explícita, sobre el estado del corpus completo. No se investiga por qué `ZScoreReversion`
no genera operaciones (D-030).

---

## Conclusión

La Sub-campaña D aportó 18 comparaciones nuevas (49 acumuladas) y confirmó, con verificación
mecánica y no solo por diseño, que la infraestructura existente (`ComparadorGestores`/
`PersistidorComparaciones`/`EjecutorProtocolo`) soporta comparar períodos temporales distintos del
mismo instrumento manteniendo constantes los demás ejes — el período queda aislado como única
variable mediante `HashCompuesto` (distinto entre períodos) y `HashConfiguracionEconomica`
(idéntico entre períodos). Los patrones de degeneración económica en timeframes cortos y la
ausencia de actividad de `ZScoreReversion`, observados por primera vez en el corpus 2024-2025, se
replican en el dataset 2022-2023. La incorporación de este segundo período **mejora la dimensión
temporal del corpus, pero no resuelve la diversidad de instrumento** — cualquier conclusión futura
basada en este corpus seguirá limitada a `BTCUSDT`. La evaluación de si el corpus acumulado
(49 comparaciones, 2 períodos, 6 estrategias, 3 gestores) es suficiente para diseñar Capa 2 queda
pendiente de una decisión posterior explícita, fuera del alcance de este documento.
