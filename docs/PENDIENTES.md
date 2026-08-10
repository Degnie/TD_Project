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

## Bugs detectados durante auditoría de uso cotidiano (adaptación del Prompt 12, un solo rol)

- **[BUG, RN-12/CU-15] Validación de capacidad financiera (`ValidadorCapacidad`) implementada
  pero desconectada del flujo real de ejecución** — `ValidadorCapacidad.Validar` (`src/Domain/
  Broker/ValidadorCapacidad.cs`) calcula correctamente `CashDisponiblePrevio` contra la reserva
  proyectada de una orden, y está cubierto por tests unitarios aislados
  (`tests/Domain.Tests/Broker/ReservaPreventivaTests.cs`: `SeApruebaLaOrdenSi...`,
  `SeRechazaLaOrdenSi...`). Pero `RegistradorOrdenes.Registrar` (`src/Domain/Broker/
  RegistradorOrdenes.cs`) registra toda `OrderRequest` incondicionalmente, y
  `BacktestRunner.Ejecutar` (`src/Application/BacktestRunner.cs`) solo invoca
  `ValidadorBolsaRequests.Evaluar` (RN-14, contradicción Buy+Sell) antes de registrar — nunca
  `ValidadorCapacidad`. Consecuencia: ninguna orden se rechaza jamás por falta de capital,
  sin importar cuán bajo sea `CapitalInicial` frente al tamaño de las órdenes. CU-15
  ("Falla validación preventiva → OrderRequestRejected") no ocurre nunca en una ejecución real
  hoy, pese a que el mecanismo que lo implementaría existe y está probado en aislamiento.
  Mismo patrón estructural que el bug ya corregido de `AplicadorFill` no conectado a
  `ConsumidorFifo`/`ResolutorCrossZero` (Fase 6): piezas construidas y probadas por separado,
  nunca cableadas al flujo real. Detectado el 2026-08-09 al construir un fixture de auditoría
  con estrategias reales. **No corregido a pedido explícito del usuario** — para esta ronda de
  auditoría de eficiencia de estrategias, el capital se considera deliberadamente no limitante.
  Queda pendiente decidir si se corrige (conectar `ValidadorCapacidad` al flujo de
  `BacktestRunner`, con su test de regresión citando RN-12/CU-15) en una ronda futura.
- ~~**[BUG PREEXISTENTE] `CalculadoraLotes.AbrirLote` produce `Margin` negativo en aperturas
  Short puras**~~ — resuelto en la Ronda 1 de auditoría de uso cotidiano (2026-08-09), como
  efecto colateral de corregir el bug de signo en `ConsumidorFifo`/`AplicadorFill` (mismo
  archivo, misma causa raíz: confundir dirección con magnitud). `Margin` ahora se calcula
  siempre sobre `Math.Abs(cantidad)`. Ver entrada de `CHANGELOG.md` para el detalle completo
  del fix. Esta entrada permanecía abierta desde la Fase 6 (ver más abajo, "Vacíos detectados
  durante la implementación").
- **[CONTRATO ABIERTO, pendiente de decisión arquitectónica] Aislamiento de `IStrategy` entre
  llamadas a `BacktestRunner.Ejecutar`** — confirmado con evidencia reproducible (Ronda 1):
  reutilizar la misma instancia de `IStrategy` en dos llamadas a `Ejecutar` produce resultados
  distintos, porque el motor no resetea ni valida el estado interno de la estrategia recibida.
  El motor en sí (`PortfolioState`, `RegistradorOrdenes`, listas de Fills/Trades) se instancia
  fresco en cada llamada — el único vector de fuga es el parámetro `strategy`, que el motor no
  controla. **No se decide todavía quién es responsable del aislamiento**: (A) el usuario de
  `IStrategy`, con un contrato documental "una instancia por ejecución, no reutilizar entre
  backtests" — simple, sin cambios de arquitectura; o (B) el motor, garantizando determinismo
  aun con una instancia reutilizada — implicaría clonado/factory/reset obligatorio, sin
  soporte hoy. RNF-06/RNF-07 hablan de aislamiento del motor y del estado del backtest, no
  necesariamente de objetos externos entregados por el usuario — por eso no se redacta como
  RNF nueva todavía. Decisión previa requerida: ¿`IStrategy` es un componente controlado por
  el motor o un plugin externo con contrato de uso propio? Esa decisión antecede al fix.
- **[MEJORA CANDIDATA, Presentation/Reporting — no requiere cambio de `Domain`] Claridad
  Cash vs. Equity cuando queda una Position viva al cierre del backtest** — confirmado (Ronda
  1) que la aritmética interna es correcta (verificado contra RN-08: la diferencia
  Cash/Equity coincide exactamente con Margin + UnrealizedPnL de la posición abierta), pero
  el reporte no distingue explícitamente "resultados realizados" (Trades cerrados) de
  "exposición viva al cierre" (posición abierta, M2M). Candidata para una futura ampliación
  de la capa de presentación (no de RNF-09 en sí, que ya cumple con estados exclusivos):
  una sección o métricas separadas de Closed Realized PnL / Unrealized PnL / Cash / Margin
  bloqueado / Equity total. No se implementa hasta que se decida explícitamente abordar esta
  mejora.

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
  - [ ] Comprobar que la trayectoria descartada (RN-11) se lee como "simulación válida no
    seleccionada" y no como un error o dato inválido (observación del auditor tras Fase 2 del
    borrador visual — Prompt 05).
  - [ ] Comprobar la lectura del dashboard en pantalla pequeña (viewport angosto, sin scroll
    horizontal, tablas legibles).
