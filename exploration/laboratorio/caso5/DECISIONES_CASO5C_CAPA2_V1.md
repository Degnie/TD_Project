# Decisiones — Caso 5C Capa 2 (Análisis Descriptivo del Corpus)

Estado: **D-123 resuelta**. Misma estructura usada en D-001 a D-122 (decisión, opciones, criterio,
restricciones, evidencia). Resuelve la pregunta abierta por `PROPUESTA_CASO5C_CAPA2_V1.md` §4: cuál
de las 3 opciones (A — análisis descriptivo, B — nuevo instrumento, C — consulta sin inferencia)
define la forma de Caso 5C Capa 2, y bajo qué límites.

---

## D-123 — Forma de Caso 5C Capa 2

**Estado**: 🟢 Aprobada.

**Decisión**: cuál de las 3 opciones no descartadas de `PROPUESTA_CASO5C_CAPA2_V1.md` §4 define qué
construye Caso 5C Capa 2 sobre el corpus de 49 comparaciones acumuladas, y qué queda explícitamente
fuera de esa construcción.

### Resolución adoptada

**Opción A — análisis descriptivo del corpus**, con límites estrictos declarados en esta misma
decisión (no diferidos a la especificación de implementación).

**Razón central**: el corpus actual (49 comparaciones, 2 períodos temporales, 6 estrategias, 3
gestores, sobre `BTCUSDT`) ya tiene suficiente estructura para responder preguntas descriptivas
sobre lo que contiene. Esperar indefinidamente más diversidad de evidencia (Opción B) antes de
construir cualquier capacidad analítica repetiría el patrón ya observado dos veces en este proyecto
(V1 → V2 → diversidad temporal, cada expansión justificada como "antes de interpretar, ampliemos
primero") sin extraer nunca conocimiento del sistema ya construido. La limitación de instrumento
único (`BTCUSDT ≠ mercado completo`) es real y permanece — pero no bloquea un análisis descriptivo
que se mantenga honesto sobre su propio alcance (D-120 ya exige declaración de limitaciones en
cualquier salida que se acerque a interpretación).

**Opción C (consulta sin inferencia) no se descarta como principio** — la distinción entre "qué
ocurrió" (C) y "qué distribuciones/patrones aparecen" (A) es de grado, no de naturaleza: ambas
observan el corpus sin decidir sobre él. Esta decisión selecciona A porque aporta más valor sobre lo
que Caso 5C Capa 1 ya provee (persistencia legible por comparación individual) sin cruzar la línea
que D-118 ya trazó permanentemente.

**Opción B (nuevo instrumento) queda pospuesta, no descartada** — mismo estatus que la Vía A de
D-121 (instrumento) respecto a la Vía B (tiempo): sigue siendo la vía correcta para cerrar la
limitación estructural de diversidad, pero no es prerrequisito para que exista un primer análisis
descriptivo del corpus ya disponible.

### Qué análisis están permitidos

Toda salida de Capa 2 bajo esta decisión debe ser **puramente descriptiva de lo que el corpus
contiene**, sin ordenar, preferir, ni sugerir ninguna combinación estrategia/gestor/timeframe/
período por encima de otra:

- **Agregaciones de cobertura**: cuántas comparaciones existen por combinación
  estrategia/timeframe/gestor/período; qué combinaciones de la matriz declarada tienen evidencia y
  cuáles no.
- **Distribución de métricas observadas**: rango (mínimo/máximo), y estadísticos descriptivos
  simples (media, mediana) de una métrica ya persistida (ej. `DrawdownMaximoPct`,
  `ProfitFactor`) agrupados por gestor o por timeframe — mostrando la distribución completa, nunca
  colapsada en un solo valor que sugiera "el resultado" de un gestor.
- **Estabilidad entre períodos**: si un patrón observado en un período (ej. degeneración económica
  en timeframes cortos) también aparece en el otro — presentado como comparación factual de
  presencia/ausencia, nunca como conclusión de "es robusto" o "es confiable".
- **Presencia de casos atípicos ya notados en auditorías previas**: ej. que `ZScoreReversion` no
  generó actividad en ninguno de los 2 períodos — un hecho verificable, no una evaluación de si eso
  es deseable.
- **Cualquier salida debe declarar explícitamente su cobertura** (sobre qué subconjunto del corpus
  se calculó, cuántas comparaciones representa) — mismo principio D-010 (Caso 1) ya exigido para
  toda comparación desde el inicio del proyecto.

### Qué análisis quedan prohibidos

- **Ranking o cualquier orden derivado de una métrica** — incluso si D-118 permite "ordenar por
  criterio explícito y declarado" como forma futura de recomendación, esta decisión **no habilita
  esa forma todavía**: D-123 abre únicamente descripción, no recomendación (D-118/D-119/D-120 fijan
  el marco de recomendación, pero su activación queda para una decisión posterior explícita, no
  implícita en abrir Capa 2).
- **Selección o sugerencia de "mejor gestor" o "mejor configuración"**, bajo cualquier nombre
  (ej. "recomendado", "óptimo", "preferible") — permanece excluida por D-118, reafirmada aquí sin
  cambios.
- **Cualquier puntuación compuesta** que combine más de una métrica en un único número — prohibido
  desde D-014/D-025/D-026/D-047/D-076/D-118, reafirmado aquí.
- **Cualquier extrapolación a instrumentos no representados en el corpus** — ninguna salida de esta
  Capa 2 puede formularse de manera que sugiera aplicabilidad más allá de `BTCUSDT`, dado que
  `PROPUESTA_CASO5C_CAPA2_V1.md` §2 estableció esta limitación como no resuelta.
- **Selección/ejecución automática de un gestor sobre una corrida real** — excluida de forma
  permanente por D-118, no condicional a esta decisión.
- **Calibración de ningún umbral o parámetro observando el propio corpus generado** (D-030) — un
  análisis descriptivo no necesita umbrales de suficiencia; si una futura fase de recomendación los
  requiere (D-119), se calibran fuera del corpus que se busca evaluar.

### Restricciones que aplican

- D-118, D-119, D-120 permanecen **intactas, sin modificación** — esta decisión no las reabre, las
  hereda como marco que Capa 2 (en cualquiera de sus formas futuras) debe seguir respetando.
- El componente de análisis descriptivo consume el corpus persistido por Caso 5C Capa 1
  (`PersistidorComparaciones`/`IDENTIDAD_COMPARACION.json`/`COMPARACION_GESTORES_V1.md`) — no
  modifica `ComparadorGestores`, `PersistidorComparaciones`, ni ningún gestor (mismo criterio de no
  tocar componentes congelados ya aplicado en V1/V2/Sub-campaña D).
- Ninguna salida de este componente puede omitir la declaración de cobertura (qué parte del corpus
  representa) — evita que una agregación parcial (ej. solo 2022-2023) se lea como si describiera
  todo el corpus.
- La Opción B (nuevo instrumento) sigue disponible como paso futuro independiente — esta decisión no
  la descarta ni la condiciona a que el análisis descriptivo se complete primero.

### Evidencia

- `PROPUESTA_CASO5C_CAPA2_V1.md` §1-§4: estado de madurez del proyecto, limitación de instrumento
  único, y las 3 opciones con sus a favor/en contra.
- `AUDITORIA_DIVERSIDAD_TEMPORAL_CASO5C_V1.md` §4: confirma que la limitación de instrumento sigue
  abierta y que 2 períodos no constituyen una serie — razón por la que ninguna salida de esta Capa 2
  puede sugerir generalización más allá de `BTCUSDT`.
- Decisión explícita del auditor: "El corpus actual ya tiene suficiente estructura para responder
  preguntas descriptivas, y esperar indefinidamente más datos antes de construir ninguna capacidad
  analítica repetiría el ciclo de expansión sin extraer conocimiento del sistema" — incorporada aquí
  como razón central, no solo como nota externa.
- D-118 (`DECISIONES_CASO5C_V1.md`): exclusión permanente de selección automática, reafirmada sin
  cambios como límite que esta decisión no toca.

---

## Fuera de alcance de este documento

No se especifica la implementación del componente de análisis descriptivo (queda para
`ESPECIFICACION_IMPLEMENTACION_CASO5C_CAPA2_V1.md`, documento posterior). No se decide si/cuándo se
abre la Opción B (nuevo instrumento). No se activa ninguna forma de recomendación (D-118/D-119/
D-120 siguen en estado de principio, sin implementación). No se calcula ningún análisis real sobre
el corpus en este documento.

---

## Próximo documento

`ESPECIFICACION_IMPLEMENTACION_CASO5C_CAPA2_V1.md` — diseño concreto del componente de análisis
descriptivo (qué agregaciones exactas, sobre qué estructura de datos, con qué formato de salida),
sujeto a los límites de "permitido"/"prohibido" fijados en D-123.
