# Índice Global de Decisiones — D-001 a D-098

Estado: **documento de referencia — actualizado al cierre de Caso 4**. Consolida en un único
lugar todas las decisiones numeradas de Caso 1 (`AUDITORIA_FINAL_CASO1_V1.md` §1), Caso 2
(`modelo_financiero/DECISIONES_MODELO_ECONOMICO_V1.md`), Caso 3A (`DECISIONES_CASO3_V1.md`) y
Caso 4 (`DECISIONES_CASO4_V1.md`, `DECISIONES_UNIDADES_EXPOSICION_CASO4_V1.md`,
`DECISIONES_VALIDADOR_CAPACIDAD_CASO4_V1.md`), evitando que abrir una fase nueva requiera rastrear
varios documentos distintos para saber si un tema ya fue decidido. No redefine ni reinterpreta
ninguna decisión — cada fila remite al documento de origen, que sigue siendo la fuente autoritativa.

**Regla vigente en todo el proyecto**: un identificador `D-N` nunca se reasigna a contenido
distinto del originalmente registrado — confirmado y aplicado dos veces (D-043/D-053 en Caso 1,
D-072/D-074 en Caso 2).

---

## Caso 1 — Laboratorio de estrategias (D-001 a D-056)

Congelado como **V1 Experimental** (tag `caso1-v1-experimental`). Fuente completa:
`protocolo/AUDITORIA_FINAL_CASO1_V1.md` §1.

### Arquitectura (estructura del sistema, capas, separación de responsabilidades)

| D-N | Establece | Estado |
|---|---|---|
| D-008 | Separación permanente Motor→Analizador→Reporte | 🟢 Congelada |
| D-009 | Métricas financieras fuera del analizador operacional (pertenecen a Caso 2) | 🟢 Congelada |
| D-015 | Arquitectura por capas: cada analizador consume la salida ya calculada, sin recalcular | 🟢 Congelada |
| D-017 | Versionado de artefactos: cambio de criterio = nueva versión, nunca edición in-place | 🟢 Congelada |
| D-035 | Clasificadores oficiales no eliminan a los experimentales — coexisten | 🟢 Congelada |
| D-038 | Una operación conserva ambos regímenes (entrada y resolución) | 🟢 Congelada |
| D-039 | Separación de capas: asignación de contexto ≠ cálculo de métricas | 🟢 Congelada |
| D-040 | Instrumentación de `TimestampEntrada`/`TimestampResolucion` | 🟢 Congelada |
| D-046 | Versionado de reporte: V2 sustituye V1 sin editarlo in-place | 🟢 Congelada |
| D-048 | Alcance del reporte final: resumen + anexos por corrida | 🟢 Congelada |
| D-049 | Identidad experimental compuesta (`IdentidadExperimentoCompleta`, hash SHA256) | 🟢 Congelada |
| D-051 | Anexos son fuente primaria de detalle; el resumen nunca replica contenido | 🟢 Congelada |
| D-052 | Metadata de versión del clasificador es pura, sin lógica | 🟢 Congelada |
| D-053 | Carpeta única por ejecución (`{Estrategia}_{timestamp}/`) | 🟢 Congelada (discrepancia de registro corregida, ver §1.5 de la auditoría) |

### Metodológicas (reglas de proceso)

