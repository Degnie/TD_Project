# Especificación del Modelo Económico Base — V1

Estado: **documento de diseño implementable — Caso 2.1, previo a implementación**. Traduce
D-057/D-058/D-059 (aprobadas en `DECISIONES_MODELO_ECONOMICO_V1.md`) a un diseño concreto. No
modifica código todavía — este documento se aprueba, luego se implementa.

Fuera de alcance (explícito, igual que las especificaciones anteriores): Masaniello, sizing,
optimización, métricas financieras finales. Solo la capa económica base.

---

## 1. Estructura del instrumento (D-057)

**Hallazgo verificado antes de diseñar**: no existe ningún tipo `Instrumento`/`Asset`/`Symbol` en
`src/` — confirmado por búsqueda exhaustiva. `Candle` es OHLCV puro sin campo de símbolo;
`ConfiguracionExperimento` no conoce qué activo se está evaluando. D-057 (TasaMargen pertenece al
instrumento) requiere **crear** este concepto, no enganchar uno existente.

**Diseño mínimo propuesto**:

```csharp
namespace TD_Project.Domain.Shared;

public sealed record Instrumento(string Simbolo, decimal TasaMargen);
```

- Vive en `Domain/Shared` (mismo namespace que `Candle`/`OrderRequest` — tipos de dominio
  transversales, sin dependencias).
- Solo dos campos: lo mínimo que D-057 requiere resolver. No se agregan `TickSize`/`LotSize`/
  `MinNotional` — no existen hoy en `src/`, no hay evidencia de que Caso 2 Base los necesite, y
  agregarlos sin uso sería sobre-construir contra el hallazgo del inventario (Sección 1 de
  `ESPECIFICACION_MODELO_ECONOMICO_V1.md`, que no los menciona como pendiente).
- `Simbolo` es informativo (trazabilidad/identidad experimental, D-049) — el motor no lo usa para
  ninguna lógica condicional.

**Punto de integración**: `ConfiguracionExperimento` gana un campo `Instrumento`:

```csharp
public sealed record ConfiguracionExperimento(
    decimal CapitalInicial,
    IReadOnlyList<Candle> Velas,
    Instrumento Instrumento,
    int Warmup = 0);
```

`BacktestRunner.cs:78` cambia de `AplicadorFill.Aplicar(portfolio, fill)` (usa el default `0.1m`) a
`AplicadorFill.Aplicar(portfolio, fill, config.Instrumento.TasaMargen)` — explícito, sin default
oculto. El parámetro `tasaMargen` de `AplicadorFill.Aplicar` deja de tener valor por defecto.

**No se toca**: `CalculadoraLotes.AbrirLote`, `ResolutorCrossZero`, ni ningún cálculo de `Margin` —
siguen recibiendo `tasaMargen` como `decimal` suelto, tal como hoy (P-001, RN-08/RN-09 congelados).
Solo cambia el origen del valor, no la fórmula.

---

## 2. Unidad económica (D-058)

**Decisión aprobada**: unidad monetaria abstracta, no USDT real.

**Diseño**: no requiere cambio de tipo — `decimal` se mantiene (ningún `Money` wrapper, coherente
con el inventario de Sección 1: "no existe en absoluto, decisión implícita de usar `decimal` puro",
no se reabre esa decisión de tipo, solo su interpretación).

**Lo que sí cambia**: documentación y presentación, no código de cálculo.
- Toda ficha/reporte que muestre `CapitalInicial`/`Cash`/`Equity`/`Margin` debe etiquetar la cifra
  como **"unidades monetarias experimentales"**, nunca "USDT" — aplica a
  `ReporteConsolidadoGenerador.cs` y cualquier reporte futuro de Caso 2.
- El campo `Instrumento.Simbolo` (ej. `"BTCUSDT"`, Sección 1) identifica el dataset de precios
  usado, **no** declara que el capital simulado es USDT real — distinción explícita a mantener en
  cualquier texto generado.

---

## 3. Flujo Cash/Margin/Equity

