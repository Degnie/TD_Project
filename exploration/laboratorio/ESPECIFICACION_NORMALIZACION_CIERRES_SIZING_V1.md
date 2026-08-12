# Especificación — Normalización de Cierres bajo Sizing (Caso 4.3, D-095)

Estado: **documento de diseño implementable — previo a implementación**. Traduce D-095 (la
cantidad efectiva de cierre debe derivarse de la posición viva, no de la cantidad nominal de la
estrategia) a diseño concreto. No modifica código en este documento. Implementación de 4.3
permanece detenida hasta cerrar este documento.

---

## 1. Hallazgo que motiva esta especificación (resumen verificado)

Con sizing activo, una estrategia histórica que "cierra" con su `Cantidad` nominal fija (ej.
`Sell 1m`) casi nunca coincide con la posición real que sizing dejó abierta (ej. `Long
0.011111`). El clasificador de 4.1 (`ClasificadorIntencionOrden`) interpreta correctamente
`|1m| > |0.011111|` como `CrossZero` — pero ese Cross-Zero es **espurio**: no representa una
intención real de invertir posición, sino un artefacto de que la estrategia no conoce el tamaño
real de lo que abrió (P-002, correcto que no lo conozca).

---

## 2. Dónde vive la normalización

**Extiende `ClasificadorIntencionOrden`, no un componente nuevo separado.** Razón: la
normalización necesita exactamente el mismo dato de entrada (`PortfolioState`, `OrderRequest`) que
la clasificación, y produce un resultado que la clasificación ya casi calcula — `PosicionActual.De
(portfolio)` (la magnitud de la posición viva) es precisamente el valor que la cantidad de cierre
debe adoptar. Separarlo en un componente distinto duplicaría la lectura de `PosicionActual` sin
beneficio de responsabilidad única real (ambos responden "qué es esta orden en relación a la
posición actual").

**Nueva forma del método** (ampliando el existente, no reemplazándolo):

```csharp
public static (IntencionOrden Intencion, decimal CantidadEfectiva) Clasificar(PortfolioState portfolio, OrderRequest request)
```

`CantidadEfectiva` es la cantidad que debe usarse en lugar de `request.Cantidad` cuando la
intención es `ReduccionParcial`/`CierreTotal` — para `Apertura`/`Aumento`/`CrossZero` genuino,
`CantidadEfectiva == request.Cantidad` (sin cambio).

**Compatibilidad con los call sites existentes**: `ClasificadorIntencionOrdenTests.cs` (Domain.Tests,
11 pruebas de 4.1) y `GestorCapitalTests.cs` (P9, `Caso3.csproj` no lo usa directamente) invocan
`Clasificar` esperando solo `IntencionOrden` — cambiar la firma de retorno a una tupla rompe esos
call sites. Se resuelve actualizando esas 2 pruebas para leer `.Intencion` en vez del valor
directo (cambio mecánico, sin alterar qué verifican).

---

## 3. Redefinición de la clasificación con normalización — los 3 casos

El hallazgo central (sección 1, verificado contra `ResolutorCrossZero.cs:9`:
`CantidadPosicionNueva = cantidadFillInversion − posicionVieja.Cantidad`) es que **normalizar la
cantidad de cierre a la posición real, antes de clasificar, elimina la mayoría de los Cross-Zero
espurios** — convierte el caso a `CierreTotal` genuino. La clasificación de 4.1 debe ejecutarse
**después** de la normalización, no antes:

```
posicionActual = PosicionActual.De(portfolio)
cantidadConSigno = Side == Buy ? Cantidad : -Cantidad
mismoSigno = posicionActual == 0 || signo(posicionActual) == signo(cantidadConSigno)

Si mismoSigno:
    CantidadEfectiva = Cantidad (sin normalizar — apertura/aumento usan la cantidad solicitada)
    retornar (Apertura o Aumento, CantidadEfectiva)

Si no mismoSigno:
    magnitudSolicitada = |cantidadConSigno|
    magnitudPosicion = |posicionActual|

    Si magnitudSolicitada >= magnitudPosicion:
        # la orden "quiere" reducir/cerrar/invertir mas de lo que hay vivo — pero la INTENCION
        # real de una estrategia historica con cantidad nominal fija es "cerrar todo lo que
        # tengo abierto", no "invertir la cantidad exacta que pedi". Normalizar a la posicion real.
        CantidadEfectiva = magnitudPosicion
        retornar (CierreTotal, CantidadEfectiva)
    Si no:
        CantidadEfectiva = magnitudSolicitada  # ya es menor a la posicion, sin normalizar
        retornar (ReduccionParcial, CantidadEfectiva)
```

**Cross-Zero deja de ser alcanzable por esta vía** — con la normalización, ninguna orden de cierre
puede exceder la posición real (la normalización la recorta exactamente a `magnitudPosicion`). Esto
es la consecuencia directa y correcta del hallazgo: el Cross-Zero que aparecía no era una intención
real de inversión, era el síntoma del defecto que D-095 corrige.

**Pregunta abierta que esta especificación deja explícita, no resuelta unilateralmente**: ¿puede
una estrategia real (no bajo sizing, o incluso con sizing) querer invertir posición
genuinamente — es decir, emitir una orden cuya cantidad nominal *intencionalmente* excede su
posición conocida para cruzar a la dirección contraria? Verificado en código: **sí** — el fixture
`EstrategiaFixtureCrossZero.cs` (Caso 1) y `EstrategiaCrossZeroControlada.cs` (test de Caso 2,
`Buy 10` → `Sell 15`) construyen exactamente ese escenario deliberadamente, **sin sizing activo**
(`Sizing=null` en ambos casos conocidos). Bajo `Sizing=null`, `GestorCapital.Ajustar` retorna
`requests` sin invocar el clasificador en absoluto (código actual, `if (sizing is null) return
requests;`) — la normalización de esta especificación solo se ejecuta cuando sizing está activo,
así que el Cross-Zero genuino de esos fixtures **no se ve afectado** (verificado como restricción
de diseño, sección 4). La normalización sizing-only significa que, bajo sizing activo, todo
"exceso" de cantidad se interpreta como error de nomenclatura de la estrategia histórica, nunca
como intención de inversión — coherente con que ninguna de las 5 estrategias reales fue diseñada
pensando en sizing.

---

## 4. Restricciones de diseño

- **La normalización solo aplica cuando sizing está activo** — bajo `Sizing=null`,
  `GestorCapital.Ajustar` retorna temprano sin invocar el clasificador (código actual, sin
  cambios) — ningún baseline congelado ni ningún test de Cross-Zero genuino (`EstrategiaFixtureCrossZero`,
  `EstrategiaCrossZeroControlada`) se ve afectado.
- **`IStrategy` no cambia** — ninguna estrategia recibe cantidad ejecutada, posición actual, ni
  capital (P-002, reafirmado).
- **No se aplica sizing a Cross-Zero** — con la normalización, Cross-Zero deja de ser alcanzable
  para el caso espurio; si en el futuro una estrategia diseñada explícitamente para sizing quisiera
  invertir posición de forma genuina, ese es un caso nuevo fuera de alcance de esta especificación
  (ninguna de las 5 estrategias actuales lo hace).
- **`ConsumidorFifo`/`ResolutorCrossZero`/`AplicadorFill` sin cambios** — la normalización ocurre
  enteramente en `ClasificadorIntencionOrden`/`GestorCapital`, antes de que la orden llegue al
  matching/fill. El motor de aplicación de fills no sabe ni necesita saber que la cantidad fue
  normalizada.

---

## 5. Cómo lo consume `GestorCapital`

```csharp
var (intencion, cantidadEfectiva) = ClasificadorIntencionOrden.Clasificar(portfolioProyectado, request);
var aplicaSizing = intencion is IntencionOrden.Apertura or IntencionOrden.Aumento;

resultado.Add(aplicaSizing
    ? request with { Cantidad = cantidadCalculada }
    : request with { Cantidad = cantidadEfectiva });
```

Para `ReduccionParcial`/`CierreTotal`, la orden ya no conserva `request.Cantidad` original sin
tocar (diseño de 4.2) — ahora conserva `cantidadEfectiva` (que coincide con `request.Cantidad`
cuando la cantidad nominal ya era menor o igual a la posición real, y la recorta cuando la excedía).
Esto es una modificación al comportamiento de 4.2, declarada explícitamente: 4.2 asumía que
"conservar la cantidad original" siempre era correcto para reducción/cierre; D-095 corrige esa
asunción para el caso donde la cantidad original no puede ser correcta por construcción (excede la
posición real bajo sizing).

**Actualización de la proyección de posición** (mismo mecanismo de 4.2,
`ESPECIFICACION_INTEGRACION_GESTOR_CAPITAL_V1.md` §2): debe avanzar con `cantidadEfectiva`, no con
`request.Cantidad` — de lo contrario la proyección para la siguiente orden de la bolsa quedaría
desalineada con lo que realmente se ejecutó.

---

## 6. Pruebas obligatorias antes de cerrar

- **P1 — Estrategia histórica + sizing activo, caso exacto del hallazgo**: reproducir
  `Buy 1m` (Apertura, sizing) → `Sell 1m` (cantidad nominal fija) sobre la posición resultante —
  verificar que la segunda orden ejecuta con `CantidadEfectiva = posición real` (ej. `0.011111`),
  no `1m`, y que la intención resultante es `CierreTotal`, no `CrossZero`.
- **P2 — Cierre parcial sin normalización necesaria**: orden de cierre con cantidad nominal ya
  menor a la posición real (ej. `Sell 0.005` sobre `Long 0.011111`) — verificar que
  `CantidadEfectiva == request.Cantidad` (sin recorte, comportamiento ya correcto de 4.2 sin
  cambios).
- **P3 — Reversión en una bolsa, con normalización**: repetir el escenario de 4.2 P9 (2 órdenes en
  la misma bolsa, cierre + apertura) pero con la estrategia usando cantidad nominal fija distinta
  de la posición real — verificar que la primera orden normaliza correctamente a `CierreTotal` y
  la proyección de posición avanza con `CantidadEfectiva`, permitiendo que la segunda orden se
  clasifique como `Apertura` sobre la base correcta.
- **P4 — Cross-Zero genuino bajo `Sizing=null` no se ve afectado**: `EstrategiaCrossZeroControlada`
  (`Buy 10` → `Sell 15`) corrida sin sizing produce exactamente el mismo resultado que antes de
  D-095 — la normalización no se ejecuta en absoluto (guarda `sizing is null` sin cambios).
- **P5 — Baselines sin sizing intactos**: los 3 baselines congelados (Caso 1, Caso 2, Caso 3A) no
  regeneran ni cambian — ninguno invoca sizing activo.
- **P6 — No regresión de D-084 (4.2)**: `ReduccionParcial`/`CierreTotal` con cantidad nominal ya
  correcta (no excede la posición) siguen comportándose exactamente como 4.2 estableció.
- **P7 — Corrida larga, verificación de escala**: repetir la verificación P8 de
  `ESPECIFICACION_IMPLEMENTACION_SIZING_CORREGIDO_V1.md` (Tres Mosqueteros, dataset 1m o 1D,
  sizing activo) — confirmar que `CashFinal`/`EquityFinal` ya no muestran la desproporción extrema
  que originó este hallazgo (sin exigir un valor "razonable" específico — eso sigue fuera de
  alcance, es calibración de parámetros, no corrección de motor).
- **P8 — Regresión de producción**: 124/124 (o el conteo vigente) tests de producción, 3 baselines
  congelados sin regenerar ni alterar.

---

## Fuera de alcance de este documento

No se implementa código. No se modifica `ConsumidorFifo.cs`, `ResolutorCrossZero.cs`,
`AplicadorFill.cs`, `OrderRequest.cs`, `IStrategy.cs`. No se resuelve el caso de una estrategia que
genuinamente quiera invertir posición bajo sizing activo (ninguna de las 5 estrategias actuales lo
requiere) — queda documentado como límite conocido, no como deuda técnica bloqueante.

---

## Próximo paso

Autorización de implementación bajo el alcance de este documento: extender
`ClasificadorIntencionOrden.Clasificar` (nueva firma con tupla), actualizar `GestorCapital.Ajustar`
para usar `CantidadEfectiva` y avanzar la proyección con ese valor, actualizar los 2 call sites de
prueba existentes (`ClasificadorIntencionOrdenTests.cs`, `GestorCapitalTests.cs` P9) a la nueva
firma, P1-P8 como criterio de cierre. Solo después: cierre formal de Caso 4.3 (D-093 a D-095).
