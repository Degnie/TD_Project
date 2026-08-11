# Documento de diseño — Fase 2B: Agregación multi-timeframe

Estado: **propuesta, sin implementar**. Deriva de `DISENO_FASE2.md` (Fase 2, aprobado) y
`PLAN_FASE2A.md` (descargador + validador, cerrado). No modifica `src/`, `SPEC.md`, ni el motor.
Todo lo descrito vive en `exploration/laboratorio/`.

## Objetivo

Crear velas superiores (2m, 5m, 10m, 15m, 30m, 1h, 2h, 4h, 8h, 12h, 1D, 1W) **únicamente** a
partir del dataset 1m congelado en Fase 2A
(`datasets/reales/BTCUSDT/1m/BTCUSDT_2024-01-02_2025-01-02_1m.csv`, 527 040 velas,
`sha256=f1a9dcbe72...`). La primera validación de esta fase es puramente matemática — "¿la
transformación 1m → timeframe superior es correcta?" — no "¿la estrategia gana más en 15m?". Eso
es Fase 2C, fuera de alcance aquí.

## Punto 1 — Anclaje temporal

**Decisión: calendario UTC estricto**, no ventana relativa al primer dato del dataset.

| Timeframe | Anclaje |
|---|---|
| 2m, 5m, 10m, 15m, 30m | Múltiplos exactos de la hora UTC (ej. 5m: `:00–:04`, `:05–:09`, ...) |
| 1h, 2h, 4h, 8h, 12h | Múltiplos exactos del día UTC (ej. 4h: `00:00`, `04:00`, `08:00`, ...) |
| 1D | Día calendario UTC: `00:00:00Z` → `23:59:59Z` |
| 1W | Semana ISO: lunes `00:00:00Z` → domingo `23:59:59Z` |

**Motivo**: el objetivo no es producir bloques matemáticos arbitrarios, sino datasets
comparables contra cualquier fuente externa futura (Binance, TradingView, otro proveedor). La
referencia es el tiempo real del mercado, no el instante en que se descargó el dataset. Esto es
consistente con el Punto 3 de `DISENO_FASE2.md` (UTC exclusivo en todo el pipeline).

## Punto 2 — Regla OHLCV

Igual que la ya aprobada en `DISENO_FASE2.md` (Punto 5), aplicada recursivamente a cualquier
timeframe superior (no solo derivado de 1m — un 1h también puede derivarse de doce velas 5m ya
agregadas, el resultado debe ser idéntico):

| Campo | Regla |
|---|---|
| Open | Open de la primera vela 1m del intervalo |
| High | máximo High del intervalo |
| Low | mínimo Low del intervalo |
| Close | Close de la última vela 1m del intervalo |
| Volume | suma de Volume del intervalo |

## Punto 3 — Tratamiento de bordes y velas parciales

**Decisión: incluir siempre, marcar completitud explícitamente. No descartar nada en el
agregador.**

El dataset 1m congelado empieza martes 2024-01-02T00:00Z y termina 2025-01-02T00:00Z — no
coincide con un borde de semana ni, en el extremo final, con el cierre exacto de todos los
timeframes. Ejemplo concreto: la semana ISO 2024-W01 va de lunes 01-01 a domingo 07-01, pero el
dataset solo tiene datos desde el martes 02-01 — esa vela semanal nace incompleta.

Cada vela superior generada lleva su propio registro de completitud:

```json
{
  "timeframe": "1W",
  "inicioUtc": "2024-01-01T00:00:00Z",
  "finUtc": "2024-01-07T23:59:00Z",
  "minutosEsperados": 10080,
  "minutosRecibidos": 8640,
  "esParcial": true
}
```

`minutosEsperados` se calcula desde el calendario (duración fija del timeframe), no desde el
dataset — es la misma técnica ya usada en `ValidadorIntegridadDatos` de Fase 2A (comparar contra
una cifra de referencia independiente del archivo). `minutosRecibidos` cuenta las velas 1m
efectivamente encontradas dentro de ese intervalo. `esParcial = minutosRecibidos < minutosEsperados`.

**Separación de responsabilidades** (explícitamente pedida): el agregador es una capa de
transformación de datos, no de decisión de backtesting. Conserva las velas parciales con su
metadata; es la capa de análisis/backtesting posterior la que decide si las usa:

```
Capa de datos (Fase 2B)          Capa de backtesting (Fase 2C, futura)
  conserva 1W parcial      →       usarSoloVelasCompletas = true
  con esParcial=true                (o false, según el análisis)
```

Fase 2B no implementa ese filtro — solo dejará el campo `esParcial` disponible para que Fase 2C
lo consuma cuando corresponda.

## Punto 4 — Validadores

Cada timeframe derivado debe demostrar, antes de congelarse:

1. **Número esperado de velas completas**: calculado desde el calendario UTC (ej. 366 días →
   366 velas 1D, de las cuales la última puede ser parcial si el dataset corta a mitad de día).
