# Especificación de Arquitectura del Gestor de Capital — V1

Estado: **documento de diseño — Caso 2.3, resolución de D-066, previo a implementación**. Define
**dónde** vive la transformación señal→cantidad, no **qué fórmula** de sizing se usa (D-067 sigue
pendiente, resuelto en un documento posterior una vez elegido el candidato de
`EVALUACION_MODELOS_GESTION_RIESGO_V1.md`). No modifica código en este documento.

---

## 1. Punto de integración verificado en el motor

`src/Application/BacktestRunner.cs`, dentro del loop principal:

```csharp
var requests = strategy.Observar(dataSlice);              // linea 47 — señal, sin cantidad de capital
// ...
foreach (var request in requests)                          // linea 56 — hoy: request.Cantidad ya fija
{
    // ValidadorCapacidad.Validar(...) usa request.Cantidad tal cual llega
    var orden = registrador.Registrar(request);
    // ...
}
```

`portfolio` (con `Cash`/`Margin`/`LotesVivos` ya poblados por el ciclo anterior) existe en el mismo
scope, **antes** del `foreach`. No hay ninguna barrera técnica para leer `portfolio.Cash`/
`portfolio.Margin` en ese punto exacto — el obstáculo no es de acceso a datos, es de **dónde debe
vivir esa lectura** para no romper P-002 (`IStrategy` no debe conocer capital).

**Confirmado**: `IStrategy.Observar(DataSlice dataSlice)` — único método, `DataSlice` no contiene
ningún campo de `Cash`/`Equity`/`PortfolioState` (`src/Domain/Strategy/IStrategy.cs`,
`src/Domain/Shared/DataSlice.cs`). La estrategia no tiene ni tendrá acceso a capital si se elige la
Opción A de esta especificación.

---

## 2. Evaluación de las 3 opciones

### Opción A — Capa externa al motor (RiskManager entre señal y ejecución)

```
Strategy.Observar(dataSlice)
        ↓  señal: OrderRequest con Cantidad = placeholder (ej. 1m, o un valor "unitario")
GestorCapital.Ajustar(requests, portfolio)
        ↓  cantidad final: OrderRequest con Cantidad recalculada
foreach (var request in requestsAjustados) { ... }  // resto del loop sin cambios
```

**Punto de inserción**: entre la línea 47 (`strategy.Observar`) y la línea 49
(`if (requests.Count > 0)`) de `BacktestRunner.cs` — una única llamada nueva,
`requests = GestorCapital.Ajustar(requests, portfolio, configSizing)`, antes de que
`ValidadorBolsaRequests`/`ValidadorCapacidad` operen sobre las cantidades ya ajustadas.

**Evaluación**:
- Mantiene `IStrategy` intacto — cero cambio de contrato, cero riesgo para las 3 estrategias
  existentes (P-002 respetado literalmente).
- Compatible con múltiples modelos (D-067 sigue abierta) — `GestorCapital` es un punto de
  extensión único, cualquier candidato (A/B/C de la evaluación) se implementa como una función
  `(IReadOnlyList<OrderRequest>, PortfolioState) → IReadOnlyList<OrderRequest>`.
- La estrategia sigue emitiendo una señal de *dirección* (Buy/Sell) — el valor de `Cantidad` que
  hoy hardcodea (`1m`) pasa a interpretarse como un placeholder que el gestor puede sobrescribir,
  no como el tamaño real cuando hay gestión de capital activa.
- **Trazabilidad**: el `OrderRequest` original (señal) y el ajustado (ejecutado) son distintos —
  requiere decidir si ambos quedan registrados en algún log, o solo el final (pendiente para la
  fase de implementación, no bloquea el diseño de arquitectura).

### Opción B — Extender el contrato de `IStrategy`

```csharp
IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice, EstadoCapitalDisponible capital);
```

**Evaluación**:
- Rompe P-002 directamente — la estrategia pasa a *conocer* capital, aunque no lo use.
- Afecta a **todas** las estrategias existentes (Tres Mosqueteros, MHI Mayoría, EMA Cross) — un
  cambio de firma en `IStrategy` obliga a recompilar/editar las 3, incluso si ninguna decide usar
  el nuevo parámetro.
