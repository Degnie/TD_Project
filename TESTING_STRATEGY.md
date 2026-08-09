# Estrategia de testing

## 1. Cobertura exigida

Se distinguen dos tipos de requisito:

- **Requisitos verificables** (toda RN 01-14, CU 01-20, EC 01-04, y RNF
  verificable: RNF-05, RNF-06, RNF-08, RNF-09, RNF-10, RNF-13) — exigen al
  menos un test que los cite. Ninguno de estos puede quedar activo sin test.
- **Requisitos cuantitativos pendientes** (RNF-01, RNF-02, RNF-03, RNF-04) —
  no exigen test con cita mientras su umbral numérico no esté definido;
  exigen en cambio un benchmark pendiente, que se activa como comprobación
  formal en cuanto el umbral se decida.

Regla de trazabilidad: ningún test sin cita a un ID existente.

Umbral de mutación: 70% sobre los módulos de `src/Domain/**` modificados en
cada cambio. Sin respaldo en un ID del SPEC — decisión técnica propia, abierta
a ajuste.

## 2. Estrategias por capa

**Domain** (Strategy, Broker, Matching, Portfolio, VelaResolution):
aserciones sobre estado observable (Position, Cash, Margin, Equity, Fills
emitidos), nunca sobre mocks internos de dominio — la verificación de una
regla de negocio no se sustituye por un doble de prueba.
- Pruebas por propiedades: RN-04, RN-11, CU-05, RNF-06.
- Tipos que impiden estados inválidos: RN-01, RN-06.
- `System.Decimal` como requisito de precisión: RNF-05 (no es parte de la
  metodología de tipos, es la elección de tipo numérico).

**Application** (Backtest/Experiment): aserciones sobre la secuencia de
invocaciones a los contratos de Domain y sobre el resultado final agregado.
Los dobles de prueba no sustituyen la verificación de reglas de dominio, pero
sí pueden aislar una frontera de Domain cuando el objetivo del test es probar
exclusivamente la orquestación (por ejemplo, manejo de errores de
Infrastructure sin ejercitar Matching real).

**Infrastructure**: dobles de prueba en la frontera hacia el sistema externo
(archivo/formato); tests de round-trip para RNF-13.

**Golden master (regresión funcional):** congela la salida de un escenario
de referencia para detectar regresiones de comportamiento — asociado a
RNF-08 (trazabilidad y reconstrucción vía Fill Log + Estado Canónico Inicial).

**Benchmark de rendimiento (separado del golden master):** mide velas/
segundo, bytes/vela, bytes/orden concurrente, pico de memoria, tiempo total
— asociado a RNF-01/02/03. No compara contra una salida congelada; compara
contra umbrales que se definirán cuando RNF-01/02/03 dejen de estar
pendientes. RNF-04 (speedup paralelo) sigue el mismo tratamiento: benchmark
pendiente hasta que exista objetivo cuantitativo.

## 3. Inyección de fallos

Derivados de los EC del SPEC:

- **EC-01** (igualdad exacta en límite de precio): caso límite forzado en
  tests de Matching.
- **EC-02** (ejecución simultánea múltiple): forzado en tests de
  Broker/VelaResolution, verificando orden estricto por Secuencia Causal.
- **EC-03** (desincronización / violación de determinismo): mismo input
  ejecutado dos veces, comparación de ResultHash; fallo esperado si difieren.
- **EC-04** (falla sistémica / aborto no manejado): el test verifica el
  contrato observable — estado final `InternalCrash` y cero resultados
  financieros emitidos (RNF-10). El mecanismo concreto que provoca el fallo
  no forma parte del requisito verificado.

## 4. Decisiones históricas y deuda técnica

_(vacío — se completa en auditorías futuras)_
