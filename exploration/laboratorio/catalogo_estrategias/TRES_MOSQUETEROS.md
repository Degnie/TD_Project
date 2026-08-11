# Ficha de Estrategia — Caso 1

## 1. Identificación

- **Nombre**: Tres Mosqueteros
- **Versión**: única (sin variantes registradas)
- **Fecha**: 2026-08-11 (migración a ficha estándar; implementación original de sesiones previas)
- **Estado**: validada (evaluada en Fase 1.5 sobre datasets sintéticos y en Fase 2C sobre dataset real BTC/USDT multi-timeframe)

## 2. Definición lógica

- **Hipótesis de comportamiento**: el color de una vela de referencia (posición fija dentro de un cuadrante de 5 velas) predice el color de la vela siguiente.
- **Señal de entrada**: mercado dividido en cuadrantes fijos de 5 velas, anclados a la posición absoluta en el dataset (`N % 5`), no a un contador interno de la estrategia. La vela 3 del cuadrante (`N % 5 == 2`, 0-indexed) es la vela de referencia; si cierra verde se apuesta Buy, si cierra roja se apuesta Sell. Vela doji (Open == Close) → sin señal.
- **Momento exacto de ejecución**: la señal se evalúa en `N % 5 == 2` y, por el desfase causal RN-13, la orden devuelta por `Observar(N)` se ejecuta contra `Velas[N+1]`.
- **Condición de salida**: apertura con el mismo lado apostado (Buy si se apostó verde, Sell si se apostó roja); cierre con el lado opuesto al de apertura en el ciclo siguiente.
- **Gestión de posiciones**: una operación lógica a la vez; el cálculo del próximo cuadrante (`N % 5 == 2`) sigue evaluándose siempre, exista o no una apuesta en curso resuelta en ese mismo ciclo.
- **Uso de martingala**: sí, hasta `maxMartingalas` reintentos configurables (0, 1 o 2). Si la apuesta pierde y quedan reintentos, se reabre en el ciclo siguiente (`EsperandoReapertura`) con el mismo color apostado.
- **Dependencias temporales**: ninguna dependencia de calendario/sesión; solo posición absoluta `N` dentro del dataset.

## 3. Supuestos de la estrategia

*(Separados de los supuestos del motor — ver `docs/PENDIENTES.md` y `DISENO_FASE2D.md` para los supuestos financieros del motor.)*

- **Patrones esperados**: continuación de color entre la vela de referencia (posición 3 del cuadrante) y la vela siguiente.
- **Condiciones de mercado donde pretende operar**: mercados con estructura direccional o cambios de volatilidad marcados; no asume lateralidad ni ausencia de tendencia.
- **Limitaciones conocidas**: la señal depende de una sola vela de referencia, sin confirmación adicional; vulnerable a ruido sin sesgo direccional.
- **Información utilizada**: únicamente el color (Open vs. Close) de la vela de referencia del cuadrante actual. No usa volumen, indicadores derivados, ni velas fuera del cuadrante.

## 4. Configuración experimental

**Fase 1.5 (sintético)**
- Dataset: escenarios sintéticos de `datasets/market/` (DobleTecho, VolatilidadTrasCalma, RuidoAleatorio, VolatilidadDecreciente, TendenciaBajista, MercadoLateral, SinMovimiento).
- Mercado: sintético, sin instrumento real.
- Parámetros: `maxMartingalas` variable por escenario (M1/M2 reportado en fichas).

**Fase 2C (real)**
- Dataset: `BTCUSDT_2024-01-02_2025-01-02_1m.csv`
- Hash/versionado: SHA256 origen `f1a9dcbe72bd...` (1m base); hashes derivados por timeframe ver tabla de identidad abajo.
- Mercado: BTC/USDT Spot (Binance).
- Timeframes evaluados: 1m, 5m, 15m, 1h, 4h, 1D.
- Ventana temporal: 2024-01-02 a 2025-01-02 (366 días, rango real descargado).
- Parámetros: capital inicial 1000; `AggVersion` = n/a para 1m (origen), 1.0 para timeframes derivados.
- Semilla aleatoria: no aplica (estrategia determinista, sin componente aleatorio).

| TF | TfSha256 (prefijo) |
|----|---------------------|
| 1m | f1a9dcbe72bd... |
| 5m | 7c8dc059320f... |
| 15m | 26ed3d03f494... |
| 1h | f3f120c7c672... |
| 4h | 2be5fba6896a... |
| 1D | 1356dd242e5a... |

