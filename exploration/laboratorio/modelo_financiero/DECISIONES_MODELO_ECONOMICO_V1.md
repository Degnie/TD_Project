# Decisiones del Modelo Económico — V1

Estado: **documento de decisión — Caso 2.0, previo a implementación**. Presenta D-057/D-058/D-059
con la misma estructura usada en D-001 a D-055 (decisión, opciones, criterio, impacto, evidencia).
No resuelve ninguna — cada una espera selección explícita del auditor antes de tocar `src/`.

Contexto completo, código verificado y principios (P-001/P-002/P-003) en
`ESPECIFICACION_MODELO_ECONOMICO_V1.md`.

---

## D-057 — Naturaleza de `TasaMargen`

**Decisión**: ¿a qué dominio pertenece el parámetro `tasaMargen` usado para calcular `Margin`?

**Evidencia**: `Margin = |Cantidad| * PrecioFill * TasaMargen`
(`src/Domain/Portfolio/CalculadoraLotes.cs`). El único valor usado hoy es `0.1m`, fijado como
default parameter de `AplicadorFill.Aplicar` (`src/Domain/Portfolio/AplicadorFill.cs:13`) — no
existe en `ConfiguracionExperimento`, no está documentado por qué es `0.1m` ni de dónde proviene.

**Opciones**:
- **A — Instrumento**: BTCUSDT (u otro activo) define su propia regla de margen. Requeriría un
  catálogo de instrumentos con `TasaMargen` propia.
- **B — Broker / mercado simulado**: la condición depende del entorno de ejecución simulado, no del
  activo — un solo valor por "tipo de broker/mercado" que se está modelando.
- **C — Experimento**: el investigador la varía como parámetro explícito de
  `ConfiguracionExperimento`, igual que `CapitalInicial`.
- **D — Mantener default temporal**, documentado explícitamente como placeholder no resuelto (sin
  mover el valor de sitio, solo etiquetarlo como pendiente).

**Impacto de cada opción**:
- A: requiere nuevo concepto (catálogo de instrumentos), mayor alcance.
- B: requiere nuevo concepto (perfil de broker/mercado), alcance medio.
- C: cambio mínimo — agregar campo a `ConfiguracionExperimento`, sin nuevo concepto de dominio.
- D: cero cambio de código, solo documentación — no resuelve el problema, lo pospone con
  trazabilidad.

**Criterio a aplicar**: la opción elegida no debe alterar el cálculo de `Margin` en sí (congelado,
P-001) — solo decide el origen/procedencia del número.

---

## D-058 — Unidad económica

**Decisión**: ¿qué representa semánticamente el `decimal` que ya circula como Cash/Margin/Equity/PnL?

**Evidencia**: no existe ningún tipo `Money`/value object en `src/Domain` — confirmado por búsqueda
exhaustiva. Todo el dinero es `decimal` crudo, sin símbolo de moneda ni unidad declarada en ningún
punto de la cadena (`ConfiguracionExperimento.CapitalInicial` → `PortfolioState.Cash` →
`EquityPoint` → `MetricsDto`).

**Opciones**:
- **A — `decimal` = USDT**: intuitivo para quien lee un reporte, pero asume implícitamente que el
  dataset (BTCUSDT) y el modelo representan un mercado real — riesgo de que un lector interprete
  cifras como dinero real utilizable.
- **B — `decimal` = unidad monetaria abstracta**: neutral, evita la asunción de mercado real, pero
  exige que todo reporte futuro aclare explícitamente "esto no es USDT real".

**Impacto de cada opción**:
- A: sin cambio de código — es una decisión de interpretación/documentación, ya que el dataset es
  BTCUSDT real. Riesgo de confusión si se difunde sin la salvedad de D-002 (fuera de alcance
  financiero real).
- B: requiere revisar el lenguaje de todos los reportes/fichas para no decir "USDT" en ningún lado.

**Criterio a aplicar**: coherencia con las exclusiones ya congeladas en Caso 1
(`VERSION_EXPERIMENTAL_CASO1_V1.md` — "sin rentabilidad real", "sin recomendación de inversión") —
la unidad elegida no debe sugerir que el modelo es apto para decisiones de dinero real.

---

## D-059 — Aplicación de restricciones de capacidad

**Decisión**: ¿el backtest financiero debe permitir estados de capital imposibles (y reportarlos) o
bloquear operaciones sin capacidad suficiente?

**Evidencia**: `ValidadorCapacidad.Validar` y `CalculadoraReservaPreventiva.Calcular`
(`src/Domain/Broker/`) implementan RN-12 Fase 1 completo: `CashDisponiblePrevio = Cash -
compromisosVigentes`, aprobado si `CashDisponiblePrevio >= reserva`. Verificado por grep: **cero
call sites** desde `BacktestRunner.cs` ni ningún otro punto de producción — el código existe,
compila, pero no está conectado. Hoy `Cash` puede volverse negativo sin que nada lo impida.

**Opciones**:
- **A — Modelo contable**: permitir que el backtest ejecute órdenes sin validar capacidad, y
  reportar (marcar) los puntos donde `Cash` cruzó a negativo o donde una orden excedió la reserva
  disponible. El sistema calcula el estado, no impone si era "posible".
- **B — Modelo operativo**: integrar `ValidadorCapacidad` al flujo de `BacktestRunner`, rechazando
  la orden (sin generar `Fill`) si `CashDisponiblePrevio < reserva`.

**Impacto de cada opción**:
- A: cambio mínimo — no toca el flujo de ejecución, solo agrega una marca/reporte posterior sobre
  la curva de Equity ya generada. No cambia el comportamiento del motor.
- B: cambio estructural — `BacktestRunner`/`MatchingEngine` deben consultar `ValidadorCapacidad`
  antes de producir un `Fill`, lo que puede alterar el número de operaciones completadas de
  cualquier estrategia ya evaluada en Caso 1 (riesgo de romper comparabilidad con
  `baseline_final/`).

