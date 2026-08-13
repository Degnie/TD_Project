# Decisiones — Caso 6: Recomendador Basado en Evidencia (V1)

Estado: **D-128 resuelta**. Misma estructura usada en D-001 a D-127 (decisión, opciones, criterio,
restricciones, evidencia). Resuelve las 5 preguntas metodológicas dejadas abiertas por
`PROPUESTA_CASO6_RECOMENDADOR_V1.md` §6: si se aprueba implementar el recomendador, sobre qué
configuraciones trabaja, qué perfiles existen en V1, cómo evita convertirse en selector, y qué
queda prohibido.

---

## D-128 — Aprobación y alcance del Recomendador V1

**Estado**: 🟢 Aprobada.

**Decisión**: si se implementa el Recomendador, y bajo qué alcance exacto de configuraciones,
perfiles, y prohibiciones.

### Resolución adoptada

**Se aprueba implementar el Recomendador V1**, como capa que lee el corpus ya persistido (67
comparaciones, `MANIFIESTO_CORPUS_CASO5C_V1.json`) y produce salidas en el formato
`RecomendacionExperimental` ya fijado por D-120 — sin ningún mecanismo de selección automática,
ejecución, ni ajuste de parámetros.

### 1. ¿Sobre qué configuraciones trabaja?

**Únicamente sobre configuraciones ya existentes en el corpus oficial** — combinaciones
estrategia/timeframe/gestor/dataset ya ejecutadas y persistidas, leídas por el mismo mecanismo ya
congelado (`LectorCorpus`/`MANIFIESTO_CORPUS_CASO5C_V1.json`). El recomendador **no genera ninguna
configuración nueva, no prueba ningún parámetro no ya presente en el corpus, no ejecuta ningún
backtest** — coincide exactamente con el análisis de `PROPUESTA_CASO6_RECOMENDADOR_V1.md` §5.

**Universo estratégico congelado en esta versión**: las 6 estrategias ya presentes en el corpus
oficial (Tres Mosqueteros, Ema Cross, ZScore Reversion, Neutral, Volumen Breakout, Mhi Mayoria).
**Ninguna estrategia nueva se incorpora en Caso 6** — en particular, las estrategias adicionales
mencionadas por el auditor (fuera de este repositorio, "15 estrategias del PDF") quedan
explícitamente fuera de esta fase. Motivo: mezclar una expansión del espacio de candidatos con el
diseño de la lógica de recomendación impediría atribuir cualquier resultado observado a una sola
variable — mismo principio de aislamiento de dimensión ya aplicado en D-121/D-125 (nunca variar
tiempo e instrumento a la vez). Una futura expansión del universo estratégico, si se decide,
requiere su propia propuesta, posterior y separada de Caso 6.

### 2. ¿Qué perfiles existen en V1?

**Perfiles de criterio único, ninguno de combinación**:

- **Crecimiento**: `CriterioUsado` sobre una sola métrica de rendimiento (`PnLTotal` o
  `ProfitFactor`, a fijar en la especificación) — orden ascendente/descendente declarado
  explícitamente.
- **Preservación de capital**: `CriterioUsado` sobre una sola métrica de riesgo
  (`DrawdownMaximoPct` o `ExposicionMaxima`, a fijar en la especificación).
- **Personalizado**: el usuario declara explícitamente cuál de las 6 métricas ya existentes
  (`PnLTotal`, `DrawdownMaximoPct`, `ProfitFactor`, `ExposicionMaxima`, `CashFinal`, `EquityFinal`)
  usar como `CriterioUsado` — el sistema no preselecciona ningún juicio de valor, solo aplica el
  criterio ya declarado por el usuario.

**Perfil "balanceado" — NO existe en V1.** Resolución explícita: cualquier noción de "equilibrio"
entre 2+ métricas requiere, por definición, una función de combinación (pesos, umbrales, o
prioridad relativa) — eso es una puntuación compuesta, prohibida por D-118 sin excepción en esta
fase. Implementarlo requeriría primero una decisión aparte, explícita, definiendo esa función como
regla visible — no se resuelve ni se aproxima aquí. Si en el futuro se abre, será una fase
posterior con su propia propuesta/decisión, no una extensión silenciosa de V1.

**Ningún perfil combina, pondera, ni prioriza más de 1 métrica a la vez en V1** — si un usuario
quiere considerar 2+ métricas, el sistema debe presentarlas como resultados separados bajo
criterios separados, nunca fusionadas en un resultado único.

### 3. ¿Cómo evita convertirse en selector?

- **Estructural**: toda salida usa `RecomendacionExperimental` (D-120) — nunca un valor único
  aislado, siempre acompañado de `CriterioUsado`/`EvidenciaUsada`/`Limitaciones`.
- **Léxico**: prohibición de términos absolutos ("mejor", "óptimo", "ganador", "ideal",
  "recomendado" sin calificar) — mismo tipo de salvaguarda ya verificada por prueba en Capa 2/
  interpretativo (P6), reconocida ahí mismo como barrera, no garantía completa — compensada
  igual que en esas fases por ausencia de prosa generada dinámicamente y por P5 (ausencia
  estructural de campos de ranking/selección en los tipos de salida).
- **De acción**: el recomendador nunca invoca `EjecutorProtocolo`, `ComparadorGestores`, ni ningún
  componente que ejecute una corrida — verificado por prueba, mismo mecanismo ya usado en Capa 2/
  interpretativo (P9/P8 respectivamente, ausencia estructural de llamadas a componentes de
  ejecución).
