# ADR-001 · Stack y arquitectura

**Estado:** Aceptado
**Fecha:** 2026-08-08

## Contexto

`SPEC.md` v6.0 define un motor de backtesting cuantitativo batch (MVP), talla **L**,
para un desarrollador único sin equipo de soporte, con horizonte de retomar el
proyecto tras 8 meses sin tocarlo. El SPEC exige explícitamente (contrato del
documento, punto 3) no nombrar lenguajes, frameworks ni infraestructura — todas
las decisiones de este ADR son responsabilidad de esta fase, no del dominio.

Inventario que determina la talla: 14 RN, 20 CU, 4 EC, 13 RNF. La talla L se
justifica por el acoplamiento entre RN-08/09/10 (contabilidad FIFO/margin/
cross-zero), la duplicación completa de resolución de vela que exige RN-11
(doble trayectoria), y las invariantes transversales RNF-06/RNF-07
(determinismo y aislamiento) que atraviesan todos los módulos.

## Decisión

**Stack:** C# / .NET 8+
- `System.Decimal` (BCL, 128 bits) como tipo numérico para los valores
  financieros del dominio (precios, cantidades, cash, margin, PnL,
  fricciones — RNF-05). No implica que todo número del sistema (contadores,
  índices, Secuencia Causal, tamaños, hashes) deba representarse como
  `Decimal`; esos usan el tipo entero/numérico que corresponda a su propia
  naturaleza.
- `IEnumerable<T>` / `yield return` como mecanismo de iteración perezosa sobre el dataset.
- xUnit + CsCheck como herramientas de testing (unitario y por propiedades).

**Estilo arquitectónico:** monolito modular organizado por funcionalidad (slices
verticales). En el subárbol `Domain`, donde se concentran 12 de las 14 RN del
SPEC, el aislamiento se sostiene mediante **contratos explícitos entre
fronteras** (Strategy, Broker, Matching, Portfolio, VelaResolution) — cada
módulo expone una interfaz propia y ningún otro módulo depende de su
implementación interna. Esto no fija todavía un patrón cerrado tipo puertos y
adaptadores como estructura obligatoria; fija la propiedad que debe cumplirse
(aislamiento por contrato), dejando la forma concreta de esos contratos como
decisión de diseño posterior a este ADR.

**Frontera a proteger** (RNF-12):
- `src/Domain/**` no depende de `src/Infrastructure/**` ni de `src/Application/**`.
- `src/Application/**` orquesta usando los contratos que expone `src/Domain/**`,
  sin definir reglas de negocio propias.
- `src/Infrastructure/**` implementa adaptadores para los contratos definidos
  en las fronteras correspondientes, sin introducir reglas de negocio.

Esta es una decisión arquitectónica que **ayuda** a cumplir RNF-12; el SPEC no
impone estructura de carpetas ni la nombra — el cumplimiento real de RNF-12
depende de los contratos de diseño, no de la organización física por sí sola.

**Mapa de responsabilidades:**

| Responsabilidad | Frontera conceptual (SPEC) | Contratos principales | Dependencias permitidas |
|---|---|---|---|
| Observar DataSlice(N), emitir OrderRequest | Strategy | Recibe `DataSlice`, devuelve 0..N `OrderRequest` | Ninguna hacia Broker/Engine/Portfolio |
| Agrupar la bolsa de Requests del ciclo N por activo, rechazar contradicción Buy+Sell (RN-14), validar capacidad en dos fases (RN-12), registrar Order con Secuencia Causal | Broker | Recibe bolsa de `OrderRequest`, produce `Order` registradas o `OrderRequestRejected` | Depende de Portfolio |
| Cruzar Orders Pending contra OHLCV(N+1) para una trayectoria dada, matemáticas de cruce/gaps, OCO, Stop-Limit, emitir Fills | Matching Engine | Recibe Orders Pending + Candle(N+1) + trayectoria (A o B), produce Fills de esa rama | No depende de Strategy ni de Broker; no decide la rama oficial |
| Mutar Position vía Fill, FIFO por lotes, Margin, Realized/Unrealized PnL, Trade, Reserva Preventiva | Portfolio | Recibe Fills de una rama, produce PositionChanged/RealizedPnLRecognized/TradeClosed y Equity de esa rama | No depende de Matching Engine ni de Strategy |
| Coordinar resolución de una vela: ejecutar Matching Engine + Portfolio para rama A y rama B sin contaminación cruzada, comparar Equity, seleccionar rama oficial (RN-11) | Resolución de Vela | Recibe Orders Pending + Candle(N+1), produce el resultado de la rama oficial | Depende de contratos de Matching Engine y Portfolio; mecanismo de aislamiento entre ramas no fijado aún |
| Orquestar el ciclo temporal completo: avanzar reloj, invocar Strategy, Broker, Resolución de Vela | Backtest (orquestador) | Coordina las fronteras anteriores en la secuencia N → N+1 | Depende de los contratos anteriores; vive en Application |

