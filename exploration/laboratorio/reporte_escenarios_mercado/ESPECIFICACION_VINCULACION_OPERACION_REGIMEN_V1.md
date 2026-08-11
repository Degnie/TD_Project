# Especificación de Vinculación Operación ↔ Régimen V1

Estado: **especificación — Fase 1.5-A del Caso 1**. Documento de diseño, no implementación.
Responde a D-036 (trazabilidad temporal obligatoria) y D-037 (correlación ≠ causalidad),
registradas en la auditoría de Fase 1.5. No se modifica `IStrategy`, `BacktestRunner`,
`InfoOperacionResuelta`, `Fill`, `Candle`, `ClasificadorRegimenV1.cs` ni ninguna estrategia
(`EstrategiaTresMosqueteros.cs`/`EstrategiaMhiMayoria.cs`).

---

## 1. Verificación del hallazgo — dónde está realmente el dato

Antes de diseñar la solución, se verificó el código real (no se asumió nada de la fase anterior):

- `InfoOperacionResuelta(int OperacionId, int MartingalasUsadas, bool Gano)` — confirmado sin
  timestamp (`exploration/EstrategiaTresMosqueteros.cs:109`).
- El callback `_onOperacionResuelta` se invoca **dentro** de `Observar(DataSlice dataSlice)`, en
  el mismo punto donde la estrategia ya tiene `dataSlice.VelaActual` — y `Candle.Timestamp` existe
  (`src/Domain/Shared/Candle.cs:5`). Es decir: **el timestamp de la vela de cierre está disponible
  en el instante exacto en que se resuelve la operación**, pero hoy no se captura ni se pasa al
  callback (`exploration/EstrategiaTresMosqueteros.cs:67`,
  `exploration/EstrategiaMhiMayoria.cs:56`).
- El consumidor actual del callback (`exploration/laboratorio/evaluacion_multi_tf/Program.cs:52-53`)
  es `operaciones.Add` — una lista simple de `InfoOperacionResuelta`, sin ningún dato adicional.

**Corrección respecto a la especificación de Fase 1.5** (`ESPECIFICACION_REPORTE_ESCENARIOS_
MERCADO_V1.md §2`): esa versión proponía correlacionar por `ResultadoBacktest.Fills`/`OperacionId`.
Verificado ahora que **`Fill` no tiene `OperacionId`** (`src/Domain/Shared/Fill.cs`) — no existe
ese campo para correlacionar. La vía real y más directa es la vela ya disponible dentro de
`Observar()`, no los Fills del motor.

---

## 2. Fuente temporal de la operación

**Decisión de diseño (no ambigua, se registra aquí directamente)**: el timestamp se toma de
`dataSlice.VelaActual.Timestamp` en el mismo ciclo donde se invoca `_onOperacionResuelta` — es
literalmente la vela cuyo color determina `acerto` en ese `Observar()`. No requiere inferencia
externa ni búsqueda posterior: es un dato que la estrategia ya tiene en mano en el momento exacto
de resolución.

