# Auditoría de Cierre — Caso 5B: Capa Comparativa de Gestores de Riesgo

Estado: **documento de cierre de sub-fase — Caso 5B completo**. Consolida evidencia verificada del
ciclo propuesta → decisión → especificación → implementación → pruebas → auditoría para D-112 a
D-115. Mismo patrón que las auditorías de cierre de Caso 5A (`AUDITORIA_CASO5A_V1.md`) y Caso 3B.

---

## 1. Alcance de Caso 5B

**Objetivo**: construir una capa comparativa reproducible de gestores de riesgo bajo condiciones
experimentales controladas.

**Incluye**:
- Ejecución múltiple de gestores sobre la misma estrategia/dataset/timeframe/configuración
  económica.
- Acumulación de resultados de N corridas en una sola estructura.
- Comparación estructurada de métricas ya existentes (D-111), lado a lado.
- Salida tabular derivada, separada del objeto comparativo.

**No incluye** (confirmado por ausencia de código, no solo por declaración — ver §5, P5/P6):
- Sistema recomendador de gestores.
- Ranking o puntuación entre gestores.
- Optimización o calibración de ningún parámetro.
- Selección automática de gestor por ninguna estrategia o corrida.

---

## 2. Relación con Caso 5A

Caso 5B no reconstruye ni duplica infraestructura — consume exclusivamente lo que Caso 5A ya
congeló (`caso5a-v1-experimental`):

| Caso 5A aportó | Caso 5B lo usa para |
|---|---|
| `IGestorRiesgo` (D-108) | Recibir una lista de gestores intercambiables sin conocer su implementación concreta |
| `IIdentidadGestorRiesgo` (D-109, precisión) | Etiquetar cada fila de la comparación con una identidad determinista y estable |
| 3 gestores concretos (D-110) | Ser los candidatos por defecto de cualquier comparación (Fixed Fractional, Fixed Risk, Volatility Sizing) |
| `MetricasFinancieras` extendida (D-111) | Ser la única fuente de datos de cada fila comparativa |
| `IdentidadExperimentoCompleta`/`HashConfiguracionEconomica` | Verificar por mecanismo, no solo por inspección, que el único eje que varía entre corridas es el gestor (P3, §5) |

**Ningún archivo de Caso 5A fue modificado** en este ciclo — `GestorCapital`, `ConfiguracionSizing`,
`IGestorRiesgo`, `IIdentidadGestorRiesgo`, los 3 gestores concretos, `BacktestRunner`,
`IdentidadExperimentoCompleta`: todos intactos, confirmado por `git status --porcelain` vacío sobre
esas rutas durante todo el ciclo de Caso 5B.

---

## 3. Resolución D-112 a D-115 — resumen

| Decisión | Resolución |
|---|---|
| D-112 | Ubicación arquitectónica: `ComparadorGestores`, componente nuevo de laboratorio — mismo patrón conceptual que `ComparadorMultiTimeframe`, sin dependencia de código con él; no toca `EjecutorProtocolo`/`EntradaProtocolo` |
| D-113 | Control experimental por construcción: `Comparar(EntradaProtocolo base, gestores)` — la única variable entre las N corridas internas es el gestor, garantizado por el propio flujo (`entradaBase with { Sizing = ... }`), no por disciplina del llamador |
| D-114 | Fuente de datos: `MetricasFinancieras` exclusivamente, ya poblada en `ResultadoCorridaTimeframe` — `ReporteOperacional` excluido explícitamente por su acoplamiento a `ResolucionDeIntentos`/martingala (D-055), que no representa a 4 de las 6 estrategias congeladas |
| D-115 | Salida estructurada (`ResultadoComparativoGestores`) como fuente única de verdad + render de tabla derivado (`RenderizadorComparacionGestores`) — sin ningún campo de ranking, puntuación o "mejor gestor", a diferencia deliberada del precedente (`MejorResultadoObservado` de `ComparadorMultiTimeframe`) |

Ninguna de las 4 decisiones fue reabierta durante la implementación — la especificación
(`ESPECIFICACION_IMPLEMENTACION_COMPARADOR_GESTORES_V1.md`) añadió una precisión de código no
prevista explícitamente en D-113 (exigir `entradaBase.Timeframes.Count == 1`, ver §6), pero no
modificó el contenido de ninguna decisión ya resuelta.

