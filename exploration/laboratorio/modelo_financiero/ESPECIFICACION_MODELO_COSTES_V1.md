# Especificación del Modelo de Costes y Fricción — V1

Estado: **documento de diseño — Caso 2.2, previo a implementación**. Continúa el ciclo inventario →
decisiones → diseño → implementación → pruebas → auditoría aplicado en Caso 2.0/2.1. No modifica
código en este documento.

Fuera de alcance (explícito): sizing, Masaniello, riesgo, métricas financieras finales — Caso 2.3 y
posteriores, después de que el coste real de ejecución esté resuelto.

---

## 1. Inventario del estado actual de costes

Verificado por lectura directa de `src/` (producción):

**`Fill.CostoFriccionReal`** (`src/Domain/Shared/Fill.cs:9`) — campo del record, existe desde
antes de Caso 2. Los dos únicos call sites que construyen un `Fill`
(`src/Domain/Matching/MatchingEngine.cs:30` y `:51`, en `Resolver` y `ResolverStopLimit`) lo fijan
literal en `0m`.

**Transporte end-to-end confirmado**: `Fill.CostoFriccionReal` → `ResultDtoMapper.cs:51` →
`FillLogEntryDto.CostoFriccionReal` (`src/Presentation/TD_Project.Contracts/FillLogEntryDto.cs:8`)
— el campo llega íntegro hasta el contrato expuesto, sin transformación.

**Hallazgo verificado — no afecta ningún cálculo económico hoy**: `AplicadorFill.Aplicar`
(`src/Domain/Portfolio/AplicadorFill.cs`) nunca lee `fill.CostoFriccionReal` — confirmado por
búsqueda exhaustiva, cero referencias dentro del archivo. El campo es puramente informativo en el
Fill Log; `Cash`/`Margin`/`Equity`/`RealizedPnL` se calculan hoy como si el costo fuera siempre
cero, independientemente de lo que el campo declare.

**Consecuencia para el diseño**: introducir un costo real que efectivamente descuente capital
requiere dos cambios coordinados, no uno — (a) calcular el valor en `MatchingEngine` (reemplazando
el literal `0m`) y (b) hacer que `AplicadorFill` lo reste de `Cash` al aplicar el Fill. Omitir (b)
repetiría exactamente el error de D-062 (un valor que existe pero no se usa en la ruta que produce
la consecuencia económica).

---

## 2. Decisiones resueltas

Orden de resolución aplicado: **D-065 primero** (¿el sistema aplica costes o solo los registra? —
decisión principal, de la que depende todo lo demás), luego D-063 (qué componentes), luego D-064
(de dónde vienen los parámetros).

### D-065 — Aplicación del coste al Cash

**Estado**: 🟢 Aprobada. **Selección: A — el coste modifica Cash/Equity.**

`AplicadorFill.Aplicar` descuenta el coste de `Cash`, además del `Margin`/`RealizedPnL` que ya
calcula — mismo punto donde D-062 propagó `tasaMargen`. `CostoFriccionReal` deja de ser
información auxiliar y pasa a ser componente del estado económico. Modelo:
`Resultado bruto − Costos de ejecución = Resultado económico neto`.

**Contrato aprobado**: todo camino que modifique `Cash` debe respetar
`PnL bruto + Costo aplicado = Estado económico final` — para cada Fill:
`Precio de ejecución → Actualizar posición/PnL bruto → Aplicar coste asociado → Actualizar Cash → Actualizar Equity`.

**Regla para Cross-Zero**: el coste se aplica a cada tramo económico realmente ejecutado (cierre de
la posición vieja y apertura de la posición nueva por separado), no como un único coste artificial
sobre el evento completo — ver detalle verificado contra código en la Sección 2.1.

---

## 2.1 Orden exacto de aplicación (diseño aprobado, no implementado)

Base de la decisión D-065 (aprobada arriba): dónde exactamente se resta el coste dentro de
`AplicadorFill.Aplicar` (`src/Domain/Portfolio/AplicadorFill.cs`). Verificado contra el código
real — `Aplicar` tiene 3 rutas, cada una muta `Cash` en un punto distinto:

