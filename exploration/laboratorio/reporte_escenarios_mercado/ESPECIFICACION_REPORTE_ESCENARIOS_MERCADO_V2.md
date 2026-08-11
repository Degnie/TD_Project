# Especificación del Reporte de Escenarios de Mercado V2

Estado: **especificación — Fase 1.5-B del Caso 1**. Documento de diseño, no implementación.
Sustituye a `ESPECIFICACION_REPORTE_ESCENARIOS_MERCADO_V1.md` como especificación vigente del
reporte (D-046, versionado: documento histórico no se edita, se reemplaza por una nueva versión
cuando el propósito cambia por descubrimientos experimentales). V1 permanece intacta como registro
histórico — fue el documento que identificó el problema de cruce operación×régimen y disparó la
Fase 1.5-A completa (Pasos 1-3, cerrados). No se modifica `MetricasPorEscenario.cs`,
`AsignadorOperacionRegimen.cs`, `InfoOperacionResuelta`, `ClasificadorRegimenV1.cs` ni
`AnalizadorOperacional.cs`.

---

## 0. Qué cambia respecto a V1

| Sección de V1 | Estado en V2 |
|---|---|
| §1 Objetivo | Vigente, sin cambios de fondo — reafirmado en sección 1 |
| §2 Problema no resuelto (cruce operación×régimen) | **Obsoleta — reemplazada**. V1 proponía `Fill.Timestamp` + `OperacionId` como mecanismo de cruce; Fase 1.5-A (`ESPECIFICACION_VINCULACION_OPERACION_REGIMEN_V1.md §1`) verificó que `Fill` no tiene `OperacionId` — esa vía nunca se implementó. El cruce real ya existe y está implementado: `InfoOperacionResuelta` (Paso 1) → `AsignadorOperacionRegimen` (Paso 2) → `MetricasPorEscenario` (Paso 3). Ver sección 2 de este documento. |
| §3 Métricas heredadas | Vigente en su intención (reutilizar Fase 1.2), materializada en el código real de `MetricasPorEscenario.cs` — ver sección 3 |
| §4 Definición matemática de escenarios | **Vigente, no se reabre** — reafirmado en sección 4 |
| §5 Tratamiento de zonas ambiguas | **Vigente**, extendido en sección 5 con la distinción "Sin régimen" (Paso 2, no existía cuando se escribió V1) |
| §6 Anti-retrospectiva | Vigente — reafirmado en sección 6 |
| §7 Sin ranking financiero, correlación≠causalidad | **Vigente, sin cambios** — reafirmado en sección 7 (D-037) |

---

## 1. Objetivo (heredado de V1 §1, sin cambios de fondo)

Transformar `MetricasPorEscenario` (Fase 1.5-A, Paso 3, ya implementado) en un reporte
comprensible para el usuario del laboratorio, presentando cómo se comportó una estrategia dentro de
cada régimen de mercado — sin ranking, sin causalidad, con las mismas restricciones ya vigentes
desde Fase 1.2/1.3/1.4.

```
Backtest → InfoOperacionResuelta → AsignadorOperacionRegimen → MetricasPorEscenario → [ESTE PASO: Reporte]
```

---

## 2. Fuente de datos (reemplaza V1 §2 — el cruce ya existe y está implementado)

El reporte consume directamente la salida ya calculada de `MetricasPorEscenario.Calcular()`
(`ReporteMetricasPorEscenario`, con dos campos: `PorRegimenEntrada` y `PorRegimenResolucion`, cada
uno una `VistaPorEscenario` con una lista de `FilaEscenario`). No hay ningún cálculo nuevo en esta
fase — D-015 (capas, sin recalcular) sigue vigente: el reporte solo formatea lo que
`MetricasPorEscenario` ya produjo.

Cada `FilaEscenario` trae: `Regimen` (nullable), `OperacionesCompletadas`, `Ganadas`, `Perdidas`,
`EficienciaOperacionalPct`, `GanoInicial`/`GanoM1`/`GanoM2`/`PerdioAgotando`,
`PctResueltasPorMartingala` — el mismo catálogo de Fase 1.2 (§4.1-4.3), agrupado por régimen en
vez de global.

