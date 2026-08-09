# ESPECIFICACIÓN DEL DOMINIO (SPEC.md)

**Etiqueta:** Motor de Backtesting Cuantitativo (MVP)
**Versión:** 6.0
**Estado:** APROBADO
**Fecha:** 2026-08-08

## Contrato de este archivo

1. **Verdad Única:** Las reglas aquí definidas son las leyes físicas de la simulación. Ningún código puede contradecirlas.
2. **Prioridad de Dominio:** Cualquier cambio o nueva feature debe originarse como una modificación a este archivo antes de escribirse una línea de código.
3. **Ausencia Técnica:** Este documento prohíbe nombrar lenguajes, frameworks, bases de datos, formatos de persistencia o infraestructuras. Define el "Qué", nunca el "Cómo".
4. **Trazabilidad en Tests:** Los tests automatizados deben citar directamente los IDs de este documento.

---

## 0 · Glosario y Clasificación Conceptual

### Núcleo y Orquestación

* **Backtest:** Orquestador temporal. Avanza el reloj, inyecta el DataSlice a la Strategy y delega la ejecución.
* **DataSlice:** Estructura de datos temporalmente truncada (hasta la vela $N$) inyectada a la Strategy. Bloqueada físicamente contra lecturas más allá de $N$.
* **Candle / OHLCV:** Unidad atómica de mercado en un instante (Timestamp) y resolución temporal (Timeframe).
* **Strategy:** Lógica de dominio del usuario. Observa el DataSlice($N$) y emite OrderRequests.
* **Experiment:** Agrupación superior que aísla y ejecuta uno o múltiples Backtests.

### Ejecución y Mensajería

* **OrderRequest:** Intención de la Strategy para operar.
* **Order:** Orden validada por el Broker y registrada como Pending.
* **Secuencia Causal:** Identificador entero, inmutable, estrictamente monótono creciente y único dentro del Experiment, asignado a cada Order en el instante de su registro por el Broker.
* **Fill / Execution:** Intercambio físico consumado a un precio y cantidad dictaminados por el Matching Engine.
* **Broker:** Gestiona el Portfolio, proyecta costos preventivamente, valida disponibilidad de fondos y procesa Fills.
* **Matching Engine:** Cruza ciegamente Orders Pending contra el OHLCV y emite Fills de manera determinista.
* **OCO (One-Cancels-Other):** Grupo lógico de órdenes mutuamente excluyentes.

### Economía y Contabilidad

* **Position:** Inventario acumulado vivo de un activo (escalar con signo).
* **Lote:** Registro individual de una entrada a la posición, con cantidad, precio de entrada y Margin asociado. Los Lotes se consumen bajo política FIFO.
* **Trade:** Ciclo vital de exposición desde apertura hasta cierre total.
* **Portfolio:** Contenedor maestro de Cash (capital libre), Margin (colateral retenido) y Positions. 
* **Cash:** Capital libre físico.
* **Margin:** Capital retenido como colateral para sostener posiciones abiertas.
* **Realized PnL:** Rentabilidad contable cristalizada.
* **Unrealized PnL:** Valoración M2M (Mark-to-Market) de la Posición viva al último Close conocido.
* **Equity:** Valor total de la cuenta: $Equity = Cash + Margin + UnrealizedPnL$.
* **Friction Model:** Modelo determinista que proyecta costos preventivamente (para validación) y calcula costos reales definitivos (para liquidación).
* **Reserva Preventiva:** Compromiso de capacidad financiera asociado a una Order Pending. No es un movimiento contable; el Cash físico permanece intacto.

---

## 1 · Resumen del negocio

Plataforma de investigación batch offline para simular estrategias de trading. Garantiza determinismo matemático, prevención estructural de look-ahead mediante desfase temporal de evaluación/ejecución, y resolución OHLCV rigurosa mediante modelado de trayectorias adversas canónicas.

---

## 2 · Eventos de dominio

