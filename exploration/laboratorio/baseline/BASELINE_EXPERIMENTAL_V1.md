# Baseline Experimental V1

Estado: **congelado — Fase 1.0 del Caso 1**. Este documento fija la línea base experimental
reproducible y auditable sobre la que se construirá la evolución del laboratorio de estrategias.
No introduce funcionalidad nueva; verifica y documenta el estado ya existente.

## Identidad

- **Fecha**: 2026-08-11
- **Commit utilizado**: `b447f3fa7a25ffd407fd3575390f1a897a5c9026` (`fix: RN-11 trayectorias canonicas A/B para Stop-Limit, RN-10 cross-zero y exponer CostoFriccionReal`)
- **Versión motor**: .NET 8.0.423 (SDK), motor de dominio en `src/` tal como quedó en el commit anterior — sin cambios de arquitectura, contratos ni lógica financiera durante esta fase.

## Dataset

- **Nombre**: `BTCUSDT_2024-01-02_2025-01-02_1m.csv`
- **Hash SHA-256**: `f1a9dcbe72bdbca65c5a7de55c776c209a63f8b3ecd93c59a5fca958e4ebded4` — recomputado directamente sobre el archivo en disco durante esta fase y coincide exactamente con el valor declarado en `metadata.json` (verificación de integridad, no solo lectura del metadata).
- **Periodo**: 2024-01-02T00:00:00Z a 2025-01-02T00:00:00Z (ventana móvil de 366 días UTC — 2024 fue bisiesto; no es un año calendario. El nombre del archivo refleja el rango real descargado, decisión tomada en Fase 2A por trazabilidad sobre conveniencia de nombre).
- **Fuente**: Binance REST API (`/api/v3/klines`), BTC/USDT Spot.
- **Características**: 527,040 velas de 1 minuto, 0 huecos, 0 duplicados, 0 errores, estado `APTO_PARA_CONGELAR` (validado en Fase 2A). Zona horaria UTC exclusiva en todo el sistema (política definida en `DISENO_FASE2.md`).

## Timeframes

Los 13 timeframes oficiales del laboratorio existen, están generados desde el mismo dataset base
(`sourceSha256` idéntico en las 12 metadata derivadas) y con anclaje calendario-UTC estricto
(`aggregationVersion: 1.0`, `DISENO_FASE2B.md`).

| Timeframe | Estado | Validado |
|-----------|--------|----------|
| 1m | Dataset base (origen, no derivado) | ✓ hash recomputado, 527040 velas |
| 2m | Generado, 0 velas parciales | ✓ |
| 5m | Generado, 0 velas parciales | ✓ |
| 10m | Generado, 0 velas parciales | ✓ |
| 15m | Generado, 0 velas parciales | ✓ |
| 30m | Generado, 0 velas parciales | ✓ |
| 1h | Generado, 0 velas parciales | ✓ |
| 2h | Generado, 0 velas parciales | ✓ |
| 4h | Generado, 0 velas parciales | ✓ |
| 8h | Generado, 0 velas parciales | ✓ |
| 12h | Generado, 0 velas parciales | ✓ |
| 1D | Generado, 0 velas parciales (366 velas, coincide exacto con año bisiesto) | ✓ |
| 1W | Generado, **2 velas parciales** (primera y última semana del rango, borde de calendario ISO) | ✓ — comportamiento esperado, documentado desde Fase 2B, no es una anomalía |

**Exclusión de velas parciales en backtest**: verificada — el reporte de completitud de Fase 2C
confirma `excluidas=0` en los 6 timeframes evaluados por el motor (1m, 5m, 15m, 1h, 4h, 1D), es
decir, el 100% de las velas disponibles en esos timeframes son completas y se usaron sin necesidad
de filtrado (el caso con parciales reales, 1W, no forma parte del conjunto evaluado por
`evaluacion_multi_tf`, pero el mecanismo de exclusión (`FiltrarParaBacktest`) es el mismo código
para los 13 timeframes).

## Estrategias utilizadas

| Estrategia | Versión | Estado |
|------------|---------|--------|
| Tres Mosqueteros | única (`exploration/EstrategiaTresMosqueteros.cs`, sin variantes) | validada — determinista, sin componente aleatorio, ficha completa en `catalogo_estrategias/TRES_MOSQUETEROS.md` |
| MHI Mayoría | única (`exploration/EstrategiaMhiMayoria.cs`, sin variantes) | validada — determinista, sin componente aleatorio, ficha completa en `catalogo_estrategias/MHI_MAYORIA.md` |

Ninguna estrategia fue modificada durante esta fase ni durante las fases previas del laboratorio.

## Configuración experimental

- **Capital inicial experimental**: 1000 (unidades del modelo de posición actual, no comparable financieramente — ver Limitaciones conocidas).
- **Tamaño operación**: 1 (fijo, `OrderRequest(..., Cantidad: 1m)` en ambas estrategias).
- **Martingalas permitidas**: hasta 2 (`maxMartingalas: 2`, configurado en `evaluacion_multi_tf/Program.cs`).
- **Timeframes evaluados por el motor de backtest**: 1m, 5m, 15m, 1h, 4h, 1D (subconjunto de los 13 timeframes disponibles; 2m/10m/30m/2h/8h/12h/1W existen como datasets generados pero no fueron parte del conjunto evaluado en Fase 2C).
- **Parámetros adicionales**: ninguno — ambas estrategias operan solo con la posición modular `N % 5` sobre el dataset, sin indicadores externos ni información fuera de la vela/cuadrante actual.

