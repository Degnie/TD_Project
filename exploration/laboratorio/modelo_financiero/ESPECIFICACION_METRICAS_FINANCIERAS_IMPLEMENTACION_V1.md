# Especificación de Implementación — Métricas Financieras V1

Estado: **documento de diseño implementable — Caso 2.4, previo a implementación**. Traduce
D-072/D-073/D-075/D-076/D-077/D-078 (D-074 explícitamente fuera de alcance, ver
`DECISIONES_MODELO_ECONOMICO_V1.md`) a un diseño concreto. No modifica código en este documento.

**Alcance confirmado (aclarado tras la revisión de D-074)**:
- **Implementadas en V1**: Capital inicial, Cash final, Equity final, PnL total, Equity Curve
  (referencia a la ya existente), Drawdown máximo, Exposición máxima.
- **Fuera de V1**: Duración del drawdown (D-074, sin DTO/cálculo/prueba en este documento).

---

## 1. Punto de integración verificado

**Hallazgo (D-077 aplicado antes de diseñar)**: `EjecutorProtocolo.EjecutarUnTimeframe`
(`exploration/laboratorio/protocolo/EjecutorProtocolo.cs:81-129`) ya tiene, en el mismo scope,
`resultado1` (`ResultadoBacktest`, fuente oficial de `EquityCurve`/`Cash`/`Margin`/`Trades`) y
`entrada.CapitalInicial` (fuente oficial de D-072) — exactamente donde hoy se construye `perfil`
(línea 120, `PerfilMultiTf.Medir`). No requiere ningún cambio en `src/` — las métricas financieras
se derivan del `ResultadoBacktest` que el motor ya produce, calculadas en la capa de laboratorio
(mismo patrón que `PerfilMultiTf`/`MetricasPorEscenario`, que tampoco tocan `src/`).

**Consecuencia de diseño**: `MetricasFinancieras` vive en `exploration/laboratorio/`, no en
`src/Application`/`src/Domain` — coherente con que Caso 1 completo (analizadores, clasificadores,
reportes) vive en el laboratorio, consumiendo el motor sin modificarlo (D-015).

---

## 2. Tipo `MetricasFinancieras`

```csharp
namespace TD_Project.ModeloFinanciero; // nuevo namespace, exploration/laboratorio/modelo_financiero/

public sealed record MetricasFinancieras(
    decimal CapitalInicial,        // D-072 — desde EntradaProtocolo.CapitalInicial
    decimal CashFinal,             // D-077 — desde ResultadoBacktest.CashFinal
    decimal EquityFinal,           // D-077 — desde ResultadoBacktest.EquityCurve[^1].Equity
    decimal PnLTotal,              // D-077 — desde ResultadoBacktest.Trades.Sum(t => t.RealizedPnL)
    decimal? DrawdownMaximoPct,    // D-073, D-078 — null si EquityCurve esta vacia
    decimal ExposicionMaxima);     // D-075 — Max(PortfolioSnapshot.Margin)
```

- `DrawdownMaximoPct` es `decimal?` — aplica D-078 directamente: si `EquityCurve` está vacía (ej.
  corrida `NotEvaluable`/`DataInvalid`), no hay pico que calcular, el campo es `null`, nunca `0m`.
- El resto de campos no son opcionales porque siempre hay una fuente válida cuando la corrida
  llegó a `Success` (único estado desde el que `EjecutarUnTimeframe` construye el `perfil`, línea
  111-113 de `EjecutorProtocolo.cs` ya descarta los demás estados antes de este punto).

---

## 3. `CalculadoraMetricasFinancieras`

