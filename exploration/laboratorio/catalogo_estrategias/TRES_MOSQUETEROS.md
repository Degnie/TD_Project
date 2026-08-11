# Ficha de Estrategia Experimental — Tres Mosqueteros

Plantilla: Estrategia Experimental v1.0 (Fase 1.1, decisión de auditoría 2026-08-11).
Migrada desde la versión previa de esta ficha (Fase 1.0) sin alterar ningún dato — solo
reorganización de contenido ya verificado y separación explícita de hipótesis vs. resultados
observados (Categoría D).

---

# Identificación

- **Nombre**: Tres Mosqueteros
- **Versión**: v1.0 (única, sin variantes registradas)
- **Estado**: Validada (evaluada en Fase 1.5 sobre datasets sintéticos y en Fase 2C sobre dataset real BTC/USDT multi-timeframe)
- **Tipo**: Patrón (decisión D-003, Fase 1.1) — genera su señal mediante una regla determinística aplicada sobre una secuencia temporal fija de velas (estructura de cuadrantes), no mediante tendencia global, indicadores direccionales ni niveles de soporte/resistencia. El componente estadístico (winrate, distribución de rachas) pertenece al análisis experimental posterior, no a la construcción de la señal.

---

# Definición lógica

**Descripción funcional**: no intenta predecir dirección absoluta del mercado, sino continuación de color dentro de una estructura temporal fija. El mercado se divide en cuadrantes fijos de 5 velas, anclados a la posición absoluta en el dataset (`N % 5`), no a un contador interno de la estrategia.

**Entrada**:
- Condición: la vela 3 del cuadrante (`N % 5 == 2`, 0-indexed) es la vela de referencia; si cierra verde se apuesta Buy, si cierra roja se apuesta Sell. Vela doji (Open == Close) → sin señal.
- Vela utilizada: vela 3 del cuadrante actual (color Open vs. Close).
- Momento exacto: la señal se evalúa en `N % 5 == 2` y, por el desfase causal RN-13, la orden devuelta por `Observar(N)` se ejecuta contra `Velas[N+1]`.
- Datos requeridos: únicamente el color de la vela de referencia. No usa volumen, indicadores derivados ni velas fuera del cuadrante.

**Salida**:
- Condición de cierre: apertura con el mismo lado apostado (Buy si se apostó verde, Sell si se apostó roja); cierre con el lado opuesto al de apertura en el ciclo siguiente.
- Resultado esperado: acierto si la vela siguiente mantiene el color apostado.
- Resolución: una operación lógica a la vez; el cálculo del próximo cuadrante (`N % 5 == 2`) sigue evaluándose siempre, exista o no una apuesta en curso resuelta en ese mismo ciclo.

**Gestión de intentos**: martingala hasta `maxMartingalas` reintentos configurables (M0/M1/M2, evaluado con máximo 2). Si la apuesta pierde y quedan reintentos, se reabre en el ciclo siguiente (`EsperandoReapertura`) con el mismo color apostado.

---

# Supuestos experimentales

*(Separados de los supuestos del motor — ver `docs/PENDIENTES.md` y `DISENO_FASE2D.md` para los supuestos financieros del motor.)*

- Velas cerradas: la señal solo usa el color (Open vs. Close) de velas ya cerradas, nunca proyecta sobre la vela en curso.
- UTC: dataset y agregación multi-timeframe bajo zona horaria UTC exclusiva (`DISENO_FASE2.md`).
- Sin costes reales: `CostoFriccionReal` no está alimentado en las corridas del laboratorio.
- Sin ejecución financiera real: capital, margen y sizing corresponden al modelo de posición experimental (Fase 2D), no a un modelo financiero validado.
- Sin dependencia de calendario/sesión: solo posición absoluta `N` dentro del dataset.

**Dataset**: `BTCUSDT_2024-01-02_2025-01-02_1m.csv` (real, Fase 2C) y escenarios sintéticos de `datasets/market/` (Fase 1.5). Ver "Configuración experimental" para el detalle completo.

**Timeframes**: 1m, 5m, 15m, 1h, 4h, 1D (subconjunto evaluado por el motor en Fase 2C; los 13 timeframes oficiales existen como datos, ver `baseline/BASELINE_EXPERIMENTAL_V1.md`).

**Configuración**:

*Fase 1.5 (sintético)*
- Dataset: escenarios sintéticos de `datasets/market/` (DobleTecho, VolatilidadTrasCalma, RuidoAleatorio, VolatilidadDecreciente, TendenciaBajista, MercadoLateral, SinMovimiento).
- Mercado: sintético, sin instrumento real.
- Parámetros: `maxMartingalas` variable por escenario (M1/M2 reportado en fichas).