---

## 4. Evidencia de implementación

**`caso5/ComparadorGestores.cs`** (nuevo):
- `FilaComparacionGestor(IdentidadGestor, Estado, Metricas)` — una fila por gestor, `Metricas` es
  `MetricasFinancieras?` (null en corridas no exitosas, D-114).
- `ResultadoComparativoGestores(Estrategia, Timeframe, NombreDataset, Filas)` — `Filas` conserva
  el orden de la lista de gestores recibida (D-112/D-114).
- `ComparadorGestores.Comparar(EntradaProtocolo entradaBase, IReadOnlyList<IGestorRiesgo> gestores)`
  — único método público de la clase (verificado por reflexión, P6). Valida entrada (§6), itera
  gestores, invoca `EjecutorProtocolo.Ejecutar` una vez por gestor vía `entradaBase with { Sizing =
  new ConfiguracionSizing(gestor) }`, extrae `MetricasFinancieras` de la corrida del timeframe
  declarado.
- `RenderizadorComparacionGestores.Generar(ResultadoComparativoGestores) : string` — tabla de
  texto derivada, sin ninguna columna/fila de ranking (D-115).

**`caso5/TestsComparadorGestores.cs`** (nuevo) — 8 pruebas, mismo patrón runner manual que
`TestsGestoresRiesgo.cs`.

**`caso5/Program.cs`** (modificado) — invoca ambas suites de Caso 5 (A y B) en la misma ejecución.

---

## 5. Evidencia de pruebas

**8/8 pruebas de Caso 5B** (`caso5/TestsComparadorGestores.cs`): P1 (validación de entrada —
`Sizing` previo, múltiples timeframes, gestores vacío/null, todos fallan explícitamente), P2
(coincidencia exacta con `EjecutorProtocolo.Ejecutar` invocado directamente), P3 (identidad
experimental), P4 (orden preservado en 3 permutaciones distintas), P5 (ausencia estructural de
ranking), P6 (ausencia de método de recomendación), P7 (corrida fallida no invalida la
comparación), P8 (fallo explícito sin `IIdentidadGestorRiesgo`).

**P3 — la evidencia más importante de esta fase**: verificó, ejecutando ambas corridas por
separado fuera del comparador, que `HashCompuesto` es **idéntico** entre `GestorFixedFractional` y
`GestorFixedRisk` sobre la misma estrategia/dataset (confirma que estrategia, dataset, parámetros y
versión de protocolo no dependen del gestor, D-082), mientras que `HashConfiguracionEconomica` es
**distinto** entre ambos (confirma que el mecanismo de identidad de D-109 efectivamente distingue
gestores por su configuración declarada). No es una verificación por inspección de código — usa el
propio mecanismo de identidad ya congelado como prueba.

**P5/P6 — confirmación estructural, no de comportamiento**: P5 usa reflexión sobre
`FilaComparacionGestor`/`ResultadoComparativoGestores` para confirmar que ningún nombre de
propiedad sugiere ranking/puntuación/recomendación. P6 usa reflexión sobre la superficie pública de
`ComparadorGestores` para confirmar que `Comparar` es el único método expuesto. Ambas fallan si
alguien agrega esa superficie en el futuro sin pasar por una decisión D-N nueva — protegen el
límite de alcance en tiempo de ejecución, no solo en documentación.

**10/10 pruebas de Caso 5A** (`caso5/TestsGestoresRiesgo.cs`): sin regresión — Caso 5B no modificó
ningún archivo de Caso 5A.

**18/18 pruebas del módulo `caso5` completo** (10 Caso 5A + 8 Caso 5B), ejecutadas en la misma
corrida de `Caso5.csproj`.

**126/126 tests de producción**: sin cambio — `git status --porcelain -- src/ tests/` vacío durante
todo el ciclo de Caso 5B.

---

## 6. Hallazgos de implementación

**Hallazgo 1 — Garantía de inmutabilidad estructural, no solo disciplina de código**: la
precaución señalada antes de autorizar la implementación (que construir N variantes de
`EntradaProtocolo` no debía mutar el objeto base compartido) está satisfecha por el propio sistema
de tipos, no únicamente por cómo se escribió el código: `EntradaProtocolo` y `ConfiguracionSizing`
son `record` de C# — `entradaBase with { Sizing = ... }` construye siempre una copia nueva:
`entradaBase` permanece exactamente igual en cada iteración del bucle, sin posibilidad de mutación
accidental entre gestores. Documentado con un comentario inline en `ComparadorGestores.cs` para que
la garantía quede auditable directamente en el código, no solo en este documento.

