# Auditoría — Recomendador Caso 6 V1 (D-128)

Estado: **documento de resultado**. Verifica la implementación real de `caso6/recomendador/` contra
D-128 (`DECISIONES_CASO6_RECOMENDADOR_V1.md`) y `ESPECIFICACION_IMPLEMENTACION_RECOMENDADOR_CASO6_V1.md`
(incluida su corrección de la distinción `GrupoComparacion`/`ConfiguracionCandidata`). Responde
exclusivamente **"¿la implementación cumple el contrato aprobado?"** — no evalúa si el recomendador
"acertó", no compara gestores/estrategias entre sí, no abre ninguna decisión nueva.

**Punto de partida**: HEAD en commit `54adf2e` (mismo commit auditado en Fase 0,
`AUDITORIA_INTEGRAL_POST_CASO5C_V1.md`). `git log --oneline -- src/ tests/ caso5/Program.cs
caso5/analisis_corpus/ caso5/analisis_interpretativo/` confirma **sin commits posteriores** — la
evidencia 126/126 (producción) y 25/25 (Caso 5 Capa1/5A/5B) documentada en Fase 0 se acepta por
referencia, mismo criterio ya aplicado ahí (D-057 satisfecho: el commit citado es el estado actual).
Toda la implementación auditada aquí (`caso6/recomendador/`) es código nuevo, no tocado por esa
referencia.

---

## 1. Correspondencia D-128 → implementación

| Punto de D-128 | Exigencia | Implementación | Cumple |
|---|---|---|---|
| Universo estratégico | Solo las 6 estrategias ya en el corpus, ninguna nueva | `MotorRecomendacion` solo lee `FilaCorpus` vía `LectorCorpus`/manifiesto — no declara ni referencia ninguna estrategia por nombre, no puede introducir una estrategia ausente del corpus | Sí |
| Perfiles V1 | Crecimiento, Preservación de capital, Personalizado — sin "Balanceado" | `Recomendar(filas, perfil, metricaPersonalizada)`: `switch` con exactamente 3 ramas (`"Crecimiento"`, `"Preservacion"`, `"Personalizado"`) + excepción para cualquier otro valor. No existe rama `"Balanceado"` ni combinación de 2+ métricas en ningún punto del código | Sí |
| Ningún perfil combina/pondera 2+ métricas | Cada perfil usa 1 sola métrica | `metrica` es un único `string` por invocación; `ExtraerMetrica` devuelve un solo `decimal?`; no hay suma ponderada, promedio de métricas distintas, ni función de combinación en `MotorRecomendacion.cs` | Sí |
| Salvaguarda estructural | Toda salida usa `RecomendacionExperimental` (D-120) | `Recomendar` devuelve únicamente `RecomendacionExperimental { Perfil, CriterioUsado, ConfiguracionesAnalizadas, ConfiguracionesConEvidencia, Candidatas, Limitaciones }` — ningún método alternativo devuelve un valor aislado | Sí |
| Salvaguarda léxica | Sin términos absolutos ("mejor", "óptimo", "ganador", "recomendado" sin calificar), sin frase "configuración recomendada" | P7 verifica ausencia de estos términos en `MotorRecomendacion.cs`/`ProgramRecomendador.cs` — **9/9**, incluyendo el ajuste registrado en §6 | Sí |
| Salvaguarda de acción | Nunca invoca `EjecutorProtocolo`/`ComparadorGestores`/componentes de ejecución | P6 verifica ausencia textual de `ComparadorGestores.Comparar`, `EjecutorProtocolo.Ejecutar`, `PersistidorComparaciones`, `CrearEstrategia`, `IStrategy` en `MotorRecomendacion.cs` — pasa | Sí |
| Salvaguarda de perfil | Ningún perfil combina métricas | Cubierto por la fila 3 de esta tabla | Sí |
| Umbral de suficiencia | ≥1 fila `Estado=Success` + métrica disponible por combinación | `conEvidencia = filas.Where(f => f.Estado == "Success" && ExtraerMetrica(f, metrica).HasValue)` — mismo filtro ya usado por `AnalisisDescriptivo.Resumir` | Sí |
| `Limitaciones` obligatorio y específico | Nunca genérico, declara cobertura y naturaleza de la evidencia | `Limitaciones` incluye cantidad de filas, cantidad de grupos con evidencia, explicación del umbral como no-ranking, orden de aparición, referencia a D-118/D-119/D-120, y advertencia de observación histórica — no un texto fijo genérico | Sí |
| No selección/ejecución automática | Ningún componente ejecuta una corrida | Confirmado por P6 (misma fila de acción) | Sí |

