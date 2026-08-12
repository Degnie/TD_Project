# Especificación de Métricas Financieras — V1

Estado: **documento de diseño — Caso 2.4, previo a implementación**. Continúa el ciclo inventario →
decisiones → diseño → implementación → pruebas → auditoría. No modifica código en este documento.

Principio de trabajo heredado del cierre de Caso 2.3 (aplicado explícitamente en cada métrica de
este documento): *una definición económica solo está implementada cuando existe una ruta
verificable desde la configuración hasta la consecuencia económica real en el punto correcto del
motor*. Por eso la Sección 1 separa, para cada métrica candidata, si el dato base ya existe, existe
parcialmente, o no existe — antes de diseñar cualquier fórmula nueva.

Fuera de alcance (explícito): Masaniello, optimización, selección automática de estrategia,
recomendación de inversión.

---

## 1. Inventario verificado contra `src/`

### 1.1 Métricas contables

| Candidata | Estado |
|---|---|
| Capital inicial | 🟡 Existe como `ConfiguracionExperimento.CapitalInicial`, pero **no está expuesto** en `ResultadoBacktest`/`MetricsDto` — el resultado de una corrida no trae de vuelta cuál fue su capital inicial. |
| Cash final | ✅ Ya existe — `ResultadoBacktest.CashFinal`. |
| Equity final | ✅ Ya existe — `MetricsDto.EquityFinal`, calculado en `ResultDtoMapper.cs` desde `EquityCurve[^1]` con redondeo Half-to-Even (RNF-05). |
| PnL absoluto | ✅ Ya existe — `MetricsDto.PnLTotal = trades.Sum(t => t.RealizedPnL)`. |

### 1.2 Métricas de rendimiento

| Candidata | Estado |
|---|---|
| Retorno porcentual | ❌ No existe — depende de "Capital inicial" (1.1, no expuesto) y de ningún cálculo `(EquityFinal − CapitalInicial) / CapitalInicial` en ningún lado de `src/`. |
| Crecimiento del capital (curva) | ✅ Ya existe — `ResultadoBacktest.EquityCurve` (`IReadOnlyList<EquityPoint>`, un punto por vela, `Cash`/`Margin`/`UnrealizedPnL`/`Equity`). |

### 1.3 Métricas de riesgo

| Candidata | Estado |
|---|---|
| Drawdown de equity (máximo, %/monetario) | ❌ No existe en ningún lado — ni `src/` ni `exploration/`. **Distinción importante verificada**: `exploration/laboratorio/evaluacion_multi_tf/PerfilMultiTf.cs` y `analisis_operacional/AnalizadorOperacional.cs` calculan `RachaNegativaMaxima` — rachas de **operaciones perdedoras consecutivas** (conteo de operaciones), un concepto distinto de drawdown de equity (caída desde un pico de la curva). No debe confundirse ni reutilizarse como si fuera lo mismo. |
| Duración del drawdown | ❌ No existe — depende de que exista el cálculo de drawdown primero. |
| Exposición máxima | ❌ No existe el cálculo. Dato base parcial: `PortfolioSnapshot.LotesVivos[].Cantidad` por vela (`src/Application/PortfolioSnapshot.cs`) permitiría sumarlo, pero ningún módulo lo agrega hoy. |
| Operaciones rechazadas por incapacidad | 🟡 Dato base ya existe — `ResultadoBacktest.IncapacidadesEfectivas` (Caso 2.1, D-059) — pero no está mapeado a ningún DTO de presentación ni a ningún reporte del laboratorio todavía. |

### 1.4 Métricas comparativas

No hay inventario de código que verificar aquí — son agregaciones sobre las métricas de 1.1-1.3
ya calculadas por corrida, presentadas lado a lado. El riesgo no es de datos faltantes, es
metodológico (ver Sección 4).

---

## 2. Decisiones — resueltas

**Regla de numeración (confirmada tras la discrepancia detectada en la revisión de este
documento)**: un identificador `D-N` corresponde a una única decisión durante toda la vida del
proyecto — mismo principio que corrigió D-043/D-053 en Caso 1. D-072 a D-076 son las decisiones
originales de este documento; D-077/D-078 son decisiones nuevas detectadas durante la resolución,
no renumeraciones de las anteriores.

