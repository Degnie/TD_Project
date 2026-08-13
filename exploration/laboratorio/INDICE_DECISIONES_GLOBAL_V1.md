# Índice Global de Decisiones — D-001 a D-122

Estado: **documento de referencia — actualizado tras la incorporación del dataset temporal 2022
(Caso 5C, D-121/D-122)**. Consolida en un único lugar todas las decisiones numeradas de Caso 1
(`AUDITORIA_FINAL_CASO1_V1.md` §1), Caso 2 (`modelo_financiero/DECISIONES_MODELO_ECONOMICO_V1.md`),
Caso 3A (`DECISIONES_CASO3_V1.md`), Caso 3B (`DECISIONES_CASO3B_V1.md`), Caso 4
(`DECISIONES_CASO4_V1.md`, `DECISIONES_UNIDADES_EXPOSICION_CASO4_V1.md`,
`DECISIONES_VALIDADOR_CAPACIDAD_CASO4_V1.md`), Caso 5A (`caso5/DECISIONES_CASO5_V1.md`), Caso 5B
(`caso5/DECISIONES_CASO5B_V1.md`), Caso 5C Capa 1 (`caso5/DECISIONES_CASO5C_V1.md`) y la expansión
de diversidad de evidencia de Caso 5C (`caso5/DECISIONES_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md`,
`caso5/DECISIONES_RANGO_ALTERNATIVO_CASO5C_V1.md`), evitando que abrir una fase nueva requiera
rastrear varios documentos distintos para saber si un tema ya fue decidido. No redefine ni
reinterpreta ninguna decisión — cada fila remite al documento de origen, que sigue siendo la fuente
autoritativa.

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

## Caso 3B — Generalización experimental, composición jerárquica (D-099 a D-107)

Congelado como **V1 Experimental** (tag `caso3b-v1-experimental`), independiente de
`caso3a-v1-experimental` — mismo objetivo general (generalización experimental de estrategias),
experimentos distintos. Fuente completa: `DECISIONES_CASO3B_V1.md`, auditoría en
`caso3/AUDITORIA_CASO3B_V1.md`, congelamiento en `caso3/VERSION_EXPERIMENTAL_CASO3B_V1.md`.

Retoma el Candidato E (señal multi-condición), evaluado y diferido en
`EVALUACION_SEGUNDA_FAMILIA_CASO3_V1.md` durante la selección de la segunda familia de Caso 3A.

| D-N | Establece | Estado |
|---|---|---|
| D-099 | Semántica de multi-condición: jerárquica — primaria habilita evaluación de secundaria (Opción C) | 🟢 Congelada |
| D-100 | Representación interna: objetos internos de condición con estado propio | 🟢 Congelada |
| D-101 | Observabilidad estructural, derivada de D-100, sin metadata nueva en `IStrategy` | 🟢 Congelada |
| D-102 | Familia concreta: Candidato H — volumen (contexto) + precio (breakout) | 🟢 Congelada |
| D-103 | Condiciones: P3 (múltiplo fijo sobre ventana) + S2 (ruptura de rango/breakout) | 🟢 Congelada |
| D-104 | `EstrategiaVolumenBreakout`: objetos con estado propio, callback existente, sin martingala/1 posición | 🟢 Congelada |
| D-105 | Parámetros: `N=20`, múltiplo `1.5×`, extremos excluyen vela actual, operador estricto — ampliada a bidireccional tras D-107 | 🟢 Congelada |
| D-106 | Especificación de implementación y pruebas | 🟢 Congelada |
| D-107 | Cierre por señal contraria — misma jerarquía evaluada en sentido opuesto a la posición abierta | 🟢 Congelada |

### Pendientes de Caso 3B (deuda técnica, no bloqueante)

| D-N | Qué queda pendiente | Impacto | ¿Bloquea fase siguiente? |
|---|---|---|---|
| — | Tercer nivel jerárquico / condiciones adicionales | 🟢 Bajo | No — D-100 rechazó diseñar para N niveles sin necesidad demostrada; candidato de fase futura si se justifica |
| D-055 | No activada adicionalmente — mismo perfil sin martingala ya cubierto por Z-Score/Neutral | 🟢 Bajo | No — postergar |
| D-044 | No activada — Caso 3B no estudia interacción estrategia/régimen | 🟢 Bajo | No — candidato de fase futura si se estudia esa interacción |
| D-084/Caso 4 | No activada dentro de Caso 3B — `GestorCapital`/sizing no interviene en esta familia | — | Solo si una fase futura combina generalización de estrategia con sizing avanzado |

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

