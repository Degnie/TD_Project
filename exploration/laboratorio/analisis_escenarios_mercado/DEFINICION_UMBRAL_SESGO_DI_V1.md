# Definición del Umbral de Sesgo DI V1

Estado: **especificación de subfase — Fase 1.4-B, Paso 3-A**. Resuelve el diseño conceptual y
metodológico de `UmbralSesgoDI` (el parámetro que distingue Lateral de Ambiguo dentro de la zona de
ADX bajo, `PARAMETRIZACION_CLASIFICADOR_REGIMEN_V1.md §2.6`). **No fija un valor numérico** — a
diferencia de Periodo ADX (14) y Umbral de tendencia (25), no existe una convención externa
equivalente para este parámetro (D-030: solo entra como "Propuesto" un parámetro con referencia
externa objetiva; este no la tiene). Fijar un número aquí, sin resolver antes las 5 preguntas de
diseño pedidas, repetiría exactamente el error que motivó dejarlo pendiente.

---

## 1. ¿Qué significa "DI en disputa"?

`DI+` y `DI-` miden, respectivamente, la magnitud del movimiento direccional alcista y bajista
suavizado dentro del periodo de cálculo (`PARAMETRIZACION_CLASIFICADOR_REGIMEN_V1.md §2.2`). "DI en
disputa" significa que, aunque el `ADX` indica baja fuerza de tendencia consolidada (`ADX < 25`),
la diferencia entre `DI+` y `DI-` sigue siendo apreciable — es decir, dentro de la ventana de
cálculo hay episodios de presión alcista y presión bajista que se cancelan mutuamente en el
promedio de `ADX` (por eso el ADX es bajo), pero que no representan un genuino equilibrio: el precio
osciló entre ambas fuerzas sin que ninguna se consolidara, en vez de moverse con calma dentro de un
rango. Es la traducción operacional de "señales contradictorias" (`DEFINICION_ESTADOS_REGIMEN_V1.md §2`).

## 2. ¿Qué significa "DI balanceado"?

`DI+` y `DI-` están próximos entre sí en magnitud absoluta, **y** ambos son relativamente bajos — no
solo que sean parecidos entre sí (dos valores bajos y parecidos no es lo mismo que dos valores altos
y parecidos: el segundo caso indicaría alta actividad direccional en ambos sentidos simultáneamente,
que ya sería más cercano a "disputa" que a "calma"). "Balanceado" es la traducción operacional de
"ausencia de señal + ausencia de ruido" (`DEFINICION_ESTADOS_REGIMEN_V1.md §2`): el mercado no
muestra presión direccional relevante en ningún sentido.

**Consecuencia de diseño**: esto sugiere que `UmbralSesgoDI` no debería aplicarse únicamente a
`|DI+ - DI-|` en aislamiento — un `|DI+-DI-|` pequeño con `DI+` y `DI-` ambos altos (mucha actividad
en ambos sentidos, cancelándose) no es conceptualmente lo mismo que un `|DI+-DI-|` pequeño con
ambos bajos (poca actividad en cualquier sentido). Ver sección 3, Opción B/C, que aborda esto.

## 3. ¿El umbral será absoluto, relativo o adaptativo?

Tres familias posibles, sin seleccionar ninguna en este documento:

