# Decisiones — Diversidad de Evidencia (Caso 5C)

Estado: **D-121 resuelta**. Misma estructura usada en D-001 a D-120 (decisión, opciones, criterio,
evidencia, resolución). Ningún código se modifica en este documento — la resolución aquí registrada
habilita la especificación de implementación siguiente, no la reemplaza. No se descarga ningún
dato, no se modifica `campana_corpus/`, no se ejecuta ninguna comparación nueva.

Contexto completo en `PROPUESTA_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md`. Verificación contra código
existente, no reconstruida de memoria (mismo criterio que abrió toda fase anterior, D-057).

---

## D-121 — Qué dimensión de diversidad de evidencia incorporar primero

**Estado**: 🟢 Aprobada. **Selección: B — diversidad temporal primero.**

**Decisión**: `AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V2.md` §5 estableció que la limitación de
diversidad de dataset/instrumento no es resoluble con más campañas sobre `BTCUSDT`. Existen 2
dimensiones distintas para cerrarla — instrumento y tiempo — y ninguna evidencia todavía indica
cuál importa más. Esta decisión fija cuál se incorpora primero, o si ambas se incorporan juntas.

### Opciones

- **A — Instrumento primero** (`PROPUESTA_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md` §3): descargar un
  segundo símbolo real (ej. `ETHUSDT`), mismo rango temporal que el dataset actual.
  - Ventaja: responde directamente si el perfil relativo entre gestores depende de la estructura
    del instrumento — la pregunta de mayor interés declarado para Capa 2 eventual (¿el mecanismo de
    comparación generaliza más allá de un activo?).
  - Riesgo confirmado en la propuesta (§3): introduce preguntas nuevas no verificadas contra código
    — disponibilidad completa del histórico en el rango elegido, y si la escala de
    precio/volumen de un instrumento distinto exige reconsiderar `TasaMargen`/costes ya congelados.
    Si el histórico de `ETHUSDT` (o el símbolo elegido) tuviera huecos que `ValidadorIntegridadDatos`
    rechace, o si la escala de precio alterara el comportamiento de `GestorFixedRisk`/
    `GestorVolatilitySizing` de forma que no sea comparable sin ajuste, el resultado observado
    mezclaría dos causas posibles (cambio de activo real vs. artefacto de escala/calidad de datos)
    sin forma de distinguirlas con una sola descarga.

- **B — Tiempo primero** (§4): descargar un segundo rango temporal del mismo `BTCUSDT` (ej.
  `2023-01-01`–`2024-01-01`), mismo pipeline, mismo instrumento.
  - Ventaja: es la misma descarga ya ejecutada una vez (`datos_reales/Program.cs:34-39`), sin
    ninguna pregunta nueva de compatibilidad de formato o escala — el símbolo, la fuente, el
    validador y el proceso de congelación ya están probados contra `BTCUSDT` específicamente.
  - Riesgo confirmado en la propuesta (§4): no responde si un patrón depende de la estructura del
    instrumento — solo de estabilidad temporal.

- **C — Ambas dimensiones con matriz definida** (§5): instrumento nuevo + rango nuevo en la misma
  ronda.
  - Ventaja: mayor cobertura de una sola vez.
  - Riesgo confirmado en la propuesta (§5) y ampliado por precisión del auditor en la revisión: si
    se cambian instrumento y período simultáneamente y el perfil relativo entre gestores cambia
    respecto al corpus actual, **no hay forma de atribuir ese cambio a una sola causa** — podría
    deberse al instrumento, al período, o a la interacción de ambos. Esto degradaría la calidad de
    la conclusión que la propia expansión busca producir, incluso con más volumen de datos.

### Criterio decisivo — capacidad de atribución causal

Precisión explícita del auditor, incorporada aquí como criterio central de esta decisión, no solo
como nota: **cobertura no es el único criterio — importa también qué tan aislado queda cada eje de
variación**. Mismo principio que ya rige `ComparadorGestores` desde D-113 (control experimental por
construcción: la única variable que cambia entre corridas comparadas debe ser conocida) aplicado
ahora a nivel de campaña de adquisición de datos, no de comparación de gestores.

Con un solo dataset nuevo variando en una sola dimensión (Opción A o B), cualquier diferencia
observada respecto al corpus actual (6 estrategias × 3 timeframes × 3 gestores sobre
`BTCUSDT 2024`) es atribuible sin ambigüedad a esa dimensión. Con la Opción C, una diferencia
observada podría deberse a cualquiera de las dos, o a ambas de forma no separable — exactamente el
mismo tipo de riesgo que D-113 ya evitó al forzar que `ComparadorGestores` varíe un único eje
(gestor) por comparación.

### Resolución adoptada

**Selección: B — diversidad temporal primero.**

**Por qué no A todavía**: introduce 2 preguntas no verificadas (disponibilidad completa del
histórico del nuevo símbolo, compatibilidad de escala económica) que solo se resuelven ejecutando
la descarga — si cualquiera de las dos resultara problemática, el resultado de esa primera
expansión quedaría contaminado por una causa distinta a "diversidad de instrumento". La Vía B no
tiene ese riesgo, porque reutiliza exactamente el mismo símbolo/pipeline/escala ya validados.

