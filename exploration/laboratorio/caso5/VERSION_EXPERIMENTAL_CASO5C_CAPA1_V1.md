# Versión Experimental — Caso 5C Capa 1: Persistencia de Evidencia Comparativa

Estado: **documento de congelamiento oficial — cierre de Caso 5C Capa 1** (autorizado tras
aprobación de `AUDITORIA_CASO5C_CAPA1_V1.md`). A partir de este documento, Caso 5C Capa 1 queda
congelado como **V1 Experimental**, dependiente de `caso5b-v1-experimental` — envuelve
exclusivamente su salida (`ComparadorGestores.Comparar`/`RenderizadorComparacionGestores.Generar`),
sin modificar ninguno de sus archivos. Igual que Caso 5B, **Capa 1 no toca `src/`** — toda su
implementación vive en `exploration/laboratorio/caso5/`. Mismo patrón que
`VERSION_EXPERIMENTAL_CASO5B_V1.md`/`VERSION_EXPERIMENTAL_CASO5A_V1.md`.

**Alcance explícito de este congelamiento**: únicamente Capa 1 (persistencia de evidencia). Capa 2
(análisis/recomendación, D-118 a D-120) **no** queda congelada aquí — permanece sin implementar,
resuelta solo a nivel de principio en `DECISIONES_CASO5C_V1.md`.

---

## Identificación

- **Nombre**: Caso 5C Capa 1 — Persistencia de evidencia comparativa
- **Versión**: V1 Experimental
- **Estado**: Congelado
- **Fecha de congelamiento**: 2026-08-12
- **Base de aprobación**: `AUDITORIA_CASO5C_CAPA1_V1.md`, aprobada por auditoría.

---

## Componentes incluidos

**`PersistidorComparaciones`** (D-116, `caso5/PersistidorComparaciones.cs`): componente nuevo de
laboratorio que envuelve `ComparadorGestores` (Caso 5B) sin modificarlo. Único método público:
`Persistir(string dirResultados, ResultadoComparativoGestores resultado) : string` — recibe un
resultado ya calculado, escribe a disco, devuelve la ruta escrita.

**Formato de identidad** (D-117): `IDENTIDAD_COMPARACION.json` — interpolación manual de string
(mismo estilo que `protocolo/Program.cs`), con estrategia/timeframe/dataset, identidad y estado de
cada gestor comparado, y `fechaGeneracionUtc` como único campo de metadata de persistencia (no
característica experimental del resultado).

**`COMPARACION_GESTORES_V1.md`**: contenido exacto de `RenderizadorComparacionGestores.Generar`
(Caso 5B), escrito verbatim — sin una segunda ruta de formateo.

**Estructura de archivos**: `caso5/resultados/{Estrategia}_{Timeframe}_{timestamp}/`, excluida de
git (`.gitignore`, mismo criterio que `protocolo/resultados/`).

**Pruebas**: `exploration/laboratorio/caso5/TestsPersistidorComparaciones.cs` (7 pruebas,
integradas al módulo satélite existente de Caso 5, `Caso5.csproj`, sin `.csproj` nuevo).

---

## Decisiones congeladas

D-116 y D-117 (2 decisiones), registradas en `DECISIONES_CASO5C_V1.md`. Ninguna reasignada a
contenido distinto del originalmente registrado. Ambas 🟢 Aprobadas e implementadas — ninguna queda
como deuda técnica bloqueante dentro del alcance de Capa 1.

**D-118, D-119, D-120 permanecen resueltas únicamente a nivel de principio — no implementadas, no
congeladas como capacidad funcional en este documento.** Quedan como marco para una fase posterior
(Capa 2), condicionada a la existencia de corpus real.

---

## Garantías

- **`ComparadorGestores` intacto**: verificado por P7 (reflexión sobre la firma pública de
  `Comparar`/`Generar`), no solo por inspección — Caso 5B no fue reabierto.
- **Sin duplicación de fuente de verdad**: P5 confirma que ningún valor numérico de métrica
  aparece en `IDENTIDAD_COMPARACION.json` — los números viven exclusivamente en
  `COMPARACION_GESTORES_V1.md`, evitando divergencia futura entre dos representaciones del mismo
  dato.
- **Reproducibilidad verificada por mecanismo**: P4 confirma que dos llamadas a `Persistir` con el
  mismo `ResultadoComparativoGestores` producen el mismo JSON salvo `fechaGeneracionUtc`.
- **Persistencia como capa secundaria, no bloqueante**: P6 confirma que un fallo de escritura a
  disco no invalida el resultado ya calculado en memoria (D-059/D-096 extendido a esta capa).
- **Identidad de gestor reutilizada, no recalculada**: cada `gestores[].identidad` proviene
  directamente de `IIdentidadGestorRiesgo.ObtenerIdentidadConfiguracion()` (Caso 5A/D-109) — sin
  ningún hash ni cálculo nuevo introducido por esta capa.