```csharp
namespace TD_Project.ModeloFinanciero;

public static class CalculadoraMetricasFinancieras
{
    // spec: Caso 2 D-072/D-073/D-075/D-077/D-078 — deriva exclusivamente de ResultadoBacktest y
    // capitalInicial (fuente oficial, D-077). No recalcula desde Fills ni operaciones individuales.
    public static MetricasFinancieras Calcular(ResultadoBacktest resultado, decimal capitalInicial)
    {
        var equityFinal = resultado.EquityCurve.Count > 0 ? resultado.EquityCurve[^1].Equity : 0m;
        var pnlTotal = resultado.Trades.Sum(t => t.RealizedPnL);
        var drawdownMaximo = CalcularDrawdownMaximo(resultado.EquityCurve);
        var exposicionMaxima = resultado.PortfolioSnapshots.Count > 0
            ? resultado.PortfolioSnapshots.Max(s => s.Margin)
            : 0m;

        return new MetricasFinancieras(capitalInicial, resultado.CashFinal, equityFinal, pnlTotal, drawdownMaximo, exposicionMaxima);
    }

    // spec: D-073 — Peak(t) = max(Equity(0..t)), Drawdown(t) = (Peak(t)-Equity(t))/Peak(t),
    // DrawdownMax = max(Drawdown(t)). spec: D-078 — EquityCurve vacia -> null, nunca 0m.
    private static decimal? CalcularDrawdownMaximo(IReadOnlyList<EquityPoint> curva)
    {
        if (curva.Count == 0)
            return null;

        var pico = curva[0].Equity;
        var drawdownMaximo = 0m;

        foreach (var punto in curva)
        {
            pico = Math.Max(pico, punto.Equity);
            if (pico == 0m)
                continue; // sin capital, drawdown porcentual no definido para este punto
            var drawdown = (pico - punto.Equity) / pico;
            drawdownMaximo = Math.Max(drawdownMaximo, drawdown);
        }

        return drawdownMaximo;
    }
}
```

- `pico == 0m` (guardia agregada): si `Equity` llega a `0`, dividir por el pico produce
  indeterminación matemática si el pico también es `0` — se omite ese punto del cálculo de
  `Drawdown(t)` en vez de lanzar excepción o producir `NaN`/infinito. No afecta el resultado si el
  capital inicial es positivo (caso normal), solo protege el caso límite.
- Sin dependencias de `src/` más allá de los tipos ya expuestos (`ResultadoBacktest`,
  `EquityPoint`) — no requiere `using` nuevo hacia módulos de dominio.

---

## 4. Separación cálculo/reporte

`CalculadoraMetricasFinancieras.Calcular` (Sección 3) es el único punto que produce
`MetricasFinancieras` — ningún generador de reporte recalcula estos valores por su cuenta (D-077).

**Reporte** (no diseñado en detalle aquí — extiende `ReporteConsolidadoGenerador` existente, mismo
patrón D-076):
- Muestra `MetricasFinancieras` junto a capital inicial, timeframe y tamaño de muestra (ya
  disponibles en `PerfilMultiTf`/`ResultadoCorridaTimeframe`).
- Sin ordenamiento cuando se listan múltiples estrategias/timeframes (D-076).
- Etiqueta explícita "unidades monetarias experimentales" en toda cifra (D-058, Caso 2.0) — nunca
  "USDT" ni lenguaje que sugiera dinero real (Sección 5 de `ESPECIFICACION_METRICAS_FINANCIERAS_
  V1.md`).
- `DrawdownMaximoPct == null` se muestra como "no disponible", nunca como "0.00%" (D-078).

---

## 5. Integración en `EjecutorProtocolo`

```csharp
// Dentro de EjecutorUnTimeframe, inmediatamente despues de construir `perfil` (linea 120):
var metricasFinancieras = CalculadoraMetricasFinancieras.Calcular(resultado1, entrada.CapitalInicial);
```

`ResultadoCorridaTimeframe` gana un campo opcional `MetricasFinancieras? MetricasFinancieras =
null` (mismo criterio D-061/D-069 — opcional, no rompe los call sites existentes de
`TestsEjecutorProtocolo.cs`), poblado solo en la rama `Success`.

---

## 6. Pruebas obligatorias antes de cerrar

