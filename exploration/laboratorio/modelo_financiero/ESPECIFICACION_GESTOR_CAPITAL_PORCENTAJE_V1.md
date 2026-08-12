# Especificación de Implementación — GestorCapital (Porcentaje) V1

Estado: **documento de diseño implementable — Caso 2.3, previo a implementación**. Traduce
D-066/D-067/D-068/D-069/D-070 (todas aprobadas, `DECISIONES_MODELO_ECONOMICO_V1.md`) a un diseño
concreto. No modifica código en este documento — mismo patrón que
`ESPECIFICACION_MODELO_ECONOMICO_BASE_V1.md` y `ESPECIFICACION_MODELO_COSTES_V1.md` aplicaron a
Caso 2.1/2.2.

Fuera de alcance: Masaniello, gestión de riesgo avanzada, sizing basado en `Equity` (D-067 lo
descarta explícitamente para V1), métricas financieras finales.

---

## 1. Tipo `ConfiguracionSizing`

```csharp
namespace TD_Project.Domain.Shared;

public sealed record ConfiguracionSizing(decimal PorcentajeRiesgo)
{
    // D-061/D-069 — experimento antiguo = sizing inactivo, Cantidad de la Strategy pasa intacta.
    public static ConfiguracionSizing? Default => null;
}
```

`Default` es `null`, no un objeto con `PorcentajeRiesgo = 0` — un porcentaje de `0%` produciría
`Cantidad = 0` en cada orden (comportamiento distinto de "sizing inactivo", que debe preservar
exactamente `Cantidad` tal como la `Strategy` la construyó). La ausencia de configuración, no un
valor neutro, es lo que representa "sin cambios respecto al histórico".

---

## 2. `GestorCapital`

```csharp
namespace TD_Project.Domain.Portfolio; // junto a AplicadorFill/PortfolioState

public static class GestorCapital
{
    // spec: Caso 2 D-066/D-067/D-070 — capa externa entre Strategy y ejecucion. No conoce
    // direccion/logica de la estrategia, solo ajusta Cantidad. sizing=null -> Cantidad intacta
    // (D-061/D-069, preserva baseline_final/ sin modificacion).
    public static IReadOnlyList<OrderRequest> Ajustar(IReadOnlyList<OrderRequest> requests, PortfolioState portfolio, ConfiguracionSizing? sizing)
    {
        if (sizing is null)
            return requests;

        var capitalDisponible = portfolio.Cash - portfolio.Margin;
        var cantidadCalculada = capitalDisponible * sizing.PorcentajeRiesgo;

        return requests.Select(r => r with { Cantidad = cantidadCalculada }).ToList();
    }
}
```

- `CapitalDisponible = Cash − Margin` (D-067, corrección aprobada) — usa solo lo que
  `PortfolioState` ya expone, sin calcular `Equity`.
- Recalcula sobre el mismo `portfolio` para **todas** las órdenes de la bolsa del ciclo (RN-14 — la
  bolsa completa se evalúa junta) — si `requests` trae más de un `OrderRequest` en el mismo ciclo,
  todas reciben la misma `Cantidad` calculada a partir del mismo `CapitalDisponible` (no se
  recalcula entre una orden y la siguiente dentro del mismo ciclo, porque ninguna se ha ejecutado
  todavía — `portfolio` no cambia hasta que `ResolutorVela.Resolver` procese la vela).