- **Fase 6 (Presentation) — estado global**: Pasos 1-4 completados. Deuda explícita heredada de
  todos los pasos: `StrategyCatalog`/selección dinámica de estrategia, `POST /api/datasets`
  (ingestión real de OHLCV), `GET /api/backtest/{runId}` + persistencia/historial de
  ejecuciones, autenticación, ejecución asíncrona. Ninguna implementada; el contrato HTTP actual
  deberá evolucionar solo cuando esas entidades existan de verdad.

## Borrador visual (Prompt 05 · Repinta · Claude) — en construcción

- **Fase 1-2 completadas**: identidad "mesa de control de laboratorio" (no dashboard de
  trading), tokens de color/tipografía/espaciado aplicados a
  `src/Presentation/TD_Project.Api/wwwroot/{index.html,styles.css,app.js}`. RN-11 tratada como
  elemento de firma visual (reordenada antes que Trades). Trayectoria descartada: atenuada por
  opacidad, deliberadamente sin `line-through` (corregido tras observación de auditoría — un
  tachado se lee como "inválido", y ambas trayectorias A/B son simulaciones válidas, RN-11 solo
  selecciona una).
- **Fase 3 completada**: hover de `tbody tr` limitado a `@media (hover: hover) and (pointer: fine)`
  (evita estados hover fantasma en táctil); entrada de `#resultado` vía `opacity` +
  `@starting-style` (250ms ease-out) al dejar de estar `hidden`; token muerto
  `--transition-standard` renombrado a `--transition-entry` con uso real. **Nota de
  compatibilidad** (observación de auditoría): `@starting-style` depende del soporte del
  navegador — en navegadores que no lo soportan, `#resultado` aparece instantáneo en vez de con
  transición suave. No es un bloqueo: es un fallback natural sin pérdida de funcionalidad, solo
  de refinamiento visual.
- **Fase 4 completada** (`web-design-guidelines`, reglas Vercel obtenidas vía WebFetch):
  corregidos automáticamente 2 hallazgos críticos — `color-scheme: dark` en `<html>`
  (`index.html`) y corrección ortográfica en mensaje de error visible ("respondio" →
  "respondió", `app.js`). 3 hallazgos no críticos evaluados y **no aplicados** por decisión
  explícita de producto:
  - **[PENDIENTE FUTURO] Formato de presentación financiera (`Intl.NumberFormat`)** — hoy los
    valores de `ResultDto` (Equity, PnL, precios) se muestran como texto crudo del JSON, sin
    agrupación de miles ni formato localizado. No es un ajuste cosmético menor: requiere definir
    antes una política de formato de reporte (locale fijo vs. dependiente del navegador,
    decimales por tipo de dato, si el dashboard refleja la precisión interna del motor u ofrece
    una lectura humana simplificada) — la presentación no debe alterar la interpretación del
    resultado financiero. Si se adopta, debe ir acompañado de una especificación explícita, no
    aplicarse como parche puntual.
  - **[DESCARTADO, severidad LOW]** `<meta name="theme-color">` — beneficio mínimo (integración
    con barra de navegador móvil), no justifica el cambio ahora.
  - **[DESCARTADO, decisión de identidad]** Aumentar contraste en `:hover`/`:active` del botón
    primario más allá del `outline`/`scale` ya existente — el ámbar debe permanecer como acento
    único y controlado (Fase 1); añadir más señal luminosa por interacción contradice esa
    restricción deliberada, no es una corrección de accesibilidad real (el foco visible ya
    cumple la regla).
