# Resultado — Calibración de UmbralSesgoDI V1

Estado: **evidencia de calibración — Fase 1.4-B, Paso 3-A (ejecución de D-032)**.
Método ejecutado exactamente como fue aprobado en
`DEFINICION_VALOR_UMBRAL_SESGO_DI_V1.md §1`: mediana de `|DI+-DI-|/(DI+ + DI-)` sobre
la zona `ADX < 25` del dataset congelado. Ninguna estrategia participó en este cálculo
(D-016). El valor obtenido no fue ajustado tras calcularse.

## Identidad

- **Dataset**: `BTCUSDT_2024-01-02_2025-01-02` (BTC/USDT Spot, Binance)
- **Timeframes**: 1m, 5m, 15m, 1h, 4h, 1D (mismo subconjunto ya usado en Fase 1.4-A)
- **Versión del cálculo**: D-032 v1 — mediana sobre zona `ADX < 25`, `PeriodoAdx = 14`

## Método

- **Fórmula**: `SesgoDI = |DI+ - DI-| / (DI+ + DI-)`, calculada sobre suavizado de Wilder
  (idéntico a `ClasificadorAdxExperimental.cs`), restringida a ventanas con `ADX < 25`.
- **Tratamiento de división por cero**: ventanas con `TR_suavizado = 0` (rango verdadero
  nulo) o `DI+ + DI- = 0` se excluyen de la serie — misma regla ya usada en
  `ClasificadorAdxExperimental.cs`, no se introduce ninguna excepción nueva.
- **Valores faltantes**: la ventana de calentamiento (`2 × PeriodoAdx` primeras velas)
  no produce muestra — no hay ADX válido todavía, consistente con
  `PARAMETRIZACION_CLASIFICADOR_REGIMEN_V1.md §3`.
- **Definición exacta de muestra**: una muestra = una ventana (vela) con `ADX` válido y
  `ADX < 25`; el conjunto de muestras es toda la zona "sin tendencia" del timeframe.

## Resultado por timeframe

| Timeframe | Muestras (ADX<25) | Mediana SesgoDI |
|---|---|---|
| 1m | 302318 | 0.156637 |
| 5m | 62689 | 0.150297 |
| 15m | 19312 | 0.149084 |
| 1h | 4253 | 0.141038 |
| 4h | 913 | 0.161702 |
| 1D | 178 | 0.193388 |

## Valor propuesto de UmbralSesgoDI

Mediana de las 6 medianas por timeframe (mismo estadístico aplicado una segunda vez para
obtener un único valor aplicable a todos los timeframes, sin elegir un timeframe
"representativo" a mano — consistente con la decisión de periodo uniforme entre escalas,
`PARAMETRIZACION_CLASIFICADOR_REGIMEN_V1.md §4`).

**UmbralSesgoDI (propuesto) = 0.153467**

Estado: **PROPUESTO, no oficial** — este valor es la salida directa del método aprobado
(D-032), no ha sido editado ni redondeado. Su congelamiento formal como parte de
`ClasificadorRegimenV1` requiere aprobación explícita adicional (Paso 3-B).

---

## Validación posterior — distribución resultante con el valor propuesto

*(Observación, no criterio de ajuste — D-032: la distribución NO se usa para modificar
el valor obtenido por el método. Señales de alerta según
`DEFINICION_VALOR_UMBRAL_SESGO_DI_V1.md §3`: % Ambiguo < 1% o > 50%, o fragmentación
desproporcionada de Ambiguo frente a Lateral.)*

| Timeframe | Alcista % | Bajista % | Lateral % | Ambiguo % | Ventanas |
|---|---|---|---|---|---|
| 1m | 20.71% | 21.92% | 28.19% | 29.18% | 527013 |
| 5m | 19.39% | 21.12% | 30.30% | 29.19% | 105381 |
| 15m | 21.11% | 23.89% | 28.14% | 26.87% | 35109 |
| 1h | 25.23% | 26.21% | 26.04% | 22.53% | 8757 |
| 4h | 30.43% | 27.48% | 20.10% | 21.99% | 2169 |
| 1D | 27.73% | 19.76% | 20.94% | 31.56% | 339 |