- **De perfil**: ningún perfil combina métricas (punto 2) — la vía más directa hacia selección
  disfrazada queda cerrada por diseño en V1, no solo por convención de nombres.

### 4. ¿Cómo comunica incertidumbre?

Hereda D-119/D-120 sin modificación:
- **D-119**: sin evidencia suficiente para una combinación dada, el sistema **no recomienda** para
  esa combinación — nunca una recomendación de baja confianza silenciosa. La ausencia de
  recomendación es una salida válida.
- **Umbral operativo de "suficiente" para V1**: una combinación (Estrategia/Timeframe/Gestor/
  Dataset) es candidata a recomendación solo si tiene **al menos 1 fila con métrica disponible
  (`Estado: Success`, `PnLTotal.HasValue`)** en el corpus oficial — mismo criterio ya usado por
  `AnalisisDescriptivo.Resumir` para distinguir evidencia real de evidencia parcial deliberada
  (`DatasetInexistente_ParaCorpusDeFallo` queda excluida por este mismo filtro, sin necesidad de
  una regla nueva).
- **`Limitaciones` obligatorio y específico, no genérico**: debe declarar explícitamente cobertura
  asimétrica cuando aplique (ej. combinaciones con 3 filas por repetición de reproducibilidad vs.
  combinaciones con 1 sola), qué instrumentos/periodos cubre la evidencia usada, y que es
  observación histórica de backtest, nunca proyección futura — mismo texto de garantía ya usado en
  Capa 2/interpretativo, extendido aquí al nivel de cada recomendación individual.

### Qué queda permitido

- Leer el corpus oficial vía `LectorCorpus`/manifiesto, sin modificarlo.
- Calcular `EstadisticaDescriptiva` por combinación usando `AnalisisDescriptivo` ya existente, sin
  cálculo nuevo de métrica.
- Ordenar/filtrar combinaciones por **1 sola métrica declarada** (perfil o personalizado).
- Producir 0, 1, o varias `RecomendacionExperimental` — nunca forzar exactamente 1 resultado si la
  evidencia no lo sostiene.
- Declarar explícitamente cuándo el corpus no cubre una combinación solicitada.

### Qué queda prohibido

- Perfil "balanceado" o cualquier combinación de 2+ métricas en un solo criterio, en esta versión.
- Cualquier término de juicio absoluto sin calificar (D-118, reforzado aquí).
- Selección o ejecución automática de cualquier configuración (D-118, sin excepción).
- Incorporar estrategias nuevas fuera de las 6 ya congeladas en el corpus (punto 1).
- Calibrar ni ajustar ningún parámetro económico o de estrategia observando resultados (D-030).
- Recomendar sobre una combinación sin evidencia (`Estado: Success` con métrica disponible) en el
  corpus.

### Restricciones que aplican

- **D-118/D-119/D-120**: heredadas sin modificación, como marco central de esta fase.
- **D-030**: ningún parámetro se calibra en el diseño ni en la ejecución del recomendador.
- **D-127**: la auditoría integral ya cerrada no se repite ni se reabre — se toma como condición ya
  satisfecha.
- **Aislamiento de dimensión** (extendido de D-121/D-125): el universo de estrategias/instrumentos/
  periodos del corpus permanece fijo durante el diseño e implementación del recomendador — ninguna
  expansión de evidencia ocurre a la vez que se construye la lógica de recomendación.

### Evidencia

- `PROPUESTA_CASO6_RECOMENDADOR_V1.md` §1-§6: análisis completo de definición, perfiles, corpus,
  formato, separación de capas, y preguntas metodológicas.
- Precisión explícita del auditor: "mi inclinación metodológica sería que la primera versión no
  implemente balanceado" — incorporada aquí como resolución central del punto 2, no como
  sugerencia adicional.
- Precisión explícita del auditor sobre estrategias nuevas: "la propuesta del recomendador no
  debería incorporar todavía las 15 estrategias del PDF... introducir estrategias nuevas mientras
  se define el recomendador mezclaría dos variables" — incorporada como resolución del punto 1.
- D-120 (`DECISIONES_CASO5C_V1.md`): formato `RecomendacionExperimental`, heredado sin
  modificación.

---

## Fuera de alcance de este documento

No se especifica la implementación técnica (queda para
`ESPECIFICACION_IMPLEMENTACION_RECOMENDADOR_CASO6_V1.md`). No se implementa código. No se decide
la métrica exacta de cada perfil "crecimiento"/"preservación" (a fijar en la especificación, dentro
del límite ya puesto aquí: 1 sola métrica, sin combinación). No se abre expansión del universo
estratégico. No se activa selección automática ni optimización.

---

## Próximo documento

`ESPECIFICACION_IMPLEMENTACION_RECOMENDADOR_CASO6_V1.md`, traduciendo D-128 a diseño de código:
componente(s) de lectura del corpus (reutilizando `LectorCorpus`/`AnalisisDescriptivo` por
referencia, mismo patrón ya usado 2 veces en Caso 5C), estructura exacta de
`RecomendacionExperimental` en C#, mecanismo de filtrado por 1 métrica declarada, umbral de
suficiencia por combinación, y pruebas equivalentes a las ya usadas en Capa 2/interpretativo
(ausencia estructural de ranking/selección, ausencia léxica de términos prohibidos, ausencia de
llamadas a componentes de ejecución, trazabilidad completa a evidencia origen).
