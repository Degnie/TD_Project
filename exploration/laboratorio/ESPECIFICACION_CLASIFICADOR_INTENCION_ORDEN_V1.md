# Especificación de Implementación — Clasificador de Intención de Orden (Caso 4.1)

Estado: **documento de diseño implementable — previo a implementación**. Traduce D-092 (componente
clasificador, previo a `GestorCapital`, fuente de verdad `PortfolioState`/`LotesVivos`) a un
diseño concreto. No modifica código en este documento.

---

## 1. Nombre y ubicación

**Nombre**: `ClasificadorIntencionOrden` (mismo nombre de trabajo usado en D-092, confirmado como
definitivo — sigue la convención de nombres ya establecida en `Domain/Portfolio/`:
`ConsumidorFifo`, `ResolutorCrossZero`, `CalculadoraLotes`, `CalculadoraRealizedPnL`).

**Ubicación**: `src/Domain/Portfolio/ClasificadorIntencionOrden.cs` — mismo namespace y carpeta que
`AplicadorFill`/`ConsumidorFifo`/`ResolutorCrossZero`, de quienes reutiliza el criterio de
clasificación (D-092: "una única definición de intención"). Vive en `src/` porque D-091 ya
estableció que las correcciones de semántica económica pueden vivir ahí, con activación
controlada — este componente no cambia comportamiento por sí solo, solo clasifica; es
`GestorCapital` quien decide qué hacer con la clasificación (sección 4).

**Tipo**: clase estática con un único método, mismo estilo que `ConsumidorFifo`/
`ResolutorCrossZero` — sin estado, sin instancia, coherente con el resto de `Domain/Portfolio/`.

---

## 2. Algoritmo exacto

Reutiliza el criterio ya verificado en `AplicadorFill.Aplicar`
(`src/Domain/Portfolio/AplicadorFill.cs:16-17,30-34`), extraído a una forma consultable antes del
`Fill`:

```
Entrada: PortfolioState portfolio, OrderRequest request

posicionActual = PosicionActual.De(portfolio)              // portfolio.LotesVivos.Sum(l => l.Cantidad)
cantidadConSigno = request.Side == Buy ? request.Cantidad : -request.Cantidad
mismoSigno = posicionActual == 0 || Math.Sign(posicionActual) == Math.Sign(cantidadConSigno)

Si mismoSigno:
    retornar Apertura (posicionActual == 0) o Aumento (posicionActual != 0)

Si no mismoSigno:
    magnitudRequest = |cantidadConSigno|
    magnitudPosicion = |posicionActual|

    Si magnitudRequest < magnitudPosicion:  retornar ReduccionParcial
    Si magnitudRequest == magnitudPosicion: retornar CierreTotal
    Si magnitudRequest > magnitudPosicion:  retornar CrossZero
```

**Idéntico en criterio** a `AplicadorFill.Aplicar` líneas 16-17 (`cantidadConSigno`/`mismoSigno`) y
30-34 (comparación de magnitudes) — el clasificador no introduce ninguna regla nueva, solo evalúa
la misma regla en un punto anterior del ciclo, usando `OrderRequest.Cantidad`/`Side` en vez de
`Fill.Cantidad`/`Fill.Side` (ambos representan la misma cantidad solicitada en este punto del
pipeline, antes de que el matching engine la resuelva contra el precio).

**Diferencia deliberada de granularidad**: `AplicadorFill` solo distingue 3 casos (mismo signo /
reduce FIFO / Cross-Zero, líneas 19, 34, 71) porque no necesita más para aplicar el fill. El
clasificador distingue **5** (Apertura, Aumento, ReduccionParcial, CierreTotal, CrossZero) porque
D-084 exige tratar distinto la apertura de una posición nueva (`posicionActual == 0`) del aumento
de una existente, y distinguir reducción parcial de cierre total — la fuente de la distinción
adicional es la misma, solo se expone con más detalle porque `GestorCapital` (sección 4) necesita
esa granularidad para decidir si aplicar sizing o no.

---

## 3. Tipo de resultado

```csharp
public enum IntencionOrden
{
    Apertura,
    Aumento,
    ReduccionParcial,
    CierreTotal,
    CrossZero
}
```

Enum simple, sin datos adicionales — la cantidad y el lado ya están en el `OrderRequest` original,
el clasificador solo etiqueta la intención, no transforma nada (mismo principio D-071 que ya rige
a `GestorCapital`: un componente de esta capa no crea ni elimina información, solo la deriva).

---

## 4. Cómo lo consume `GestorCapital` (adelanto de diseño, no implementado aquí)

No se implementa en este documento (queda para la especificación de 4.2), pero se declara el
contrato esperado para que 4.1 quede completa como diseño verificable:

```csharp
var intencion = ClasificadorIntencionOrden.Clasificar(portfolio, request);
// GestorCapital solo transforma Cantidad si intencion es Apertura o Aumento.
// ReduccionParcial/CierreTotal/CrossZero conservan la Cantidad original de la orden
// (la cantidad necesaria para cerrar exactamente la posición existente).
```