*Fase 2C (real)*
- Dataset: `BTCUSDT_2024-01-02_2025-01-02_1m.csv`
- Hash/versionado: SHA256 origen `f1a9dcbe72bd...` (1m base, verificado por recomputación directa en Fase 1.0); hashes derivados por timeframe ver tabla abajo.
- Mercado: BTC/USDT Spot (Binance).
- Ventana temporal: 2024-01-02 a 2025-01-02 (366 días, rango real descargado).
- Capital inicial: 1000. Tamaño de operación: 1 (fijo). `AggVersion` = n/a para 1m (origen), 1.0 para timeframes derivados.
- Semilla aleatoria: no aplica (estrategia determinista, sin componente aleatorio).

| TF | TfSha256 (prefijo) |
|----|---------------------|
| 1m | f1a9dcbe72bd... |
| 5m | 7c8dc059320f... |
| 15m | 26ed3d03f494... |
| 1h | f3f120c7c672... |
| 4h | 2be5fba6896a... |
| 1D | 1356dd242e5a... |

---

# Métricas evaluadas

**Operaciones**:

| TF | OpCompletas | Ganadas | Winrate | RachaNegMax | %Martingala | ExpMax | AbiertaAlCierre |
|----|-------------|---------|---------|-------------|-------------|--------|------------------|
| 1m | 82475 | 71816 | 87.08% | 6 | 37.2% | 1 | no |
| 5m | 16829 | 14791 | 87.89% | 4 | 38.4% | 1 | no |
| 15m | 5605 | 4914 | 87.67% | 6 | 39.6% | 1 | sí (capital comprometido 9473.201) |
| 1h | 1380 | 1194 | 86.52% | 3 | 40.7% | 1 | sí (capital comprometido 9480.299) |
| 4h | 350 | 302 | 86.29% | 2 | 38.6% | 1 | sí (capital comprometido 9462.708) |
| 1D | 61 | 54 | 88.52% | 2 | 39.3% | 1 | no |

**Resolución de intentos** (dependencia de escalado — victoria inicial / M1 / M2 / pérdida agotando intentos):

| TF | VictoriaInicial | VictoriaM1 | VictoriaM2 | PerdioAgotando | %RecuperaciónM1 | %RecuperaciónM2 |
|----|------------------|------------|------------|-----------------|-------------------|-------------------|
| 1m | 41097 | 20396 | 10323 | 10659 | 24.73% | 12.52% |
| 5m | 8337 | 4285 | 2169 | 2038 | 25.46% | 12.89% |
| 15m | 2695 | 1493 | 726 | 691 | 26.64% | 12.95% |
| 1h | 632 | 372 | 190 | 186 | 26.96% | 13.77% |
| 4h | 167 | 95 | 40 | 48 | 27.14% | 11.43% |
| 1D | 30 | 19 | 5 | 7 | 31.15% | 8.20% |

*(`%RecuperaciónM1`/`%RecuperaciónM2` = GanoM1/M2 sobre operaciones completadas del timeframe. Desglosar el `%Martingala` agregado permite distinguir cuánta ganancia depende del primer vs. segundo reintento, relevante porque dos estrategias con el mismo `%Martingala` combinado pueden tener perfiles de riesgo de escalado muy distintos.)*

**Distribución de rachas negativas** (longitud=conteo): 1m: 2=1085, 3=141, 4=21, 5+=3 (máx=6). 5m: 2=193, 3=24, 4=2 (máx=4). 15m: 2=65, 3=5, 5+=1 (máx=6). 1h: 2=19, 3=4 (máx=3). 4h: 2=3 (máx=2). 1D: 2=1 (máx=2).

**Completitud del dataset usado**: 0 velas parciales usadas en ningún timeframe (100% de velas disponibles utilizadas en las 6 corridas; ver reporte de completitud Fase 2C).

**Distribución temporal**: no varía por calendario — la señal depende exclusivamente de la posición modular `N % 5`, no de hora/día.

---

# Hipótesis experimental

*(Solo hipótesis — separado de los resultados observados, ver sección siguiente.)*

**Comportamiento esperado**: continuación de color entre la vela de referencia (posición 3 del cuadrante) y la vela siguiente, en mercados con estructura direccional o cambios de volatilidad marcados.

**Escenarios favorables (hipótesis previa)**: estructura direccional definida y patrones repetitivos; cambios de régimen de volatilidad.

**Escenarios de fallo (hipótesis previa)**:
- Ruido sin sesgo direccional: la señal de una sola vela no debería tener ventaja cuando no hay estructura direccional que capturar.
- Volatilidad decreciente: al reducirse el rango intra-vela, la señal de color pierde separación (velas casi doji), degradando la confiabilidad de la referencia.
- Secuencias largas de velas del mismo color contrario al apostado.

---

# Resultados observados

*(Completado tras ejecutar Fase 1.5 y Fase 2C — separado de la hipótesis anterior.)*

