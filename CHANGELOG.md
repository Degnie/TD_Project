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
