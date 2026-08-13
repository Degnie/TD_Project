# Propuesta — Diversidad de Instrumento V2 (retomar Vía A de D-121)

Estado: **documento de apertura — previo a cualquier decisión, descarga, o modificación de
código**. Continúa `AUDITORIA_ANALISIS_INTERPRETATIVO_CASO5C_V1.md`, que dejó pendiente esta
evaluación explícita. Retoma la Vía A de `PROPUESTA_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md` (pospuesta
por D-121 en favor de la Vía B/tiempo, nunca descartada). No es una fase nueva del ciclo D-N
implementada — plantea la decisión que debe resolverse antes de tocar código o descargar datos.

**No se descarga ningún dato en este documento. No se modifica `datos_reales/`/`campana_corpus/`/
`analisis_corpus/`/`analisis_interpretativo/`. No se decide todavía si esta vía se abre.**

---

## 1. Dónde está el proyecto ahora

Con `AUDITORIA_ANALISIS_INTERPRETATIVO_CASO5C_V1.md` cerrada, Caso 5C cubre 3 niveles separados,
todos operando sobre el mismo corpus:

```
Evidencia (Capa 1)             — "qué ocurrió"
        ↓
Descripción (Capa 2)           — "qué contiene el corpus"
        ↓
Relaciones observadas (D-124)  — "qué patrones aparecen dentro del corpus"
```

Sin cruzar al cuarto nivel (decisión operativa — D-118/D-119/D-120 intactas). El corpus que
sostiene estos 3 niveles sigue siendo el mismo desde `HALLAZGO_DATASET_TEMPORAL_2022_CASO5C_V1.md`:
**49 comparaciones, 2 períodos temporales, 1 único instrumento (`BTCUSDT`)**.

**La pregunta de esta propuesta no es técnica** (ya se demostró, 2 veces, que el sistema puede
incorporar una nueva dimensión de evidencia sin romper nada — primero con tiempo, D-121/D-122). Es
de alcance: **¿conviene abrir la Vía A ahora, o el corpus actual (con sus 3 niveles ya
implementados) es un punto de parada razonable?**

---

## 2. La limitación exacta que Vía A atacaría

Repetida en las últimas 4 auditorías/documentos sin resolverse (`AUDITORIA_CORPUS_COMPARATIVO_
CASO5C_V2.md` §5, `AUDITORIA_DIVERSIDAD_TEMPORAL_CASO5C_V1.md` §4, `RESULTADO_ANALISIS_CORPUS_
CASO5C_CAPA2_V1.md` §4, `AUDITORIA_ANALISIS_INTERPRETATIVO_CASO5C_V1.md` §5 implícitamente): **todo
el corpus, sin excepción, es sobre `BTCUSDT`**. Ningún patrón detectado — ni los descriptivos de
Capa 2, ni las relaciones/consistencias del análisis interpretativo (D-124) — puede afirmarse válido
más allá de ese instrumento. Es la única limitación que ha sobrevivido intacta a través de 2
expansiones de corpus (V1→V2) y 1 expansión de dimensión (tiempo, D-121/D-122).

**Qué NO resolvería una tercera expansión sobre el mismo instrumento**: ya establecido en
`AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V2.md` §5 — más timeframes, más repeticiones, o más análisis
sobre `BTCUSDT` no dicen nada sobre si un patrón es específico de este activo o generalizable.

---

## 3. Evidencia existente del pipeline, verificada contra código (reconfirmado, no ha cambiado)

Mismo hallazgo que `PROPUESTA_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md` §2 documentó — reverificado aquí:

- `datos_reales/Program.cs:24`: `const string symbol = "BTCUSDT";` — sigue siendo el único cambio
  de código necesario para apuntar la descarga a otro instrumento. `BinanceClient`/
  `DescargadorVelas` no están hardcodeados a BTC más allá de esta constante.
- El patrón completo (exploración de disponibilidad → descarga → `ValidadorIntegridadDatos` →
  congelación → vista de compatibilidad si aplica → campaña → manifiesto) ya está probado **dos
  veces** con éxito (dataset 2024-2025 original, dataset 2022-2023 vía D-121/D-122) — no hay
  mecanismo nuevo que diseñar, solo repetirlo con instrumento en vez de tiempo.
- `ExploradorDisponibilidad` (creado para D-122) ya es genérico por `symbol`/`interval`, no
  necesita ningún cambio para explorar un instrumento distinto.

**Preguntas que esta vía introduciría, sin resolver aquí** (mismas que V1 de esta propuesta ya
anticipó, siguen abiertas):
- Disponibilidad del histórico completo en Binance para el instrumento candidato, en el mismo rango
  temporal que D-121 exige preservar (2024-01-02–2025-01-02, para no variar 2 dimensiones a la vez).
- Diferencias de escala de precio/volumen entre instrumentos — si `TasaMargen=0.1m`/costes
  `0.001m`/`0.001m` (ya congelados) siguen siendo válidos sin recalibrar, o si eso requeriría una
  decisión aparte (D-030: nunca calibrar observando resultados).

---

## 4. Instrumento candidato (no decidido, solo identificado)

`ETHUSDT` fue el ejemplo concreto que `PROPUESTA_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md` §3 ya usó —
mismo pipeline (Binance Spot), suficiente liquidez/historial esperado para no repetir el rechazo que
sufrió el rango 2023 por huecos de datos. **No se confirma aquí si es viable** — igual que D-122 no
asumió que 2022 fuera viable sin explorar primero, esta propuesta no asume que `ETHUSDT` lo sea. Si
se decide abrir Vía A, el primer paso técnico sería exploración de disponibilidad (mismo mecanismo
de D-122), no descarga directa.

---