- **`src/` intacto**: Capa 1 no modifica ningún archivo de `src/` — verificado por
  `git status --porcelain -- src/` vacío en todo el ciclo.
- **Caso 5A/5B intactos**: ningún archivo de `GestorCapital`, gestores concretos,
  `IdentidadExperimentoCompleta`, `ComparadorGestores`, `RenderizadorComparacionGestores` fue
  modificado.

---

## Exclusiones (explícitas)

- **Capa 2 completa**: análisis, recomendación, ranking, orden por criterio explícito, umbral de
  suficiencia de evidencia — ningún componente de esta versión interpreta el corpus persistido.
  Candidato de una sub-fase posterior, condicionada a la existencia de corpus real generado por
  esta misma Capa 1.
- **Selección automática de gestor**: excluida por rol del sistema (D-118), no por madurez de
  evidencia — no reabierta por esta versión.
- **Duplicación de métricas numéricas en el JSON de identidad**: ningún valor de
  `MetricasFinancieras` se repite fuera del `.md` (P5).
- **Índice o explorador de comparaciones persistidas**: ningún componente lee ni agrega el corpus
  acumulado — cada `Persistir` es independiente, sin conocimiento de ejecuciones anteriores.
- **Kelly fraccionado, Masaniello**: siguen fuera, bloqueo metodológico de Caso 2.3 no resuelto
  (D-110, heredado sin cambios).
- **`IStrategy`, las 6 estrategias, `AplicadorFill`, `ResolutorCrossZero`, `GestorCapital`,
  `IGestorRiesgo`, `EjecutorProtocolo`, `EntradaProtocolo`, `ComparadorGestores`,
  `RenderizadorComparacionGestores` intactos**: ninguna modificación de código.

Todo lo anterior queda registrado en `DECISIONES_CASO5C_V1.md`,
`ESPECIFICACION_IMPLEMENTACION_PERSISTENCIA_EVIDENCIA_V1.md` y `AUDITORIA_CASO5C_CAPA1_V1.md` —
fuera de esta versión.

---

## Evidencia

- **7/7 pruebas Caso 5C Capa 1** (`caso5/Program.cs`, `TestsPersistidorComparaciones.EjecutarTodos()`).
- **18/18 pruebas Caso 5A + 5B** sin regresión.
- **25/25 pruebas del módulo `caso5` completo**, ejecutadas en la misma corrida.
- **126/126 tests de producción** sin cambio.
- **`git status --porcelain -- src/ tests/`**: vacío en todo el ciclo de Capa 1.
- Auditoría de cierre: `caso5/AUDITORIA_CASO5C_CAPA1_V1.md`.

---

## Regla de evolución

Cualquier extensión que amplíe el alcance de esta versión — Capa 2 (análisis/recomendación),
índice/explorador de comparaciones persistidas, selección automática — requiere una **nueva fase**,
nunca una edición in-place de V1 (mismo principio que la regla de evolución de
`VERSION_EXPERIMENTAL_CASO5B_V1.md`/`VERSION_EXPERIMENTAL_CASO5A_V1.md`).

```
V1 Experimental — Caso 5C Capa 1 (congelada)
        |
        v
  corpus acumulado suficiente (evaluacion posterior, no automatica)
        |
        v
Caso 5C Capa 2 — analisis y recomendacion experimental
```

---

## Fuera de alcance de este documento

No se implementó código adicional. No se modifica ningún módulo. No se decide si el corpus
generable por esta capa es suficiente para diseñar Capa 2 — esa evaluación queda para después de
este congelamiento, no se resuelve aquí. No se abre ninguna fase siguiente.

---

## Criterio de cierre de este documento

- ✓ Identificación formal (nombre, versión, estado, fecha) registrada.
- ✓ Componentes incluidos listados con archivo y decisión de origen (D-116/D-117).
- ✓ Decisiones congeladas referenciadas, sin reasignaciones, ambas aprobadas e implementadas;
  D-118/D-119/D-120 declaradas explícitamente como principio, no como funcionalidad congelada.
- ✓ Garantías (Caso 5B intacto, sin duplicación de fuente de verdad, reproducibilidad verificada,
  persistencia no bloqueante, identidad reutilizada, `src/`/Caso 5A/5B intactos) declaradas y
  respaldadas por evidencia ya verificada.
- ✓ Exclusiones declaradas explícitamente (Capa 2 completa, selección automática, duplicación
  numérica, índice/explorador, Kelly/Masaniello).
- ✓ Evidencia referenciada (7/7 + 18/18 + 25/25 + 126/126, `src/`/`tests/` sin cambios).
- ✓ Regla de evolución (nueva fase ante cambio de alcance) establecida.
- ⏳ Pendiente: preparación de commit y tag `caso5c-capa1-v1-experimental`.
