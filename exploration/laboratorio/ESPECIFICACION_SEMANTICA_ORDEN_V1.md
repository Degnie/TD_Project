# Especificación — Semántica de la Orden (Caso 4.1)

Estado: **documento de diseño implementable — previo a implementación**. Resuelve la pregunta
previa que D-091 exige antes de tocar `GestorCapital`: ¿cómo representa/determina el motor que una
`OrderRequest` es apertura, aumento, reducción, cierre o reversión, en el punto donde
`GestorCapital.Ajustar` necesita saberlo? No modifica código en este documento.

---

## 1. El problema, verificado en código

`GestorCapital.Ajustar` (`src/Domain/Portfolio/GestorCapital.cs:21`) se ejecuta en
`BacktestRunner.cs:52`, **antes** de que exista ningún `Fill` — solo tiene `OrderRequest`
(`Side`, `Type`, `Cantidad`) y `PortfolioState` (`Cash`, `Margin`, `LotesVivos`). En ese punto,
`Ajustar` no distingue si la orden va a abrir/aumentar una posición o a reducir/cerrar una
existente — sobrescribe `Cantidad` igual en ambos casos.

Más abajo en el mismo ciclo, `AplicadorFill.Aplicar` (`src/Domain/Portfolio/AplicadorFill.cs:17`)
**sí** determina esa distinción — pero después del `Fill`, comparando el signo del fill contra
`PosicionActual.De(portfolio)`:

```csharp
var cantidadConSigno = fill.Side == Side.Buy ? fill.Cantidad : -fill.Cantidad;
var mismoSigno = posicionActual == 0m || Math.Sign(posicionActual) == Math.Sign(cantidadConSigno);
```

`mismoSigno` → abre/aumenta (línea 19-27). `!mismoSigno` con `|Fill| <= |Posición|` → reduce FIFO
(línea 34-70, delegado a `ConsumidorFifo`). `!mismoSigno` con `|Fill| > |Posición|` → Cross-Zero,
cierra la posición vieja y abre una nueva en el mismo evento (línea 71-98, delegado a
`ResolutorCrossZero`).

**Hallazgo central**: el criterio que `AplicadorFill` usa (`Side` de la orden vs. signo de
`LotesVivos`) es calculable en el punto donde vive `GestorCapital`, porque `Ajustar` ya recibe
`PortfolioState portfolio` como parámetro (`GestorCapital.cs:11`) — `portfolio.LotesVivos` está
disponible antes del `Fill`, no solo después. La intención no requiere un dato nuevo del exterior;
requiere que `GestorCapital` haga la misma inferencia que `AplicadorFill` ya hace, en su propio
punto del ciclo.

**Verificación de las 5 estrategias existentes**: Tres Mosqueteros, MHI Mayoría, EMA Cross,
Z-Score Reversal, Neutral — todas emiten `Cantidad=1m` uniformemente en apertura y cierre (`Side`
es lo único que cambia entre abrir y cerrar). Ninguna estrategia real emite una `Cantidad` de
cierre distinta a la de apertura — el único caso con cantidades desiguales en todo el código es
`EstrategiaFixtureCrossZero.cs` (`Buy 10m` → `Sell 15m`), un fixture de test de Caso 1 diseñado
específicamente para forzar el camino Cross-Zero, no una estrategia de producción.

---

## 2. Opciones comparadas

### Opción 1 — Intención explícita agregada a `OrderRequest`

Agregar un campo a `OrderRequest` (ej. `IntencionOrden? Intencion = null`) que la estrategia
declara al construir la orden.

- **Ventajas**: la intención queda disponible sin inferencia en ningún punto del pipeline; permite
  a una estrategia futura expresar una intención que no se pueda derivar solo del signo (ej. cerrar
  parcialmente una cantidad específica distinta a "todo lo abierto").
