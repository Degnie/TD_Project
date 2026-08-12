# Versión Experimental — Caso 5B: Capa Comparativa de Gestores de Riesgo

Estado: **documento de congelamiento oficial — cierre de Caso 5B** (autorizado tras aprobación de
`AUDITORIA_CASO5B_V1.md`). A partir de este documento, el Caso 5B queda congelado como
**V1 Experimental**, dependiente de `caso5a-v1-experimental` — consume exclusivamente su
infraestructura (`IGestorRiesgo`, `IIdentidadGestorRiesgo`, gestores concretos, identidad
económica), sin modificar ninguno de sus archivos. A diferencia de Caso 5A, **Caso 5B no toca
`src/`** — toda su implementación vive en `exploration/laboratorio/caso5/`. Mismo patrón que
`VERSION_EXPERIMENTAL_CASO5A_V1.md`/`caso3/VERSION_EXPERIMENTAL_CASO3B_V1.md`.

---

## Identificación

- **Nombre**: Caso 5B — Capa comparativa de gestores de riesgo
- **Versión**: V1 Experimental
- **Estado**: Congelado
- **Fecha de congelamiento**: 2026-08-12
- **Base de aprobación**: `AUDITORIA_CASO5B_V1.md`, aprobada por auditoría.

---

## Componentes incluidos

**`ComparadorGestores`** (D-112, `caso5/ComparadorGestores.cs`): componente nuevo de laboratorio,
mismo patrón conceptual que `ComparadorMultiTimeframe` (sin dependencia de código entre ambos).
Único método público: `Comparar(EntradaProtocolo entradaBase, IReadOnlyList<IGestorRiesgo>
gestores)`.

**Control experimental por construcción** (D-113): `Comparar` construye internamente N variantes
de `entradaBase` (`entradaBase with { Sizing = new ConfiguracionSizing(gestor) }`) — garantiza que
estrategia/dataset/timeframe/instrumento/costes son idénticos entre las N corridas, sin depender de
que el llamador arme correctamente N `EntradaProtocolo` separadas. Exige `entradaBase.Sizing ==
null` y `entradaBase.Timeframes.Count == 1` — falla explícitamente si no se cumple.

**`FilaComparacionGestor`/`ResultadoComparativoGestores`** (D-114): estructura de resultado —
`MetricasFinancieras` como única fuente de datos, `ReporteOperacional` explícitamente excluido por
su acoplamiento a martingala (D-055). `Filas` conserva el orden de entrada de la lista de gestores,
nunca reordenado por valor.

**`RenderizadorComparacionGestores`** (D-115): render de tabla derivado del objeto comparativo, sin
ningún campo de ranking/puntuación/recomendación.

**Pruebas**: `exploration/laboratorio/caso5/TestsComparadorGestores.cs` (8 pruebas, integradas al
módulo satélite existente de Caso 5, `Caso5.csproj`, sin `.csproj` nuevo).

---

## Decisiones congeladas

D-112 a D-115 (4 decisiones), registradas en `DECISIONES_CASO5B_V1.md`. Ninguna reasignada a
contenido distinto del originalmente registrado. Todas 🟢 Aprobadas e implementadas — ninguna queda
como deuda técnica bloqueante dentro del alcance de Caso 5B.

---

## Garantías

- **Comparación reproducible, verificada por mecanismo**: P3 confirmó que `HashCompuesto` es
  idéntico entre gestores distintos sobre la misma estrategia/dataset, mientras que
  `HashConfiguracionEconomica` difiere — no por inspección de código, sino ejecutando el propio
  mecanismo de identidad ya congelado en Caso 5A.
- **Control experimental garantizado por construcción, no por convención**: `entradaBase with {
  Sizing = ... }` es la única forma en que `Comparar` obtiene una variante — imposible construir
  una comparación que difiera en más de un eje sin modificar el propio componente.
- **Inmutabilidad estructural, no disciplina de código**: `EntradaProtocolo`/`ConfiguracionSizing`
  son `record` — `with` construye siempre una copia nueva, `entradaBase` permanece intacto en cada
  iteración, garantizado por el sistema de tipos de C#, no por revisión manual.
- **Sin ranking ni recomendación, verificado en tiempo de ejecución**: P5 (reflexión sobre los
  tipos de resultado, ningún campo sugiere posición/puntuación) y P6 (reflexión sobre la superficie
  pública de `ComparadorGestores`, único método expuesto es `Comparar`) — ambas fallan si esa
  superficie se agrega en el futuro sin pasar por una decisión D-N nueva.
- **Corrida individual fallida no invalida la comparación**: P7 confirma que una corrida
  `Failed`/`Incomplete` se refleja en su propia fila (`Metricas = null`), sin descartar las demás
  filas de gestores que sí tuvieron éxito.
