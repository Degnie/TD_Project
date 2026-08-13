# Propuesta — Auditoría Integral Post Caso 5C (Fase 0 de Caso 6)

Estado: **documento de apertura — previo a cualquier decisión, ejecución, o implementación**.
Define el objetivo, alcance (A-H), criterio de aprobación, y límites de la auditoría integral que
debe preceder a cualquier diseño de capa de recomendación, siguiendo el mismo ciclo que toda fase
anterior del proyecto: propuesta → decisión → especificación → implementación/ejecución →
auditoría → congelamiento. **No se ejecuta ninguna verificación en este documento.**

**Punto de partida**: cierre de `AUDITORIA_CIERRE_CASO5C_V2.md` (commit `54adf2e`) — Caso 5C V2
completo (Capa 1, corpus declarado, Capa 2 descriptiva, análisis interpretativo limitado, 67
comparaciones, 2 instrumentos). El auditor definió el siguiente paso como una auditoría de estado
global, no como una nueva capacidad experimental — precedente distinto a toda apertura de fase
anterior (Caso 5A/5B/5C no partieron de una revalidación del sistema completo, sino de
`MAPA_EVOLUCION_V2.md` §0, la validación integral previa a Caso 5).

---

## 1. Objetivo de esta fase

**Pregunta que responde**: ¿la plataforma construida en Caso 1 → Caso 5C V2 está suficientemente
estable y trazable para añadir una capa de recomendación?

**Pregunta que NO responde** (explícitamente fuera de esta fase): ¿qué recomienda el sistema? Esa
pregunta pertenece a Caso 6 Fase 1 (recomendador), condicionada al cierre aprobado de esta
auditoría.

