# Pendientes

Decisiones aplazadas, metodologías evaluadas y no activadas, y riesgos
detectados sin resolver durante Fases 1-5. Este documento se actualiza en
cada auditoría posterior; no se cierra ni se vacía.

## SPEC 7.0 (RN-15 a RN-19, CU-21 a CU-24, RNF-16) — pendientes registrados al cierre

- **[DEUDA DE TRAZABILIDAD, preexistente a este delta, no corregida por aislamiento de
  alcance]** `verify.ps1` comprobación 3 reporta 6 archivos de test sin cita `spec:`
  inmediatamente adyacente a cada `[Fact]`/`[Theory]`: `GestorCapitalTests.cs`,
  `ModeloCostesTests.cs`, `ModeloEconomicoBaseTests.cs`, `ClasificadorIntencionOrdenTests.cs`,
  `GestorVolatilitySizingTests.cs`, `VentanaDeVelasTests.cs`. Confirmado que ninguno pertenece
  al delta SPEC 7.0 (ya estaban modificados/sin trackear en el árbol de trabajo antes de
  iniciar este delta). No corregidos porque hacerlo excede el alcance declarado en la Etapa 0
  de este cambio — corregirlos habría requerido tocar archivos fuera de la lista autorizada.
  Pendiente: mover la cita `spec:` de cada archivo a un comentario inmediatamente adyacente a
  cada método de prueba individual (hoy la cita vive en un comentario de cabecera de clase, no
  contiguo).
- **[MEJORA DE COBERTURA, no solicitada en este delta]** Tests HTTP dedicados para los 3
  endpoints nuevos (`POST/GET /api/datasets`, `POST /api/strategies/dsl/run`,
  `POST /api/capital-managers/recommend`) — la cobertura actual de CU-21/22/23 vive a nivel de
  librería (`Domain`/`Application`/`Infrastructure`) más el test de integración de punta a
  punta (`FlujoIntegracionCompletoTests`), sin ejercitar el contrato REST en sí (códigos de
  estado HTTP, forma exacta del JSON de request/response, casos de error vía `WebApplicationFactory`,
  mismo patrón que `BacktestRunEndpointTests.cs`). Decisión explícita del auditor: no ampliar
  cobertura fuera del plan aprobado sin una decisión separada. Si se requiere validación de
  contrato REST, debe abrirse como tarea/delta propio.

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
- ~~**[BUG, RN-11] Trayectorias A/B nunca divergían en un resultado real (Stop-Limit)**~~ —
  resuelto en la mini-investigación abierta entre Ronda 2 y Ronda 3 (2026-08-10).
  `MatchingEngine.ResolverStopLimit` recibía el parámetro `Trayectoria` pero nunca lo
  consultaba; el disparo del Stop y el cruce del Limit se evaluaban contra `vela.Open/High/Low/
  Close` sin ningún orden temporal, por lo que `EquityA == EquityB` siempre se cumplía y el
  desempate (selección de A) era, en la práctica, la única regla que jamás corría. Confirmado
  con evidencia empírica directa (no solo lectura de código) de que ni el ejemplo canónico
  CU-13 del propio SPEC divergía. Corregido con `RecorridoVela` (nuevo tipo) + recorrido
  tramo-a-tramo en `ResolverStopLimit`. Ver `CHANGELOG.md` para el detalle completo del fix y
  el caso de prueba congelado.
