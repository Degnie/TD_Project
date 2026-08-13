# Decisiones — Auditoría Integral Post Caso 5C (Fase 0 de Caso 6)

Estado: **D-127 resuelta**. Misma estructura usada en D-001 a D-126 (decisión, opciones, criterio,
restricciones, evidencia). Resuelve `PROPUESTA_AUDITORIA_INTEGRAL_POST_CASO5C_V1.md`: si se aprueba
la ejecución de la auditoría integral, con qué alcance vinculante, bajo qué reglas de ejecución, y
con qué criterio de cierre.

---

## D-127 — Aprobación de la Auditoría Integral Post Caso 5C (línea base previa a Caso 6)

**Estado**: 🟢 Aprobada.

**Decisión**: si se ejecuta la auditoría integral propuesta, y bajo qué alcance/reglas/criterio
exactos.

### Resolución adoptada

**Se aprueba ejecutar la Auditoría Integral Post Caso 5C**, con el alcance A-H, el criterio de
aprobación, y la restricción de ejecución exactamente como los fijó
`PROPUESTA_AUDITORIA_INTEGRAL_POST_CASO5C_V1.md` — sin ampliar ni reducir ese alcance en esta
decisión. Esta fase es la **línea base declarada** previa a cualquier diseño de recomendador —
condición necesaria, no suficiente, para abrir Caso 6 Fase 1.

**Esta decisión no resuelve** arquitectura del recomendador, semántica de recomendación, ni
criterios de decisión automática — esas preguntas pertenecen a una propuesta aparte
(`PROPUESTA_CASO6_RECOMENDADOR_V1.md`), posterior y condicionada al cierre aprobado de esta
auditoría.

### Alcance vinculante (A-H, heredado sin modificación de la propuesta)

- **A. Motor base**: compilación, pipeline principal, ausencia de regresiones, suite de producción,
  determinismo, consistencia de hashes de identidad, reproducibilidad de datasets congelados.
- **B. Estrategias**: las 6 estrategias congeladas cargan correctamente, parámetros congelados
  válidos, ausencia de dependencias ocultas.
- **C. Motor financiero**: cálculo de métricas, costes, capital, estados incompletos/fallidos.
- **D. Gestores de riesgo**: los 3 gestores intercambiables, mismo comportamiento con misma
  entrada, separación estrategia/gestor.
- **E. Comparador**: comparación multi-gestor, identidad experimental, ausencia de ranking/
  recomendación accidental.
- **F. Persistencia de evidencia**: escritura, lectura, manifiesto, corpus oficial.
- **G. Capa analítica**: descriptiva (cobertura/distribuciones/métricas) e interpretativa
  (relaciones/patrones/restricciones); confirmar ausencia de recomendación/selección/reglas
  operativas en ambas.
- **H. Datos**: los 3 datasets congelados (`BTCUSDT` 2024-2025, `BTCUSDT` 2022-2023, `ETHUSDT`
  2024-2025) — hashes, metadata, estructura.

### Criterio de cierre (heredado de §3 de la propuesta, sin modificación)

Para cada área A-H, aprobación binaria y verificable:

- **A-E**: la suite de tests correspondiente pasa en su totalidad (N/N), ejecutada en esta fase —
  no citada de memoria (D-057).
- **F**: manifiesto reconciliado con disco por conjunto (0 huérfanas).
- **G**: las 2 suites de análisis pasan en su totalidad, salvaguardas estructurales (P5/P6 de cada
  una) confirmadas.
- **H**: SHA-256 de cada dataset coincide con su `metadata.json` declarado.

**Ningún criterio depende del resultado financiero observado** — mismo principio administrativo de
D-126.

**Formato de resultado obligatorio** (`AUDITORIA_INTEGRAL_POST_CASO5C_V1.md`): tabla por área con
columnas Funciona correctamente (Sí/No), Regresión (Sí/No), Evidencia válida (Sí/No), Problemas
encontrados (lista), Requiere corrección (lista) — exactamente el formato ya solicitado en la
propuesta.

### Reglas de ejecución vinculantes (heredadas de §5 de la propuesta)

1. **Primera opción, orden obligatorio**: `dotnet test`, ejecutables con pruebas ya existentes
   (P1-Pn de cada `Tests*.cs`), verificaciones estructurales, inspección/lectura de artefactos
   existentes — ninguna de estas escribe a `caso5/resultados/`.
2. **Si una verificación requiere ejecución real que escriba evidencia**: el artefacto resultante
   debe quedar identificado en `caso6/auditoria_integral/ejecuciones_tecnicas/` (o equivalente),
   nunca mezclado con `caso5/resultados/`, nunca incorporado al manifiesto, corpus oficial, ni
   ningún análisis.
3. **No se aplica por defecto el patrón de clasificación de residuos de Caso 5C** — solo si resulta
   inevitable un residuo, se clasifica explícitamente; la regla por defecto es evitarlo.

### Qué queda prohibido en esta fase

- Diseñar o especificar el recomendador.
- Ejecutar campañas nuevas o generar comparaciones nuevas (corpus experimental adicional).
- Modificar cualquier componente de `src/` o `exploration/laboratorio/caso5/` como parte de la
  auditoría misma — si aparece un problema real, se reporta y se resuelve como corrección aparte,
  con su propio registro, antes de cerrar la auditoría (mismo patrón ya usado para los 2 defectos
  de Caso 5C V2).
- Activar D-118/D-119/D-120 — permanecen a nivel de principio; esta auditoría no es condición
  suficiente para su activación.
- Reabrir o reevaluar cualquier decisión D-001 a D-126 — la auditoría verifica que su
  implementación sigue vigente, no las reevalúa.

### Restricciones que aplican

- **D-030**: ningún parámetro se calibra ni ajusta como parte de esta auditoría.
- **D-057**: verificación contra código/estado actual, no reconstruida de memoria.
- **D-118/D-119/D-120**: intactas, no activadas ni condicionadas por esta fase.
- **Todas las decisiones previas (D-001 a D-126)**: se auditan en cuanto a vigencia de su
  implementación, nunca reabiertas como contenido.

### Evidencia

- `PROPUESTA_AUDITORIA_INTEGRAL_POST_CASO5C_V1.md` completa — objetivo, alcance A-H con evidencia
  ya existente mapeada, criterio de aprobación, límites, relación con Caso 6 Fase 1.
- `AUDITORIA_CIERRE_CASO5C_V2.md` (commit `54adf2e`): estado congelado que esta auditoría toma
  como punto de partida a verificar.

---

## Fuera de alcance de este documento

No se ejecuta ninguna verificación. No se especifica la implementación técnica de la auditoría
(queda para `ESPECIFICACION_IMPLEMENTACION_AUDITORIA_INTEGRAL_POST_CASO5C_V1.md`, si resulta
necesaria, o ejecución directa bajo este alcance ya vinculante). No se abre Caso 6 Fase 1
(recomendador). No se resuelve arquitectura, semántica, ni criterios de decisión automática del
futuro recomendador.

---

## Próximo documento

Si el alcance de D-127 se considera suficientemente operativo sin necesidad de una especificación
técnica intermedia, el siguiente paso es la ejecución directa y `AUDITORIA_INTEGRAL_POST_CASO5C_
V1.md` con el resultado por área. Si se requiere traducir el alcance a pasos de verificación más
concretos primero, el siguiente documento es `ESPECIFICACION_IMPLEMENTACION_AUDITORIA_INTEGRAL_
POST_CASO5C_V1.md` — a decidir por el auditor antes de ejecutar.