**Criterio a aplicar**: dado que Caso 1 ya congeló un baseline (`caso1-v1-experimental`) construido
sin esta validación, la opción B tiene el riesgo adicional de invalidar comparabilidad retroactiva
con esa evidencia — debe evaluarse explícitamente si esto es aceptable o si amerita una nueva
versión experimental separada.

---

## D-060 — Momento de evaluación de capacidad

**Estado**: 🟢 Aprobada. **Selección: A — Evaluar antes de aplicar la orden (al momento del
`OrderRequest`, no retrospectivamente sobre el `Fill`).**

**Decisión**: ¿en qué punto del ciclo se invoca `ValidadorCapacidad.Validar` — antes de que la
orden se registre/ejecute, o después de que el `Fill` ya ocurrió?

**Opciones consideradas**:
- **A — Antes de aplicar la orden**: `OrderRequest` → `ValidadorCapacidad` → (registro si
  insuficiente) → `Fill` aplicado igual (D-059: solo observar, nunca bloquear).
- **B — Después del Fill**: validación retrospectiva sobre el resultado ya aplicado.

**Motivo de la selección**: la capacidad económica debe evaluarse en el momento donde la operación
intenta existir, no después de que sus consecuencias ya se aplicaron — evaluar sobre el `Fill` ya
resuelto describiría un hecho consumado, no la condición de capacidad que existía cuando la orden
se originó.

**Punto de integración verificado en código**: `src/Application/BacktestRunner.cs`, dentro del
`foreach (var request in requests)` (líneas 52-57) — ahí es donde cada `OrderRequest` aprobado por
`ValidadorBolsaRequests` (RN-14) se convierte en `Order` y se agrega a `ordenesActivas`, antes de
que `ResolutorVela.Resolver` (línea 62) produzca los `Fill` de ese ciclo. La evaluación de
`ValidadorCapacidad` se inserta en ese mismo `foreach`, por cada `request` individual.

**Consecuencia — conserva D-059**: la evaluación de capacidad no interrumpe el flujo — la orden se
registra en `ordenesActivas` y se resuelve normalmente sin importar el resultado de
`ValidadorCapacidad.Validar`. Solo se agrega una entrada a `Incapacidades` cuando la validación es
negativa. Flujo resultante:

```
OrderRequest
    ↓
Evaluación de capacidad (ValidadorCapacidad.Validar)
    ↓
Registro de Incapacidad si CashDisponiblePrevio < reserva (no bloquea)
    ↓
Orden registrada y resuelta normalmente (Fill aplicado igual, D-059)
```

---

## Fuera de alcance de este documento

No se resuelve ninguna decisión aquí. No se modifica código. No se abre sizing, Masaniello, ni
métricas financieras — conforme al orden fijado en `ESPECIFICACION_MODELO_ECONOMICO_V1.md` §4.

---

## D-061 — Compatibilidad de `ConfiguracionExperimento`/`ResultadoBacktest`

**Estado**: 🟢 Aprobada. **Selección: parámetro opcional con default equivalente al comportamiento
histórico**, en lugar de parámetro obligatorio.

**Decisión**: `ConfiguracionExperimento` y `ResultadoBacktest` son records posicionales con 20 y 3
call sites respectivamente, varios dentro de `tests/` (motor congelado, fuera del alcance
autorizado a modificar en esta fase). `Instrumento` se agrega como `Instrumento? Instrumento =
null` con propiedad `InstrumentoEfectivo` (`?? Instrumento.Default`); `Incapacidades` se agrega
como `IReadOnlyList<RegistroIncapacidad>? Incapacidades = null` con propiedad
`IncapacidadesEfectivas` (`?? Array.Empty<...>()`).

**Motivo**: permitir evolución incremental del modelo económico sin romper consumidores existentes
ni modificar tests congelados durante esta fase (P-001). El default debe tener una única fuente —
`Instrumento.Default` (`src/Domain/Shared/Instrumento.cs`) — para no duplicar el valor histórico
`0.1m` en más de un lugar.

## D-062 — Propagación de parámetros económicos al resolutor

**Estado**: 🟢 Aprobada. **Selección: propagar `tasaMargen` a `ResolutorVela.Resolver`/
`ResolverOco`**, con default `0.1m` para compatibilidad (mismo criterio D-061).

**Decisión**: los componentes que calculan la trayectoria económica oficial (`ResolutorVela`,
RN-11 — comparación de trayectorias A/B por Equity mínimo) deben recibir explícitamente los
parámetros económicos definidos por `Instrumento`, no solo los componentes que calculan el estado
final (`AplicadorFill` desde `BacktestRunner`).

**Evidencia del problema**: detectado por el test P2 (cambiar `TasaMargen`, esperar que `Equity`
cambie) — falló porque `ResolutorVela.Resolver`/`ResolverOco` (`src/Domain/VelaResolution/
ResolutorVela.cs`) calculan `EquityFinal`/`MarginFinal`/`CashFinal` sobre `PortfolioState`
clonados, llamando internamente a `AplicadorFill.Aplicar` sin pasar `tasaMargen` (usaba siempre el
default `0.1m`). Esta es una ruta de cálculo paralela e independiente de la que `BacktestRunner`
usa para `Trades`/`CashFinal` (que sí recibía `instrumento.TasaMargen`) — sin corregirlo,
`EquityCurve` habría quedado permanentemente fija en `0.1m` sin importar el instrumento
configurado, mientras el resto del resultado sí cambiaba: dos modelos económicos divergentes
dentro del mismo experimento.

**Motivo de la selección**: D-057 no solo dice "guardar `TasaMargen` en `Instrumento`" — implica
que el modelo económico completo debe obtener sus parámetros desde el dominio correcto. Dejar una
capa crítica (la que decide la trayectoria oficial, RN-11) ignorando el instrumento habría dejado
D-057 incompleta.

**Impacto verificado**: el cambio (agregar `tasaMargen` como parámetro y reenviarlo a
`AplicadorFill.Aplicar` dentro de `ResolverRama`/`ResolverRamaOco`) no modifica FIFO, Cross-Zero,
cálculo de `RealizedPnL`, reglas de posición, resolución OCO ni el algoritmo RN-11 en sí — es
propagación de configuración, no modificación del modelo de posiciones. No contradice P-001.

