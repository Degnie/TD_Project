# Especificación de Implementación — Sizing Corregido (Caso 4.3, D-093/D-094)

Estado: **documento de diseño implementable — previo a implementación**. Traduce D-093 (Opción A:
porcentaje sobre margen) + D-094 (`Close` de vela siguiente) a diseño concreto. No modifica código
en este documento.

---

## 1. Fórmula objetivo (ya fijada por D-093/D-094, sin reabrir)

```
CapitalDisponible = Cash − Margin                          (sin cambio, D-067)
MargenObjetivo     = CapitalDisponible × PorcentajeRiesgo   (D-093)
CantidadActivo     = MargenObjetivo / (CloseReferencia × TasaMargen)   (D-093, despejado de CalculadoraLotes)
```

Donde `CloseReferencia` es el `Close` de la vela siguiente (D-094) — el mismo precio que
`ValidadorCapacidad.Validar`/`CalculadoraReservaPreventiva.Calcular` ya usan
(`BacktestRunner.cs:60,65,67`).

---

## 2. Cómo llega `CloseReferencia` hasta `GestorCapital`

**Cambio de firma** (único diseño consistente con D-094 — el precio no existe en ningún tipo que
`GestorCapital.Ajustar` ya reciba):

```csharp
// Antes:
public static IReadOnlyList<OrderRequest> Ajustar(IReadOnlyList<OrderRequest> requests, PortfolioState portfolio, ConfiguracionSizing? sizing)

// Despues:
public static IReadOnlyList<OrderRequest> Ajustar(IReadOnlyList<OrderRequest> requests, PortfolioState portfolio, ConfiguracionSizing? sizing, decimal precioReferencia, decimal tasaMargen)
```

`tasaMargen` se agrega también como parámetro — la fórmula de la sección 1 la requiere y
`GestorCapital` no la recibe hoy (`Instrumento.TasaMargen` vive en `ConfiguracionExperimento`,
fuera del alcance actual de `Ajustar`). Mismo criterio que `ResolutorVela.Resolver` ya recibe
`tasaMargen` como parámetro explícito (`ResolutorVela.cs:15`) — no se introduce un patrón nuevo.

**Call sites a actualizar** (verificados por búsqueda exhaustiva, no asumidos):
- `src/Application/BacktestRunner.cs:52` — único call site de producción. Pasa a:
  `GestorCapital.Ajustar(requests, portfolio, config.Sizing, config.Velas[n + 1].Close,
  instrumento.TasaMargen)` — ambos valores ya calculados/disponibles en ese scope
  (`instrumento` en la línea 24, `config.Velas[n + 1]` ya indexado en la línea 60 para
  `closeSiguiente`; en la nueva versión se lee una línea antes, sin dato nuevo).
- `tests/Application.Tests/GestorCapitalTests.cs:159,185` — las 2 pruebas nuevas de 4.2 (P9, P10)
  que invocan `GestorCapital.Ajustar` directamente. Requieren agregar los 2 argumentos nuevos.

**No hay más call sites** — `GestorCapital` no se invoca desde `exploration/` ni desde ningún otro
punto de `src/`.

---

## 3. Preservación de `Sizing=null`, baselines y estrategias existentes

- **`Sizing=null`**: sin cambio de comportamiento — la guarda `if (sizing is null) return
  requests;` permanece como primera línea, antes de leer `precioReferencia`/`tasaMargen`. Los 3
  baselines congelados (Caso 1, Caso 2, Caso 3A) no invocan sizing activo, no se ven afectados por
  el cambio de fórmula.
- **Estrategias existentes**: ninguna de las 5 estrategias (Tres Mosqueteros, MHI Mayoría, EMA
  Cross, Z-Score Reversal, Neutral) requiere modificación — la fórmula nueva vive enteramente
  dentro de `GestorCapital`, invisible para `IStrategy` (P-002 preservado).
- **`BacktestRunner.cs`**: el único cambio es la línea 52 (2 argumentos nuevos en la llamada) — sin
  alterar el orden del ciclo, sin mover ninguna lectura de vela fuera de su scope actual.

---

## 4. Consecuencia declarada sobre pruebas ya congeladas de Caso 2 (`GestorCapitalTests.cs` P1-P6)

**Verificado explícitamente, no asumido**: P2
(`ConSizingActivoLaCantidadEsCapitalDisponiblePorPorcentaje`, línea 34-45 del archivo actual) y P4
(`OrdenesDeLaMismaBolsaRecibenLaMismaCantidadCalculada`, línea 62-73) verifican el **valor
numérico exacto** que produce la fórmula actual sin conversión (`Cantidad = 100m` para
`PorcentajeRiesgo=0.1`, `Cash=1000`, `Margin=0`) — con la fórmula corregida, ese valor cambia
(`CantidadActivo = (1000 × 0.1) / (Close × 0.1)`, dependiente del precio de la vela, ya no `100`
plano). **Estas 2 pruebas requieren actualizar su valor esperado**, no solo "no romperse" —
consecuencia directa e inevitable de corregir la fórmula, declarada aquí explícitamente en vez de
descubrirse como sorpresa durante la implementación.

**P1, P3, P5, P6 no requieren cambio de valor esperado** — verifican propiedades estructurales
(regresión sin sizing, dirección/tipo de orden sin cambio, determinismo, trazabilidad de hash)
independientes de la fórmula exacta de sizing.

