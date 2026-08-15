# Changelog

Formato basado en [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Agregado
- **Validación de Resultados de Backtest y Control de Exposición V1 — RN-12, CU-15, RN-04,
  RNF-16** (activación condicional del bloqueo de capacidad para el flujo de cliente final,
  exposición de incapacidades y posiciones abiertas; línea `caso14/` en `exploration/laboratorio`):
  - `ConfiguracionExperimento.BloquearPorCapacidad: bool = false` (nuevo, opcional) — activa,
    solo cuando se solicita explícitamente, el rechazo atómico de órdenes sin capacidad que
    RN-12/CU-15 ya describían textualmente ("la orden se ajusta a la capacidad máxima permitida
    o se rechaza atómicamente"; "Falla validación preventiva (RN-12) → OrderRequestRejected").
    Antes de este cambio, `ValidadorCapacidad` estaba conectado pero solo observaba, nunca
    bloqueaba (D-059/D-060, decisión histórica de Fase 2D para el laboratorio interno). Default
    `false` preserva ese comportamiento exactamente, sin tocar ningún caller existente.
  - `BacktestRunner.Ejecutar`: `continue` condicional antes de `RegistradorOrdenes.Registrar`
    cuando `BloquearPorCapacidad=true` y la orden falla `ValidadorCapacidad.Validar` — la orden
    nunca se registra ni ejecuta, y nunca consume Secuencia Causal (RN-04: no hay hueco ni
    violación de monotonía, la orden simplemente no entra al conjunto de Órdenes registradas).
  - `RegistroIncapacidad.Bloqueada: bool = false` (nuevo) — distingue, dentro de `Incapacidades`,
    entre "se detectó pero se dejó pasar" (comportamiento histórico) y "se detectó y se impidió"
    (solo bajo `BloquearPorCapacidad=true`).
  - Sin ningún `EstadoBacktest` nuevo — RNF-09 fija los 6 estados como exclusivos; un bloqueo de
    capacidad es una condición válida del experimento, no una falla del sistema. `Estado`
    permanece `Success`, el bloqueo se comunica exclusivamente vía `Incapacidades`.
  - `ResultDto`: `Incapacidades` (lista de `IncapacidadDto`, nunca null) y `Exposicion`
    (`ExposicionFinalDto`: `CantidadNetaViva`, `MarginRetenido`, `UnrealizedPnL`, `PnLRealizado`,
    `ResultadoConPosicionesAbiertas`) — distingue explícitamente PnL realizado (Trades cerrados)
    de resultado incluyendo posiciones vivas al cierre (Equity, que ya incorpora UnrealizedPnL).
  - `ExplicacionDto`: `AdvertenciaPosicionesAbiertas`/`AdvertenciaIncapacidadCapital` (RNF-16,
    ambos opcionales) — texto en español poblado por `ResultDtoMapper` cuando corresponde, con
    el texto aprobado: "El resultado incluye posiciones abiertas al finalizar la simulación. La
    ganancia/pérdida final puede variar si esas posiciones fueran cerradas."
  - Activación: `BloquearPorCapacidad=true` únicamente en `POST /api/strategies/dsl/run` y
    `POST /api/capital-managers/recommend` (flujo de cliente final de SPEC 7.0).
    `POST /api/backtest/run` (demo histórico ya auditado) sin cambios.
  - Sin ningún ADR modificado — no se introduce ninguna interfaz, adaptador ni componente
    arquitectónico nuevo; `BloquearPorCapacidad` sigue el mismo patrón de campo opcional ya
    usado para `Instrumento?`/`Costes?`/`Sizing?`.
  - Sin `<ids_nuevos>` de SPEC — RN-12/CU-15 ya normaban el comportamiento; este delta lo activa
    de forma condicional, sin invalidar la excepción histórica D-059/D-060 del laboratorio.
  - Suite nueva: 16 tests (`ControlCapacidadTests` en `Application.Tests`, extensión de
    `ResultDtoMapperTests` en `Presentation.Tests`), citando RN-12/CU-15/RN-04/RNF-16 vía
    `spec:`. Suite completa del proyecto: **182/182 en verde**.
- **SPEC 7.0 — RN-15 a RN-19, CU-21 a CU-24, RNF-16** (Ingestión de datasets, DSL JSON
  declarativo, recomendación automatizada de Gestor de Capital, clasificación de régimen de
  mercado, explicabilidad de reportes para no expertos):
  - `Domain/Ingestion`: `ValidadorDataset` (RN-15, rechazo atómico ante timestamps
    duplicados/desordenados, valores nulos, o precios `High<Low`/`≤0`) y `DatasetHash`
    (SHA-256 determinista sobre el contenido del dataset).
  - `Domain/Strategy.Dsl`: `EsquemaDsl`/`ValidadorDsl`/`InterpreteDsl` (RN-16) — esquema
    declarativo mínimo V1 (condición `SMA(periodo)` vs. campo de la vela actual, operadores
    `>`/`<`/`>=`/`<=`, acción Market Buy/Sell), evaluación puramente declarativa sobre
    `DataSlice(N)`, rechazo explícito de referencias look-ahead (`offset`) y de comandos de
    ejecución externa (`comando`). `InterpreteDsl` implementa `IStrategy` sin cambios al
    contrato existente.
  - `Domain/Regimen`: `ClasificadorRegimen` V1 (RN-19) — pendiente de regresión lineal por
    mínimos cuadrados sobre ventana móvil W=20 de `Close`, umbral épsilon V1 documentado y
    aislado (constante propia, sin mezclar con calibración futura, por restricción explícita
    del auditor).
  - `Application.CapitalManagerRecommender` (RN-18, CU-23) — evalúa el mismo backtest de forma
    aislada contra cada Gestor de Capital pre-cargado (reutiliza `GestorFixedFractional`/
    `GestorFixedRisk`/`GestorVolatilitySizing` ya existentes, sin nuevos gestores — RN-17),
    calcula `CR = PnLTotal / (MaxDrawdown + 1)`, recomienda el de mayor CR sin liquidación de
    cuenta, o inadaptabilidad (`GestorRecomendado = null`) si todos liquidan.
  - `Application.ReporteRegimen` (RN-19, CU-24) — segmenta PnL/WinRate por fase de mercado,
    asociando cada `Trade` a su régimen mediante `Trade.TimestampApertura` (asociación
    explícita, nunca por orden de listas ni inferencia sobre Fills/EquityCurve — decisión
    explícita del auditor). Racha negativa/exposición quedan fuera de esta segmentación
    (heredado de D-045 del laboratorio); sin ranking/comparación entre regímenes salvo el
    `RegimenOptimo` que el propio SPEC exige mostrar.
  - `Infrastructure.IDatasetRepository`/`DatasetRepositoryLocal` (RN-15, CU-21) — catálogo
    local en disco (archivo `<hash>.json` por dataset + índice `catalogo.json`), sin base de
    datos (Opción A del ADR-001 actualizado).
  - `TD_Project.Api`: 3 endpoints nuevos, separados conceptualmente y sin modificar
    `POST /api/backtest/run` (ya auditado): `POST/GET /api/datasets` (ingestión y catálogo),
    `POST /api/strategies/dsl/run` (ejecución de estrategia DSL contra un dataset ya
    ingerido), `POST /api/capital-managers/recommend` (recomendación de gestor). Nuevos DTOs
    en `TD_Project.Contracts`: `CandleDto`/`IngestarDatasetRequestDto`/
    `IngestarDatasetResponseDto`/`CatalogoDatasetEntradaDto`, `EjecutarDslRequestDto`,
    `RecomendarGestorRequestDto`/`ResultadoGestorDto`/`RecomendarGestorResponseDto`.
  - `ResultDto.Explicacion` (nuevo, opcional) + `ExplicacionDto` (RNF-16) — descripciones
    interpretativas en español (resumen de resultado, régimen óptimo, gestor recomendado) y
    aviso obligatorio: "Los resultados corresponden a simulación histórica y no garantizan
    resultados futuros." `ResultDtoMapper.Mapear` gana 2 parámetros opcionales
    (`recomendacion`, `reporteRegimen`) al final de su firma, sin alterar las llamadas
    existentes.
  - `Trade` (glosario, RN-19) gana `TimestampApertura`/`TimestampCierre` (opcionales, default
    0) — metadata de ejecución pura, propagada por `AcumuladorTrade`/`BacktestRunner` desde
    `Fill.Timestamp`, sin alterar cálculo de PnL/lotes/margin. Decisión explícita del auditor
    (Opción 1, sobre alternativa descartada de reconstrucción por orden de listas — ver
    "Rechazado / Descartado").
  - Suite nueva: 30 tests (Domain 24, Application 9, Infrastructure 8 incluyendo un test de
    integración de punta a punta: ingestión → DSL → backtest → recomendación → régimen), todos
    citando su ID vía `spec:`. Suite completa del proyecto: **172/172 en verde**.
- Andamiaje inicial del proyecto: solución .NET 8, proyectos Domain,
  Application, Infrastructure y sus respectivos proyectos de test.
- Script `verify` con las seis comprobaciones de ADR-002.
- Suite de tests derivada del SPEC: RN-01..14, CU-01..20, EC-01..04 y los
  RNF verificables (05, 06, 07, 08, 09, 10, 12, 13), citando su ID vía
  comentario `spec:`.
- Implementación de dominio que satisface la suite completa (50/50 tests):
  - `Matching`: transiciones de Order (RN-01, RN-06) y motor de cruce
    (RN-02, RN-03, RN-05, RN-06).
  - `Broker`: Secuencia Causal (RN-04), bolsa de Requests (RN-14), reserva
    preventiva en dos fases (RN-12).
  - `Portfolio`: Margin por lotes FIFO (RN-08, RN-09), reversión Cross-Zero
    (RN-10), inmutabilidad de origen (RN-07).
  - `VelaResolution`: resolución de las dos trayectorias canónicas A/B sin
    contaminación cruzada, selección por Equity mínimo (RN-11).
  - `Application.BacktestRunner`: orquestación del ciclo N/N+1 (RN-13),
    estados de observabilidad (RNF-09), integridad de falla (RNF-10).
  - `Infrastructure.SerializadorResultado`: serialización simétrica en JSON
    (RNF-13).
- `src/Presentation/TD_Project.Contracts` (Fase 6, Paso 1): DTOs de salida para la capa de
  presentación — `ResultDto`, `ExperimentInfoDto`, `EquityPointDto`, `TradeDto`,
  `FillLogEntryDto`, `PortfolioSnapshotDto`, `LoteDto`, `MetricsDto`, `BranchResolutionDto`.
  Sin referencia a `Domain` ni `Application` (RNF-12), sin lógica ni cálculos, precisión decimal
  completa (redondeo diferido al mapper/reporte). `tests/Presentation.Tests/
  TD_Project.Contracts.Tests` (4 tests: construcción, round-trip JSON, precisión, frontera de
  dependencias).
- `src/Presentation/TD_Project.Api/Mapping/ResultDtoMapper.cs` (Fase 6, Paso 2): mapea
  `ResultadoBacktest` + `ConfiguracionExperimento` a `ResultDto`. Solo conversión de tipos
  (`Side`/`OrderType`/`TrayectoriaResolucion`/`EstadoBacktest` → string) y agregados de reporte
  simples (`MetricsDto`: último `EquityPoint`, suma de `RealizedPnL`, conteo de `Trades`); no
  recalcula Equity, no reconstruye Trades, no decide `TrayectoriaOficial`. `TD_Project.Api`
  referencia `Application` y `Contracts` (no `Domain` directamente). `tests/Presentation.Tests/
  TD_Project.Api.Tests` (7 tests: mapeo completo, precisión decimal, conversión enum/string,
  resultado vacío, ambas ramas RN-11, métricas agregadas, propagación de `Estado`).
- `TD_Project.Api` promovido a Minimal API (Fase 6, Paso 3): endpoint único
  `POST /api/backtest/run`, sin body de entrada, ejecución síncrona sin estado entre requests
  (sin `runId`, sin `GET /latest`, sin persistencia). `Demo/DatasetDemo.cs` y
  `Demo/EstrategiaDemo.cs` (configuración y estrategia fijas de demostración, exclusivas de
  `Presentation.Api.Demo`). `tests/Presentation.Tests/TD_Project.Api.Tests/
  BacktestRunEndpointTests.cs` (4 tests vía `WebApplicationFactory`: respuesta 200 con
  `ResultDto`, `Estado=Success` con `EquityCurve` poblada, body ignorado, determinismo entre
  llamadas sucesivas).
- Añadido visor web local para resultados de backtest (Fase 6, Paso 4):
  `src/Presentation/TD_Project.Api/wwwroot` (`index.html`, `app.js`, `styles.css`), HTML/JS/CSS
  puro sin dependencias frontend externas. Cuatro vistas: Resumen, Curva de Equity (SVG nativo),
  Trades, Resolución RN-11 (ambas ramas A/B). Servido vía `UseDefaultFiles`/`UseStaticFiles`.
  `tests/Presentation.Tests/TD_Project.Api.Tests/DashboardTests.cs` (4 tests: index.html servido,
  assets estáticos accesibles, nombres de campo del contrato correctos, ausencia de
  dependencias frontend externas).
- Primer borrador visual del dashboard (Prompt 05 · Repinta · Claude), sobre
  `src/Presentation/TD_Project.Api/wwwroot`. **Pendiente de auditoría visual** (Prompt 06 ·
  Pinta · Gemini) antes de considerarse definitivo. Identidad "mesa de control de laboratorio"
  (instrumento de auditoría, no dashboard de trading): sin verde=ganancia/rojo=pérdida, ámbar
  técnico como único acento reservado a la trayectoria oficial RN-11 y al estado `Success`.
  Sistema de tokens (color verificado ≥4.5:1 WCAG, tipografía `IBM Plex Mono`/`IBM Plex Sans`
  con fallback de sistema sin fuentes binarias en el repo, escala de espaciado densa) aplicado a
  `index.html`/`styles.css`/`app.js` sin alterar IDs, contrato JSON, ni lógica de fetch/mapeo.
  Resolución RN-11 reordenada como elemento de firma visual, antes que Trades. Trayectoria
  descartada: atenuada por opacidad, deliberadamente sin tachado (una simulación no
  seleccionada no es un dato inválido). Micro-interacciones: hover de tabla limitado a
  `@media (hover: hover)` (evita hover fantasma táctil), entrada suave de `#resultado` vía
  `@starting-style`. Auto-auditoría (`web-design-guidelines`): `color-scheme: dark` añadido,
  corrección ortográfica en mensaje de error visible. Formato de presentación numérica
  (`Intl.NumberFormat`), `meta theme-color` y contraste adicional en botón evaluados y
  diferidos — ver `docs/PENDIENTES.md`. Cero dependencias externas nuevas, `verify` en verde
  (79/79 tests) sin ningún test roto por el cambio visual.

### ADR Actualizado
- **ADR-001** (Opción A del delta SPEC 7.0): incorpora `IDatasetRepository` (contrato en
  `Infrastructure`, adaptador `DatasetRepositoryLocal` — almacenamiento local en disco,
  archivo por dataset + índice de catálogo, sin base de datos en esta fase) y
  `CapitalManagerRecommender` (orquestador multi-experimento en `Application`, ejecuta el
  backtest de forma aislada contra cada Gestor de Capital pre-cargado). Ninguna otra decisión
  del ADR-001 original se modifica. **Aplicación formal cerrada**: sección "Decisión
  adicional — Persistencia de datasets y recomendación de gestores (SPEC 7.0)" agregada a
  `docs/adr/ADR-001-stack-y-arquitectura.md`, cambio exclusivamente documental (sin tocar
  `src/`, `tests/`, corpus ni manifiesto).

### Cambiado
- `AplicadorFill.Aplicar` (RN-09, RN-10): pasa de abrir siempre un lote nuevo a decidir, según
  signo y magnitud de la Position actual, entre abrir/aumentar, reducir vía `ConsumidorFifo`, o
  invertir vía `ResolutorCrossZero`. Firma cambia de `void` a `ResultadoAplicacionFill` (nuevo
  record: `Trade? TradeCerrado, decimal RealizedPnLReconocido, decimal MarginLiberado`).
- `ConsumidorFifo.Consumir` y `ResolutorCrossZero.Resolver`: ahora calculan `RealizedPnL` (vía
  `CalculadoraRealizedPnL`, nuevo) además de Margin liberado.
- `BacktestRunner`: acumula el ciclo de vida completo de cada `Trade` (potencialmente
  multi-Fill) mediante `AcumuladorTrade` (nuevo, `Application`); `ResultadoBacktest.Trades` deja
  de estar siempre vacío.
- `ResultadoResolucionVela` (RN-11, RNF-08): amplía con `FillsA/FillsB/EquityA/EquityB`
  (evidencia de ambas trayectorias evaluadas, sin alterar la selección oficial) y
  `CashFinal/MarginFinal/UnrealizedPnLFinal/LotesVivosFinal` (desglose de `EquityFinal`, rama
  oficial únicamente). `ResultadoBacktest` amplía con `EquityCurve`, `PortfolioSnapshots` y
  `BranchResolutions` (nuevos records `EquityPoint`, `PortfolioSnapshot`, `BranchResolutionInfo`,
  `TrayectoriaResolucion` en `Application`), acumulados por vela en `BacktestRunner` a partir de
  campos ya resueltos por `ResolutorVela` — sin recalcular ni interpretar Fills.
- `ResultDto` (Fase 6, Paso 2): gana un campo `Estado` (string, mapeado de `EstadoBacktest`).
  Sin él, un resultado no-`Success` (listas vacías) era indistinguible de un experimento válido
  con cero actividad — hallazgo detectado al diseñar el mapper `ResultadoBacktest → ResultDto`.

### Rechazado / Descartado
- Regla Nueva estricta para inyección de Tasa de Margen (vinculada a RNF-08): Descartada como regla obligatoria del SPEC. Reclasificada como mejora de diseño (deuda técnica) para favorecer la auditabilidad.
- `<meta name="theme-color">` en el dashboard (Fase 4, auto-auditoría del borrador visual):
  descartado por severidad LOW — beneficio mínimo (integración con la barra de navegador
  móvil), no justifica el cambio para un MVP local.
- Aumentar contraste en `:hover`/`:active` del botón "Ejecutar" más allá del `outline`/`scale`
  ya existente (Fase 4): descartado porque el ámbar debe permanecer como acento único y
  controlado (decisión de identidad de Fase 1 — "mesa de control de laboratorio"); añadir más
  señal luminosa por interacción contradice esa restricción deliberada. El foco visible ya
  cumple la exigencia real de accesibilidad.
- Formateo de valores financieros con `Intl.NumberFormat` en el dashboard (Fase 4): **no
  descartado, diferido** — no es una preferencia estética sin respaldo, requiere antes una
  política explícita de formato de reporte (locale, decimales por tipo de dato, si se refleja
  la precisión interna del motor o una lectura humana simplificada). Ver
  `docs/PENDIENTES.md` § Borrador visual.
- Regla nueva "Aislamiento de `IStrategy` entre ejecuciones" (Ronda 1, auditoría de uso
  cotidiano): **no incorporada a `SPEC.md` todavía** — antecede una decisión arquitectónica sin
  tomar (¿`IStrategy` es un componente controlado por el motor o un plugin externo con contrato
  de uso propio?). Ver `docs/PENDIENTES.md` § Bugs detectados durante auditoría de uso
  cotidiano.
- Regla nueva de reporte "Cash vs. Equity con posición viva al cierre" (Ronda 1): reclasificada
  como mejora candidata de Presentation/Reporting, no como RN/RNF — la aritmética del motor ya
  es correcta, es una carencia de observabilidad en la capa de presentación. Ver
  `docs/PENDIENTES.md`.

### Corregido
- `MatchingEngine`: la dirección de cruce Limit/Stop estaba invertida para
  el mismo lado (Buy Limit dispara bajando, Buy Stop dispara subiendo).
- `BacktestRunner`: distinguía `StrategyError` de `InternalCrash` por el
  tipo de excepción esperado; `List<T>` fuera de rango lanza
  `ArgumentOutOfRangeException`, no `IndexOutOfRangeException`.
- `tools/verify.ps1`: el extractor de IDs de `SPEC.md` solo reconocía el
  primer ID de una declaración agrupada (ej. "RNF-01, RNF-02, RNF-03"),
  dejando RNF-02 y RNF-03 fuera de la comprobación de trazabilidad.
- `tools/verify.ps1`: la búsqueda de la cita `spec:` de un test usaba una
  ventana fija de 3 líneas hacia atrás, produciendo falsos negativos en
  comentarios de test más largos.
- `ResolutorVela.CalcularEquity` (RN-08): calculaba Equity como
  `Cash + Margin`, omitiendo el `UnrealizedPnL` (valoración M2M de la
  posición viva al último Close conocido) que exige la fórmula completa del
  glosario. Corregido para sumar `Σ(Cantidad_lote × (Close − PrecioEntrada_lote))`
  sobre los lotes vivos de cada rama.
- `ConsumidorFifo`/`AplicadorFill`/`CalculadoraLotes` (RN-08, RN-09): una reducción FIFO sobre
  una posición **corta** calculaba `RealizedPnL` y `MarginLiberado` con el signo invertido.
  Causa raíz: `Lote.Cantidad` conserva el signo de la posición (negativo en Short, por diseño
  de RN-08), pero `ConsumidorFifo.Consumir` usaba ese `Lote.Cantidad` con signo directamente
  como magnitud a consumir (`Math.Min(lote.Cantidad, restante)` entre un negativo y un
  positivo), corrompiendo el propio bucle FIFO — no solo el signo final, también dejaba lotes
  sin remover del portfolio cuando se consumían por completo. `CalculadoraLotes.AbrirLote`
  tenía el mismo defecto de raíz (`Margin = cantidad * precioFill * tasaMargen` con `cantidad`
  con signo, produciendo `Margin` negativo en aperturas Short puras — bug preexistente ya
  documentado en `docs/PENDIENTES.md` desde la Fase 6, sin manifestarse en ningún test hasta
  ahora). Las tres correcciones comparten la misma regla: el signo captura dirección, la
  magnitud absoluta captura cantidad financiera — nunca deben mezclarse. Detectado en la Ronda
  1 de auditoría de uso cotidiano (adaptación de un solo rol del Prompt 12), al construir un
  fixture con estrategias reales que, a diferencia de los tests existentes (todos sobre
  posiciones Long), abrían posiciones cortas desde cero. Cubierto por el nuevo test
  `AplicadorFillIntegracionTests.UnFillDeReduccionSobreUnaPosicionCortaCalculaElRealizedPnLConElSignoCorrecto`
  (`spec: RN-08`, `RN-09`), simétrico al test ya existente para el camino Long.
- `MatchingEngine.ResolverStopLimit` (RN-11): las dos trayectorias canónicas (A: O→H→L→C, B:
  O→L→H→C) nunca podían divergir en un resultado real. Causa raíz: `ResolverStopLimit` recibía
  el parámetro `Trayectoria` pero nunca lo consultaba — evaluaba el disparo del Stop y el cruce
  del Limit contra `vela.Open/High/Low/Close` directamente (agnóstico al orden temporal), lo
  mismo que `Market`/`Limit`/`Stop` puros (correcto para esos tres, porque no tienen ambigüedad
  de orden entre dos condiciones). Verificado empíricamente que ni siquiera el ejemplo canónico
  del propio SPEC (CU-13) diverge bajo el código anterior. Corregido introduciendo
  `RecorridoVela` (`src/Domain/Matching/RecorridoVela.cs`, nuevo): construye el recorrido
  Open→Primero→Segundo→Close según la trayectoria, y `ResolverStopLimit` ahora recorre ese
  camino tramo a tramo para encontrar el punto exacto de disparo del Stop y, desde ahí, evalúa
  el cruce del Limit únicamente en el tramo restante hasta Close (no en la vela completa) — el
  disparo tardío en una trayectoria puede agotar el tramo disponible para el Limit,
  produciendo Fill en una rama y no en la otra. Cambio acotado a `ResolverStopLimit` y sus
  funciones auxiliares; `Market`/`Limit`/`Stop` puros, `ResolutorVela`, `BacktestRunner` y
  `SPEC.md` sin cambios. Caso mínimo de divergencia hallado por búsqueda exhaustiva (no a mano):
  Buy Stop-Limit 102/101 sobre vela Open=100/High=102/Low=90/Close=102 — trayectoria A hace
  Fill @101 (el Stop dispara subiendo directo al High, cruzando el Limit de camino),
  trayectoria B no hace Fill (el Stop recién dispara al llegar al High tras bajar primero, sin
  tramo restante hasta Close donde el Limit sea alcanzable). Cubierto por
  `StopLimitTests.StopLimitPuedeDivergirEntreTrayectorias`,
  `TrayectoriasCanonicasTests.TrayectoriaSeleccionadaSigueSiendoLaDeMenorEquityCuandoDivergenPorStopLimit`
  y `CicloVitalTests.BacktestCompletoMantieneDeterminismoConNuevaResolucionTemporal` (`spec:
  RN-11`, RNF-06). Detectado en la mini-investigación abierta entre las Rondas 2 y 3 de
  auditoría de uso cotidiano, a partir de un hallazgo de Ronda 2 sobre precisión de Margin que
  llevó a cuestionar si RN-11 producía divergencia real alguna vez.
- `FillLogEntryDto` (RNF-08): no exponía `CostoFriccionReal`, dato obligatorio del "Fill Log
  Mínimo" para reconstrucción determinística. `Domain.Shared.Fill` ya lo calculaba;
  `ResultDtoMapper.MapearFill` lo descartaba en silencio. Agregado el campo al DTO y propagado
  en el mapper. `CostoFriccionReal` llega en `0m` hoy porque `MatchingEngine` aún no calcula
  fricción real — gap de Domain preexistente, fuera de este alcance; el campo se propagará sin
  cambios adicionales en Contracts/Mapper cuando exista. Cubierto por
  `ResultDtoMapperTests.PropagaElCostoDeFriccionRealDeCadaFillAlLog`. Detectado en la Ronda 3 de
  auditoría de uso cotidiano, ángulo "lectura del ResultDto por un consumidor JSON puro".
- `ValidadorBolsaRequests` (RN-08, RN-14): no rechazaba `OrderRequest` con `Cantidad <= 0`. Una
  `Cantidad` negativa permitía que `Side` y el signo de `Cantidad` fueran dos fuentes de verdad
  de dirección potencialmente contradictorias (ej. `Side=Buy, Cantidad=-10`), dejando que
  `AplicadorFill` interpretara la ambigüedad silenciosamente en vez de rechazarla en la
  frontera de entrada — mismo patrón de riesgo "dirección vs. magnitud" ya corregido en RN-08/
  RN-09 (Ronda 1). `Cantidad = 0` viola la misma invariante (ninguna operación real) y se
  rechaza por el mismo mecanismo. Extendida la condición de rechazo atómico ya existente de
  RN-14 (`tieneCantidadInvalida = bolsa.Any(r => r.Cantidad <= 0m)`), sin nuevo tipo ni cambio
  de firma. Cubierto por `BolsaRequestsTests.OrderRequestConCantidadNegativaEsRechazado` y
  `OrderRequestConCantidadCeroEsRechazado`. Detectado en la Ronda 3, ángulo "ejecución en los
  bordes de lo razonable".
- `ResultDtoMapperTests` (RNF-09): agregado
  `UnResultadoNoSuccessMantieneEstadoDistintoAunqueLaFormaDeDatosSeaIgual` — no era un bug de
  implementación (`Estado` ya existía, ya se propagaba, ya se mostraba en el dashboard), sino un
  vacío de contrato sin verificar: nada garantizaba explícitamente que un `Success` con cero
  actividad y un resultado no-`Success` (ambos con `Trades`/`EquityCurve` vacíos y `Metrics` en
  cero) permanecieran distinguibles. El test fija esa garantía como propiedad verificada del
  contrato. Sin cambios en `ResultDto`/`ResultDtoMapper`/`MetricsDto`/`app.js`. Detectado en la
  Ronda 3, ángulo "comparación entre corridas".
- `AcumuladorTrade` (RN-10, glosario "Trade"): en un Cross-Zero, `PrecioApertura`/`CantidadInicial`
  del ciclo que queda abierto tras la inversión heredaban el valor "stale" del ciclo anterior ya
  cerrado, en vez del precio/magnitud reales de ese mismo Fill. Causa raíz: `AntesDeAplicar`
  solo fijaba la apertura cuando la posición previa al Fill era exactamente cero; en un
  Cross-Zero la posición nunca pasa por cero (el mismo Fill cierra el ciclo viejo y abre el
  nuevo atómicamente), así que el nuevo ciclo nunca fijaba su propia apertura y arrastraba el
  valor del ciclo anterior indefinidamente. El defecto era de reporte en `Application`
  (`RealizedPnL` se calculaba correctamente en Domain, vía `ResolutorCrossZero`), pero llegaba
  intacto hasta `TradeDto.PrecioApertura` en el JSON expuesto por HTTP. Corregido agregando
  `AcumuladorTrade.DespuesDeAplicar` (nuevo método): tras aplicar el Fill, si cerró un Trade
  (`TradeCerrado is not null`) y la posición resultante ya no es cero, ese mismo Fill es la
  apertura real del ciclo siguiente — se fija ahí, después de extraer el Trade que se estaba
  cerrando (orden importa: `CerrarYExtraer` debe leer los valores del ciclo viejo antes de que
  `DespuesDeAplicar` los sobrescriba con los del ciclo nuevo). Semántica de `PrecioApertura`
  (precio del primer Fill que abre el ciclo, no promedio ponderado) confirmada contra el
  comentario de diseño ya existente en el archivo y contra el test preexistente
  `UnaPosicionConDosReduccionesCierraUnSoloTradeAlLlegarACero` — no redefinida, solo extendida
  al caso Cross-Zero que no estaba cubierto. Cubierto por
  `CicloVitalTests.TresCrossZeroConsecutivosReportanElPrecioDeAperturaRealDeCadaCiclo` (3
  Cross-Zero consecutivos, cada Trade con su propio `PrecioApertura`/`CantidadInicial`
  verificados). Detectado en la Ronda 4 (adversarial) de auditoría de uso cotidiano, ángulo
  "Cross-Zero + posición viva al cierre".
- `ResultDtoMapper` (RNF-05): `MetricsDto.EquityFinal` no aplicaba el redondeo de reporte
  exigido ("Equity_rep = Cash_rep + Margin_rep + UnrealizedPnL_rep, Half-to-Even a 2 decimales,
  exclusivo al final") — tomaba el valor crudo de `EquityCurve[^1].Equity` sin redondear.
  `RedondeoReporte.EquityReportado` (`src/Domain/Shared/RedondeoReporte.cs`) ya existía,
  implementado correctamente y probado en aislamiento
  (`tests/Domain.Tests/Precision/RedondeoDecimalTests.cs`), pero sin ningún caller real —mismo
  patrón "construido y probado pero no cableado" ya visto en RN-12 y CU-19/OCO. Conectado en
  `ResultDtoMapper.Mapear` vía el nuevo helper privado `EquityFinalReportado`, que aplica
  `RedondeoReporte.EquityReportado` sobre los tres componentes (`Cash`/`Margin`/`UnrealizedPnL`)
  del último `EquityPoint`. `EquityCurve` (la serie completa) conserva precisión decimal
  completa sin redondear — RNF-05 exige 8 decimales intermedios ahí; solo el agregado de
  `Metrics` (dato de reporte) se redondea. Cubierto por
  `ResultDtoMapperTests.MetricsEquityFinalAplicaElRedondeoDeReporteExigidoPorRnf05`. Detectado
  en la Ronda 4, verificación sistemática del patrón "no cableado" en todo `src/Domain`.
- Demo del endpoint `POST /api/backtest/run` (Ciclo 4B): `DatasetDemo`/`EstrategiaDemo`
  pasaron de un único `Market Buy` sin ambigüedad posible (`EquityA == EquityB` trivial en las
  tres velas, no demostraba nada) al caso canónico de divergencia real de RN-11 (`Buy
  Stop-Limit 102/101` sobre vela `Open=100/High=102/Low=90/Close=102`, el mismo escenario de
  `StopLimitTests.StopLimitPuedeDivergirEntreTrayectorias`). El demo en vivo ahora produce
  `BranchResolutions` con trayectorias A/B genuinamente distintas (`FillsA` con un Fill, `FillsB`
  vacío), verificado con `dotnet run` + POST real. Motivo: la Ronda 4 encontró que el flujo HTTP
  productivo no ejercitaba ninguno de los bugs ya corregidos — la demo ahora sí muestra la
  garantía más distintiva del proyecto.
- Dashboard (`wwwroot/index.html`, `app.js`, Ciclo 4B): agregadas las secciones "Fill Log" y
  "Posición por vela" (Cash/Margin/Lotes vivos), consumiendo `dto.fillLog` y
  `dto.portfolioSnapshots` — datos que el contrato ya exponía correctamente (confirmado en
  Ronda 4) pero que ningún elemento visual leía, incluido `CostoFriccionReal` (agregado en
  Ronda 3). Mismo patrón de tabla ya usado por Trades/BranchResolutions (`<caption
  class="sr-only">`, `scope="col"`). Sin cambios de diseño visual nuevo (paleta/tipografía/
  tokens intactos, Fase visual ya cerrada). `AppJsReferenciaLosCamposRealesDelContrato`
  extendido con `dto.fillLog`/`dto.portfolioSnapshots`.
- `BacktestRunner` (rendimiento, O(n²) → O(n)): el loop principal reconstruía dos estructuras
  completas en cada vela. (1) `DataSlice` se creaba con `config.Velas.Take(n + 1).ToList()`,
  copiando toda la porción vista hasta el momento en cada iteración. (2) las órdenes "Pending"
  se recalculaban con `ordenesPending.Where(o => o.Status == OrderStatus.Pending).ToList()`
  sobre el historial completo de órdenes emitidas, y la búsqueda de la orden de cada Fill usaba
  `ordenesPending.First(...)` sobre la misma lista creciente. Con los datasets sintéticos usados
  hasta ahora (~200 velas) el costo es imperceptible; con el primer dataset real de escala
  completa (Fase 2A, BTCUSDT 1m, 527.040 velas) el proceso quedó colgado sin completar el
  primer timeframe evaluado en Fase 2C. Causa raíz: ningún escenario previo (sintético ni real)
  había alcanzado ese volumen de velas hasta la introducción de datos reales — el bug estuvo
  presente en todo el motor desde antes de esta sesión sin manifestarse nunca. Corrección: (1)
  nueva `Domain.Shared.VentanaDeVelas` (`IReadOnlyList<Candle>` de solo lectura sobre la lista
  original, O(1) por construcción, indexador que preserva el bloqueo físico de RN-13 lanzando
  fuera de `[0, longitud)`) reemplaza el `Take().ToList()`; (2) nueva lista `ordenesActivas`
  mantenida en paralelo a `ordenesPending` (el registro completo, sin cambios en el resultado
  final), añadida/removida en los mismos puntos donde `OrdenTransiciones.Ejecutar`/`Cancelar`
  ya mutaban el `Status` — elimina el filtro sobre el historial completo. Semántica financiera
  sin cambios (mismo `CashFinal`/`Trades`/`Fills`/`BranchResolutions` que antes, verificado por
  la suite completa sin modificaciones). Test de regresión
  `RendimientoEscalaTests.UnDatasetDeEscalaRealCompletaEnTiempoLinealYEsDeterminista` corre
  527.040 velas (tamaño exacto del dataset real congelado) dos veces, verificando determinismo,
  con `Timeout=30_000`ms para que una regresión futura falle rápido en vez de colgar la suite.
  Un caso distinto queda fuera de este fix y documentado como debt en `docs/PENDIENTES.md`: una
  estrategia que nunca cierra posiciones (acumula lotes vivos sin límite) sigue siendo O(n) por
  vela en el cálculo de `UnrealizedPnL`/`Margin` sobre lotes vivos — ninguna estrategia real del
  laboratorio (Tres Mosqueteros, MHI, ni las del laboratorio sintético) produce ese patrón. Tras
  el fix, la matriz de `exploration/laboratorio` (2 estrategias × 6 timeframes, BTCUSDT real,
  527.040 velas base) completó en 14.6s con `Estado=Success` y reconciliación financiera OK en
  las 12 corridas — confirma que el bug era exclusivamente de rendimiento, sin efecto en ningún
  resultado financiero ya validado en Fases 1-2B (datasets sintéticos, muy por debajo del
  volumen donde el costo O(n²) se vuelve perceptible).