| D-N | Establece | Estado |
|---|---|---|
| D-005 | Dependencia de martingala: solo porcentaje, sin clasificación cualitativa no validada | 🟢 Congelada |
| D-006 | Orden obligatorio: Dataset→Clasificador→Segmentos→Evaluación | 🟢 Congelada |
| D-007 | Distinción permanente "timeframe disponible" ≠ "timeframe evaluado" | 🟢 Congelada |
| D-010 | Tamaño de muestra obligatorio en toda comparación | 🟢 Congelada |
| D-014 | Sin ranking implícito entre timeframes | 🟢 Congelada |
| D-016 | Prohibición de clasificar régimen usando conocimiento de construcción sintética | 🟢 Congelada |
| D-021 | Selección de familia de clasificador vía comparación experimental | 🟢 Congelada |
| D-022 | Parámetros exploratorios no son oficiales — etiquetado obligatorio | 🟢 Congelada |
| D-023 | Selección del clasificador oficial es decisión separada y posterior | 🟢 Congelada |
| D-024 | Estabilidad se mide incluyendo variación entre timeframes | 🟢 Congelada |
| D-025 | Un clasificador sin discriminación de régimen no se evalúa por una métrica aislada | 🟢 Congelada |
| D-026 | Métricas dependientes de escala deben normalizarse antes de comparar | 🟢 Congelada |
| D-029 | Ningún clasificador es oficial sin cumplir el modelo de estados aprobado | 🟢 Congelada |
| D-030 | Parámetros con referencia externa pueden entrar como "Propuesto" sin calibrar | 🟢 Congelada |
| D-032 | Procedimiento de calibración fijado antes de calcular el valor | 🟢 Congelada |
| D-036 | Asignación operación→régimen por coincidencia exacta de timestamp | 🟢 Congelada |
| D-041 | "Sin régimen asignable" es categoría propia, distinta de "Ambiguo" | 🟢 Congelada |
| D-042 | Implementación de D-036: `InicioUtcMs` exacto, sin tolerancia | 🟢 Congelada |
| D-043 | Asignación de régimen no define la métrica de agrupación | 🟢 Congelada (discrepancia de registro corregida, ver §1.5 de la auditoría) |
| D-045 | "Peores escenarios" queda fuera del catálogo segmentado por régimen | 🟢 Congelada |
| D-047 | Reporte de escenarios sin conclusión comparativa ni ranking entre regímenes | 🟢 Congelada |
| D-050 | El resumen consolidado no muestra una cifra única si depende del timeframe | 🟢 Congelada |
| D-054 | EMA Cross valida generalidad del pipeline, nunca evalúa rentabilidad | 🟢 Congelada |
| D-056 | Aprobación de `AUDITORIA_FINAL_CASO1_V1.md`, autoriza deuda técnica y versión experimental | 🟢 Congelada |

### Producto (contenido/presentación, qué se congela como oficial)

| D-N | Establece | Estado |
|---|---|---|
| D-001 | Baseline Experimental V1 congelado — cambios exigen V2 | 🟢 Congelada |
| D-002 | ROI real, Sharpe, riesgo monetario, costes, Masaniello fuera de alcance de Caso 1 | 🟢 Congelada |
| D-003 | Tipo de estrategia: Patrón vs. Tendencia | 🟢 Congelada |
| D-004 | Plantilla de ficha de catálogo de estrategia (10 secciones) | 🟢 Congelada |
| D-028 | Modelo oficial de 4 estados de régimen: Alcista/Bajista/Lateral/Ambiguo | 🟢 Congelada |
| D-031 | Método de `SesgoDI`: relativo, `\|DI+-DI-\|/(DI++DI-)` | 🟢 Congelada |
| D-033 | Valor congelado `UmbralSesgoDI = 0.153467` | 🟢 Congelada |
| D-034 | `ClasificadorRegimenV1` congelado (4 estados + parámetros) | 🟢 Congelada |
| D-037 | Nota obligatoria de correlación≠causalidad en reportes con régimen | 🟢 Congelada |

### Pendientes de Caso 1 (deuda técnica, no bloqueante)

| D-N | Qué queda pendiente | Impacto | ¿Bloquea Caso 3? |
|---|---|---|---|
| D-011 | Métrica principal de comparación multi-timeframe distinta de Eficiencia operacional | 🟢 Bajo | No — postergar |
| D-012 | Umbral de "muestra reducida" | 🟢 Bajo | No — postergar |
| D-013 | Extender cobertura de timeframes evaluados | 🟢 Bajo | No — postergar |
| D-018 | Umbral numérico de régimen fuera de ADX/SesgoDI (clasificadores no congelados) | 🟢 Bajo | No — postergar |
| D-019 | Tamaño de ventana de clasificación | 🟢 Bajo | No — postergar |
| D-020 | Categoría "Indeterminado" distinta de "Ambiguo" | 🟢 Bajo | No — postergar |
| D-044 | Dimensión principal de agrupación del reporte por régimen (parcialmente evitado por D-048) | 🟡 Medio | No — candidato de Caso 3 si estudia interacción estrategia/régimen |
| D-055 | Catálogo de métricas asume martingala universalmente (confirmado por EMA Cross) | 🟡 Medio | Depende — resolver si Caso 3 introduce nuevas familias de estrategias |

