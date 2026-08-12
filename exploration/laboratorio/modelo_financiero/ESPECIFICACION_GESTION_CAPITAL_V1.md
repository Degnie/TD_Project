# Especificación de Gestión de Capital y Sizing — V1

Estado: **documento histórico de apertura de decisiones — Caso 2.3, D-066/D-067/D-068 ya
resueltas**. Este documento presentó las preguntas originales; las decisiones se cerraron en
`EVALUACION_MODELOS_GESTION_RIESGO_V1.md` (comparación de candidatos, D-067),
`ESPECIFICACION_ARQUITECTURA_GESTOR_CAPITAL_V1.md` (D-066/D-068) y
`DECISIONES_MODELO_ECONOMICO_V1.md` (registro formal final de las 3 más D-069/D-070). Se conserva
sin reescribir como registro del razonamiento original — el contenido de las secciones 2-3 abajo
es **histórico**, no la definición vigente.

Fuera de alcance (explícito, sigue vigente): Masaniello, gestión de riesgo avanzada, métricas
financieras finales.

---

## 1. Inventario del estado actual de sizing/exposición

Verificado por lectura directa de `src/` y `exploration/` (producción y estrategias del
laboratorio):

**`OrderRequest.Cantidad`** (`src/Domain/Shared/OrderRequest.cs:7`) — campo `decimal` libre, sin
ninguna validación ni política de sizing en el motor. Quien decide el valor es exclusivamente la
`Strategy` que construye el `OrderRequest`.

**Las 3 estrategias existentes usan `Cantidad: 1m` fijo, hardcoded** — confirmado por lectura
directa:
- `exploration/EstrategiaTresMosqueteros.cs` — 4 call sites, todos `1m`.
- `exploration/EstrategiaMhiMayoria.cs` — 4 call sites, todos `1m`.
- `exploration/EstrategiaEmaCross.cs` — 4 call sites, todos `1m`.

Ninguna estrategia hoy calcula `Cantidad` en función de `Cash`/`Equity` disponible — el tamaño de
operación es una constante sin relación con el capital.

**`ValidadorCapacidad`/`CalculadoraReservaPreventiva`** (`src/Domain/Broker/`, integrados en Caso
2.1, D-059/D-060) — evalúan si `CashDisponiblePrevio >= reserva` para cada `OrderRequest`, pero
**solo observan y registran** (`RegistroIncapacidad`), nunca ajustan ni bloquean `Cantidad`. No
existe ningún mecanismo que traduzca "cuánto capital tengo" en "qué tamaño de orden debo pedir" —
esa traducción es exactamente lo que falta y lo que Caso 2.3 debe definir.

**Consecuencia para el diseño**: a diferencia de `TasaMargen` (D-057) o `CostoFriccionReal`
(D-063), que eran valores que existían pero no se usaban en la ruta correcta, **el sizing no
existe en absoluto** — no hay un valor mal conectado, hay un concepto ausente. El riesgo de D-062/
slippage (parámetro que no representa una diferencia real en el modelo) no aplica aquí de la misma
forma — aquí el riesgo es diseñar una fórmula de sizing que la `Strategy` nunca consulta, si la
integración no llega hasta el punto donde `OrderRequest.Cantidad` se construye.

---

## 2. Decisiones a resolver

### D-066 — Quién calcula el tamaño de operación

- **A — La `Strategy` sigue decidiendo `Cantidad` directamente**, pero recibe información de
  capital disponible (`Cash`/`Equity`) en el `DataSlice` o similar, para que pueda calcularlo ella
  misma si quiere. El motor no impone ninguna fórmula.
- **B — Un componente nuevo, externo a la `Strategy`, calcula `Cantidad`** a partir de una política
  configurable (capital fijo, porcentaje de capital), y la `Strategy` solo decide dirección
  (Buy/Sell) y tipo de orden — el motor reescribe/completa `Cantidad` antes de pasar la orden a
  `ValidadorCapacidad`/`MatchingEngine`.