- **[BUG, mayor alcance que el cableado — CU-19/OCO] `BacktestRunner` no tiene mecanismo de
  agrupación OCO; `ResolverOco`/`ResolutorVela.ResolverOco` son inalcanzables desde una
  ejecución real** — confirmado durante la mini-investigación de RN-11 (2026-08-10):
  `ResolverOco` (`src/Domain/Matching/MatchingEngine.cs`) y `ResolutorVela.ResolverOco`
  (`src/Domain/VelaResolution/ResolutorVela.cs`) están completos y cubiertos por tests
  unitarios propios (`tests/Domain.Tests/Matching/OcoTests.cs`,
  `TrayectoriasCanonicasTests.OcoAmbiguoResuelveLaRamaCruzadaYCancelaLaHermanaSegunTrayectoriaOficial`),
  pero `BacktestRunner.Ejecutar` (`src/Application/BacktestRunner.cs`) mantiene
  `ordenesPending` como una lista plana de `Order` y solo llama a `ResolutorVela.Resolver` (el
  camino no-OCO) — no existe en ningún punto del flujo real la construcción de un `OcoGroup`.
  A diferencia de RN-12 (donde solo faltaba la llamada), aquí falta la capacidad completa:
  `Order`/`OrderRequest` no tienen forma de expresar "estas dos órdenes son un grupo OCO", y
  `IStrategy.Observar` no tiene manera de declarar esa relación al devolver `OrderRequest[]`.
  CU-19 ("OCO Múltiple Ambiguo") no puede ocurrir nunca en una ejecución real hoy. Corrección
  probablemente toca: modelo de `Order`/`OrderRequest` (concepto de grupo/vínculo), creación de
  grupos desde `RegistradorOrdenes` o la estrategia, `BacktestRunner.Ejecutar` (agrupar antes de
  resolver), y tests de integración nuevos vía `BacktestRunner.Ejecutar` (no solo unitarios del
  resolvedor, que ya existen y no prueban el flujo real). Deliberadamente **no incluido** en el
  fix de RN-11 — alcance mayor, requiere su propio ciclo de diseño.
- ~~**[BUG, RN-08/RN-14] `OrderRequest.Cantidad <= 0` no se rechazaba en la frontera de
  entrada**~~ — resuelto en el Ciclo A de la Ronda 3 (2026-08-10). `ValidadorBolsaRequests`
  extendido para rechazar atómicamente cualquier bolsa que contenga una `Cantidad <= 0`, mismo
  mecanismo ya usado por RN-14. Ver `CHANGELOG.md` para el detalle completo.
- ~~**[BUG/vacío de contrato, RNF-09] Resultado no-`Success` indistinguible por forma de un
  `Success` con cero actividad**~~ — resuelto en el Ciclo A de la Ronda 3 (2026-08-10) como test
  de contrato, no como cambio de implementación: `Estado` ya existía, ya se propagaba
  correctamente y ya se mostraba en el dashboard — el vacío era la ausencia de una garantía
  explícitamente verificada de que sigue siendo el discriminante válido aun cuando el resto del
  payload es idéntico. Ver `CHANGELOG.md`.

- ~~**[BUG, RN-10] `AcumuladorTrade.PrecioApertura`/`CantidadInicial` contaminados tras
  Cross-Zero**~~ — resuelto en el Ciclo 4A de la Ronda 4 (2026-08-10). El ciclo que queda
  abierto tras un Cross-Zero heredaba el precio/magnitud del ciclo previo ya cerrado, porque
  `AntesDeAplicar` solo detectaba apertura cuando la posición previa era exactamente cero (nunca
  ocurre en Cross-Zero, donde cierre y apertura son el mismo Fill). Ver `CHANGELOG.md`.
- ~~**[BUG, RNF-05, mismo patrón OCO/RN-12] `RedondeoReporte` implementado y probado en
  aislamiento, nunca invocado**~~ — resuelto en el Ciclo 4A de la Ronda 4 (2026-08-10).
  Conectado en `ResultDtoMapper` para `MetricsDto.EquityFinal`. Ver `CHANGELOG.md`. Corrige
  además la descripción de este gap: no era "pendiente de implementar" (como decía la nota del
  Paso 1 de Fase 6, línea ~263 de este documento), la pieza ya existía completa — el gap real
  era la ausencia de cableado, patrón ya visto en RN-12 y CU-19/OCO.