**Consecuencia — no ocultar el instrumento nuevamente**: flujo verificado end-to-end:
`ConfiguracionExperimento` → `Instrumento` → `BacktestRunner` → `ResolutorVela` → `AplicadorFill`
→ `PortfolioState`. Confirmado por la nueva prueba de consistencia interna (P3): el último
`EquityPoint` de la curva coincide exactamente con `CashFinal` y con `Cash + Margin +
UnrealizedPnL = Equity` calculado sobre el mismo instrumento.

---

## D-065 — Aplicación del coste al Cash

**Estado**: 🟢 Aprobada. **Selección: A — el coste modifica Cash/Equity.**

**Decisión**: `AplicadorFill.Aplicar` debe descontar el coste de `Cash`, no solo transportarlo
como dato informativo en el `Fill`.

**Evidencia**: `Fill.CostoFriccionReal` existe y se transporta hasta `FillLogEntryDto`, pero
`AplicadorFill.Aplicar` nunca lo lee (cero referencias, confirmado por búsqueda exhaustiva en
`ESPECIFICACION_MODELO_COSTES_V1.md` §1) — mismo patrón de riesgo que motivó D-062 (un valor
existente que no llega a la ruta que produce la consecuencia económica).

**Motivo de la selección**: Caso 2 busca un modelo financiero interpretable — un coste de
ejecución es una consecuencia económica, no solo un dato descriptivo. Modelo:
`Resultado bruto − Costos de ejecución = Resultado económico neto`.

**Contrato aprobado**: todo camino que modifique `Cash` respeta
`PnL bruto + Costo aplicado = Estado económico final`. Orden por Fill:
`Precio de ejecución → PnL/Margin bruto → Coste → Cash actualizado → Equity actualizado`.

**Punto de integración verificado** (`ESPECIFICACION_MODELO_COSTES_V1.md` §2.1): `AplicadorFill.
Aplicar` tiene 3 rutas (abrir/aumentar, reducir FIFO, Cross-Zero), cada una muta `Cash` en un
punto distinto — el coste se resta en cada una, no en un único punto central. Cross-Zero tiene dos
mutaciones (cierre de posición vieja + apertura de posición nueva); el coste se aplica a cada
tramo económico realmente ejecutado, nunca como un coste artificial único sobre el evento
completo.

## D-063 — Componentes de coste incluidos en V1

**Estado**: 🟢 Aprobada. **Selección: B — Comisión + Slippage.**

**Decisión**: V1 incluye comisión (coste explícito de ejecución) y slippage (diferencia entre
precio esperado y precio ejecutado). No incluye spread explícito (el motor no tiene modelo bid/ask
— `MatchingEngine` opera sobre OHLC por vela, no libro de órdenes) ni funding (depende de mercado/
tiempo mantenido/reglas externas, fuera de esta versión).

**Fórmula aprobada**: `CostoTotal = Comision + Slippage`, con
`Comision = Cantidad * PrecioFill * TasaComision`. Slippage solo aplica a órdenes `Market`
(`precioFill = vela.Open`, sin precio de referencia distinto) — para `Limit`/`Stop`/`StopLimit` el
fill ya ocurre exactamente al precio pactado por la orden (RN-03), sin divergencia que modelar.
Detalle completo en `ESPECIFICACION_MODELO_COSTES_V1.md` §D-063.

**Motivo de la selección**: superar el nivel de "PnL bruto" sin intentar replicar un broker
completo — comisión + slippage son los dos componentes que el motor actual puede calcular sin
rediseñar la ejecución.

## D-064 — Origen del parámetro de coste

**Estado**: 🟢 Aprobada. **Selección: C — Experimento.**

**Decisión**: `TasaComision`/`TasaSlippage` no se agregan a `Instrumento` — viven en la
configuración económica experimental (`ConfiguracionExperimento` o tipo agregado propio).

**Motivo de la selección**: el mismo símbolo (ej. BTCUSDT) puede evaluarse bajo distintas
hipótesis de coste dentro del laboratorio (Experimento A: comisión 0.05%, Experimento B: comisión
0.10%) — la propiedad del coste no pertenece al activo, pertenece a la condición económica
simulada. Mantiene la separación `Instrumento` (identidad del mercado + margen, D-057) ≠
`Configuración económica experimental` (condiciones económicas simuladas).

---

## D-066 — Responsable del cálculo de tamaño de operación

**Estado**: 🟢 Aprobada. **Selección: A — capa externa `GestorCapital`**, entre la señal de la
`Strategy` y el resto del loop de `BacktestRunner`.

**Decisión**: `Strategy.Observar(dataSlice)` produce una señal (dirección + `Cantidad` placeholder,
hoy `1m`); `GestorCapital.Ajustar(requests, portfolio, sizing?)` transforma esa señal en la
`Cantidad` final antes de que `ValidadorCapacidad`/`ValidadorBolsaRequests` operen sobre ella —
punto de integración verificado en `BacktestRunner.cs`, entre las líneas 47 y 56 (antes del
`foreach` que registra las órdenes).

**Opciones descartadas, con evidencia**:
- **B — Extender `IStrategy`** (recibir `Cash`/`Equity` como parámetro de `Observar`): rompe P-002
  directamente, y afecta a las 3 estrategias existentes aunque ninguna use el parámetro nuevo —
  mayor superficie de regresión sin beneficio sobre A.
- **C — Generador previo al backtest** (pre-transformar la lista completa de órdenes): descartada
  por razón estructural, no de preferencia — verificado en `EstrategiaTresMosqueteros.cs` que la
  martingala decide la señal siguiente dentro del propio `Observar`, a partir del resultado del
  Fill anterior (callback `onOperacionResuelta`). No existe una secuencia completa de órdenes
  "pre-generable" sin ejecutar el backtest primero — intentarlo duplicaría el motor fuera de sí
  mismo.

