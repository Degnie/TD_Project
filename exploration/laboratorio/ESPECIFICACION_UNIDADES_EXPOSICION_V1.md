# Especificación — Unidades y Exposición (Caso 4.3, D-085)

Estado: **documento de diseño — previo a cualquier decisión o implementación**. Resuelve la
pregunta que 4.1/4.2 dejaron explícitamente pendiente: ¿qué representa económicamente
`Cantidad`, y cómo se relaciona con `CapitalInicial`? No modifica código en este documento. No se
toca `ValidadorCapacidad` — su corrección depende de que esta definición quede resuelta primero.

---

## 1. Inventario dimensional — verificado en código, no reconstruido de memoria

| Campo | Tipo | Unidad real (verificada) | Dónde se calcula |
|---|---|---|---|
| `OrderRequest.Cantidad` / `Fill.Cantidad` | `decimal` | Unidades del activo (ej. BTC) | Emitido por `IStrategy.Observar`; nunca transformado de unidad, solo de magnitud |
| `Instrumento.TasaMargen` | `decimal` | Fracción adimensional (ej. `0.1` = 10%) | `Instrumento.Default = ("N/A", 0.1m)`, D-057 |
| `Lote.Margin` | `decimal` | Unidad monetaria abstracta (D-058) | `CalculadoraLotes.AbrirLote:13` — `Margin = \|Cantidad\| × PrecioFill × TasaMargen` |
| `PortfolioState.Cash` / `.Margin` | `decimal` | Unidad monetaria abstracta (D-058) | Acumulado por `AplicadorFill` sobre cada `Fill` |
| `Equity` | `decimal` | Unidad monetaria abstracta (D-058) | `Cash + Margin + UnrealizedPnL` (`ResolutorVela.CalcularEquity:118-119`) |
| `MetricasFinancieras.ExposicionMaxima` | `decimal` | Unidad monetaria abstracta | `Max(PortfolioSnapshot.Margin)`, D-075 |
| `ConfiguracionSizing.PorcentajeRiesgo` | `decimal` | Fracción adimensional | `GestorCapital.Ajustar:23-24` — `cantidadCalculada = (Cash − Margin) × PorcentajeRiesgo` |

**Hallazgo central, ya identificado en D-085 original y reconfirmado aquí con la ecuación
completa**: `cantidadCalculada` de `GestorCapital` (línea 24) tiene la unidad de **`Cash`**
(monetaria abstracta), porque `(Cash − Margin) × PorcentajeRiesgo` es monetario × adimensional =
monetario. Pero esa `cantidadCalculada` se asigna directamente a `OrderRequest.Cantidad`
(`GestorCapital.cs:38`, sección 3 de `ESPECIFICACION_INTEGRACION_GESTOR_CAPITAL_V1.md`), cuya
unidad real es **unidades del activo**, no monetaria. **`GestorCapital` mezcla dos unidades
distintas en la misma variable sin conversión** — este es el defecto dimensional exacto que D-085
documentó desde Caso 2, ahora localizado en la línea precisa donde ocurre, no solo observado en el
resultado final (`CashFinal` absurdo).

**Evidencia de la verificación P8 de 4.2** (corrida con `PorcentajeRiesgo=0.000002`, dataset 1m,
BTCUSDT ≈ 90,000): `cantidadCalculada ≈ (1000 − Margin) × 0.000002`, un número del orden de
`0.002` **unidades monetarias** — asignado directamente como si fueran **BTC**. El resultado
(`CashFinal ≈ -32.7M`) es la consecuencia acumulada de esa mezcla de unidades sobre miles de
operaciones, no un error de escala del `PorcentajeRiesgo` en sí (ya descartado por D-083, que lo
calibró dimensionalmente sin resolver el problema de fondo).

---

## 2. Definición dimensional propuesta (para discusión, no decidida aquí)

**Cantidad de activo** (`OrderRequest.Cantidad`): unidades del activo subyacente (BTC). Esta es la
unidad "nativa" de una orden — lo que la estrategia decide comprar/vender.

**Exposición nominal** de una posición: `|Cantidad| × PrecioFill` — unidad monetaria abstracta, el
valor de mercado total de la posición, independiente de margen.

**Margen requerido**: `Exposición nominal × TasaMargen` — unidad monetaria abstracta, el colateral
retenido (`Lote.Margin`, ya implementado así, sin cambios).

**Capital disponible para sizing**: `Cash − Margin` — unidad monetaria abstracta (ya implementado,
sin cambios).

**La conversión que falta**: para que `GestorCapital` produzca una `Cantidad` en unidades de
activo a partir de un `capitalDisponible` en unidades monetarias, la fórmula dimensionalmente
correcta requiere dividir por un precio:

```
CantidadActivo = (CapitalDisponible × PorcentajeRiesgo) / (PrecioReferencia × TasaMargen)
```

Esto no es una fórmula nueva inventada aquí — es la despejada de `Margin = Cantidad × Precio ×
TasaMargen` (la ecuación que `CalculadoraLotes` ya usa, sección 1), resuelta para `Cantidad` en
vez de para `Margin`. `GestorCapital` no tiene hoy acceso a un `PrecioReferencia` en el punto donde
corre (`Ajustar` no recibe el precio de la vela, solo `PortfolioState`/`ConfiguracionSizing`) — su
firma tendría que extenderse si esta es la dirección elegida.

