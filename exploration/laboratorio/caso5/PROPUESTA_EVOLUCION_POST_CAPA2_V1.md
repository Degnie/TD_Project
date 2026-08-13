# Propuesta — Evolución Post-Capa 2 (previo a decidir la próxima dirección de Caso 5C)

Estado: **documento de apertura — previo a cualquier decisión, implementación, o ejecución**.
Continúa directamente `RESULTADO_ANALISIS_CORPUS_CASO5C_CAPA2_V1.md`, que dejó pendiente esta
evaluación explícita. No es una fase nueva del ciclo D-N implementada — plantea la decisión que
debe resolverse antes de tocar código o escribir una especificación.

**No se elige todavía entre las opciones planteadas. No se implementa ningún componente. No se
descarga ningún dato nuevo.**

---

## 1. Dónde está el proyecto ahora

Con `RESULTADO_ANALISIS_CORPUS_CASO5C_CAPA2_V1.md` cerrado, la cadena completa queda demostrada de
punta a punta, con verificación mecánica en cada eslabón:

```
Datos congelados (ValidadorIntegridadDatos, SHA-256)
        ↓
Ejecución reproducible (EjecutorProtocolo, HashCompuesto)
        ↓
Comparación estructurada entre gestores (Caso 5B)
        ↓
Persistencia de evidencia (Caso 5C Capa 1)
        ↓
Comparación entre períodos temporales (Sub-campaña D)
        ↓
Corpus oficial declarado, separado de evidencia técnica (manifiesto)
        ↓
Análisis descriptivo del corpus completo (Caso 5C Capa 2)
```

**Lo que ya no es la pregunta**: si el sistema puede generar, persistir, distinguir y describir
evidencia comparativa de forma reproducible. Eso está demostrado, con verificación directa (no solo
por diseño) en cada capa.

**Lo que sí es la pregunta**: qué se construye a continuación — más evidencia (otra dimensión
experimental) o más capacidad de leer la evidencia ya existente (interpretación, todavía sin cruzar
a recomendación).

---

## 2. Qué demuestra el corpus actual, y qué no

**Demuestra** (verificado en `RESULTADO_ANALISIS_CORPUS_CASO5C_CAPA2_V1.md`):
- Cobertura completa y simétrica: 6 estrategias × 3 timeframes × 3 gestores, sin combinación
  faltante en la matriz declarada.
- Patrones que se repiten en ambos períodos disponibles (degeneración de `fixed-fractional` en
  timeframes cortos, ausencia de actividad de ZScore Reversion) — replicados, no solo observados
  una vez.
- Un caso de evidencia parcial (`Estado: Incomplete`) correctamente representado y distinguido de
  una corrida degradada.

**No demuestra**:
- Si esos patrones son propios de `BTCUSDT` o generalizables a otro instrumento — el corpus tiene
  un único instrumento, sin ninguna excepción.
- Si 2 períodos son suficientes para hablar de "estabilidad temporal" en un sentido más fuerte que
  "se repitió en las 2 muestras disponibles" — 2 puntos no son una serie (ya señalado en
  `AUDITORIA_DIVERSIDAD_TEMPORAL_CASO5C_V1.md` §4).
- Ninguna afirmación de superioridad, robustez general, ni base para recomendación — D-118/D-119/
  D-120 siguen sin activarse, y nada en este corpus cambia esa situación por sí solo.

---

## 3. Restricciones ya congeladas que cualquier opción debe respetar

No se reabren aquí — aplican a ambas vías sin excepción:

- **D-118**: selección automática de gestor excluida de forma permanente y no condicional a la
  evidencia disponible — es un límite de rol del sistema, no de madurez del corpus.
- **D-119**: sin evidencia suficiente, el sistema no recomienda — y ninguna de las 2 vías de esta
  propuesta, por sí sola, resuelve qué cuenta como "suficiente" (ese umbral sigue diferido, D-030).
- **D-120**: cualquier salida que se acerque a recomendación requiere `CriterioUsado` +
  `EvidenciaUsada` + `Limitaciones` explícitas — sigue sin implementarse, ninguna vía de esta
  propuesta la activa por sí misma.
- **D-121**: si se retoma la Vía A (instrumento), debe reusar el rango temporal original
  (2024-01-02–2025-01-02) para preservar la capacidad de atribución causal ya establecida — no el
  rango 2022-2023, que ya varió la dimensión temporal.
- **D-030**: ningún parámetro/umbral se calibra observando el corpus actual.

---

## 4. Vía A — Diversidad de instrumento

Retomar D-121 (pospuesta, no descartada): incorporar un segundo instrumento (ej. `ETHUSDT`),
manteniendo la misma matriz (6 estrategias × 3 timeframes × 3 gestores) y el rango temporal
original, siguiendo el mismo patrón ya aplicado a la diversidad temporal (exploración de
disponibilidad → descarga → validación → congelación → vista de compatibilidad si aplica →
campaña → manifiesto → auditoría/análisis).

**A favor**:
- Ataca directamente la limitación más severa y ya identificada 3 veces (auditoría V2, auditoría de
  diversidad temporal, resultado de Capa 2): el corpus completo depende de un único instrumento.
- El patrón de trabajo ya existe y está probado — no hay que diseñar un mecanismo nuevo, solo
  repetirlo con instrumento en vez de tiempo.
