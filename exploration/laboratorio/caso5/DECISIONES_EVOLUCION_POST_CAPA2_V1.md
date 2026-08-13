# Decisiones — Evolución Post-Capa 2 (Análisis Interpretativo Limitado)

Estado: **D-124 resuelta**. Misma estructura usada en D-001 a D-123 (decisión, opciones, criterio,
restricciones, evidencia). Resuelve la pregunta abierta por `PROPUESTA_EVOLUCION_POST_CAPA2_V1.md`
§4-§5: cuál de las 2 vías (A — diversidad de instrumento, B — interpretación limitada del corpus
actual) se formaliza primero, y bajo qué alcance exacto.

---

## D-124 — Dirección de evolución post-Capa 2 y alcance del análisis interpretativo limitado

**Estado**: 🟢 Aprobada.

**Decisión**: cuál de las 2 vías no descartadas de `PROPUESTA_EVOLUCION_POST_CAPA2_V1.md` §4-§5 se
formaliza primero, y — si es Vía B — qué análisis quedan permitidos y cuáles prohibidos en esa
nueva capa.

### Resolución adoptada

**Vía B — interpretación limitada del corpus actual**, formalizada primero. **Vía A (diversidad de
instrumento) queda pospuesta, no descartada** — mismo estatus que ya tenía respecto a la diversidad
temporal en D-121, ahora extendido: sigue siendo necesaria antes de cualquier recomendación futura,
pero no es condición previa para construir una capa de interpretación controlada sobre la evidencia
ya disponible.

**Razón central**: el sistema ya tiene infraestructura y evidencia suficiente (49 comparaciones, 2
períodos, cadena completa demostrada hasta Capa 2 descriptiva) para extraer conocimiento descriptivo
adicional. Continuar aumentando el corpus antes de aprender algo más del corpus existente repetiría
un ciclo de acumulación sin extracción de conocimiento — mismo riesgo ya señalado al decidir D-123
sobre la Opción B de esa propuesta (ampliar evidencia en vez de analizar), ahora aplicado a la
elección entre Vía A y Vía B de esta propuesta.

**Nombre de la fase**: **Análisis interpretativo limitado** — deliberadamente no "análisis
avanzado", "motor de insights", ni ningún nombre que sugiera cercanía a recomendación. El nombre
debe seguir comunicando el límite tan claramente como "análisis descriptivo" lo hizo para Capa 2.

### Qué análisis quedan permitidos

Un nivel por encima de Capa 2 (que se limitó deliberadamente a una dimensión de agrupación por vez,
`ESPECIFICACION_IMPLEMENTACION_CASO5C_CAPA2_V1.md` §3), pero estrictamente por debajo de
recomendación:

- **Detectar relaciones observadas entre condiciones**: ej. cruces explícitos de 2+ dimensiones
  (estrategia × timeframe × gestor × período) que Capa 2 evitó deliberadamente por diseño — con la
  salvedad de que un cruce multi-dimensional debe presentarse siempre como tabla/lista completa
  (todas las combinaciones observadas), nunca como una única celda destacada.
- **Agrupar comportamientos**: ej. clasificar combinaciones del corpus según si muestran o no un
  patrón ya nombrado en auditorías previas (degeneración de drawdown, ausencia de actividad) —
  agrupación por presencia/ausencia de un hecho ya documentado, no por un juicio nuevo de calidad.
- **Describir condiciones donde aparece cierta evidencia**: ej. "el patrón X aparece en las
  combinaciones {lista}, no aparece en {lista}" — enumeración factual, sin ordenar esas listas por
  ninguna métrica de deseabilidad.
- **Comparar estabilidad de patrones observados**: ej. si un patrón aparece en el mismo subconjunto
  de condiciones en ambos períodos, o en subconjuntos distintos — descripción de consistencia
  factual, nunca calificada como "confiable" o "robusto" en sentido evaluativo.
- **Trazabilidad hacia atrás**: desde una observación descrita hasta las carpetas/comparaciones
  concretas del manifiesto que la sostienen — extensión directa del campo `CarpetaOrigen`/
  `CarpetasOrigen` ya presente en `analisis_corpus/` (Capa 2), sin cambio de principio.

### Qué análisis quedan prohibidos

- **Recomendar gestor o estrategia**, bajo cualquier nombre — permanece excluido por D-118 de forma
  permanente, reafirmado aquí sin cambios.