## 5. Métricas operativas

### Métricas operativas oficiales

*(Sin interpretación financiera — métricas oficiales de Caso 1.)*

| TF | OpCompletas | Ganadas | Winrate | RachaNegMax | %Martingala | ExpMax | AbiertaAlCierre |
|----|-------------|---------|---------|-------------|-------------|--------|------------------|
| 1m | 82475 | 71816 | 87.08% | 6 | 37.2% | 1 | no |
| 5m | 16829 | 14791 | 87.89% | 4 | 38.4% | 1 | no |
| 15m | 5605 | 4914 | 87.67% | 6 | 39.6% | 1 | sí (capital comprometido 9473.201) |
| 1h | 1380 | 1194 | 86.52% | 3 | 40.7% | 1 | sí (capital comprometido 9480.299) |
| 4h | 350 | 302 | 86.29% | 2 | 38.6% | 1 | sí (capital comprometido 9462.708) |
| 1D | 61 | 54 | 88.52% | 2 | 39.3% | 1 | no |

**Desglose de resolución de intentos** (victoria inicial / victoria M1 / victoria M2 / pérdida agotando intentos):

| TF | VictoriaInicial | VictoriaM1 | VictoriaM2 | PerdioAgotando | %RecuperaciónM1 | %RecuperaciónM2 |
|----|------------------|------------|------------|-----------------|-------------------|-------------------|
| 1m | 41097 | 20396 | 10323 | 10659 | 24.73% | 12.52% |
| 5m | 8337 | 4285 | 2169 | 2038 | 25.46% | 12.89% |
| 15m | 2695 | 1493 | 726 | 691 | 26.64% | 12.95% |
| 1h | 632 | 372 | 190 | 186 | 26.96% | 13.77% |
| 4h | 167 | 95 | 40 | 48 | 27.14% | 11.43% |
| 1D | 30 | 19 | 5 | 7 | 31.15% | 8.20% |

*(`%RecuperaciónM1`/`%RecuperaciónM2` = GanoM1/M2 sobre operaciones completadas del timeframe. Desglosar el `%Martingala` agregado permite distinguir cuánta ganancia depende del primer vs. segundo reintento, relevante porque dos estrategias con el mismo `%Martingala` combinado pueden tener perfiles de riesgo de escalado muy distintos.)*

### Datos derivados del modelo actual (no comparables financieramente)

*(Estos valores corresponden al modelo económico experimental vigente — sizing no definido como modelo financiero, margen pendiente, fricción = 0, interpretación monetaria no validada. No representan rendimiento financiero real ni son comparables entre mercados. Ver Fase 2D y "Decisión sobre Retorno%" en el histórico de revisión del catálogo.)*

**Definiciones**:
- **EquityInicial**: valor de la cuenta (Cash + Margen + PnL no realizado) en el primer punto de la curva de equity del backtest — coincide con el capital inicial configurado (1000) porque ninguna operación se ha resuelto todavía.
- **EquityFinal**: valor de la cuenta en el último punto de la curva de equity, al cierre del dataset — resultado acumulado de **todas** las operaciones de la corrida (intento inicial + M1 + M2, sin distinguir entre ellas).

**Importante — sin desglose por nivel de martingala**: el motor no calcula equity segmentado por M0/M1/M2. `EquityFinal` es un único total de cuenta que mezcla el resultado de operaciones resueltas en cualquier nivel. Lo único desglosado por nivel es el **conteo** de operaciones ganadas en cada uno (tabla de "Desglose de resolución de intentos" arriba) — no hay una atribución de cuánto PnL corresponde a cada nivel. Construir ese desglose requeriría diseñar primero una regla de atribución (ej. cómo repartir el PnL de una operación que perdió en M0, reabrió en M1 y ganó), lo cual está fuera del alcance de esta ficha.

| TF | EquityInicial | EquityFinal | Retorno% |
|----|----------------|-------------|----------|
| 1m | 1000 | 99965.42 | 9896.54% |
| 5m | 1000 | 40470.91 | 3947.09% |
| 15m | 1000 | 19672.58 | 1867.26% |
| 1h | 1000 | -29940.68 | -3094.07% |
| 4h | 1000 | -14331.63 | -1533.16% |
| 1D | 1000 | -5063.81 | -606.38% |

