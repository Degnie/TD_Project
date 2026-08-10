# Changelog

Formato basado en [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Agregado
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
