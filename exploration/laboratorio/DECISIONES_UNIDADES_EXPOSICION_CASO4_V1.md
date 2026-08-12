# Decisiones — Unidades y Exposición (Caso 4.3, D-093 a D-095)

Estado: **D-093, D-094 y D-095 resueltas e implementadas — Caso 4.3 cerrado. D-085 resuelta**
(causa raíz corregida por la corrección dimensional completa D-093/D-094/D-095). Misma estructura
usada en D-001 a D-092 (decisión, opciones, criterio, evidencia). Ningún código se modifica en
este documento — resuelve la causa raíz identificada en `ESPECIFICACION_UNIDADES_EXPOSICION_V1.md`:
`GestorCapital` mezclaba unidad monetaria (`Cash − Margin`) con unidad de activo
(`OrderRequest.Cantidad`) sin conversión, y las órdenes de cierre de estrategias históricas no
reconciliaban su cantidad nominal contra la posición real bajo sizing activo.

Orden de resolución: D-093 primero (qué significa `PorcentajeRiesgo`), D-094 después (de dónde
sale el precio que la fórmula de D-093 necesita) — D-094 depende de la fórmula que D-093 fije, no
al revés. D-095 surgió durante la verificación P8 de la implementación de D-093/D-094 (hallazgo
en código, no anticipado en la especificación original), y fue resuelta y verificada en un ciclo
posterior.

**D-093, D-094 y D-095 resueltas por auditoría e implementadas** — ver resolución al final de cada
sección. Auditoría de cierre consolidada en `AUDITORIA_CASO4_3_UNIDADES_EXPOSICION_V1.md`.

---

## D-093 — Significado de `PorcentajeRiesgo`

**Estado**: 🟢 Aprobada. **Selección: A — porcentaje sobre margen requerido.**

**Decisión**: ¿`PorcentajeRiesgo` (`ConfiguracionSizing.PorcentajeRiesgo`) representa una fracción
del capital disponible que se convierte en **margen objetivo**, en **exposición nominal**, o
directamente en **cantidad de activo** (interpretación actual, ya identificada como
dimensionalmente inconsistente)?

**Evidencia** (`ESPECIFICACION_UNIDADES_EXPOSICION_V1.md` §1-2): `CalculadoraLotes.AbrirLote`
(`src/Domain/Portfolio/CalculadoraLotes.cs:13`) ya fija la relación entre las 3 magnitudes:
`Margin = |Cantidad| × PrecioFill × TasaMargen`. Cualquier interpretación de `PorcentajeRiesgo`
debe despejarse de esta misma ecuación — no se introduce una fórmula nueva, se elige qué variable
de la ecuación existente recibe el resultado de `CapitalDisponible × PorcentajeRiesgo`.

### Opciones

- **A — Porcentaje sobre margen requerido**: `MargenObjetivo = CapitalDisponible ×
  PorcentajeRiesgo`; despejando de la ecuación de `CalculadoraLotes`: `CantidadActivo =
  MargenObjetivo / (PrecioReferencia × TasaMargen)`.
  - Ventaja: coincide exactamente con la ecuación que el motor ya usa para calcular `Margin` — no
    introduce ningún concepto nuevo, solo invierte la ecuación existente. `PorcentajeRiesgo`
    significa literalmente "qué fracción del capital arriesgo como colateral", coherente con su
    nombre.
  - Riesgo: la `Cantidad` resultante depende de `TasaMargen` además de `PrecioReferencia` — dos
    estrategias con el mismo `PorcentajeRiesgo` pero distinto `Instrumento.TasaMargen` producirían
    exposiciones nominales distintas para el mismo riesgo de margen (comportamiento esperado del
    modelo de margen, no un defecto).
- **B — Porcentaje sobre valor nominal (exposición)**: `Exposicion = CapitalDisponible ×
  PorcentajeRiesgo`; `CantidadActivo = Exposicion / PrecioReferencia` (sin involucrar
  `TasaMargen` en el cálculo de cantidad).
  - Ventaja: más intuitivo financieramente — "qué fracción de mi capital quiero tener expuesta al
    mercado", independiente del apalancamiento del instrumento. Fórmula más simple (una división
    menos).
  - Riesgo: desacopla `PorcentajeRiesgo` de `Margin` — dos instrumentos con `TasaMargen` distinta
    producirían la misma exposición nominal pero distinto consumo real de `Cash` vía `Margin`,
    pudiendo sorprender si el nombre "riesgo" se interpreta como "riesgo de capital consumido" en
    vez de "riesgo de exposición de mercado".
