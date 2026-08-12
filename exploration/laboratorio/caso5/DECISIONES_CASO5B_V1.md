# Decisiones — Caso 5B: Capa Comparativa de Gestores de Riesgo

Estado: **D-112 a D-115 resueltas**. Misma estructura usada en D-001 a D-111 (decisión, opciones,
criterio, evidencia, resolución). Ningún código se modifica en este documento — las resoluciones
aquí registradas habilitan la especificación de implementación siguiente, no la reemplazan.

Contexto completo en `PROPUESTA_CASO5B_V1.md`. Verificación contra código existente, no
reconstruida de memoria (mismo criterio que abrió Caso 2/Caso 3/Caso 4/Caso 5A, D-057).

---

## D-112 — Ubicación arquitectónica del comparador

**Estado**: 🟢 Aprobada. **Selección: B — nuevo componente comparador de laboratorio.**

**Decisión**: dónde vive la lógica que ejecuta N gestores sobre la misma estrategia/dataset y
agrega sus resultados — dentro de `EjecutorProtocolo` (Opción A), en un componente nuevo y
separado (Opción B), o como una capa que solo compara resultados ya persistidos, sin ejecutar nada
(Opción C).

### Opciones

- **A — Extender protocolo existente**: `EjecutorProtocolo`/`EntradaProtocolo` reciben una lista de
  gestores en vez de un único `ConfiguracionSizing?`, e iteran internamente.
  - Riesgo confirmado en la propuesta: `EjecutorProtocolo.Ejecutar` ya orquesta un eje de
    iteración (`Timeframes`, líneas 88-98 de `EjecutorProtocolo.cs`) — agregar un segundo eje
    (gestor) dentro del mismo método combina dos responsabilidades de orquestación distintas en un
    solo componente, sin que exista necesidad demostrada de que compartan implementación.
- **B — Nuevo componente comparador de laboratorio**: mismo patrón que `ComparadorMultiTimeframe`
  (`analisis_multitimeframe/PerfilMultiTimeframe.cs:40`) — invoca `EjecutorProtocolo.Ejecutar` una
  vez por gestor desde afuera, agrega los resultados en una estructura nueva.
  - Ventaja: no toca `EjecutorProtocolo` ni `EntradaProtocolo` — mismo nivel de aislamiento que ya
    tiene el precedente respecto a `BacktestRunner` (D-008/D-009). Reutiliza un patrón ya validado
    en el proyecto en vez de introducir uno nuevo.
- **C — Capa posterior sobre resultados persistidos**: no ejecuta nada, recibe N
  `ResultadoProtocolo` ya generados por quien sea y solo los compara.
  - Riesgo confirmado en la propuesta: no garantiza por construcción que las N corridas comparadas
    compartan estrategia/dataset/timeframe/configuración económica salvo el gestor — ese control
    experimental (D-113) quedaría fuera del componente, dependiente de disciplina externa no
    verificable por el propio comparador.

### Resolución adoptada

**Selección: B.** Mismo patrón que `ComparadorMultiTimeframe`: un componente `ComparadorGestores`
que invoca `EjecutorProtocolo.Ejecutar` una vez por gestor (recibiendo la lista de gestores como
parámetro, no como cambio de contrato de `EntradaProtocolo`) y agrega los resultados en una
estructura nueva (D-114). No requiere tocar `EjecutorProtocolo`, `EntradaProtocolo`, ni ningún
archivo de `src/` — la única superficie nueva vive en el laboratorio, como el precedente.

**Por qué no A**: mezclaría dos ejes de orquestación (timeframe, ya existente; gestor, nuevo) en el
mismo método, aumentando la complejidad interna de `EjecutorProtocolo.Ejecutar` sin necesidad — el
comparador puede envolver el protocolo sin modificarlo.

**Por qué no C**: el control experimental (D-113) debe verificarse por construcción, no asumirse —
un comparador que solo agrega resultados ya generados no puede garantizar que la única variable que
cambió entre ellos sea el gestor.

### Evidencia

- `ComparadorMultiTimeframe.Comparar` (`analisis_multitimeframe/PerfilMultiTimeframe.cs:44-78`):
  precedente arquitectónico directo — mismo problema estructural (comparar N resultados que
  difieren en un solo eje), misma capa (laboratorio, no `src/`).
- `EjecutorProtocolo.Ejecutar` (`protocolo/EjecutorProtocolo.cs:82-111`): confirma que ya orquesta
  un eje de iteración (timeframes) internamente — evidencia del riesgo de mezclar responsabilidades
  bajo la Opción A.

---

## D-113 — Unidad de comparación

**Estado**: 🟢 Aprobada.

**Decisión**: qué significa, exactamente, "una comparación" — qué se mantiene fijo y qué varía
entre las N corridas que `ComparadorGestores` agrega.

### Resolución adoptada

Una comparación es: **una estrategia + un dataset + un timeframe + una configuración económica
(instrumento, costes) fijos, variando exclusivamente el gestor de riesgo activo** — mismo criterio
ya fijado en `DECISIONES_CASO5_V1.md` ("Criterio adicional de Caso 5A — control experimental"),
ahora aplicado como precondición verificable de este componente, no solo como regla declarada.