- **C — No cambiar nada todavía**: mantener `Cantidad` fija por decisión de la `Strategy`, y
  limitar Caso 2.3 a solo *medir* (reportar) qué porcentaje de capital representa cada operación,
  sin cambiar cómo se decide `Cantidad`.

**Nota de alcance**: la Opción B es la única que reescribe el contrato de cómo una estrategia se
relaciona con el tamaño de sus operaciones — las 3 estrategias existentes (`Cantidad: 1m` fijo)
tendrían que decidir si delegan sizing al motor o lo siguen fijando ellas. Afecta directamente
`IStrategy`, que P-002 (`ESPECIFICACION_MODELO_ECONOMICO_V1.md` §3) protege como frontera —
separar contabilidad de estrategia.

### D-067 — Modelo de sizing en V1

Presentado como comparación, no como elección — mismo criterio que D-021 aplicó a clasificadores de
régimen en Caso 1 (comparar antes de elegir, no elegir directamente).

- **Capital fijo por operación**: `Cantidad` constante, igual a como funciona hoy. Simple, fácil de
  auditar, no adapta a cambios de capital.
- **Porcentaje de capital**: `Cantidad` proporcional a `Cash`/`Equity` disponible en el momento de
  la orden (ej. 1% del capital actual). Estándar financiero, pero cambia el comportamiento
  operacional de una estrategia ya validada en Caso 1 (Tres Mosqueteros/MHI Mayoría/EMA Cross
  fueron evaluadas con `Cantidad: 1m` fijo — un cambio de sizing altera resultados, no solo
  economía).
- **Masaniello**: modelo basado en número de operaciones, probabilidad, objetivo. Requiere
  supuestos fuertes (probabilidad de acierto estimada) y depende de estimar comportamiento futuro
  — fuera de alcance de V1 por decisión explícita de la auditoría (este documento no lo diseña).
- **No decidir todavía — crear `EVALUACION_MODELOS_GESTION_RIESGO_V1.md`**: comparar
  capital-fijo/porcentaje/Masaniello con criterios explícitos antes de elegir uno para V1, en vez
  de que esta especificación ya presuponga cuál es mejor.

### D-068 — Relación entre `ValidadorCapacidad` y el sizing

Con sizing real (si D-066/D-067 introducen una fórmula), `ValidadorCapacidad` deja de evaluar
"¿esta cantidad fija cabe en el capital?" y empieza a evaluar "¿la cantidad que el sizing calculó
efectivamente cabe?". Ambas preguntas usan el mismo validador (D-059/D-060 ya lo dejaron en modo
observación), pero la relación entre "sizing calcula la cantidad" y "capacidad valida la cantidad"
debe quedar explícita para no duplicar lógica.

- **A — Sizing y capacidad son independientes**: sizing calcula `Cantidad` sin consultar
  `ValidadorCapacidad`; si el resultado excede la capacidad, queda registrado como
  `RegistroIncapacidad` igual que hoy (D-059, sin bloquear).
- **B — Sizing consulta la capacidad disponible antes de calcular `Cantidad`**: evita proponer un
  tamaño que ya se sabe insuficiente, pero acopla el cálculo de sizing a `ValidadorCapacidad`.

---

## 3. Fuera de alcance de esta especificación

Masaniello (solo mencionado como candidato a evaluar en `EVALUACION_MODELOS_GESTION_RIESGO_V1.md`,
no diseñado aquí), gestión de riesgo avanzada, métricas financieras finales, optimización de
parámetros. Ningún cambio de código en este documento.

---

## Próximo paso

Presentar D-066/D-067/D-068 para decisión. Si D-067 se resuelve como "no decidir todavía", el
siguiente documento antes de cualquier implementación es `EVALUACION_MODELOS_GESTION_RIESGO_V1.md`
— mismo patrón que Caso 1 aplicó a la comparación de clasificadores de régimen antes de congelar
`ClasificadorRegimenV1`.