## Caso 5A — Evaluación comparativa de gestores de riesgo (D-108 a D-111)

Congelado como **V1 Experimental** (tag `caso5a-v1-experimental`, commit `d923002`). Primera
fase que modifica `src/` desde Caso 4 — capacidad transversal de gestión de capital intercambiable,
no una nueva familia de estrategias. Fuente completa: `caso5/DECISIONES_CASO5_V1.md`, especificación
en `caso5/ESPECIFICACION_IMPLEMENTACION_GESTORES_RIESGO_V1.md`, auditoría en
`caso5/AUDITORIA_CASO5A_V1.md`, congelamiento en `caso5/VERSION_EXPERIMENTAL_CASO5A_V1.md`.

Retoma el framework de gestores intercambiables del mapa de evolución V3 (Caso 5A), distinto del
framing original de "gestión de exposición/límites/drawdown" propuesto inicialmente para "Caso 5" —
ese framing queda diferido a una fase posterior distinta (ver exclusiones en
`VERSION_EXPERIMENTAL_CASO5A_V1.md`).

| D-N | Establece | Estado |
|---|---|---|
| D-108 | Aislamiento cálculo/clasificación: `IGestorRiesgo` (único método, calcula cantidad) + `GestorCapital` orquesta, conserva D-092/D-095 sin duplicar | 🟢 Congelada |
| D-109 | `ConfiguracionSizing` describe una elección (`GestorActivo: IGestorRiesgo`), no un enum de tipos — precisión derivada: identidad experimental separada vía `IIdentidadGestorRiesgo`, no forma parte del contrato funcional del gestor | 🟢 Congelada |
| D-110 | Alcance inicial: Fixed Fractional (control) → Fixed Risk → Volatility Sizing; Kelly/Masaniello diferidos, comparten el bloqueo metodológico de Caso 2.3 | 🟢 Congelada |
| D-111 | Métricas de comparación por categoría — `ProfitFactor`/`CapitalLibreMinimo`/`MargenMaximoUtilizado`(=`ExposicionMaxima`) implementadas; `RachaPositivaMaxima`/duración de drawdown/riesgo de ruina diferidas, no bloqueantes | 🟢 Congelada |

### Pendientes de Caso 5A (deuda técnica, no bloqueante)

| D-N | Qué queda pendiente | Impacto | ¿Bloquea fase siguiente? |
|---|---|---|---|
| D-110 | Kelly fraccionado/Masaniello — comparten el bloqueo de probabilidad-de-acierto de Caso 2.3 | 🟡 Medio | Sí, para incluirlos — requiere resolver primero la fuente de esa probabilidad como valor fijo declarado |
| D-111 | `RachaPositivaMaxima` — requiere tocar `PerfilMultiTf.cs`, fuera del alcance autorizado de Caso 5A | 🟢 Bajo | No — candidato de una fase que amplíe capacidades de análisis operacional |
| D-111 | Duración de drawdown, riesgo de ruina — sin fuente de dato tan directa como las métricas ya implementadas | 🟢 Bajo | No — postergar hasta tener definición formal |
| — | `LaboratorioSintetico.csproj` no compila — falla preexistente, no causada por Caso 5A (verificado por `git log`) | 🟢 Bajo | No — corrección de build compartido, fuera del alcance de cualquier Caso individual |
| — | Sistema recomendador de gestores (Caso 5B), gestión avanzada de exposición/límites, portfolio multi-instrumento | — | No bloquean Caso 5A — son fases futuras distintas, condicionadas a que Caso 5A produzca evidencia comparativa primero |

---

## Caso 5B — Capa comparativa de gestores de riesgo (D-112 a D-115)