- **Abrir/aumentar posición** (mismo signo, líneas 19-25): `portfolio.Cash -= lote.Margin`. El
  coste de esta operación (comisión sobre el nocional del Fill) se restaría en el mismo punto,
  después de descontar `Margin`.
- **Reducir vía FIFO** (signo contrario, `|Fill| <= |Position|`, líneas 32-65):
  `portfolio.Cash += consumo.MarginLiberado + consumo.RealizedPnL`. El coste se restaría de este
  mismo `Cash +=`, después de sumar `RealizedPnL` — orden: `PnL bruto → Coste → Cash actualizado`
  (coincide con el modelo "PnL neto" que D-065 opción A propone).
- **Cross-Zero** (signo contrario, `|Fill| > |Position|`, líneas 67-86): dos mutaciones de `Cash`
  — una al liberar la posición vieja (línea 76, con `RealizedPnL`) y otra al abrir el lote nuevo
  (línea 81, con `Margin`). El coste de un Fill que cruza cero afecta ambos tramos: el "cierre" de
  la posición original consume `RealizedPnL` (coste aplica ahí, mismo criterio que FIFO), el
  "apertura" de la posición nueva consume `Margin` (coste aplica ahí, mismo criterio que
  abrir/aumentar).

**Consecuencia para la implementación futura (no ejecutada aquí)**: el coste no se resta en un
único punto de `Aplicar` — se resta en cada una de las 3 rutas, en el mismo punto donde esa ruta ya
muta `Cash`, siguiendo el orden `Fill → PnL/Margin de la ruta → Coste → Cash actualizado`. Aplicar
el coste en un lugar distinto (ej. antes de calcular `RealizedPnL`, o como un cuarto paso separado
fuera de las 3 ramas) requeriría más justificación de la que este documento aporta — el diseño más
simple es el que reutiliza el punto que cada rama ya tiene.

---

### D-063 — Qué componentes de coste incluye V1

**Estado**: 🟢 Aprobada. **Selección: B — Comisión + Slippage.**

**Incluido**:
- **Comisión**: coste explícito de ejecución, porcentaje fijo sobre el valor nocional de cada Fill
  (`Cantidad * PrecioFill * TasaComision`).
- **Slippage**: diferencia entre precio esperado y precio ejecutado.

**No incluido en V1**:
- **Spread explícito** — el motor no tiene modelo bid/ask; agregarlo requeriría rediseñar la
  ejecución (`MatchingEngine` opera sobre un único precio OHLC por vela, no sobre libro de
  órdenes).
- **Funding** — depende de mercado/tiempo mantenido/reglas externas, fuera de esta versión.

**Definición matemática**:

```
CostoTotal = Comision + Slippage
Comision   = Cantidad * PrecioFill * TasaComision
```

**Slippage — verificado contra `MatchingEngine.cs` antes de definir la fórmula, y corregido durante
implementación**: el "precio esperado" solo tiene sentido para órdenes `Market`, donde
`precioFill = vela.Open` (`MatchingEngine.cs:22`) sin ningún precio de referencia previo distinto
del propio Open. Para `Limit`/`Stop`/`StopLimit`, el precio ejecutado ya es el precio pactado por
la orden (`PrecioLimite`/`PrecioStop`, RN-03) — no hay divergencia que modelar, el fill ocurre
exactamente al precio configurado por diseño.

**Hallazgo corregido durante implementación**: el primer diseño proponía
`Slippage = |PrecioFill − PrecioReferencia| * Cantidad * TasaSlippage` con `PrecioReferencia =
vela.Open`. El test P3 (slippage debe reducir `Cash` en órdenes Market) falló — porque para
`Market`, `PrecioFill` **es** `vela.Open`: la resta siempre da `0`, el término de slippage nunca
podía aplicar bajo esa fórmula. Corregido a:

```
Slippage = 0                                    si orden.Type != Market
Slippage = Cantidad * PrecioFill * TasaSlippage si orden.Type == Market
```

Mismo patrón que `Comisión` pero con `TasaSlippage` propia — con el motor actual (sin libro de
órdenes ni ejecución fraccionada), no existe un segundo precio independiente contra el cual
comparar `PrecioFill`, así que modelar slippage como un porcentaje fijo del propio nocional es la
única forma consistente con lo que el motor puede observar hoy, sin inventar un precio "real de
mercado" que no existe en el dataset.