- **C — Porcentaje sobre cantidad de activo (interpretación actual)**: descartada salvo
  justificación explícita del auditor — es la interpretación ya vigente en el código
  (`GestorCapital.cs:24`, sin cambios de 4.1/4.2), y es exactamente la que produce la mezcla de
  unidades documentada en la especificación. Mantenerla sin corrección reproduce el defecto
  original de D-085 en cualquier corrida futura con sizing activo.

**Criterio a aplicar**: la opción elegida fija qué variables entran en la fórmula de D-094 (A y B
difieren en si `TasaMargen` participa del cálculo de `Cantidad` o solo del cálculo de `Margin`
posterior) — debe resolverse antes de diseñar D-094.

### Resolución adoptada

**Selección: A.** `PorcentajeRiesgo` representa la fracción del capital disponible que el
experimento está dispuesto a comprometer como **margen objetivo**, no como cantidad de activo ni
como exposición nominal. Cadena: `CapitalDisponible → × PorcentajeRiesgo → MargenObjetivo → /
(PrecioReferencia × TasaMargen) → CantidadActivo`.

**Motivo**: coincide exactamente con la ecuación que `CalculadoraLotes.AbrirLote` ya usa (`Margin
= Cantidad × PrecioFill × TasaMargen`) — el sizing produce una variable directamente compatible
con esa ecuación existente, sin introducir una capa conceptual intermedia. Semánticamente: "cuánto
margen estoy dispuesto a comprometer", no "cuántos BTC comprar".

**Opción B rechazada**: introduce una capa adicional (`Capital → exposición nominal → margen`)
mientras el motor ya protege capacidad exclusivamente vía margen (`ValidadorCapacidad`,
`CalculadoraReservaPreventiva`) — desalinearía sizing del mecanismo de protección de capacidad ya
vigente.

**Opción C descartada definitivamente**: es el origen documentado de D-085 (dinero asignado
directamente como BTC sin conversión).

---

## D-094 — Fuente de precio para sizing

**Estado**: 🟢 Aprobada. **Selección: `Close` de la vela siguiente (`closeSiguiente`).**

**Decisión**: la fórmula de D-093 (opción A o B) requiere `PrecioReferencia`. `GestorCapital.
Ajustar` no recibe ningún precio hoy (`src/Domain/Portfolio/GestorCapital.cs:16`, firma actual:
`Ajustar(IReadOnlyList<OrderRequest> requests, PortfolioState portfolio, ConfiguracionSizing?
sizing)`) — ¿de dónde debe obtenerlo?

**Evidencia verificada en `BacktestRunner.cs:44-79`** (el único call site de `GestorCapital.
Ajustar`, línea 52):

| Candidato | Disponibilidad real en el ciclo | Evidencia |
|---|---|---|
| Precio de apertura de la vela siguiente (`Open`) | Ya accesible en el scope de la línea 52 — `config.Velas[n + 1]` es indexable ahí, aunque no se lee hasta la línea 60 | Ningún dato nuevo requerido, solo adelantar una lectura ya usada más abajo (`config.Velas[n + 1].Close`, línea 60) |
| Precio de cierre de la vela siguiente (`Close`) | Igual que arriba — es literalmente `closeSiguiente`, ya usado por `ValidadorCapacidad`/`CalculadoraReservaPreventiva` (líneas 65, 67) para el mismo propósito (estimar exposición antes del Fill real) | Mismo precio que el motor ya usa como "precio de referencia previo al Fill" en el resto del ciclo — máxima consistencia con el código existente |
| Precio estimado de fill (`PrecioFill` proyectado) | **No disponible en este punto** — el `Fill` real se produce en `ResolutorVela.Resolver` (línea 79), **después** de `GestorCapital.Ajustar` (línea 52). Requeriría invertir el orden causal del ciclo (calcular el fill antes del sizing que lo determina) | Estructuralmente contradictorio: el sizing determina `Cantidad`, que es un insumo del `Fill`, no al revés |
| Precio del `Instrumento` (precio de referencia estático) | `Instrumento` (`src/Domain/Shared/Instrumento.cs`) no tiene campo de precio — solo `Simbolo`/`TasaMargen`. Añadirlo sería un precio desactualizado por diseño (el mercado se mueve vela a vela), no un precio de referencia real | Requiere campo nuevo en un tipo ya congelado (D-057), sin justificación clara sobre por qué sería más correcto que el precio de la vela actual |

