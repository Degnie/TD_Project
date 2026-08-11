# Especificación del Reporte de Escenarios de Mercado V1

Estado: **especificación — Fase 1.5 del Caso 1**. Documento de diseño, no implementación. No se
modifica `BacktestRunner`, `IStrategy`, DTOs, estrategias, `AnalizadorOperacional.cs` (Fase 1.2),
`PerfilMultiTimeframe.cs` (Fase 1.3) ni `ClasificadorRegimenV1.cs` (Fase 1.4-B, congelado — D-017:
ninguna fase posterior lo edita, solo lo consume).

---

## 1. Objetivo

Consumir `ClasificadorRegimenV1` (ya congelado) para presentar cómo se comportó una estrategia
dentro de cada régimen de mercado (Alcista, Bajista, Lateral, Ambiguo), reutilizando el catálogo de
métricas ya definido en Fase 1.2 — mismo patrón arquitectónico que Fase 1.3 (`PerfilMultiTimeframe`,
que agrupa por timeframe), aplicado aquí a un criterio de agrupación distinto: régimen de mercado.

```
Backtest → PerfilMultiTf → AnalizadorOperacional → [NUEVO: agrupación por régimen] → Reporte
```

D-015 (analizadores compuestos por capas) sigue vigente: este reporte no recalcula ninguna métrica
operacional, solo agrupa operaciones ya resueltas según en qué régimen cayeron.

---

## 2. El problema no resuelto todavía: cruzar operación × régimen

Ni Fase 1.4-A ni Fase 1.4-B necesitaron resolver esto — el clasificador produce una serie de
`VentanaClasificada` (una por vela/ventana, con `InicioUtcMs`), y las operaciones de estrategia
(`InfoOperacionResuelta`) no tienen timestamp propio en su registro actual — solo se conoce su
`OperacionId` y en qué backtest (estrategia × timeframe) ocurrieron, no en qué vela específica se
resolvió cada una.

**Esto es un hallazgo de esta fase, no un defecto de fases previas**: Fase 1.2/1.3 nunca necesitaron
cruzar una operación contra un timestamp externo porque solo agregaban sobre el conjunto completo de
operaciones de una corrida. Fase 1.5 sí lo necesita, y el mecanismo para obtenerlo no existe.

**Cómo resolverlo sin tocar contratos existentes** (`IStrategy`, `InfoOperacionResuelta` no se
modifican — restricción heredada de todas las fases anteriores): el motor ya expone
`ResultadoBacktest.Trades`/`Fills`, que sí tienen `Timestamp` (campo `VelaTimestamp` en
`FillLogEntryDto`/`Fill`, ver `src/Domain/Shared/Fill.cs` y equivalentes). El vínculo
operación-régimen se construye **fuera del motor**, en la capa de análisis: cada `Fill` de cierre de
una operación completada tiene un timestamp; ese timestamp se busca en la salida de
`ClasificadorRegimenV1.Clasificar()` para determinar en qué régimen cayó. Esto requiere:

1. Que la corrida de evaluación (equivalente a `evaluacion_multi_tf/Program.cs`) conserve, además de
   `InfoOperacionResuelta`, el `Fill` de cierre correspondiente a cada operación — dato que el motor
   ya produce (`ResultadoBacktest.Fills`) pero que el código de evaluación actual no correlaciona
   explícitamente con `InfoOperacionResuelta` por `OperacionId`.
2. Una función de búsqueda `timestamp → régimen` sobre la salida ya congelada del clasificador — no
   requiere modificar `ClasificadorRegimenV1`, solo indexar su salida por rango de tiempo.

**Esto no se implementa en este documento** — se identifica como el primer bloque de trabajo del
Paso 1 de implementación (fuera de esta especificación), y se señala explícitamente porque cambia el
alcance de "solo agrupar" a "primero construir el cruce, luego agrupar".

---

## 3. Métricas heredadas — mismo catálogo, nuevo criterio de agrupación

Reutiliza exactamente `ReporteOperacional` (Fase 1.2) y el patrón de `PerfilMultiTimeframe` (Fase
1.3), sustituyendo "por timeframe" por "por régimen":

| Heredado de | Qué se reutiliza |
|---|---|
| `AnalizadorOperacional.Analizar()` | Eficiencia operacional, resolución de intentos, peores escenarios — sin cambios en las fórmulas |
| `ComparadorMultiTimeframe` (patrón) | Estructura de agrupación en filas + consistencia (mín/máx/amplitud) + separación mejor-resultado/mayor-evidencia (D-014) |
| D-010 (Fase 1.3) | Tamaño de muestra obligatorio junto a cualquier métrica — aquí, cantidad de operaciones por régimen |

**Diferencia estructural importante frente a Fase 1.3**: en Fase 1.3, cada fila (timeframe) provenía
de una corrida de backtest completa e independiente. En Fase 1.5, las 4 filas (regímenes) provienen
de **subconjuntos de una misma corrida** — la partición de las operaciones de una única evaluación
estrategia×timeframe según el régimen de su Fill de cierre. Esto significa que la suma de
operaciones en las 4 filas de régimen debe igualar el total de operaciones completadas de esa
corrida (partición exhaustiva) — una verificación de integridad nueva que Fase 1.3 no necesitaba.

---

## 4. Definición matemática de cada escenario (heredada, no redefinida)

No se redefine ningún criterio de clasificación — Alcista/Bajista/Lateral/Ambiguo son exactamente
los que produce `ClasificadorRegimenV1.Clasificar()`, con sus parámetros ya congelados (D-034). Este
documento no vuelve a discutir umbrales de ADX ni SesgoDI — esa discusión está cerrada.

