# Validación Integral del Sistema V1 — Auditoría de Consolidación

Estado: **documento de cierre administrativo transversal**. No es una fase experimental, no abre
numeración D-N, no representa una capacidad nueva. Consolida en un único artefacto de referencia la
evidencia ya generada por la prueba integral end-to-end ejecutada tras el cierre de Caso 4 —
documentada en detalle en `AUDITORIA_PRUEBA_INTEGRAL_SISTEMA_V1.md` (origen:
`PROPUESTA_PRUEBA_INTEGRAL_V1.md`), aquí formalizada como entidad propia con alcance,
límites y estado administrativo explícitos, en vez de quedar como carpeta/documentos sueltos.

---

## 1. Objetivo

Confirmar, mediante una ejecución end-to-end sobre datos sintéticos deterministas, que el conjunto
de fases congeladas del laboratorio experimental compone un sistema coherente — no solo que cada
fase individualmente pasa sus propias pruebas, sino que las capas interactúan correctamente entre
sí cuando se ejecutan juntas.

**Qué se validó**: carga de datos (formato base de 6 columnas, sin ningún cambio de código),
ejecución de las 6 estrategias congeladas, motor de matching/resolución de fills/gestión de
posiciones, modelo económico (costes, sizing bajo Fixed Fractional), generación de reportes
financieros, identidad experimental y reproducibilidad bit a bit.

**Qué NO se buscó medir** (explícito desde el diseño original, `PROPUESTA_PRUEBA_INTEGRAL_V1.md`
§7): rentabilidad, selección de parámetros óptimos, ni ninguna forma de calibración — ningún
resultado numérico de esta validación debe leerse como evidencia de "qué estrategia/configuración
es mejor".

---

## 2. Fases cubiertas por la evidencia

**Cobertura real, verificada contra la matriz ejecutada** (no asumida por el título del documento
original, que decía "Caso 1 a Caso 4" de forma imprecisa — la matriz sí incluyó `VolumenBreakout`,
estrategia de Caso 3B):

| Fase | Tag | Cubierta |
|---|---|---|
| Caso 1 — Laboratorio de estrategias | `caso1-v1-experimental` | ✅ Tres Mosqueteros, MHI Mayoría, EMA Cross |
| Caso 2 — Modelo financiero | `caso2-v1-experimental` | ✅ Costes, sizing (Escenario 1 duplicado) |
| Caso 3A — Generalización experimental | `caso3a-v1-experimental` | ✅ Z-Score Reversal, Estrategia Neutral |
| Caso 3B — Composición jerárquica | `caso3b-v1-experimental` | ✅ VolumenBreakout (Long, Short, reversión, económico extremo) |
| Caso 4 — Evolución financiera | `caso4-v1-experimental` | ✅ Incapacidades registradas sin bloqueo (Escenario 5, Tres Mosqueteros y VolumenBreakout) |
| **Caso 5A — Gestores de riesgo intercambiables** | `caso5a-v1-experimental` | ❌ **No cubierta** — esta validación se ejecutó y congeló antes de que Caso 5A se abriera; usa exclusivamente `GestorCapital` con la fórmula de Fixed Fractional inline, no el contrato `IGestorRiesgo`/`ConfiguracionSizing.GestorActivo` introducido después |

**Consecuencia explícita**: esta validación integral demuestra que el sistema hasta
`caso4-v1-experimental`/`caso3b-v1-experimental` es coherente end-to-end. **No** demuestra nada
sobre el framework de gestores de riesgo de Caso 5A — esa evidencia vive exclusivamente en
`caso5/TestsGestoresRiesgo.cs` (10/10, ver `caso5/AUDITORIA_CASO5A_V1.md`), un ciclo de pruebas
distinto, con su propio alcance y sin ejecución conjunta con el resto del sistema bajo datos
sintéticos de esta batería. Extender esta validación integral para incluir Caso 5A sería una
**extensión de alcance nueva**, no una reafirmación de lo ya congelado — no se realiza en este
documento.

---

## 3. Evidencia

- **33/33 hallazgos registrados, 0 contradicciones** en la ejecución final (tras corregir un
  defecto del propio generador de datos de prueba — ver §4).
- **Matriz dirigida**: 16/16 combinaciones escenario×estrategia con `Estado=Success`, 0
  excepciones no controladas — detalle completo en `AUDITORIA_PRUEBA_INTEGRAL_SISTEMA_V1.md` §3-4.
- **5 escenarios sintéticos deterministas** (sin `Random`, funciones puras de índice de vela,
  regenerables exactamente): Alcista, Bajista, Lateral, Cambio brusco de régimen, Económico
  extremo — 300 velas cada uno, timeframe único `1D`.
- **Reproducibilidad**: 2 ejecuciones idénticas de `EjecutorProtocolo.Ejecutar` (VolumenBreakout ×
  Escenario 4) produjeron `HashCompuesto`/`HashConfiguracionEconomica`/texto de reporte financiero
  idénticos.
- **Pruebas negativas**: capital insuficiente (Escenario 5, incapacidades registradas sin
  bloquear la corrida), reversión rápida (Escenario 4), ausencia de señales (Escenario 3 con
  Neutral vs. Z-Score), datos extremos (precio alto + capital bajo simultáneos).