- **Riesgos**: requiere que las 5 estrategias existentes se modifiquen para declarar la intención
  (o que el campo sea opcional con inferencia como fallback, complicando el contrato); traslada al
  autor de la estrategia una responsabilidad que el motor ya puede calcular por sí mismo — ninguna
  estrategia actual necesita decidir "abro o cierro", solo decide `Side`/`Cantidad`, igual que
  siempre. Viola el criterio de éxito de `PROPUESTA_CASO4_V1.md` §5 ("ninguna `IStrategy` existente
  conoce `Cash`/`Margin`/`Equity`") si el campo requiere que la estrategia conozca el estado del
  portfolio para decidir qué intención declarar.

### Opción 2 — Inferencia en una capa previa a `GestorCapital`

Un componente nuevo (ej. `ClasificadorIntencionOrden`) que recibe `OrderRequest` +
`PortfolioState` y produce la intención, ejecutándose inmediatamente antes de
`GestorCapital.Ajustar` en `BacktestRunner.cs`.

- **Ventajas**: reutiliza exactamente el criterio que `AplicadorFill` ya usa (mismo signo/magnitud
  contra `LotesVivos`), sin duplicar lógica dentro de `GestorCapital`; mantiene `GestorCapital`
  enfocado en su responsabilidad actual (calcular `Cantidad`), delegando la clasificación a un
  componente separado y testeable de forma aislada.
- **Riesgos**: introduce un nuevo tipo/componente en `src/Domain/Portfolio/`; requiere decidir si
  la intención calculada se le pasa a `GestorCapital` como parámetro nuevo (cambio de firma) o si
  el componente reemplaza la responsabilidad completa de `Ajustar`.

### Opción 3 — Resolver dentro de `GestorCapital`/`Portfolio`, sin componente nuevo

`GestorCapital.Ajustar` calcula la intención inline, replicando el criterio de `AplicadorFill`
directamente en su propio cuerpo (mismo criterio que Opción 2, pero sin extraerlo a un componente
separado).

- **Ventajas**: cambio más pequeño — un solo archivo modificado (`GestorCapital.cs`), sin tipo
  nuevo; no introduce una capa adicional al pipeline.
- **Riesgos**: duplica el criterio de clasificación (`Side` vs. signo de posición) en dos lugares
  del código (`AplicadorFill` y `GestorCapital`) sin una fuente única — si el criterio cambiara en
  el futuro, requeriría sincronizar ambos manualmente; menos testeable de forma aislada que un
  componente dedicado.

### Opción 4 — Mantener compatibilidad con estrategias existentes como restricción transversal

No es una opción alternativa a 1/2/3, sino una restricción que aplica a cualquiera de ellas:
ninguna de las 5 estrategias existentes debe requerir modificación de código para seguir
funcionando exactamente igual (mismo `HashCompuesto`, mismos resultados) bajo `Sizing=null` — ya
garantizado estructuralmente por D-091 (activación explícita), pero se declara aquí también a
nivel de firma de tipos: si la Opción elegida cambia la firma de `OrderRequest` (Opción 1), el
campo nuevo debe ser opcional con default que preserve el comportamiento actual.

---

## 3. Recomendación técnica (no vinculante — decisión del auditor)

**Opción 2** presenta el mejor balance para este proyecto: reutiliza el criterio ya validado y
probado de `AplicadorFill` (evita una segunda fuente de verdad divergente, a diferencia de la
Opción 3), no requiere que ninguna estrategia existente cambie (a diferencia de la Opción 1, que
trasladaría al autor de la estrategia una decisión que el motor puede calcular solo), y es
testeable de forma aislada (un componente nuevo con su propio conjunto de pruebas, mismo patrón
que `ConsumidorFifo`/`ResolutorCrossZero` ya establecieron como componentes de una sola
responsabilidad dentro de `Domain/Portfolio/`).

La Opción 4 (compatibilidad) aplica como restricción independientemente de cuál de las 3 primeras
se elija.

---

## 4. Qué NO se decide en este documento

- No se decide la firma exacta del componente/campo nuevo (nombres, tipos, ubicación exacta del
  archivo) — eso es diseño de implementación, posterior a esta elección.
- No se modifica `GestorCapital.cs` ni `OrderRequest.cs`.
- No se resuelve D-085 (unidades/exposición) — depende de que 4.1 esté cerrada, pero es una
  sub-fase separada (4.3 en `PROPUESTA_CASO4_V1.md` §6).
- No se activa sizing en ningún baseline congelado.

---

## 5. Pruebas obligatorias antes de cerrar (una vez elegida la opción)

- **Clasificación correcta en los 3 casos**: mismo signo (abre/aumenta), signo contrario con
  `|Cantidad| <= |Posición|` (reduce), signo contrario con `|Cantidad| > |Posición|` (Cross-Zero) —
  verificado contra los mismos casos que ya cubren `AplicadorFillTests`/`ConsumidorFifoTests`/
  `ResolutorCrossZeroTests` en `tests/Domain.Tests/`, sin duplicar sus casos, solo confirmando que
  la clasificación previa a `GestorCapital` coincide con la que `AplicadorFill` calcula después.
- **Sin cambio de comportamiento con `Sizing=null`**: las 5 estrategias existentes, corridas con
  `Sizing=null`, producen exactamente el mismo `HashCompuesto`/resultado que antes de esta fase —
  mismo criterio de no regresión que D-061/D-069 ya establecieron.
- **Compatibilidad de las 5 estrategias sin modificación de código**: ninguna estrategia existente
  requiere cambios en su implementación de `IStrategy.Observar` para seguir funcionando.

---

## Próximo paso

Selección de opción (1/2/3) por el auditor. Tras eso: especificación de implementación de 4.1
(diseño concreto del componente/campo elegido) y, solo después de cerrada, la especificación de
4.2 (corrección de `GestorCapital` usando la semántica ya resuelta).
