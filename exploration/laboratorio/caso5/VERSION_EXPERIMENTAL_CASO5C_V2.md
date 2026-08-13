# Versión Experimental — Caso 5C V2: Corpus Ampliado, Análisis Descriptivo e Interpretativo

Estado: **documento de congelamiento oficial — cierre deliberado de la fase de ampliación de
Caso 5C** (autorizado tras revisión de `RESULTADO_ANALISIS_CORPUS_CASO5C_V2.md`). A partir de este
documento, Caso 5C queda congelado como **V2 Experimental**, extendiendo `caso5c-capa1-v1-
experimental` con 3 capacidades nuevas (corpus declarado por manifiesto, análisis descriptivo, y
análisis interpretativo limitado) y 2 dimensiones de diversidad de evidencia (tiempo, instrumento) —
sin modificar ningún componente de Capa 1 (`ComparadorGestores`, `PersistidorComparaciones`,
`RenderizadorComparacionGestores`). Mismo patrón que `VERSION_EXPERIMENTAL_CASO5C_CAPA1_V1.md`.

**Alcance explícito de este congelamiento**: corpus declarado (manifiesto), Capa 2 descriptiva
(D-123), análisis interpretativo limitado (D-124), diversidad temporal (D-121/D-122) y de
instrumento (D-125/D-126). **No** queda congelada aquí ninguna forma de recomendación — D-118/
D-119/D-120 permanecen resueltas únicamente a nivel de principio, exactamente como en Capa 1.

---

## Identificación

- **Nombre**: Caso 5C V2 — Corpus ampliado, análisis descriptivo e interpretativo
- **Versión**: V2 Experimental
- **Estado**: Congelado
- **Fecha de congelamiento**: 2026-08-13
- **Base de aprobación**: `RESULTADO_ANALISIS_CORPUS_CASO5C_V2.md`, revisado y aprobado.

---

## Componentes incluidos

**`MANIFIESTO_CORPUS_CASO5C_V1.json`**: artefacto declarado (vive en `caso5/`, fuera de
`resultados/` gitignored), separa 67 comparaciones oficiales de 172−67=105 carpetas excluidas con
lista explícita más 25 sin lista (`primera-ejecucion-interrumpida-v2`, categoría preexistente),
clasificadas exclusivamente por inspección de contenido — nunca por timestamp.

**`analisis_corpus/`** (D-123): `LectorCorpus.Leer(dirResultados, rutaManifiesto)` +
`AnalisisDescriptivo.Resumir` — cobertura, distribuciones, comparación entre datasets, casos
atípicos. Ningún ranking, score, ni selección (P5/P6 estructurales). 11/11 pruebas (P1-P9, P4b,
P8b).

**`analisis_interpretativo/`** (D-124): `DetectorRelaciones` — `CruzarDimensiones`,
`AgruparPorPatron`, `DescribirCondicionesDeAparicion`, `CompararConsistencia`. Reutiliza
`LectorCorpus`/`AnalisisDescriptivo` por referencia de compilación, nunca los duplica. 8/8 pruebas
(P1-P8), incluyendo P6 (ausencia léxica de lenguaje prescriptivo) y P7 (trazabilidad completa).

**Diversidad temporal** (D-121/D-122, Sub-campaña D): dataset `BTCUSDT_2022-01-01_2023-01-01`
(vista de compatibilidad en `datasets/reales/BTCUSDT_2022/`), 18 comparaciones, mismo rango de
1 año, mismo instrumento.

**Diversidad de instrumento** (D-125/D-126, Sub-campaña E): dataset
`datasets/reales/ETHUSDT/` (13 timeframes, SHA-256 verificado), 18 comparaciones, mismo rango
temporal 2024-01-02–2025-01-02 ya congelado para `BTCUSDT` — instrumento como única dimensión
variada, verificado por `HashCompuesto`/`HashConfiguracionEconomica`.

**Pruebas de campaña** (`campana_corpus/TestsCampanaCorpus.cs`): P1-P9, incluyendo P6/P8 (identidad
experimental: el dataset/instrumento es el único eje que cambia `HashCompuesto`,
`HashConfiguracionEconomica` no depende de periodo ni instrumento, reproducibilidad verificada por
doble ejecución) y P7/P9 (cobertura: 18/18 comparaciones persistidas sin duplicados, en cada
sub-campaña).

---

## Decisiones congeladas

**D-123 a D-126** (4 decisiones), registradas en `DECISIONES_CASO5C_CAPA2_V1.md`,
`DECISIONES_EVOLUCION_POST_CAPA2_V1.md`, `DECISIONES_DIVERSIDAD_INSTRUMENTO_CASO5C_V2.md`,
`DECISIONES_INCORPORACION_ETHUSDT_CASO5C_V1.md`. Ninguna reasignada a contenido distinto del
originalmente registrado. Las 4 🟢 Aprobadas e implementadas.