Nota conceptual: Margin (estado contable de posiciones ejecutadas, RN-08) y
Reserva Preventiva (compromiso de capacidad sobre Orders Pending, RN-12) viven
ambos dentro de Portfolio como estado financiero, pero son conceptos distintos
y no intercambiables.

**Talla:** L

**Árbol de directorios exacto:**

```
TD_Project/
├── SPEC.md
├── docs/
│   ├── adr/
│   │   ├── ADR-001-stack-y-arquitectura.md
│   │   └── ADR-002-estrategia-verificacion.md
│   ├── PENDIENTES.md
│   └── BENCHMARKING.md          (si aplica)
├── TESTING_STRATEGY.md
├── README.md
├── src/
│   ├── Domain/
│   │   ├── Shared/              (Candle, DataSlice, tipos base compartidos del dominio)
│   │   ├── Strategy/            (contrato Strategy — RN-13)
│   │   ├── Broker/              (validación de bolsa, RN-14, RN-12, Secuencia Causal — RN-04)
│   │   ├── Matching/            (Matching Engine: cruce, gaps, OCO, Stop-Limit — RN-01,02,03,05,06)
│   │   ├── Portfolio/           (Position, Lote, Trade, Cash, Margin, Reserva Preventiva — RN-07,08,09,10,12)
│   │   └── VelaResolution/      (coordina Matching+Portfolio por rama, selecciona oficial — RN-11)
│   ├── Application/             (Backtest/Experiment: orquestación N→N+1, sin reglas propias)
│   └── Infrastructure/          (lectura de Dataset, serialización de Result — adaptadores)
├── tests/
│   ├── Domain.Tests/
│   │   ├── Broker/
│   │   ├── Matching/             (incluye transiciones de Order — RN-01)
│   │   ├── Portfolio/
│   │   ├── VelaResolution/
│   │   ├── Precision/
│   │   └── Determinismo/
│   ├── Application.Tests/
│   │   └── Fakes/
│   └── Infrastructure.Tests/
│       └── Fakes/
└── TD_Project.sln
```

**Rutas fijas:**
- `SPEC.md` vive en la raíz del proyecto.
- Los tests viven en `tests/`, en subcarpetas espejo de `src/`, con sufijo
  `.Tests` en el nombre del proyecto (`Domain.Tests`, `Application.Tests`,
  `Infrastructure.Tests`) — convención estándar de proyectos de test .NET,
  aplicada durante la implementación (Prompt 3, Etapa 1).

## Alternativas descartadas

**Opción A · Python 3.12+ (mínima).**
Descartada porque, aunque `decimal.Decimal` cumple RNF-05 igual que C#, el
tipado dinámico deja el cumplimiento de las fronteras de RNF-12 dependiendo
solo de disciplina personal, no del compilador — mayor riesgo a los 8 meses
sin tocar el proyecto. Rendimiento esperado inferior para el volumen de
RNF-01/02/03, aunque no medido con benchmark controlado.

**Opción C · Java/Kotlin (techo alto).**
Descartada principalmente porque su herramienta de pruebas por propiedades
(`jqwik`) está en modo de mantenimiento puro desde 2025, sin desarrollo de
nuevas features — riesgo concreto para un proyecto de largo plazo sin equipo.
Además, `BigDecimal` ofrece precisión arbitraria que ningún ID del SPEC exige
(RNF-05 fija 8 decimales, cubiertos sobradamente por `System.Decimal`).

**TypeScript/Node.js.**
Descartada para RNF-05: no tiene tipo decimal en su biblioteca estándar (el
candidato TC39 Decimal sigue en Stage 1, sin fecha). Satisfacerlo exigiría una
dependencia de terceros que las otras tres opciones no necesitan.

**Aislamiento por contratos extendido a todo el sistema (no solo Domain).**
Descartado: Strategy y Application no muestran densidad de reglas de negocio
que lo justifique más allá de exponer su propio contrato: Strategy solo
observa DataSlice y emite OrderRequest; Application solo orquesta. Forzar el
mismo nivel de aislamiento formal ahí sería una capa sin regla del SPEC que
la exija (regla permanente 6, YAGNI).

## Consecuencias

- Cumplimiento de RNF-05 sin dependencias externas.
- Fronteras de Fase 3 verificables por herramientas de análisis de arquitectura
  para .NET (decisión de implementación, no de este ADR).
- El rendimiento esperado de `System.Decimal` frente a RNF-01/02/03 es una
  hipótesis, no un hecho medido: **queda como condición abierta**, a validar
  con benchmark propio antes de fijar umbrales.
- El mecanismo de aislamiento entre ramas A/B de RN-11 (clonación, snapshots,
  u otro) no queda fijado en este ADR — es una decisión de diseño de contratos
  posterior a esta arquitectura base.