- No aporta nada que la Opción A no resuelva ya sin tocar el contrato — descartada.

### Opción C — Generador previo al backtest (transforma órdenes antes de ejecutar)

```
Pre-generar: Strategy corre "en vacío" contra todo el dataset → lista completa de OrderRequest
        ↓
Generador de sizing transforma la lista completa
        ↓
BacktestRunner ejecuta la lista ya transformada
```

**Evaluación — riesgo estructural verificado contra el motor real**: `BacktestRunner.cs` no separa
"decidir todas las órdenes" de "ejecutarlas" — son la misma pasada (`strategy.Observar` se invoca
dentro del mismo loop que resuelve Fills y muta `portfolio`, línea por línea). Una estrategia con
martingala (Tres Mosqueteros/MHI Mayoría) decide su *siguiente* señal en función del resultado del
Fill *anterior* (`InfoOperacionResuelta`, vía el callback `onOperacionResuelta` que ambas
estrategias reciben) — no es posible "pre-generar" todas las órdenes de una estrategia con
martingala sin ejecutar el backtest completo primero, lo cual vuelve la Opción C circular (para
generar las órdenes necesitaría los resultados que solo se conocen al ejecutar esas mismas
órdenes). Esta opción **no es viable** para el motor actual sin un rediseño mayor — descartada, no
solo por preferencia sino por incompatibilidad estructural con RN-13/martingala.

---

## 3. Decisión propuesta

**Opción A** es la única compatible con el motor actual sin romper `IStrategy` ni requerir
rediseño estructural — confirmado contra código real, no solo por preferencia de diseño.

**Forma concreta**:

```csharp
namespace TD_Project.Domain.Portfolio; // o Domain.Broker — junto a ValidadorCapacidad

public static class GestorCapital
{
    public static IReadOnlyList<OrderRequest> Ajustar(
        IReadOnlyList<OrderRequest> requests, PortfolioState portfolio, ConfiguracionSizing? sizing = null);
}
```

- `sizing` opcional, default `null` → sin ajuste, `Cantidad` de la `Strategy` pasa intacta (mismo
  criterio D-061 — experimentos existentes sin `ConfiguracionSizing` producen resultado idéntico,
  preservando `baseline_final/` sin modificación, conforme a D-069).
- No decide *cómo* calcular `Cantidad` — esa es la fórmula de D-067, todavía sin elegir.
- Punto único de integración en `BacktestRunner.cs`, entre `strategy.Observar` y el resto del loop
  — no requiere tocar `ResolutorVela`, `AplicadorFill`, `MatchingEngine` ni ningún motor de
  posiciones.

---

## 4. Relación con D-068 (`ValidadorCapacidad`)

Confirma la dirección ya aprobada: `GestorCapital.Ajustar` se ejecuta **antes** de
`ValidadorCapacidad.Validar` en el loop (línea 60 de `BacktestRunner.cs`) — el validador sigue
evaluando la `Cantidad` que reciba, sin importar si vino de la `Strategy` directamente (sizing
inactivo) o del `GestorCapital` (sizing activo). Ningún cambio en `ValidadorCapacidad` ni
`CalculadoraReservaPreventiva` — siguen siendo consumidores, no generadores de tamaño.

---

## Fuera de alcance de esta especificación

Fórmula de sizing (D-067, sin resolver). Implementación de código. Modificación de estrategias
existentes. Alteración de `baseline_final/`. Masaniello.

---

## Próximo paso

Con D-066 resuelta (Opción A), la auditoría puede: (a) resolver D-067 seleccionando un candidato
de `EVALUACION_MODELOS_GESTION_RIESGO_V1.md` para implementar dentro de `GestorCapital`, o (b)
implementar primero `GestorCapital` con un único modelo trivial (capital fijo, equivalente al
comportamiento actual) como esqueleto, y resolver D-067 en una iteración posterior.
