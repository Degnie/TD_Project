# Auditoría de Cierre — Caso 5C Capa 1: Persistencia de Evidencia Comparativa

Estado: **documento de cierre de sub-fase — Caso 5C Capa 1 completa**. Consolida evidencia
verificada del ciclo propuesta → decisión → especificación → implementación → pruebas → auditoría
para D-116/D-117 (Capa 1). D-118 a D-120 (Capa 2) quedan fuera de este cierre — resueltas solo a
nivel de principio en `DECISIONES_CASO5C_V1.md`, sin implementación. Mismo patrón que las
auditorías de cierre de Caso 5A (`AUDITORIA_CASO5A_V1.md`) y Caso 5B (`AUDITORIA_CASO5B_V1.md`).

---

## 1. Objetivo de Caso 5C Capa 1

**Objetivo**: persistencia reproducible de resultados comparativos ya calculados por Caso 5B —
conservar evidencia, no interpretarla.

**Incluye**:
- Escritura a disco de cada `ResultadoComparativoGestores` ejecutado, en una carpeta timestamped
  identificable por estrategia/timeframe.
- Identidad de la comparación y de cada gestor comparado, en formato reproducible.
- El render de texto ya producido por Caso 5B, escrito verbatim.

**No incluye** (confirmado por ausencia de código, no solo por declaración — ver §5, P5/P7):
- Ranking, puntuación, o criterio de "mejor gestor" en ningún artefacto persistido.
- Ningún valor numérico de métrica duplicado en el JSON de identidad.
- Análisis, recomendación, umbral de evidencia, o cualquier elemento de Capa 2.
- Modificación de `ComparadorGestores.cs` (Caso 5B, congelado).

---

## 2. Relación con Caso 5B

Caso 5C Capa 1 no reconstruye ni duplica infraestructura — envuelve exclusivamente lo que Caso 5B
ya congeló (`caso5b-v1-experimental`):

| Caso 5B aporta | Caso 5C Capa 1 lo usa para |
|---|---|
| `ComparadorGestores.Comparar` | Producir el `ResultadoComparativoGestores` en memoria que se persiste — sin reejecutar ninguna corrida |
| `ResultadoComparativoGestores`/`FilaComparacionGestor` | Ser la única fuente de los campos escritos en `IDENTIDAD_COMPARACION.json` |
| `RenderizadorComparacionGestores.Generar` | Ser la fuente exacta y única de `COMPARACION_GESTORES_V1.md` — sin una segunda ruta de formateo |

**Separación de responsabilidad**:

```
ComparadorGestores          (Caso 5B — comparar, en memoria, sin tocar disco)
        |
        v
ResultadoComparativoGestores
        |
        v
PersistidorComparaciones     (Caso 5C Capa 1 — conservar evidencia comparable, en disco)
        |
        v
Evidencia almacenada
```

**Ningún archivo de Caso 5B fue modificado** en este ciclo — `ComparadorGestores.cs` intacto,
confirmado por P7 (§5, verificación por reflexión de firma pública) y por
`git status --porcelain` vacío sobre esa ruta durante todo el ciclo.

---

## 3. Resolución D-116 a D-120 — resumen

| Decisión | Resolución | Estado en este cierre |
|---|---|---|
| D-116 | Persistencia separada: `PersistidorComparaciones`, componente nuevo que envuelve `ComparadorGestores` sin modificarlo — extensión directa del patrón `protocolo/resultados/` | ✅ Implementada |
| D-117 | Insumo válido para análisis futuro: campos ya presentes en `IDENTIDAD_COMPARACION.json` + `MetricasFinancieras` — régimen de mercado y datos externos explícitamente excluidos | ✅ Implementada (define el insumo; ningún análisis lo consume todavía) |
| D-118 | Semántica de "recomendar": selección automática excluida por rol del sistema (no por madurez de evidencia); sugerencia y orden explícito quedan vivas como opciones futuras | ⏳ Sin activar — Capa 2 no implementada |
| D-119 | Umbral de suficiencia de evidencia: principio fijado ("sin evidencia suficiente → no recomendar"), sin valores numéricos | ⏳ Sin activar — Capa 2 no implementada |
| D-120 | Formato de `RecomendacionExperimental` con `EvidenciaUsada`/`Limitaciones` obligatorios | ⏳ Sin activar — Capa 2 no implementada |

Ninguna de las 5 decisiones fue reabierta durante la implementación de Capa 1.

---

## 4. Evidencia de implementación