- **Pendientes para el auditor visual (Prompt 06), declarados al cierre de Fase 5 del
  borrador**: decisiones visuales tomadas de forma autónoma durante el borrador que el
  auditor debería cuestionar explícitamente, no asumir como correctas por defecto:
  1. El reordenamiento de secciones (RN-11 antes que Trades) es una decisión editorial sobre
     qué es "lo más importante" en pantalla — el SPEC no dicta jerarquía de vista.
  2. La ausencia total de color semántico convencional (verde/rojo financiero) es una apuesta
     fuerte de identidad; si el usuario final espera lectura financiera convencional, podría
     generar fricción de aprendizaje inicial.
  3. El uso de opacidad (no tachado) para "descartada" es una interpretación de cómo comunicar
     "válida pero no seleccionada" — vale la pena revisarla con datos reales en pantalla, no
     solo en abstracto (ver también el punto 3 más abajo: hoy no hay etiqueta de texto).
  4. No se agregó ningún feedback adicional al pasar el mouse/tocar el botón "Ejecutar" más
     allá de color+escala — podría sentirse insuficiente en un flujo real de espera de
     respuesta HTTP (no hay estado de "cargando"; ver punto 4 más abajo).
  5. La tipografía Plex no está auto-hospedada: la mayoría de usuarios verá el fallback del
     sistema, no la tipografía realmente elegida en Fase 1 — divergencia entre "lo diseñado"
     y "lo que efectivamente se ve" que el auditor debería decidir si amerita resolverse.
- **[PENDIENTE DE VALIDACIÓN HUMANA, cierre Fase 5]** Auditoría del Prompt 05 identificó 5 puntos
  a validar antes de considerar cerrada la identidad visual (no bloquean el borrador, requieren
  prueba de uso con perfiles reales: ingeniero/auditor, usuario cuantitativo, usuario nuevo):
  1. Jerarquía RN-11 antes que Trades/métricas — decisión editorial fuerte, sin validar con
     usuarios que buscan primero "¿cuánto ganó / cuántos trades hubo?".
  2. Percepción del usuario sin verde/rojo financiero convencional.
  3. **Claridad visual de trayectoria descartada — confirmado que hoy NO existe una etiqueta de
     texto visible** (ej. "Estado: descartada por RN-11"), solo el atributo `data-descartada`
     que dispara opacidad vía CSS (`app.js` líneas 54-62, `styles.css` regla
     `td[data-descartada="true"]`). La sola atenuación visual podría no ser suficiente para un
     usuario nuevo — añadir esa etiqueta es un cambio de contenido/columna, no un ajuste de
     estilo, por eso se deja para la fase de auditoría siguiente en vez de aplicarse ahora.
  4. Ausencia de estado "cargando" durante la espera HTTP del POST — el flujo actual va de
     click a resultado sin feedback intermedio; evaluar un estado operativo textual ("Ejecutando
     simulación...") sin barra de progreso falsa, coherente con la metáfora de laboratorio.
  5. Decisión sobre auto-hospedar `IBM Plex` — correcta por ahora (evita archivos/licencias/peso
     para una demo local); reabrir solo si el producto pasa a distribución pública.
- **[RESTRICCIÓN DE ALCANCE, confirmada por auditoría]** No incorporar velas/candlestick,
  indicadores técnicos, heatmaps ni widgets financieros adicionales en este borrador. La
  identidad visual actual (curva de Equity + Resolución RN-11) se sostiene por restricción
  deliberada, no por carencia — cualquier gráfico nuevo requiere justificación propia antes de
  añadirse, no se agrega "porque otros dashboards lo tienen" (ver también
  `docs/BENCHMARKING.md` ítem #12, candidato diferido, no aprobado para esta fase).

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