---

## 3. Relación con sizing — qué significa `PorcentajeRiesgo` hoy vs. qué debería significar

**Hoy** (código actual, sin cambios de 4.1/4.2 en esta fórmula): `PorcentajeRiesgo` se interpreta
como "fracción del capital disponible que se convierte directamente en la cantidad de la orden" —
dimensionalmente inconsistente, según el hallazgo de la sección 1.

**Alternativa dimensionalmente correcta**: `PorcentajeRiesgo` como "fracción del capital
disponible que se arriesga como margen de la nueva posición" — consistente con el nombre
("riesgo"), y resoluble con la fórmula de la sección 2 si se agrega un precio de referencia.

**No se decide aquí cuál interpretación es correcta** — es la primera pregunta que la
especificación de implementación (documento siguiente) debe resolver explícitamente, con opciones
y criterio, mismo patrón que toda decisión de este proyecto.

---

## 4. Restricciones de compatibilidad

- **Ninguna estrategia histórica cambia**: Tres Mosqueteros, MHI Mayoría, EMA Cross (Cantidad=1
  fija) no requieren modificación de código bajo ninguna opción de esta especificación.
- **`Sizing=null` preserva el comportamiento exacto**: los 3 baselines congelados (Caso 1, Caso 2,
  Caso 3A) no invocan `GestorCapital` con sizing activo — ninguna corrección de la fórmula de
  sizing los afecta, sin necesidad de guardas adicionales más allá del `if (sizing is null) return
  requests;` ya existente.
- **`CapitalInicial=1000` no se recalibra**: mismo principio ya fijado en D-085 original — el
  problema se resuelve hacia adelante (fórmula correcta para sizing futuro), no reinterpretando
  valores ya congelados.
- **Ninguna estrategia conoce el precio de referencia por decisión propia**: si la fórmula de la
  sección 2 requiere un precio, ese precio viene del ciclo del motor (`Close` de la vela ya
  disponible en `BacktestRunner`), nunca de la `IStrategy` — preserva P-002.

---

## 5. Qué NO se decide en este documento

- No se decide si `PorcentajeRiesgo` significa "fracción de capital → cantidad" o "fracción de
  capital → margen" (sección 3).
- No se decide la firma exacta de `GestorCapital.Ajustar` si necesita recibir un precio (cambio de
  contrato, requiere su propia decisión con evidencia de impacto en `BacktestRunner.cs`).
- No se modifica `ValidadorCapacidad.cs`/`CalculadoraReservaPreventiva.cs` — dependen de que esta
  definición quede resuelta primero (correctamente identificado en tu mensaje: su corrección debe
  esperar).
- No se resuelve si esto requiere solo documentación (formalizar lo que ya existe con una
  advertencia más precisa que D-085 original) o cambios de motor (corregir la fórmula de
  `GestorCapital`) — es la decisión central que el documento de decisiones siguiente debe tomar,
  con opciones explícitas.

---

## 6. Opciones de alcance para D-085 (a decidir en el próximo documento, presentadas aquí sin selección)

- **Opción A — Documentación solamente**: formalizar la advertencia dimensional (ya existe una
  versión general en el reporte financiero, D-085 original) con precisión matemática exacta —
  cero cambio de código, `GestorCapital` sigue mezclando unidades, pero el reporte lo declara sin
  ambigüedad. Riesgo: sizing activo sigue siendo dimensionalmente incorrecto en cualquier corrida
  futura, solo mejor documentado.
- **Opción B — Nuevos tipos, sin cambiar la fórmula existente**: introducir tipos que hagan
  explícita la unidad (ej. un wrapper `CantidadActivo`/`CantidadMonetaria` distintos por tipo) para
  que un error de mezcla de unidades sea un error de compilación futuro — no corrige el defecto
  actual de `GestorCapital`, pero previene que se repita en código nuevo.
- **Opción C — Corregir la fórmula de sizing (cambio de motor)**: implementar la conversión de la
  sección 2, extendiendo la firma de `GestorCapital.Ajustar` para recibir un precio de referencia
  — corrige el defecto de raíz, mismo criterio de "causa raíz, no parche" ya aplicado en 4.1/4.2,
  pero con mayor superficie de cambio (firma pública modificada, todos los call sites de
  `Ajustar` — hoy solo `BacktestRunner.cs:52` — deben actualizarse).

---

## Fuera de alcance de este documento

No se implementa código. No se modifica `GestorCapital.cs`, `ValidadorCapacidad.cs`,
`CalculadoraReservaPreventiva.cs`. No se selecciona ninguna opción de la sección 6 — corresponde
al documento de decisiones siguiente. No se recalibra `CapitalInicial` ni ninguna estrategia
histórica.

---

## Próximo paso

`DECISIONES_CASO4_3_V1.md` (o extensión de `DECISIONES_CASO4_V1.md` con D-094+) — selección entre
las opciones de la sección 6, y si se elige B o C, definición de la interpretación de
`PorcentajeRiesgo` (sección 3) antes de cualquier cambio de código.
