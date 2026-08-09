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