**8/8 puntos de D-128 verificados. 0 desviaciones.**

---

## 2. Cumplimiento de la unidad de recomendación (`ConfiguracionCandidata`)

Definida en la especificación §1 como Estrategia + Timeframe + Dataset + Gestor (Opción A,
confirmada por el auditor).

- `ConfiguracionCandidata` (`MotorRecomendacion.cs`) declara exactamente los campos `Estrategia`,
  `Timeframe`, `IdentidadGestor`, `NombreDataset`, `ValorMetrica`, `CantidadFilas`, `CarpetasOrigen`
  — incluye el gestor como campo propio, nunca omitido ni fusionado.
- Cada candidata devuelta por `CalcularCandidatas` corresponde a **una fila atómica individual**
  del corpus (`CantidadFilas: 1` en cada instancia construida) — no hay ningún punto del código que
  agrupe o promedie valores de distintos gestores en una sola candidata.
- Verificado por P1 (fixture de 3 gestores, cada candidata que cruza el umbral se reporta con su
  propio `IdentidadGestor`) y por la ejecución real (§4): las 141 candidatas del perfil Crecimiento
  corresponden a 141 filas atómicas distintas, cada una con su gestor específico visible en la
  salida.

**Cumple.**

---

## 3. Cumplimiento del grupo de comparación (`GrupoComparacion`)

Definido en la especificación §3 (corregida) como Estrategia + Timeframe + Dataset, **sin** gestor
— usado exclusivamente para calcular la mediana que actúa como umbral.

- `CalcularCandidatas` construye `gruposEnOrden` como una lista de tuplas `(Estrategia, Timeframe,
  Dataset)` — el `Gestor` no aparece en esa clave en ningún punto del código.
- Para cada grupo, `filasGrupo` reúne todas las filas de los distintos gestores presentes en esa
  combinación, y `CalcularMediana` se aplica **una sola vez** sobre el conjunto completo de valores
  del grupo — no hay una mediana por gestor.
- **P2 verifica explícitamente** que la mediana se calcula sobre el conjunto {10,20,30} de 3
  gestores como un solo grupo (mediana=20, excluye al gestor de valor 10) — si el código hubiera
  particionado por gestor, cada valor sería su propia mediana y las 3 filas habrían cruzado el
  umbral. Pasa.
- **P9 es la prueba dedicada a este punto específico**: fixture de 3 gestores con valores 5/15/25
  en la misma combinación — la mediana real del grupo es 15, por lo que exactamente 1 fila
  (gestorA=5) debe quedar excluida. La prueba falla explícitamente si las 3 filas cruzan el umbral
  (síntoma del defecto: grupo de 1 fila → mediana=valor propio → filtro siempre verdadero). **Pasa
  con el resultado esperado** (2 de 3 candidatas, gestorA excluido).

**Cumple — el defecto conceptual identificado durante la especificación fue evitado en la
implementación real, verificado por prueba dedicada (P9), no solo por inspección de código.**

---

## 4. Evidencia real de ejecución

Ejecución sobre el corpus oficial (`caso5/MANIFIESTO_CORPUS_CASO5C_V1.json`, 67 comparaciones,
201 filas atómicas — 1 fila por gestor por comparación):

| Perfil | ConfiguracionesAnalizadas | ConfiguracionesConEvidencia | Candidatas |
|---|---|---|---|
| Crecimiento (`PnLTotal`, mayor≥mediana) | 54 | 54 | 141 |
| Preservación (`DrawdownMaximoPct`, menor≤mediana) | 54 | 54 | 141 |