- **[PENDIENTE DE DEFINICIÓN DE PRODUCTO, no bug — semántica ambigua, no redefinida sin
  evidencia] `AcumuladorTrade.CantidadInicial` no acumula múltiples Fills de apertura en el
  mismo sentido** — detectado junto al bug de Cross-Zero de arriba (Ronda 4), pero es un defecto
  distinto y NO corregido: si una posición se abre con más de un Fill del mismo lado antes de
  cerrarse (ej. Buy 3 @100, luego Buy 4 @110, luego se cierra todo junto), `CantidadInicial`
  reporta solo la magnitud del primer Fill (3), no el tamaño real de la posición cerrada (7). El
  glosario de `SPEC.md` define `Trade` como "ciclo vital de exposición desde apertura hasta
  cierre total" pero no precisa si `CantidadInicial`/`PrecioApertura` deben representar el
  primer Fill de apertura o un agregado del ciclo completo (cantidad total, precio promedio
  ponderado) — el test preexistente `UnaPosicionConDosReduccionesCierraUnSoloTradeAlLlegarACero`
  fija la convención actual (`CantidadInicial=10` para una única apertura de 10, sin ejercitar
  el caso multi-fill), y el comentario de diseño en `AcumuladorTrade.cs` confirma la intención
  original ("el primer Fill que rompe cero"), pero eso no responde si esa intención es correcta
  para el caso de aperturas fraccionadas. Antes de tocar código: decidir si `CantidadInicial`
  debe ser "magnitud del primer Fill de apertura" (semántica actual, ya consistente) o "magnitud
  total del ciclo" (requeriría rediseño de `AcumuladorTrade`, análogo al fix ya hecho para
  Cross-Zero). No cambiar el contrato sin esa decisión.
- ~~**[MEJORA DE COBERTURA, severidad alta] El demo HTTP en producción no ejercita ninguno de
  los 5 bugs ya corregidos en rondas anteriores**~~ — resuelto en el Ciclo 4B (2026-08-10):
  `DatasetDemo`/`EstrategiaDemo` ahora ejecutan el caso canónico de divergencia RN-11
  (Stop-Limit 102/101), verificado con `dotnet run` real mostrando `BranchResolutions` con
  trayectorias A/B genuinamente distintas. Ver `CHANGELOG.md`. Nota: este escenario cubre RN-11;
  no cubre RN-08/RN-09 (posición corta), RN-10 (Cross-Zero), ni `Cantidad<=0` rechazada — sigue
  pendiente si se quiere una demo que ejercite los 5 bugs a la vez (no se intentó forzar los 5
  en un único dataset/estrategia lineal, por simplicidad de la demo).
  `experimentInfo` sigue sin consumirse en `app.js` — no incluido en este ciclo, alcance menor,
  puede añadirse junto a una futura iteración del dashboard.
- ~~**[MEJORA DE COBERTURA] `app.js` no consume `fillLog` (incluido `CostoFriccionReal`,
  agregado en Ronda 3), ni `portfolioSnapshots`**~~ — resuelto en el Ciclo 4B (2026-08-10):
  agregadas las secciones "Fill Log" y "Posición por vela" en `index.html`/`app.js`. Ver
  `CHANGELOG.md`.