**Reglas de implementación aprobadas para `GestorCapital`**:
- No debe: conocer señales internas de estrategia, modificar dirección Buy/Sell, decidir entradas,
  resolver martingala, alterar órdenes por motivos técnicos (ej. corrección de precio).
- Debe: recibir órdenes propuestas, conocer el estado económico necesario (`PortfolioState`),
  calcular `Cantidad`, devolver órdenes ajustadas, ser determinista.

Detalle completo en `ESPECIFICACION_ARQUITECTURA_GESTOR_CAPITAL_V1.md`.

## D-067 — Modelo de gestión de capital en V1

**Estado**: 🟢 Aprobada. **Selección: B — Porcentaje de capital**, implementado dentro de
`GestorCapital`.

**Decisión**: el primer modelo de gestión de capital implementado es porcentaje del capital
disponible no comprometido. Fórmula oficial:

```
CapitalDisponible = Cash − Margin
Cantidad = CapitalDisponible × PorcentajeRiesgo
```

**Motivo de la selección**: proporciona una relación reproducible entre capital y exposición
manteniendo la separación entre estrategia y economía (P-002), con menor dependencia de supuestos
que modelos adaptativos como Masaniello — no requiere probabilidad estimada, número de operaciones
ni objetivo (ver comparación completa en `EVALUACION_MODELOS_GESTION_RIESGO_V1.md`).

**Corrección aplicada durante la resolución**: la formulación inicial usaba `EquityDisponible`
(`Cash + Margin + UnrealizedPnL`), pero `PortfolioState` (único estado disponible para
`GestorCapital` en su punto de integración, antes de `ResolutorVela.Resolver`) no expone `Equity`
— ese cálculo ocurre después, dentro de `ResolutorVela.CalcularEquity`, usando el `Close` de la
vela siguiente que `GestorCapital` aún no conoce en ese punto del ciclo. Usar `Equity` habría
exigido duplicar esa lógica económica en un segundo lugar — mismo riesgo que D-062 corrigió (una
consecuencia económica debe tener una única fuente de cálculo). Corregido a `Cash − Margin`
(capital disponible no comprometido), que `PortfolioState` ya expone sin cálculo adicional.

**Distinción explícita a mantener en la documentación**: `Cash − Margin` (sizing inicial) ≠
`Equity` (evaluación económica/reportes) — son conceptos distintos, no sinónimos. Un futuro sizing
basado en `Equity` (`Cantidad = Equity × Riesgo%`) requeriría una nueva decisión/versionado, no
una incorporación silenciosa — implica acceso al valor razonable de posiciones, definición
temporal del precio usado, y reglas para PnL no realizado que esta decisión explícitamente no
resuelve.

**Restricciones aprobadas para `GestorCapital`** (no debe): aumentar exposición por racha
ganadora, reducir por interpretación de mercado, usar winrate histórico, usar régimen de mercado,
usar predicción. El porcentaje es un parámetro experimental fijo, no calibrado con resultados
históricos.

**Masaniello**: no eliminado — queda como candidato para una fase posterior, cuando existan modelo
probabilístico definido, horizonte experimental, métrica de objetivo y validación independiente.

## D-071 — `GestorCapital` como transformación de órdenes

**Estado**: 🟢 Aprobada.

**Decisión**: `GestorCapital` no crea nuevas operaciones ni señales; únicamente transforma la
`Cantidad` de órdenes ya existentes generadas por la `Strategy`.

**Motivo**: mantener trazabilidad entre la señal original (dirección, tipo de orden decididos por
la estrategia) y la exposición aplicada (`Cantidad` final) — relevante para auditoría futura, que
debe poder reconstruir tanto qué decidió la estrategia como qué tamaño terminó ejecutándose, sin
ambigüedad sobre cuál capa produjo cada parte de la orden.

**Consecuencia de implementación**: `GestorCapital.Ajustar` transforma la lista de `OrderRequest`
recibida (misma cantidad de elementos, mismo `Side`/`Type`/`PrecioLimite`/`PrecioStop`) — nunca
agrega ni quita órdenes de la bolsa que la estrategia emitió.

## D-068 — Relación entre sizing y `ValidadorCapacidad`

**Estado**: 🟢 Aprobada. **Selección: `GestorCapital` propone, `ValidadorCapacidad` valida — no se
invierten responsabilidades.**

**Decisión**: `GestorCapital.Ajustar` se ejecuta antes de `ValidadorCapacidad.Validar` en el loop
de `BacktestRunner` — el validador sigue evaluando la `Cantidad` que reciba, sin importar si vino
de la `Strategy` directamente (sizing inactivo) o del `GestorCapital` (sizing activo). Ningún
cambio en `ValidadorCapacidad` ni `CalculadoraReservaPreventiva` — siguen siendo consumidores, no
generadores de tamaño. Flujo: `GestorCapital → Cantidad propuesta → ValidadorCapacidad → Orden
aceptada / incapacidad registrada` (D-059, sin bloquear).

## D-069 — Separación de versiones económicas

**Estado**: 🟢 Aprobada. **Regla**: una modificación del sizing de una estrategia existente no
modifica el Caso 1 — genera una nueva configuración experimental/versionado económico.

**Motivo**: cambiar `Cantidad = 1` (fijo) por `Cantidad` variable afecta exposición, resultados,
operaciones y equity — no es una mejora interna, es un nuevo experimento. Mismo principio que
D-017/D-046 aplicaron a artefactos individuales, extendido aquí a cualquier estrategia que
incorpore sizing real.

**Restricción aprobada para Caso 2.3**: no modificar estrategias actuales, no reemplazar
`Cantidad` existente, no alterar `baseline_final/`, no introducir Masaniello directamente.

## D-070 — Arquitectura del gestor de capital

**Estado**: 🟢 Aprobada.

**Decisión**: la gestión de capital se implementa como una capa intermedia entre la estrategia y
la ejecución. Las estrategias continúan generando señales sin conocimiento financiero; el gestor
transforma la exposición antes de la validación de capacidad y ejecución.

**Motivo**: preservar la separación estrategia/economía (P-002) y permitir múltiples modelos de
sizing intercambiables sin reabrir la arquitectura por cada candidato evaluado en D-067.