1.  **BacktestIniciado**
2.  **VelaCerrada**
3.  **SignalGenerated**
4.  **OrderRequestCreated**
5.  **OrderRequestRejected**
6.  **OrderRegistered** (Asigna la Secuencia Causal)
7.  **OrderTriggered** (Gatillo alcanzado; modifica condiciones internas)
8.  **OrderExecuted** (Fill)
9.  **OrderCancelled**
10. **PositionChanged**
11. **RealizedPnLRecognized**
12. **TradeClosed**
13. **BacktestFinalizado**

---

## 3 · Modelo de dominio (Relaciones Topológicas)

```text
Backtest
├── En Tiempo N:   Inyecta DataSlice(N) a --> Strategy --> emite --> OrderRequest
├── En Tiempo N+1: Envía Orders Pending y OHLCV(N+1) a --> Matching Engine --> genera --> Fill
└── Actualiza con Fills a --> Broker --> muta --> Position
                                                    ├── deriva --> RealizedPnLRecognized
                                                    └── deriva --> TradeClosed
```

---

## 4 · Reglas de negocio e invariantes (RN)

### Órdenes y Matemáticas de Ejecución

**RN-01 · Transiciones de Orden.**
El estado de una Order Pending solo muta de forma terminal hacia Executed, Cancelled o Rejected. Internamente, una orden condicional puede modificar sus parámetros de ejecución (emitiendo OrderTriggered) sin abandonar el estado Pending.

**RN-02 · Ejecución Atómica.**
Todo Fill satisface el 100% de la Orden. Cero Partial Fills.

**RN-03 · Matemáticas de Cruce y Gaps.**
La evaluación límite es inclusiva ($\ge$, $\le$). El Open es el primer precio observable.
* Si un gap de apertura atraviesa directamente el precio solicitado, la orden ejecuta al Open.
* Si la apertura no la atraviesa, pero la trayectoria simulada del rango (High/Low) la cruza, la orden ejecuta exactamente al precio solicitado.

**RN-04 · Ordenamiento de Ejecuciones Simultáneas (Secuencia Causal).**
Si múltiples órdenes son ejecutadas en el mismo instante lógico (ej. cruzadas por un gap en el Open), el motor procesa la contabilidad en orden estrictamente ascendente de su Secuencia Causal. Esto garantiza que el impacto sobre Cash, Margin, OCO y Position sea completamente determinista y único.

**RN-05 · Exclusividad OCO.**
La determinación de ejecución válida de una rama muta atómicamente a las ramas hermanas a Cancelled en la misma resolución temporal.

**RN-06 · Ciclo de Vida Stop-Limit.**
Nace inactiva. Al ser atravesado su precio Stop, emite OrderTriggered en el instante lógico del cruce y muta a una Limit viva. La máquina de estados es: $Pending(Stop\text{-}Limit) \rightarrow Pending(Limit) \rightarrow Terminal$. En esa misma vela y a partir de ese instante, si el rango de la trayectoria simulada atraviesa el precio Limit, ejecuta y hace Fill.

### Posición y Contabilidad

**RN-07 · Inmutabilidad de Origen.**
Position y Trade mutan EXCLUSIVAMENTE a causa de un Fill. RealizedPnL y TradeClosed son consecuencias contables puras.

**RN-08 · Estructura de Capital y Margin por Lotes.**
* **Margin por Lote:** Cada lote de entrada $k$ retiene un colateral calculado en el instante de su Fill: $Margin_k = Q_k \times Precio\_Fill_k \times Tasa\_Margen$. 
* **Invariante:** El Margin retenido por un lote vivo queda asociado permanentemente a su cantidad y precio de entrada originales. No se recalcula por cambios en el mercado o valoraciones M2M. Permanece fijo hasta que el lote es consumido.
* **Apertura/Aumento:** Mueve fondos físicos desde Cash hacia Margin según la fórmula del lote.
* **Cierre/Reducción (FIFO):** Libera exactamente el Margin original de los lotes consumidos ($Q_{consumido} \times Precio\_Fill_k \times Tasa\_Margen$) devolviéndolo al Cash. El RealizedPnL se suma/resta al Cash.
* **Reversión Cross-Zero:** Libera todo el Margin de la posición vieja (liquidando PnL al Cash), y retiene el nuevo Margin de la posición entrante basándose en su Fill Price.