**Fase 1.5 (sintético)**:
- Funcionó según lo esperado en: DobleTecho (+0.89%), VolatilidadTrasCalma (+1.96%) — estructura direccional y patrones repetitivos.
- Falló según lo esperado en: RuidoAleatorio (-0.31%), VolatilidadDecreciente (-0.92%), TendenciaBajista (-0.12%, el ruido local alrededor de la pendiente generó velas en contra del sesgo global — matiz no anticipado exactamente en la hipótesis, pero consistente con "ruido sin sesgo").
- SinMovimiento: 0 operaciones — comportamiento correcto, la estrategia reconoce que está fuera de su dominio de señal (todas las velas dojis) y no fuerza operaciones.

**Fase 2C (real, BTC/USDT)**:
- Retorno positivo y winrate estable en 1m/5m/15m; retorno negativo en 1h/4h/1D pese a winrate operativo similar (86-88% en todos los timeframes). Este hallazgo **no estaba anticipado en la hipótesis previa** — la caída de retorno en timeframes largos no viene de peor tasa de acierto, sino de la relación entre el tamaño fijo de posición y el rango de precio absoluto (efecto del modelo de posición, `[SUPUESTO FINANCIERO NO EXPLICITADO]`, Fase 2D).
- Winrate consistente (85-95%) tanto en sintético como en real, por diseño de la martingala (muchas ganancias chicas + pocas pérdidas grandes) — confirmado como patrón estructural, no específico de un dataset.

**Datos derivados del modelo actual (no comparables financieramente)**:

*(Estos valores corresponden al modelo económico experimental vigente — sizing no definido como modelo financiero, margen pendiente, fricción = 0, interpretación monetaria no validada. No representan rendimiento financiero real ni son comparables entre mercados.)*

- **EquityInicial**: valor de la cuenta (Cash + Margen + PnL no realizado) en el primer punto de la curva de equity — coincide con el capital inicial configurado (1000) porque ninguna operación se ha resuelto todavía.
- **EquityFinal**: valor de la cuenta en el último punto de la curva de equity, al cierre del dataset — resultado acumulado de todas las operaciones de la corrida (inicial + M1 + M2, sin distinguir entre ellas; el motor no calcula equity segmentado por nivel de martingala).

| TF | EquityInicial | EquityFinal | Retorno% |
|----|----------------|-------------|----------|
| 1m | 1000 | 99965.42 | 9896.54% |
| 5m | 1000 | 40470.91 | 3947.09% |
| 15m | 1000 | 19672.58 | 1867.26% |
| 1h | 1000 | -29940.68 | -3094.07% |
| 4h | 1000 | -14331.63 | -1533.16% |
| 1D | 1000 | -5063.81 | -606.38% |

**Observación experimental — equity negativo**: en 1h, 4h y 1D el `EquityFinal` es negativo. No se clasifica como bug: `Estado=Success`, reconciliación financiera coherente y determinismo confirmado (3 corridas idénticas, Fase 1.0). El modelo actual puede producir equity negativo bajo determinadas combinaciones estrategia/timeframe porque representa exposición acumulada bajo tamaño fijo de posición, sin un modelo financiero completo de riesgo/margen (frontera Caso 1/Caso 2).

---

# Limitaciones

- Falla lógica: ninguna detectada — el mecanismo de señal, apertura, martingala y cierre opera según lo diseñado en las 12 corridas de Fase 2C (`Estado=Success`, reconciliación OK).
- Falla estadística: la señal de una sola vela pierde poder predictivo en ausencia de sesgo direccional o cuando el rango intra-vela se reduce.
- Falla por régimen de mercado: mercados sin tendencia neta o de baja volatilidad degradan la señal, sin ser un fallo del motor.
- Falla por supuesto incorrecto: el retorno% negativo en timeframes largos no es una falla de la estrategia ni del motor — es consecuencia del supuesto financiero no explicitado de tamaño fijo de posición (Fase 2D), pendiente de resolución bajo un futuro Caso 2.
- Falla por implementación: ninguna — corregido el bug de rendimiento O(n²) de `BacktestRunner` (ver `CHANGELOG.md`), las 12 corridas completan con determinismo verificado y reconciliación financiera OK.

---

# Conclusión experimental

La estrategia está correctamente representada por el motor: ejecuta su lógica de cuadrantes fijos, martingala y cierre exactamente como está definida en `EstrategiaTresMosqueteros.cs`, con reconciliación financiera OK y determinismo verificado en las 12 combinaciones estrategia×timeframe evaluadas. El comportamiento observado es consistente entre datasets sintéticos y reales: winrate estable (~86-88%) impulsado por el diseño de la martingala, con sensibilidad marcada a la estructura direccional del mercado (mejor en tendencia/cambios de volatilidad, peor en ruido sin sesgo o lateralidad). La divergencia de retorno% entre timeframes cortos y largos no refleja una diferencia de calidad de señal (el winrate se mantiene estable) sino un efecto del modelo de posición actual, explícitamente fuera del alcance de interpretación financiera de Caso 1.