- **Sin recalcular ninguna métrica**: toda `MetricasFinancieras` de cada fila proviene sin
  modificación de `ResultadoCorridaTimeframe.MetricasFinancieras`, ya calculada por
  `CalculadoraMetricasFinancieras` (D-072/D-077).
- **`src/` intacto**: a diferencia de Caso 5A, Caso 5B no modifica ningún archivo de `src/` —
  verificado por `git status --porcelain -- src/` vacío en todo el ciclo.
- **Caso 5A intacto**: ningún archivo de `GestorCapital`, `ConfiguracionSizing`, `IGestorRiesgo`,
  `IIdentidadGestorRiesgo`, los 3 gestores concretos, `BacktestRunner`,
  `IdentidadExperimentoCompleta` fue modificado.

---

## Exclusiones (explícitas)

- **Sistema recomendador de gestores**: fuera de esta versión — ningún componente decide ni
  sugiere un gestor. Candidato de una fase posterior ("Caso 5C" o equivalente), condicionada a que
  exista evidencia comparativa acumulada suficiente.
- **Pesos de métricas / puntuación combinada**: ninguna función combina métricas de distintos
  gestores en un único indicador.
- **Criterio de elección**: ningún campo ni método expresa preferencia entre gestores.
- **Optimización/calibración**: ningún parámetro de ningún gestor se ajusta observando resultados
  (D-030).
- **Kelly fraccionado, Masaniello**: siguen fuera, bloqueo metodológico de Caso 2.3 no resuelto
  (D-110, heredado sin cambios desde Caso 5A).
- **Comparación multi-timeframe/multi-estrategia**: `Comparar` exige exactamente 1 timeframe en
  `entradaBase` — extender a más de un eje simultáneo es alcance futuro explícito, no resuelto
  aquí.
- **`IStrategy`, las 6 estrategias, `AplicadorFill`, `ResolutorCrossZero`, `EjecutorProtocolo`,
  `EntradaProtocolo` intactos**: ninguna modificación de código.

Todo lo anterior queda registrado en `DECISIONES_CASO5B_V1.md`,
`ESPECIFICACION_IMPLEMENTACION_COMPARADOR_GESTORES_V1.md` y `AUDITORIA_CASO5B_V1.md` — fuera de
esta versión.

---

## Evidencia

- **8/8 pruebas Caso 5B** (`caso5/Program.cs`, `TestsComparadorGestores.EjecutarTodos()`).
- **10/10 pruebas Caso 5A** sin regresión.
- **18/18 pruebas del módulo `caso5` completo**, ejecutadas en la misma corrida.
- **126/126 tests de producción** sin cambio.
- **`git status --porcelain -- src/ tests/`**: vacío en todo el ciclo de Caso 5B.
- Auditoría de cierre: `caso5/AUDITORIA_CASO5B_V1.md`.

---

## Regla de evolución

Cualquier extensión que amplíe el alcance de Caso 5B — sistema recomendador, ranking, pesos de
métricas, comparación multi-timeframe/multi-estrategia, Kelly/Masaniello — requiere una **nueva
fase**, nunca una edición in-place de V1 (mismo principio que la regla de evolución de
`VERSION_EXPERIMENTAL_CASO5A_V1.md`/`caso3/VERSION_EXPERIMENTAL_CASO3B_V1.md`).

```
V1 Experimental — Caso 5B (congelada)
        |
        v
  recomendador / ranking / pesos / multi-timeframe / Kelly / Masaniello activados
        |
        v
Caso 5C — o fase equivalente
```

---

## Fuera de alcance de este documento

No se implementó código adicional. No se modifica ningún módulo. No se selecciona ni abre ninguna
fase siguiente (Caso 5C, sistema recomendador) — conforme a la restricción explícita de este cierre.

---

## Criterio de cierre de este documento

- ✓ Identificación formal (nombre, versión, estado, fecha) registrada.
- ✓ Componentes incluidos listados con archivo y decisión de origen (D-112 a D-115).
- ✓ Decisiones congeladas referenciadas, sin reasignaciones, todas aprobadas e implementadas.
- ✓ Garantías (comparación reproducible verificada por mecanismo, control experimental por
  construcción, inmutabilidad estructural, sin ranking verificado en tiempo de ejecución, sin
  recalcular métricas, `src/`/Caso 5A intactos) declaradas y respaldadas por evidencia ya
  verificada.
- ✓ Exclusiones declaradas explícitamente (recomendador, pesos, criterio de elección,
  optimización, Kelly/Masaniello, multi-timeframe/multi-estrategia).
- ✓ Evidencia referenciada (8/8 + 10/10 + 18/18 + 126/126, `src/`/`tests/` sin cambios).
- ✓ Regla de evolución (nueva fase ante cambio de alcance) establecida.
- ⏳ Pendiente: preparación de commit y tag `caso5b-v1-experimental`.