---

## Caso 2 — Modelo financiero (D-057 a D-085)

Congelado como **V1 Experimental** (tag `caso2-v1-experimental`). Fuente completa:
`modelo_financiero/DECISIONES_MODELO_ECONOMICO_V1.md`.

### Modelo económico base — Caso 2.1 (D-057 a D-062)

| D-N | Establece | Estado |
|---|---|---|
| D-057 | `TasaMargen` pertenece al `Instrumento` | 🟢 Congelada |
| D-058 | Unidad económica = monetaria abstracta, no USDT real | 🟢 Congelada |
| D-059 | Restricción de capacidad: registrar incapacidad, no bloquear | 🟢 Congelada |
| D-060 | Evaluación de capacidad antes de aplicar la orden | 🟢 Congelada |
| D-061 | Compatibilidad de contratos: parámetro opcional con default histórico | 🟢 Congelada |
| D-062 | Propagación de `tasaMargen` a `ResolutorVela` — corrige divergencia Equity/Cash | 🟢 Congelada |

### Modelo de costes — Caso 2.2 (D-063 a D-065)

| D-N | Establece | Estado |
|---|---|---|
| D-063 | Componentes de coste en V1: Comisión + Slippage (sin spread/funding) | 🟢 Congelada |
| D-064 | Origen del parámetro de coste: Experimento, no Instrumento | 🟢 Congelada |
| D-065 | Aplicación del coste al Cash (modifica Cash/Equity, PnL neto) | 🟢 Congelada |

### Gestión de capital — Caso 2.3 (D-066 a D-071)

| D-N | Establece | Estado |
|---|---|---|
| D-066 | Responsable del cálculo de tamaño: capa externa `GestorCapital` | 🟢 Congelada |
| D-067 | Modelo de sizing V1: porcentaje de `Cash − Margin` | 🟢 Congelada |
| D-068 | `GestorCapital` propone, `ValidadorCapacidad` valida | 🟢 Congelada |
| D-069 | Sizing nuevo en una estrategia = nueva versión experimental | 🟢 Congelada |
| D-070 | Arquitectura del gestor de capital: capa intermedia | 🟢 Congelada |
| D-071 | `GestorCapital` transforma, nunca crea/elimina órdenes | 🟢 Congelada |

### Métricas financieras — Caso 2.4 (D-072 a D-078)

| D-N | Establece | Estado |
|---|---|---|
| D-072 | Capital inicial expuesto desde `ConfiguracionExperimento`, en reportes | 🟢 Congelada |
| D-073 | Drawdown de equity, porcentual, sobre `EquityCurve` | 🟢 Congelada |
| D-074 | Duración del drawdown | 🟡 Definida conceptualmente, no implementada |
| D-075 | Exposición máxima = `Max(PortfolioSnapshot.Margin)` | 🟢 Congelada |
| D-076 | Métricas comparativas: tabla sin ranking | 🟢 Congelada |
| D-077 | Fuente oficial de datos: `EquityCurve`/`Cash`/`Margin`/`Trades` únicamente | 🟢 Congelada |
| D-078 | Métricas no disponibles = `null`, nunca `0` | 🟢 Congelada |

### Baseline financiero e identidad económica (D-079 a D-085)