**RN-09 · Asignación FIFO.**
Las reducciones parciales de posición imputan el PnL y la liberación de Margin contra los lotes más antiguos (FIFO).

**RN-10 · Reversión Cross-Zero.**
Un Fill que invierte la posición cierra el Trade activo (consolidando PnL, rebalanceando Margin) y abre uno nuevo por la cantidad excedente (reteniendo Margin nuevo).

### Resolución, Causalidad y Reproducibilidad

**RN-11 · Peor Escenario Deliberado (Trayectorias Canónicas).**
OHLCV impide conocer la trayectoria real. El dominio define dos trayectorias canónicas artificiales para resolver ambigüedad:
* Trayectoria A: $O \rightarrow H \rightarrow L \rightarrow C$
* Trayectoria B: $O \rightarrow L \rightarrow H \rightarrow C$

El motor **evalúa obligatoriamente ambas trayectorias** (A y B) para la vela. Para cada rama, resuelve íntegramente Fills, cancelaciones, posiciones, contabilidad, costos y M2M. Calcula el Equity final resultante al cierre.
* **Selección:** $Trayectoria\_Oficial = \arg\min(Equity_A,\ Equity_B)$.
* **Desempate:** Si $Equity_A == Equity_B$, se selecciona A.
* **Equivalencia:** Dos trayectorias son matemáticamente equivalentes si producen idéntica secuencia causal de Fills/eventos, misma Position, Cash, Margin y Equity finales.

**RN-12 · Ciclo de Capacidad y Reserva Preventiva (Validación en dos fases).**
La estimación `MarginProyectado + CostoProyectado` constituye exclusivamente una **reserva preventiva**. No altera Cash físico.
* **Cálculo:**
  * Market: $Close(N) \times Cantidad \times Tasa\_Margen$
  * Limit: $Precio\_Limite \times Cantidad \times Tasa\_Margen$
  * Stop: $Precio\_Stop \times Cantidad \times Tasa\_Margen$
  * Stop-Limit: $\max(Precio\_Stop, Precio\_Limite) \times Cantidad \times Tasa\_Margen$
* **Fase 1 (Validación):** Antes de registrar la Order $i$, se calcula:
  $Cash\_Disponible\_Previo = Cash\_Total - \sum_{j \in Pending} (MarginProyectado_j + CostoProyectado_j)$
  La Order $i$ se aprueba si: $Cash\_Disponible\_Previo \ge MarginProyectado_i + CostoProyectado_i$. Un Request rechazado no consume Secuencia Causal ni reserva fondos.
* **Fase 2 (Registro):** Sólo tras aprobarla, la reserva de $i$ se suma a los compromisos. Nunca se cuenta dos veces.
* **Ciclo vital de la reserva:**
  * **Fill:** La reserva preventiva se elimina y se aplican Margin y Costo reales al Cash físico.
  * **Cancelled / OCO Cancelled:** La reserva se libera íntegramente de los compromisos.
  * **OrderTriggered (Stop-Limit → Limit):** La reserva se recalcula con el precio Limit. Esta transición **nunca puede aumentar** el compromiso proyectado, sólo puede mantenerlo o reducirlo.

**RN-13 · Causalidad Temporal y Desfase N / N+1.**
En $N$, Strategy lee DataSlice($N$) y emite Requests. En $N+1$, Matching Engine cruza Orders contra OHLCV($N+1$). Prevención absoluta de look-ahead.

**RN-14 · Rechazo por Contradicción.**
Toda la bolsa de Requests generada en el ciclo $N$ para un activo se evalúa junta. Si contiene un Buy y un Sell, **TODA la bolsa** es rechazada atómicamente.

---

## 5 · Escenarios de aceptación (CU)

