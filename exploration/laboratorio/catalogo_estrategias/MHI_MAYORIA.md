# Ficha de Estrategia — Caso 1

## 1. Identificación

- **Nombre**: MHI Mayoría
- **Versión**: única (sin variantes registradas)
- **Fecha**: 2026-08-11 (migración a ficha estándar; implementación original de sesiones previas)
- **Estado**: validada (evaluada en Fase 1.5 sobre datasets sintéticos y en Fase 2C sobre dataset real BTC/USDT multi-timeframe)

## 2. Definición lógica

- **Hipótesis de comportamiento**: el color mayoritario de las últimas 3 velas de un cuadrante predice el color de la primera vela del cuadrante siguiente.
- **Señal de entrada**: mercado dividido en cuadrantes fijos de 5 velas, anclados a la posición absoluta en el dataset (`N % 5`), no a ventana deslizante. Al cerrar la vela 5 del cuadrante (`N % 5 == 4`, 0-indexed) se toman las velas 3, 4 y 5 de ese mismo cuadrante (`N-2, N-1, N`) y se cuenta el color mayoritario. Si hay algún doji entre las 3 velas o empate 1.5/1.5 (defensivo, no debería ocurrir con 3 velas sin doji), no hay señal.
- **Momento exacto de ejecución**: la señal se calcula en `N % 5 == 4` (vela 5 recién cerrada) y, por el desfase causal RN-13, la orden se ejecuta contra `Velas[N+1]` — la vela 1 del cuadrante siguiente.
- **Condición de salida**: apertura con el lado de la mayoría apostada; cierre con el lado opuesto al de apertura en el ciclo siguiente.
- **Gestión de posiciones**: una operación lógica a la vez; una sola evaluación por cuadrante — nunca se recalcula la mayoría en cada vela disponible.
- **Uso de martingala**: sí, hasta `maxMartingalas` reintentos configurables (0, 1 o 2), idéntico mecanismo de reapertura diferida que Tres Mosqueteros.
- **Dependencias temporales**: ninguna dependencia de calendario/sesión; solo posición absoluta `N` dentro del dataset.

## 3. Supuestos de la estrategia

*(Separados de los supuestos del motor — ver `docs/PENDIENTES.md` y `DISENO_FASE2D.md` para los supuestos financieros del motor.)*

- **Patrones esperados**: continuación de la mayoría de color observada en las velas 3-5 del cuadrante hacia la vela 1 del cuadrante siguiente.
- **Condiciones de mercado donde pretende operar**: mercados con expansión o cambios de régimen de volatilidad; una mayoría de 3 velas busca capturar tramos donde el rango cambia, no solo el color de una vela aislada.
- **Limitaciones conocidas**: requiere las 3 velas sin doji para tener señal; en mercados de baja volatilidad con muchas velas casi-doji, pierde frecuencia de señal.
- **Información utilizada**: color (Open vs. Close) de las 3 últimas velas del cuadrante. No usa volumen, indicadores derivados, ni información fuera del cuadrante actual.

## 4. Configuración experimental

**Fase 1.5 (sintético)**
- Dataset: escenarios sintéticos de `datasets/market/` (VolatilidadExtrema, VolatilidadTrasCalma, VolatilidadDecreciente, MercadoLateral, DobleTecho, SinMovimiento).
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
| 1m | 79443 | 69073 | 86.95% | 5 | 38.3% | 1 | no |
| 5m | 16795 | 14721 | 87.65% | 4 | 38.7% | 1 | sí (capital comprometido 9460.128) |
| 15m | 5551 | 4829 | 87.01% | 4 | 40.0% | 1 | no |
| 1h | 1392 | 1211 | 87.00% | 4 | 39.7% | 1 | no |
| 4h | 350 | 308 | 88.00% | 2 | 34.0% | 1 | sí (capital comprometido 9462.708) |
| 1D | 57 | 48 | 84.21% | 2 | 31.6% | 1 | no |