- **P1 — Capital inicial correcto**: `MetricasFinancieras.CapitalInicial` coincide exactamente con
  `EntradaProtocolo.CapitalInicial` de la corrida.
- **P2 — Cash/Equity/PnL desde fuente oficial**: los 3 campos coinciden con lectura directa de
  `ResultadoBacktest.CashFinal`/`EquityCurve[^1].Equity`/`Trades.Sum(RealizedPnL)` — sin
  recalcular con otra fórmula.
- **P3 — Drawdown correcto sobre caso conocido**: dataset sintético con un pico y una caída
  conocidos produce el `DrawdownMaximoPct` esperado calculado a mano.
- **P4 — Drawdown null cuando corresponde (D-078)**: `EquityCurve` vacía produce
  `DrawdownMaximoPct = null`, nunca `0m`.
- **P5 — Exposición máxima correcta**: coincide con `Max(PortfolioSnapshots.Select(s => s.Margin))`
  calculado independientemente en el test.
- **P6 — Regresión de Caso 1**: agregar `MetricasFinancieras` a `ResultadoCorridaTimeframe` no
  cambia el `HashCompuesto` del baseline (`A48CCC57...`) ni ningún campo existente de
  `ResultadoCorridaTimeframe`/`ResultadoProtocolo`.
- **P7 — Determinismo**: misma entrada produce las mismas `MetricasFinancieras` en dos ejecuciones.

---

## Fuera de alcance de esta especificación

Duración del drawdown (D-074), Masaniello, optimización, selección automática, recomendación de
inversión. Ningún cambio de código en este documento.

---

## 7. Cambios reales aplicados

| Archivo | Cambio |
|---|---|
| `exploration/laboratorio/modelo_financiero/MetricasFinancieras.cs` (nuevo) | `record` de la Sección 2 |
| `exploration/laboratorio/modelo_financiero/CalculadoraMetricasFinancieras.cs` (nuevo) | `Calcular`/`CalcularDrawdownMaximo` de la Sección 3 |
| `exploration/laboratorio/modelo_financiero/TestsMetricasFinancieras.cs` (nuevo) | 7 pruebas (P1-P7) |
| `exploration/laboratorio/modelo_financiero/ModeloFinanciero.csproj`/`Program.cs` (nuevos) | satélite ejecutable, mismo patrón que otros módulos del laboratorio |
| `exploration/laboratorio/protocolo/EjecutorProtocolo.cs` | `ResultadoCorridaTimeframe` gana `MetricasFinancieras? = null`; poblado en la rama `Success` con `CalculadoraMetricasFinancieras.Calcular(resultado1, entrada.CapitalInicial)` |
| `exploration/laboratorio/protocolo/Protocolo.csproj` | agrega `Compile Include Link` a los 2 archivos de `modelo_financiero/` |
| `exploration/laboratorio/protocolo/TestsEjecutorProtocolo.cs` | nueva prueba de integración: `MetricasFinancieras` poblada en `Success`, `null` en `Incomplete` |
| `exploration/laboratorio/LaboratorioSintetico.csproj` | agrega `Compile Remove="modelo_financiero\**\*.cs"` (mismo patrón recurrente de exclusión wildcard) |

**No se modifica**: ningún archivo de `src/`. `EquityCurve` no se recalcula ni se muta.

**Verificación**: 7/7 tests de `TestsMetricasFinancieras` (P1-P7 de la Sección 6) + 7/7 tests de
`TestsEjecutorProtocolo` (incluida la nueva prueba de integración) + 107/107 tests de producción
(`src/`/`tests/`, sin cambios, confirmando P6 explícitamente). Hash de baseline de Caso 1 sin
cambio: `A48CCC57DA1919F533F4D532FDC0F945705681DCDA813B385BBFE7F44F40998E`.

---

## Próximo paso

Implementación completa. Pendiente: auditoría de cierre de Caso 2.4.