### D-072 — Capital inicial expuesto en el resultado

**Estado**: 🟢 Aprobada.

**Decisión**: el capital inicial usado en métricas financieras proviene exclusivamente de
`ConfiguracionExperimento.CapitalInicial` — no se reconstruye ni se infiere de ningún otro dato
(coherente con D-077). Se agrega como campo expuesto en el punto donde se generan reportes/
métricas (`EjecutorProtocolo`/laboratorio), no necesariamente como campo nuevo de
`ResultadoBacktest` — evita el mismo problema de compatibilidad que D-061 resolvió para otros
campos (record posicional con call sites en `tests/`). Cuando se comparan experimentos, el capital
inicial de cada uno se muestra junto a sus métricas, nunca implícito. No forma parte de la
identidad experimental (`IdentidadExperimentoCompleta`, D-049) — es un dato de reporte, no de
configuración que cambie el comportamiento del motor.

### D-073 — Definición de drawdown de equity

**Estado**: 🟢 Aprobada.

**Decisión**: drawdown porcentual, calculado sobre `EquityCurve` (fuente oficial, D-077) —
`PortfolioSnapshot`/`Trades`/rachas no participan en este cálculo.

```
PeakEquity(t) = max(Equity(0..t))
Drawdown(t)   = (PeakEquity(t) - Equity(t)) / PeakEquity(t)
DrawdownMax   = max(Drawdown(t)) para todo t en EquityCurve
```

No se calcula drawdown monetario como campo separado en V1 — el porcentual es comparable entre
corridas con distinto capital inicial, que es el caso de uso relevante (comparar estrategias/
configuraciones, D-076).

### D-074 — Duración del drawdown

**Estado**: 🟡 Definida conceptualmente. ⏳ No implementada en V1.

**Decisión**: no se implementa en V1 — mismo criterio que D-063 excluyó spread/funding: no es
necesario para responder la pregunta central de Caso 2.4 (medición financiera básica confiable).
Queda como candidata para una versión posterior.

**Definición operacional de referencia** (registrada para cuando se implemente, **no** alcance
aprobado para V1): inicio = primera vela posterior al máximo histórico donde `Equity(t) <
PeakEquity(t)`; recuperación = `Equity(t) >= PeakEquity` previo al inicio del tramo; sin
recuperación = duración hasta el final de la curva; unidad = velas (coherente con que
`EquityCurve` es una serie por vela, no por timestamp calendario uniforme).

**Aclaración explícita**: una definición aprobada no implica automáticamente una implementación
aprobada — verificado en revisión de cierre de este documento, donde se detectó el riesgo de tratar
la descripción conceptual como si fuera alcance de código. `ESPECIFICACION_METRICAS_FINANCIERAS_
IMPLEMENTACION_V1.md` no incluye DTO, cálculo, ni pruebas para esta métrica.

### D-075 — Exposición máxima

**Estado**: 🟢 Aprobada.

**Decisión**: `ExposicionMaxima = Max(PortfolioSnapshot.Margin)` sobre la serie ya existente en
`ResultadoBacktest.PortfolioSnapshots` — verificado contra código (`src/Application/
PortfolioSnapshot.cs`): `Margin` es un campo directo del record, `Max()` es una consulta LINQ sin
ningún cálculo de dominio nuevo. No se implementa la Opción A (suma de `Cantidad` de `LotesVivos`
en unidades del instrumento) — `Margin` ya es capital comprometido, coherente con D-057
(`TasaMargen` como propiedad del instrumento) y evita introducir una segunda unidad de medida de
exposición (unidades del activo vs. capital) sin necesidad demostrada.

### D-076 — Métricas comparativas

**Estado**: 🟢 Aprobada.

**Decisión**: mantiene la regla heredada de D-014/D-047 (Caso 1) — métrica financiera ≠ ranking
automático. Se muestran valores, contexto, tamaño de muestra y timeframe, lado a lado, sin
ordenamiento por ninguna columna (extiende el mismo formato que `ReporteConsolidadoGenerador` ya
usa para métricas operacionales). Nunca se genera texto de tipo "mejor estrategia"/"más rentable"/
"recomendada" — mismo patrón de nota obligatoria que D-037 exige para régimen de mercado, aplicado
aquí a resultado financiero.

### D-077 — Fuente oficial de datos financieros