**D-121/D-122** (diversidad temporal, ya congeladas en la fase previa a esta versión) permanecen
intactas — Sub-campaña D es la evidencia física de su implementación, incorporada aquí como parte
del corpus congelado en V2.

**D-118, D-119, D-120 permanecen resueltas únicamente a nivel de principio** — no implementadas, no
congeladas como capacidad funcional en este documento. Esta versión no las activa ni las acerca a
su activación: la ampliación del corpus (49→67) y la incorporación de una segunda capa de análisis
(interpretativo) no son, por sí solas, condición hacia recomendación.

---

## Garantías

- **Capa 1 intacta**: `ComparadorGestores.cs`, `PersistidorComparaciones.cs`,
  `RenderizadorComparacionGestores.cs`, `EjecutorProtocolo.cs` sin modificación — verificado por
  `git status --porcelain` en todo el ciclo, y por P9 de `analisis_corpus`/P8 de
  `analisis_interpretativo` (ausencia estructural de referencias a componentes de ejecución).
- **`src/`/`tests/` intactos**: ningún archivo de producción modificado en ningún punto de esta
  fase — verificado por `git status --porcelain -- src/ tests/` vacío.
- **Manifiesto reconciliado con disco**: verificado por conjunto (no suma aritmética),
  0 carpetas físicas sin clasificar (`AUDITORIA_CONSISTENCIA_CORPUS_CASO5C_V2.md` §1).
- **Identidad experimental verificada por hash, no por diseño únicamente**: P6 (Sub-campaña D) y P8
  (Sub-campaña E) confirman que el dataset/instrumento es el único eje que cambia
  `HashCompuesto`, que `HashConfiguracionEconomica` no depende de periodo ni instrumento, y que la
  reproducibilidad se sostiene entre ejecuciones repetidas.
- **Ausencia estructural de ranking/selección/recomendación**: P5/P6 de ambos componentes de
  análisis (Capa 2 e interpretativo) — reflexión sobre tipos de salida sin campos prohibidos,
  ausencia léxica de lenguaje prescriptivo. Reconocido explícitamente como barrera, no garantía
  completa (`AUDITORIA_ANALISIS_INTERPRETATIVO_CASO5C_V1.md` §2) — compensado por redundancia
  estructural y ausencia de prosa generada dinámicamente.
- **Parámetros económicos sin cambio**: `TasaMargen=0.1m`, costes `0.001m`/`0.001m` idénticos en
  las 67 comparaciones oficiales — verificado por `HashConfiguracionEconomica` idéntico entre
  instrumentos y periodos (D-030 respetado en toda la fase).
- **2 defectos de infraestructura detectados y corregidos, documentados sin ocultamiento**: el
  texto fijo de `Limitaciones` (Capa 2 e interpretativo) afirmaba "instrumento único (BTCUSDT)"
  como literal, desactualizado al incorporar `ETHUSDT`; corregido para derivar la lista de
  instrumentos de los datos reales, filtrando evidencia sin métrica (`DatasetInexistente_
  ParaCorpusDeFallo`) — mismo criterio ya usado para "período temporal" (`RESULTADO_ANALISIS_
  CORPUS_CASO5C_CAPA2_V1.md`). Documentado en `RESULTADO_ANALISIS_CORPUS_CASO5C_V2.md` §0.

---

## Exclusiones (explícitas)

- **Recomendación, selección automática, ranking, score compuesto**: ningún componente de esta
  versión los implementa — D-118/D-119/D-120 permanecen a nivel de principio.
- **Causalidad entre instrumento y patrón observado**: ninguna diferencia entre `BTCUSDT`/`ETHUSDT`
  se atribuye a una causa (liquidez, volatilidad, u otra) — solo se documenta presencia/ausencia.
- **Extrapolación fuera del corpus**: ningún patrón se generaliza a instrumentos, periodos, o
  configuraciones no representadas en las 67 comparaciones oficiales.
- **Recalibración de parámetros económicos**: ningún ajuste de `TasaMargen`/costes por diferencia
  de escala entre instrumentos — permanece como decisión aparte, explícita, no tomada aquí (D-030).
- **Tercer instrumento o rango temporal adicional**: fuera de esta versión — cualquier expansión
  futura del corpus requiere su propia propuesta/decisión, mismo patrón ya usado 2 veces.
- **Nueva capacidad analítica más allá de descripción/relaciones**: evaluada explícitamente y
  descartada por el auditor en esta fase — "no se justifica ahora... necesitaría una pregunta
  concreta, no solo 'extraer más'".

---

## Evidencia

- **11/11 pruebas `analisis_corpus`** (P1-P9, P4b, P8b) sobre el corpus real de 67 comparaciones.
- **8/8 pruebas `analisis_interpretativo`** (P1-P8) sobre el mismo corpus.
- **9/9 pruebas `campana_corpus`** (P1-P9, incluyendo identidad experimental y cobertura de Sub-
  campañas D y E).
