# Auditoría de Cierre — Caso 4.3: Unidades, Exposición y Sizing Dimensional

Estado: **documento de cierre de sub-fase — Caso 4.3 completo**. Consolida evidencia verificada
del ciclo especificación → decisión → implementación → pruebas → auditoría para D-093, D-094 y
D-095, y cierra D-085 (deuda técnica heredada de Caso 2). Mismo patrón que las auditorías de
cierre de sub-fase de Caso 2/Caso 3A (`AUDITORIA_CASO_2_3_GESTION_CAPITAL_V1.md`,
`caso3/AUDITORIA_CASO3A_V1.md`).

---

## 1. Alcance auditado

Documentos de origen: `ESPECIFICACION_UNIDADES_EXPOSICION_V1.md`, `DECISIONES_UNIDADES_
EXPOSICION_CASO4_V1.md` (D-093 a D-095), `ESPECIFICACION_IMPLEMENTACION_SIZING_CORREGIDO_V1.md`,
`ESPECIFICACION_NORMALIZACION_CIERRES_SIZING_V1.md`. Implementación:
`src/Domain/Portfolio/GestorCapital.cs`, `src/Domain/Portfolio/ClasificadorIntencionOrden.cs`,
`src/Application/BacktestRunner.cs`, `tests/Application.Tests/GestorCapitalTests.cs`,
`tests/Domain.Tests/Portfolio/ClasificadorIntencionOrdenTests.cs`.

---

## 2. Problema inicial — D-085

**Causa raíz original** (Caso 2, `DECISIONES_MODELO_ECONOMICO_V1.md`): las estrategias fijan
`Cantidad` sin relación dimensional con `CapitalInicial` — `Margin ≈ Cantidad × PrecioFill ×
TasaMargen` producía valores desproporcionados frente al capital (`Margin ≈ 9,000` vs.
`CapitalInicial=1000`), visible solo al calcular `MetricasFinancieras` en el baseline de Caso 2.
Registrada como deuda técnica, explícitamente fuera de alcance de Caso 2 V1.

**Reformulación durante Caso 4.3** (`ESPECIFICACION_UNIDADES_EXPOSICION_V1.md` §1): verificado en
código que el defecto es más preciso de lo que D-085 original describía — `GestorCapital.Ajustar`
mezclaba unidad monetaria (`Cash − Margin`, resultado de `CapitalDisponible × PorcentajeRiesgo`)
con unidad de activo (`OrderRequest.Cantidad`) sin ninguna conversión, asignando directamente un
valor monetario como si fueran unidades de BTC.

---

## 3. Hallazgos D-093/D-094

**D-093 — Significado de `PorcentajeRiesgo`**: resuelto como "fracción del capital disponible
comprometida como margen objetivo" (Opción A), despejando `CantidadActivo` de la misma ecuación
que `CalculadoraLotes.AbrirLote` ya usa (`Margin = Cantidad × PrecioFill × TasaMargen`) — sin
introducir un concepto económico nuevo, solo invirtiendo una ecuación ya congelada.

**D-094 — Fuente de precio de referencia**: resuelto como `Close` de la vela siguiente, la misma
referencia que `ValidadorCapacidad`/`CalculadoraReservaPreventiva` ya usan para el mismo propósito
conceptual (estimar magnitud económica antes del `Fill` real) — evita una segunda noción de
"precio de referencia" dentro del mismo ciclo del motor. Precio estimado de fill descartado por
contradicción causal (`Cantidad → Fill`, no al revés) verificada contra el orden real de
`BacktestRunner.cs`.

**Fórmula resultante**: `CantidadActivo = (CapitalDisponible × PorcentajeRiesgo) /
(CloseReferencia × TasaMargen)`, con `CapitalDisponible = Cash − Margin` (D-067, sin cambio).

---

## 4. Integración D-095

**Hallazgo, no anticipado en la especificación original**: verificación P8 de la fórmula
D-093/D-094 mostró `CashFinal` desproporcionado incluso en un dataset corto — diagnóstico aislado
reveló que una estrategia histórica con `Cantidad` nominal fija (ej. `Sell 1m` para "cerrar") casi
nunca coincide con la posición real que sizing dejó abierta (ej. `Long 0.011111`). El clasificador
de 4.1 identificaba correctamente esto como `CrossZero` (dado que la cantidad solicitada excedía
la posición) — pero ese Cross-Zero era espurio: síntoma del defecto, no intención real de
inversión.

**Resolución**: la intención de reducción/cierre prevalece sobre la cantidad nominal — se
normaliza contra la posición real. Implementado extendiendo `ClasificadorIntencionOrden.Clasificar`
para retornar `CantidadEfectiva` junto a la intención; `GestorCapital` reinterpreta un `CrossZero`
como `CierreTotal` normalizado únicamente cuando sizing está activo. El clasificador en sí no
cambió su criterio de clasificación (`CrossZero` sigue siendo un resultado posible sin normalizar)
— preserva su condición de consulta pura sin conocer configuración de sizing.

---

## 5. Evidencia de pruebas