Formaliza como decisión numerada la arquitectura ya detallada en D-066 y
`ESPECIFICACION_ARQUITECTURA_GESTOR_CAPITAL_V1.md` — flujo completo:
`Strategy → GestorCapital → ValidadorCapacidad → Motor`.

---

## Resumen de decisiones registradas

| Decisión | Selección | Estado |
|---|---|---|
| D-057 — Naturaleza de `TasaMargen` | A — Pertenece al instrumento | 🟢 Aprobada |
| D-058 — Unidad económica | B — Unidad monetaria abstracta | 🟢 Aprobada |
| D-059 — Restricción de capacidad | A — Registrar incapacidad, no bloquear | 🟢 Aprobada |
| D-060 — Momento de evaluación de capacidad | A — Antes de aplicar la orden (sobre `OrderRequest`) | 🟢 Aprobada |
| D-061 — Compatibilidad de contratos existentes | Parámetro opcional con default histórico | 🟢 Aprobada |
| D-062 — Propagación de `tasaMargen` a `ResolutorVela` | Propagar con default `0.1m` | 🟢 Aprobada |
| D-063 — Componentes de coste en V1 | Comisión + Slippage | 🟢 Aprobada |
| D-064 — Origen del parámetro de coste | Experimento (no Instrumento) | 🟢 Aprobada |
| D-065 — Aplicación del coste al Cash | Modifica Cash/Equity (PnL neto) | 🟢 Aprobada |
| D-066 — Responsable del cálculo de tamaño | A — Capa externa `GestorCapital` | 🟢 Aprobada |
| D-067 — Modelo de gestión de capital en V1 | B — Porcentaje de `Cash − Margin` | 🟢 Aprobada |
| D-068 — Relación sizing / `ValidadorCapacidad` | Gestor propone, validador valida | 🟢 Aprobada |
| D-069 — Separación de versiones económicas | Sizing nuevo = nueva versión experimental | 🟢 Aprobada |
| D-070 — Arquitectura del gestor de capital | Capa intermedia, formaliza D-066 | 🟢 Aprobada |
| D-071 — `GestorCapital` transforma, no crea órdenes | Solo ajusta `Cantidad` de órdenes existentes | 🟢 Aprobada |

---

## D-072 — Capital inicial expuesto en el resultado

**Estado**: 🟢 Aprobada. Capital inicial de métricas proviene exclusivamente de
`ConfiguracionExperimento.CapitalInicial`, expuesto en el punto donde se generan reportes (no
necesariamente como campo nuevo de `ResultadoBacktest`, evitando el problema de compatibilidad que
D-061 resolvió). No forma parte de `IdentidadExperimentoCompleta` — es dato de reporte, no de
configuración que altere comportamiento del motor. Ver `ESPECIFICACION_METRICAS_FINANCIERAS_V1.md`
§D-072.

## D-073 — Definición de drawdown de equity

**Estado**: 🟢 Aprobada. `Drawdown(t) = (PeakEquity(t) − Equity(t)) / PeakEquity(t)` sobre
`EquityCurve` (D-077 — fuente oficial), `DrawdownMax = max(Drawdown(t))`. Solo porcentual en V1,
sin campo monetario separado.

## D-074 — Duración del drawdown

**Estado**: 🟡 Definida conceptualmente. ⏳ No implementada en V1 — mismo criterio que D-063
excluyó spread/funding.

**Aclaración registrada tras discrepancia detectada en la revisión de cierre**: la definición
operacional (inicio = primera vela posterior al máximo histórico donde `Equity` cae; fin =
recuperación del máximo previo; sin recuperación = duración hasta el final de la curva; unidad:
velas) queda documentada como referencia para una fase futura, **no como alcance aprobado para
V1**. Una definición aprobada no implica automáticamente una implementación aprobada — la decisión
de implementar una métrica requiere su propio alcance explícito (misma disciplina de Caso 1: no
confundir "se discutió cómo sería" con "se autorizó construirlo"). `ESPECIFICACION_METRICAS_
FINANCIERAS_IMPLEMENTACION_V1.md` no incluye DTO, cálculo, ni pruebas para esta métrica.

## D-075 — Exposición máxima

**Estado**: 🟢 Aprobada. `ExposicionMaxima = Max(PortfolioSnapshot.Margin)` — campo ya existente
en `ResultadoBacktest.PortfolioSnapshots`, sin cálculo de dominio nuevo (verificado contra
`src/Application/PortfolioSnapshot.cs`). No se implementa exposición en unidades del activo
(suma de `LotesVivos.Cantidad`) — evita una segunda unidad de exposición sin necesidad demostrada.

## D-076 — Métricas comparativas

**Estado**: 🟢 Aprobada. Extiende D-014/D-047 (Caso 1) a resultado financiero — tabla lado a lado
sin ordenamiento, con nota obligatoria (mismo patrón D-037) de que ninguna métrica financiera
implica recomendación de uso.

## D-077 — Fuente oficial de datos financieros

**Estado**: 🟢 Aprobada. Toda métrica se deriva exclusivamente de `EquityCurve`/`Cash`/`Margin`/
`Trades` (salida directa de `BacktestRunner`). Prohibido recalcular PnL/equity desde `Fills`
individuales u operaciones — mismo riesgo de divergencia que D-062 corrigió.

## D-078 — Tratamiento de métricas no disponibles