- **Opción A — Absoluto**: `UmbralSesgoDI` es un número fijo (ej. "si `|DI+-DI-| < 5`, es Lateral;
  si no, Ambiguo"), igual para todos los timeframes y todo el histórico. Más simple, pero no
  resuelve la observación de la sección 2 (no distingue "ambos DI bajos" de "ambos DI altos y
  parecidos").
- **Opción B — Relativo a la magnitud de DI**: `UmbralSesgoDI` se expresa como proporción de
  `DI+ + DI-` (ej. "`|DI+-DI-| / (DI+ + DI-) < X%`"), en vez de una diferencia absoluta. Responde
  directamente a la observación de la sección 2: normaliza contra el nivel general de actividad
  direccional en la ventana, no solo la diferencia entre ambos lados.
- **Opción C — Adaptativo por distribución histórica**: `UmbralSesgoDI` se deriva de la distribución
  observada de `|DI+-DI-|` en el propio dataset (ej. un percentil). Es la opción con mayor riesgo de
  violar la pregunta 4 (evitar ajuste contra BTC) si no se diseña con cuidado — un umbral "adaptado
  al dataset" es, por definición, calculado mirando el dataset, aunque no necesariamente mirando el
  *resultado de una estrategia* (la distinción es sutil y se retoma en la pregunta 4).

**No se selecciona ninguna opción en este documento.** La Opción B responde mejor a la observación
conceptual de la sección 2 sin introducir el riesgo de la Opción C, pero elegirla formalmente es una
decisión que corresponde a una aprobación explícita posterior, no a este análisis.

## 4. ¿Cómo evitar ajustar contra BTC?

Regla heredada directamente de D-018 (nunca resuelta con una excepción para este parámetro) y
extendida aquí con precisión:

- **Prohibido**: calcular `UmbralSesgoDI` ejecutando el clasificador sobre BTC/USDT con distintos
  valores y eligiendo el que produzca una distribución Ambiguo/Lateral "que se vea razonable" o que
  maximice/minimice alguna métrica del dataset ya conocido — esto es exactamente la selección
  retrospectiva que toda la Fase 1.4 existe para prevenir (`ESPECIFICACION_ANALISIS_ESCENARIOS_MERCADO_V1.md §2`).
- **Matiz sobre la Opción C (adaptativo)**: incluso si el umbral se deriva de percentiles del propio
  dataset (en vez de un valor fijo elegido a mano), sigue siendo aceptable **solo si** el
  procedimiento para derivarlo se fija primero (ej. "percentil 50 de `|DI+-DI-|` normalizado,
  siempre, sin excepciones") y se aplica igual sin importar qué distribución resulte — lo prohibido
  no es "usar información del dataset", es "elegir el resultado antes de fijar la regla". Esta
  distinción debe quedar explícita si en el futuro se opta por la Opción C.
- **Permitido**: usar convención externa si en el futuro se identifica una (ej. algún umbral de
  `|DI+-DI-|` ya publicado en literatura técnica derivada de Wilder o de plataformas de trading
  establecidas) — este documento no encontró una convención externa tan específica como la que
  existe para el periodo ADX (14) o el umbral de tendencia (25), pero no descarta que exista y no
  haya sido localizada.

## 5. ¿Cómo validar que no genera estados artificiales?

"Estado artificial" = una categoría que existe en el output del clasificador pero no corresponde a
ninguna condición real y diferenciada del mercado — por ejemplo, si `UmbralSesgoDI` quedara tan
ajustado que "Ambiguo" apareciera solo en 0.01% del dataset (artificialmente raro, sin utilidad
práctica) o en 95% del dataset (artificialmente dominante, indistinguible de "todo es Ambiguo").

**Validación propuesta, reutilizando infraestructura ya construida en Fase 1.4-A** (no se inventa
un método nuevo): una vez fijado un valor candidato de `UmbralSesgoDI` (por cualquiera de las 3
opciones de la sección 3), se ejecuta el mismo procedimiento de `EvaluadorClasificadores.cs` ya
usado para comparar A/B/C — cobertura, distribución por régimen, duración media, fragmentación —
sobre el candidato B extendido con 4 estados. Los mismos criterios de sensatez ya aplicados
informalmente en el análisis de A (D-025: "estabilidad extrema sin discriminación no es
suficiente") se aplican aquí: si Ambiguo resulta en 0% o en >80% del dataset en cualquier
timeframe, es evidencia de que el umbral no es sensato y debe reconsiderarse — **sin que
"reconsiderar" signifique ajustar mirando qué produce mejor resultado de estrategia** (eso seguiría
prohibido). Esta validación mide la **razonabilidad del instrumento como clasificador**, la misma
frontera que D-016 ya estableció, no su relación con ninguna estrategia.

---

## D-031 — Familia matemática aprobada (2026-08-11)

**Estado: ✅ Aprobado — Opción B (relativo)**, de las 3 familias descritas en la sección 3.
Fórmula: `|DI+ - DI-| / (DI+ + DI-)`. Motivo de la selección: responde directamente a la asimetría
identificada en la sección 2 (dos DI cercanos entre sí pero ambos bajos ≠ dos DI cercanos entre sí
pero ambos altos) sin el riesgo de ajuste retrospectivo de la Opción C (adaptativo). D-031 aprueba
únicamente la familia — el valor numérico del umbral sobre esta fórmula relativa sigue sin
definirse, ver `DEFINICION_VALOR_UMBRAL_SESGO_DI_V1.md`.

---

## Fuera de alcance (respetado)

No se fija un valor numérico de `UmbralSesgoDI` en este documento (resuelto en
`DEFINICION_VALOR_UMBRAL_SESGO_DI_V1.md`). No se implementa código. No se ejecuta ninguna
estrategia.

---

## Criterio de cierre del Paso 3-A

- ✓ "DI en disputa" y "DI balanceado" definidos conceptual y operacionalmente (secciones 1-2), con
  una observación de diseño relevante para la sección 3 (normalizar contra el nivel de actividad,
  no solo la diferencia absoluta).
- ✓ 3 familias de umbral (absoluto/relativo/adaptativo) documentadas con sus riesgos, sin
  seleccionar ninguna.
- ✓ Regla explícita para evitar ajuste contra BTC, con el matiz necesario sobre la Opción C
  (adaptativo) para que no se confunda "usar información del dataset" con "elegir el resultado".
- ✓ Método de validación contra estados artificiales definido, reutilizando la infraestructura ya
  construida en Fase 1.4-A en vez de inventar un mecanismo nuevo.
- ⏳ Selección de familia de umbral (Opción A/B/C) y su valor concreto — pendiente de decisión
  explícita antes de proceder al Paso 3-B (implementación de `ClasificadorRegimenV1`).