**Desglose de resolución de intentos** (victoria inicial / victoria M1 / victoria M2 / pérdida agotando intentos):

| TF | VictoriaInicial | VictoriaM1 | VictoriaM2 | PerdioAgotando | %RecuperaciónM1 | %RecuperaciónM2 |
|----|------------------|------------|------------|-----------------|-------------------|-------------------|
| 1m | 38680 | 20294 | 10099 | 10370 | 25.54% | 12.71% |
| 5m | 8226 | 4381 | 2114 | 2074 | 26.08% | 12.59% |
| 15m | 2609 | 1472 | 748 | 722 | 26.52% | 13.48% |
| 1h | 658 | 370 | 183 | 181 | 26.58% | 13.15% |
| 4h | 189 | 73 | 46 | 42 | 20.86% | 13.14% |
| 1D | 30 | 11 | 7 | 9 | 19.30% | 12.28% |

*(`%RecuperaciónM1`/`%RecuperaciónM2` = GanoM1/M2 sobre operaciones completadas del timeframe. Desglosar el `%Martingala` agregado permite distinguir cuánta ganancia depende del primer vs. segundo reintento, relevante porque dos estrategias con el mismo `%Martingala` combinado pueden tener perfiles de riesgo de escalado muy distintos.)*

### Datos derivados del modelo actual (no comparables financieramente)

*(Estos valores corresponden al modelo económico experimental vigente — sizing no definido como modelo financiero, margen pendiente, fricción = 0, interpretación monetaria no validada. No representan rendimiento financiero real ni son comparables entre mercados. Ver Fase 2D y "Decisión sobre Retorno%" en el histórico de revisión del catálogo.)*

**Definiciones**:
- **EquityInicial**: valor de la cuenta (Cash + Margen + PnL no realizado) en el primer punto de la curva de equity del backtest — coincide con el capital inicial configurado (1000) porque ninguna operación se ha resuelto todavía.
- **EquityFinal**: valor de la cuenta en el último punto de la curva de equity, al cierre del dataset — resultado acumulado de **todas** las operaciones de la corrida (intento inicial + M1 + M2, sin distinguir entre ellas).

**Importante — sin desglose por nivel de martingala**: el motor no calcula equity segmentado por M0/M1/M2. `EquityFinal` es un único total de cuenta que mezcla el resultado de operaciones resueltas en cualquier nivel. Lo único desglosado por nivel es el **conteo** de operaciones ganadas en cada uno (tabla de "Desglose de resolución de intentos" arriba) — no hay una atribución de cuánto PnL corresponde a cada nivel. Construir ese desglose requeriría diseñar primero una regla de atribución (ej. cómo repartir el PnL de una operación que perdió en M0, reabrió en M1 y ganó), lo cual está fuera del alcance de esta ficha.

| TF | EquityInicial | EquityFinal | Retorno% |
|----|----------------|-------------|----------|
| 1m | 1000 | 22514.40 | 2151.44% |
| 5m | 1000 | 16981.64 | 1598.16% |
| 15m | 1000 | -12657.32 | -1365.73% |
| 1h | 1000 | -18438.48 | -1943.85% |
| 4h | 1000 | -498.19 | -149.82% |
| 1D | 1000 | -8170.30 | -917.03% |

**Observación experimental — equity negativo**: en 15m, 1h, 4h y 1D el `EquityFinal` es negativo. No se clasifica como bug: `Estado=Success`, reconciliación financiera coherente y determinismo confirmado en las 12 corridas de Fase 2C. El modelo actual puede producir equity negativo bajo determinadas combinaciones estrategia/timeframe porque representa exposición acumulada bajo tamaño fijo de posición, sin un modelo financiero completo de riesgo/margen (frontera Caso 1/Caso 2, documentada en Fase 2D — no un defecto del motor).