Esto es lo que redefine D-084 en `DECISIONES_CASO4_V1.md`: `GestorCapital` deja de sobrescribir
`Cantidad` incondicionalmente (`GestorCapital.cs:21` actual) y solo lo hace para
`Apertura`/`Aumento`.

---

## 5. Casos cubiertos

| Caso | `posicionActual` | `Side`/`Cantidad` de la orden | Resultado esperado |
|---|---|---|---|
| Apertura | `0` | cualquiera | `Apertura` |
| Aumento | `!= 0`, mismo signo que la orden | cualquiera | `Aumento` |
| Reducción parcial | `!= 0`, signo contrario | `|Cantidad| < |posicionActual|` | `ReduccionParcial` |
| Cierre total | `!= 0`, signo contrario | `|Cantidad| == |posicionActual|` | `CierreTotal` |
| Cross-Zero | `!= 0`, signo contrario | `|Cantidad| > |posicionActual|` | `CrossZero` |

Espejado exactamente contra los 4 casos ya cubiertos por `AplicadorFillIntegracionTests.cs`
(`UnFillDelMismoSignoAbreUnLoteSinRealizedPnL`, `UnFillDeReduccionConsumeLotesFIFOEnFlujoReal`,
`UnFillDeReduccionSobreUnaPosicionCortaCalculaElRealizedPnLConElSignoCorrecto`,
`UnFillCrossZeroCierraLaPosicionAnteriorYAbreUnaNueva`) — el clasificador debe producir el
resultado que anticipa lo que esos tests ya verifican que `AplicadorFill` hace después.

---

## 6. Compatibilidad con `Sizing=null`

El clasificador se ejecuta siempre (no depende de si sizing está activo) — es un componente puro
de consulta, sin efecto secundario. La compatibilidad con el comportamiento histórico
(`Sizing=null`) no depende del clasificador en sí, sino de cómo lo use `GestorCapital` en 4.2: si
`sizing is null`, `GestorCapital.Ajustar` retorna `requests` sin cambios (código actual,
`GestorCapital.cs:13-14`, no se toca) — el clasificador ni siquiera necesita invocarse en ese
camino. Esto preserva la restricción de D-091 (comportamiento histórico bit-a-bit idéntico) sin
que el clasificador tenga que saber nada sobre sizing.

---

## 7. Restricciones de no-modificación (reiteradas de D-092)

- `IStrategy`: sin cambios, sin nuevos miembros.
- Las 5 estrategias existentes (Tres Mosqueteros, MHI Mayoría, EMA Cross, Z-Score Reversal,
  Neutral): sin cambios de código.
- `ConsumidorFifo`/`ResolutorCrossZero`/`AplicadorFill`: sin cambios — el clasificador reutiliza su
  criterio, no lo reemplaza ni lo modifica.
- `OrderRequest`: sin campos nuevos (Opción 1 de `ESPECIFICACION_SEMANTICA_ORDEN_V1.md`,
  explícitamente rechazada por D-092).

---

## 8. Pruebas obligatorias antes de cerrar

- **P1-P5 — Los 5 casos de la tabla de la sección 5**: cada uno verificado con un
  `PortfolioState` construido explícitamente (mismo estilo que
  `AplicadorFillIntegracionTests.cs`), sin pasar por el pipeline completo.
- **P6 — Coincidencia con `AplicadorFill`**: para cada uno de los 4 escenarios ya cubiertos por
  `AplicadorFillIntegracionTests.cs`, clasificar la orden *antes* de aplicar el fill y verificar
  que la intención resultante es consistente con la rama que `AplicadorFill.Aplicar` efectivamente
  toma (mismo signo → `Apertura`/`Aumento`; FIFO → `ReduccionParcial`/`CierreTotal`; Cross-Zero →
  `CrossZero`) — prueba de integración cruzada entre el clasificador nuevo y el componente ya
  congelado, sin modificar este último.
- **P7 — Pureza**: dos invocaciones sucesivas con el mismo `PortfolioState`/`OrderRequest`
  producen el mismo resultado, y `portfolio` no se modifica por la clasificación (verificado
  comparando `PortfolioState` antes/después de `Clasificar`).
- **P8 — Regresión**: 107/107 tests de producción sin cambio (el clasificador es código nuevo, no
  invocado todavía por ningún flujo existente hasta que 4.2 lo integre) + 3 baselines congelados
  (Caso 1, Caso 2, Caso 3A) sin regenerar ni alterar.

---

## Fuera de alcance de este documento

No se modifica `GestorCapital.cs` — la integración es 4.2, documento siguiente. No se modifica
`OrderRequest.cs`, `AplicadorFill.cs`, `ConsumidorFifo.cs`, ni `ResolutorCrossZero.cs`. No se
resuelve D-085.

---

## Próximo paso

Autorización de implementación bajo el alcance de este documento:
`src/Domain/Portfolio/ClasificadorIntencionOrden.cs` + pruebas en `tests/Domain.Tests/Portfolio/`,
P1-P8 como criterio de cierre. Solo después: especificación de 4.2 (integración en
`GestorCapital`).