2. **Continuidad temporal**: igual criterio que `ValidadorIntegridadDatos` de Fase 2A, aplicado
   al timeframe derivado — sin saltos entre velas superiores consecutivas (más allá de los
   bordes ya marcados como parciales).
3. **Ausencia de huecos artificiales**: el agregador no debe generar una vela superior "vacía"
   donde no había ninguna vela 1m — un intervalo sin datos de origen no produce una fila, no una
   fila con ceros.
4. **Coherencia OHLC contra las velas 1m originales**: fixture de reconstrucción manual (ver
   Punto 6) — no basta con que el agregador "corra sin error", cada valor debe verificarse contra
   un cálculo independiente conocido.
5. **Determinismo**: agregar el mismo rango dos veces produce exactamente el mismo resultado
   (mismo principio RNF-06 ya validado en el motor, aplicado aquí a la capa de datos).

## Punto 5 — Estructura de datasets derivados y reproducibilidad

```
datasets/reales/BTCUSDT/
 ├── 1m/
 │    ├── BTCUSDT_2024-01-02_2025-01-02_1m.csv    (ya congelado, Fase 2A)
 │    └── metadata.json
 ├── 5m/
 │    ├── BTCUSDT_2024-01-02_2025-01-02_5m.csv
 │    └── metadata.json
 ├── 15m/
 │    └── ...
 └── 1h/ ... 1W/
```

`metadata.json` por timeframe derivado:

```json
{
  "source": "BTCUSDT_2024-01-02_2025-01-02_1m.csv",
  "sourceSha256": "f1a9dcbe72bdbca65c5a7de55c776c209a63f8b3ecd93c59a5fca958e4ebded4",
  "timeframe": "15m",
  "generatedAt": "AAAA-MM-DDTHH:MM:SSZ",
  "sha256": "...",
  "aggregationVersion": "1.0",
  "velasCompletas": 0,
  "velasParciales": 0
}
```

`sourceSha256` ata cada derivado al hash exacto del 1m que lo generó — si el 1m cambiara (no
debería, está congelado), quedaría evidencia inmediata de qué derivados quedaron desactualizados.
`aggregationVersion` existe para el mismo motivo que un seed en datos sintéticos: si la regla de
agregación cambia en el futuro (ej. tratamiento de bordes distinto), los datasets viejos siguen
siendo identificables como generados con una regla anterior, sin necesidad de recalcular el hash
del 1m para saberlo.

Mismas reglas de congelación que Fase 2A: no se regenera automáticamente; cualquier regeneración
produce nuevo hash y fecha, sin sobreescribir en silencio.

## Punto 6 — Fixture de validación del agregador (test-first, antes de correr contra el dataset real)

Antes de agregar las 527 040 velas reales, el agregador necesita casos deterministas escritos a
mano:

- **Caso simple**: 5 velas 1m con valores conocidos → 1 vela 5m con Open/High/Low/Close/Volume
  calculados a mano, comparados exactos.
- **Caso de borde alineado**: un rango que empieza exactamente en un borde de calendario (ej.
  00:00:00Z) → cero velas parciales.
- **Caso de borde no alineado**: un rango que empieza a mitad de una vela superior (ej. dataset
  arrancando 00:03Z con timeframe 5m) → la primera vela sale marcada `esParcial=true` con
  `minutosRecibidos` correcto.
- **Caso recursivo**: agregar 1m→5m→1h debe dar el mismo resultado que agregar 1m→1h
  directamente (verifica que la regla OHLCV es asociativa, no solo correcta a un nivel).
- **Caso de hueco heredado**: si Fase 2A hubiera dejado un hueco en el 1m (no debería, el dataset
  actual tiene 0), el agregador no debe "tapar" el hueco fabricando una vela superior completa —
  debe propagar la ausencia como menos `minutosRecibidos`, nunca inventar datos.

## Fuera de alcance de este documento

- Fase 2C (integración con `EjecutorLaboratorio`/`BacktestRunner`, filtro
  `usarSoloVelasCompletas`): planificación separada, después de que Fase 2B esté implementada y
  validada.
- Ejecutar estrategias sobre cualquier timeframe derivado.
- Cualquier cambio a `src/`, `SPEC.md`, o los contratos del motor.

## Orden de implementación propuesto (una vez aprobado este plan)

1. Fixtures del agregador (Punto 6) — sin tocar el dataset real de 527 040 velas.
2. Agregador (`AgregadorMultiTimeframe`) aplicado a un timeframe (ej. 5m) contra el dataset real,
   verificado manualmente contra un tramo conocido antes de generalizar a los 12 timeframes.
3. Generalización a los 12 timeframes restantes + metadata + hash por derivado.
4. Cierre de Fase 2B con reporte de resultados (mismo patrón que Fases 1/1.5/2A) antes de
   considerar Fase 2C.
