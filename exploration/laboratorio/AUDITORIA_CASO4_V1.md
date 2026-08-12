# Auditoría de Cierre — Caso 4 Completo: Evolución Financiera

Estado: **documento de cierre de fase — Caso 4 completo (4.1 a 4.4)**. Consolida el ciclo
especificación → decisión → implementación → pruebas → auditoría para D-084, D-085, D-091 a D-098.
Mismo patrón que `AUDITORIA_CASO3A_V1.md` y `AUDITORIA_CASO4_3_UNIDADES_EXPOSICION_V1.md`, que este
documento reemplaza como cierre de nivel superior (ambos permanecen como evidencia de sub-fase, no
se eliminan).

---

## 1. Problema inicial

Caso 4 se abrió para resolver dos piezas de deuda técnica heredadas de Caso 2
(`DECISIONES_MODELO_ECONOMICO_V1.md`), ambas registradas pero explícitamente fuera de alcance en
su momento:

- **D-084 — `GestorCapital` no distingue apertura/cierre**: `GestorCapital.Ajustar` recalculaba
  `Cantidad` en toda `OrderRequest` sin excepción, incluyendo órdenes que debían cerrar o reducir
  una posición existente — produciendo lotes residuales o posiciones cruzadas espurias bajo sizing
  activo.
- **D-085 — descalce dimensional `Cantidad`/`CapitalInicial`**: las estrategias fijan `Cantidad`
  sin relación dimensional explícita con el capital, produciendo `Margin` desproporcionado
  (`≈9,000` frente a `CapitalInicial=1000` en el baseline de Caso 2).

Ambas piezas resultaron estar acopladas en el mismo componente (`GestorCapital.cs`) y en el mismo
punto del ciclo del motor (`BacktestRunner.cs`, antes de `ValidadorCapacidad`) — verificado en
`PROPUESTA_CASO4_V1.md` §2 antes de abrir ninguna sub-fase.

---

## 2. Correcciones — resumen por sub-fase

### 4.1 — Clasificación de intención de orden (D-091, D-092)

**D-091** resolvió que la corrección vive en `src/` (el defecto ya estaba ahí, no en una capa de
laboratorio) pero con activación exclusivamente vía `Sizing != null` explícito — comportamiento
histórico (`Sizing=null`) bit-a-bit idéntico, mismo patrón que D-061/D-069/D-079/D-082.

**D-092** resolvió introducir `ClasificadorIntencionOrden` como componente separado, previo a
`GestorCapital`, derivando la clasificación exclusivamente de `PortfolioState`/`LotesVivos` — nunca
de nombre de estrategia, tipo de orden, ni metadata manual. Reubica una regla ya existente en
`AplicadorFill.Aplicar`, no crea semántica nueva.

Implementado: `src/Domain/Portfolio/ClasificadorIntencionOrden.cs` — enum `IntencionOrden`
(`Apertura`/`Aumento`/`ReduccionParcial`/`CierreTotal`/`CrossZero`) + `Clasificar(PortfolioState,
OrderRequest)`.

### 4.2 — Integración en `GestorCapital` (D-084 resuelta)

`GestorCapital.Ajustar` clasifica secuencialmente cada `OrderRequest` de una bolsa contra una
posición **proyectada** local (nunca contra `PortfolioState` real, que sigue mutando
exclusivamente vía `AplicadorFill`, D-071 vigente) — solo `Apertura`/`Aumento` reciben sizing;
`ReduccionParcial`/`CierreTotal`/`CrossZero` conservan la cantidad necesaria para la posición real.

**D-084 resuelta**: verificado con la corrida exacta que originó el hallazgo original
(`EstrategiaTresMosqueteros`, `PorcentajeRiesgo=0.000002m` de D-083) — de colgarse 25+ minutos sin
terminar a `Success` en ~10 segundos.

### 4.3 — Unidades, exposición y normalización de cierres (D-085 resuelta)

**D-093** resolvió `PorcentajeRiesgo` como fracción del capital disponible comprometida como margen
objetivo (Opción A), despejando `CantidadActivo` de la misma ecuación que `CalculadoraLotes.
AbrirLote` ya usa — sin inventar concepto económico nuevo.

**D-094** resolvió `Close` de la vela siguiente como precio de referencia — misma fuente que
`ValidadorCapacidad`/`CalculadoraReservaPreventiva` ya usan para el mismo propósito conceptual.

