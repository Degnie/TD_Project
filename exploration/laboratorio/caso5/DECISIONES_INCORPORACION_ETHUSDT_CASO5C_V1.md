# Decisiones — Incorporación de ETHUSDT al Corpus Oficial (Caso 5C)

Estado: **D-126 resuelta**. Misma estructura usada en D-001 a D-125 (decisión, opciones, criterio,
restricciones, evidencia). Resuelve la única pregunta dejada pendiente por
`AUDITORIA_SUBCAMPANA_E_CASO5C_V1.md` §7: si las 18 comparaciones `ETHUSDT` auditadas pasan a
formar parte del corpus oficial declarado en `MANIFIESTO_CORPUS_CASO5C_V1.json`. **Decisión
administrativa/experimental — no evalúa ni resuelve qué instrumento, gestor, o estrategia es
preferible.**

---

## D-126 — Incorporación de la Sub-campaña E al corpus oficial declarado

**Estado**: 🟢 Aprobada.

**Decisión**: ¿la evidencia auditada de Sub-campaña E (18 comparaciones `ETHUSDT`, `AUDITORIA_
SUBCAMPANA_E_CASO5C_V1.md`) forma parte del corpus declarado en `MANIFIESTO_CORPUS_CASO5C_V1.json`?
No se decide, y queda explícitamente fuera de esta resolución, cuál instrumento produce mejores
resultados, qué gestor usar, o qué estrategia elegir — esas preguntas no tienen lugar en esta
decisión, ni en ninguna de las 125 decisiones previas del proyecto (D-118/D-119/D-120 permanecen
intactas).

### Resolución adoptada

**Sí — la Sub-campaña E se incorpora al corpus oficial.** Las 18 comparaciones `ETHUSDT` pasan a
declararse en `comparaciones[]` del manifiesto, bajo un nuevo `origen: "SubcampanaE"`, siguiendo
exactamente el mismo mecanismo ya usado 3 veces (V1, V2, SubcampanaD) — sin alterar ninguna
comparación ya declarada, sin recalcular nada, sin tocar ningún componente de Capa 1/Capa 2/
análisis interpretativo.

**Criterio de admisión aplicado** (administrativo, no basado en el contenido de los resultados):

1. **Cobertura completa**: 6×3=18 combinaciones, 3/3 gestores Success en cada una — sin huecos,
   sin escritura interrumpida (verificado en `AUDITORIA_SUBCAMPANA_E_CASO5C_V1.md` §1).
2. **Identidad experimental correcta**: instrumento como única dimensión variada frente a la matriz
   `BTCUSDT` 2024-2025 ya oficial, configuración económica idéntica, verificado por hash (§2 de la
   auditoría).
3. **Reproducibilidad verificada**: mismo `HashCompuesto` entre 2 ejecuciones consecutivas (§3 de
   la auditoría).
4. **Trazabilidad completa**: cada una de las 18 carpetas tiene `IDENTIDAD_COMPARACION.json` y
   `COMPARACION_GESTORES_V1.md` consistentes, verificado por inspección directa de contenido, no
   por conteo del log de ejecución (mismo criterio riguroso ya aplicado a las 88 carpetas de la
   clasificación previa).

**Ninguno de estos 4 criterios depende de qué dice el resultado** — los mismos 4 se habrían exigido
igual si `ETHUSDT` hubiera mostrado menos drawdown extremo que `BTCUSDT`, más `SinActividad`, o
cualquier otro patrón. Esto es lo que distingue esta decisión de una selección por resultado,
prohibida explícitamente por D-125.

### Qué queda permitido

- **Actualizar `MANIFIESTO_CORPUS_CASO5C_V1.json`**: `totalOficial` pasa de 49 a **67**;
  `comparaciones[]` se extiende con las 18 carpetas de Sub-campaña E (`origen: "SubcampanaE"`);
  `excluidos.categorias[]` se extiende con las categorías ya propuestas en `CLASIFICACION_
  PROPUESTA_CARPETAS_SUBCAMPANA_E_V1.md` (`repeticion-tecnica-subcampana-e`: 68;
  `escritura-interrumpida`: 8→10).