**Cómo exponerlo sin tocar el contrato existente**: `InfoOperacionResuelta` es un tipo definido en
`exploration/` (no en `src/`), por lo que agregar un campo a este record **no es una modificación
de contratos del motor** — es una extensión de instrumentación de laboratorio ya opcional por
diseño (comentario en `EstrategiaTresMosqueteros.cs:28-32`: "Instrumentacion OPCIONAL exclusiva de
analisis"). Se añadiría `TimestampResolucion` (o nombre equivalente) al record, y ambas estrategias
pasarían `velaActual.Timestamp` en la línea donde ya invocan el callback. Esto no se implementa en
este documento — se señala como el cambio mínimo necesario del Paso 1 de implementación.

---

## 3. Regla de asignación operación → régimen

Con el timestamp de resolución disponible, la asignación es una búsqueda directa: el timestamp se
busca en la salida ya congelada de `ClasificadorRegimenV1.Clasificar()` (lista de
`VentanaClasificada`, cada una con `InicioUtcMs`), tomando la ventana cuyo rango contiene ese
timestamp. No hay aproximación ni inferencia por posición — es coincidencia exacta de rango
temporal (D-036).

Si el timestamp de resolución cae en la ventana de calentamiento del clasificador (primeras
`2 × PeriodoAdx` velas, sin clasificación — ver `CLASIFICADOR_REGIMEN_V1.md`, sección "Tratamiento
de bordes"), la operación no tiene régimen asignable. Tratamiento: sección 5.

---

## 4. Punto crítico — qué vela representa la operación (entrada vs. resolución)

Pregunta abierta señalada en la auditoría de Fase 1.5, con 3 opciones. Antes de presentarlas, un
dato verificado que acota la respuesta: en ambas estrategias, la operación completa (intento
inicial + martingalas) puede abarcar varios ciclos de `Observar()` — cada martingala reabre en el
ciclo siguiente (`Fase.EsperandoReapertura`) — por lo que la vela de entrada inicial y la vela de
resolución final casi nunca son la misma vela cuando hay martingala.

**Opción A — Régimen de la vela de resolución** (la que ya se identificó en la sección 2: la vela
donde `acerto`/`_martingalasUsadas` se define y se invoca el callback).
- Ventaja: coincide exactamente con el resultado financiero/operacional de la operación.
- Riesgo: si hubo martingalas, el régimen puede haber cambiado varias velas después de que la
  estrategia tomó la decisión de entrar.
- Costo de implementación: ninguno adicional — es el timestamp ya identificado en sección 2.

**Opción B — Régimen de la vela de entrada inicial** (el ciclo donde `_operacionIdActual` se
asigna, `EstrategiaTresMosqueteros.cs:95` / `EstrategiaMhiMayoria.cs:102`).
- Ventaja: representa el régimen vigente en el momento en que la estrategia decidió apostar —
  más fiel a "cómo se comporta la estrategia según el régimen que ve al decidir".
- Riesgo: si la operación se resuelve varias velas después (con martingala), el resultado
  financiero puede corresponder a un tramo de mercado ya distinto al que motivó la entrada.
- Costo de implementación: requiere capturar el timestamp en el ciclo de apertura (línea de
  `_operacionIdActual = _siguienteOperacionId++`), no en el de resolución — un punto de
  instrumentación distinto al de la sección 2, pero del mismo tipo (dato ya disponible en
  `dataSlice.VelaActual.Timestamp` en ese ciclo).

**Opción C — Guardar ambos** (régimen de entrada y régimen de resolución, como dos campos
separados en el reporte).
- Ventaja: máxima trazabilidad — permite responder tanto "¿en qué régimen entró?" como "¿en qué
  régimen cerró?", y detectar cuántas operaciones cambiaron de régimen durante su propia
  resolución (dato adicional, no solicitado explícitamente pero disponible sin costo extra una
  vez que se capturan ambos timestamps).
- Riesgo: el reporte de Fase 1.5 (`ESPECIFICACION_REPORTE_ESCENARIOS_MERCADO_V1.md §3`) definió
  una partición exhaustiva de operaciones por régimen — con dos timestamps por operación, hay que
  decidir explícitamente cuál de los dos se usa para esa partición (probablemente resolución,
  por ser la que corresponde al resultado financiero), y el otro quedaría como dato adicional, no
  como el criterio de agrupación oficial.
- Costo de implementación: captura de dos timestamps (dos puntos de instrumentación) en vez de
  uno; el resto de la lógica de búsqueda (sección 3) se ejecuta dos veces por operación.

**Esta decisión no se toma en este documento** — pendiente de selección explícita por el auditor
antes de iniciar el Paso 1 de implementación.

---

## 5. Caso donde no existe vela exacta / régimen no asignable

Dos situaciones posibles, ambas ya verificadas contra el comportamiento real del motor y del
clasificador:

1. **Timestamp de operación cae en la ventana de calentamiento del clasificador** (sección 3):
   la operación no tiene régimen — se etiqueta explícitamente como "Sin régimen (ventana de
   calentamiento)", no se descarta ni se fuerza a ningún estado existente. Esta categoría es
   distinta de "Ambiguo" (que sí es un régimen calculado) y debe declararse aparte en el reporte.
2. **Operación abierta al cierre del dataset** (`OperacionAbiertaAlCierre`, campo ya existente en
   `PerfilMultiTf` desde Fase 1.2): por definición no tiene `InfoOperacionResuelta` — nunca se
   invoca el callback para ella — por lo tanto no participa de la vinculación operación↔régimen en
   absoluto. Se mantiene fuera de la partición exhaustiva de este análisis, igual que ya está
   fuera de `OperacionesCompletadas` en `AnalizadorOperacional.cs`.

---

## 6. Tratamiento de martingalas

`InfoOperacionResuelta.MartingalasUsadas` ya identifica cuántos reintentos tuvo la operación. La
vinculación operación↔régimen es **por operación lógica completa**, no por intento individual —
consistente con el diseño ya establecido de que la estrategia es "la única fuente de verdad sobre
qué intentos pertenecen a la misma operación lógica" (comentario en
`EstrategiaTresMosqueteros.cs:30-32`). No se crea un régimen por cada martingala; la pregunta de
la sección 4 (qué vela representa la operación) ya cubre cómo tratar el hecho de que una operación
con martingala se extiende sobre varias velas.