- Es la vía que D-121 ya dejó planteada como pendiente explícitamente.

**En contra**:
- No añade capacidad de interpretación — el corpus crecería (de 49 a potencialmente ~100
  comparaciones) sin que el sistema gane ninguna forma nueva de leer esa evidencia. El "análisis
  descriptivo" ya construido en Capa 2 seguiría siendo la única forma de consultarlo.
- Introduce otra variable experimental antes de haber usado a fondo la que ya existe — el corpus
  actual (2 períodos) no ha sido explotado más allá de un resumen descriptivo; no está claro que
  "más datos" sea el cuello de botella real en este momento.
- Repite, por tercera vez, el patrón "antes de decidir algo distinto, ampliemos evidencia primero"
  — ya señalado como riesgo en `PROPUESTA_CASO5C_CAPA2_V1.md` §4 al evaluar la Opción B de esa
  propuesta.

---

## 5. Vía B — Interpretación limitada del corpus actual

Construir una capa nueva (posterior a Capa 2 descriptiva, sin fusionarse con ella) que responda
preguntas de un nivel más específico que "qué contiene el corpus", pero estrictamente por debajo de
recomendación:

- "¿Qué comportamientos se repiten entre condiciones específicas?" (ej. cruzar estrategia ×
  timeframe × gestor de forma más fina que la agrupación de una sola dimensión que Capa 2 permite).
- "¿En qué condiciones aparece una distribución particular?" (ej. bajo qué combinaciones aparece
  consistentemente `DrawdownMaximoPct≥99%`, ya explorado parcialmente en §4 del resultado de Capa 2,
  pero sin una capa dedicada a esa pregunta).
- "¿Qué evidencia respalda una observación dada?" — trazabilidad hacia atrás desde un patrón
  descrito hasta las carpetas/comparaciones concretas que lo sostienen.

Explícitamente **sin llegar** a "elige este gestor" o "usa esta configuración" — la línea que D-118
ya trazó de forma permanente.

**A favor**:
- Usa el corpus ya disponible sin esperar más adquisición de datos — coherente con la razón que
  llevó a abrir Capa 2 en primer lugar (D-123: "esperar indefinidamente más datos... repetiría el
  ciclo de expansión sin extraer conocimiento del sistema").
- Explora una zona metodológica que todavía no existe en el proyecto: cruces multi-dimensionales
  explícitos y trazabilidad de evidencia hacia una observación — Capa 2 deliberadamente se limitó a
  una dimensión de agrupación por vez (`ESPECIFICACION_IMPLEMENTACION_CASO5C_CAPA2_V1.md` §3, para
  evitar sugerir "la mejor celda"); esta vía tendría que resolver cómo cruzar dimensiones sin caer
  en esa misma trampa.
- No depende de resolver la limitación de instrumento único para tener valor — puede ejecutarse
  sobre el corpus actual tal como está.

**En contra**:
- No resuelve la limitación estructural más citada en las últimas 3 auditorías (instrumento único)
  — cualquier interpretación construida seguirá siendo válida solo para `BTCUSDT`.
- Es la vía metodológicamente más delicada de las dos: cuanto más cerca esté una salida de
  responder "qué comportamientos se repiten en qué condiciones", más fácil es que, sin querer, se
  deslice hacia una forma de recomendación implícita — requeriría el mismo nivel de disciplina de
  salvaguardas estructurales que Capa 2 ya aplicó (sin campos "mejor", sin ordenamiento por valor),
  pero sobre un dominio de preguntas más rico y más difícil de acotar por adelantado.
- No hay un patrón previo en el proyecto exactamente equivalente a esta capa — a diferencia de la
  Vía A (que repite un patrón ya probado 2 veces: descarga → validación → congelación → campaña),
  esta vía requeriría diseño nuevo desde cero.

---

## 6. Lo que ambas vías tienen en común

Ninguna de las dos implementa selección/recomendación automática (D-118 la excluye
permanentemente, en cualquiera de las dos). Ninguna fija un umbral numérico de "suficiencia" sin
declararlo como punto de partida conservador (D-030). No son mutuamente excluyentes de forma
permanente — podría abrirse una y evaluar la otra después; la pregunta de esta propuesta es cuál
va primero, no cuál se descarta.

---

## Fuera de alcance de este documento

No se elige ninguna vía. No se implementa ningún componente. No se descarga ningún dato nuevo. No
se diseña todavía ningún mecanismo de cruce multi-dimensional ni de trazabilidad. No se reabre
D-118 (selección automática sigue excluida en cualquier escenario). No se fija ningún umbral de
suficiencia de evidencia (D-119).

---

## Próximo documento

Depende de la decisión: si se elige Vía A, un documento equivalente a
`PROPUESTA_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md` pero enfocado en instrumento (probablemente
`PROPUESTA_DIVERSIDAD_INSTRUMENTO_CASO5C_V1.md`, reutilizando el patrón de exploración de
disponibilidad antes de comprometerse a una descarga completa, D-122). Si se elige Vía B, una
decisión formal (D-124 candidata) fijando el alcance exacto de la nueva capa de interpretación
limitada, con la misma disciplina de "qué está permitido / qué está prohibido" ya aplicada en
D-123.