**Por qué es una fase propia y no una ejecución técnica directa**: a diferencia de las auditorías
de cierre de cada fase individual (que verifican una capacidad nueva contra su propia
especificación), esta auditoría verifica el **estado acumulado** de 8 fases congeladas (Caso 1,
Caso 2, Caso 3A, Caso 3B, Caso 4, Caso 5A, Caso 5B, Caso 5C V2) como conjunto — es la línea base
declarada antes de que el sistema cambie de rol (de "generar y describir evidencia" a "también
recomendar"). Merece registro formal, no solo ejecución.

---

## 2. Alcance (A-H)

### A. Motor base

**Verificar**: compilación completa, ejecución del pipeline principal, ausencia de regresiones,
suite de tests existente, determinismo (misma entrada → mismo resultado), consistencia de hashes
de identidad, reproducibilidad de datasets congelados.

**Evidencia disponible sin ejecución nueva**: `dotnet test -c Release` (suite de producción:
Domain.Tests, Application.Tests, Infrastructure.Tests, Contracts.Tests, Api.Tests — 126 pruebas en
el último estado verificado), `IdentidadExperimentoCompleta`/`HashCompuesto`/
`HashConfiguracionEconomica` ya verificados por prueba en múltiples fases (D-113, Caso 5B/5C).

### B. Estrategias

**Verificar**: las 6 estrategias congeladas (Tres Mosqueteros, Ema Cross, ZScore Reversion,
Neutral, Volumen Breakout, Mhi Mayoria) cargan correctamente, sus parámetros congelados siguen
siendo válidos, ausencia de dependencias ocultas entre estrategias.

**Evidencia disponible**: `Domain.Tests` (suite de estrategias), fixtures de cada `Caso3`/`Fase1`
correspondiente, ejecución real ya cubierta por la campaña de 67 comparaciones de Caso 5C.

### C. Motor financiero

**Verificar**: cálculo de métricas (PnLTotal, DrawdownMaximoPct, ProfitFactor, ExposicionMaxima,
CashFinal, EquityFinal), costes, capital, estados incompletos/fallidos.

**Evidencia disponible**: `modelo_financiero/TestsMetricasFinancieras.cs`, verificación manual ya
documentada en Caso 5C Capa 2 (`RESULTADO_ANALISIS_CORPUS_CASO5C_V2.md`).

### D. Gestores de riesgo

**Verificar**: los 3 gestores intercambiables (FixedFractional, FixedRisk, VolatilitySizing) —
mismo comportamiento con misma entrada, separación estrategia/gestor.

**Evidencia disponible**: `caso5/TestsGestoresRiesgo.cs` (10/10, Caso 5A), P7 de Caso 5A
("comparación de control: misma estrategia/dataset/economía, solo el gestor cambia la cantidad").

### E. Comparador

**Verificar**: comparación multi-gestor, identidad experimental, ausencia de ranking, ausencia de
recomendación accidental.

**Evidencia disponible**: `caso5/TestsComparadorGestores.cs` (8/8, Caso 5B), P5/P6 (ausencia
estructural de ranking, ausencia de método de recomendación).

### F. Persistencia de evidencia

**Verificar**: escritura, lectura, manifiesto, corpus oficial.

**Evidencia disponible**: `caso5/TestsPersistidorComparaciones.cs` (7/7, Capa 1),
`MANIFIESTO_CORPUS_CASO5C_V1.json` ya verificado por conjunto contra disco (0 huérfanas,
`AUDITORIA_CONSISTENCIA_CORPUS_CASO5C_V2.md`).

### G. Capa analítica

**Verificar** — Descriptiva: cobertura, distribuciones, métricas. Interpretativa: relaciones,
patrones, restricciones. Confirmar en ambas: no recomienda, no selecciona, no genera reglas
operativas.

**Evidencia disponible**: `analisis_corpus/` (11/11), `analisis_interpretativo/` (8/8), ambas ya
ejecutadas sobre el corpus de 67 comparaciones en `RESULTADO_ANALISIS_CORPUS_CASO5C_V2.md`.

### H. Datos

**Verificar**: datasets `BTCUSDT_2024-01-02_2025-01-02`, `BTCUSDT_2022-01-01_2023-01-01`,
`ETHUSDT_2024-01-02_2025-01-02` — hashes, metadata, estructura.

**Evidencia disponible**: `metadata.json` de cada dataset (SHA-256 ya verificado en cada
congelación), estructura de 13 timeframes por instrumento ya verificada por conjunto.

---

## 3. Qué se considera evidencia de aprobación

Para cada área A-H, el criterio de aprobación es binario y verificable, no una impresión:

- **A/B/C/D/E**: la suite de tests correspondiente pasa en su totalidad (N/N), ejecutada en esta
  fase (no solo citada de memoria) — mismo criterio de "verificado contra código actual, no
  reconstruido de memoria" ya aplicado en cada apertura de fase previa (D-057).
- **F**: manifiesto reconciliado con disco por conjunto (0 huérfanas), mismo mecanismo ya usado 2
  veces en Caso 5C.
- **G**: las 2 suites de análisis pasan en su totalidad, y las salvaguardas estructurales (P5/P6 de
  cada una) confirman ausencia de ranking/selección/recomendación.
- **H**: SHA-256 de cada dataset coincide con el declarado en su `metadata.json` — verificación
  directa, no re-descarga.

**Ningún criterio depende de si el resultado financiero observado es favorable o desfavorable** —
mismo principio administrativo ya aplicado en D-126 (criterios de admisión al corpus, no de
resultado).

---

## 4. Qué queda fuera de esta fase

- **No se diseña ni especifica el recomendador** — eso es Caso 6 Fase 1, posterior y condicionada
  al cierre aprobado de esta auditoría.
- **No se ejecuta ninguna campaña nueva ni se generan comparaciones nuevas** — la auditoría usa
  evidencia y suites ya existentes, no produce corpus adicional (ver §5 sobre ejecución).
- **No se modifica ningún componente de `src/`, ni de `exploration/laboratorio/caso5/`** — esta
  fase valida, no corrige por sí sola; si aparece un problema real, se reporta y se resuelve como
  una corrección aparte, con su propio registro, antes de cerrar la auditoría (mismo patrón ya
  usado para los defectos de "3 periodos" e "instrumento único" en Caso 5C).
- **No se activa D-118/D-119/D-120** — siguen a nivel de principio; esta auditoría no es una
  condición suficiente para su activación, solo la línea base necesaria antes de evaluarlo en Caso
  6 Fase 1.
- **No se reabre ninguna decisión D-001 a D-126** — la auditoría verifica que su implementación
  sigue vigente, no las reevalúa.

---

## 5. Restricción de ejecución (regla de esta fase, distinta de Caso 5C)

**Primero evitar generar residuos; si aparece alguno inevitablemente, clasificarlo
explícitamente** — orden de preferencia:

1. **Primera opción**: `dotnet test`, ejecutables con pruebas ya existentes (P1-Pn de cada
   `Tests*.cs`), verificaciones estructurales, inspección y lectura de artefactos existentes
   (manifiesto, metadata, hashes). Ninguna de estas ejecuciones escribe a `caso5/resultados/`.
2. **Si alguna verificación requiere ejecución real** que sí escriba evidencia (ej. una corrida de
   `campana_corpus/` para reconfirmar determinismo end-to-end): el artefacto resultante debe
   quedar identificado aparte, en una ubicación propia (`caso6/auditoria_integral/
   ejecuciones_tecnicas/` o equivalente), **nunca mezclado con `caso5/resultados/`**, y **nunca
   incorporado al manifiesto, al corpus oficial, ni a ningún análisis**.
3. **No se aplica el patrón de clasificación de residuos de Caso 5C por defecto** — esa
   clasificación fue necesaria porque las campañas de Caso 5C generaban evidencia experimental
   deliberadamente. Esta auditoría no busca producir evidencia nueva, así que la regla es evitar el
   residuo primero, clasificar solo si resulta inevitable.

---

## 6. Relación con la futura capa de recomendación (Caso 6 Fase 1)

Esta auditoría es la **línea base declarada**, no una parte del diseño del recomendador. Su cierre
aprobado es condición necesaria (no suficiente) para abrir `PROPUESTA_CASO6_RECOMENDADOR_V1.md` —
que a su vez deberá resolver, como decisión aparte, qué significa recomendar, qué evidencia mínima
requiere, qué puede sugerir, qué está prohibido, cómo declara incertidumbre, y cómo evita
convertirse en selector (D-118/D-119/D-120 seguirán rigiendo esa fase).

```
Caso 5C V2 (congelado, commit 54adf2e)
        ↓
Auditoría integral post Caso 5C (esta fase — línea base)
        ↓
[condicionado a cierre aprobado]
        ↓
Caso 6 Fase 1 — Recomendador basado en evidencia (propuesta aparte)
```

---

## 7. Documento siguiente

Si esta propuesta se aprueba: `DECISIONES_AUDITORIA_INTEGRAL_POST_CASO5C_V1.md` (candidata D-127),
fijando el alcance A-H como vinculante, el criterio de aprobación de §3, y la restricción de
ejecución de §5. Después, `ESPECIFICACION_IMPLEMENTACION_AUDITORIA_INTEGRAL_POST_CASO5C_V1.md`
(o, si la decisión ya deja suficientemente operativo el alcance, ejecución directa sin
especificación intermedia — a resolver en la decisión). Finalmente, ejecución y
`AUDITORIA_INTEGRAL_POST_CASO5C_V1.md` con el resultado por área (tabla Sí/No + problemas
encontrados + requiere corrección, según formato ya solicitado).

---

## Fuera de alcance de este documento

No se ejecuta ninguna verificación. No se decide todavía el alcance vinculante (queda para la
decisión). No se abre Caso 6 Fase 1. No se modifica ningún código ni artefacto existente.
