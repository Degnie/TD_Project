# Pendientes

Decisiones aplazadas, metodologías evaluadas y no activadas, y riesgos
detectados sin resolver durante Fases 1-5. Este documento se actualiza en
cada auditoría posterior; no se cierra ni se vacía.

## Umbrales y objetivos cuantitativos

- **RNF-01/02/03** — sin valores numéricos de velas/segundo, bytes/vela,
  bytes/orden concurrente, pico de memoria ni tiempo total. Requiere
  benchmark propio sobre el escenario de referencia (1 activo, 10M velas,
  estrategia O(1), 1000 órdenes sin OCO, hilo único) antes de fijar umbrales.
- **RNF-04** — objetivo cuantitativo de speedup paralelo sin definir.
  Permanece activo como requisito, no descartado ni fuera de alcance del MVP.
- **RNF-13** — formato concreto de serialización sin definir. El SPEC exige
  únicamente la propiedad `Deserializar(Serializar(Result)) == Result`; el
  formato es decisión técnica de implementación.
- **Umbral de mutación (70%)** — propuesto en `TESTING_STRATEGY.md` sin
  respaldo en un ID del SPEC. Abierto a ajuste una vez que exista código real
  sobre el que medir.

## Vacíos del dominio

- **Ingestión del Dataset** — el mecanismo concreto de entrada del OHLCV no
  está definido en el SPEC. Es un vacío técnico esperado (el SPEC prohíbe
  nombrar infraestructura), no una contradicción ni una omisión a corregir
  en el documento.

## Candidatos de benchmarking diferidos (Líneas de investigación aprobadas)

- **[ACLARACIÓN CONCEPTUAL] Ítem #13 — Modelo de Fricción / Slippage:** Aclarar formalmente si `Friction Model` en RN-12 y RNF-08 abarca únicamente comisiones o si debe incluir un componente explícito de *slippage* / *market impact*. No requiere cambios en `SPEC.md` v6.0 hoy; es una precisión conceptual pendiente.
- **[REPORTE FUTURO] Ítem #12 — Métricas de riesgo y rendimiento:** Evaluación de métricas estadísticas (Max Drawdown, Win Rate, Profit Factor, Sharpe, Sortino, Calmar) como parte de la capa de presentación/reporte (`Presentation.Api`), sin alterar el estado o cálculo del resultado financiero canónico en `Domain`.
- **[INVESTIGACIÓN FUTURA] Ítem #3 — Auditoría activa anti look-ahead:** Herramienta post-hoc estilo Freqtrade (`lookahead-analysis`) para auditar la estrategia del usuario buscando lecturas futuras sutiles. Se mantiene como investigación futura si se admite la ejecución de estrategias de terceros no confiables.
- **[INVESTIGACIÓN ARQUITECTÓNICA] Ítem #9 — Motor vectorizado secundario:** Exploración de un motor secundario independiente (`FastSimulationEngine`) optimizado para búsqueda masiva de parámetros en paralelo, preservando intacto el motor canónico determinista event-driven de `TD_Project`.

## Decisiones de diseño diferidas a la fase de contratos (Prompt 3)

- ~~**Mecanismo de aislamiento entre ramas A/B (RN-11)**~~ — resuelto en
  Etapa 3 (Prompt 3): `Order.Clonar()` y `PortfolioState.Clonar()` producen
  una copia independiente por rama antes de que `ResolutorVela` invoque
  `MatchingEngine`/`AplicadorFill`, sin mutar el estado original hasta
  seleccionar la rama oficial.
- **Contratos concretos entre Broker, Matching Engine, Portfolio y
  VelaResolution** — interfaces, tipos de entrada/salida y forma de
  invocación aún no definidos. Solo están fijadas las responsabilidades y
  las direcciones de dependencia permitidas (ver ADR-001).
- **Dependencias circulares VelaResolution ↔ módulos coordinados** — riesgo
  a vigilar explícitamente al definir los contratos: VelaResolution depende
  de Matching Engine y Portfolio, pero ninguno de los dos debe depender de
  vuelta de VelaResolution.

## Vacíos detectados durante la implementación (Etapa 3)

