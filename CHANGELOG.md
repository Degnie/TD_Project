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

### Cambiado

### Rechazado / Descartado

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