**Hallazgo 2 — Precisión de código para D-113, no cubierta explícitamente por la decisión
original**: `EntradaProtocolo.Timeframes` es una lista (soporta multi-timeframe en el protocolo
general), pero D-113 fija la unidad de comparación a un único timeframe. La especificación de
implementación resolvió esto exigiendo `entradaBase.Timeframes.Count == 1` — falla explícitamente
si no se cumple, en vez de comparar silenciosamente solo el primer timeframe o iterar todos sin
autorización. Verificado por P1. No reabre D-113 — completa una condición de ejecución necesaria
para cumplirla, mismo criterio que D-062/D-083/D-084/D-095/D-107/precisión-D-109 en fases
anteriores.

**Ningún hallazgo requirió una decisión D-116 nueva.**

---

## 7. Límites congelados

Fuera de Caso 5B, confirmado por ausencia de código (no solo por declaración):

- **Sistema recomendador de gestores** — ningún componente decide ni sugiere un gestor.
- **Pesos de métricas** — ninguna función combina métricas de distintos gestores en un indicador
  único.
- **Criterio de elección** — ningún campo ni método expresa preferencia entre gestores (P5/P6).
- **Optimización/calibración** — ningún parámetro de ningún gestor se ajustó observando
  resultados (D-030).
- **Kelly fraccionado, Masaniello** — siguen fuera, bloqueo metodológico de Caso 2.3 no resuelto
  (D-110, heredado sin cambios).
- **`IStrategy`, las 6 estrategias, `AplicadorFill`, `ResolutorCrossZero`, `GestorCapital`,
  `IGestorRiesgo`, `EjecutorProtocolo`, `EntradaProtocolo`**: sin ninguna modificación.

---

## 8. Estado final — Decisiones de Caso 5B

| Decisión | Estado |
|---|---|
| D-112 | ✅ `ComparadorGestores` como componente separado |
| D-113 | ✅ Control experimental por construcción |
| D-114 | ✅ `MetricasFinancieras` como fuente única, `ReporteOperacional` excluido |
| D-115 | ✅ Salida estructurada + render derivado, sin ranking |

**Caso 5B V1 implementado**:
- ✅ Comparación reproducible (P3).
- ✅ Múltiples gestores (P2, P4, P7).
- ✅ Misma condición experimental garantizada por construcción (D-113, P1).
- ✅ Salida estructurada, separada de su render (D-115).

**Ninguna deuda técnica bloqueante queda abierta dentro del alcance de Caso 5B.**

**Pendiente futuro, explícitamente fuera de esta fase**: recomendación de gestores (candidato de
una fase posterior — "Caso 5C" o equivalente en el mapa de evolución), condicionada a que exista
evidencia comparativa acumulada suficiente antes de proponerse.

---

## Fuera de alcance de este documento

No se decide si Caso 5B se congela como versión experimental independiente ni si requiere una
sub-fase adicional antes del congelamiento. No se recalibra ningún parámetro. No se abre ninguna
fase de recomendación. No se extiende la comparación a multi-timeframe ni multi-estrategia.

---

## Criterio de cierre de esta sub-fase

- ✓ D-112 a D-115: cada una con opciones evaluadas, evidencia y selección explícita del auditor.
- ✓ 2 hallazgos de implementación detectados y documentados — ninguno requirió una decisión D-N
  nueva, ninguno oculto.
- ✓ 8/8 pruebas Caso 5B + 10/10 Caso 5A sin regresión + 18/18 módulo completo + 126/126 producción.
- ✓ P3 (identidad experimental) y P5/P6 (ausencia estructural de ranking/recomendación) verificadas
  con evidencia directa, no por inspección declarativa.
- ✓ Ninguna restricción de alcance relajada: `IStrategy`, estrategias, motor de matching, Caso 5A,
  Kelly/Masaniello, recomendador — todos fuera, como estaba autorizado.
- ⏳ Pendiente de tu decisión: congelar Caso 5B como versión experimental (`caso5b-v1-experimental`)
  o abrir una sub-fase adicional antes del congelamiento.