| D-N | Establece | Estado |
|---|---|---|
| D-079 | Configuración financiera explícita en `EntradaProtocolo` | 🟢 Congelada |
| D-080 | Reporte financiero independiente (`ReporteFinancieroGenerador`), Caso 1 intacto | 🟢 Congelada |
| D-081 | Programa dedicado para el baseline financiero, sin tocar `protocolo/Program.cs` | 🟢 Congelada |
| D-082 | Identidad experimental: `HashCompuesto` + `HashConfiguracionEconomica` separados | 🟢 Congelada |
| D-083 | Calibración dimensional de `PorcentajeRiesgo` (no es valor recomendado) | 🟢 Congelada |
| D-084 | `GestorCapital` no distingue apertura/cierre de posición | ✅ Resuelta en Caso 4 (4.2) |
| D-085 | Escala económica histórica: `Cantidad=1` sin relación dimensional con `CapitalInicial` | ✅ Resuelta en Caso 4 (4.3) |

### Pendientes de Caso 2 (deuda técnica, no bloqueante salvo excepción)

| D-N | Qué queda pendiente | Impacto | ¿Bloquea Caso 3? |
|---|---|---|---|
| D-074 | Duración del drawdown | 🟢 Bajo | No — postergar |
| — | Spread/funding, sizing alternativo (Equity, Masaniello) | 🟢 Bajo | No — explícitamente excluidos de V1 |

D-084 y D-085 dejaron de ser deuda técnica de Caso 2 — resueltas de punta a punta en Caso 4 (ver
abajo). Se mantienen en la tabla de Caso 2 por ser su documento de origen; su estado final vive en
la sección de Caso 4.

---

## Caso 3A — Generalización experimental (D-086 a D-090)

Congelado como **V1 Experimental** (tag `caso3a-v1-experimental`). Fuente completa:
`DECISIONES_CASO3_V1.md`, auditoría en `caso3/AUDITORIA_CASO3A_V1.md`, congelamiento en
`caso3/VERSION_EXPERIMENTAL_CASO3A_V1.md`.

| D-N | Establece | Estado |
|---|---|---|
| D-086 | Alcance de generalización: 2 familias nuevas mínimo (Z-Score Reversal, Estrategia Neutral) | 🟢 Congelada |
| D-087 | Criterio de selección: máxima distancia estructural con lo ya probado | 🟢 Congelada |
| D-088 | Metadata de capacidades externa a `IStrategy`, no inferida desde resultados observados | 🟢 Congelada |
| D-089 | D-055 no bloquea inicio de Caso 3A; confirmado no bloqueante para su cierre | 🟢 Congelada |
| D-090 | Ubicación de la metadata: clase de laboratorio (`CaracteristicasEstrategia` en `EjecutorProtocolo.cs`) | 🟢 Congelada |

### Pendientes de Caso 3A (deuda técnica, no bloqueante)

| D-N | Qué queda pendiente | Impacto | ¿Bloquea fase siguiente? |
|---|---|---|---|
| D-055 | Catálogo de métricas de martingala sin rediseño completo (presentación "no aplica" sí implementada) | 🟡 Medio | No — postergar a una fase que rediseñe el catálogo si se justifica |
| D-044 | No activada — ninguna familia de Caso 3A estudia interacción estrategia/régimen | 🟢 Bajo | No — candidato de fase futura si se estudia esa interacción |
| D-084 | No activada dentro de Caso 3A — resuelta como corrección de motor en Caso 4 | — | Ver sección Caso 4 |

---

## Caso 4 — Evolución financiera (D-091 a D-098, resuelve D-084/D-085)

Congelado como **V1 Experimental** (tag `caso4-v1-experimental`). Fuente completa:
`DECISIONES_CASO4_V1.md`, `DECISIONES_UNIDADES_EXPOSICION_CASO4_V1.md`,
`DECISIONES_VALIDADOR_CAPACIDAD_CASO4_V1.md`, auditoría en `AUDITORIA_CASO4_3_UNIDADES_
EXPOSICION_V1.md` y `AUDITORIA_CASO4_V1.md`, congelamiento en `caso4/VERSION_EXPERIMENTAL_
CASO4_V1.md`.

### Arquitectura y clasificación de intención — Caso 4.1/4.2 (D-091/D-092, resuelve D-084)