**Estado**: 🟢 Aprobada. Métrica sin fuente válida se representa como ausente (`null`/"no
disponible"), nunca `0`. Prohibida cualquier inferencia para rellenar el vacío — `0` y "no
disponible" son estados distintos, mezclarlos falsearía comparaciones entre corridas.

**Nota de numeración**: D-077/D-078 son decisiones nuevas detectadas durante la resolución de
D-072-D-076, no renumeraciones — discrepancia detectada y corregida antes de registrar nada
(mismo principio que corrigió D-043/D-053 en Caso 1: un identificador `D-N` nunca cambia de
significado).

---

## Resumen de decisiones registradas (actualizado)

| Decisión | Selección | Estado |
|---|---|---|
| D-072 — Capital inicial expuesto | Desde `ConfiguracionExperimento`, en reportes | 🟢 Aprobada |
| D-073 — Drawdown de equity | Porcentual, sobre `EquityCurve` | 🟢 Aprobada |
| D-074 — Duración del drawdown | Definida conceptualmente, no implementada en V1 | 🟡 Definida / ⏳ Futura |
| D-075 — Exposición máxima | `Max(PortfolioSnapshot.Margin)` | 🟢 Aprobada |
| D-076 — Métricas comparativas | Tabla sin ranking, nota obligatoria | 🟢 Aprobada |
| D-077 — Fuente oficial de datos | `EquityCurve`/`Cash`/`Margin`/`Trades` únicamente | 🟢 Aprobada |
| D-078 — Métricas no disponibles | `null`, nunca `0` | 🟢 Aprobada |

## D-079 — Configuración financiera del protocolo

**Estado**: 🟢 Aprobada. **Selección: A — Extender `EntradaProtocolo`.**

**Decisión**: el pipeline experimental (`EjecutorProtocolo.cs`) permite recibir configuración
financiera explícita (`Instrumento?`, `ConfiguracionCostes?`, `ConfiguracionSizing?`) mediante
`EntradaProtocolo`, propagada a `ConfiguracionExperimento` en `EjecutorUnTimeframe`, manteniendo
compatibilidad retroactiva mediante valores opcionales — mismo criterio D-061: `null` conserva
exactamente el comportamiento histórico vía los defaults ya existentes.

**Motivo**: sin esta extensión, cualquier corrida del pipeline (incluido un baseline de Caso 2)
solo podía ejecutar con los defaults de Caso 1 — el modelo económico, costes y gestión de capital
de Caso 2 quedaban inaccesibles desde `EjecutorProtocolo`, aunque implementados y probados de forma
aislada. Necesario para que el baseline financiero represente el modelo activo, no Caso 1 corrido
de nuevo.

**Restricciones aprobadas**: no modificar `IStrategy` ni ninguna estrategia existente, no cambiar
ningún valor default, no alterar el resultado de corridas que no configuren estos campos
explícitamente (compatibilidad con `TestsEjecutorProtocolo.cs`/`Program.cs` existentes).

## D-080 — Reporte financiero independiente

**Estado**: 🟢 Aprobada. **Selección: A — Nuevo `ReporteFinancieroGenerador.cs`.**

**Decisión**: las métricas financieras de Caso 2 se reportan mediante un generador específico
(`modelo_financiero/ReporteFinancieroGenerador.cs`), separado de `ReporteConsolidadoGenerador.cs`
(Caso 1) — este último permanece exactamente como fue congelado en
`VERSION_EXPERIMENTAL_CASO1_V1.md`, sin ninguna modificación.

**Motivo**: `ReporteConsolidadoGenerador` fue congelado con el texto "modelo económico incompleto"
en su sección de limitaciones — correcto para el estado histórico de Caso 1. Editarlo ahora
mezclaría evolución del sistema, corrección histórica y cambio documental sobre un artefacto ya
declarado congelado — mismo principio que impidió reabrir `EquityCurve`/`AplicadorFill` de
`src/` sin una decisión explícita (P-001).

---

## Resumen de decisiones registradas (D-079/D-080)

| Decisión | Selección | Estado |
|---|---|---|
| D-079 — Configuración financiera del protocolo | `EntradaProtocolo` extendida, opcional | 🟢 Aprobada |
| D-080 — Reporte financiero independiente | Generador nuevo, Caso 1 intacto | 🟢 Aprobada |

## D-081 — Programa de corrida del baseline financiero

**Estado**: 🟢 Aprobada. **Selección: A — Programa dedicado en `modelo_financiero/`.**

**Decisión**: el baseline financiero utiliza un ejecutable dedicado
(`modelo_financiero/ProgramBaselineFinanciero.cs`), sin modificar el punto de entrada congelado de
Caso 1 (`protocolo/Program.cs`). La corrida usa la misma estrategia, dataset y timeframes del
baseline operacional (Tres Mosqueteros, `BTCUSDT_2024-01-02_2025-01-02`, 1m + 1D), con
configuración financiera explícita congelada.

**Configuración financiera de la corrida** (D-079 aplicado, valores no optimizados):
- **Instrumento**: `Simbolo=BTCUSDT`, `TasaMargen=0.1m` (conserva el valor histórico del motor,
  evita introducir un cambio adicional de apalancamiento en el baseline).
- **Costes**: `TasaComision=0.001m`, `TasaSlippage=0.001m` (fricción no nula, ejercita la ruta
  económica de Caso 2.2 — no representan costes reales de un exchange específico, son parámetros
  experimentales congelados).
- **Sizing**: `PorcentajeRiesgo=0.01m` (activa `GestorCapital`, exposición pequeña, verifica la
  integración sin convertir el baseline en búsqueda de rendimiento).

**Motivo de la ubicación**: mantiene todos los artefactos de Caso 2 juntos, evita una segunda
infraestructura paralela de protocolo, deja claro que es una ejecución específica del modelo
financiero — no una variante del protocolo base.

**Motivo de la comparabilidad**: misma estrategia/mercado que `baseline_final/` (Caso 1) permite
comparar "sin modelo financiero activo" vs. "modelo financiero activo" sin cambiar el experimento
base.

---

## Resumen de decisiones registradas (D-081)

| Decisión | Selección | Estado |
|---|---|---|
| D-081 — Programa de corrida del baseline financiero | Ejecutable dedicado, config. financiera explícita | 🟢 Aprobada |

## D-082 — Identidad experimental y configuración económica

**Estado**: 🟢 Aprobada. **Selección: A — Corregir `IdentidadExperimentoCompleta.Calcular`.**

**Decisión**: la identidad experimental debe incluir la configuración económica efectiva
utilizada en la corrida (`Instrumento`, `ConfiguracionCostes`, `ConfiguracionSizing`). Los valores
por defecto se normalizan (`null` → `Default`) antes del cálculo del hash, para no producir dos
identidades distintas para el mismo experimento efectivo.

**Evidencia del problema**: `IdentidadExperimentoCompleta.Calcular`
(`protocolo/IdentidadExperimentoCompleta.cs:23-36`) calculaba el `HashCompuesto` solo a partir de
`estrategia, versionEstrategia, parametros, datasetSourceSha256, clasificadorRegimenVersion,
versionProtocolo` — nunca recibía la configuración económica. Detectado al generar el primer
intento del baseline financiero: una corrida con `Instrumento`/`Costes`/`Sizing` explícitos
produjo el mismo `HashCompuesto` que `baseline_final/` (Caso 1, sin configuración económica) —
contradice directamente D-069 (sizing nuevo = nueva identidad, "sin mecanismo adicional") y la
garantía de trazabilidad citada como criterio de cierre desde Caso 1 (`AUDITORIA_FINAL_CASO1_V1.md`).

**Restricción de implementación**: el baseline de Caso 1 debe permanecer reproducible — el
`HashCompuesto` congelado `A48CCC57DA1919F533F4D532FDC0F945705681DCDA813B385BBFE7F44F40998E` no
puede cambiar. Verificado antes de escribir el reporte financiero: entrada histórica sin
configuración explícita → valores efectivos normalizados a `Instrumento.Default`/
`ConfiguracionCostes.Default`/sizing inactivo → mismo texto de identidad → mismo hash histórico.

**Regla de serialización aprobada**: `Estrategia + Parámetros + Dataset + Clasificador +
Protocolo + Instrumento efectivo + Costes efectivos + Sizing efectivo`, usando siempre el valor
efectivo (`?? Default`), nunca la presencia/ausencia de `null` en la entrada.

**Verificaciones obligatorias antes de regenerar el baseline financiero**:
1. Caso 1: hash nuevo == hash congelado `A48CCC57...`.
2. Caso 2 financiero: hash nuevo != hash de Caso 1.
3. Cambiar comisión, slippage, sizing o margen produce un hash distinto entre sí.

---

## Resumen de decisiones registradas (D-082)

| Decisión | Selección | Estado |
|---|---|---|
| D-082 — Identidad experimental incluye configuración económica efectiva | Normalizada a Default antes del hash | 🟢 Aprobada |

## D-083 — Configuración de sizing del baseline financiero

**Estado**: 🟢 Aprobada.

**Decisión**: el parámetro `PorcentajeRiesgo` del baseline financiero no puede elegirse
interpretando directamente el porcentaje como exposición monetaria. La fórmula aprobada de sizing
(D-067, `GestorCapital.Ajustar`) opera en unidades del activo (`Cantidad` de BTC, no unidades
monetarias) — la configuración debe validarse contra la relación entre cantidad, precio del activo
y margen requerido antes de congelar evidencia.

**Evidencia del problema**: con `PorcentajeRiesgo=0.01` (valor inicial de D-081) y `Precio`
BTCUSDT en el rango real del dataset (~44,000 a ~96,000), la primera orden reservó
`Margin ≈ 60,000` — 60 veces `CapitalInicial=1000`. `ValidadorCapacidad` solo observa (D-059, no
bloquea), por lo que la orden se ejecutó igual, produciendo `Cash final` fuertemente negativo y
`Equity` con magnitud de cientos de miles. No es un defecto de `GestorCapital`/D-067 — es una
consecuencia correcta de la fórmula bajo un valor mal calibrado dimensionalmente.

**Procedimiento aprobado**: calcular `Margin_primera_orden ≈ CapitalInicial × PorcentajeRiesgo ×
Precio × TasaMargen`, elegir `PorcentajeRiesgo` tal que ese margen sea una fracción claramente
razonable de `CapitalInicial` (no un óptimo, solo evitar sobregiro inmediato), validando **solo**
exposición inicial + compatibilidad dimensional + reproducibilidad — nunca mirando PnL,
rentabilidad, drawdown, ni comparando timeframes/estrategias.

**Cálculo aplicado**: con `CapitalInicial=1000`, `Precio≈90,000` (extremo superior del rango real
del dataset, conservador), `TasaMargen=0.1`: `Margin ≈ 9,000,000 × PorcentajeRiesgo`. Para
`Margin_objetivo ≈ 20` (≈2% de `CapitalInicial`): `PorcentajeRiesgo ≈ 0.0000022`. Valor fijo
elegido: **`PorcentajeRiesgo = 0.000002`** (`Margin_primera_orden ≈ 18`, ≈1.8% del capital
inicial).

**Explícitamente fuera de esta decisión**: fórmula de `GestorCapital`, D-067, D-068, D-069,
integración de `ValidadorCapacidad`, bloqueo de operaciones — el hallazgo no demuestra que la
arquitectura sea incorrecta, demuestra que la interpretación del parámetro del baseline era
incompleta.

**Consecuencia esperada**: cambia `HashConfiguracionEconomica` (nueva configuración económica),
`HashCompuesto` estratégico permanece igual (D-082 — separación de hashes).

---

## Resumen de decisiones registradas (D-083)

| Decisión | Selección | Estado |
|---|---|---|
| D-083 — Configuración de sizing del baseline financiero | `PorcentajeRiesgo = 0.000002` | 🟢 Aprobada |

## D-084 — Semántica de órdenes para sizing (Tratamiento de órdenes de cierre en Gestión de Capital)

**Estado**: 🟡 Deuda técnica registrada. **Categoría: Arquitectura / Gestión de capital.**
Resolución explícitamente fuera de Caso 2 V1.

**Decisión**: `GestorCapital` actualmente recibe órdenes sin una semántica explícita de intención
(apertura/cierre/reducción/reversión). Aplicar sizing uniforme sobre todas las órdenes puede
alterar cierres de posiciones existentes y producir residuos de lotes no originados por la
estrategia. La resolución queda fuera de Caso 2 V1.

**Evidencia del problema**: al generar el baseline financiero con `Sizing` activo
(`PorcentajeRiesgo=0.000002`, D-083) sobre el timeframe 1m (~525,600 velas, ~82,475 operaciones en
la corrida equivalente de Caso 1), la ejecución no terminó en un tiempo razonable (>25 minutos,
CPU acumulada casi nula — descartando bucle apretado). Causa raíz aislada: `GestorCapital.Ajustar`
(`src/Domain/Portfolio/GestorCapital.cs:19-21`) recalcula `Cantidad` en **toda** `OrderRequest`
recibida, sin distinguir apertura de cierre — `EstrategiaTresMosqueteros.Observar` emite
`OrderRequest(lado, Market, 1m)` idéntico como objeto tanto para abrir como para cerrar/reabrir.
La orden de cierre recibe así una `Cantidad` recalculada con el `Cash`/`Margin` del instante, que
casi nunca coincide exactamente con la cantidad realmente abierta en el lote — `AplicadorFill`/
`ConsumidorFifo` interpretan la discrepancia como cierre parcial (`item.CantidadConsumida ==
magnitudLote` casi nunca es cierto), dejando un residuo de lote cada vez. A lo largo de decenas de
miles de operaciones, `LotesVivos` crece sin límite en vez de mantenerse en 0-1 elementos (como en
Caso 1, sin sizing).

**Por qué no se detectó en pruebas previas**: `GestorCapitalTests.cs` (P1-P6) valida `Ajustar` de
forma aislada, sin una corrida completa de miles de operaciones donde el residuo acumulado se
manifieste. Con `PorcentajeRiesgo=0.01` (primer intento de calibración, ver D-083) el capital
colapsaba tan rápido que la corrida terminaba en segundos, sin tiempo para acumular lotes
residuales — el problema solo se manifestó al calibrar `PorcentajeRiesgo` a un valor que no
destruye el capital de inmediato.

**No resuelto por diseño** (explícitamente rechazado): modificar `GestorCapital` parcialmente,
introducir heurísticas para detectar cierres, comparar por lado/precio para inferir intención, o
cambiar `OrderRequest` sin una nueva decisión — cualquiera de estos sería una solución implícita a
un problema de arquitectura, no una decisión explícita.

**Consecuencia sobre el baseline financiero**: el baseline de Caso 2 se genera con `Instrumento`
activo + `Costes` activos + `Sizing=null`. La infraestructura de `GestorCapital` (D-066-D-071)
permanece implementada, probada (P1-P6) y documentada — pero no forma parte de la configuración
congelada de referencia hasta que D-084 se resuelva en una fase futura.

---

## Resumen de decisiones registradas (D-084)

| Decisión | Selección | Estado |
|---|---|---|
| D-084 — Semántica de órdenes para sizing | Deuda técnica, fuera de Caso 2 V1 | 🟡 Deuda técnica |

## D-085 — Escala económica histórica de estrategias (relación entre capital inicial y tamaño nominal de estrategia)

**Estado**: 🟡 Deuda técnica registrada. **Categoría: Modelo económico / Compatibilidad
histórica.** Resolución explícitamente fuera de Caso 2 V1.

**Decisión**: la exposición de métricas financieras absolutas reveló que las estrategias
históricas de Caso 1 utilizan cantidades nominales fijas (`Cantidad=1`) sin una relación
dimensional explícita con `CapitalInicial`. El modelo económico permite observar esta situación,
pero Caso 2 V1 no redefine el capital ni el tamaño histórico de las estrategias.

**Evidencia del problema**: con `Sizing=null` (D-084 resuelto) — `GestorCapital` no interviene,
`Cantidad` viene directo de `EstrategiaTresMosqueteros` (fija en `1m`, 1 BTC por orden). `Margin =
Cantidad × PrecioFill × TasaMargen ≈ 1 × 90,000 × 0.1 = 9,000` por lote — 9 veces
`CapitalInicial=1000` desde la primera orden. `ValidadorCapacidad` observa esto pero nunca bloquea
(D-059), así que el motor ejecuta la orden igual, produciendo `CashFinal` fuertemente negativo
(`-38,171,769.28` en 1m). Este desajuste dimensional siempre existió desde Caso 1 —
`EstrategiaTresMosqueteros` está congelada con `Cantidad=1` fija — pero quedó oculto porque Caso 1
nunca calculó ni reportó `MetricasFinancieras` (solo `EquityInicial`/`EquityFinal`/`RetornoPct`
derivados, explícitamente "no comparables financieramente", `DEUDA_TECNICA_CASO1_V1.md` §1-2).

**Consecuencia para el baseline financiero**: no se recalibra `CapitalInicial` (rechazado usar
`1,000,000` u otro valor para "normalizar" resultados — sería introducir una calibración nueva
sobre un parámetro que hasta ahora era parte del contexto experimental de Caso 1, cambiando la
interpretación económica de los resultados, no solo la configuración del baseline). Se mantiene
`CapitalInicial=1000` como continuidad experimental con Caso 1. El reporte financiero debe incluir
una advertencia visible: *"Las métricas financieras absolutas representan la ejecución del modelo
económico experimental bajo la configuración histórica de tamaño de posición. No constituyen una
simulación de capital real ni una recomendación de dimensionamiento."*

**Separación explícita de D-084**: D-084 es sobre `GestorCapital` modificando incorrectamente
órdenes de cierre (deuda técnica de sizing). D-085 es sobre `Cantidad` histórica fija +
`CapitalInicial` no dimensionalizado (deuda técnica del modelo económico histórico, independiente
de si sizing está activo o no). No deben fusionarse — son hallazgos distintos con causas distintas.

---

## Resumen de decisiones registradas (D-085)

| Decisión | Selección | Estado |
|---|---|---|
| D-085 — Escala económica histórica de estrategias | Deuda técnica, `CapitalInicial=1000` mantenido | 🟡 Deuda técnica |

## Próximo paso

Caso 2.4 completamente decidido (D-072 a D-078), congelado como V1 Experimental. D-079 a D-083
aprobadas e implementadas. D-084/D-085 registradas como deuda técnica — pendiente agregar
advertencia obligatoria al reporte y regenerar `baseline_financiero_final/` definitivo.