Congelado como **V1 Experimental** (tag `caso5b-v1-experimental`, commit `633fea7`).
Dependiente de `caso5a-v1-experimental` — consume exclusivamente su infraestructura
(`IGestorRiesgo`/`IIdentidadGestorRiesgo`/gestores concretos/identidad económica), sin modificar
ninguno de sus archivos. A diferencia de Caso 5A, **no toca `src/`** — toda la implementación vive
en `exploration/laboratorio/caso5/`. Fuente completa: `caso5/DECISIONES_CASO5B_V1.md`,
especificación en `caso5/ESPECIFICACION_IMPLEMENTACION_COMPARADOR_GESTORES_V1.md`, auditoría en
`caso5/AUDITORIA_CASO5B_V1.md`, congelamiento en `caso5/VERSION_EXPERIMENTAL_CASO5B_V1.md`.

Origen: `caso5/AUDITORIA_CAPACIDAD_COMPARATIVA_V1.md` — verificó contra código que no existía
ningún componente que comparara múltiples gestores de riesgo bajo una misma estrategia/dataset
antes de abrir esta fase.

| D-N | Establece | Estado |
|---|---|---|
| D-112 | Ubicación arquitectónica: `ComparadorGestores`, componente nuevo de laboratorio — mismo patrón conceptual que `ComparadorMultiTimeframe`, sin dependencia de código; no toca `EjecutorProtocolo`/`EntradaProtocolo` | 🟢 Congelada |
| D-113 | Control experimental por construcción: `Comparar(entradaBase, gestores)` garantiza que el único eje que varía entre corridas es el gestor, vía `entradaBase with { Sizing = ... }` | 🟢 Congelada |
| D-114 | Fuente de datos: `MetricasFinancieras` exclusivamente — `ReporteOperacional` excluido explícitamente por su acoplamiento a martingala (D-055) | 🟢 Congelada |
| D-115 | Salida estructurada (`ResultadoComparativoGestores`) + render derivado (`RenderizadorComparacionGestores`), sin ranking ni "mejor gestor" — diferencia deliberada del precedente | 🟢 Congelada |

### Pendientes de Caso 5B (deuda técnica, no bloqueante)

| D-N | Qué queda pendiente | Impacto | ¿Bloquea fase siguiente? |
|---|---|---|---|
| — | Sistema recomendador de gestores ("Caso 5C" o equivalente) | — | No bloquea Caso 5B — condicionado a que exista evidencia comparativa acumulada suficiente antes de proponerse |
| — | Comparación multi-timeframe/multi-estrategia — `Comparar` exige exactamente 1 timeframe en `entradaBase` | 🟢 Bajo | No — alcance futuro explícito, no resuelto en esta fase |
| D-110 | Kelly fraccionado/Masaniello — heredado sin cambios desde Caso 5A, no reabierto por Caso 5B | 🟡 Medio | Igual que en Caso 5A — requiere resolver primero la fuente de probabilidad-de-acierto |

---

## Caso 5C Capa 1 — Persistencia de evidencia comparativa (D-116, D-117; D-118 a D-120 a nivel de principio)

Congelado como **V1 Experimental** (tag `caso5c-capa1-v1-experimental`, pendiente de commit).
Dependiente de `caso5b-v1-experimental` — envuelve exclusivamente su salida
(`ComparadorGestores.Comparar`/`RenderizadorComparacionGestores.Generar`), sin modificar ninguno de
sus archivos. Igual que Caso 5B, **no toca `src/`** — toda la implementación vive en
`exploration/laboratorio/caso5/`. Fuente completa: `caso5/PROPUESTA_CASO5C_V1.md`,
`caso5/DECISIONES_CASO5C_V1.md`, especificación en
`caso5/ESPECIFICACION_IMPLEMENTACION_PERSISTENCIA_EVIDENCIA_V1.md`, auditoría en
`caso5/AUDITORIA_CASO5C_CAPA1_V1.md`, congelamiento en
`caso5/VERSION_EXPERIMENTAL_CASO5C_CAPA1_V1.md`.

Origen: `PROPUESTA_CASO5C_V1.md` §0 — verificó contra código que no existía ningún corpus
acumulado de comparaciones antes de abrir esta fase; `ComparadorGestores`/
`RenderizadorComparacionGestores` (Caso 5B) nunca escriben a disco.

**Capa 1 y Capa 2 se resolvieron en la misma ronda de decisiones (D-116 a D-120), pero solo Capa 1
se implementó y congela en esta versión** — separación justificada por el principio D-030 (nunca
introducir reglas/umbrales calibrados sin evidencia experimental sobre la que calibrarlos).

