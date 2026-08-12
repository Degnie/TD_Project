# Especificación del Modelo Económico — V1

Estado: **documento de diseño — Caso 2.0/2.1, previo a implementación**. No modifica ningún módulo
de `src/`. Objetivo: separar lo ya congelado del motor económico (no se redefine) de lo pendiente
(sí se decide), conforme a la auditoría de transición aceptada.

---

## 1. Inventario del modelo económico existente

Verificado por lectura directa de `src/` (producción), no por descripción de intención.

### A. Componentes ya existentes y no abiertos

**A.1 Estado económico — ✅ Existente, congelado**

`Cash + Margin + UnrealizedPnL = Equity` (RN-08, `src/Domain/VelaResolution/ResolutorVela.cs:114-119`).
`UnrealizedPnL = Σ Cantidad_lote * (Close_actual - PrecioEntrada_lote)`, mark-to-market al Close de
cada vela. Un `EquityPoint` por vela procesada (`src/Application/EquityPoint.cs`). No se rediseña.

**A.2 Posiciones — ✅ Existente, congelado**

FIFO, long/short, apertura/reducción/inversión (Cross-Zero), `RealizedPnL` (RN-09/RN-10):
`src/Domain/Portfolio/AplicadorFill.cs`, `CalculadoraLotes.cs`, `ConsumidorFifo.cs`,
`ResolutorCrossZero.cs`, `CalculadoraRealizedPnL.cs`. No se redefine.

**A.3 Margen — 🟡 Parcialmente definido**

`Margin = |Cantidad| * PrecioFill * TasaMargen` (`CalculadoraLotes.cs`). El cálculo está congelado;
lo que no está resuelto es el origen de `TasaMargen`: default parameter `0.1m` en
`AplicadorFill.Aplicar` (`src/Domain/Portfolio/AplicadorFill.cs:13`), no expuesto en
`ConfiguracionExperimento`, sin documentar a qué dominio pertenece. Ver D-057.

### B. Componentes pendientes reales

**B.1 Unidad monetaria — 🟡 Abierto (reformulado)**

No es "crear dinero" — `decimal` ya circula como Cash/Margin/Equity/PnL. Es definir qué representa
semánticamente ese `decimal`. Ver D-058.

**B.2 Costes — 🟡 Abierto, confirmado**

`Fill.CostoFriccionReal` (`src/Domain/Shared/Fill.cs:9`) existe como campo del contrato y se
transporta end-to-end (`MatchingEngine.cs` → `ResultDtoMapper.cs` → `FillLogEntryDto`), pero ambos
call sites de construcción de `Fill` (`MatchingEngine.cs:30` y `:51`) lo fijan literal en `0m`. Sin
ningún parámetro de comisión/spread/slippage/funding en toda la cadena. Documentado como gap
conocido en `docs/PENDIENTES.md:520`.

**B.3 Gestión de riesgo — 🟡 Abierto, fuera de esta especificación**

Sizing, porcentaje de capital, Masaniello. No existe en `src/` en absoluto (cero menciones de
Masaniello/martingala-como-modelo/apalancamiento). Se resuelve en Caso 2.2, después del modelo
económico base — no se mezcla aquí (misma separación que D-054 aplicó entre estrategia y pipeline).

**B.4 Capacidad de capital — 🟡 Abierto, confirmado**

`ValidadorCapacidad`/`CalculadoraReservaPreventiva` (`src/Domain/Broker/`) implementan RN-12 Fase 1
completo: `CashDisponiblePrevio = Cash - compromisosVigentes`, aprobado si
`CashDisponiblePrevio >= reserva`. Verificado por grep: **cero call sites** desde
`BacktestRunner.cs` ni desde ningún otro punto de producción — código implementado pero no
integrado. El motor actual permite que `Cash` se vuelva negativo sin bloquear la orden. Ver D-059.

---

## 2. Decisiones a resolver

### D-057 — Naturaleza de `TasaMargen`

La pregunta no es si existe margen (ya existe), es a qué dominio pertenece el valor `0.1m`.

- **A — Instrumento**: BTCUSDT define su propia regla de margen.
- **B — Broker / mercado simulado**: el entorno de ejecución define la condición, independiente del instrumento.
- **C — Experimento**: el investigador la varía como parámetro de `ConfiguracionExperimento`.
- **D — Mantener default temporal**, pero documentado explícitamente como placeholder (no como decisión).

### D-058 — Unidad económica

- **A — `decimal` = USDT**: intuitivo, pero asume instrumento/mercado real.
- **B — `decimal` = unidad monetaria abstracta**: neutral, menos comprensible directamente.

### D-059 — Aplicación de restricciones de capacidad

`ValidadorCapacidad` ya existe pero no está conectado.

- **A — Permitir estados imposibles y reportarlos**: el backtest corre sin bloquear, y se marca/reporta cuando `Cash` cruzó a negativo (visibilidad sin restricción).
- **B — Bloquear operaciones sin capacidad**: integrar `ValidadorCapacidad` al flujo de `BacktestRunner`, rechazando la orden si no hay `CashDisponiblePrevio` suficiente.

---

## 3. Principios de Caso 2

Reglas de trabajo heredadas de la disciplina del Caso 1 — no son decisiones nuevas, previenen que
Caso 2 contamine o reabra lo que Caso 1 ya congeló.

- **P-001 — No alterar motor económico probado**: cualquier cambio en posiciones, FIFO, Equity o
  Margin requiere una nueva versión del modelo económico, nunca una edición in-place del cálculo
  ya congelado (mismo principio que D-017/D-046 aplicaron a artefactos de Caso 1).
- **P-002 — Separar contabilidad de estrategia**: la estrategia produce `OrderRequest`; el modelo
  económico interpreta ejecución + capital + resultado. Ninguna estrategia debe conocer ni decidir
  sobre Cash/Margin/Equity directamente.
- **P-003 — No usar resultados financieros para validar estrategia**: la validación primaria de una
  estrategia sigue siendo operacional (Caso 1 — ¿el motor reproduce su comportamiento?). El dinero
  es una capa de interpretación posterior, no una fuente de verdad sobre si la estrategia es
  correcta.

---

## 4. Fuera de alcance de esta especificación

- Modelo de sizing/gestión de riesgo (Masaniello incluido) — Caso 2.2, posterior y separado.
- Métricas financieras oficiales (PnL%, drawdown, Sharpe) — Caso 2.3, posterior a que D-057/D-058/D-059 estén resueltas.
- Datasets de validación económica sintética — Caso 2.4.
- Ninguna modificación de código en este documento.

---

## Próximo paso

Presentar D-057, D-058 y D-059 para decisión — cada una requiere una elección explícita antes de
implementar cualquier cambio en `src/` (misma disciplina de Caso 1: decisión numerada → aprobación
→ implementación → prueba).
