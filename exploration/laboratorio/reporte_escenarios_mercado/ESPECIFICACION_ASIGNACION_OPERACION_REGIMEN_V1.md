# Especificación de Asignación Operación → Régimen V1

Estado: **especificación — Fase 1.5-A, Paso 2 del Caso 1**. Documento de diseño, no
implementación. No se calculan métricas todavía (D-039: primero contexto, después métricas) — el
resultado de este paso es una estructura intermedia `Operacion → (RegimenEntrada, RegimenResolucion)`,
consumida recién en el Paso 3. No se modifica `InfoOperacionResuelta` (ya extendido en Paso 1,
cerrado), `ClasificadorRegimenV1.cs` (congelado, D-017), `IStrategy`, `BacktestRunner` ni ningún
contrato de `src/`.

---

## 1. Verificación previa — qué representa realmente `VentanaClasificada`

Antes de diseñar la búsqueda, se verificó cómo `ClasificadorRegimenV1.Clasificar()` construye su
salida, porque cambia la naturaleza del problema:

```csharp
resultado.Add(new VentanaClasificada(muestra.InicioUtcMs, muestra.InicioUtcMs, escenario));
```

(`ClasificadorRegimenV1.cs`, dentro de `Clasificar`). **`InicioUtcMs` y `FinUtcMsExclusivo` son el
mismo valor** — pese a que el tipo `VentanaClasificada` (heredado de Fase 1.4-A,
`Escenario.cs`) fue diseñado originalmente para representar un rango, `ClasificadorRegimenV1`
produce una clasificación **por vela individual**, no por rango de varias velas. Cada
`VentanaClasificada.InicioUtcMs` es exactamente el `InicioUtcMs` de una vela del dataset (el mismo
campo que `CalibradorUmbralSesgoDI.CalcularSerie` toma de `velas[i].InicioUtcMs`, verificado en
`CalibradorUmbralSesgoDI.cs`).

Además, se verificó que **la fuente de timestamps es la misma en ambos lados**: `Candle.Timestamp`
(consumido por las estrategias vía `dataSlice.VelaActual.Timestamp`, capturado en Paso 1) se
construye en `LectorDerivado.FiltrarParaBacktest` como `new Candle(v.InicioUtcMs, ...)` — es decir,
`Candle.Timestamp == VelaDerivadaCruda.InicioUtcMs`, el mismo campo que usa
`ClasificadorRegimenV1`. No hay dos sistemas de tiempo a reconciliar — es el mismo entero, del
mismo dataset, en ambos lados.

**Consecuencia directa para el diseño**: la asignación no requiere "buscar en qué rango cae un
timestamp" — es una **búsqueda por coincidencia exacta de entero** (`TimestampEntrada`/
`TimestampResolucion` de la operación contra `VentanaClasificada.InicioUtcMs` del clasificador).
Esto es más simple y más estricto que lo que planteaba la especificación de Fase 1.5-A original
(que hablaba de "rango de la ventana") — se corrige aquí con el dato verificado.

---

## 2. Regla de asignación

Dado `TimestampEntrada` y `TimestampResolucion` de una `InfoOperacionResuelta` (Paso 1), y la
salida `IReadOnlyList<VentanaClasificada>` de `ClasificadorRegimenV1.Clasificar(velas)` sobre el
**mismo dataset y mismo timeframe** de la corrida:

```
RegimenEntrada     = clasificacion.Where(v => v.InicioUtcMs == op.TimestampEntrada).Escenario
RegimenResolucion  = clasificacion.Where(v => v.InicioUtcMs == op.TimestampResolucion).Escenario
```

Sin tolerancia, sin vecino más cercano, sin interpolación (D-036: no se permite aproximación). Si
no hay coincidencia exacta, no hay régimen asignable — tratamiento en sección 4.

**Restricción operativa que esto impone**: el clasificador debe ejecutarse sobre exactamente el
mismo conjunto de velas (`velas` filtradas por `FiltrarParaBacktest`, mismo timeframe) que produjo
la corrida de la estrategia — no sobre un dataset recortado o de otro timeframe. Esto ya es
consistente con cómo se estructura cada corrida hoy (`CorrerUna` en
`evaluacion_multi_tf/Program.cs`, una estrategia × un timeframe × un conjunto de velas).