---

## 3. Estructura del reporte

Cuatro bloques, en este orden:

```
Reporte de Escenarios de Mercado — {Estrategia} / {Timeframe}

1. Resumen general
   (Total de operaciones completadas de la corrida — mismo dato que PerfilMultiTf.OperacionesCompletadas,
   sin segmentar, punto de referencia para verificar la partición exhaustiva de las dos vistas)

2. Vista por régimen de entrada
   (Tabla: Régimen | Operaciones | Eficiencia operacional % | Muestra | Victoria inicial/M1/M2/Agotamiento)
   Pregunta que responde: "¿bajo qué contexto de mercado decidió actuar la estrategia?"

3. Vista por régimen de resolución
   (Misma estructura de tabla, agrupada por RegimenResolucion)
   Pregunta que responde: "¿en qué contexto de mercado terminó cada operación?"

4. Nota metodológica obligatoria
   (Correlación ≠ causalidad — D-037, texto fijo, ver sección 7)
```

**No hay una quinta sección de "conclusión" o "síntesis"** — a diferencia del formato de reporte de
Fase 1.2 (que sí tenía una sección de interpretación separada), aquí cualquier síntesis que compare
las dos vistas o proponga una lectura combinada cae directamente en el tipo de afirmación prohibida
por la sección 7 (comparar regímenes invita a leer causalidad). Se documenta como decisión explícita
de esta especificación, no como omisión.

Cada tabla (bloques 2 y 3) es exactamente una `VistaPorEscenario` ya calculada — mismo orden de
filas que produce `MetricasPorEscenario` (Alcista, Bajista, Lateral, Ambiguo, luego "Sin régimen"
al final, ya fijado por `OrderBy` en el código existente).

---

## 4. Definición matemática de escenarios (heredada de V1 §4, no reabierta)

Sin cambios — Alcista/Bajista/Lateral/Ambiguo son los que produce `ClasificadorRegimenV1.Clasificar()`
(Fase 1.4-B, congelado). Este documento no discute umbrales de ADX ni SesgoDI.

---

## 5. Tratamiento de Ambiguo y de "Sin régimen" (extiende V1 §5)

Dos categorías distintas, ambas presentadas como fila explícita en cada tabla — ninguna se oculta
ni se combina con la otra:

- **Ambiguo** (heredado de V1 §5, sin cambios): régimen *calculado* por `ClasificadorRegimenV1` —
  el clasificador sí evaluó esa vela y determinó que la evidencia direccional es insuficiente para
  Alcista/Bajista/Lateral. Se etiqueta en el reporte como "régimen sin evidencia direccional
  suficiente (Ambiguo)", nunca como una condición de mercado predecible.
- **Sin régimen** (nuevo en V2 — no existía cuando se escribió V1, es resultado del Paso 2 de Fase
  1.5-A): la operación no tiene fila de `VentanaClasificada` con coincidencia exacta de timestamp
  — típicamente porque su vela cae en la ventana de calentamiento del clasificador (primeras
  `2 × PeriodoAdx` velas). Se etiqueta como "régimen no disponible (fuera de la ventana evaluable
  del clasificador)". Es un dato faltante, no un estado de mercado — nunca se interpreta como
  "la estrategia funciona mejor cuando no hay régimen".

Ninguna interpretación del reporte puede decir "la estrategia funciona mejor en mercados ambiguos"
ni "en operaciones sin régimen" sin aclarar inmediatamente qué representa cada categoría —
consistente con la regla ya fijada en V1 §5.

---

## 6. Anti-selección retrospectiva (heredada de V1 §6, reafirmada)

Sin cambios de fondo. `ClasificadorRegimenV1` fue congelado (D-034) antes de que cualquier
operación de estrategia se cruzara con él — el orden ya se cumplió en Fase 1.4-A/B, y Fase 1.5-A
(Pasos 1-3) construyó el cruce sin tocar ni un parámetro del clasificador. Si los resultados de
este reporte generan la tentación de ajustar `UmbralSesgoDI` u otro parámetro, esa recalibración
requeriría `ClasificadorRegimenV2` (D-017), nunca una edición de V1.

