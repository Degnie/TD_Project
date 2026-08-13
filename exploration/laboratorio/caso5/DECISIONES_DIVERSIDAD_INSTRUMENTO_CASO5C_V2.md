# Decisiones — Diversidad de Instrumento V2 (Retomar Vía A de D-121)

Estado: **D-125 resuelta**. Misma estructura usada en D-001 a D-124 (decisión, opciones, criterio,
restricciones, evidencia). Resuelve la pregunta abierta por `PROPUESTA_DIVERSIDAD_INSTRUMENTO_
CASO5C_V2.md` §6: cuál de las 3 opciones (A — abrir diversidad de instrumento, B — congelar la fase
actual, C — nueva capacidad analítica) define el siguiente paso de Caso 5C, y bajo qué alcance
exacto si es Opción A.

---

## D-125 — Apertura de diversidad de instrumento (fase de adquisición y validación, no de
recomendación)

**Estado**: 🟢 Aprobada.

**Decisión**: cuál de las 3 opciones no descartadas de `PROPUESTA_DIVERSIDAD_INSTRUMENTO_
CASO5C_V2.md` §6 se ejecuta a continuación, y qué queda explícitamente permitido/prohibido si es
Opción A.

### Resolución adoptada

**Opción A — abrir diversidad de instrumento**, retomando la Vía A pospuesta desde D-121, pero
**únicamente como fase de adquisición y validación de evidencia** — no como paso hacia
recomendación. Esta decisión no activa D-118/D-119/D-120, no acerca su activación, y no presupone
que el resultado de esta fase determine si se abren en el futuro.

**Razón central**: la limitación de instrumento único (`BTCUSDT` en las 49 comparaciones oficiales,
en los 3 niveles de análisis ya implementados — Capa 1, Capa 2, análisis interpretativo D-124) sigue
siendo la más citada en el historial completo de auditorías de Caso 5C, y es la única que ha
sobrevivido intacta a 2 expansiones de corpus (V1→V2) y 1 expansión de dimensión (tiempo, D-121/
D-122). A diferencia de una expansión arbitraria, `ETHUSDT` no es una ampliación sin propósito —
responde directamente a una limitación identificada y documentada desde el principio de la fase de
diversidad de evidencia.

**Disciplina de secuencia, explícita y no negociable**:

```
Nuevo instrumento
        ↓
Exploración de disponibilidad
        ↓
Dataset congelado
        ↓
Campaña comparable
        ↓
Auditoría
```

**Nunca**:

```
Nuevo instrumento
        ↓
buscar confirmación de patrones existentes
```

La segunda secuencia invertiría la disciplina experimental ya establecida: el instrumento se elige
y se valida por disponibilidad de datos reales, nunca por si sus resultados confirman o refutan lo
ya observado en `BTCUSDT`. Ningún patrón detectado en el corpus actual (degeneración de
`fixed-fractional`, ausencia de actividad de ZScore Reversion) puede influir en si `ETHUSDT` se
acepta o se descarta como instrumento candidato.

### Qué queda permitido

- **Incorporar `ETHUSDT` (u otro instrumento) si pasa validación**: mismo mecanismo ya usado 2 veces
  (`ValidadorIntegridadDatos`, rechazo estricto sin relleno automático) — el instrumento se congela
  solo si el histórico completo en el rango temporal exigido (2024-01-02–2025-01-02, D-121) pasa la
  validación, exactamente igual que el dataset 2022-2023 pasó y el 2023 (rango, no instrumento) fue
  rechazado.
- **Repetir la matriz experimental existente**: 6 estrategias × 3 timeframes × 3 gestores (18
  comparaciones, mismo tamaño que Sub-campaña D) — ninguna estrategia/gestor nuevo, ningún parámetro
  nuevo, mismo patrón ya usado para diversidad temporal.
- **Comparar presencia/ausencia de patrones**: usando la infraestructura ya construida
  (`analisis_corpus/`, `analisis_interpretativo/`) sobre el corpus ampliado — sin definir ningún
  patrón nuevo en esta decisión, los mismos ya nombrados (`DrawdownMaximoPct>=99%`, `SinActividad`).
- **Ampliar el corpus oficial**: de 49 a ~67 comparaciones (49 + 18), declaradas en el manifiesto
  por el mismo mecanismo de inspección de contenido ya usado 2 veces — nunca por timestamp.

### Qué queda prohibido

- **Elegir instrumento por resultados esperados**: la selección de `ETHUSDT` (o cualquier
  alternativa, si `ETHUSDT` no pasa la exploración de disponibilidad) se basa exclusivamente en
  disponibilidad de datos reales verificable — nunca en una expectativa de qué resultado produciría.