- ~~**UnrealizedPnL / M2M en `ResolutorVela.CalcularEquity`**~~ — resuelto
  en el commit `2f0f121` (hallazgo de auditoría, `docs/hallazgos-actual.md`):
  `CalcularEquity` ahora suma `Σ(Cantidad_lote × (Close − PrecioEntrada_lote))`
  sobre los lotes vivos de cada rama, evaluado contra el `Close` de la vela
  resuelta. Cubierto por el test `ElEquityIncluyeLaValoracionM2MDeLaPosicionVivaAlCierre`
  (`spec: RN-08`).
- **[MEJORA DE DISEÑO] Configuración explícita de `TasaMargen`** — `AplicadorFill.Aplicar`
  (`src/Domain/Portfolio/AplicadorFill.cs`) usa un valor fijo por defecto
  (`0.1m`). Aunque no viola RNF-08 estrictamente, extraer este valor a la
  `ConfiguracionExperimento` mejoraría sustancialmente la reproducibilidad
  y auditabilidad del sistema. Clasificado como deuda técnica pendiente de
  decisión de diseño, no como regla obligatoria del SPEC.
- ~~**`AplicadorFill` no conectaba `ConsumidorFifo`/`ResolutorCrossZero` al flujo real (RN-09,
  RN-10)**~~ — resuelto durante el diseño de la capa de Presentation (Fase 6), al investigar si
  `Trade` era reconstruible: `AplicadorFill.Aplicar` solo llamaba a `CalculadoraLotes.AbrirLote`,
  sin importar el signo de la posición previa; una reducción o inversión de posición nunca
  reducía, cerraba, ni generaba `RealizedPnL`. Corregido para decidir el camino
  (abrir/aumentar, reducir FIFO, o Cross-Zero) según signo y magnitud de la Position actual,
  delegando las matemáticas a `ConsumidorFifo`/`ResolutorCrossZero`/`CalculadoraRealizedPnL`
  (nuevo). `AplicadorFill.Aplicar` pasa de `void` a `ResultadoAplicacionFill` (`Trade?
  TradeCerrado, decimal RealizedPnLReconocido, decimal MarginLiberado`). `BacktestRunner`
  acumula el ciclo de vida del Trade completo (multi-Fill) vía `AcumuladorTrade` — Domain solo
  emite el evento puntual por Fill, Application ensambla el histórico. Cubierto por
  `AplicadorFillIntegracionTests.cs` (RN-09, RN-10) y
  `UnaPosicionConDosReduccionesCierraUnSoloTradeAlLlegarACero` (glosario "Trade").
- **[BUG PREEXISTENTE, fuera de alcance] `CalculadoraLotes.AbrirLote` produce `Margin` negativo
  en aperturas Short puras** — `Margin = cantidad × precioFill × tasaMargen` usa `cantidad` con
  signo (RN-08 literal); si `cantidad` es negativa (posición Short abierta desde cero), el
  Margin calculado sale negativo, lo cual contradice la noción de Margin como colateral. Nunca
  se manifestó antes porque ningún test abría una posición Short pura desde cero. Detectado al
  implementar el camino Cross-Zero de `AplicadorFill` (que sí lo evita localmente, ver commit
  de esta fase), pero **no corregido en `CalculadoraLotes.cs`** por estar fuera del alcance
  aprobado para este cambio. Pendiente de decisión: ¿`CalculadoraLotes.AbrirLote` debe tomar
  magnitud y aplicar signo solo a `Lote.Cantidad`, dejando `Margin` siempre no negativo?
- ~~**`ResultadoResolucionVela` no conservaba evidencia de ambas ramas A/B ni el desglose de
  Equity (RN-11, RNF-08)**~~ — resuelto durante el diseño de la capa de Presentation (Fase 6,
  Paso 0): `ResolutorVela` calculaba `FillsA/FillsB/EquityA/EquityB` y `UnrealizedPnL` como
  variables locales y los descartaba al construir el resultado, dejando solo la rama
  seleccionada. `ResultadoResolucionVela` amplía con `FillsA/FillsB/EquityA/EquityB` (evidencia
  de ambas trayectorias, sin alterar la selección RN-11) y `CashFinal/MarginFinal/
  UnrealizedPnLFinal/LotesVivosFinal` (desglose de `EquityFinal`, rama oficial únicamente;
  `LotesVivosFinal` es una copia, no la referencia viva del `PortfolioState`).
  `BacktestRunner` acumula por vela `EquityCurve`, `PortfolioSnapshots` y `BranchResolutions`
  (nuevos records en `Application`, incluyendo `TrayectoriaResolucion` como espejo propio de
  `Domain.Shared.Trayectoria` para no filtrar un enum de Domain hasta Presentation) —
  `BacktestRunner` solo lee y apila campos ya resueltos por `ResolutorVela`, no calcula nada.
  Cubierto por `ElResultadoConservaFillsYEquityDeAmbasTrayectorias`,
  `ElDesgloseDeEquityCorrespondeALaRamaOficial` (`spec: RN-11`, `RN-08`) y
  `ElResultadoConservaEquityCurvePortfolioSnapshotsYBranchResolutionsPorVela` (`spec: RNF-08`).