**D-095** (hallazgo no anticipado, descubierto en verificación, no en diseño): la fórmula corregida
seguía produciendo `CashFinal` desproporcionado — diagnóstico aisló que una cantidad nominal
histórica de cierre (ej. `Sell 1m`) casi nunca coincide con la posición real que sizing dejó
abierta (ej. `Long 0.011111`), generando Cross-Zero espurio. Resuelto extendiendo
`ClasificadorIntencionOrden.Clasificar` para retornar `CantidadEfectiva` normalizada contra la
posición real; `GestorCapital` reinterpreta `CrossZero` como `CierreTotal` normalizado únicamente
bajo sizing activo — el clasificador en sí permanece una consulta pura, sin conocer configuración
de sizing.

**D-085 resuelta**: causa raíz corregida de punta a punta (unidad de sizing dimensional + cierre
bajo sizing) — límite declarado explícitamente: es corrección dimensional, no calibración de
valores "razonables" de estrategia.

### 4.4 — Observabilidad de incapacidades (D-096, D-097, D-098)

Hallazgo previo: `ResultadoBacktest.Incapacidades` existe desde Caso 2 (D-059/D-060) pero nunca fue
consumido por ningún componente de `exploration/` — dato calculado, huérfano.

**D-096** resolvió exponer el dato como observación/reporte (Opción A) — sin bloqueo ni modo
estricto, deferred explícitamente.

**D-097** resolvió la semántica: incapacidad = restricción económica observable, no error de orden.
Distingue `ValidadorBolsaRequests` (¿orden bien formada?) de `ValidadorCapacidad` (¿capital soporta
la orden?) — ambos pueden divergir sin contradicción. Mandató lenguaje neutral en el reporte, nunca
"falló"/"inválido"/"debe descartarse" como afirmación.

**D-098** resolvió el aislamiento estructural: módulo satélite `caso4/Caso4.csproj`, mismo patrón
que `caso3/Caso3.csproj` — evita mezclar evidencia de Caso 2 (`ModeloFinanciero.csproj`, que además
excluye `ReporteFinancieroGenerador.cs` por una colisión histórica) con evidencia nueva de Caso 4.

Implementado: `ResultadoCorridaTimeframe.Incapacidades` (campo opcional trailing, mismo patrón
D-072), nueva sección 4 en `ReporteFinancieroGenerador.cs` con lenguaje neutral D-097, agrupación
por `Side`. `ValidadorCapacidad.cs`/`RegistroIncapacidad.cs`/`BacktestRunner.cs`/
`ResultadoBacktest.cs` sin ninguna modificación.

---

## 3. Evidencia de pruebas — consolidado

- **126/126 tests de producción** (`dotnet test -c Release`): progresión 118 (tras 4.1) → 122 (tras
  4.2, +4) → 124 (tras D-093/D-094) → 126 (tras D-095, +2). 4.4 no toca `src/`/`tests/`, permanece
  en 126.
- **4/4 tests de Caso 4.4** (`caso4/TestsReporteIncapacidades.cs`, módulo satélite fuera de
  `dotnet test`): P1 (sin incapacidades), P2 (con incapacidades, flujo end-to-end
  `ResultadoBacktest → EjecutorProtocolo → ResultadoCorridaTimeframe → ReporteFinancieroGenerador`,
  incluyendo verificación textual del lenguaje neutral D-097), P3 (determinismo), P4 (regresión de
  secciones/renumeración del reporte).
- **3 criterios de aceptación de D-095 verificados con evidencia directa** (más allá de pruebas
  unitarias, contra escenarios exactos planteados por el auditor):
  1. `Long 0.011111` + `Sell 1 BTC` → cierre total exacto, sin short residual.
  2. `Long 10` + `Sell 15` bajo `Sizing=null` → Cross-Zero genuino preservado sin normalizar.
  3. `Sizing=null` → comportamiento intacto, clasificador no se invoca.
- **Pipeline Caso 1** (`protocolo/Program.cs`): 7/7 tests, incluyendo verificación de hash
  reproducible — confirma que el campo trailing opcional nuevo (`Incapacidades`) no afecta
  identidad experimental.

---

## 4. Confirmación de no regresión

- **3 baselines congelados** (Caso 1 `baseline_final/`, Caso 2 `baseline_financiero_final/`, Caso
  3A `caso3a-v1-experimental`): `git status --porcelain` vacío sobre las 3 rutas durante todo Caso
  4 — ninguno regenerado ni alterado.
- **`IStrategy` y las 5 estrategias existentes**: sin ningún cambio de código, en ninguna sub-fase.
- **`AplicadorFill.cs`, `ConsumidorFifo.cs`, `OrderRequest.cs`, `ValidadorCapacidad.cs`,
  `RegistroIncapacidad.cs`, `BacktestRunner.cs` (excepto el call site de `GestorCapital.Ajustar`),
  `ResultadoBacktest.cs`, `Instrumento.cs`**: sin modificación.
  `ResolutorCrossZero.cs` permaneció sin cambio — su lógica de cálculo de cantidad
  (`CantidadPosicionNueva`) fue el precedente citado para D-095, no una ruta modificada.
