# Auditoría de Corpus Comparativo — Caso 5C V2

Estado: **documento de auditoría — evalúa evidencia real, no propone ni implementa Capa 2**.
Continúa `AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V1.md`, evaluando el corpus ampliado tras
`PROPUESTA_EXPANSION_CORPUS_CASO5C_V2.md`/`ESPECIFICACION_IMPLEMENTACION_EXPANSION_CORPUS_CASO5C_
V2.md`. Responde las mismas 5 preguntas que V1, sobre el corpus acumulado completo (V1 + V2). No
determina qué gestor es mejor, no recomienda, no fija ningún criterio de recomendación.

**Corpus auditado**: las 6 comparaciones de V1 (commit `c83c7a6`) + las 25 comparaciones generadas
por la ejecución real de V2 (sub-campaña V1 repetida internamente como parte del código de
expansión, sub-campaña A, sub-campaña B, sub-campaña C) = **31 comparaciones totales acumuladas**
en `caso5/resultados/`, excluyendo la carpeta preexistente de pruebas técnicas
(`TresMosqueteros_1D_20260812T204012Z`, mismo criterio de exclusión que V1 §0).

**Nota administrativa**: la primera ejecución de la campaña V2 se detuvo en P5 por un supuesto
incorrecto en el código de verificación (asumía `Estado: Failed` para dataset inexistente; el
comportamiento real de `EjecutorProtocolo` es `Estado: Incomplete` — ver
`EjecutorProtocolo.cs:22`). Esa ejecución sí alcanzó a persistir sus comparaciones antes de
detenerse (P4 ya había pasado). Tras corregir la aserción, se re-ejecutó la campaña completa. Ambas
ejecuciones dejaron evidencia en `caso5/resultados/` — ninguna se descartó ni se borró (mismo
criterio de no eliminar evidencia automáticamente ya establecido). Este documento audita el corpus
resultante de la **segunda ejecución** (la que completó las 5 verificaciones), identificada por el
rango de timestamps `2026-08-12T22:19*`–`22:22*`; las carpetas de la primera ejecución parcial
(`22:19*` más tempranas, mismo estrategia/timeframe, timestamps distintos) quedan como evidencia
adicional válida pero no se cuentan por separado en este análisis para evitar doble conteo de las
mismas combinaciones.

**Precisión posterior (registrada durante la implementación de Caso 5C Capa 2,
`ESPECIFICACION_IMPLEMENTACION_CASO5C_CAPA2_V1.md`, D-123)**: el criterio de identificación usado
arriba (rango de timestamps) resultó insuficiente para separar mecánicamente las 25 comparaciones
auditadas de las carpetas físicas — el rango `22:19*`–`22:22*` contiene **52 carpetas**, no 25,
porque ambas ejecuciones de la campaña (la interrumpida y la completa) caen dentro de la misma
franja de tiempo, entrelazadas cronológicamente por estrategia. Este documento **no cambia su
resultado ni sus conclusiones** — el corpus de 31 comparaciones (6 V1 + 25 V2) que analizó sigue
siendo el correcto, y así quedó confirmado por inspección de contenido (no de timestamp) de las 52
carpetas: 25 con ejecución completa (3 gestores, dataset esperado, última repetición cronológica de
cada combinación) + 25 de la primera pasada interrumpida + 2 restos de una escritura interrumpida
(solo 2 de 3 gestores). La clasificación completa, y el criterio de pertenencia basado en contenido
en vez de timestamp, quedan formalizados en `resultados/MANIFIESTO_CORPUS_CASO5C_V1.json`. Se deja
esta nota para que una auditoría futura no necesite reconstruir la diferencia entre 49 (corpus
oficial acumulado tras Sub-campaña D) y el total físico de carpetas en `caso5/resultados/` — el
manifiesto es la fuente de verdad para esa distinción, no el rango de timestamps documentado aquí.

---

## 1. Qué corpus existe

**31 comparaciones persistidas** (6 V1 + 25 V2), con 93 corridas individuales representadas
(6×3 + 25×3, aunque la fila de la sub-campaña C no contiene métricas).

| Origen | Comparaciones | Corridas internas | Estado |
|---|---|---|---|
| V1 (original, commit `c83c7a6`) | 6 | 18 | 18/18 Success |
| V2 — sub-campaña V1 (repetida como parte de la ejecución de expansión) | 6 | 18 | 18/18 Success |
| V2 — sub-campaña A (4 estrategias nuevas) | 12 | 36 | 36/36 Success |
| V2 — sub-campaña B (repetición explícita de V1) | 6 | 18 | 18/18 Success |
| V2 — sub-campaña C (evidencia parcial) | 1 | 3 | 3/3 Incomplete |