**Observación experimental — equity negativo**: en 1h, 4h y 1D el `EquityFinal` es negativo. No se clasifica como bug: `Estado=Success`, reconciliación financiera coherente y determinismo confirmado en las 12 corridas de Fase 2C. El modelo actual puede producir equity negativo bajo determinadas combinaciones estrategia/timeframe porque representa exposición acumulada bajo tamaño fijo de posición, sin un modelo financiero completo de riesgo/margen (frontera Caso 1/Caso 2, documentada en Fase 2D — no un defecto del motor).

- **Operaciones incompletas**: 0 velas parciales usadas en ningún timeframe (100% de velas disponibles utilizadas en las 6 corridas; ver reporte de completitud Fase 2C).
- **Distribución temporal**: no varía por calendario — la señal depende exclusivamente de la posición modular `N % 5`, no de hora/día.
- **Distribución de rachas negativas** (longitud=conteo): 1m: 2=1085, 3=141, 4=21, 5+=3 (máx=6). 5m: 2=193, 3=24, 4=2 (máx=4). 15m: 2=65, 3=5, 5+=1 (máx=6). 1h: 2=19, 3=4 (máx=3). 4h: 2=3 (máx=2). 1D: 2=1 (máx=2).

## 6. Análisis de comportamiento

- **Escenarios donde funciona** (Fase 1.5, sintético): estructura direccional definida y patrones repetitivos — DobleTecho (+0.89%), VolatilidadTrasCalma (+1.96%).
- **Escenarios donde falla** (Fase 1.5, sintético): ruido sin sesgo direccional (RuidoAleatorio, -0.31%); volatilidad decreciente (-0.92%, la señal de color pierde separación con velas casi doji); tendencia bajista (-0.12%, ruido local contradice el sesgo global).
- **Sensibilidad al timeframe** (Fase 2C, real): retorno positivo y winrate estable en 1m/5m/15m; retorno negativo en 1h/4h/1D pese a winrate operativo similar (86-88% en todos los timeframes) — la caída no viene de peor tasa de acierto, sino de la relación entre el tamaño fijo de posición y el rango de precio absoluto en timeframes más largos (ver Fase 2D, `[SUPUESTO FINANCIERO NO EXPLICITADO]` sobre tamaño fijo de posición).
- **Sensibilidad al dataset**: winrate consistente (85-95%) tanto en sintético como en real, por diseño de la martingala (muchas ganancias chicas + pocas pérdidas grandes) — confirmado como patrón estructural, no específico de un dataset.
- **Dependencia de condiciones específicas**: SinMovimiento (todas las velas dojis) → 0 operaciones en ambos casos; comportamiento correcto (la estrategia reconoce que está fuera de su dominio de señal, no fuerza operaciones).

## 7. Escenarios de falla

- **Falla lógica**: ninguna detectada — el mecanismo de señal, apertura, martingala y cierre opera según lo diseñado en las 12 corridas de Fase 2C (`Estado=Success`, reconciliación OK).
- **Falla estadística**: la señal de una sola vela pierde poder predictivo en ausencia de sesgo direccional (RuidoAleatorio) o cuando el rango intra-vela se reduce (velas casi doji en volatilidad decreciente).
- **Falla por régimen de mercado**: mercados sin tendencia neta o de baja volatilidad degradan la señal, sin ser un fallo del motor.
- **Falla por supuesto incorrecto**: el retorno% negativo en timeframes largos (1h/4h/1D) no es una falla de la estrategia ni del motor — es consecuencia del supuesto financiero no explicitado de tamaño fijo de posición (documentado en Fase 2D), pendiente de resolución bajo un futuro Caso 2.
- **Falla por implementación**: ninguna — corregido el bug de rendimiento O(n²) de `BacktestRunner` (ver `CHANGELOG.md`), las 12 corridas completan con determinismo verificado y reconciliación financiera OK.

## 8. Conclusión experimental

La estrategia está correctamente representada por el motor: ejecuta su lógica de cuadrantes fijos, martingala y cierre exactamente como está definida en `EstrategiaTresMosqueteros.cs`, con reconciliación financiera OK y determinismo verificado en las 12 combinaciones estrategia×timeframe evaluadas. El comportamiento observado es consistente entre datasets sintéticos y reales: winrate estable (~86-88%) impulsado por el diseño de la martingala, con sensibilidad marcada a la estructura direccional del mercado (mejor en tendencia/cambios de volatilidad, peor en ruido sin sesgo o lateralidad). La divergencia de retorno% entre timeframes cortos y largos no refleja una diferencia de calidad de señal (el winrate se mantiene estable) sino un efecto del modelo de posición actual, explícitamente fuera del alcance de interpretación financiera de Caso 1.