- **Usar la infraestructura de Capa 2/análisis interpretativo (`LectorCorpus`,
  `AnalisisDescriptivo`, `DetectorRelaciones`) sobre el corpus ampliado**, una vez el manifiesto
  esté actualizado — sin necesidad de ningún cambio de código en esos componentes (ya son agnósticos
  del instrumento, verificado en `ESPECIFICACION_IMPLEMENTACION_DIVERSIDAD_INSTRUMENTO_CASO5C_
  V2.md` §4).
- **Actualizar el texto fijo de `Limitaciones`** en `ProgramAnalisisCorpus.cs`/
  `ProgramAnalisisInterpretativo.cs` (hoy afirma que el corpus es exclusivamente `BTCUSDT`) para
  reflejar la nueva composición — cambio de texto descriptivo, no de lógica.

### Qué queda prohibido

- **Ninguna conclusión sobre qué instrumento es preferible** en este documento ni como consecuencia
  de él — la incorporación es administrativa, no evaluativa.
- **Ninguna forma de ranking, selección, ni recomendación** se activa por esta decisión —
  D-118/D-119/D-120 permanecen exactamente en el mismo estado que antes de D-126.
- **Ningún análisis nuevo se ejecuta en este documento** — la actualización del manifiesto (si se
  autoriza como paso siguiente) es un cambio de datos declarados, no un análisis; cualquier lectura
  del corpus ampliado (Capa 2, interpretativo) requeriría su propia ejecución posterior, separada de
  esta decisión.
- **Ninguna comparación instrumento-vs-instrumento reducida a un solo número o veredicto** — las
  observaciones descriptivas ya documentadas en la auditoría (§4) permanecen como están: factuales,
  sin conversión a puntuación.

### Restricciones que aplican

- **D-125**: la incorporación se basa exclusivamente en los 4 criterios administrativos de arriba,
  nunca en si el resultado de `ETHUSDT` "confirma" o "contradice" patrones ya vistos en `BTCUSDT`.
- **D-030**: ningún parámetro se recalibra como consecuencia de esta incorporación.
- **D-118/D-119/D-120**: intactas — la ampliación del corpus a 67 comparaciones no es, por sí sola,
  una condición hacia su activación.
- **Mismo criterio de manifiesto por inspección de contenido**: la actualización debe seguir
  exactamente el patrón ya usado 3 veces — nunca por timestamp, nunca automático.

### Evidencia

- `AUDITORIA_SUBCAMPANA_E_CASO5C_V1.md` §1-§3: cobertura, identidad experimental, y
  reproducibilidad verificadas — base de los 4 criterios de admisión.
- `CLASIFICACION_PROPUESTA_CARPETAS_SUBCAMPANA_E_V1.md`: clasificación previa que separó las 18
  comparaciones `ETHUSDT` de las 68 de repetición técnica y las 2 de escritura interrumpida,
  condición necesaria para que esta decisión supiera exactamente qué 18 carpetas incorporar.
- Decisión explícita del auditor: "La decisión es administrativa/experimental: ¿esta evidencia
  forma parte del corpus declarado? No: ¿qué evidencia gana?" — incorporada aquí como el criterio
  central de esta resolución.

---

## Fuera de alcance de este documento

No se modifica todavía `MANIFIESTO_CORPUS_CASO5C_V1.json` — esta decisión autoriza el criterio de
incorporación, la actualización mecánica del manifiesto es un paso de implementación posterior. No
se ejecuta ningún análisis sobre el corpus ampliado. No se activa D-118/D-119/D-120. No se decide
qué instrumento, gestor, o estrategia es preferible.

---

## Próximo documento

Actualización mecánica de `MANIFIESTO_CORPUS_CASO5C_V1.json` (totalOficial 49→67, `comparaciones[]`
+18 con `origen: "SubcampanaE"`, `excluidos.categorias[]` con las 2 categorías propuestas) —
siguiendo el mismo patrón ya usado 3 veces, sujeta a autorización explícita separada de esta
decisión.