- **Operaciones incompletas**: 0 velas parciales usadas en ningún timeframe (100% de velas disponibles utilizadas en las 6 corridas; ver reporte de completitud Fase 2C).
- **Distribución temporal**: no varía por calendario — la señal depende exclusivamente de la posición modular `N % 5`, no de hora/día.
- **Distribución de rachas negativas** (longitud=conteo): 1m: 2=1034, 3=159, 4=20, 5+=4 (máx=5). 5m: 2=182, 3=24, 4=4 (máx=4). 15m: 2=68, 3=10, 4=1 (máx=4). 1h: 2=27, 3=2, 4=1 (máx=4). 4h: 2=5 (máx=2). 1D: 2=1 (máx=2).

## 6. Análisis de comportamiento

- **Escenarios donde funciona** (Fase 1.5, sintético): expansión y cambios de volatilidad — VolatilidadExtrema (+2.35%), VolatilidadTrasCalma (+3.81%), VolatilidadDecreciente (+1.08%). La mayoría de 3 velas captura mejor los tramos de cambio de régimen que la señal de una sola vela.
- **Escenarios donde falla** (Fase 1.5, sintético): lateralidad pura (MercadoLateral, -0.18%) — sin tendencia neta, la mayoría de 3 velas tampoco anticipa reversión ni continuidad.
- **Sensibilidad al timeframe** (Fase 2C, real): winrate estable (84-88%) en todos los timeframes; retorno positivo en 1m/5m, negativo en 15m/1h/4h/1D. Igual que en Tres Mosqueteros, la caída de retorno no correlaciona con caída de winrate — apunta al mismo supuesto de tamaño fijo de posición, no a degradación de la señal.
- **Sensibilidad al dataset**: winrate consistente (85-95%) tanto en sintético como en real, por diseño de la martingala — mismo patrón estructural que Tres Mosqueteros.
- **Dependencia de condiciones específicas**: SinMovimiento (todas las velas dojis) → 0 operaciones; comportamiento correcto, la estrategia reconoce ausencia de señal válida (sin 3 velas de color definido) y no fuerza operaciones.

## 7. Escenarios de falla

- **Falla lógica**: ninguna detectada — el mecanismo de mayoría, apertura, martingala y cierre opera según lo diseñado en las 12 corridas de Fase 2C (`Estado=Success`, reconciliación OK).
- **Falla estadística**: en mercados sin tendencia neta (MercadoLateral) la mayoría de 3 velas no aporta ventaja predictiva sobre el color siguiente.
- **Falla por régimen de mercado**: mercados de baja volatilidad con velas casi-doji reducen la frecuencia de señal válida (requiere 3 velas con color definido sin empate).
- **Falla por supuesto incorrecto**: igual que Tres Mosqueteros, el retorno% negativo en timeframes largos no es falla de estrategia ni de motor, sino consecuencia del supuesto de tamaño fijo de posición (Fase 2D), pendiente de resolución en un futuro Caso 2.
- **Falla por implementación**: ninguna — mismas correcciones de rendimiento de `BacktestRunner` aplican; determinismo y reconciliación verificados en las 12 corridas.

## 8. Conclusión experimental

La estrategia está correctamente representada por el motor: ejecuta su lógica de mayoría de 3 velas por cuadrante, martingala y cierre exactamente como está definida en `EstrategiaMhiMayoria.cs`, con reconciliación financiera OK y determinismo verificado en las 12 combinaciones estrategia×timeframe evaluadas. El comportamiento observado es consistente entre datasets sintéticos y reales: winrate estable (~84-88%) impulsado por el diseño de la martingala, con mejor desempeño relativo en escenarios de expansión/cambio de volatilidad que en lateralidad pura. Al igual que en Tres Mosqueteros, la divergencia de retorno% entre timeframes no refleja diferencia de calidad de señal, sino el efecto del modelo de posición actual — explícitamente fuera del alcance de interpretación financiera de Caso 1.
