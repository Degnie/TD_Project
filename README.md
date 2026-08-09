[PROYECTO LIBRE]

# TD_Project · Motor de Backtesting Cuantitativo

Motor de backtesting cuantitativo batch offline para simular estrategias de
trading, con determinismo matemático, prevención estructural de look-ahead y
resolución OHLCV rigurosa mediante trayectorias adversas canónicas.

Especificación completa en [`SPEC.md`](SPEC.md). Decisiones de stack y
arquitectura en [`docs/adr/`](docs/adr/). Estrategia de pruebas en
[`TESTING_STRATEGY.md`](TESTING_STRATEGY.md).

## Arquitectura

### El sistema y su entorno

Este proyecto es un motor de backtesting cuantitativo: toma una
configuración de experimento (capital inicial, fricciones, dataset de velas
OHLCV, y una estrategia de trading) y produce un resultado financiero
completo y determinista, o un estado de fallo puro sin resultados parciales.
El motor (Domain + Application) corre como proceso batch de un solo hilo,
sin dependencias de red ni estado compartido entre ejecuciones — no conoce
la existencia de ninguna interfaz.

Sobre ese motor existe una capa opcional de presentación
(`src/Presentation/`) que expone el resultado vía HTTP y lo visualiza en un
dashboard web local. Es una fachada de solo lectura: no persiste
ejecuciones, no mantiene estado entre requests, y cada llamada ejecuta el
motor batch desde cero. Ver [visualizador local](#visualizador-local-opcional)
más abajo.

### Piezas internas y responsabilidades

El sistema se organiza en tres capas con una regla de dependencia
conceptual: Domain es independiente de Application e Infrastructure;
Application orquesta usando lo que Domain expone; Infrastructure adapta
hacia el exterior sin introducir reglas propias.

- **Domain** concentra toda la lógica de negocio del SPEC, en seis módulos:
  - *Strategy*: el contrato que observa la porción de mercado visible hasta
    el instante actual y emite intenciones de operar.
  - *Broker*: agrupa las intenciones de un mismo ciclo, las valida contra la
    capacidad financiera disponible y las registra con un orden causal
    estricto.
  - *Matching*: dado un conjunto de órdenes pendientes y la vela siguiente,
    resuelve qué se ejecuta y a qué precio, para una única trayectoria de
    mercado.
  - *Portfolio*: mantiene la posición, el capital, el colateral retenido y
    la contabilidad resultante de cada ejecución.
  - *VelaResolution*: coordina Matching y Portfolio sobre dos trayectorias
    de mercado alternativas para la misma vela, sin que una contamine a la
    otra, y selecciona cuál de las dos se considera oficial.
  - *Shared*: los tipos base compartidos del dominio.
- **Application** orquesta el ciclo temporal completo de un experimento —
  avanza el reloj, invoca Strategy, Broker y VelaResolution en secuencia —
  sin definir ninguna regla de negocio propia.
- **Infrastructure** implementa los adaptadores concretos: cómo se lee el
  dataset de entrada y cómo se serializa el resultado final, sin introducir
  lógica de dominio.
- **Presentation** (opcional, no forma parte del motor) expone el resultado
  hacia afuera sin introducir reglas propias:
  - *Contracts*: DTOs de salida, sin referencia a Domain ni Application.
  - *Api*: Minimal API que ejecuta el motor y traduce `ResultadoBacktest` a
    `ResultDto` vía un mapper — conversión de tipos y agregados de reporte
    simples, nunca recálculo financiero.
  - *wwwroot*: dashboard estático (HTML/JS/CSS sin frameworks) que consume
    la API.

## Visualizador local (opcional)

Capa de demostración sobre el motor batch — no forma parte del dominio
cuantitativo ni de sus garantías (RN/CU/RNF del SPEC no la mencionan).

### Qué representa "Demo"

`DatasetDemo` y `EstrategiaDemo` (`src/Presentation/TD_Project.Api/Demo/`)
son una configuración de experimento y una estrategia **fijas**, escritas a
mano únicamente para tener algo real que ejecutar y mostrar. No existe hoy
ingestión de datasets externos ni selección de estrategias — ver
`docs/PENDIENTES.md` para el alcance futuro explícito.

### Cómo ejecutar

```
dotnet run --project src/Presentation/TD_Project.Api
```

Abrir la URL que imprime la consola (por ejemplo `http://localhost:5299/`)
en un navegador y presionar **Ejecutar**. Cada clic dispara `POST
/api/backtest/run`, que corre el motor completo desde cero (sin caché ni
historial) y renderiza el resultado: resumen, curva de Equity, Trades, y la
resolución de trayectorias A/B (RN-11).