## Capa de Presentation (Fase 6) — en construcción

- **Paso 1 completado**: `src/Presentation/TD_Project.Contracts` — DTOs de salida (`ResultDto`
  y 8 records asociados), sin referencia a `Domain` ni `Application` (verificado por
  `ContractsNoReferenciaDomainNiApplication`). `TradeDto` es espejo directo del `Trade` real
  (no del diseño original desfasado, que asumía Timestamps/SecuenciaCausal inexistentes en la
  fuente). `BranchResolutionDto` deliberadamente **no** tiene un campo `Motivo`: no es un dato
  persistido, es interpretación derivable en la UI comparando `EquityA`/`EquityB`.
  `ExperimentInfoDto` deliberadamente **no** tiene `ExperimentId`/`EstrategiaNombre`: no existen
  en `ConfiguracionExperimento` ni en `ResultadoBacktest` hoy — agregarlos requeriría antes
  decidir si el `Experiment`/`IStrategy` obtienen identidad, cambio de alcance fuera de
  Contracts. Redondeo decimal: **no aplicado** en Contracts (RNF-05 solo exige redondeo "a
  tiempo de reporte"; Contracts transporta precisión completa, verificado por
  `LaSerializacionNoPierdePrecisionDecimal`); el redondeo a 2 decimales queda diferido al
  mapper/reporte (Paso 2, aún no implementado).
- **Paso 2 completado**: `src/Presentation/TD_Project.Api/Mapping/ResultDtoMapper.cs` — mapea
  `ResultadoBacktest` + `ConfiguracionExperimento` (necesita ambos: `ExperimentInfoDto` deriva
  de `config.Velas`, no de `ResultadoBacktest`) a `ResultDto`. `TD_Project.Api` creado como
  `classlib` (no `webapi` todavía — sin endpoints, YAGNI hasta Paso 3). Solo conversión de
  tipos y agregados de reporte (`MetricsDto.EquityFinal` = último `EquityPoint`, `PnLTotal` =
  suma de `RealizedPnL`, `TotalTrades` = conteo); no recalcula Equity, no reconstruye Trades,
  no decide `TrayectoriaOficial`. `ResultDto` ganó un campo `Estado` (string) durante este paso
  — sin él, un resultado `InternalCrash`/`NotEvaluable`/etc. (listas vacías) era indistinguible
  de un experimento válido con cero actividad; hallazgo detectado al diseñar el mapper, resuelto
  ampliando Contracts antes de mapear, no documentado como limitación.
- **Paso 3 completado**: `TD_Project.Api` promovido a `Microsoft.NET.Sdk.Web` (Minimal API).
  Endpoint único `POST /api/backtest/run`: sin body de entrada (cualquiera enviado se ignora,
  verificado por `PostRunIgnoraCualquierBodyEnviado`), ejecuta `DatasetDemo.Configuracion()` +
  `EstrategiaDemo` vía `BacktestRunner.Ejecutar`, devuelve el `ResultDto` directo. **Sin estado
  entre requests**: no hay `runId`, no hay `GET /latest`, no hay almacenamiento — cada POST
  recalcula desde cero (determinismo verificado por
  `DosLlamadasSucesivasDevuelvenElMismoResultado`, RNF-06). `DatasetDemo.cs`/`EstrategiaDemo.cs`
  viven exclusivamente en `Presentation.Api.Demo`, nunca en Domain/Application/Infrastructure.
  `TD_Project.Api.csproj` referencia `Domain` explícitamente (no oculta la dependencia detrás
  de `Application`): `EstrategiaDemo` implementa `Domain.Strategy.IStrategy` directamente, como
  el punto de extensión de usuario que es.
  **Deuda futura explícita, ninguna implementada**: `StrategyCatalog`/selección dinámica de
  estrategia, `POST /api/datasets` (ingestión real de OHLCV), `GET /api/backtest/{runId}` +
  persistencia/historial de ejecuciones, autenticación, ejecución asíncrona. El contrato HTTP
  actual (`POST /run` sin parámetros) deberá evolucionar a aceptar `{dataset, strategy}` **solo
  cuando esas entidades existan de verdad** — no antes, para no exponer opciones decorativas.