**Candidato con evidencia más fuerte**: `Close` de la vela siguiente — es exactamente el mismo
precio que `ValidadorCapacidad.Validar`/`CalculadoraReservaPreventiva.Calcular` ya usan
(`closeSiguiente`, línea 65/67) para el mismo propósito conceptual (estimar magnitud económica de
una orden antes de que el `Fill` real ocurra). Usar el mismo precio para sizing que para
validación de capacidad evita introducir una segunda noción de "precio de referencia" en el mismo
ciclo.

**Consecuencia de contrato**: bajo cualquier opción salvo "precio estimado de fill" (descartada),
`GestorCapital.Ajustar` necesita recibir un precio nuevo como parámetro — cambio de firma pública,
con un único call site a actualizar (`BacktestRunner.cs:52`) según la evidencia ya verificada.

### Resolución adoptada

**Selección: `Close` de la vela siguiente.** No por ser "el precio perfecto", sino porque
`ValidadorCapacidad`/`CalculadoraReservaPreventiva` ya usan exactamente ese mismo precio
(`closeSiguiente`, `BacktestRunner.cs:60,65,67`) para responder una pregunta conceptualmente
equivalente ("¿la operación cabe económicamente antes de ejecutarse?"). Que `GestorCapital` y
`ValidadorCapacidad` compartan la misma referencia de precio evita que ambos respondan preguntas
de capacidad con datos distintos — mismo tipo de divergencia que D-062 corrigió entre `EquityCurve`
y `Cash`/`Trades` (una sola fuente de verdad para un mismo concepto económico dentro del ciclo).

**Precio estimado de fill descartado**: la causalidad del ciclo es `Cantidad → Fill`, no `Fill →
Cantidad` (`ResolutorVela.Resolver` corre después de `GestorCapital.Ajustar` en
`BacktestRunner.cs`, línea 79 vs. 52) — no puede ser fuente de sizing sin invertir el orden causal
del pipeline.

---

## D-095 — Cierre de posiciones bajo sizing activo (cantidad nominal vs. cantidad real)

**Estado**: 🟢 Aprobada e implementada. **Selección: la intención de reducción/cierre prevalece
sobre la cantidad nominal de la estrategia — normalización previa a `GestorCapital`/
`AplicadorFill`.** Especificación: `ESPECIFICACION_NORMALIZACION_CIERRES_SIZING_V1.md`.

**Origen del hallazgo**: durante la verificación P8 de `ESPECIFICACION_IMPLEMENTACION_SIZING_
CORREGIDO_V1.md` (corrida larga con la fórmula D-093/D-094 ya implementada), `CashFinal` seguía
desproporcionado incluso en un dataset corto (1D, `CashFinal ≈ -40,000` con `CapitalInicial=1000`
en solo 15 operaciones) — no explicable por acumulación de volumen (la causa de D-084 original, ya
resuelta). Diagnóstico aislado con evidencia mínima (`GestorCapital.Ajustar`/
`ClasificadorIntencionOrden.Clasificar`/`AplicadorFill.Aplicar` invocados directamente, sin
`BacktestRunner`): una apertura `Buy 1m` con sizing activo ejecuta `Cantidad=0.011111` real (
correcto). `EstrategiaTresMosqueteros`, sin conocer ese valor (P-002, correcto que no lo conozca),
intenta cerrar con `Sell 1m` — su cantidad de diseño fija. El clasificador compara `|1m| >
|0.011111|` y correctamente identifica `CrossZero` (no `CierreTotal`) — por diseño ya aprobado en
4.2 (`ESPECIFICACION_INTEGRACION_GESTOR_CAPITAL_V1.md` §3/§6), Cross-Zero conserva la `Cantidad`
original sin aplicar sizing. Resultado: se ejecuta un Fill de `Cantidad=1` BTC real (exposición
completa) en vez de cerrar los `0.011111` realmente abiertos.