- **Seleccionar una configuración** como punto de partida sugerido — misma exclusión que D-118 ya
  aplica a "sugerir candidatos" hasta que una decisión futura explícita lo autorice.
- **Puntuar alternativas**: ningún cálculo que combine 2+ métricas en un número único, ni ningún
  campo que ordene combinaciones por deseabilidad — mismo principio D-014/D-025/D-026/D-047/D-076/
  D-118, reafirmado.
- **Inferir comportamiento futuro**: toda observación de esta capa describe evidencia histórica ya
  ocurrida — ninguna salida puede formularse como proyección, expectativa, o probabilidad de
  comportamiento futuro (D-016 extendido, mismo criterio ya fijado para D-120 §"Limitaciones").
- **Crear reglas operativas**: ninguna salida de esta capa puede tomar la forma de "si ocurre X,
  entonces usar Y" — eso sería una regla de decisión disfrazada de observación.
- **Extrapolar fuera de BTCUSDT**: ninguna relación/agrupación puede presentarse como válida más
  allá del instrumento único que el corpus contiene — mismo límite ya aplicado en D-123.

### Restricciones que aplican

- D-118, D-119, D-120 permanecen **intactas, sin modificación** — esta decisión no las reabre, las
  hereda como marco que el análisis interpretativo limitado debe seguir respetando, igual que Capa 2
  lo hizo.
- D-121 (atribución causal, rango original si se retoma Vía A) permanece vigente para cuando la Vía
  A se formalice — no se resuelve ni se modifica en esta decisión.
- El análisis interpretativo limitado consume el corpus vía el mismo manifiesto
  (`MANIFIESTO_CORPUS_CASO5C_V1.json`) y, cuando corresponda, la infraestructura ya construida de
  Capa 2 (`analisis_corpus/LectorCorpus`) — no ejecuta ningún backtest nuevo, mismo principio D-123
  §2 ("ninguna ejecución nueva como parte del análisis").
- Ninguna salida puede omitir su propia declaración de límites — extensión del principio ya usado en
  `ResumenCorpus.Limitaciones` (D-123), ahora obligatorio también para cualquier relación/agrupación
  que esta nueva capa produzca.
- La Vía A (diversidad de instrumento) sigue disponible como paso futuro independiente — esta
  decisión no la descarta ni la condiciona a que el análisis interpretativo limitado se complete
  primero.

### Evidencia

- `PROPUESTA_EVOLUCION_POST_CAPA2_V1.md` §2-§6: estado de madurez del proyecto tras Capa 2, qué
  demuestra y qué no demuestra el corpus actual, y las 2 vías con sus a favor/en contra.
- `RESULTADO_ANALISIS_CORPUS_CASO5C_CAPA2_V1.md`: confirma que el corpus ya sostiene observaciones
  descriptivas de valor (patrones replicados en 2 períodos) — base fáctica para considerar que hay
  margen de interpretación adicional sin necesidad de más datos.
- Decisión explícita del auditor: "El sistema ya tiene una cantidad suficiente de infraestructura y
  evidencia para extraer conocimiento descriptivo adicional. Continuar aumentando corpus antes de
  aprender nada del corpus existente podría repetir un ciclo infinito de acumulación" — incorporada
  aquí como razón central.
- D-118 (`DECISIONES_CASO5C_V1.md`): exclusión permanente de selección automática, reafirmada sin
  cambios como límite que esta decisión no toca.
- D-123 (`DECISIONES_CASO5C_CAPA2_V1.md`): precedente directo de la disciplina "permitido/prohibido"
  aplicada aquí con el mismo formato.

---

## Fuera de alcance de este documento

No se especifica la implementación del análisis interpretativo limitado (queda para
`ESPECIFICACION_IMPLEMENTACION_ANALISIS_INTERPRETATIVO_CASO5C_V1.md` o nombre equivalente, documento
posterior). No se decide si/cuándo se formaliza la Vía A. No se activa ninguna forma de
recomendación (D-118/D-119/D-120 siguen en estado de principio, sin implementación). No se calcula
ningún análisis real en este documento.

---

## Próximo documento

`ESPECIFICACION_IMPLEMENTACION_ANALISIS_INTERPRETATIVO_CASO5C_V1.md` (o nombre equivalente) —
diseño concreto del componente de análisis interpretativo limitado (qué cruces exactos, sobre qué
estructura de datos, con qué formato de salida y qué declaración de límites obligatoria), sujeto a
los límites de "permitido"/"prohibido" fijados en D-124.