| D-N | Establece | Estado |
|---|---|---|
| D-091 | Corrección en `src/` con activación experimental explícita, default histórico preservado (Opción C) | 🟢 Congelada |
| D-092 | Componente clasificador de intención separado, previo a `GestorCapital`, fuente = `PortfolioState`/`LotesVivos` (Opción 2) | 🟢 Congelada |

### Unidades, exposición y normalización — Caso 4.3 (D-093 a D-095, resuelve D-085)

| D-N | Establece | Estado |
|---|---|---|
| D-093 | `PorcentajeRiesgo` = fracción sobre margen requerido, no exposición nominal (Opción A) | 🟢 Congelada |
| D-094 | Precio de referencia para sizing = `Close` de la vela siguiente | 🟢 Congelada |
| D-095 | Intención de reducción/cierre prevalece sobre cantidad nominal — normalización previa a Cross-Zero | 🟢 Congelada |

### Observabilidad de incapacidades — Caso 4.4 (D-096 a D-098)

| D-N | Establece | Estado |
|---|---|---|
| D-096 | Exposición de incapacidades como observación/reporte, sin bloqueo (Opción A) | 🟢 Congelada |
| D-097 | Incapacidad = restricción económica observable, no error de orden ni orden inválida | 🟢 Congelada |
| D-098 | Aislamiento estructural: módulo satélite `caso4/Caso4.csproj` | 🟢 Congelada |

### Pendientes de Caso 4 (deuda técnica, no bloqueante)

| D-N | Qué queda pendiente | Impacto | ¿Bloquea fase siguiente? |
|---|---|---|---|
| — | Modo estricto de `ValidadorCapacidad` (bloqueo/rechazo por incapacidad) | 🟢 Bajo | No — deferred explícitamente en D-096, candidato de fase futura si se justifica |
| — | Calibración de `PorcentajeRiesgo`/`CapitalInicial` "razonables" por estrategia | 🟢 Bajo | No — fuera del objetivo de Caso 4 (corrección dimensional, no calibración) |
| — | Referencia obsoleta a D-085 en `ReporteFinancieroGenerador.cs` §6 ("no resuelta en Caso 2 V1") | 🟢 Bajo | No — deuda documental histórica, pendiente de mecanismo de errata, no de reapertura de Caso 2 |

---

## Evaluación de bloqueo — ¿alguna deuda impide usar Caso 1/Caso 2/Caso 3A/Caso 4 como referencia estable?

**No.** Ninguna deuda listada invalida la identidad, reproducibilidad o separación de capas ya
congeladas en `caso1-v1-experimental`/`caso2-v1-experimental`/`caso3a-v1-experimental`/
`caso4-v1-experimental`. La deuda de impacto alto que existía (D-084) fue resuelta de punta a
punta en Caso 4, junto con D-085 — ninguna deuda de impacto alto permanece abierta.

**Deuda a resolver solo si una fase futura la toca directamente**:
- D-055 — si una fase futura rediseña el catálogo de métricas o introduce más familias sin
  martingala.
- D-044 — si una fase futura estudia interacción estrategia/régimen.
- Modo estricto de `ValidadorCapacidad` — si una fase futura introduce políticas de riesgo o
  bloqueo automático.

**Deuda que permanece como límite conocido del modelo, sin fecha de resolución**:
D-011, D-012, D-013, D-018, D-019, D-020, D-074, spread/funding, sizing alternativo, referencia
documental obsoleta de D-085 en el reporte financiero.

---

## Tags de referencia

| Tag | Commit | Alcance |
|---|---|---|
| `caso1-v1-experimental` | `eaaddb5` | Laboratorio de estrategias, D-001 a D-056 |
| `caso2-v1-experimental` | `1f0f967` | Modelo financiero, D-057 a D-085 |
| `caso3a-v1-experimental` | `43852ab` | Generalización experimental, D-086 a D-090 |
| `caso4-v1-experimental` | _(pendiente)_ | Evolución financiera, D-091 a D-098 (resuelve D-084/D-085) |

---

## Próximo documento

Ninguno abierto todavía — Caso 4 cerrado, pendiente de commit/tag. Próxima fase (Caso 3B / Caso 5)
pendiente de decisión del auditor.