- **Auditoría de capas**: confirmado por construcción que ninguna estrategia conoce
  `PortfolioState`/`Cash`/`Sizing`; confirmado que el motor no altera `Side` de ninguna orden
  emitida.
- **126/126 tests de producción** sin cambio durante toda la validación.
- **Hash congelado de Caso 1** (`A48CCC57DA1919F533F4D532FDC0F945705681DCDA813B385BBFE7F44F40998E`)
  verificado idéntico.

Evidencia completa, sección por sección, en `AUDITORIA_PRUEBA_INTEGRAL_SISTEMA_V1.md` — este
documento no la reproduce íntegra, la consolida y la enmarca administrativamente.

---

## 4. Hallazgo detectado y resuelto durante la validación

Un defecto en `GeneradorDatasetSintetico.Escenario1Alcista`/`Escenario2Bajista` (crecimiento lineal
de volumen que nunca cruzaba el umbral `1.5×` de D-105) fue detectado, aislado matemáticamente,
documentado y corregido — siguiendo estrictamente el ciclo detectar → aislar → documentar → decidir
exigido para esta validación, sin corrección automática. **No fue un defecto del sistema auditado**
(motor, estrategias, `src/`) — fue un defecto del propio arnés de prueba de esta validación,
distinguido explícitamente como tal (mismo criterio que Caso 4.2 al distinguir un defecto de test
fake de una limitación real del motor). No abrió ninguna decisión D-N. Detalle completo en
`AUDITORIA_PRUEBA_INTEGRAL_SISTEMA_V1.md` §6.

---

## 5. Límites — qué esta validación NO demuestra

Explícito, para que ningún documento futuro cite esta validación más allá de lo que prueba:

- **No valida mercado real**: los 5 escenarios son sintéticos, construidos por diseño para
  ejercitar condiciones específicas (tendencia, lateralidad, quiebre de régimen) — no son una
  muestra ni una simulación de comportamiento real de ningún instrumento.
- **No valida ejecución real**: no hay latencia, slippage real de mercado, ni interacción con
  ningún broker — el modelo de costes es el mismo modelo experimental abstracto ya usado en Caso 2.
- **No valida brokers ni conectividad**: el laboratorio no tiene, y esta validación no ejercita,
  ninguna capa de conexión a un exchange o broker real.
- **No prueba rentabilidad futura**: ningún resultado financiero de esta validación (`CashFinal`,
  `PnLTotal`, etc.) constituye evidencia de que alguna estrategia o configuración vaya a ser
  rentable bajo condiciones de mercado reales — los escenarios están diseñados para ejercitar
  mecanismos, no para representar dinámica de mercado plausible.
- **No sustituye forward testing**: ni paper trading ni testing sobre datos reales fuera de
  muestra. Esta validación es exclusivamente de integridad estructural del sistema.
- **No cubre Caso 5A** (ver §2) — el framework de gestores de riesgo intercambiables tiene su
  propia evidencia, separada, no incluida en esta batería.
- **No es exhaustiva**: la matriz es dirigida (16 de 30 combinaciones posibles
  escenario×estrategia), cada combinación elegida por qué capacidad específica ejercita, no por
  cobertura combinatoria completa.

---

## 6. Resultado administrativo

**Estado: Sistema experimental consolidado hasta Caso 4 / Caso 3B** (`caso1-v1-experimental`,
`caso2-v1-experimental`, `caso3a-v1-experimental`, `caso3b-v1-experimental`,
`caso4-v1-experimental`) — confirmado coherente end-to-end bajo condiciones sintéticas
deterministas y reproducibles, sin contradicciones no resueltas. **Caso 5A queda fuera de esta
consolidación** (§2) — su evidencia de cierre es independiente
(`caso5/AUDITORIA_CASO5A_V1.md`, 10/10 pruebas).

Esta validación no invalida ni reabre ninguna decisión D-001 a D-111. No modifica `src/`,
`tests/`, `IStrategy`, ni ningún baseline congelado — confirmado por `git status --porcelain -- src/
tests/` vacío durante todo su ciclo de ejecución original.

---

## Fuera de alcance de este documento

No se implementa código. No se ejecuta ninguna corrida nueva. No se extiende la matriz para cubrir
Caso 5A. No se abre ninguna decisión D-N. No se decide la siguiente fase de evolución (Caso 5B,
gestión avanzada de exposición, Caso 3C) — eso corresponde a un documento de propuesta separado.

---

## Criterio de cierre

- ✓ Objetivo declarado explícitamente (qué se validó, qué no se buscó medir).
- ✓ Fases cubiertas listadas con precisión verificada contra la matriz real ejecutada — corrección
  de la imprecisión del título original ("Caso 1 a Caso 4" → en realidad incluye Caso 3B).
- ✓ Caso 5A explícitamente excluido de la cobertura, sin ambigüedad.
- ✓ Evidencia consolidada y referenciada a su fuente completa, sin duplicar el detalle íntegro.
- ✓ Hallazgo de la propia validación (defecto del generador sintético) documentado, distinguido
  del sistema auditado.
- ✓ Límites declarados explícitamente — qué esta validación NO demuestra, incluyendo su no
  cobertura de Caso 5A.
- ✓ Resultado administrativo declarado sin ambigüedad de alcance.
- ⏳ Pendiente de tu revisión antes de commit/tag (si corresponde) y antes de abrir la auditoría de
  capacidad comparativa de Caso 5B.