---

## 7. Evidencia generada

El resultado de esta vinculación, una vez implementada, es una estructura intermedia (nombre
tentativo, no fijado aquí): una lista de `(OperacionId, Escenario)` — o `(OperacionId, Escenario
Entrada, Escenario Resolución)` si se aprueba la Opción C — por corrida (estrategia × timeframe).
Esta estructura es el insumo directo del Paso 2 (asignación, ya cubierto por este documento) y
Paso 3 (métricas por escenario, sección 3 de
`ESPECIFICACION_REPORTE_ESCENARIOS_MERCADO_V1.md`) — no se implementa aquí.

Verificación de integridad requerida antes de aceptar esta vinculación como válida: la suma de
operaciones vinculadas a un régimen (incluyendo "Sin régimen") debe igualar exactamente
`OperacionesCompletadas` de esa corrida (mismo principio de partición exhaustiva ya usado en
Fase 1.2/1.3).

---

## Fuera de alcance (respetado)

No se implementa código en este documento. No se modifica `InfoOperacionResuelta`,
`EstrategiaTresMosqueteros.cs`, `EstrategiaMhiMayoria.cs`, `ClasificadorRegimenV1.cs`,
`BacktestRunner` ni ningún contrato de `src/`. No se selecciona la Opción A/B/C de la sección 4 —
queda pendiente de decisión explícita. No se calculan métricas por régimen (eso es Fase 1.5, Paso
3, posterior a este documento).

---

## Pregunta pendiente para decisión del auditor

**¿Qué vela representa el régimen de una operación: la de entrada (A), la de resolución (B), o
ambas (C)?** — ver sección 4 para ventajas/riesgos/costo de cada opción.

---

## Criterio de cierre de Fase 1.5-A (diseño)

- ✓ Hallazgo de Fase 1.5 verificado directamente contra el código (sección 1) — se corrige la vía
  de solución propuesta anteriormente (Fills no tiene `OperacionId`; la fuente real es
  `dataSlice.VelaActual.Timestamp` dentro de `Observar()`).
- ✓ Fuente temporal de la operación identificada y localizada con precisión de línea (sección 2).
- ✓ Regla de asignación operación→régimen definida, sin aproximación ni inferencia (sección 3,
  D-036).
- ⏳ Punto crítico (vela de entrada vs. resolución vs. ambas) presentado con 3 opciones — pendiente
  de selección explícita (sección 4).
- ✓ Caso sin vela exacta / régimen no asignable definido, sin forzar clasificación (sección 5).
- ✓ Tratamiento de martingalas aclarado — vinculación por operación lógica, no por intento
  (sección 6).
- ✓ Evidencia a generar identificada, sin implementarla todavía (sección 7).
- ⏳ Auditoría aprueba la especificación y resuelve la pregunta pendiente — antes de iniciar
  código.