| D-N | Establece | Estado |
|---|---|---|
| D-116 | Persistencia separada: `PersistidorComparaciones` envuelve `ComparadorGestores` sin modificarlo — extensión directa del patrón `protocolo/resultados/` | 🟢 Congelada, implementada |
| D-117 | Insumo válido para análisis futuro: campos ya presentes en identidad/`MetricasFinancieras`; régimen de mercado y datos externos excluidos | 🟢 Congelada, implementada (define el insumo; ningún análisis lo consume todavía) |
| D-118 | Semántica de "recomendar": selección automática excluida por rol del sistema; sugerencia/orden explícito quedan vivas para Capa 2 | 🟡 Congelada a nivel de principio — sin implementar |
| D-119 | Umbral de suficiencia de evidencia: "sin evidencia suficiente → no recomendar", sin valores numéricos fijados | 🟡 Congelada a nivel de principio — sin implementar |
| D-120 | Formato de `RecomendacionExperimental` (`Contenido`/`CriterioUsado`/`EvidenciaUsada`/`Limitaciones` obligatorios) | 🟡 Congelada a nivel de principio — sin implementar |

### Pendientes de Caso 5C Capa 1 (deuda técnica, no bloqueante)

| D-N | Qué queda pendiente | Impacto | ¿Bloquea fase siguiente? |
|---|---|---|---|
| D-118/D-119/D-120 | Capa 2 completa (análisis/recomendación) — sin implementar | — | No bloquea el uso de Capa 1 — condicionada a que exista corpus real acumulado antes de proponerse |
| — | Índice o explorador del corpus persistido — cada `Persistir` es independiente, sin conocimiento de ejecuciones anteriores | 🟢 Bajo | No — candidato natural de Capa 2 o de una utilidad intermedia, no decidido aquí |
| D-110 | Kelly fraccionado/Masaniello — heredado sin cambios desde Caso 5A/5B | 🟡 Medio | Igual que en fases anteriores — requiere resolver primero la fuente de probabilidad-de-acierto |

---

## Caso 5C — Diversidad de evidencia (D-121, D-122)

**No es una sub-fase congelada como V1 Experimental** — es una expansión de la evidencia
disponible para Caso 5C, ejecutada tras `AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V2.md` §5 (concluyó
que la limitación de dataset único no es resoluble con más campañas sobre el mismo rango). Fuente
completa: `caso5/PROPUESTA_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md`,
`caso5/DECISIONES_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md` (D-121),
`caso5/HALLAZGO_RECHAZO_DATASET_2023_CASO5C_V1.md`,
`caso5/DECISIONES_RANGO_ALTERNATIVO_CASO5C_V1.md` (D-122),
`caso5/ESPECIFICACION_IMPLEMENTACION_EXPLORACION_DISPONIBILIDAD_CASO5C_V1.md`,
`caso5/ESPECIFICACION_IMPLEMENTACION_DIVERSIDAD_TEMPORAL_CASO5C_V1.md`,
`caso5/HALLAZGO_DATASET_TEMPORAL_2022_CASO5C_V1.md`.

| D-N | Establece | Estado |
|---|---|---|
| D-121 | Vía B (tiempo) antes que Vía A (instrumento) — criterio decisivo: capacidad de atribución causal, variar una sola dimensión por expansión (mismo principio que D-113) | 🟢 Congelada |
| D-122 | Selección de rango alternativo tras rechazo de 2023: exploración de disponibilidad por bloques mensuales (`ExploradorDisponibilidad`, sin escribir a disco) antes de comprometerse a una descarga completa | 🟢 Congelada, implementada |

**Resultado material de esta expansión**: dataset `BTCUSDT 2022-01-01 – 2023-01-01` descargado,
validado (0 huecos/duplicados/errores, `ValidadorIntegridadDatos`), congelado en
`datasets/reales/BTCUSDT/1m_2022/` (+ 12 timeframes derivados), commit `77e69d4`. El dataset
`2024-2025` original permanece intacto — ambos coexisten sin mezcla. **Ninguna campaña ni
comparación de gestores se ejecutó todavía sobre el dataset 2022** — queda como paso siguiente,
condicionado a autorización explícita.

### Pendientes de esta expansión