- Preserva `Side`/`Type`/`PrecioLimite`/`PrecioStop` del `OrderRequest` original (`with` expression)
  — el gestor solo toca `Cantidad`, nunca dirección ni tipo de orden (regla aprobada: "no debe
  modificar dirección Buy/Sell, decidir entradas, resolver martingala").
- Sin capital negativo por diseño: si `CapitalDisponible <= 0`, `Cantidad` resulta `<= 0` — no se
  añade ninguna cláusula especial; la orden con `Cantidad <= 0` sigue su curso normal hacia
  `ValidadorCapacidad` (que la marcará como incapacidad si corresponde, D-059) y hacia
  `MatchingEngine`, sin bloqueo introducido por el gestor mismo (consistente con D-059: el gestor
  no bloquea, solo calcula).

---

## 3. Punto de integración en `BacktestRunner.cs`

Verificado contra el código real — inserción entre `strategy.Observar` y el resto del loop:

```csharp
var requests = strategy.Observar(dataSlice);              // linea 47, sin cambios
requests = GestorCapital.Ajustar(requests, portfolio, config.SizingEfectivo);  // NUEVO

if (requests.Count > 0)
{
    var evaluacion = ValidadorBolsaRequests.Evaluar(requests);  // linea 51 — ya opera sobre Cantidad ajustada
    if (evaluacion.Aprobada)
    {
        var closeSiguiente = config.Velas[n + 1].Close;
        foreach (var request in requests)  // linea 55 — request.Cantidad ya es la ajustada
        {
            if (!ValidadorCapacidad.Validar(portfolio, request, closeSiguiente, instrumento.TasaMargen, portfolio.Margin))
            {
                var reserva = CalculadoraReservaPreventiva.Calcular(request, closeSiguiente, instrumento.TasaMargen);
                incapacidades.Add(new RegistroIncapacidad(...));
            }
            var orden = registrador.Registrar(request);  // Order hereda Cantidad ya ajustada
            // ...
        }
    }
}
```

**Punto crítico verificado**: `CalculadoraReservaPreventiva.Calcular` usa `request.Cantidad`
(`src/Domain/Broker/CalculadoraReservaPreventiva.cs:19`) — la reserva de capacidad debe calcularse
sobre la `Cantidad` **ya ajustada** por `GestorCapital`, no sobre la original de la `Strategy`.
Insertar `GestorCapital.Ajustar` antes de la línea 51 (evaluación de la bolsa) garantiza esto sin
ningún cambio en `ValidadorCapacidad`/`CalculadoraReservaPreventiva` — ambos siguen leyendo
`request.Cantidad` tal cual llega, sin saber si vino de la `Strategy` o del gestor (D-068).

---

## 4. `ConfiguracionExperimento`

```csharp
public sealed record ConfiguracionExperimento(
    decimal CapitalInicial,
    IReadOnlyList<Candle> Velas,
    int Warmup = 0,
    Instrumento? Instrumento = null,
    ConfiguracionCostes? Costes = null,
    ConfiguracionSizing? Sizing = null)  // NUEVO
{
    public Instrumento InstrumentoEfectivo => Instrumento ?? Domain.Shared.Instrumento.Default;
    public ConfiguracionCostes CostesEfectivos => Costes ?? Domain.Shared.ConfiguracionCostes.Default;
    public ConfiguracionSizing? SizingEfectivo => Sizing; // null es el estado valido "sin sizing"
}
```

Mismo criterio D-061 — parámetro opcional, default `null` preserva el comportamiento histórico sin
tocar los call sites existentes (20+ en `src/`/`tests/`/`exploration/`).

---

## 5. Trazabilidad (D-069)

Cualquier corrida con `Sizing` distinto de `null` produce un `HashCompuesto`
(`IdentidadExperimentoCompleta`, D-049) distinto de una corrida sin sizing — porque
`ConfiguracionExperimento` cambia, y por lo tanto los parámetros que alimentan el hash cambian.
Esto satisface D-069 sin necesidad de código adicional: activar sizing en una estrategia ya
existente automáticamente genera una identidad experimental nueva, nunca sobrescribe la evidencia
de la corrida histórica sin sizing.

**No se requiere ningún cambio en `EjecutorProtocolo`/`IdentidadExperimentoCompleta`** — ambos ya
son agnósticos a qué campos tiene `ConfiguracionExperimento`, mientras la entrada declarada al
pipeline (`EntradaProtocolo`) incluya el parámetro como parte de su configuración.

---

## 6. Pruebas obligatorias antes de cerrar

Mismo patrón que Caso 2.1/2.2 — P1 regresión, mas las específicas de este modelo:

- **P1 — Regresión sin sizing**: `Sizing = null` produce resultado idéntico al histórico (mismo
  hash de baseline, `A48CCC57...`).
- **P2 — Cálculo correcto**: con `Sizing` activo, `Cantidad` de cada `Order` registrada
  corresponde exactamente a `(Cash − Margin) × PorcentajeRiesgo` en el momento de la orden.
- **P3 — No modifica dirección**: `Side`/`Type`/`PrecioLimite`/`PrecioStop` del `OrderRequest`
  ajustado son idénticos al original — solo `Cantidad` cambia.
- **P4 — Bolsa completa consistente (RN-14)**: si una `Strategy` emite más de un `OrderRequest` en
  el mismo ciclo, todas reciben la misma `Cantidad` calculada sobre el mismo `CapitalDisponible`.
- **P5 — Determinismo**: misma entrada con `Sizing` activo produce el mismo resultado en dos
  ejecuciones.
- **P6 — Trazabilidad (D-069)**: dos configuraciones idénticas salvo `Sizing` producen
  `HashCompuesto` distinto.

---

## 7. Cambios reales aplicados en `src/`

| Archivo | Cambio |
|---|---|
| `Domain/Shared/ConfiguracionSizing.cs` (nuevo) | `record ConfiguracionSizing(PorcentajeRiesgo)`, con `Default => null` |
| `Domain/Portfolio/GestorCapital.cs` (nuevo) | `Ajustar(requests, portfolio, sizing)` — implementación exacta del diseño de la Sección 2 |
| `Application/ConfiguracionExperimento.cs` | agrega `ConfiguracionSizing? Sizing = null` (D-061) |
| `Application/BacktestRunner.cs` | `requests = GestorCapital.Ajustar(requests, portfolio, config.Sizing)` insertado inmediatamente después de `strategy.Observar`, antes de `ValidadorBolsaRequests.Evaluar` — mismo punto verificado en la Sección 3 |

**No se modifica**: `IStrategy`, estrategias existentes, `FIFO`/`Cross-Zero`/`RealizedPnL`
(`AplicadorFill`), `MatchingEngine`, `ValidadorCapacidad`/`CalculadoraReservaPreventiva` — el
cambio vive exclusivamente en `GestorCapital`/`ConfiguracionSizing` y su punto único de
integración en `BacktestRunner`.

**Verificación**: 107/107 tests pasan (101 preexistentes de Caso 2.1/2.2 sin modificar + 6 nuevos
— P1 regresión, P2 cálculo `(Cash−Margin)×%`, P3 no modifica dirección/tipo, P4 bolsa completa
RN-14, P5 determinismo, P6 trazabilidad). Baseline de Caso 1 sin cambio: hash
`A48CCC57DA1919F533F4D532FDC0F945705681DCDA813B385BBFE7F44F40998E` idéntico al congelado.

---

## Fuera de alcance de esta especificación

Masaniello, sizing basado en `Equity`, gestión de riesgo avanzada, métricas financieras finales,
modificación de estrategias existentes (siguen construyendo `OrderRequest` con `Cantidad: 1m`
placeholder — el gestor la sobrescribe cuando está activo).

---

## Próximo paso

Aprobación de este diseño → implementación en `src/` con las 6 pruebas de la Sección 6, siguiendo
el mismo patrón de verificación aplicado en Caso 2.1 (D-052-style: antes/después con sizing
inactivo produce resultado idéntico al baseline).