`ComparadorGestores.Comparar` recibe: una `EntradaProtocolo` base (sin `Sizing`, o con
`Sizing=null`) y una lista de `IGestorRiesgo` — construye internamente N variantes de esa misma
`EntradaProtocolo`, cada una con `Sizing = new ConfiguracionSizing(gestor)`, y las ejecuta. Esto
**garantiza por construcción** que estrategia/dataset/timeframe/instrumento/costes son idénticos
entre las N corridas — no depende de que el llamador arme correctamente N `EntradaProtocolo`
separadas.

**Múltiples estrategias, múltiples timeframes, múltiples datasets**: fuera de alcance de esta
decisión — Caso 5B compara gestores sobre una unidad fija (una estrategia, un timeframe, un
dataset). Extender la comparación a más de un eje simultáneamente (ej. "estrategia × gestor",
"timeframe × gestor") es una ampliación de alcance futura, no resuelta aquí — evita repetir el
riesgo ya señalado en Caso 3B/D-100 de diseñar para N dimensiones sin necesidad demostrada.

### Restricciones que aplican

- `ComparadorGestores` no acepta una lista de `EntradaProtocolo` ya armadas por separado — solo una
  base + una lista de gestores, precisamente para que el control experimental sea estructural, no
  convencional.
- Ningún gestor de la lista puede ser `null` — la comparación siempre es entre gestores activos
  (el caso `Sizing=null`/sin gestor no participa de una comparación, es la ausencia de sizing).

### Evidencia

- `DECISIONES_CASO5_V1.md`, sección "Criterio adicional de Caso 5A — control experimental": origen
  textual de la regla, ahora convertida en precondición de diseño.
- `EntradaProtocolo` (`protocolo/EjecutorProtocolo.cs:57-69`): confirma que todos los campos
  necesarios para fijar el resto de la unidad (estrategia, dataset, timeframe, instrumento, costes)
  ya son parámetros del mismo record — no requiere ningún campo nuevo para expresar "todo igual
  salvo el gestor".

---

## D-114 — Estructura de resultado comparativo y fuente de datos

**Estado**: 🟢 Aprobada.

**Decisión**: forma concreta del tipo que acumula N resultados, y qué fuente de datos usa cada
fila de la comparación.

### Resolución adoptada

**Fuente de datos: `MetricasFinancieras` exclusivamente** — `ResultadoCorridaTimeframe.
MetricasFinancieras` (`protocolo/EjecutorProtocolo.cs:38`), ya poblado directamente por
`EjecutorProtocolo` en la rama `Success`, sin pasar por `AnalizadorOperacional`/`ReporteOperacional`
en ningún punto. **`ReporteOperacional` queda explícitamente excluido como fuente de esta
comparación** — su dependencia de `ResolucionDeIntentos`/martingala (D-055) no representa a las 4
de 6 estrategias congeladas que no usan martingala; usarlo introduciría un sesgo de cobertura que
`MetricasFinancieras` no tiene.

**Estructura del resultado comparativo** (nueva, ubicación a fijar en la especificación de
implementación — candidata: `exploration/laboratorio/caso5b/` o análogo):

```
FilaComparacionGestor
{
    IdentidadGestor: string       // IIdentidadGestorRiesgo.ObtenerIdentidadConfiguracion()
    Metricas: MetricasFinancieras // fuente unica, sin recalcular (D-072/D-077)
    Estado: EstadoCorridaTimeframe // Success/Failed/Incomplete — una corrida individual puede fallar sin invalidar las demas
}

ResultadoComparativoGestores
{
    Estrategia: string
    Timeframe: string
    Dataset: string
    Filas: IReadOnlyList<FilaComparacionGestor>  // orden de entrada = orden de presentacion, D-112
}
```

**Por qué no `ResultadoBacktest` directo**: expone `Fills`/`Trades`/`EquityCurve` completos — más
detalle del necesario para una comparación de métricas agregadas, y ya tiene una capa de resumen
(`MetricasFinancieras`) construida exactamente para este propósito (D-072). Usar `ResultadoBacktest`
crudo obligaría a recalcular lo que esa capa ya calcula, violando D-077.

**Por qué no un DTO combinado nuevo con ambas fuentes**: mezclar `MetricasFinancieras` con algo de
`ReporteOperacional` reintroduce el sesgo de martingala que esta decisión excluye explícitamente —
si una fase futura necesita comparar también datos operacionales, debe ser una extensión explícita
y evaluada aparte, no una mezcla por defecto.

**Orden de entrada = orden de presentación, sin reordenar por valor** (heredado del precedente,
D-112): `Filas` conserva el orden de la lista de gestores recibida por `ComparadorGestores.Comparar`
— ninguna fila se reordena por magnitud de ninguna métrica, mismo principio que
`ComparadorMultiTimeframe` ya aplica y que evita un ranking implícito.

### Restricciones que aplican

