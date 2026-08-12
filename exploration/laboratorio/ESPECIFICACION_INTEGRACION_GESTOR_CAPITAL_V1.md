# Especificación — Integración de `ClasificadorIntencionOrden` en `GestorCapital` (Caso 4.2)

Estado: **documento de diseño implementable — previo a implementación**. Cierra D-084 integrando
el componente de 4.1 (`ClasificadorIntencionOrden`, aprobado y congelado) en `GestorCapital.Ajustar`.
No modifica código en este documento.

---

## 1. Punto de partida — código actual, verificado

`src/Domain/Portfolio/GestorCapital.cs:11-22`:

```csharp
public static IReadOnlyList<OrderRequest> Ajustar(IReadOnlyList<OrderRequest> requests, PortfolioState portfolio, ConfiguracionSizing? sizing)
{
    if (sizing is null)
        return requests;

    var capitalDisponible = portfolio.Cash - portfolio.Margin;
    var cantidadCalculada = capitalDisponible * sizing.PorcentajeRiesgo;

    return requests.Select(r => r with { Cantidad = cantidadCalculada }).ToList();
}
```

Invocado en `src/Application/BacktestRunner.cs:52`, recibiendo **la bolsa completa del ciclo N**
(`IReadOnlyList<OrderRequest> requests`, RN-14: "la bolsa completa del ciclo N se evalúa junta") —
no una orden aislada. Esto es relevante para la sección 2.

---

## 2. Hallazgo previo a la integración — clasificación dentro de una bolsa multi-orden

`ClasificadorIntencionOrden.Clasificar` es una consulta pura sobre `PortfolioState` en un instante
dado — no anticipa el efecto de otras órdenes de la misma bolsa que aún no se han aplicado.
Verificado en código de estrategias existentes que **una misma bolsa puede contener 2
`OrderRequest`** cuando una estrategia cierra una posición y abre la contraria en el mismo ciclo:

`exploration/EstrategiaNeutral.cs:48,51` (idéntico patrón en `EstrategiaZScoreReversion.cs`,
`EstrategiaTresMosqueteros.cs`, `EstrategiaMhiMayoria.cs`):

```csharp
ordenes.Add(new OrderRequest(Side.Sell, OrderType.Market, 1m));  // cierra el Buy vivo
// ...
ordenes.Add(new OrderRequest(Side.Sell, OrderType.Market, 1m));  // abre el nuevo Short
```

Si `GestorCapital.Ajustar` clasificara ambas órdenes de la bolsa contra el mismo `portfolio` sin
actualizar el estado entre una y otra, la segunda `Sell` (que en la ejecución real abre una
posición nueva, porque la primera ya cerró la existente) se clasificaría incorrectamente como
`CierreTotal` de una posición que, en ese punto de la secuencia, ya no debería considerarse viva —
produciría una clasificación basada en un estado de portfolio desactualizado dentro del mismo
ciclo.

**Esto no es un caso hipotético** — es el patrón real de las 5 estrategias existentes en cualquier
ciclo donde revierten posición. La especificación debe resolverlo explícitamente, no asumir que
cada `OrderRequest` de la bolsa se clasifica de forma independiente.

**Opción de resolución** (recomendación técnica, no vinculante): clasificar y procesar las órdenes
de la bolsa **secuencialmente**, actualizando una proyección local de `PosicionActual` (no
`PortfolioState` real — `GestorCapital` no debe mutar `Cash`/`Margin`/`LotesVivos`, eso sigue
siendo responsabilidad exclusiva de `AplicadorFill`, D-071) a medida que cada orden se clasifica,
de modo que la segunda orden de la bolsa se clasifique contra la posición que *resultaría* de la
primera, no contra el estado original. Esto requiere que `Ajustar` mantenga una variable local de
posición proyectada (`decimal posicionProyectada`, inicializada desde `PosicionActual.De(portfolio)`
y actualizada con la `Cantidad` con signo de cada orden ya clasificada), sin tocar
`portfolio.LotesVivos`.

---

## 3. Qué intenciones reciben sizing

**Regla central** (ya fijada en D-092, aquí se traduce a código):

| Intención | ¿`GestorCapital` sobrescribe `Cantidad`? | Razón |
|---|---|---|
| `Apertura` | Sí — aplica `cantidadCalculada` | Posición nueva, sizing determina cuánto arriesgar |
| `Aumento` | Sí — aplica `cantidadCalculada` | Ampliar posición existente, mismo criterio de riesgo |
| `ReduccionParcial` | No — conserva `request.Cantidad` original | La cantidad ya representa exactamente cuánto se quiere reducir; sizing no debe alterar una reducción parcial deliberada |
| `CierreTotal` | No — conserva `request.Cantidad` original | Debe cerrar exactamente la posición viva; sobrescribir `Cantidad` es la causa raíz original de D-084 (residuos de lotes) |
| `CrossZero` | No — conserva `request.Cantidad` original | La orden ya codifica cierre + apertura en una sola cantidad (`magnitudPosicion` a cerrar + excedente a abrir); aplicar sizing rompería ambos tramos a la vez |

**Justificación de excluir `CrossZero` de sizing** (no solo `CierreTotal`): a diferencia de
`ReduccionParcial`/`CierreTotal`, una orden Cross-Zero mezcla intención de cierre y apertura en una
sola `Cantidad` — no hay forma de aplicar sizing únicamente al tramo de apertura sin recalcular la
cantidad total de la orden, lo cual reintroduce el mismo problema dimensional de D-085 en un nuevo
lugar. Se excluye del alcance de 4.2 completo (no solo de sizing) — una estrategia que produce
Cross-Zero bajo sizing activo queda como caso no cubierto, documentado explícitamente en la sección
6, no silenciado.