- **25/25 pruebas Caso5 Capa1/5A/5B** sin regresión.
- **126/126 tests de producción** sin cambio.
- **`git status --porcelain -- src/ tests/`**: vacío en todo el ciclo.
- **Manifiesto verificado por conjunto contra disco**: 172 carpetas físicas, 0 sin clasificar.
- Documentos de la fase: `PROPUESTA_CASO5C_CAPA2_V1.md`, `DECISIONES_CASO5C_CAPA2_V1.md`,
  `ESPECIFICACION_IMPLEMENTACION_CASO5C_CAPA2_V1.md`, `RESULTADO_ANALISIS_CORPUS_CASO5C_CAPA2_
  V1.md`, `PROPUESTA_EVOLUCION_POST_CAPA2_V1.md`, `DECISIONES_EVOLUCION_POST_CAPA2_V1.md`,
  `ESPECIFICACION_IMPLEMENTACION_ANALISIS_INTERPRETATIVO_CASO5C_V1.md`, `AUDITORIA_ANALISIS_
  INTERPRETATIVO_CASO5C_V1.md`, `PROPUESTA_DIVERSIDAD_INSTRUMENTO_CASO5C_V2.md`,
  `DECISIONES_DIVERSIDAD_INSTRUMENTO_CASO5C_V2.md`, `ESPECIFICACION_IMPLEMENTACION_DIVERSIDAD_
  INSTRUMENTO_CASO5C_V2.md`, `CLASIFICACION_PROPUESTA_CARPETAS_SUBCAMPANA_E_V1.md`,
  `AUDITORIA_SUBCAMPANA_E_CASO5C_V1.md`, `DECISIONES_INCORPORACION_ETHUSDT_CASO5C_V1.md`,
  `AUDITORIA_CONSISTENCIA_CORPUS_CASO5C_V2.md`, `RESULTADO_ANALISIS_CORPUS_CASO5C_V2.md`.
- Auditoría de cierre: `caso5/AUDITORIA_CIERRE_CASO5C_V2.md`.

---

## Regla de evolución

Cualquier extensión que amplíe el alcance de esta versión — Capa de recomendación (D-118/D-119/
D-120), tercer instrumento o periodo, nueva capacidad analítica — requiere una **nueva fase**,
nunca una edición in-place de V2 (mismo principio que la regla de evolución de
`VERSION_EXPERIMENTAL_CASO5C_CAPA1_V1.md`).

```
V1 Experimental — Caso 5C Capa 1 (congelada)
        |
        v
Corpus declarado + Capa 2 descriptiva + analisis interpretativo
+ diversidad temporal + diversidad de instrumento
        |
        v
V2 Experimental — Caso 5C (congelada, este documento)
        |
        v
  pregunta metodologica concreta (evaluacion posterior, no automatica)
        |
        v
Caso 5C — siguiente fase (recomendacion, nueva dimension, o nueva capacidad)
```

---

## Fuera de alcance de este documento

No se implementó código adicional. No se modifica ningún módulo. No se decide si la evidencia
acumulada es suficiente para diseñar una capa de recomendación — D-118/D-119/D-120 permanecen sin
resolver más allá del principio ya fijado. No se abre ninguna fase siguiente. No se elige tercer
instrumento ni periodo adicional.

---

## Criterio de cierre de este documento

- ✓ Identificación formal (nombre, versión, estado, fecha) registrada.
- ✓ Componentes incluidos listados con archivo y decisión de origen (D-121 a D-126).
- ✓ Decisiones congeladas referenciadas, sin reasignaciones, las 4 (D-123 a D-126) aprobadas e
  implementadas; D-118/D-119/D-120 declaradas explícitamente como principio, no como
  funcionalidad congelada.
- ✓ Garantías (Capa 1 intacta, `src/`/`tests/` intactos, manifiesto reconciliado, identidad
  experimental verificada por hash, ausencia estructural de ranking/selección, parámetros
  económicos sin cambio, 2 defectos documentados sin ocultamiento) declaradas y respaldadas por
  evidencia ya verificada.
- ✓ Exclusiones declaradas explícitamente (recomendación, causalidad, extrapolación,
  recalibración, tercer instrumento, nueva capacidad analítica).
- ✓ Evidencia referenciada (11/11 + 8/8 + 9/9 + 25/25 + 126/126, manifiesto por conjunto,
  `src/`/`tests/` sin cambios).
- ✓ Regla de evolución (nueva fase ante cambio de alcance) establecida.
- ⏳ Pendiente: `AUDITORIA_CIERRE_CASO5C_V2.md`, luego preparación de commit conjunto y tag
  `caso5c-v2-experimental`.