## Evidencia de ejecución

**Determinismo — 3 corridas idénticas** ejecutadas en esta fase (`dotnet run --configuration Release`
sobre `exploration/laboratorio/evaluacion_multi_tf`, .NET 8.0.423):

```
Resultado ejecución 1 (normalizado, sin timestamp de ejecución)
=
Resultado ejecución 2 (normalizado, sin timestamp de ejecución)
=
Resultado ejecución 3 (normalizado, sin timestamp de ejecución)
```

Confirmado por `diff` binario entre las 3 salidas (excluyendo únicamente la línea
`FechaEjecucionUtc`, que varía por diseño en cada corrida): **0 diferencias** en las 12
combinaciones estrategia×timeframe — matriz comparativa, distribución de rachas, integridad del
motor e interpretación cualitativa idénticas en las 3 corridas.

- **SHA-256 de la salida normalizada (evidencia)**: `cad46d9b08b1dba6a7b98531bc485f55b2ad415becf446f31cd1ad8139bfd70d`
- **Cantidad de operaciones**: 12 corridas (2 estrategias × 6 timeframes), entre 30 (MHI/1D) y 82,475 (Tres Mosqueteros/1m) operaciones completadas por corrida — detalle completo en `catalogo_estrategias/*.md`.
- **Reconciliación**: coherente en las 12 corridas (`ReconciliacionCoherente=true`, sin errores).
- **Estado del motor**: `Estado=Success` en las 12 corridas.
- **Exposición máxima**: 1 en las 12 corridas (consistente con tamaño de operación fijo).

## Identidad del experimento (`IdentidadExperimento.cs`)

Verificado — el record captura, por cada corrida:

```csharp
public sealed record IdentidadExperimento(
    string Dataset, string Timeframe, string Estrategia, decimal CapitalInicial,
    string AggregationVersion, string SourceSha256, string TimeframeSha256, DateTime FechaEjecucionUtc);
```

Cualquier resultado puede reconstruirse a partir de estos 8 campos: dataset por nombre, timeframe,
estrategia, capital, versión del algoritmo de agregación, hash del dataset origen (1m) y hash del
dataset del timeframe específico usado. `SourceSha256` es idéntico en las 12 corridas
(`f1a9dcbe72bd...`) porque todas parten del mismo dataset base — la trazabilidad diferencial entre
timeframes depende de `TimeframeSha256`, que sí varía por timeframe y coincide exactamente con el
`sha256` de cada `metadata.json` verificado en la sección Timeframes.

## Limitaciones conocidas

Heredadas de Fase 2D, no resueltas en esta fase ni bajo su alcance:

- **Modelo económico incompleto**: tamaño de posición fijo (no escalado a precio ni a riesgo), `TasaMargen` con valor hardcoded no expuesto en contratos ni `SPEC.md` (`[SUPUESTO FINANCIERO NO EXPLICITADO]`), fricción de ejecución en 0.
- **Equity no comparable financieramente**: `EquityFinal`/`Retorno%` son datos derivados del modelo de posición actual, pueden ser negativos bajo ciertas combinaciones estrategia/timeframe (observación experimental documentada en el catálogo, no un bug — reconciliación y determinismo se mantienen coherentes incluso con equity negativo).
- **Sin costes reales**: `CostoFriccionReal` existe como campo en el contrato (`FillLogEntryDto`) pero no está siendo alimentado con un modelo de costos real en las corridas del laboratorio.
- **Sin gestión de riesgo real**: no hay stop-loss, sizing dinámico ni límites de exposición más allá del máximo estructural de 1 (una operación abierta a la vez, por diseño de las estrategias evaluadas).
- **Sin correr todos los 13 timeframes en Fase 2C**: 2m/10m/30m/2h/8h/12h/1W están generados y verificados como datos, pero no fueron parte del conjunto evaluado por el motor de backtest en esta ronda — extender la evaluación a esos timeframes es una decisión futura, no un defecto de esta fase.

## Clasificación de hallazgos de esta fase

Ningún hallazgo nuevo. Todo lo verificado coincide con lo ya documentado en Fases 2A-2D y en el
catálogo de estrategias — esta fase confirma, no descubre.

## Fuera de alcance (respetado)

No se realizaron cambios en arquitectura, contratos, estrategias, gestión de riesgo, modelo
financiero, indicadores nuevos ni optimizaciones. El único artefacto nuevo de esta fase es este
documento; los datos de ejecución (3 corridas) fueron generados y descartados como evidencia
temporal (hash registrado arriba), no se modificó ningún archivo de `exploration/laboratorio/`
fuera de este directorio `baseline/`.

## Criterio de cierre de Fase 1.0

- ✓ Dataset identificado y congelado.
- ✓ Timeframes validados (13/13).
- ✓ Estrategias base identificadas (2/2).
- ✓ Ejecución determinista comprobada (3/3 corridas idénticas).
- ✓ Resultados reproducibles.
- ✓ Documento `BASELINE_EXPERIMENTAL_V1` creado.
- ✅ Auditoría aprobada — Fase 1.0 cerrada (2026-08-11). Decisión D-001: este documento (V1) queda congelado; cualquier cambio futuro que afecte motor, estrategia, dataset, timeframe o configuración debe generar una nueva versión (`BASELINE_EXPERIMENTAL_V2`), sin modificar este archivo. Decisión D-002: ROI real, Sharpe, riesgo monetario, costes, margen y Masaniello permanecen fuera de alcance — pertenecen a Caso 2.