- **Descartar un instrumento por resultados desfavorables**: si el instrumento elegido pasa
  validación de integridad de datos, su corpus se congela y se incluye — no existe un criterio de
  "resultado desfavorable" que justifique descartar un instrumento ya validado. La única causa de
  rechazo es integridad de datos (huecos, duplicados, errores estructurales), mismo criterio que
  rechazó el rango 2023.
- **Modificar parámetros económicos**: `TasaMargen=0.1m`, costes `0.001m`/`0.001m` permanecen sin
  cambio — si una diferencia de escala de precio/volumen entre instrumentos hiciera necesario
  ajustarlos, eso requeriría una decisión aparte, explícita, nunca una calibración silenciosa
  observando resultados (D-030).
- **Crear criterios de selección**: ningún mecanismo de esta fase puede producir un criterio que
  ordene, puntúe, o prefiera un instrumento/gestor/estrategia sobre otro — mismas salvaguardas ya
  verificadas por prueba en Capa 2 (P5) y en el análisis interpretativo (P5/P6), que la
  implementación de esta fase debe heredar sin excepción.

### Restricciones que aplican

- **D-121**: el rango temporal debe ser el original (`2024-01-02`–`2025-01-02`), no el rango
  2022-2023 — preserva la capacidad de atribución causal (varía instrumento, no varía tiempo a la
  vez). Sin esto, cualquier diferencia observada entre `BTCUSDT` y el nuevo instrumento sería
  inatribuible a una sola dimensión.
- **D-122**: mismo mecanismo de exploración de disponibilidad por bloques mensuales
  (`ExploradorDisponibilidad`, ya genérico por `symbol`) antes de comprometerse a una descarga
  completa — no se asume viabilidad de `ETHUSDT` sin explorar primero.
- **D-030**: ningún parámetro se calibra observando el corpus nuevo.
- **D-118/D-119/D-120**: permanecen intactas — esta decisión no las activa, no las condiciona, no
  las acerca. La ampliación de corpus a ~67 comparaciones no es, por sí sola, un cambio en el
  estado de esas 3 decisiones.
- **Mismo criterio de manifiesto**: cualquier evidencia nueva se declara en
  `MANIFIESTO_CORPUS_CASO5C_V1.json` por inspección de contenido, no por timestamp — mismo mecanismo
  ya aplicado 2 veces (V1/V2, Sub-campaña D).

### Evidencia

- `PROPUESTA_DIVERSIDAD_INSTRUMENTO_CASO5C_V2.md` §2-§6: limitación exacta que esta vía ataca,
  pipeline reverificado contra código actual (`datos_reales/Program.cs:24`), instrumento candidato
  identificado sin confirmar viabilidad, y las 3 opciones con sus a favor/en contra.
- Decisión explícita del auditor: secuencia obligatoria "Nuevo instrumento → Exploración →
  Congelación → Campaña → Auditoría", nunca "Nuevo instrumento → buscar confirmación de patrones
  existentes" — incorporada aquí como restricción central, no como nota adicional.
- D-121 (`DECISIONES_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md`): precedente directo de la Vía A pospuesta,
  ahora retomada con las mismas condiciones que ya fijó (rango original si se abre después de la
  Vía B).
- D-122 (`DECISIONES_RANGO_ALTERNATIVO_CASO5C_V1.md`): precedente del mecanismo de exploración
  previa a congelación, reutilizado aquí sin modificación.

---

## Fuera de alcance de este documento

No se especifica la implementación de la exploración/descarga/campaña (queda para un documento de
especificación posterior). No se confirma si `ETHUSDT` pasa la exploración de disponibilidad — eso
es el primer paso técnico después de esta decisión, no algo que se resuelva aquí. No se activa
ninguna forma de recomendación (D-118/D-119/D-120 siguen en estado de principio). No se calcula
ningún análisis sobre corpus nuevo en este documento.

---

## Próximo documento

Especificación de implementación para la fase de diversidad de instrumento — probablemente
`ESPECIFICACION_IMPLEMENTACION_DIVERSIDAD_INSTRUMENTO_CASO5C_V2.md`, siguiendo el mismo patrón ya
usado en `ESPECIFICACION_IMPLEMENTACION_DIVERSIDAD_TEMPORAL_CASO5C_V1.md`: generalizar el pipeline
de descarga por instrumento (si hace falta algún ajuste más allá de la constante ya identificada),
exploración de disponibilidad sobre el instrumento candidato, y solo si resulta viable, descarga
completa → validación → congelación → campaña sobre la matriz de 18 comparaciones.