- **D-059/D-060**: no reabiertas, permanecen como fueron cerradas en Caso 2.
- **`VERSION_EXPERIMENTAL_CASO2_V1.md`**: no modificado. La referencia obsoleta a "D-085, no
  resuelta en Caso 2 V1" dentro de `ReporteFinancieroGenerador.cs` §6 quedó deliberadamente sin
  corregir por instrucción explícita del auditor — registrada como deuda documental histórica, no
  como pendiente técnico (ver §6).

---

## 5. Límites de Caso 4

**Explícitamente fuera de alcance, no tratado como pendiente**:

- **Calibración de valores de estrategia**: D-085 corrige la relación dimensional
  (`Cantidad = f(Capital, PorcentajeRiesgo, Precio, TasaMargen)`), no ajusta qué `PorcentajeRiesgo`
  o `CapitalInicial` son "razonables" para ninguna estrategia — eso no es un defecto, es un
  parámetro experimental fijo (mismo criterio que la sección de Límites del reporte financiero ya
  declara, D-076).
  - `ponytail: si en el futuro se necesita calibración real, ese es un caso nuevo — no una extensión de Caso 4.`
- **Modo estricto de `ValidadorCapacidad`** (Opción B de D-096, bloqueo/rechazo de órdenes por
  incapacidad): deferred explícitamente en D-096, nunca evaluado en 4.4.
- **`ValidadorBolsaRequests`**: no modificado ni evaluado — D-097 lo distingue de
  `ValidadorCapacidad` pero Caso 4 no tocó su lógica.
- **Corrección de la referencia D-085 obsoleta en `ReporteFinancieroGenerador.cs` §6**: pendiente
  de un mecanismo futuro de errata/índice de evolución documental, no de una reapertura de Caso 2.
- **Caso 3B**: explícitamente diferido desde la apertura de Caso 4 (`PROPUESTA_CASO4_V1.md`),
  ninguna decisión de Caso 4 lo activa.

---

## 6. Estado final — Decisiones de Caso 4

| Decisión | Resolución | Estado |
|---|---|---|
| D-084 | `GestorCapital` distingue apertura/cierre vía clasificación previa | ✅ Resuelta (4.2) |
| D-085 | Sizing dimensional corregido + normalización de cierres | ✅ Resuelta (4.3) |
| D-091 | Corrección en `src/`, activación experimental explícita (Opción C) | ✅ Resuelta |
| D-092 | Componente clasificador separado, fuente = `PortfolioState`/`LotesVivos` (Opción 2) | ✅ Resuelta |
| D-093 | `PorcentajeRiesgo` = fracción sobre margen requerido (Opción A) | ✅ Resuelta |
| D-094 | Precio de referencia = `Close` de vela siguiente | ✅ Resuelta |
| D-095 | Normalización de cierres contra posición real, previa a Cross-Zero | ✅ Resuelta |
| D-096 | Exposición de incapacidades como observación/reporte (Opción A) | ✅ Resuelta |
| D-097 | Incapacidad = restricción económica observable, no error | ✅ Resuelta |
| D-098 | Módulo satélite `caso4/Caso4.csproj` | ✅ Resuelta |

**Ninguna deuda técnica bloqueante queda abierta dentro del alcance definido por
`PROPUESTA_CASO4_V1.md` §6.**

---

## Criterio de cierre de Caso 4

- ✓ D-084 y D-085 resueltas con evidencia directa, no con parche — causa raíz corregida en `src/`.
- ✓ 8 decisiones de diseño/implementación (D-091 a D-098) resueltas, cada una con opciones
  explícitas evaluadas y rechazos justificados.
- ✓ 126/126 tests de producción + 4/4 tests de Caso 4.4 + 3 criterios de aceptación de D-095 con
  evidencia directa + pipeline Caso 1 reproducible.
- ✓ 3 baselines congelados intactos, `IStrategy` y estrategias existentes sin modificación,
  D-059/D-060 sin reabrir.
- ✓ Límites declarados explícitamente: sin calibración, sin modo estricto, sin tocar
  `ValidadorBolsaRequests`.
- ⏳ Pendiente de tu decisión: congelar Caso 4 como versión experimental (tag, ej.
  `caso4-v1-experimental`, mismo patrón que `caso3a-v1-experimental`) o abrir una nueva fase.