---

## 7. Presentación — sin ranking, correlación ≠ causalidad (heredada de V1 §7, D-037)

Sin cambios de fondo respecto a V1. Reafirmado con ejemplos actualizados al formato real de las dos
vistas (sección 3):

**Interpretación permitida**:
```
Durante periodos clasificados como Alcistas en el régimen de resolución, se observaron 1,200
operaciones completadas (eficiencia operacional 88%, muestra=1200). En periodos Ambiguos, 300
operaciones (eficiencia operacional 85%, muestra=300).
```

**Interpretación prohibida**:
```
La estrategia funciona mejor en mercados alcistas.                    [PROHIBIDO — D-014/D-009]
Conviene usar esta estrategia solo en tendencia.                       [PROHIBIDO — D-037]
El régimen de entrada explica por qué ganó esta operación.             [PROHIBIDO — D-037]
```

**Nota metodológica obligatoria (D-037)** — texto fijo que debe aparecer en todo reporte generado
por esta especificación, visible, no en letra pequeña:

```
La clasificación de régimen describe una coincidencia temporal observada entre comportamiento de
mercado y resultados de estrategia. No demuestra que el régimen sea la causa del resultado. El
dataset corresponde a un único periodo histórico; los regímenes no están distribuidos de forma
experimentalmente controlada.
```

---

## 8. Verificación de integridad del reporte

Heredada de Fase 1.2/1.3/1.5-A, aplicada a cada una de las dos vistas por separado (consistente con
la prueba ya implementada `VerificarIgualdadSegmentadoVsTotal`, Paso 3):

```
Σ (OperacionesCompletadas de todas las filas de PorRegimenEntrada) == Resumen general (bloque 1)
Σ (OperacionesCompletadas de todas las filas de PorRegimenResolucion) == Resumen general (bloque 1)
```

Ambas igualdades deben cumplirse simultáneamente — ya garantizado por `MetricasPorEscenario`
(Paso 3, prueba ya pasando), este bloque solo señala que el reporte debe **mostrar** el resumen
general (bloque 1) junto a las tablas para que la igualdad sea verificable por el lector, no solo
por una prueba interna.

---

## Fuera de alcance (respetado)

No se implementa código en este documento. No se modifica `MetricasPorEscenario.cs`,
`AsignadorOperacionRegimen.cs`, `ClasificadorRegimenV1.cs` ni ningún tipo de Fase 1.5-A. No se
calcula ranking de escenarios, "mejor mercado", recomendación de inversión ni retorno financiero.
No se agrega una matriz de transición Entrada×Resolución (D-044, ya descartada). No se define una
sección de "conclusión"/síntesis combinada (sección 3, decisión explícita de esta versión).

---

## Criterio de cierre de Fase 1.5-B (diseño)

- ✓ Objetivo reafirmado sin cambios de fondo respecto a V1 (sección 1).
- ✓ Fuente de datos actualizada a la arquitectura real ya implementada — reemplaza la propuesta
  obsoleta de V1 §2 (sección 2).
- ✓ Estructura del reporte definida en 4 bloques, con la ausencia de una quinta sección de síntesis
  documentada como decisión explícita, no omisión (sección 3).
- ✓ Definición matemática de escenarios explícitamente no reabierta (sección 4).
- ✓ Tratamiento de Ambiguo (heredado) y "Sin régimen" (nuevo de Fase 1.5-A) diferenciados y ambos
  visibles (sección 5).
- ✓ Regla anti-retrospectiva reafirmada sin cambios (sección 6).
- ✓ Regla de presentación sin ranking, con nota obligatoria de correlación≠causalidad en texto fijo
  (sección 7, D-037).
- ✓ Verificación de integridad extendida a ambas vistas, visible en el reporte mismo (sección 8).
- ⏳ Auditoría aprueba la especificación — pendiente de confirmación explícita antes de iniciar
  código.