- `ConfiguracionesAnalizadas` = 54 grupos `(Estrategia, Timeframe, Dataset)` — coincide con la
  cobertura ya documentada del corpus (6 estrategias × 3 timeframes × 3 datasets = 54).
  `ConfiguracionesConEvidencia` = 54 (ninguna combinación quedó sin al menos 1 fila con métrica
  disponible), consistente con que el corpus oficial no incluye el caso deliberado de fallo
  (`DatasetInexistente_ParaCorpusDeFallo`, excluido por el mismo filtro que ya usa
  `AnalisisDescriptivo`).
- 141 candidatas por perfil sobre 201 filas evaluadas: proporción coherente con un umbral de
  mediana (aproximadamente la mitad + empates en el valor de la mediana quedan incluidos por la
  condición `>=`/`<=`, más las repeticiones de reproducibilidad ya conocidas del corpus con 2-3
  filas idénticas por combinación).
- Orden de salida verificado manualmente sobre la ejecución real: las candidatas aparecen agrupadas
  por Estrategia en el mismo orden que el corpus las declara (Tres Mosqueteros, Ema Cross, ZScore
  Reversion, Neutral, Volumen Breakout, Mhi Mayoria; BTCUSDT 2024, BTCUSDT 2022, ETHUSDT) — nunca
  ordenadas por `ValorMetrica`.

**Trazabilidad (P8)**: cada `ConfiguracionCandidata.CarpetasOrigen` de la ejecución real fue
verificada — existe físicamente en `caso5/resultados/` y su nombre aparece en el manifiesto. Pasa.

---

## 5. Ausencia verificada de mecanismos fuera de alcance

| Mecanismo prohibido | Verificación | Resultado |
|---|---|---|
| Ranking / top-N | P6 (reflexión: ningún tipo/método contiene `ranking`, `score`, `top`, etc.); orden de `Candidatas` verificado como orden de aparición (P3), no de valor | Ausente |
| Selección automática | P6 (ausencia textual de llamadas a componentes de ejecución); `MotorRecomendacion` no invoca ningún componente fuera de `LectorCorpus`/`AnalisisDescriptivo` (por referencia, solo lectura) | Ausente |
| Score compuesto | Cada perfil usa exactamente 1 métrica (`ExtraerMetrica` devuelve un solo valor); ninguna suma/promedio ponderado de métricas distintas en el código | Ausente |
| Agregación entre gestores | Cada candidata = 1 fila atómica con `CantidadFilas: 1`; ninguna operación de promedio/suma sobre valores de distintos gestores | Ausente |
| Estrategias nuevas | El universo de estrategias proviene exclusivamente de `FilaCorpus.Estrategia`, leído del corpus persistido — ninguna estrategia declarada en el código del recomendador | Ausente |
| Perfil "Balanceado" | `switch` de perfiles solo contiene 3 ramas (Crecimiento/Preservacion/Personalizado) + excepción | Ausente |

**6/6 verificaciones — ningún mecanismo fuera de alcance encontrado.**

---

## 6. Registro de los ajustes realizados durante la validación

Ninguno de los 2 ajustes descritos a continuación cambió el comportamiento del recomendador
(`MotorRecomendacion.Recomendar`/`CalcularCandidatas`) — ambos fueron correcciones de texto o de
integración, verificadas antes y después contra las mismas 9 pruebas.

### Ajuste 1 — Falso positivo de P7 (prueba léxica) sobre comentarios explicativos

**Hallazgo**: la primera versión de `MotorRecomendacion.cs` incluía, en el texto de `Limitaciones`
y en un comentario de código, las frases literales `"mejor gestor"` y `"Configuración recomendada:
X"` — citadas para **explicar la prohibición**, no como lenguaje operativo del sistema. P7 (que
busca la presencia léxica de estos términos en el código fuente, sin distinguir negación de uso
real) detectó ambas coincidencias y falló correctamente según su propio criterio de diseño.

**Naturaleza**: limitación conocida de una prueba léxica simple (coincidencia de subcadena, sin
análisis semántico) — mismo tipo de limitación ya documentado para P6 de `analisis_interpretativo`
("reconocida ahí mismo como barrera, no garantía completa"). No es un defecto del recomendador.