| D-N | Qué queda pendiente | Impacto | ¿Bloquea fase siguiente? |
|---|---|---|---|
| — | Sub-campaña D (18 comparaciones sobre el dataset 2022, ya especificada en `ESPECIFICACION_IMPLEMENTACION_DIVERSIDAD_TEMPORAL_CASO5C_V1.md` §5) — no ejecutada | — | No bloquea nada — es el siguiente paso natural, pendiente de autorización |
| — | Vía A (instrumento, D-121) — pospuesta, no descartada; si se abre, debe ejecutarse contra el rango temporal original (2024-2025), no el 2022, para preservar atribución causal | — | No bloquea — condicionada a evaluar primero los resultados de la Vía B |

---

## Evaluación de bloqueo — ¿alguna deuda impide usar Caso 1/Caso 2/Caso 3A/Caso 3B/Caso 4/Caso 5A/Caso 5B/Caso 5C Capa 1 como referencia estable?

**No.** Ninguna deuda listada invalida la identidad, reproducibilidad o separación de capas ya
congeladas en `caso1-v1-experimental`/`caso2-v1-experimental`/`caso3a-v1-experimental`/
`caso3b-v1-experimental`/`caso4-v1-experimental`/`caso5a-v1-experimental`/`caso5b-v1-experimental`/
`caso5c-capa1-v1-experimental`. La deuda de impacto alto que existía (D-084) fue resuelta de punta
a punta en Caso 4, junto con D-085 — ninguna deuda de impacto alto permanece abierta. La expansión
de diversidad de evidencia (D-121/D-122) no introduce deuda nueva — el dataset 2022 está validado y
congelado, solo falta ejecutar la campaña que lo consume.

**Deuda a resolver solo si una fase futura la toca directamente**:
- D-055 — si una fase futura rediseña el catálogo de métricas o introduce más familias sin
  martingala.
- D-044 — si una fase futura estudia interacción estrategia/régimen.
- Modo estricto de `ValidadorCapacidad` — si una fase futura introduce políticas de riesgo o
  bloqueo automático.
- Tercer nivel jerárquico/condiciones adicionales (Caso 3B) — si una fase futura extiende la
  composición de condiciones.
- D-110 (Kelly/Masaniello) — si una fase futura resuelve la fuente de probabilidad-de-acierto como
  valor fijo declarado.
- D-111 (`RachaPositivaMaxima`/duración de drawdown/riesgo de ruina) — si una fase futura amplía
  `PerfilMultiTf.cs` o define formalmente "ruina"/duración de drawdown.
- Comparación multi-timeframe/multi-estrategia (Caso 5B) — si una fase futura extiende
  `ComparadorGestores` a más de un eje simultáneo.
- D-118/D-119/D-120 (Capa 2 de Caso 5C) — si el corpus generado por Capa 1 se evalúa suficiente
  para diseñar análisis/recomendación.

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
| `caso4-v1-experimental` | `6594c9e` | Evolución financiera, D-091 a D-098 (resuelve D-084/D-085) |
| `caso3b-v1-experimental` | `282e307` | Composición jerárquica de condiciones, D-099 a D-107 |
| `caso5a-v1-experimental` | `d923002` | Gestores de riesgo intercambiables, D-108 a D-111 |
| `caso5b-v1-experimental` | `633fea7` | Capa comparativa de gestores de riesgo, D-112 a D-115 |
| `caso5c-capa1-v1-experimental` | _(pendiente)_ | Persistencia de evidencia comparativa, D-116/D-117 (D-118 a D-120 a nivel de principio) |

---

## Próximo documento

Ninguno abierto todavía. Caso 5C Capa 1 congelada (tag `caso5c-capa1-v1-experimental`); dataset
temporal `BTCUSDT 2022` validado y congelado (commit `77e69d4`, D-121/D-122), sin ejecutar todavía
la sub-campaña que lo consume. Próximo paso natural: autorizar la Sub-campaña D (18 comparaciones
sobre 2022, misma matriz ya usada para 2024-2025) y su auditoría posterior — antes de eso, la
pregunta de si el corpus acumulado justifica abrir Caso 5C Capa 2 (análisis/recomendación) sigue
sin evaluarse. Caso 3C / gestión avanzada de exposición / Vía A (instrumento, D-121) siguen como
alternativas no decididas.