- **Paso 4 completado**: `src/Presentation/TD_Project.Api/wwwroot` — dashboard estático
  (`index.html`/`app.js`/`styles.css`, HTML/JS/CSS puro, sin npm/CDN/framework, verificado por
  `NoExistenDependenciasFrontendExternas`). Servido vía `app.UseDefaultFiles()` +
  `app.UseStaticFiles()` en `Program.cs`. Flujo: click "Ejecutar" → `fetch POST
  /api/backtest/run` → render directo del JSON recibido. Sin almacenamiento local, sin caché,
  sin histórico, sin polling/websockets — cada ejecución es independiente, igual que el
  endpoint. Cuatro vistas: Resumen (`MetricsDto`), Curva de Equity (SVG nativo, sin librería de
  gráficos), Trades (tabla), Resolución RN-11 (ambas ramas A/B con Equity y conteo de Fills).
  `app.js` referencia los nombres de campo reales del contrato en `camelCase` (forma de
  serialización por defecto de Minimal API, confirmada contra la respuesta real del servidor
  en vivo) — verificado por `AppJsReferenciaLosCamposRealesDelContrato`.
  **Limitación de verificación**: no se probó el render visual en un navegador real (click
  interactivo, inspección del SVG generado); se verificó la integridad HTTP/JSON de punta a
  punta (servidor real respondiendo con la forma exacta que `app.js` consume) y la suite
  automatizada vía `WebApplicationFactory`. Esta es una categoría de validación distinta de
  "HTTP/JSON correcto" — no implica sospecha de fallo, pero el dashboard no debe considerarse
  visualmente terminado hasta completar el siguiente checklist manual:
  - [ ] Abrir el dashboard en un navegador real (`dotnet run` + visitar `http://localhost:<puerto>/`).
  - [ ] Ejecutar el botón "Ejecutar" y confirmar que la sección de resultado deja de estar oculta.
  - [ ] Comprobar el render de la curva de Equity (SVG con la polilínea visible, proporciones correctas).
  - [ ] Comprobar que la tabla de Trades se puebla (o queda vacía sin error, si el dataset demo no cierra Trades).
  - [ ] Comprobar la tabla de Resolución RN-11 (ambas columnas A/B, conteo de Fills coherente).
- **Fase 6 (Presentation) — estado global**: Pasos 1-4 completados. Deuda explícita heredada de
  todos los pasos: `StrategyCatalog`/selección dinámica de estrategia, `POST /api/datasets`
  (ingestión real de OHLCV), `GET /api/backtest/{runId}` + persistencia/historial de
  ejecuciones, autenticación, ejecución asíncrona. Ninguna implementada; el contrato HTTP actual
  deberá evolucionar solo cuando esas entidades existan de verdad.

## Metodologías evaluadas y no activadas

- **Pruebas metamórficas** — no activas como metodología principal porque
  las relaciones exigidas por el SPEC ya quedan cubiertas por determinismo
  (RNF-06) y pruebas por propiedades (CU-05). No descartadas por principio:
  reevaluar si aparecen relaciones metamórficas útiles no cubiertas por lo
  anterior.
- **Contratos en tiempo de ejecución** — no activos como metodología
  transversal porque los tipos ya cubren los estados inválidos donde
  corresponde (RN-01, RN-06) y las pruebas por propiedades cubren las
  invariantes sobre secuencias de ejecución (RN-04, RN-11, RNF-06).
  Validaciones runtime puntuales pueden añadirse en la implementación sin
  constituir una metodología declarada.
- **Migraciones como código** — no activa; ningún ID del SPEC habla de
  esquema evolutivo de persistencia.
- **Modelado de amenazas ligero** — no activo; el dominio no incluye datos
  personales.