- Ninguna métrica se recalcula dentro de `ComparadorGestores` — todo campo de `FilaComparacionGestor.
  Metricas` proviene sin modificación de `ResultadoCorridaTimeframe.MetricasFinancieras` (D-072/
  D-077, sin excepción).
- Una corrida individual en estado `Failed`/`Incomplete` no aborta la comparación completa — se
  refleja como tal en su propia fila (mismo principio ya usado por `EjecutorProtocolo` para
  timeframes, `ESPECIFICACION_PIPELINE_EXPERIMENTAL_V1.md` §6: "un fallo en un timeframe no detiene
  la evaluación de los demás").

### Evidencia

- `ResultadoCorridaTimeframe` (`protocolo/EjecutorProtocolo.cs:32-39`): confirma que
  `MetricasFinancieras` ya es un campo poblado directamente, opcional solo en la rama no-Success.
- `MetricasFinancieras.cs`: confirma los campos disponibles sin cálculo adicional (D-111 ya
  extendió esta fuente con `ProfitFactor`/`CapitalLibreMinimo`).
- `PerfilMultiTimeframe`/`ComparadorMultiTimeframe`: precedente de "orden de entrada = orden de
  presentación", reutilizado sin reabrir la pregunta.

---

## D-115 — Formato de salida

**Estado**: 🟢 Aprobada.

**Decisión**: cómo se presenta `ResultadoComparativoGestores` — tabla en texto, estructura de datos
consumible por otro componente, o ambas.

### Resolución adoptada

**Ambas, con la estructura de datos como fuente única de verdad**: `ResultadoComparativoGestores`
(D-114) es el objeto que efectivamente compara — cualquier presentación en texto es una
transformación de ese objeto, nunca una fuente independiente de datos (mismo principio D-072/D-077
aplicado a la capa de presentación: separación cálculo/reporte que el proyecto ya aplica en todas
sus fases, ej. `CalculadoraMetricasFinancieras`/`ReporteFinancieroGenerador`).

**Salida en texto**: una tabla simple, alineando gestores en columnas y métricas en filas (o
viceversa) — mismo estilo que `ReporteFinancieroGenerador` ya usa para presentar
`MetricasFinancieras` de una corrida individual, extendido a N corridas. No se diseña el formato
exacto en esta decisión — corresponde a la especificación de implementación.

**Sin ninguna forma de "mejor gestor" ni de puntuación agregada**: a diferencia de
`MejorResultadoObservado` en `ComparadorMultiTimeframe` (que sí reporta una observación puntual
sobre un eje único, Eficiencia Operacional), **Caso 5B no incluye ningún campo equivalente en esta
fase** — ni siquiera como "observación puntual, no recomendación". Motivo: a diferencia de
timeframe (donde "mejor" es ambiguo pero inofensivo, D-011 ya lo dejó como eje fijo pendiente),
declarar cualquier gestor como destacado en Caso 5B —aunque se etiquete como no vinculante— es
exactamente el primer paso hacia el recomendador que esta fase excluye explícitamente (§1 de
`PROPUESTA_CASO5B_V1.md`). La salida se limita a presentar métricas lado a lado, sin ningún campo
que ya prefigure una preferencia.

**Sin pesos de métricas, sin ranking, sin puntuación combinada**: ninguna métrica se combina con
otra en un único indicador — mismo criterio D-014/D-025/D-026/D-047/D-076 ya aplicado en toda fase
anterior a comparaciones de timeframe/régimen.

### Restricciones que aplican

- Ninguna función de agregación entre métricas de distintos gestores (suma, promedio ponderado,
  score compuesto) se introduce en esta fase.
- La tabla de texto es estrictamente derivada — un cambio en su formato nunca requiere cambiar
  `ResultadoComparativoGestores`, y viceversa un cambio en las métricas disponibles se refleja
  automáticamente sin lógica de presentación adicional.

### Evidencia

- `ReporteFinancieroGenerador.Generar` (`modelo_financiero/ReporteFinancieroGenerador.cs:14`):
  precedente de separación cálculo/presentación ya establecido, extendido aquí a comparación.
- `PerfilMultiTimeframe.MejorResultadoObservado`: precedente citado y explícitamente **no**
  replicado — diferenciado por escrito para que quede claro que la omisión es deliberada, no un
  olvido.

---

## Fuera de alcance de este documento

No se implementó código. No se modifica ningún módulo de `src/`. D-112 a D-115 quedan resueltas a
nivel de diseño — la especificación de implementación siguiente traduce cada resolución a
estructura de código concreta (nombres de tipos, firmas, ubicación exacta de archivos), sin reabrir
ninguna de las decisiones aquí fijadas.

---

## Próximo documento

`ESPECIFICACION_IMPLEMENTACION_CASO5B_V1.md`, traduciendo D-112 (`ComparadorGestores` como
componente nuevo de laboratorio), D-113 (firma exacta que garantiza el control experimental),
D-114 (`FilaComparacionGestor`/`ResultadoComparativoGestores`, ubicación de archivo), y D-115
(formato de tabla de texto) a diseño de código, previo a cualquier implementación.
