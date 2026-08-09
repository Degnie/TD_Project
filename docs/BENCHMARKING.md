# Benchmarking de referencias

**Estado:** aprobado. Análisis comparativo de `SPEC.md` v6.0 frente a 4 motores de backtesting de
referencia, revisado por 2 auditores adicionales. No se modifica `SPEC.md`, código ni arquitectura
como resultado de este documento.

**Contexto:** esta fase corresponde a la "Fase 5 · Benchmarking de referencias" definida en
`agent-workflow/prompts/01M-migracion-gemini.md`, que no se había ejecutado en este proyecto
(confirmado por ausencia total de menciones a estos 4 repos en todo el historial de git previo).
Se ejecutó de forma retroactiva sobre el `SPEC.md` v6.0 ya implementado — no para modificar el
SPEC, sino para documentar qué tan alineadas o divergentes están nuestras decisiones de diseño
frente a la industria, y detectar aprendizajes puntuales.

**Repos analizados:**
1. [backtesting.py](https://github.com/kernc/backtesting.py) (kernc)
2. [vectorbt](https://github.com/polakowo/vectorbt) (polakowo)
3. [Backtrader](https://github.com/mementum/backtrader) (mementum)
4. [Freqtrade](https://github.com/freqtrade/freqtrade)

**Método:** investigación documental (README, docs oficiales, páginas de API) vía WebFetch/WebSearch.
No se clonó ni ejecutó código de ninguno de los 4. Donde la documentación pública no cubre un
punto, se marca explícitamente como "no documentado" en vez de inferir — ver detalle de
confiabilidad por fuente en la sección final.

---

## Tabla comparativa

Filtro obligatorio del prompt de origen: **un aprendizaje que no pueda citar un ID concreto del
SPEC no se adopta**. El veredicto por defecto es "referencia, no adoptado". Adoptar exige
justificación explícita ligada a un RN/CU/RNF/EC.

| # | Tema | Qué hacen ellos | Qué hacemos nosotros (ID SPEC) | Veredicto | Justificación |
|---|---|---|---|---|---|
| 1 | Ambigüedad High/Low intra-vela | **Freqtrade**: regla nombrada explícita — "low antes que high" para stoploss (peor caso, protege capital), invertida para trailing stop. **vectorbt**: SL asumido antes que TP (confianza media, fuente secundaria). **backtesting.py / Backtrader**: NO documentan una regla explícita — gap confirmado en ambos. Ninguno de los 4 **evalúa ambas alternativas y compara resultados**; aplican una regla fija unidireccional. | RN-11: evaluamos **ambas** trayectorias canónicas completas (A: O→H→L→C, B: O→L→H→C), resolvemos Fills/Position/Equity de cada una por separado, y seleccionamos la de **menor Equity final** (peor caso real para esa vela, no una regla fija a priori). | **Ya cubierto, con enfoque más exhaustivo — no se adopta nada.** | RN-11 usa un modelo de resolución más exhaustivo que las reglas deterministas intra-vela observadas en los 4 repos: no asume de antemano qué trayectoria es "peor", lo determina calculando ambas. Esto es una elección de diseño — pagar complejidad computacional a cambio de auditabilidad determinista — no una superioridad absoluta: para investigación exhaustiva es más riguroso, para millones de simulaciones rápidas una regla fija puede ser preferible. Freqtrade se acerca más en espíritu (worst-case explícito) pero no re-simula la alternativa. Este es el hallazgo más significativo del benchmarking. |
| 2 | Transparencia de la regla de desambiguación | Freqtrade es el único que **nombra y documenta** su regla públicamente ("Low happens before high for stoploss"); Backtrader/backtesting.py la dejan implícita en el código, sin explicarla en docs. | RN-11 documenta la regla completa en el SPEC (fórmula de selección, desempate, equivalencia). | **Ya cubierto — no se adopta nada.** | Nuestra documentación ya es más explícita que 3 de los 4 repos. Aprendizaje de proceso (no de código): confirma que documentar la regla de resolución de ambigüedad OHLC como regla de negocio explícita, no como detalle de implementación, es buena práctica — ya la seguimos. |
| 3 | Detección de look-ahead bias | **Freqtrade**: herramientas dedicadas de primera clase — comandos `lookahead-analysis` y `recursive-analysis` que auditan la estrategia del usuario buscando fugas de datos futuros. Los otros 3 solo mitigan estructuralmente (indicadores no-NaN, DataSlice truncado) sin una herramienta de auditoría activa. | RN-13 (desfase N/N+1) + CU-06 (bloqueo físico de lectura futura → StrategyError) previenen el look-ahead estructuralmente, vía `DataSlice` bloqueado. No existe una herramienta de *auditoría* que analice retroactivamente si una `Strategy` ya escrita intentó hacer trampa de forma sutil (ej. usando un indicador cuyo cálculo interno mira adelante). | **Candidato a evaluar — no adoptado.** | Son dos categorías distintas, no equivalentes: la nuestra es **prevención** (imposibilidad física de leer futuro, garantizada en tiempo de ejecución); la de Freqtrade es **auditoría** (detectar, después de escritas, estrategias que ya hicieron trampa por una vía indirecta, ej. un indicador mal calculado). Nuestra prevención sigue siendo la garantía más fuerte de las dos. No se recomienda adoptarlo ahora — no hay ID de SPEC que lo ampare. Queda como línea de investigación futura, relevante sobre todo si algún día `TD_Project` admite estrategias de terceros (escenario en el que una herramienta de auditoría, no solo de prevención, gana valor). |
| 4 | Reversión de posición (Cross-Zero) | **backtesting.py**: sin `hedging=True`, una orden opuesta a un long activo simplemente cierra la posición existente (FIFO); no hay reversión directa en una sola orden salvo lógica manual del usuario. **vectorbt**: soporta reversión vía combinación de dirección + flag `accumulate`, en el motor vectorizado. **Backtrader/Freqtrade**: reversión documentada solo para mercados de futuros (Freqtrade) o no detallada explícitamente en las páginas revisadas (Backtrader). | RN-10: un Fill que invierte la posición cierra el Trade activo completo (liquidando PnL, liberando Margin) y abre uno nuevo por el excedente, en una sola operación atómica, para cualquier instrumento (no limitado a futuros). | **Ya cubierto, con alcance más amplio — no se adopta nada.** | Nuestra Reversión Cross-Zero es más general que la de Freqtrade (no restringida a futuros) y más explícita como regla de negocio que en backtesting.py (donde requiere hedging=True + lógica manual) o vectorbt (donde es un flag de comportamiento, no una regla documentada de negocio). |
| 5 | Asignación FIFO en reducciones parciales | **backtesting.py**: confirma FIFO explícitamente en el modo no-hedging. Los otros 3 no documentan explícitamente FIFO/LIFO en las páginas de Position/Portfolio revisadas (posible gap documental de su lado, no evidencia de ausencia). | RN-09: reducciones parciales imputan PnL y liberación de Margin contra los lotes más antiguos (FIFO), con Margin por lote fijo desde su apertura (RN-08). | **Ya cubierto — no se adopta nada.** | Coincide con la única confirmación explícita que encontramos (backtesting.py). Nuestro modelo de "Lote" con Margin individual fijo por entrada es más granular que lo documentado en los 4 — ninguno detalla explícitamente un concepto equivalente a "Margin por lote inmutable hasta consumo". |
| 6 | Precisión numérica / determinismo financiero | Los 4 usan floats estándar de NumPy/pandas (double precision). Ninguno documenta uso de tipos decimales de precisión fija para evitar error de coma flotante binaria en cálculos financieros. | RNF-05: precisión interna de 8 decimales, redondeo Half-to-Even a 2 decimales solo al reportar, identidad `Equity_rep = Cash_rep + Margin_rep + UnrealizedPnL_rep` estrictamente respetada. | **Ya superior — no se adopta nada.** | Ningún competidor documenta una garantía equivalente. Este es otro punto donde el SPEC es más riguroso que la práctica común de la industria de referencia — coherente con que los 4 son herramientas de investigación/prototipado rápido (Python + NumPy), no motores diseñados desde cero para exactitud decimal auditable. |
| 7 | Aislamiento entre ejecuciones (RNF-07) | No se encontró declaración explícita en ninguno de los 4 sobre garantías de aislamiento de estado entre backtests corridos en el mismo proceso. Freqtrade documenta explícitamente lo opuesto en un caso: con pairlists dinámicos, "reproducibility of backtesting-results cannot be guaranteed" — es decir, admite que su determinismo puede romperse bajo ciertas configuraciones. | RNF-06 (determinismo bit-a-bit) + RNF-07 (aislamiento absoluto, sin estado compartido entre simulaciones), verificado con `AislamientoExperimentosTests.cs`. | **Ya superior — no se adopta nada.** | Freqtrade documenta explícitamente una limitación de reproducibilidad que nuestro SPEC prohíbe por diseño. Punto a favor claro de nuestra garantía. |
| 8 | Separación de responsabilidades / extensibilidad de Strategy | **Freqtrade**: la interfaz `IStrategy` es compartida entre backtest, dry-run y live — mismo código de estrategia en los 4 modos, para minimizar el salto backtest→producción. | `IStrategy` (Domain.Strategy): único punto de extensión del usuario, observa `DataSlice(N)` y emite `OrderRequest`. No existe modo "live" en el alcance actual (Fuera de alcance: "Live Trading — Dominio es batch estático"). | **Referencia, no adoptado.** | El objetivo de Freqtrade (paridad backtest/live) está explícitamente fuera del alcance de nuestro SPEC. No hay ID que lo ampare y adoptarlo violaría la frontera "Fuera de alcance" ya aprobada. Se registra solo como contexto: si en el futuro se abriera un alcance de ejecución en vivo, la lección de Freqtrade (misma interfaz de estrategia en todos los modos) sería el patrón a seguir — pero eso es una decisión de negocio nueva, no técnica. |
| 9 | Rendimiento — vectorizado vs event-driven | **vectorbt**: motor vectorizado (NumPy + Numba JIT + Rust opcional), miles de combinaciones de parámetros evaluadas en paralelo como arrays. **Backtrader**: híbrido — indicadores vectorizados ("runonce"), decisiones event-driven. **backtesting.py / Freqtrade**: event-driven puro, vela por vela. | Nuestro motor es event-driven puro: `BacktestRunner` avanza vela por vela (`for n in 0..Velas.Count-1`), sin modo vectorizado. RNF-01/02/03/04 (throughput, memoria, speedup paralelo) están con objetivos cuantitativos `[Decisión Técnica Pendiente]` — ver `docs/PENDIENTES.md`. | **Candidato a evaluar — no adoptado.** | vectorbt no es "una versión más rápida" del mismo motor — es otro paradigma (arrays → señales → simulación masiva, contra nuestro vela → evento → orden → fill → portfolio). Adoptar vectorización dentro del motor actual arriesgaría destruir justamente lo que lo hace valioso: Fill Log, RN-11, auditoría causal, resolución A/B. **Recomendación explícita: no modificar el motor canónico.** Si en el futuro se necesita esa clase de rendimiento, la vía coherente con la arquitectura es un motor secundario y separado (ej. un `FastSimulationEngine`) para exploración masiva de parámetros, nunca una reescritura del motor event-driven existente. |
| 10 | Modelo de órdenes vinculadas (OCO) | **Backtrader**: el más completo — OCO explícito vía parámetro `oco=<order_ref>`, encadenable, más "bracket orders" (entrada + SL + TP como grupo). **backtesting.py**: SL/TP como OCO contingente implícito por trade, sin OCO arbitrario de usuario. **Freqtrade/vectorbt**: sin OCO como primitiva expuesta al usuario; se resuelve por prioridad fija interna (Stoploss→ROI→Trailing en Freqtrade). | RN-05 (Exclusividad OCO) + RN-11 (resolución OCO cruzada con trayectorias A/B, ver CU-19): grupo lógico de órdenes mutuamente excluyentes, con cancelación atómica de las ramas hermanas en la misma resolución temporal, evaluado contra ambas trayectorias. | **Ya cubierto, con un matiz superior — no se adopta nada.** | Backtrader tiene la API de OCO más flexible/general (encadenable) de los 4, pero ninguno cruza la resolución OCO contra dos trayectorias intra-vela como hace nuestro CU-19. El "OCO Múltiple Ambiguo" (RN-05 + RN-11 combinadas) no tiene equivalente documentado en ningún competidor. |
| 11 | Fill Log / trazabilidad mínima | Ninguno de los 4 documenta explícitamente un "Fill Log mínimo" como contrato formal (campos obligatorios por Fill). Cada uno expone algo equivalente vía sus objetos de trade/order internos, pero no como una regla de negocio declarada. | RNF-08: Fill Log Mínimo con campos obligatorios explícitos (Secuencia Causal, Dirección, Cantidad, Fill Price, Costo Fricción Real, Timestamp, Tipo de Orden Original), más Estado Canónico Inicial invariante. | **Ya superior — no se adopta nada.** | Formalizar la trazabilidad como contrato de RNF (no solo como estructura de datos incidental) es más riguroso que la práctica observada en los 4 repos. |
| 12 | Reporting / visualización — métricas de riesgo | Los 4 tienen reporting maduro: `backtesting.py` (30+ métricas + plot HTML/Bokeh), Backtrader (Analyzers + matplotlib), Freqtrade (breakdown temporal, p-value de significancia estadística, FreqUI web, Telegram), vectorbt (Plotly interactivo, heatmaps multi-parámetro, animaciones). | `MetricsDto` (Equity final, PnL total, conteo de Trades) + dashboard estático (curva de Equity SVG nativo, tabla de Trades, resolución RN-11 A/B). Sin métricas de riesgo (Sharpe, Sortino, drawdown, win rate, profit factor) ni significancia estadística. | **Candidato a evaluar — no adoptado. Candidato prioritario.** | Es la brecha más grande y evidente frente a los 4 competidores, y no es solo "menos gráficos": hay una asimetría real entre la precisión/trazabilidad ya alcanzada (alta) y la capacidad actual de responder "¿esta estrategia fue buena?" (baja — hoy solo hay EquityFinal/PnL/Trades). Nuestro dashboard es deliberadamente mínimo (Fase 6, alcance explícito: visualizador local de una ejecución, no una plataforma de análisis). Ninguna métrica de riesgo tiene hoy un ID de SPEC que la exija — adoptar cualquiera de ellas requeriría primero definirlas como RN/RNF nuevas, no es una decisión de implementación libre. Progresión sugerida: **primera etapa** — máximo drawdown, retorno acumulado, win rate, profit factor (describen comportamiento del sistema, cálculo directo sobre datos ya existentes); **segunda etapa** — Sharpe, Sortino, Calmar (requieren decisiones estadísticas adicionales, ej. tasa libre de riesgo). |
| 13 | Slippage / modelo de fricción | **Backtrader**: el más detallado — slippage porcentual o fijo, configurable, con flags para permitir o no exceder el rango high/low de la vela. **Freqtrade**: documenta explícitamente que **no** modela slippage por defecto (fills al precio exacto solicitado). **backtesting.py/vectorbt**: modelo de fricción vía comisión simple, sin slippage dedicado documentado. | `Friction Model` (glosario): modelo determinista que proyecta costos preventivamente y calcula costos reales al liquidar (RN-12 para la proyección, `CostoFriccionReal` en el Fill Log RNF-08). No se detalla en el SPEC si el modelo de fricción incluye un componente de slippage además de comisión — término usado de forma genérica. | **Candidato a evaluar — no adoptado.** | El SPEC ya tiene el concepto (`Friction Model`) pero no está claro, sin volver a los ADRs/implementación, si cubre solo comisión o también slippage price-impact. No es un hallazgo de "nos falta algo que ellos tienen" sino una pregunta de precisión terminológica: ¿`Friction Model` en RN-12/RNF-08 incluye slippage explícitamente, o es terreno abierto? |

---

## Tabla de créditos (solo filas con veredicto "adoptado")

**Ninguna fila fue adoptada.** Todos los candidatos (#3, #9, #12, #13) quedan marcados como
"candidato a evaluar" porque adoptarlos requeriría primero abrir una modificación de `SPEC.md`
(nuevo RN/RNF) — fuera del alcance de este documento. Esta tabla se completará únicamente si en
el futuro se aprueba alguno de los candidatos y se abre el proceso correspondiente de cambio de
SPEC.

| Repositorio | Idea tomada | Dónde se aplicaría | Estado |
|---|---|---|---|
| — | — | — | Sin adopciones |

---

## Advertencia de filtro (obligatoria según el prompt de origen)

De las 13 filas evaluadas: **8 concluyen que nuestro SPEC ya es igual o más riguroso** que los
4 competidores en ese punto (filas 1, 2, 4, 5, 6, 7, 10, 11), **1 está fuera de alcance por
decisión de negocio ya tomada** (fila 8, Live Trading), y **4 quedan como candidatas explícitas
para evaluación futura** (filas 3, 9, 12, 13) — ninguna adoptada unilateralmente. Esta proporción
(0 adopciones automáticas) es consistente con la regla del prompt de origen: "el veredicto por
defecto es no adoptar, adoptar exige justificación" con ID de SPEC concreto, y evita la señal de
alerta que el propio prompt define ("si la mayoría dice adoptado, el filtro no se está
aplicando").

## Diferencias filosóficas de diseño

Varios resultados de la tabla no son "features que nos faltan" sino consecuencia directa de que
cada proyecto optimiza para un objetivo distinto. Esto no es un ranking — son cinco proyectos
resolviendo problemas distintos, cada uno bien resuelto para su propio objetivo:

| Proyecto | Prioridad de diseño |
|---|---|
| backtesting.py | Simplicidad de uso para investigación rápida |
| Backtrader | Flexibilidad operativa (tipos de orden, OCO, brokers) |
| Freqtrade | Paridad backtest/live y operación real de trading |
| vectorbt | Rendimiento masivo — miles de combinaciones evaluadas en paralelo |
| TD_Project | Auditabilidad determinista — trazabilidad completa, resolución causal, precisión decimal exacta |

Dejarlo explícito evita que un futuro lector interprete cualquier fila con veredicto "candidato"
como una carencia. El benchmarking no muestra que `TD_Project` deba parecerse a estos motores —
muestra que las decisiones ya tomadas tienen una filosofía consistente, y ayuda a ubicar las
fronteras naturales de crecimiento (líneas 3, 9, 12, 13) sin comprometer esa filosofía.

## Confiabilidad de fuentes — punto crítico (ambigüedad High/Low)

| Proyecto | ¿Documenta explícitamente la regla de desambiguación intra-vela? | Confianza |
|---|---|---|
| backtesting.py | No — gap confirmado en docs oficiales | Alta |
| Backtrader | No — gap confirmado en docs oficiales | Alta |
| Freqtrade | Sí — regla nombrada verbatim en docs oficiales | Alta |
| vectorbt | Parcial — confirmado solo el caso open-vs-stop; la regla SL-antes-que-TP viene de fuente secundaria | Media |

---

## Conclusión

El benchmarking no identifica una necesidad inmediata de adoptar componentes externos. Las
diferencias encontradas responden principalmente a objetivos de diseño distintos: `TD_Project`
prioriza auditabilidad determinista y trazabilidad causal frente a optimización masiva o
integración live.

Las oportunidades futuras identificadas (filas 3, 9, 12, 13) requieren evaluación independiente
y, en caso de aceptación, modificación formal de `SPEC.md`.