**Por qué no C todavía**: por el criterio de atribución causal (ver arriba) — variar 2 dimensiones
a la vez en la primera expansión posterior a Caso 5C V2 arriesga producir un corpus más grande pero
menos interpretable, exactamente el tipo de compromiso que la disciplina de este proyecto ya evitó
en decisiones anteriores (D-113, D-030).

**Secuencia resultante, explícita**: Vía B (tiempo) primero, con su propia auditoría de corpus
posterior. Si esa auditoría encuentra que los patrones son estables entre períodos (o que no lo
son, lo cual también es información válida), **eso no descarta la Vía A** — la decisión de abrir
Instrumento como una fase siguiente, después de tener resultados de Tiempo, queda pendiente de una
futura propuesta, no de esta decisión. C sigue sin descartarse de forma permanente, solo se pospone
hasta tener, como mínimo, una dimensión de diversidad aislada ya evaluada.

### Cómo se mantiene la trazabilidad

Sin mecanismo nuevo — el pipeline ya construido en `datos_reales/`/`PLAN_FASE2A.md` §6 ya provee
la separación completa (`PROPUESTA_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md` §2): CSV crudo + metadata con
SHA-256 → validación de integridad → promoción manual explícita → `datasets/reales/` inmutable →
agregación a timeframes con su propio `metadata.json` (`sourceSha256` + `sha256` propio). El nuevo
rango temporal de `BTCUSDT` sigue exactamente este mismo camino, sin ninguna extensión. La campaña
(`campana_corpus/`) seguiría leyendo únicamente del dataset ya congelado, igual que hoy —
`ComparadorGestores`/`PersistidorComparaciones` no requieren ningún cambio de contrato.

### Qué datasets se congelan

**No se decide el rango exacto en este documento** — queda para la especificación de
implementación siguiente (ej. qué año concreto, cuántos timeframes agregar del nuevo rango: los
mismos 3 ya usados en campaña, `15m`/`1h`/`1D`, o el conjunto completo de 13). Lo que sí queda
fijado aquí es la naturaleza del dataset: mismo símbolo (`BTCUSDT`), mismo pipeline, un rango
temporal distinto al ya congelado (`2024-01-02`–`2025-01-02`).

### Qué campañas se ejecutan

**No se ejecuta ninguna en este documento.** Tras la descarga/validación/congelación del nuevo
rango, una campaña de estructura equivalente a V1/V2 (mismas 6 estrategias, mismos 3 gestores,
mismos 3 timeframes ya usados, ahora sobre el dataset nuevo) sería el candidato natural — a
confirmar en la especificación de implementación siguiente, no aquí. Esa campaña seguiría el mismo
principio rector de V1/V2: generación de evidencia, no optimización; matriz fija declarada antes de
ejecutar; sin selección por resultado.

### Restricciones que aplican

- Reafirmadas de `PROPUESTA_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md` §7, sin relajar: ningún parámetro
  económico se recalibra observando el nuevo dataset (D-030); congelación manual, no automática;
  no se generan datasets sintéticos; ningún baseline congelado se toca; `ComparadorGestores`/
  `PersistidorComparaciones`/`RenderizadorComparacionGestores` sin modificación.
- Nueva, derivada del criterio de atribución causal: **la Vía A (instrumento), si se abre en el
  futuro, debe ejecutarse variando solo esa dimensión respecto al corpus ya existente** (mismo
  rango temporal que el dataset original, no el nuevo rango de la Vía B) — para preservar la
  posibilidad de atribuir cualquier diferencia observada a una sola causa, no a la combinación de
  ambas expansiones.

### Evidencia

- `AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V2.md` §5: origen de la brecha, ya confirmó que no es
  resoluble con más campañas sobre el dataset actual.
- `PROPUESTA_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md` §2-§5: evidencia del pipeline existente y
  planteamiento de las 3 vías, sin preselección.
- `datos_reales/Program.cs:34-39`, `PLAN_FASE2A.md` §6: pipeline y separación
  descarga/congelación ya verificados contra código, reutilizados sin cambio para la Vía B.
- Precisión explícita del auditor en la revisión de la propuesta: capacidad de atribución causal
  como criterio adicional para D-121 — incorporada aquí como razón central de la resolución, no
  como nota externa.
- D-113 (`DECISIONES_CASO5B_V1.md`): precedente directo del mismo principio (aislar una única
  variable de cambio) aplicado a nivel de comparación de gestores, extendido aquí a nivel de
  campaña de adquisición de datos.

---

## Fuera de alcance de este documento

No se implementó código. No se descargó ningún dato. No se modifica `campana_corpus/`,
`ComparadorGestores`, `PersistidorComparaciones`, ni `datos_reales/`. No se fija el rango exacto de
la Vía B ni el símbolo de una futura Vía A — ambos quedan para especificaciones de implementación
posteriores. No se ejecuta ninguna comparación nueva.

---

## Próximo documento

Una especificación de implementación para la Vía B (diversidad temporal): rango exacto a descargar,
confirmación de que `datos_reales/Program.cs` no requiere cambios más allá de las 2 constantes de
símbolo/rango (`symbol` ya es `BTCUSDT`, solo cambia el rango), pasos de validación/congelación, y
solo entonces la descarga real — antes de definir la campaña que consumiría el dataset ampliado.