**No se rediseña** (P-001) — `Cash + Margin + UnrealizedPnL = Equity` (RN-08), FIFO/Cross-Zero
(RN-09/RN-10) permanecen exactamente como están en `src/Domain/Portfolio/` y
`src/Domain/VelaResolution/ResolutorVela.cs`. El único cambio de flujo de esta especificación es el
origen de `tasaMargen` (Sección 1) — ningún otro punto de `AplicadorFill`, `CalculadoraLotes`,
`ConsumidorFifo` o `ResolutorCrossZero` se modifica.

---

## 4. Comportamiento ante falta de capacidad (D-059, D-060)

**Decisión aprobada**: registrar incapacidad, no bloquear (D-059) — Caso 1 (¿qué habría ocurrido
siguiendo la estrategia?) se mantiene sin alterar; Caso 2 agrega una capa de medición, no de
restricción. Evaluada antes de aplicar la orden, sobre el `OrderRequest` (D-060).

**Momento de evaluación (D-060)**: antes de aplicar la orden, sobre el `OrderRequest` — no
retrospectivamente sobre el `Fill` ya resuelto. Punto de integración verificado:
`ValidadorCapacidad`/`CalculadoraReservaPreventiva` (`src/Domain/Broker/`, ya implementados, RN-12
Fase 1) se **invocan** dentro del `foreach (var request in requests)` de `BacktestRunner.cs`
(líneas 52-57), el mismo bucle donde cada `OrderRequest` aprobado por `ValidadorBolsaRequests`
(RN-14) se convierte en `Order` — sin usar el resultado para rechazar la orden (D-059):

```csharp
// Dentro de BacktestRunner.cs, foreach (var request in requests) { ... } (líneas 52-57):
foreach (var request in requests)
{
    var capacidadSuficiente = ValidadorCapacidad.Validar(portfolio, request, config.Velas[n + 1].Close, config.Instrumento.TasaMargen, compromisosVigentes: portfolio.Margin);
    if (!capacidadSuficiente)
    {
        var reserva = CalculadoraReservaPreventiva.Calcular(request, config.Velas[n + 1].Close, config.Instrumento.TasaMargen);
        incapacidades.Add(new RegistroIncapacidad(config.Velas[n + 1].Timestamp, request, reserva, portfolio.Cash));
    }

    var orden = registrador.Registrar(request);
    ordenesPending.Add(orden);
    ordenesActivas.Add(orden); // la orden se registra y resuelve igual — Caso 1 no se altera (D-059)
}
```

- **Nuevo tipo** `RegistroIncapacidad(long Timestamp, OrderRequest Request, decimal ReservaRequerida, decimal CashDisponible)` — vive en `Domain/Broker` junto al validador que ya existe.
- **`ResultadoBacktest`** gana un campo `IReadOnlyList<RegistroIncapacidad> Incapacidades` — vacío si nunca ocurrió, sin afectar ningún campo existente del record.
- **No se modifica** el número de `Fill`/`Trade`/operaciones completadas de ninguna estrategia — la
  secuencia histórica y la comparabilidad con `baseline_final/` (Caso 1, `caso1-v1-experimental`)
  quedan intactas, tal como exige el criterio de D-059.
- Es la primera medición, no la decisión final: si en el futuro se decide bloquear (D-059 opción B),
  este registro ya provee la evidencia de cuánto cambiaría el comportamiento antes de decidirlo.

---

## 5. Integración con el motor existente — cambios reales aplicados en `src/`

**Hallazgo corregido durante implementación (D-062)**: el diseño original de esta sección asumía
que `ResolutorVela` no necesitaba cambios. Verificado como incorrecto en la práctica — el test P2
(cambiar `TasaMargen`, esperar que `Equity` cambie) falló porque `ResolutorVela.Resolver`/
`ResolverOco` calculan `EquityFinal`/`MarginFinal`/`CashFinal` (RN-11, comparación de trayectorias
A/B) sobre `PortfolioState` clonados, llamando a `AplicadorFill.Aplicar` sin `tasaMargen` (default
fijo `0.1m`) — una ruta de cálculo paralela e independiente de la que usa `BacktestRunner` para
`Trades`/`CashFinal`. Sin corregirlo, `EquityCurve` habría quedado permanentemente en `0.1m`
mientras el resto del resultado sí reflejaba el instrumento — dos modelos económicos divergentes
en el mismo experimento. D-062 (aprobada) resuelve esto propagando `tasaMargen` a través de
`ResolutorVela`, con default `0.1m` para preservar compatibilidad (mismo criterio D-061).