**`caso5/PersistidorComparaciones.cs`** (nuevo):
- `Persistir(string dirResultados, ResultadoComparativoGestores resultado) : string` — único método
  público. Recibe el resultado ya calculado, no reejecuta ninguna corrida.
- Crea `caso5/resultados/{Estrategia}_{Timeframe}_{timestamp}/` (mismo patrón de nombre que
  `protocolo/Program.cs:50`, extendido con `{Timeframe}` porque `ComparadorGestores` opera sobre un
  único timeframe por invocación, D-113).
- Escribe `IDENTIDAD_COMPARACION.json` (interpolación manual de string, mismo estilo que
  `protocolo/Program.cs:58-68` — sin `JsonSerializer`, consistente con el resto del proyecto).
- Escribe `COMPARACION_GESTORES_V1.md` con el contenido exacto de
  `RenderizadorComparacionGestores.Generar(resultado)`.
- Devuelve la ruta de la carpeta escrita (mismo patrón que `protocolo/Program.cs:71`).

**Formato generado, verificado en ejecución real**:
```json
{
  "estrategia": "Tres Mosqueteros",
  "timeframe": "1D",
  "nombreDataset": "BTCUSDT_2024-01-02_2025-01-02",
  "gestores": [
    { "identidad": "fixed-fractional:v1:riesgo=0.1", "estado": "Success" },
    { "identidad": "fixed-risk:v1:monto=50", "estado": "Success" }
  ],
  "fechaGeneracionUtc": "2026-08-12T20:40:12Z"
}
```
Coincide exactamente con el formato fijado en
`ESPECIFICACION_IMPLEMENTACION_PERSISTENCIA_EVIDENCIA_V1.md` §3.

**Identidad**: cada `gestores[].identidad` proviene de `IIdentidadGestorRiesgo.
ObtenerIdentidadConfiguracion()` (Caso 5A/D-109) — determinista y estable, sin recalcular ni
inferir ningún valor nuevo. `fechaGeneracionUtc` es el único campo sin origen en
`ResultadoComparativoGestores` — metadata de persistencia, no característica experimental del
resultado (precisión del auditor incorporada a D-116).

**Exclusión de `caso5/resultados/`**: agregada línea en `.gitignore` raíz
(`exploration/laboratorio/caso5/resultados/`), mismo criterio que
`exploration/laboratorio/protocolo/resultados/` ya excluido — evidencia regenerable, no fuente.
Verificado con `git check-ignore -v`.

**`caso5/TestsPersistidorComparaciones.cs`** (nuevo) — 7 pruebas, mismo patrón runner manual que
`TestsComparadorGestores.cs`.

**`caso5/Program.cs`** (modificado) — invoca las 3 suites de Caso 5 (A, B, Capa 1 de C) en la misma
ejecución.

---

## 5. Evidencia de pruebas

**7/7 pruebas de Caso 5C Capa 1** (`caso5/TestsPersistidorComparaciones.cs`):
- P1 — estructura de carpeta correcta (exactamente 2 archivos).
- P2 — contenido de `IDENTIDAD_COMPARACION.json` coincide campo a campo con el
  `ResultadoComparativoGestores` en memoria.
- P3 — contenido de `COMPARACION_GESTORES_V1.md` idéntico al render directo de
  `RenderizadorComparacionGestores.Generar` — confirma que no existe una segunda ruta de
  formateo divergente.
- P4 — reproducibilidad: dos llamadas con el mismo resultado producen el mismo JSON salvo
  `fechaGeneracionUtc`.
- P5 — el JSON no contiene ninguna clave de métrica financiera (`pnlTotal`, `drawdownMaximoPct`,
  `profitFactor`, `exposicionMaxima`, `cashFinal`, `equityFinal`) — confirma D-116/§6 de la
  especificación (fuente única de verdad, sin duplicar números).
- P6 — un fallo de escritura a disco (ruta inválida) no invalida el `ResultadoComparativoGestores`
  ya calculado en memoria — confirma que la persistencia es una capa secundaria sin efecto
  retroactivo sobre el cálculo (D-059/D-096).
- P7 — la firma pública de `ComparadorGestores.Comparar`/`RenderizadorComparacionGestores.Generar`
  permanece idéntica a Caso 5B, verificado por reflexión — protege que Caso 5B no fue reabierto.