---

## 4. Preservación exacta de `Sizing=null`

Sin cambios respecto al código actual: `if (sizing is null) return requests;` permanece como
primera línea de `Ajustar`, inalterada — el camino histórico (Caso 1, Caso 2, Caso 3A, los 3
baselines congelados) ni siquiera invoca `ClasificadorIntencionOrden`. Esto ya fue verificado como
diseño en `ESPECIFICACION_CLASIFICADOR_INTENCION_ORDEN_V1.md` §6 y se reafirma aquí como
restricción de 4.2: la integración se agrega **después** de ese `return` temprano, nunca antes.

---

## 5. Interacción con `ValidadorCapacidad`

Verificado en `BacktestRunner.cs:65,67`: `ValidadorCapacidad.Validar` y
`CalculadoraReservaPreventiva.Calcular` se invocan **después** de `GestorCapital.Ajustar` (línea
52 vs. 65), sobre `request.Cantidad` ya transformado. Esto significa:

- Con `ReduccionParcial`/`CierreTotal` sin sobrescribir (sección 3), la reserva calculada por
  `CalculadoraReservaPreventiva` para esas órdenes será la que corresponde a la cantidad real de
  cierre — coherente, a diferencia del comportamiento actual donde una orden de cierre podía
  reservar capacidad para una `cantidadCalculada` arbitraria de sizing, sin relación con lo que
  realmente se iba a cerrar.
- No se modifica `ValidadorCapacidad.cs` ni `CalculadoraReservaPreventiva.cs` en esta sub-fase —
  ambos siguen siendo puramente observacionales (D-059/D-060, no se reabren) y siguen operando
  sobre `request.Cantidad`, que ahora será más coherente por construcción, sin que ellos necesiten
  saber por qué.

---

## 6. Casos explícitamente fuera de alcance de 4.2

- **Cross-Zero bajo sizing activo**: la orden se ejecuta con su `Cantidad` original (sección 3),
  sin optimizar ni corregir su dimensión — quedará con la misma desproporción potencial que D-085
  ya documentó para cualquier `Cantidad` fija. No se resuelve aquí.
- **D-085 (unidades/exposición)**: sigue sin resolverse — 4.2 solo corrige *cuándo* se aplica
  sizing, no *cómo* se calcula `cantidadCalculada` (línea 19, sin cambios en esta sub-fase).
- **`ValidadorCapacidad` deja de ser observacional**: no se evalúa en 4.2 — sigue siendo D-059/
  D-060 vigente, sin reabrir.

---

## 7. Pruebas obligatorias antes de cerrar

- **P1-P5 — Sizing aplicado solo donde corresponde**: para cada intención de la tabla de la
  sección 3, verificar que `Ajustar` sobrescribe `Cantidad` (Apertura/Aumento) o la conserva
  (ReduccionParcial/CierreTotal/CrossZero) — con `sizing` no nulo en todos los casos.
- **P6 — Bolsa multi-orden (cierre + apertura en el mismo ciclo)**: reproducir el patrón real de
  `EstrategiaNeutral`/`EstrategiaZScoreReversion` (2 `OrderRequest` en la misma bolsa, la segunda
  de signo contrario a la posición original) y verificar que la segunda orden se clasifica como
  `Apertura` (no `CierreTotal`) gracias a la posición proyectada de la sección 2 — prueba
  específica del hallazgo documentado ahí, no cubierta por P1-P5.
- **P7 — `Sizing=null` sin cambio de comportamiento**: las 5 estrategias existentes, corridas con
  `Sizing=null`, producen el mismo `HashCompuesto` que antes de 4.1/4.2.
- **P8 — Corrida larga sin residuos de lotes**: repetir la corrida que originalmente colgó en el
  hallazgo de D-084 (baseline financiero con sizing activo en 1m, ~82k operaciones) con la
  integración completa — debe terminar en tiempo razonable, sin residuos de lotes acumulados, y
  sin degradar rendimiento respecto a `Sizing=null`.
- **P9 — No mutación de `PortfolioState` real por `GestorCapital`**: `portfolio.Cash`/`Margin`/
  `LotesVivos` permanecen sin cambios tras `Ajustar` (la posición proyectada de la sección 2 es
  estrictamente local a la función, nunca escrita de vuelta a `portfolio`) — D-071 sigue vigente
  (`GestorCapital` transforma `Cantidad`, nunca crea/elimina/muta estado del portfolio).
- **P10 — Regresión**: 107/107 (o el conteo vigente) tests de producción sin cambio + 3 baselines
  congelados (Caso 1, Caso 2, Caso 3A) sin regenerar ni alterar.

---

## Fuera de alcance de este documento

No se implementa código. No se modifica `ValidadorCapacidad.cs`/`CalculadoraReservaPreventiva.cs`.
No se resuelve D-085. No se define aquí el tratamiento de Cross-Zero bajo sizing más allá de
excluirlo (sección 3/6) — queda como deuda técnica explícita si se decide abordar en una sub-fase
posterior.

---

## Próximo paso

Autorización de implementación bajo el alcance de este documento: modificar
`src/Domain/Portfolio/GestorCapital.cs` integrando `ClasificadorIntencionOrden` con la resolución
de posición proyectada de la sección 2, pruebas P1-P10 como criterio de cierre. Tras cerrar 4.2,
D-084 queda resuelta — próximo paso natural es 4.3 (D-085, unidades y exposición).
