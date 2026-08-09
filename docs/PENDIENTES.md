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

- **UnrealizedPnL / M2M en `ResolutorVela.CalcularEquity`** — el cálculo de
  Equity por rama (`src/Domain/VelaResolution/ResolutorVela.cs`) usa
  `Cash + Margin`, sin componente de Unrealized PnL. Ningún test actual deja
  una posición viva al cierre de la vela resuelta, así que el alcance mínimo
  no lo ejercita. Pendiente de incorporación cuando exista un caso que lo
  requiera; no se amplía el alcance ahora sin requisito o test que lo fuerce.
- **Configuración real de `TasaMargen`** — `AplicadorFill.Aplicar`
  (`src/Domain/Portfolio/AplicadorFill.cs`) usa un valor por defecto
  (`0.1m`) porque el test aprobado en Etapa 2 (`InmutabilidadOrigenTests`)
  invoca el método con dos argumentos únicamente. Ese default es válido para
  satisfacer la API de tests existente, pero debe quedar separado de la
  configuración definitiva del Experimento — `ConfiguracionExperimento` no
  expone todavía `TasaMargen`. No se introduce esa expansión de
  configuración en esta etapa.

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