### Ciclo Vital y Experimentación
* **CU-01:** Input válido $\rightarrow$ Ejecución $\rightarrow$ Reporte (Success).
* **CU-02:** Dataset corrupto $\rightarrow$ DataInvalid $\rightarrow$ 0 velas procesadas.
* **CU-03:** Longitud $\le$ warmup $\rightarrow$ NotEvaluable $\rightarrow$ 0 Fills.
* **CU-04:** Estrategia Ciega $\rightarrow$ Cash intacto, 0 Trades.
* **CU-05:** $InputHash_A == InputHash_B \Rightarrow ResultHash_A == ResultHash_B$.
* **CU-06 (Look-ahead):** Lectura futura bloqueada $\rightarrow$ Aborto (StrategyError).
* **CU-07 (Fin):** Órdenes Pending se cancelan. Position viva se documenta M2M.

### Fills y Matemáticas de Ejecución
* **CU-08:** Signal Market en $N$ $\rightarrow$ Fill en Open($N+1$).
* **CU-09:** Limit Buy \$100, Open \$90 $\rightarrow$ Fill \$90.
* **CU-10:** Limit Buy \$100, Open \$105, Low \$95 $\rightarrow$ Fill \$100.
* **CU-11:** Limit No Ejecutada $\rightarrow$ Sigue Pending.
* **CU-12:** Stop Sell \$100, Open \$90 $\rightarrow$ Fill \$90 (Peor precio).
* **CU-13:** Stop-Limit Buy \$100/\$102. Vela 95/105/95. Trayectoria simulada detona Stop a \$100 (subiendo), Limit intercepta a \$102 (bajando) $\rightarrow$ Fill \$102.
* **CU-14:** Stop-Limit Gatillado sin Fill Limit $\rightarrow$ Muta a Pending Limit.

### Contabilidad y Casos Complejos
* **CU-15 (Capacidad):** Falla validación preventiva (RN-12) $\rightarrow$ OrderRequestRejected.
* **CU-16:** Request Cancelación en $N$ $\rightarrow$ Orden Cancelled. Reserva liberada.
* **CU-17:** Long 2 a \$50 + Long 8 a \$60. Sell 4 $\rightarrow$ Consume Lote(2x\$50) y Lote(2x\$60). Libera Margin FIFO y liquida PnL FIFO.
* **CU-18:** Cross-Zero $\rightarrow$ Libera todo Margin viejo FIFO, retiene Margin nuevo.
* **CU-19:** OCO Múltiple Ambiguo $\rightarrow$ Motor evalúa trayectorias. Selecciona la de menor Equity final. Rama cruzada hace Fill, hermana se cancela.
* **CU-20:** Contradicción Buy+Sell en ciclo $N$ $\rightarrow$ Rechazo total.

---

## 6 · Casos límite (EC)

* **EC-01 (Igualdad Exacta):** Buy Limit \$100, Low \$100.00 $\rightarrow$ Fill \$100.
* **EC-02 (Ejecución Simultánea):** Órdenes escalonadas a \$90, \$80, \$70; Open en \$60 $\rightarrow$ Cruce al Open. Procesamiento contable estrictamente ordenado por Secuencia Causal (RN-04).
* **EC-03 (Desincronización):** Falla de determinismo $\rightarrow$ Violación crítica del dominio (RN-04/RN-11).
* **EC-04 (Falla Sistémica):** Aborto no manejado $\rightarrow$ InternalCrash. 0 Resultados financieros emitidos.

---

## 7 · Requisitos no funcionales (RNF)