**P5 y P7 — confirmación estructural, no de comportamiento**: mismo criterio que P5/P6 de
`TestsComparadorGestores.cs` en Caso 5B — ambas fallan automáticamente si una modificación futura
introduce una segunda fuente de verdad numérica o reabre `ComparadorGestores.cs` sin pasar por una
decisión D-N nueva.

**18/18 pruebas de Caso 5A + 5B**: sin regresión — Capa 1 no modificó ningún archivo de esas
sub-fases.

**25/25 pruebas del módulo `caso5` completo** (10 Caso 5A + 8 Caso 5B + 7 Caso 5C Capa 1),
ejecutadas en la misma corrida de `Caso5.csproj`.

**126/126 tests de producción**: sin cambio — `git status --porcelain -- src/ tests/` vacío durante
todo el ciclo.

---

## 6. Hallazgos de implementación

**Ninguna desviación respecto a la especificación**: el formato de `IDENTIDAD_COMPARACION.json`, la
estructura de carpeta, y el contenido de `COMPARACION_GESTORES_V1.md` implementados coinciden
exactamente con `ESPECIFICACION_IMPLEMENTACION_PERSISTENCIA_EVIDENCIA_V1.md` §3-§4, verificado por
P2/P3, no solo por inspección de código.

**Ningún hallazgo requirió una decisión D-N nueva.**

---

## 7. Límites congelados

Fuera de Caso 5C Capa 1, confirmado por ausencia de código (no solo por declaración):

- **Capa 2 no existe todavía** — ningún componente de análisis, recomendación, ranking, o umbral de
  evidencia fue implementado. D-118/D-119/D-120 quedan resueltas únicamente a nivel de principio.
- **Ninguna interpretación del corpus persistido** — `PersistidorComparaciones` escribe evidencia
  cruda ya calculada por Caso 5B; ningún archivo escrito contiene una conclusión.
- **`ComparadorGestores`/`EjecutorProtocolo`/`EntradaProtocolo`/Caso 5A**: sin ninguna modificación.
- **Selección automática de gestor**: sigue excluida por rol del sistema (D-118), no reabierta.

**Pendiente futuro, explícitamente fuera de esta fase**: diseño e implementación de Capa 2
(análisis/recomendación), condicionada a que este mecanismo de persistencia produzca corpus real
acumulado — no antes.

---

## 8. Estado final — Decisiones de Caso 5C

| Decisión | Estado |
|---|---|
| D-116 | ✅ `PersistidorComparaciones` implementado, `ComparadorGestores` intacto |
| D-117 | ✅ Insumo válido definido y persistido; ningún consumidor lo usa todavía |
| D-118 | ⏳ Principio fijado, sin implementación (Capa 2) |
| D-119 | ⏳ Principio fijado, sin implementación (Capa 2) |
| D-120 | ⏳ Formato fijado, sin implementación (Capa 2) |

**Caso 5C Capa 1 implementada**:
- ✅ Persistencia reproducible (P2, P3, P4).
- ✅ Sin duplicación de fuente de verdad numérica (P5).
- ✅ Fallo de disco no invalida el resultado en memoria (P6).
- ✅ Caso 5B no reabierto (P7).

**Ninguna deuda técnica bloqueante queda abierta dentro del alcance de Capa 1.**

---

## Fuera de alcance de este documento

No se decide si Caso 5C Capa 1 se congela como versión experimental independiente. No se diseña ni
especifica Capa 2. No se evalúa todavía si el corpus generable por esta capa es suficiente para
diseñar Capa 2 — esa pregunta queda para después de este cierre, no se resuelve aquí.

---

## Criterio de cierre de esta sub-fase

- ✓ D-116/D-117: implementadas y verificadas con evidencia directa, no por inspección declarativa.
- ✓ D-118 a D-120: estado explícitamente declarado como "principio fijado, sin activar" — no se
  presentan como implementadas.
- ✓ 0 hallazgos que requirieran una decisión D-N nueva.
- ✓ 7/7 pruebas Caso 5C Capa 1 + 18/18 Caso 5A/5B sin regresión + 25/25 módulo completo + 126/126
  producción.
- ✓ P5 (sin duplicación numérica) y P7 (Caso 5B no reabierto) verificadas con evidencia directa.
- ✓ Ninguna restricción de alcance relajada: Capa 2, selección automática, ranking, análisis —
  todos fuera, como estaba autorizado.
- ⏳ Pendiente de tu decisión: congelar Caso 5C Capa 1 como versión experimental
  (`caso5c-capa1-v1-experimental` o nombre equivalente) o abrir una sub-fase adicional antes del
  congelamiento.