**126/126 tests de producción**: 62 Domain.Tests (incluyendo 11 de `ClasificadorIntencionOrdenTests`
actualizadas a la nueva firma de tupla), 4 Contracts.Tests, 2 Infrastructure.Tests, 18 Api.Tests,
40 Application.Tests (incluyendo P2/P4 de `GestorCapitalTests` recalculadas al contrato
dimensional corregido, P7-P10 de 4.2, P3/P7/nuevas de 4.3, y 2 pruebas nuevas de normalización).

**3 criterios de aceptación de D-095 verificados con evidencia directa** (más allá de las pruebas
unitarias, contra el escenario exacto planteado por el auditor):
1. `Long 0.011111` (posición real bajo sizing) + `Sell 1 BTC` (cantidad nominal histórica) →
   `CantidadEjecutada = 0.011111` exacto, posición final `= 0` — cierre total limpio, sin short
   residual.
2. `Long 10` + `Sell 15` bajo `Sizing=null` → conserva `Cantidad=15` sin normalizar, Cross-Zero
   genuino preservado: posición final `Short 5`, `TradeCerrado` no nulo.
3. `Sizing=null` → `GestorCapital.Ajustar` retorna la orden intacta, sin invocar el clasificador.

**Corrida larga de verificación** (Tres Mosqueteros, dataset 1D+1m real, sizing activo,
`PorcentajeRiesgo=0.1`): ambos timeframes `Success`, `CashFinal ≈ 577` (1D) / `≈ 0` (1m),
`ExposicionMaxima ≈ 100` — coherente con `MargenObjetivo=100` esperado (`10% × CapitalInicial`),
sin la desproporción de millones del hallazgo original, sin colgarse (9.7s total).

---

## 6. Confirmación de no regresión

- **107+ tests de producción previos**: sin cambio de comportamiento — 126/126 pasando incluye
  todo el conjunto histórico.
- **3 baselines congelados** (Caso 1 `baseline_final/`, Caso 2 `baseline_financiero_final/`, Caso
  3A `caso3a-v1-experimental`): `git status --porcelain` vacío sobre las 3 rutas en todo el ciclo
  de Caso 4.3 — ninguno regenerado ni alterado, todos corren con `Sizing=null`.
- **`IStrategy` y las 5 estrategias existentes**: sin ningún cambio de código.
- **`AplicadorFill.cs`, `ResolutorCrossZero.cs`, `ConsumidorFifo.cs`, `OrderRequest.cs`,
  `ValidadorCapacidad.cs`, `Instrumento.cs`**: sin ningún cambio — verificado explícitamente,
  ninguna de las restricciones impuestas por el auditor fue relajada durante la implementación.
- **Cross-Zero genuino bajo `Sizing=null`** (`EstrategiaCrossZeroControlada`,
  `EstrategiaFixtureCrossZero`): comportamiento idéntico al previo a D-095 — verificado por
  criterio de aceptación 2.

---

## 7. Decisiones activadas por esta sub-fase

**D-044**: no activada — ninguna parte de 4.3 estudia interacción estrategia/régimen.

**D-055**: no activada — 4.3 no toca el catálogo de métricas de martingala.

**Ninguna decisión nueva más allá de D-093/D-094/D-095** se abre en este documento.

---

## 8. Estado final — Decisiones de Caso 4

| Decisión | Estado |
|---|---|
| D-084 | ✅ Resuelta (4.2) |
| D-085 | ✅ Resuelta (4.3) |
| D-091 | ✅ Arquitectura híbrida (corrección en `src/` + activación explícita) |
| D-092 | ✅ Intención de orden (`ClasificadorIntencionOrden`) |
| D-093 | ✅ Porcentaje sobre margen requerido |
| D-094 | ✅ Precio de referencia (`Close` de vela siguiente) |
| D-095 | ✅ Normalización de cierres bajo sizing |

**Ninguna deuda técnica bloqueante queda abierta dentro del alcance ya cubierto por Caso 4.1-4.3.**

---

## Fuera de alcance de este documento

No se decide todavía si Caso 4 continúa hacia `ValidadorCapacidad` (observación vs. bloqueo
económico — último punto previsto en `PROPUESTA_CASO4_V1.md` §6, sub-fase 4.4) ni si Caso 4 se
declara formalmente congelado como versión experimental. No se recalibra ningún parámetro de
estrategia ni `CapitalInicial`.

---

## Criterio de cierre de esta sub-fase

- ✓ D-093/D-094: fórmula dimensional corregida, implementada y verificada.
- ✓ D-095: normalización de cierres implementada, 3 criterios de aceptación verificados con
  evidencia directa (no solo pruebas unitarias).
- ✓ D-085: resuelta — causa raíz corregida de punta a punta (unidad de sizing + cierre bajo
  sizing), límite declarado explícitamente (no es calibración de valores "razonables").
- ✓ 126/126 pruebas de producción + 3 baselines congelados intactos + Cross-Zero genuino
  preservado bajo `Sizing=null`.
- ✓ Ninguna restricción de alcance relajada: `IStrategy`, estrategias, `AplicadorFill`,
  `ResolutorCrossZero`, `ConsumidorFifo`, `ValidadorCapacidad` sin modificación.
- ⏳ Auditoría revisa este documento — pendiente de confirmación antes de decidir si se abre 4.4
  (`ValidadorCapacidad`) o se congela Caso 4 en su alcance actual.