**RNF-01, RNF-02, RNF-03 · Estabilidad Formal y Benchmarks**
* **Escenario de Referencia Inmutable:** 1 Activo, 10 Millones de velas, Estrategia medias móviles $O(1)$, 1,000 órdenes completadas sin OCO, hilo simple secuencial.
* **Estabilidad del Experimento:** Una ejecución válida debe terminar en un estado determinista (RNF-09). Su **memoria máxima es finita y acotada** por las dimensiones simultáneas del dominio (órdenes Pending, Lotes vivos, estado necesario de Strategy), sin exigir retener la historia financiera o el dataset completo en memoria residente. El **tiempo de procesamiento por vela está acotado** causalmente por la cantidad de órdenes Pending evaluadas, eventos y resolución de trayectorias (RN-11), no por el número de vela.
* **Métricas a Medir:** Velas procesadas/segundo, Bytes/Vela procesada, Bytes/Orden concurrente, Pico memoria, Tiempo total.
* **Objetivos:** `[Decisión Técnica Pendiente]`.

**RNF-04 · Speedup paralelo:**
Métrica de eficiencia bajo carga concurrente. `[Decisión Pendiente]`.

**RNF-05 · Precisión Financiera y Regla de Redondeo Final:**
* Exactitud decimétrico-absoluta. Sin pérdida por coma flotante binaria.
* Precisión interna de Precios, Cantidades, Cash, Margin, PnL y Fricciones: 8 decimales.
* Cálculos intermedios: 8 decimales.
* Redondeo exclusivo al final (Half-to-Even a 2 decimales para la moneda).
* **Identidad reportada inmutable:** 
  $Cash_{rep} = Round_{HTE}(Cash_{interno}, 2)$
  $Margin_{rep} = Round_{HTE}(Margin_{interno}, 2)$
  $UnrealizedPnL_{rep} = Round_{HTE}(UnrealizedPnL_{interno}, 2)$
  $Equity_{rep} = Cash_{rep} + Margin_{rep} + UnrealizedPnL_{rep}$
* El $Equity_{rep}$ es la suma estricta de sus componentes redondeados; **ningún componente se ajusta arbitrariamente**. El Equity a 8 decimales sigue disponible para auditoría de la precisión interna.

**RNF-06 · Determinismo:** Igual input produce bit a bit igual output.
**RNF-07 · Aislamiento:** Ausencia absoluta de estado o memoria compartida entre simulaciones.

**RNF-08 · Trazabilidad y Fill Log:**
La simulación es determinísticamente reconstruible mediante la suma de:
* **Estado Canónico Inicial:** Configuración completa (Capital, Margin, Fricciones, Dataset, Strategy). **Invariante Obligatoria:** Todo experimento nace con $Position\_Inicial = 0$, $Margin\_Inicial = 0$ y $Trades = 0$.
* **Fill Log Mínimo:** Cada Fill debe rastrearse inequívocamente a su Order mediante la **Secuencia Causal**. Datos obligatorios por Fill: Secuencia Causal, Dirección, Cantidad, Fill Price, Costo Fricción Real, Timestamp, Tipo de Orden Original.

**RNF-09 · Observabilidad:** Estados exclusivos: `Success`, `StrategyError`, `DataInvalid`, `ConfigInvalid`, `NotEvaluable`, `InternalCrash`.
**RNF-10 · Integridad de falla:** 0 Resultados emitidos si Estado $\neq$ Success.
**RNF-11 · Verificabilidad:** Todas las RN, CU y EC verificables lógicamente sin dependencias de estado externo no controlado.
**RNF-12 · Evolución Controlada:** Fronteras de dominio (Broker, Engine, Strategy, Portfolio) rígidamente aisladas por contratos conceptuales, pudiendo mutar internamente sin requerir cascadas de cambios cruzados.
**RNF-13 · Persistencia Simétrica:** $Deserializar(Serializar(Result)) == Result$. (Formato `[Decisión Técnica Pendiente]`).

---

## 8 · Fuera de alcance
| Descartado | Razón Estructural |
| :--- | :--- |
| **Live Trading** | Dominio es batch estático. |
| **Partial Fills** | Falta nivel 2 de Order Book. |
| **Hedging** | Bloqueo estructural (RN-14). |
| **Posiciones Iniciales** | MVP evalúa desde 0 absoluto (RNF-08). |

## 9 · IDs retirados
RNF-14 (Latencia Interactiva), RNF-15 (Sandbox de Seguridad).