**Por qué no es un defecto de 4.1/4.2**: el clasificador interpreta correctamente la situación que
se le presenta (`posición + orden solicitada → CrossZero` es la clasificación correcta dado
`|1m| > |0.011111|`). El defecto está un nivel más arriba: bajo sizing activo, la cantidad nominal
que una estrategia histórica usa para expresar "quiero cerrar" (`Cantidad=1m`, fija, diseñada para
un mundo sin sizing) deja de coincidir con la posición real que sizing dejó abierta — dos
contratos incompatibles conviviendo sin reconciliación.

**Regla aprobada**: cuando una orden de una estrategia histórica representa una reducción de
posición, la cantidad efectiva de cierre debe derivarse de la posición viva (`PortfolioState`/
`LotesVivos`), no de la cantidad emitida originalmente por la estrategia. `Sell 1m` sobre una
posición `Long 0.011111` debe interpretarse como "cerrar 0.011111", no como "vender 1m" (lo cual
Cross-Zero interpretaría como cerrar todo lo existente y abrir 0.988889 en corto).

**Explícitamente rechazado**:
- Aplicar sizing directamente a Cross-Zero — ya rechazado en 4.2 por mezclar cierre + apertura en
  un solo cálculo (razón original sigue vigente: antes de aplicar sizing al evento completo, debe
  separarse el tramo de cierre del tramo de apertura).
- Modificar estrategias existentes para que conozcan la posición real — violaría P-002.
- Extender `IStrategy` con cantidad ejecutada/posición actual/capital — mismo principio que
  rechazó la Opción 1 de D-092 (metadata económica no pertenece a la interfaz de estrategia).

**Consecuencia arquitectónica**: se agrega una capa de **normalización de cantidad** entre
`ClasificadorIntencionOrden` (determina qué representa la orden) y `GestorCapital`/`AplicadorFill`
(ejecutan la consecuencia) — la estrategia sigue sin conocer sizing ni posición real; la
normalización ocurre exclusivamente dentro del motor, sobre datos que ya están disponibles
(`PortfolioState`).

**Alcance de Caso 4.3 actualizado**: dejó de ser únicamente "corregir la fórmula dimensional de
sizing" (D-093/D-094) y pasó a incluir "corregir la compatibilidad entre sizing proporcional y
estrategias con cantidades nominales históricas" (D-095).

### Implementación y verificación

`src/Domain/Portfolio/ClasificadorIntencionOrden.cs` — `Clasificar` retorna
`ResultadoClasificacion(IntencionOrden, decimal CantidadEfectiva)` en vez de solo el enum. La
clasificación en sí no cambió (`CrossZero` sigue siendo un resultado posible sin normalizar) — el
componente sigue siendo una consulta pura sobre `PortfolioState`/`OrderRequest`, sin conocer
configuración de sizing (P-002 extendido a este componente). `src/Domain/Portfolio/
GestorCapital.cs` — bajo sizing activo, si la clasificación da `CrossZero`, se reinterpreta como
`CierreTotal` normalizado usando `Math.Abs(posicionProyectada)` en vez de la cantidad nominal
solicitada; la proyección de posición avanza con la cantidad efectivamente ejecutada.

**3 criterios de aceptación verificados con evidencia directa** (no solo por las pruebas
unitarias):
1. `Long 0.011111` (posición real bajo sizing) + `Sell 1 BTC` (cantidad nominal histórica) →
   `CantidadEjecutada = 0.011111` exacto, posición final `= 0` — cierre total limpio, sin short
   residual.
2. `Long 10` + `Sell 15` bajo `Sizing=null` → conserva `Cantidad=15` nominal sin normalizar,
   produce Cross-Zero genuino: posición final `Short 5`, `TradeCerrado` no nulo — confirma que
   D-095 no eliminó Cross-Zero como mecanismo, solo los Cross-Zero artificiales provocados por
   sizing activo.
3. `Sizing=null` → `GestorCapital.Ajustar` retorna la orden intacta, sin invocar el clasificador.

**Pruebas**: 126/126 tests de producción (62 Domain.Tests incluyendo 11 de
`ClasificadorIntencionOrdenTests` actualizadas a la nueva firma; 4 Contracts; 2 Infrastructure; 18
Api; 40 Application incluyendo 2 pruebas nuevas de normalización). Corrida larga de verificación
(Tres Mosqueteros, dataset 1D+1m real, sizing activo): `CashFinal ≈ 577` (1D) y `≈ 0` (1m),
`ExposicionMaxima ≈ 100` — coherente con `MargenObjetivo=100` esperado, sin la desproporción de
millones del hallazgo original, sin colgarse.