| Archivo | Cambio |
|---|---|
| `Domain/Shared/Instrumento.cs` (nuevo) | `record Instrumento(string Simbolo, decimal TasaMargen)`, con `Instrumento.Default` (fuente única del valor histórico `0.1m`, D-061) |
| `Application/ConfiguracionExperimento.cs` | agrega `Instrumento? Instrumento = null` (opcional, D-061) + propiedad `InstrumentoEfectivo` |
| `Application/BacktestRunner.cs` | usa `config.InstrumentoEfectivo`; pasa `instrumento.TasaMargen` a `ResolutorVela.Resolver` (D-062) y a `AplicadorFill.Aplicar`; invoca `ValidadorCapacidad.Validar` sobre cada `OrderRequest` antes del Fill, en modo observación (D-059/D-060); agrega `incapacidades` al `ResultadoBacktest` |
| `Domain/VelaResolution/ResolutorVela.cs` | `Resolver`/`ResolverOco`/`ResolverRama`/`ResolverRamaOco` reciben `tasaMargen` (default `0.1m`) y lo reenvían a `AplicadorFill.Aplicar` (D-062) |
| `Domain/Broker/RegistroIncapacidad.cs` (nuevo) | record de evidencia de incapacidad |
| `Application/ResultadoBacktest.cs` | agrega `IReadOnlyList<RegistroIncapacidad>? Incapacidades = null` (opcional, mismo criterio D-061 — también se construye desde `tests/`) + propiedad `IncapacidadesEfectivas` |

**`AplicadorFill.Aplicar` conserva su default `tasaMargen = 0.1m`** — no se eliminó (el primer
intento de quitarlo rompió `ResolutorVela.cs` y 6 call sites de `tests/Domain.Tests/Portfolio/`
que lo llaman con 2 argumentos; revertido para preservar P-001).

**No se modifica**: `CalculadoraLotes`, `ConsumidorFifo`, `ResolutorCrossZero`,
`CalculadoraRealizedPnL`, `MatchingEngine`, `ValidadorCapacidad`, `CalculadoraReservaPreventiva`
(estos dos últimos se **usan**, no se editan) — ninguna fórmula de posición/FIFO/Cross-Zero/
RealizedPnL cambió, solo se agregó el parámetro `tasaMargen` que ya existía internamente y su
propagación desde `Instrumento`.

**Consecuencia para consumidores existentes**: todo campo nuevo (`Instrumento` en
`ConfiguracionExperimento`, `Incapacidades` en `ResultadoBacktest`, `tasaMargen` en
`ResolutorVela`) es opcional con default equivalente al comportamiento histórico — los 20 call
sites existentes (incluidos los 8 en `tests/` y 2 en `tests/` para `ResultadoBacktest`) compilan
sin modificación y producen resultados idénticos (verificado: 96/96 tests de `src/`/`tests/`
pasan, hash de baseline `A48CCC57DA1919F533F4D532FDC0F945705681DCDA813B385BBFE7F44F40998E`
idéntico al de Caso 1 congelado).

---

## 6. Fuera de alcance de esta especificación

Sizing, Masaniello, optimización, métricas financieras finales (PnL%, drawdown, Sharpe) — Caso 2.2
y posteriores. Ningún cambio de código en este documento.

---

## Próximo paso

Aprobación de este diseño → implementación en `src/` con las pruebas correspondientes:
(1) antes/después con `TasaMargen=0.1m` explícito produce resultados idénticos al baseline actual
(mismo patrón de prueba que exigió D-052), (2) `Incapacidades` vacío en todas las corridas que hoy
pasan sin problema de capital, (3) `Incapacidades` no vacío en un escenario sintético diseñado para
forzarlo.