**92 de 93 corridas en `Success`, 3 en `Incomplete`** (todas de la sub-campaña C, por diseño). El
corpus contiene ahora, por primera vez, un caso representado de evidencia no exitosa — cierra la
limitación 4 identificada en la auditoría V1.

**Identidad verificada**: las 3 identidades de gestor (`fixed-fractional:v1:riesgo=0.1`,
`fixed-risk:v1:monto=50`, `volatility-sizing:v1:ventana=20:base=0.1:desviacionReferencia=2`) son
idénticas en las 31 comparaciones — ninguna variación accidental de parámetro entre V1 y V2.

---

## 2. Qué diversidad contiene

**Estrategias**: 6 de 6 congeladas en el laboratorio — Tres Mosqueteros, Ema Cross, ZScore
Reversion, Neutral, Volumen Breakout, Mhi Mayoria. **Cierra por completo la limitación 3** de la
auditoría V1 (antes solo 2 de 6).

**Timeframes**: siguen siendo 3 — `15m`, `1h`, `1D`. No se amplió (fuera del alcance mínimo de V2,
§2 de la propuesta).

**Datasets**: sigue siendo 1 — `BTCUSDT_2024-01-02_2025-01-02`. **La limitación 2 de la auditoría
V1 permanece abierta**, tal como la propuesta V2 anticipó explícitamente que ocurriría (§1: "no
puede cerrarse por completo con lo ya disponible").

**Gestores**: siguen siendo los mismos 3, cobertura completa desde V1.

**Repetición**: por primera vez, 2 combinaciones idénticas (Tres Mosqueteros/Ema Cross × 15m/1h/1D)
existen dos veces en el corpus con parámetros idénticos — cierra la limitación 1 de la auditoría V1.

**Evidencia parcial**: por primera vez, 1 comparación con las 3 filas en estado no exitoso — cierra
la limitación 4 de la auditoría V1.

---

## 3. Qué comparaciones están representadas

**Reproducibilidad verificada con datos reales, no solo por diseño del mecanismo**: se comparó
directamente `TresMosqueteros_1D` de V1 original contra su repetición en la sub-campaña B de V2 —
**`PnLTotal`, `DrawdownMaximoPct`, `ProfitFactor`, `ExposicionMaxima`, `CashFinal`, `EquityFinal`
son idénticos hasta el último dígito decimal** entre ambas ejecuciones. Esto confirma, con
evidencia real (no solo con el mecanismo de identidad ya verificado por P4 de Caso 5C Capa 1), que
el sistema completo (estrategia → motor → métricas → comparación → persistencia) es determinista
extremo a extremo sobre el mismo dataset/configuración.

**Patrón de degeneración económica, ahora observado en más estrategias**: el mismo patrón que la
auditoría V1 señaló (§3: `DrawdownMaximoPct > 100%` y `CashFinal` negativo con `FixedFractional`/
`FixedRisk` en timeframes cortos) se repite en 3 de las 4 estrategias nuevas en `15m`
(`Neutral`, `VolumenBreakout`, `MhiMayoria`) — el patrón no es exclusivo de `TresMosqueteros`/
`EmaCross`, aparece de forma consistente entre estrategias distintas bajo el mismo timeframe corto.
`VolatilitySizing` vuelve a ser, en todas las combinaciones revisadas, el único gestor que se
mantiene con `CashFinal` positivo y `DrawdownMaximoPct` acotado.

**Hallazgo nuevo, no anticipado por V1**: `ZScoreReversion` (ventana=5, umbralEntrada=2.0,
umbralSalida=0.5) no generó ninguna operación en ninguno de sus 3 timeframes —
`PnLTotal: 0`, `DrawdownMaximoPct: 0`, `CashFinal: 1000` (capital inicial sin cambio) en las 9
corridas (3 timeframes × 3 gestores). Las 9 corridas son `Success` — la estrategia se ejecutó
correctamente, simplemente no encontró condiciones de entrada bajo estos parámetros sobre este
dataset. **Esta comparación existe en el corpus pero no aporta evidencia sobre diferencias entre
gestores** — cuando ninguna operación ocurre, los 3 gestores producen el mismo resultado trivial
(sin actividad que dimensionar). No se investiga aquí si otros valores de `ventana`/`umbralEntrada`
producirían actividad — eso sería calibrar un parámetro observando resultados (D-030), fuera de
alcance de esta auditoría.

---

## 4. Qué limitaciones tiene

**Limitaciones cerradas por V2**:
- ~~Sin repetición~~ — cerrada, con confirmación de reproducibilidad byte-exacta (§3).
- ~~Solo 2 de 6 estrategias~~ — cerrada, 6/6 representadas.
- ~~Sin casos de corrida fallida~~ — cerrada, aunque el estado real es `Incomplete`, no `Failed`
  (el corpus no distingue todavía entre ambos tipos de evidencia parcial — ver limitación nueva
  abajo).

**Limitaciones que permanecen abiertas**:
- **Sin diversidad de dataset/instrumento** — declarada irresoluble con el repositorio actual desde
  la propuesta V2, confirmada aquí sin cambios. Sigue siendo la limitación estructural más
  relevante: toda observación de este corpus, incluida la reproducibilidad y los patrones de
  degeneración, está condicionada a `BTCUSDT` en una única ventana temporal.
- **Solo 1 tipo de evidencia parcial representado**: la sub-campaña C solo cubre `Incomplete`
  (dataset inexistente) — el corpus no contiene ningún caso de `Failed` propiamente dicho. Si
  `Success`/`Failed`/`Incomplete` requieren tratamiento distinto en una futura Capa 2, el corpus
  actual no permite verificar el caso `Failed`.
- **Una estrategia sin evidencia útil**: `ZScoreReversion` contribuye 3 comparaciones sin actividad
  real (§3) — de las 31 comparaciones del corpus, 3 (~10%) no aportan información sobre diferencias
  entre gestores.
- **Sin repetición de las 4 estrategias nuevas de la sub-campaña A**: solo Tres
  Mosqueteros/Ema Cross tienen repetición verificada — el hallazgo de reproducibilidad extremo a
  extremo (§3) no está confirmado todavía para ZScore Reversion/Neutral/Volumen
  Breakout/Mhi Mayoria.
- **Volumen todavía modesto en términos absolutos**: 31 comparaciones, 93 corridas — mayor que V1
  (6/18), pero D-119 (aún sin valores fijados) probablemente seguiría considerando esto una muestra
  inicial, no un corpus maduro, especialmente dada la limitación de dataset único.

---

## 5. ¿La evidencia actual permite o no diseñar Capa 2?

**Evidencia todavía insuficiente, con una brecha estructural que no depende de más campañas de
este tipo.**

Razones:
- La limitación más severa identificada en V1 — diversidad de dataset/instrumento — **no se pudo
  cerrar** y no es cerrable ejecutando más comparaciones sobre el mismo dataset. Cualquier patrón
  observado en este corpus (incluido el rol favorable de `VolatilitySizing`) sigue condicionado a
  `BTCUSDT` en esta ventana temporal específica — una tercera expansión que solo agregue más
  timeframes o repeticiones sobre el mismo dataset no resolvería esto.
- Las 4 estrategias nuevas no tienen repetición verificada — la confirmación de reproducibilidad
  extremo a extremo (§3) es válida solo para 2 de las 6 estrategias del corpus.
- Solo 1 de 2 tipos de evidencia no exitosa está representado.
- ~10% del corpus (`ZScoreReversion`) no aporta señal útil bajo los parámetros usados.

**Lo que sí cambió respecto a V1**: la infraestructura demostró, con datos reales (no solo por
diseño), ser determinista extremo a extremo, y ahora cubre las 6 estrategias congeladas y un caso
de evidencia parcial. El sistema quedó **mejor caracterizado**, aunque la conclusión de suficiencia
para Capa 2 sigue siendo negativa.

**Recomendación de alcance para una futura decisión** (no vinculante, no es una nueva propuesta):
si se busca cerrar la limitación de diversidad de dataset, la vía no es una nueva campaña sobre
`BTCUSDT` — requeriría obtener un segundo dataset real (otro instrumento o otro rango temporal), lo
cual está fuera del alcance de lo que el repositorio provee hoy y sería una decisión de
infraestructura distinta a una campaña de evidencia.

---

## Fuera de alcance de este documento

No se determina qué gestor es mejor. No se recomienda ningún gestor. No se define ningún criterio
de recomendación ni umbral de D-119. No se decide si se busca o descarga un segundo dataset. No se
investiga por qué `ZScoreReversion` no generó operaciones (sería calibración de parámetros
observando resultados, D-030).

---

## Conclusión

El corpus creció de 6 a 31 comparaciones, cerrando 3 de las 4 limitaciones que la auditoría V1
identificó (repetición, cobertura de estrategias, evidencia parcial), y confirmó con datos reales
—no solo por diseño del mecanismo— que el sistema es determinista extremo a extremo. **La evidencia
sigue siendo insuficiente para diseñar Capa 2**, y la limitación que queda abierta (diversidad de
dataset/instrumento) no es resoluble con una campaña adicional del mismo tipo — es una limitación
del repositorio, no de la disciplina experimental seguida hasta ahora.