**No modificado** (restricciones respetadas): `IStrategy`, las 5 estrategias existentes,
`AplicadorFill.cs`, `ResolutorCrossZero.cs`, `ConsumidorFifo.cs`, `OrderRequest.cs`,
`ValidadorCapacidad.cs`. `git status --porcelain` confirma únicamente
`ClasificadorIntencionOrden.cs`/su test (nuevos) y `GestorCapital.cs`/`BacktestRunner.cs`/
`GestorCapitalTests.cs` (modificados, ya reportados en D-093/D-094).

---

## D-085 — Escala económica histórica: resuelta

**Estado**: 🟢 Resuelta (cerrada por la corrección dimensional completa de D-093/D-094/D-095).
Anteriormente 🟡 deuda técnica registrada en Caso 2 (`DECISIONES_MODELO_ECONOMICO_V1.md`).

**Causa raíz original**: `Cantidad` de las estrategias (fija, ej. `1m`) sin relación dimensional
con `CapitalInicial` — `Margin ≈ Cantidad × PrecioFill × TasaMargen` producía valores
desproporcionados frente al capital (`Margin ≈ 9,000` vs. `CapitalInicial=1000`, evidenciado por
primera vez en el baseline financiero de Caso 2).

**Resolución**: la cadena completa queda definida y verificada — `CapitalDisponible →
× PorcentajeRiesgo → MargenObjetivo → / (PrecioReferencia × TasaMargen) → CantidadActivo` (D-093/
D-094), y la cantidad de salida de una orden de cierre ya no depende de la cantidad nominal
histórica de la estrategia sino de la posición real (D-095). Verificado con evidencia: una corrida
completa con sizing activo produce `CashFinal`/`Margin`/`ExposicionMaxima` en el orden de magnitud
del `CapitalInicial`, no en millones.

**Qué NO resuelve D-085** (límite declarado, no deuda bloqueante): la corrección es dimensional,
no de calibración — `PorcentajeRiesgo`/`CapitalInicial` siguen siendo input explícito del
experimento (D-030), ningún valor "razonable" específico está garantizado ni recomendado por el
motor. `CapitalInicial=1000` de Caso 1/Caso 2 no fue recalibrado (nunca se tocó, D-085 se resolvió
hacia adelante).

---

## Resumen de decisiones

| Decisión | Selección | Estado |
|---|---|---|
| D-093 | Porcentaje sobre margen requerido (Opción A) | 🟢 Aprobada e implementada |
| D-094 | `Close` de la vela siguiente, misma referencia que `ValidadorCapacidad` (Opción A) | 🟢 Aprobada e implementada |
| D-095 | Normalización de cantidad de cierre contra posición real, previa a `GestorCapital`/`AplicadorFill` | 🟢 Aprobada e implementada |
| D-085 | Corrección dimensional completa (D-093+D-094+D-095) | 🟢 Resuelta |

**Fórmula objetivo D-093/D-094** (implementada): `CantidadActivo = (CapitalDisponible ×
PorcentajeRiesgo) / (CloseReferencia × TasaMargen)`, con `CapitalDisponible = Cash − Margin` (sin
cambio respecto al modelo actual, D-067). **Normalización D-095** (implementada): cantidad de
cierre = magnitud de la posición real cuando la cantidad nominal la excede o iguala.

---

## Fuera de alcance de este documento

No se modifica código en este documento (D-093/D-094/D-095 ya implementadas). No se toca
`ValidadorCapacidad.cs`, `Instrumento.cs`, `OrderRequest.cs`, `AplicadorFill.cs`,
`ResolutorCrossZero.cs`. No se recalibra `CapitalInicial` ni ningún parámetro de estrategia
histórica.

---

## Próximo paso

Caso 4.3 cerrado. Auditoría consolidada: `AUDITORIA_CASO4_3_UNIDADES_EXPOSICION_V1.md`. Próxima
sub-fase candidata (no abierta todavía): revisión de `ValidadorCapacidad`
(observación vs. bloqueo económico) — último punto previsto en la propuesta inicial de Caso 4, sin
decisión de apertura tomada.