---

## 3. Estructura de salida (sin métricas, D-039)

Tipo intermedio propuesto (nombre tentativo, no se implementa aquí):

```
OperacionConRegimen(
    OperacionId: int,
    Gano: bool,
    MartingalasUsadas: int,
    TimestampEntrada: long,
    TimestampResolucion: long,
    RegimenEntrada: Escenario?,      // null = sin régimen asignable (sección 4)
    RegimenResolucion: Escenario?)   // null = sin régimen asignable (sección 4)
```

`Escenario?` (nullable) en vez de forzar un valor — preserva la distinción entre "esta operación
cayó en régimen Ambiguo" (un régimen calculado, válido) y "esta operación no tiene régimen
asignable" (sección 4, una categoría completamente distinta, ya señalada por D-041/sección 5 de la
especificación de Fase 1.5). Este tipo solo copia campos ya existentes de `InfoOperacionResuelta` y
agrega el resultado de la búsqueda de la sección 2 — no recalcula nada (D-015).

---

## 4. Qué ocurre si falta una vela exacta

Dos causas ya identificadas, verificadas contra el código real, con tratamiento distinto:

1. **Ventana de calentamiento del clasificador** (`CLASIFICADOR_REGIMEN_V1.md`, "Tratamiento de
   bordes": primeras `2 × PeriodoAdx` velas sin clasificación). Si `TimestampEntrada` o
   `TimestampResolucion` cae ahí, no existe ninguna `VentanaClasificada` con ese `InicioUtcMs` —
   la búsqueda de la sección 2 no encuentra coincidencia. Se asigna `null` (Régimen no disponible
   por calentamiento), nunca se fuerza a Lateral ni a ningún otro estado.
2. **Vela excluida por ser parcial** (`FiltrarParaBacktest` descarta velas con `EsParcial=true`
   antes de construir el `Candle` que ve la estrategia). Si por alguna razón el clasificador
   corriera sobre un conjunto de velas distinto al que vio la estrategia (violación de la
   restricción operativa de la sección 2), también resultaría en ausencia de coincidencia — este
   caso no debería ocurrir si se respeta la sección 2, se señala aquí solo como verificación de
   integridad, no como caso esperado.

En ambos casos el tratamiento es el mismo: `RegimenEntrada`/`RegimenResolucion` quedan `null`,
etiquetados en el reporte (Paso 3/4) como "Sin régimen (dato no disponible)" — categoría distinta
de "Ambiguo", que sí es una clasificación calculada. No se descarta la operación de la partición
exhaustiva: se cuenta, pero en su propia categoría, igual que ya se definió para "Ambiguo" en
`ESPECIFICACION_REPORTE_ESCENARIOS_MERCADO_V1.md §5`.

---

## 5. Qué ocurre con operaciones incompletas

`OperacionAbiertaAlCierre` (campo de `PerfilMultiTf`, ya existente desde Fase 1.2) identifica la
operación que quedó abierta al final del dataset — por definición, esa operación **nunca invoca
`_onOperacionResuelta`** (verificado en Paso 1: el callback solo se dispara al cerrar, líneas 67/56
de las estrategias). No genera ninguna `InfoOperacionResuelta`, por lo tanto no participa de este
paso de asignación en absoluto — no tiene `TimestampEntrada`/`TimestampResolucion` que buscar. Se
mantiene fuera de esta estructura intermedia, exactamente como ya está fuera de
`OperacionesCompletadas` — ninguna definición nueva requerida aquí.

---

## 6. Qué ocurre si el clasificador no tiene estado disponible

Distinto del caso de la sección 4 (vela dentro del dataset pero sin clasificación por
calentamiento): esta pregunta cubre qué pasa si `ClasificadorRegimenV1.Clasificar(velas)` no fue
ejecutado en absoluto para ese timeframe, o devolvió una lista vacía (por ejemplo, dataset con
menos de `2 × PeriodoAdx` velas — ver guarda en `CalibradorUmbralSesgoDI.CalcularSerie`,
`if (velas.Count <= periodo * 2) return Array.Empty<...>()`, reutilizada por
`ClasificadorRegimenV1`).

**Tratamiento**: si la lista de clasificación está vacía o no se proporcionó, el paso de asignación
no falla — todas las operaciones de esa corrida quedan con `RegimenEntrada = null` y
`RegimenResolucion = null` (mismo caso que la sección 4, incorporado sin necesidad de una regla
distinta: "no hay coincidencia" cubre tanto "esa vela específica no tiene clasificación" como
"ninguna vela de esta corrida tiene clasificación"). No se debe interpretar la ausencia de
clasificador como error de ejecución — es una corrida legítima para la que el análisis por régimen
simplemente no está disponible (por ejemplo, un timeframe con muy pocas velas, como ya se observó
en Fase 1.3 con 1D=61 operaciones).

---

## 7. Estructura de salida que consumirá el reporte (Paso 3)

El Paso 3 (métricas por escenario, `ESPECIFICACION_REPORTE_ESCENARIOS_MERCADO_V1.md §3`) recibirá
una lista de `OperacionConRegimen` (sección 3) por corrida (estrategia × timeframe) y construirá la
partición exhaustiva agrupando por `RegimenResolucion` (criterio principal de agrupación, en línea
con "el resultado financiero corresponde al momento de resolución" — decisión ya tomada
implícitamente al aprobar D-038 conservar ambos, pero **no fijada explícitamente todavía como
criterio de agrupación principal del reporte**: se deja señalada aquí como pregunta abierta para el
Paso 3, no se resuelve en este documento, consistente con D-039 — este paso solo entrega contexto,
no decide cómo se resume).

Verificación de integridad requerida en el Paso 3 (ya anticipada en
`ESPECIFICACION_REPORTE_ESCENARIOS_MERCADO_V1.md §3`): la suma de operaciones por
`RegimenResolucion` (incluyendo la categoría "Sin régimen") debe igualar `OperacionesCompletadas`
de esa corrida.

---

## Fuera de alcance (respetado)

No se implementa código en este documento. No se modifica `InfoOperacionResuelta`,
`ClasificadorRegimenV1.cs`, `EstrategiaTresMosqueteros.cs`, `EstrategiaMhiMayoria.cs`,
`AnalizadorOperacional.cs` ni `PerfilMultiTimeframe.cs`. No se calculan métricas por régimen (Paso
3, posterior). No se decide si el reporte final usará `RegimenEntrada`, `RegimenResolucion` o
ambos como clasificación principal — solo se señala la pregunta para el Paso 3 (sección 7).

---

## Criterio de cierre de Fase 1.5-A, Paso 2 (diseño)

- ✓ Verificado contra el código real que `VentanaClasificada` es, en la práctica, una clasificación
  por vela individual (`InicioUtcMs == FinUtcMsExclusivo`), y que los timestamps de estrategia y
  clasificador provienen del mismo campo (`Candle.Timestamp == VelaDerivadaCruda.InicioUtcMs`) —
  sección 1, corrige la especificación de Fase 1.5-A original (que hablaba de "rango").
- ✓ Regla de asignación definida como coincidencia exacta de entero, sin aproximación (sección 2,
  D-036).
- ✓ Estructura de salida definida sin calcular métricas (sección 3, D-039), con `Escenario?`
  nullable para distinguir "Ambiguo" de "sin régimen".
- ✓ Caso de vela faltante (calentamiento) definido, sin forzar clasificación (sección 4).
- ✓ Caso de operación incompleta definido — queda fuera de este paso, sin necesidad de regla nueva
  (sección 5).
- ✓ Caso de clasificador no disponible/vacío definido — mismo tratamiento que vela faltante
  (sección 6).
- ✓ Estructura de salida hacia el Paso 3 identificada, con una pregunta explícitamente dejada
  abierta para ese paso (criterio de agrupación principal: entrada, resolución o ambos) — sección
  7, no se resuelve aquí por D-039.
- ⏳ Auditoría aprueba la especificación — pendiente de confirmación explícita antes de iniciar
  código.