- **[REGLA NUEVA CANDIDATA, no decidida] Contrato de ciclo de vida de `IStrategy` — extender a
  ejecución paralela** — la nota ya existente más abajo ("Aislamiento de `IStrategy` entre
  llamadas a `BacktestRunner.Ejecutar`") documentaba el caso de reutilización SECUENCIAL. La
  Ronda 4 confirmó con evidencia reproducible que el mismo vector se agrava en ejecución
  PARALELA con la misma instancia (race condition real y confirmada en el estado mutable de la
  Strategy, aunque el motor mismo permanece perfectamente aislado — verificado explícitamente:
  sin campos `static` mutables en todo `src/Domain`+`src/Application`, `PortfolioState`/
  `RegistradorOrdenes` se recrean limpios en cada llamada incluso tras un `InternalCrash`
  previo). Ambos casos (secuencial y paralelo) comparten la misma causa raíz y la misma
  resolución arquitectónica pendiente — deben decidirse juntos, no por separado. No modificar
  RNF-07 todavía.
- ~~**[BUG, rendimiento] `BacktestRunner` O(n²) en el loop principal — invisible con datasets
  sintéticos, bloqueante a escala real**~~ — resuelto (2026-08-10). `DataSlice` se construía con
  `config.Velas.Take(n + 1).ToList()` en cada vela (copia de toda la porción vista hasta el
  momento), y las órdenes "Pending" se recalculaban con `ordenesPending.Where(...).ToList()`/
  `.First(...)` sobre el historial completo de órdenes en cada vela y cada Fill. Con datasets
  sintéticos (~200 velas, todo el trabajo de Fase 1/1.5) el costo es imperceptible; con el
  primer dataset real de escala completa (Fase 2A, BTCUSDT 1m, 527.040 velas) el proceso quedó
  colgado sin completar el primer timeframe de Fase 2C — nunca se había alcanzado ese volumen
  de velas hasta la introducción de datos reales. Directamente relevante al escenario de
  referencia de RNF-01/02/03 (arriba, "10M velas") — este bug habría bloqueado ese benchmark
  también. Corrección: nueva `Domain.Shared.VentanaDeVelas` (vista O(1) sobre la lista
  original, preserva el bloqueo físico de RN-13) + lista `ordenesActivas` mantenida en paralelo
  a `ordenesPending`. Ver `CHANGELOG.md` para el detalle completo. Test de regresión
  `RendimientoEscalaTests` corre 527.040 velas dos veces con `Timeout=30_000`ms.
  **Deuda residual, no resuelta por este fix**: una estrategia que nunca cierra posiciones
  (acumula lotes vivos sin límite, ej. un fake de test como `EstrategiaMarketSiempre`) sigue
  siendo O(n) por vela en el cálculo de `UnrealizedPnL`/`Margin` sobre `PortfolioState.
  LotesVivos` — no es un patrón que produzca ninguna estrategia real del laboratorio (Tres
  Mosqueteros, MHI, ni las de `exploration/laboratorio/Fixtures`), por lo que queda fuera de
  alcance de este fix. Si en el futuro se necesita soportar ese patrón a escala, requiere una
  estructura de lotes con inserción/consulta O(log n) o O(1), no una lista lineal.

## Deuda documentada — Ronda 3 (auditoría de uso cotidiano), no comprometida a implementación

Hallazgos clasificados originalmente como `[REGLA NUEVA]` por los agentes de Ronda 3, revisados
y reclasificados explícitamente como candidatos/deuda documental — **no abrir SPEC.md, no
comprometer implementación** hasta que se decida abordarlos en un ciclo propio (Ciclos B/C).

- **[CANDIDATO FUTURO, Presentation/Reporting] `CapitalInicial` no expuesto en ningún DTO; sin
  retorno normalizado** — `ExperimentInfoDto`/`MetricsDto` no exponen `CapitalInicial` ni un
  campo de retorno porcentual. Comparar `PnLTotal`/`EquityFinal` absolutos entre corridas de
  distinto capital o distinta longitud de dataset lleva a conclusiones erróneas (ejemplo
  numérico: 10% de retorno sobre capital 1000 vs. 0.5% sobre capital 100000 tienen el mismo
  `PnLTotal` nominal si ambos dan 100 y 500 respectivamente, pero uno es claramente más
  eficiente). Destino probable: `ExperimentInfoDto.CapitalInicial` + `MetricsDto.
  RetornoPorcentual`. No abrir hasta tener más evidencia de uso real que lo justifique.
- **[PENDIENTE DE ANÁLISIS] `TradeDto` sin campo de dirección (Long/Short)** — `TradeDto.
  CantidadInicial` es siempre magnitud sin signo (`Math.Abs`, confirmado en `AplicadorFill.cs`),
  sin campo `Side` que indique dirección, a diferencia de `FillLogEntryDto` (magnitud + `Side`
  explícito) y de `LoteDto` (cantidad signada, sin `Side`) — tres convenciones distintas entre
  DTOs hermanos del mismo `ResultDto`. Pregunta a resolver antes de tocar el contrato: ¿el
  consumidor puede reconstruir Long/Short con los datos actuales de otra forma (ej. cruzando con
  `FillLog`), o la información realmente se pierde en `TradeDto`? No modificar el DTO hasta
  responder esa pregunta.
- **[LIMPIEZA FUTURA, no urgente] `TradeDto.PrecioCierre` es `decimal?` pero nunca llega `null`
  en la práctica** — `BacktestRunner.cs` solo agrega a `Trades` cuando `TradeCerrado is not
  null`, por lo que `Trades`/`TradeDto[]` representa exclusivamente ciclos cerrados; un trade
  abierto se reporta solo vía `PortfolioSnapshotDto.LotesVivos`, nunca en `Trades` con
  `PrecioCierre: null`. La nulabilidad es "de sobra" sin explicar. Si se confirma que no puede
  existir un `Trade` abierto en esa lista, quitar el `?` sería limpieza de contrato válida, pero
  no es urgente.
- **[DEUDA DOCUMENTAL, aprobada como importante] Los campos `*Timestamp` del `ResultDto` no
  documentan unidad ni zona horaria** — afecta a los seis campos que remontan a `Candle.
  Timestamp` (`EquityPointDto.Timestamp`, `FillLogEntryDto.VelaTimestamp`, `PortfolioSnapshotDto.
  Timestamp`, `BranchResolutionDto.Timestamp`, `ExperimentInfoDto.FechaInicioTimestamp/
  FechaFinTimestamp`). Un consumidor del JSON no puede convertir esos valores a fecha/hora sin
  adivinar o inspeccionar el dataset de origen — riesgo real de interoperabilidad. El vacío nace
  en `Domain.Shared.Candle`, no en Contracts. Acción propuesta: confirmar la unidad real que usa
  el dataset de origen y documentarla (comentario XML en `Candle.cs`, y/o glosario de `SPEC.md`
  si se considera parte del contrato de datos, y/o README de la API) — no requiere
  necesariamente una entrada de SPEC.md, puede resolverse como documentación de Contracts.
- **[MEJORA DE COBERTURA, no cambio de contrato] Tests end-to-end faltantes de coherencia
  inter-capa** — tres huecos confirmados por lectura de los tests existentes (Ronda 3, ángulo
  "consistencia interna"): (1) ningún test ejercita `MetricsDto.PnLTotal` cuando hay una
  Position viva al cierre (hoy `PnLTotal` solo suma Trades cerrados; con posición viva no
  representa el PnL económico total — mismo terreno conceptual que la nota ya existente "Cash
  vs. Equity" más abajo en este documento); (2) ningún test compara `BranchResolutionDto.
  FillsA/FillsB` contra `PortfolioSnapshotDto` de la misma vela a través de `BacktestRunner`
  (hoy solo se verifica a nivel `ResolutorVela` aislado); (3) `ResultDtoMapperTests` usa
  fixtures manuales con datos "convenientes" sin relación económica real entre sí (ej. Trades y
  EquityCurve construidos independientemente), nunca un `ResultadoBacktest` producido por una
  ejecución real de `BacktestRunner.Ejecutar`. Ninguno de los tres es un bug — la aritmética
  actual es correcta por construcción — es una mejora de cobertura de regresión futura.
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

- **[ACLARACIÓN CONCEPTUAL] Ítem #13 — Modelo de Fricción / Slippage:** Aclarar formalmente si `Friction Model` en RN-12 y RNF-08 abarca únicamente comisiones o si debe incluir un componente explícito de *slippage* / *market impact*. No requiere cambios en `SPEC.md` v6.0 hoy; es una precisión conceptual pendiente. **Escenario de laboratorio preparado, no ejecutable todavía**: `exploration/laboratorio/` registra un escenario `FriccionExtrema` (documentado, no incluido en el conteo de hipótesis PASA/FALLA) para cuando exista esta capacidad — hoy `MatchingEngine` fija `CostoFriccionReal` en `0` en todo Fill, así que cualquier dataset de "comisión extrema" solo confirmaría que la fricción no existe, no probaría nada real.
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

## Laboratorio Sintético (`exploration/laboratorio/`) — Fases 1-2D cerradas, Caso 2 diferido

Fases 1/1.5 (datasets sintéticos), 2A/2B (datos reales BTCUSDT congelados + agregación
multi-timeframe determinista) y 2C (evaluación de comportamiento de Tres Mosqueteros/MHI
Mayoría sobre datos reales, 6 timeframes, validada funcionalmente el 2026-08-10) cerradas — ver
`exploration/laboratorio/DISENO_FASE2.md`, `DISENO_FASE2B.md`, `PLAN_FASE2A.md`,
`DISENO_FASE2C.md`. `[BUG]` acumulado en todo el laboratorio: 0. `[REGLA NUEVA]`: 0.

**Fase 2D (normalización de modelo financiero) cerrada como hoja de ruta el 2026-08-11** — ver
`DISENO_FASE2D.md` para el detalle completo. Decisión final: el sistema persigue **Caso 1 —
Laboratorio de estrategias** bajo **Modelo A — la estrategia controla la cantidad**, sin cambios
en `src/`, `SPEC.md`, `IStrategy`, ni contratos. Consecuencias:

- **Métricas oficiales de esta etapa**: operativas (cantidad de operaciones, ganadas/perdidas,
  winrate, rachas negativas, uso de martingala inicial/M1/M2, exposición máxima, operaciones
  abiertas al cierre, consistencia de ejecución) — ya implementadas y válidas desde Fase 1.5/2C,
  sin cambios necesarios.
- **No oficiales para esta etapa** (no incorrectas, solo prematuras): retorno porcentual,
  rendimiento anualizado, Sharpe/Sortino, cualquier métrica de riesgo financiero comparable
  entre estrategias o mercados. Los retornos de Fase 2C permanecen etiquetados como "resultado
  bajo modelo de posición actual, no retorno financiero comparable" — no se recalculan mientras
  el sistema opere bajo Caso 1.
- **Supuestos financieros documentados, deliberadamente sin resolver bajo Caso 1** (detectados
  durante Fase 2D, clasificados `[SUPUESTO FINANCIERO NO EXPLICITADO]`, no `[BUG]`):
  `TasaMargen = 0.1` hardcodeado en `AplicadorFill.Aplicar` (`src/Domain/Portfolio/
  AplicadorFill.cs:13`, no expuesto en ningún contrato ni en `SPEC.md`) y
  `CostoFriccionReal = 0` fijo en cada `Fill` (`src/Domain/Matching/MatchingEngine.cs`, mismo
  gap del Friction Model ya señalado en este documento — ver más abajo, "Modelo de Fricción /
  Slippage"). Ninguno requiere corrección ahora — son decisiones pendientes exclusivamente de
  cuando se abra Caso 2.
- **Regla para la evolución a Caso 2 (Simulador financiero realista) o Caso 3 (Plataforma
  multi-mercado), cuando se decida abrirla**: antes de tocar código, definir explícitamente
  modelo de sizing, modelo de riesgo, modelo de margen, modelo de costos, unidad financiera
  comparable, e impacto sobre contratos (`IStrategy.Observar` cambiaría de firma bajo Modelo
  B/C — rompe todas las implementaciones existentes, incluidas las fakes de test). No debe
  hacerse una migración parcial que mezcle Caso 1 y Caso 2. Mismo patrón de "documento antes de
  código" ya seguido en Fase 2A/2B/2C/2D.

**Próximo uso previsto del laboratorio** (no una fase numerada nueva, sino explotación del
objetivo ya definido): evaluar nuevas estrategias bajo el mismo protocolo — dataset definido,
timeframe definido, estrategia congelada, métricas operativas, análisis de comportamiento — sin
interpretar retorno monetario hasta que se abra formalmente Caso 2.
