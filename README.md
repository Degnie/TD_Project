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
Corre como proceso batch local, de un solo hilo, sin dependencias de red ni
estado compartido entre ejecuciones. No hay componente interactivo ni
servidor: se invoca, procesa el dataset completo, y termina.

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