### D-064 — Origen del parámetro de coste

**Estado**: 🟢 Aprobada. **Selección: C — Experimento.**

`TasaComision`/`TasaSlippage` no pertenecen a `Instrumento` — el mismo símbolo (ej. BTCUSDT) puede
evaluarse bajo distintas hipótesis de coste dentro del laboratorio (ej. Experimento A: comisión
0.05%, Experimento B: comisión 0.10%). La propiedad del coste no pertenece al activo, sino a la
condición económica simulada.

**Separación aprobada**:
- **`Instrumento`** define identidad del mercado y margen (`Simbolo`, `TasaMargen` — sin cambios,
  D-057/D-061).
- **Configuración económica experimental** (nuevo, en `ConfiguracionExperimento` o un tipo
  agregado propio) define condiciones económicas simuladas: `TasaComision`, `TasaSlippage`.

No se agregan campos de coste dentro de `Instrumento` — mantiene la separación
`Instrumento ≠ Configuración económica experimental`.

---

## 4. Cambios reales aplicados en `src/`

| Archivo | Cambio |
|---|---|
| `Domain/Shared/ConfiguracionCostes.cs` (nuevo) | `record ConfiguracionCostes(TasaComision, TasaSlippage)`, con `ConfiguracionCostes.Default` (0m, 0m — D-061/D-065, preserva baseline) |
| `Domain/Matching/MatchingEngine.cs` | `Resolver`/`ResolverStopLimit` reciben `ConfiguracionCostes? costes = null`; nuevo método `CalcularCostoFriccion` reemplaza el literal `CostoFriccionReal: 0m` en ambos call sites |
| `Domain/VelaResolution/ResolutorVela.cs` | `Resolver`/`ResolverOco`/`ResolverRama`/`ResolverRamaOco` reciben y propagan `costes` hasta `MatchingEngine.Resolver` |
| `Domain/Portfolio/AplicadorFill.cs` | las 3 rutas (abrir/aumentar, reducir FIFO, Cross-Zero) restan `fill.CostoFriccionReal` de `Cash`; Cross-Zero prorratea el coste entre el tramo de cierre y el de apertura, proporcional a la cantidad de cada tramo |
| `Application/ConfiguracionExperimento.cs` | agrega `ConfiguracionCostes? Costes = null` + propiedad `CostesEfectivos` (mismo criterio D-061) |
| `Application/BacktestRunner.cs` | usa `config.CostesEfectivos`, lo pasa a `ResolutorVela.Resolver` |

**Hallazgo corregido durante implementación**: la fórmula de slippage originalmente diseñada
(`|PrecioFill − vela.Open|`) daba siempre `0` para órdenes `Market`, porque `PrecioFill` **es**
`vela.Open` en ese camino — detectado por el test P3, corregido a `Cantidad * PrecioFill *
TasaSlippage` (mismo patrón que comisión). Ver detalle en Sección 2.1.

**Verificación**: 101/101 tests pasan (96 preexistentes de Caso 2.1 sin modificar + 5 nuevos de
Caso 2.2 — P1 regresión, P2 comisión, P3 slippage solo Market, P4 Cross-Zero por tramo, P5
determinismo). Baseline de Caso 1 sin cambio: hash
`A48CCC57DA1919F533F4D532FDC0F945705681DCDA813B385BBFE7F44F40998E` idéntico al congelado.

**No se modifica**: FIFO, Cross-Zero, `RealizedPnL`, `IStrategy`, estrategias existentes — el
coste se resta de `Cash` en los mismos puntos donde cada ruta de `AplicadorFill` ya mutaba `Cash`,
sin alterar ninguna fórmula de posición.

---

## 3. Fuera de alcance de esta especificación

Sizing, Masaniello, gestión de riesgo, métricas financieras finales (drawdown, Sharpe) — Caso 2.3
y posteriores.

---

## Próximo paso

Implementación completa, 101/101 tests pasan, baseline de Caso 1 preservado. Pendiente: auditoría
de cierre de Caso 2.2.