**Esto no es una ruptura de "pruebas congeladas nunca cambian"** — el criterio de inmutabilidad
que rige en este proyecto es sobre **baselines congelados** (`baseline_final/`,
`baseline_financiero_final/`, evidencia de `caso3a-v1-experimental`), no sobre pruebas unitarias
de un componente cuya fórmula interna D-093/D-094 acaban de redefinir explícitamente. Ninguna
prueba de Caso 1 (`TestsEjecutorProtocolo.cs`) ni ningún baseline se ve afectado — todos corren
con `Sizing=null`.

---

## 5. Pruebas obligatorias antes de cerrar

- **P1 — Equivalencia histórica sin sizing**: `Sizing=null` produce exactamente el mismo resultado
  que antes del cambio de firma — mismo criterio que la P1 ya existente, sin modificación.
- **P2 (actualizada) — Fórmula corregida produce unidades de activo coherentes**: con
  `PorcentajeRiesgo=0.1`, `Cash=1000`, `Margin=0`, `Close=100`, `TasaMargen=0.1` →
  `CantidadActivo = (1000 × 0.1) / (100 × 0.1) = 10` — verificar este valor exacto, reemplazando
  el valor `100m` de la P2 original.
- **P3 — Consistencia con `CalculadoraLotes`**: aplicar el `Fill` resultante (`Cantidad=
  CantidadActivo` de P2, al mismo `Close`) y verificar que `Lote.Margin` calculado por
  `CalculadoraLotes.AbrirLote` coincide con el `MargenObjetivo` original (`CapitalDisponible ×
  PorcentajeRiesgo`) — prueba de cierre del círculo dimensional completo (la razón de ser de
  D-093/D-094): el margen que efectivamente se retiene debe coincidir con el margen que se
  pretendía comprometer.
- **P4 (actualizada) — Bolsa completa, misma cantidad**: mismo criterio que la P4 original
  (2 `OrderRequest` en la misma bolsa reciben la misma `Cantidad`), con el valor recalculado según
  la fórmula corregida.
- **P5/P6 — Sin cambio**: determinismo y trazabilidad de hash, sin modificación respecto a las
  pruebas actuales.
- **P7 — No regresión de D-084**: repetir la verificación de bolsa multi-orden con reversión
  (`ESPECIFICACION_INTEGRACION_GESTOR_CAPITAL_V1.md` P9, ya implementada) bajo la fórmula nueva —
  confirmar que `ReduccionParcial`/`CierreTotal`/`CrossZero` siguen conservando `Cantidad`
  original (el cambio de fórmula solo afecta `Apertura`/`Aumento`, no la lógica de clasificación
  de 4.1/4.2).
- **P8 — Corrida larga sin residuos, con fórmula corregida**: repetir la verificación P8 de 4.2
  (Tres Mosqueteros, dataset 1m real, `PorcentajeRiesgo` recalibrado si es necesario dado que la
  fórmula cambia de escala) — confirmar que termina en tiempo razonable y que `CashFinal`/
  `EquityFinal` ya no muestran la desproporción extrema documentada en D-085 (aunque no se exige
  un valor "razonable" específico — eso excede el alcance dimensional de esta sub-fase, ver
  sección 6).
- **P9 — Regresión de producción**: 122/122 (o el conteo vigente) tests de producción, 3 baselines
  congelados sin regenerar ni alterar.

---

## 6. Qué NO resuelve esta sub-fase (fuera de alcance, declarado explícitamente)

- **No garantiza que `CashFinal` sea "razonable"** — corrige la *unidad* de `Cantidad`, no
  recalibra `PorcentajeRiesgo`/`CapitalInicial` a valores que produzcan resultados
  interpretables. Calibrar esos valores sigue siendo responsabilidad del experimento (D-030), no
  del motor.
- **No modifica `ValidadorCapacidad.cs`/`CalculadoraReservaPreventiva.cs`** — ambos ya usan
  `request.Cantidad` (ahora dimensionalmente correcta gracias a esta sub-fase) y `closeSiguiente`
  (ya la misma referencia que D-094 fijó) — se benefician de la corrección sin requerir cambio de
  código propio.
- **No modifica `Instrumento.cs`/`OrderRequest.cs`** — el precio de referencia viaja como
  parámetro de `GestorCapital.Ajustar`, no como campo nuevo de ningún tipo existente.
- **No resuelve Cross-Zero bajo sizing** — sigue excluido de sizing por diseño de 4.2
  (`ESPECIFICACION_INTEGRACION_GESTOR_CAPITAL_V1.md` §3/§6), sin cambios en esta sub-fase.

---

## Fuera de alcance de este documento

No se implementa código. No se modifica `ValidadorCapacidad.cs`, `Instrumento.cs`,
`OrderRequest.cs`, `AplicadorFill.cs`. No se recalibra `CapitalInicial` ni ningún parámetro de
estrategia histórica.

---

## Próximo paso

Autorización de implementación bajo el alcance de este documento: modificar
`src/Domain/Portfolio/GestorCapital.cs` (nueva firma + fórmula), `src/Application/
BacktestRunner.cs:52` (único call site de producción), actualizar P2/P4 en
`tests/Application.Tests/GestorCapitalTests.cs`, agregar P3/P7/P8/P9 nuevas. Tras cerrar, D-085
queda resuelta dentro del alcance declarado (corrección dimensional de sizing) — la pregunta de si
queda deuda técnica residual (calibración de valores "razonables") se documenta en el cierre, no
se resuelve.