## 5. Restricciones ya congeladas que aplican si se abre esta vía

- **D-121**: si Vía A se retoma, debe reusar el rango temporal original
  (`2024-01-02`–`2025-01-02`) — no el rango 2022-2023 — para preservar la capacidad de atribución
  causal (varía instrumento, no varía tiempo a la vez).
- **D-030**: ningún parámetro económico (`TasaMargen`, costes) se recalibra observando resultados
  del nuevo instrumento — se mantiene el valor ya congelado, o se abre una decisión aparte
  explícita si se determina que no aplica.
- **D-118/D-119/D-120**: siguen sin activarse — abrir Vía A no es, por sí solo, una condición
  suficiente para diseñar recomendación. Sigue siendo una decisión posterior, independiente.
- **Mismo criterio de manifiesto ya establecido** (`MANIFIESTO_CORPUS_CASO5C_V1.json`): cualquier
  corpus nuevo generado por esta vía debe declararse en el manifiesto por inspección de contenido
  (no por timestamp), separando evidencia oficial de reintentos de desarrollo — mismo patrón ya
  aplicado 2 veces.

---

## 6. Opciones

### Opción A — Abrir Vía A ahora

Retomar D-121, ejecutar el patrón ya probado (exploración → descarga → validación → congelación →
campaña sobre la matriz completa: 6 estrategias × 3 timeframes × 3 gestores = 18 comparaciones
nuevas, mismo tamaño que Sub-campaña D) con `ETHUSDT` (o el instrumento que la exploración confirme
viable), manteniendo el rango 2024-2025.

**A favor**: ataca la única limitación que ha sobrevivido intacta a 2 expansiones de corpus previas
y a la implementación completa de 2 capas de análisis — es, con diferencia, la limitación más
citada en todo el historial de auditorías de Caso 5C. Aumentaría el corpus a ~67 comparaciones (49 +
18), con la primera oportunidad real de saber si algún patrón detectado (degeneración de
`fixed-fractional` en timeframes cortos, ausencia de actividad de ZScore Reversion) es específico de
`BTCUSDT` o más general.

**En contra**: repite, por cuarta vez en el historial del proyecto, el patrón "antes de decidir algo
distinto, ampliemos evidencia" (V1→V2, diversidad temporal, y ahora esto) — patrón que ya se señaló
como riesgo en `PROPUESTA_CASO5C_CAPA2_V1.md` §4 y de nuevo en `PROPUESTA_EVOLUCION_POST_CAPA2_V1.md`
§4. La infraestructura analítica actual (Capa 2 + análisis interpretativo) todavía no fue usada
sobre ningún corpus más allá del que ya existe — no está demostrado que "más instrumento" sea el
cuello de botella real frente a "más capacidad de leer lo que ya hay", que fue justamente la
justificación para priorizar D-124 sobre esta misma vía la última vez que se comparó.

### Opción B — Mantener la fase actual congelada

No abrir ninguna vía nueva. El estado actual (infraestructura completa, corpus reproducible,
2 niveles de análisis sin cruzar a recomendación) se declara un punto de parada válido para el
estado experimental alcanzado — no como abandono, sino como cierre deliberado de esta fase del
proyecto, dejando ambas vías (instrumento, nueva capacidad analítica) documentadas y disponibles
para retomar en el futuro sin perder contexto.

**A favor**: evita seguir un patrón de expansión indefinida sin una necesidad concreta que lo
justifique — no hay ninguna pregunta pendiente ahora mismo que solo un nuevo instrumento pueda
responder (a diferencia de cuando se abrió D-121, donde la pregunta "¿el patrón es del instrumento
o del período?" ya estaba planteada explícitamente por el auditor).

**En contra**: la limitación de instrumento único sigue sin resolverse, y cuanto más tiempo pase sin
abordarla, más se acumula evidencia (interpretativa incluida) que sigue atada a un solo activo.

### Opción C — Nueva capacidad analítica (no diversidad de evidencia)

Explorar si existe alguna pregunta legítima que el corpus actual (49 comparaciones, 3 niveles ya
implementados) todavía no puede responder, sin cruzar a recomendación — ej. algo más allá de lo que
D-124 ya cubre. Solo sería defendible si esa pregunta cumple las 3 condiciones que el auditor ya
fijó: no ser una recomendación disfrazada, usar evidencia existente, tener salvaguarda clara contra
ranking/selección.

**A favor**: no depende de resolver la limitación de instrumento para tener valor, igual que D-124
no dependió de eso.

**En contra**: a diferencia de Vía A (que tiene un candidato concreto y verificado, `ETHUSDT`) y de
Vía A de D-124 (que tuvo una especificación completa con 4 capacidades concretas), esta opción no
tiene todavía ninguna pregunta específica identificada — abrir esta opción requeriría primero
encontrar esa pregunta, no solo decidir ejecutarla.

---

## Fuera de alcance de este documento

No se elige ninguna opción. No se descarga ningún dato. No se modifica ningún código. No se
confirma si `ETHUSDT` es viable (requeriría exploración de disponibilidad, mismo mecanismo de
D-122, solo si se elige Opción A). No se reabre D-118/D-119/D-120.

---

## Próximo documento

Depende de la decisión: si se elige Opción A, un documento de decisión (candidata D-125) fijando el
instrumento y confirmando el rango temporal a preservar, seguido de exploración de disponibilidad
(mismo patrón D-122) antes de cualquier descarga completa. Si se elige Opción B, un documento de
cierre declarando el estado actual como punto de parada de esta fase (sin implicar que el proyecto
termina, solo que esta línea de expansión se detiene deliberadamente). Si se elige Opción C, un
documento de propuesta específico una vez identificada la pregunta concreta que la justifica.