**Estado**: 🟢 Aprobada.

**Decisión**: toda métrica financiera se deriva exclusivamente de los objetos ya producidos por el
motor: `EquityCurve`, `Cash`, `Margin`, `Trades`. Ninguna métrica se reconstruye desde operaciones
individuales, señales de estrategia, o cualquier dato que no sea salida directa de
`BacktestRunner`/`ResultadoBacktest`. Prohibido explícitamente como fuente secundaria: recalcular
PnL o equity a partir de `Fills` sumando manualmente precios (ya existe `RealizedPnL` por `Trade`
y `EquityCurve` — recalcularlo en otro lugar arriesga divergencia, mismo riesgo que D-062
corrigió). Aplica directamente a D-073 (drawdown desde `EquityCurve`, no desde `Trades`/rachas).

### D-078 — Tratamiento de métricas no disponibles

**Estado**: 🟢 Aprobada.

**Decisión**: cuando una métrica no tiene fuente válida para calcularse, se representa como
ausente (`null`/"no disponible") — nunca como `0`. Prohibida cualquier inferencia para rellenar el
vacío (ej. no calcular drawdown desde PnL acumulado de operaciones si no existe `EquityCurve` para
esa corrida). `0` significa "el valor calculado fue cero"; "no disponible" significa "no había con
qué calcularlo" — mezclar ambos casos falsearía comparaciones entre corridas.

---

## 3. Principio aplicado (verificado, no asumido)

Cada candidata de la Sección 1 fue clasificada en ✅/🟡/❌ por lectura directa de código, no por
intención. Ninguna decisión de la Sección 2 asume que un cálculo "debería funcionar" sin haber
confirmado primero si el dato base existe — mismo principio que el cierre de Caso 2.3 dejó
registrado tras las 3 correcciones de D-062/D-063/D-067.

---

## 4. Riesgo metodológico de las métricas comparativas

Verificado contra decisiones ya congeladas: D-014 (Caso 1, "sin ranking implícito entre
timeframes") y D-047 (Caso 1, "reporte de escenarios sin conclusión comparativa ni ranking entre
regímenes") ya establecieron el mismo principio para otras dimensiones. Caso 2.4 no introduce una
excepción nueva — extiende la misma regla a comparación entre estrategias por resultado económico.
Un reporte que muestre PnL de Estrategia A junto a Estrategia B sin ordenar ni destacar ninguna
sigue este principio; uno que las ordene de mayor a menor retorno ya sugiere una recomendación,
incluso sin decirlo explícitamente.

---

## 5. Separación dinero simulado ≠ resultado real

Diferencia explícita respecto a Caso 1: allá se evitaba interpretar resultados **operacionales**
(D-014/D-047, "no hay estrategia mejor"). Aquí existe un riesgo adicional — interpretar dinero
simulado como rendimiento financiero real. Ambos riesgos coexisten y se tratan por separado:

- **Resultado económico experimental** (lo que Caso 2.4 mide): `EquityFinal`/`PnLTotal`/
  `RetornoPct`/`DrawdownMax` calculados sobre `CapitalInicial` en unidad monetaria experimental
  (D-058, Caso 2.0) — un número interno del laboratorio, sin unidad de moneda real declarada.
- **Resultado financiero real** (lo que Caso 2.4 explícitamente NO produce): cualquier afirmación
  de que esas cifras representan dinero que se ganaría/perdería operando con capital real —
  prohibido por D-002 (Caso 1) y por las exclusiones ya congeladas en
  `VERSION_EXPERIMENTAL_CASO1_V1.md`.

Todo reporte de Caso 2.4 debe distinguir explícitamente ambos conceptos, misma disciplina que
D-058 exigió para no confundir la unidad monetaria abstracta con USDT real.

---

## Fuera de alcance de esta especificación

Masaniello, optimización, selección automática de estrategia, recomendación de inversión. Ningún
cambio de código en este documento.

---

## Próximo paso

D-072 a D-078 resueltas. Siguiente: redactar `ESPECIFICACION_METRICAS_FINANCIERAS_IMPLEMENTACION_
V1.md` — diseño de implementación concreto (tipos, punto de integración, pruebas obligatorias)
antes de tocar código, mismo patrón que Caso 2.1/2.2/2.3.