---

## 5. Cómo tratar zonas ambiguas (en el reporte de estrategia, no en el clasificador)

El clasificador ya resuelve "qué es Ambiguo" (Fase 1.4-B). Lo que esta fase debe definir es **cómo
se presenta** una operación que cae en un tramo Ambiguo:

- Se reporta como una fila más, con el mismo catálogo de métricas que Alcista/Bajista/Lateral — no
  se excluye ni se oculta.
- Se etiqueta explícitamente en el reporte como "régimen sin evidencia direccional suficiente
  (Ambiguo)", nunca simplemente como una cuarta categoría de mercado sin explicación — reutiliza la
  distinción conceptual ya fijada en `DEFINICION_ESTADOS_REGIMEN_V1.md §2`.
- Ninguna interpretación del reporte puede decir "la estrategia funciona mejor en mercados
  ambiguos" sin aclarar inmediatamente que "ambiguo" no es una condición de mercado con
  características predecibles — es la admisión de que el instrumento no tuvo evidencia suficiente
  para clasificar esa ventana con confianza.

---

## 6. Cómo evitar selección retrospectiva (extiende D-016/sección 2 de Fase 1.4)

**Restricción central, ya establecida y no reinterpretable**: el clasificador fue congelado (D-034)
**antes** de que este documento se escribiera y sin conocer ningún resultado de estrategia. El orden
ya se cumplió:

```
Dataset → Clasificador (Fase 1.4-A/B, cerrado) → Segmentación (congelada) → Evaluación de
estrategias (esta fase, primera vez que ambos se cruzan)
```

Esta fase es, por diseño, el primer punto del laboratorio donde clasificador y estrategia se
encuentran — y por eso mismo es el punto de mayor riesgo de que alguien, en una fase futura, quiera
"ajustar `UmbralSesgoDI`" al ver que una estrategia se comporta de cierta forma en Ambiguo. **Esta
fase no reabre esa puerta**: si los resultados de este reporte generan la tentación de recalibrar el
clasificador, esa recalibración requeriría una nueva versión (`ClasificadorRegimenV2`, D-017),
nunca un ajuste de `ClasificadorRegimenV1` motivado por el comportamiento de una estrategia
específica.

---

## 7. Cómo presentar resultados sin ranking financiero (extiende D-014/D-009)

Misma regla ya aplicada en Fase 1.3 (sección 5 de esa especificación) y en Fase 1.4
(`ESPECIFICACION_ANALISIS_ESCENARIOS_MERCADO_V1.md §8`), aplicada aquí a régimen en vez de
timeframe:

Interpretación permitida:
```
La estrategia completó 1,200 operaciones en tramos clasificados como Alcistas (eficiencia
operacional 88%, muestra=1200) y 300 en tramos Ambiguos (eficiencia operacional 85%, muestra=300).
```

Interpretación prohibida:
```
La estrategia funciona mejor en mercados alcistas.        [PROHIBIDO — no declara ganador, D-014]
Conviene usar esta estrategia solo en tendencia.           [PROHIBIDO — afirma causalidad, §8]
```

**Regla nueva de esta fase — separar dato observado de causalidad**: a diferencia de Fase 1.3 (donde
comparar timeframes de una misma estrategia no sugiere causalidad de mercado), comparar Alcista vs.
Bajista vs. Lateral vs. Ambiguo invita naturalmente a leer "el régimen *causa* el resultado". El
reporte debe declarar explícitamente, en una nota visible (no en letra pequeña), que la correlación
entre régimen y eficiencia operacional no implica que el régimen sea la causa — el dataset tiene un
único periodo histórico, y los regímenes no están distribuidos de forma experimentalmente
controlada (no hay dos mercados idénticos, uno alcista y otro bajista, para aislar el efecto).

---

## Fuera de alcance (respetado)

No se implementa código en esta fase. No se modifica `ClasificadorRegimenV1.cs` (congelado, D-017).
No se modifica `AnalizadorOperacional.cs` ni `PerfilMultiTimeframe.cs`. No se resuelve el mecanismo
de cruce operación×régimen (sección 2) — se identifica como bloque de trabajo del Paso 1 de
implementación, no se construye aquí. No se calcula ranking financiero ni se afirma causalidad entre
régimen y resultado de estrategia.

---

## Criterio de cierre de Fase 1.5 (diseño)

- ✓ Objetivo definido: consumir `ClasificadorRegimenV1` para presentar comportamiento de estrategia
  por régimen, reutilizando el catálogo de Fase 1.2 (sección 1).
- ✓ Identificado y explicado el problema no resuelto del cruce operación×régimen, con una vía de
  solución que no requiere tocar contratos existentes (sección 2) — nuevo hallazgo de esta fase, no
  heredado.
- ✓ Métricas heredadas identificadas, sin recalcular fórmulas ya congeladas (sección 3), con la
  verificación de integridad nueva (partición exhaustiva de operaciones por régimen).
- ✓ Definición matemática de escenarios explícitamente no reabierta (sección 4).
- ✓ Tratamiento de Ambiguo en el reporte definido, distinto de su definición en el clasificador
  (sección 5).
- ✓ Regla explícita contra selección retrospectiva, con el riesgo concreto identificado (primera
  vez que clasificador y estrategia se cruzan) — sección 6.
- ✓ Regla de presentación sin ranking financiero, con una regla nueva específica de esta fase
  (separar correlación de causalidad, sección 7).
- ⏳ Auditoría aprueba la especificación — pendiente de confirmación explícita antes de iniciar
  código.