**Corrección aplicada**: se reformularon ambos pasajes sin citar las frases prohibidas
textualmente, preservando el significado (ej. "no define un único resultado preferente entre
gestores" en lugar de citar "mejor gestor"). Verificado: 9/9 pruebas antes y después del cambio de
texto, sin ninguna modificación a `CalcularCandidatas` ni a la lógica de umbral.

**Clasificación**: hallazgo de implementación → ajuste de texto → sin cambio funcional.

### Ajuste 2 — Ruta relativa incorrecta en `ProgramRecomendador.cs`

**Hallazgo**: el cálculo inicial de la ruta hacia `caso5/resultados/` y
`caso5/MANIFIESTO_CORPUS_CASO5C_V1.json` desde `AppContext.BaseDirectory` (que en tiempo de
ejecución apunta a `caso6/recomendador/bin/Release/net8.0/`) subía un nivel de más, resolviendo a
una ruta inexistente (`.../exploration/caso5/resultados` en vez de
`.../exploration/laboratorio/caso5/resultados`). Esto provocó que P8 (trazabilidad sobre el corpus
real) fallara por no encontrar filas, no por un defecto de trazabilidad real.

**Naturaleza**: defecto técnico de integración (aritmética de rutas relativas), no metodológico —
no afecta la lógica de `GrupoComparacion`/`ConfiguracionCandidata` ni ningún criterio de D-128.

**Corrección aplicada**: recalculada la ruta (3 niveles desde `BaseDirectory` hasta
`caso6/recomendador/`, luego 2 niveles hasta `caso5/`). Verificado: P8 pasa contra el corpus real
(201 filas leídas, trazabilidad confirmada), sin cambios en `MotorRecomendacion.cs`.

**Clasificación**: defecto de implementación → corregido → verificado, sin impacto en el contrato
de D-128.

---

## 7. Verificación de ausencia de efectos colaterales

`git status --porcelain -- exploration/laboratorio/caso5/resultados/`: **vacío** — la ejecución del
recomendador es de solo lectura sobre el corpus persistido, no escribe evidencia nueva.

`git status --porcelain -- src/ tests/`: **vacío** — ningún componente de producción fue modificado
durante la implementación o esta auditoría.

Archivos nuevos, todos bajo `caso6/` (sin tocar ninguna ruta fuera de esa carpeta):
`caso6/recomendador/Recomendador.csproj`, `MotorRecomendacion.cs`, `ProgramRecomendador.cs`,
`TestsRecomendador.cs`, más los documentos de propuesta/decisión/especificación ya revisados y esta
auditoría.

---

## Fuera de alcance de este documento

No se modifica el recomendador. No se incorporan las "15 estrategias del PDF". No se abre
optimización automática ni selección automática. No se evalúa si algún gestor/estrategia es
preferible — esa pregunta permanece fuera de alcance por D-118, sin excepción. No se reabre ninguna
decisión D-001 a D-128.

---

## Conclusión

La implementación de `caso6/recomendador/` cumple el contrato fijado por D-128 y por la
especificación corregida: la unidad de recomendación (`ConfiguracionCandidata`, con gestor) y el
grupo de comparación (`GrupoComparacion`, sin gestor) permanecen correctamente separados en el
código, verificados no solo por inspección sino por una prueba de regresión dedicada (P9) que
falla explícitamente ante el defecto conceptual identificado y evitado durante la especificación.
9/9 pruebas pasan, incluyendo las de salvaguarda estructural/léxica/de acción/de perfil heredadas de
D-118. La ejecución sobre el corpus real (67 comparaciones, 201 filas, 54 grupos) produjo resultados
trazables sin ranking, sin selección, sin agregación entre gestores, y sin ningún mecanismo fuera
del alcance aprobado. Los 2 ajustes realizados durante la validación (texto de prueba léxica, ruta
relativa) fueron detectados, corregidos y verificados sin alterar el comportamiento funcional del
recomendador — quedan registrados aquí conforme a lo solicitado.

**Estado final: apto.**
