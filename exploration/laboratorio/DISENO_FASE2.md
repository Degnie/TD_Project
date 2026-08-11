# Documento de diseño — Fase 2: Datos reales y Multi-Timeframe

Estado: **propuesta, sin implementar**. No modifica `src/`, `SPEC.md`, ni ningún contrato del
motor. Todo lo descrito aquí, una vez aprobado, se construirá exclusivamente dentro de
`exploration/laboratorio/`, igual que las Fases 1 y 1.5.

## Objetivo

Definir cómo incorporar datos externos reales al laboratorio sin romper reproducibilidad,
trazabilidad, determinismo, ni compatibilidad con el motor actual. El riesgo que se investiga en
esta fase ya no es la lógica del motor (validada en Fase 1/1.5 con 0 [BUG] sobre 20 corridas
sintéticas) sino la calidad y transformación del dato de entrada.

## Punto 1 — Fuente de datos

| Campo | Decisión |
|---|---|
| Proveedor | Binance API pública |
| Instrumento | BTC/USDT Spot |
| Mercado | Criptomoneda (24/7, sin sesiones ni cierres) |
| Período inicial | 1 año histórico |
| Licencia | Gratuita, uso público de la API |
| Formato original | JSON/REST (klines de Binance) |

**Motivo**: cripto elimina variables externas (sesiones, feriados de bolsa, splits, dividendos,
premarket/after-hours) que complicarían la primera validación del pipeline. El objetivo de esta
etapa es responder "¿el motor procesa correctamente un mercado real?", no medir rentabilidad ni
usar datos de calidad institucional — eso queda para una etapa posterior con fuente comercial, si
se justifica.

**No implica** que el sistema se diseñe para cripto. El motor permanece agnóstico; BTC/USDT es
el primer vehículo de validación por ser el de menor ruido externo. Repetir el laboratorio con
Forex/acciones/índice queda como paso posterior para confirmar que la arquitectura es universal.

## Punto 2 — Formato interno

Se mantiene CSV como formato canónico (coherente con `datasets/market/` y `datasets/scenarios/`
de Fase 1). Contrato de columnas:

```
TimestampUtc,Open,High,Low,Close,Volume
```

Sin columna de símbolo, timeframe ni zona horaria por fila — esos datos son **metadata del
dataset**, no de cada vela (ver Punto 6). Precisión de precio: la que entregue Binance para el
par (`decimal`, consistente con el tipo `Candle` ya usado por el motor — sin redondeo adicional
en la ingesta).

## Punto 3 — Zona horaria

**Decisión: UTC exclusivo en todo el pipeline** (descarga, CSV congelado, agregación, ejecución
del motor). Binance entrega timestamps en epoch/UTC de forma nativa — no hay conversión a hora
local en ningún punto del laboratorio.

El formato de dataset reserva metadata opcional para sesión/huso horario, pensando en la futura
incorporación de Forex/acciones (que sí tienen sesiones, aperturas y cierres). Ver estructura de
metadata en el Punto 6 — no se agrega como columna redundante por vela.

## Punto 4 — Resolución base

**Decisión: Opción A — 1 minuto como única fuente primaria, agregación interna determinista.**

```
Binance 1m (fuente única)
      ↓
CSV congelado (fixture oficial)
      ↓
Agregador interno determinista
      ↓
5m, 10m, 15m, 30m, 1h, 2h, 4h, 8h, 12h, 1D, 1W (derivados)
      ↓
Motor (BacktestRunner)
```

**Motivo**: descargar cada timeframe directamente del proveedor introduce múltiples fuentes de
verdad (redondeos, cierres de vela y timestamps propios de cada endpoint), rompiendo la garantía
de reproducibilidad exacta. Con una única fuente de 1 minuto, cualquier vela superior es
auditable hasta el minuto exacto que la compone.

## Punto 5 — Agregación multi-timeframe

Reglas de agregación (N velas de 1m → 1 vela de Nm):

| Campo | Regla |
|---|---|
| Open | Open de la primera vela del grupo |
| High | máximo High del grupo |
| Low | mínimo Low del grupo |
| Close | Close de la última vela del grupo |
| Volume | suma de Volume del grupo |

El agregador debe tener sus propios fixtures de validación antes de usarse en Fase 2 (mismo
patrón que `ValidadorReconciliacionFinanciera` en Fase 1: cada regla se congela como hipótesis
ejecutable, no como descripción a inspeccionar a ojo). Ejemplo de fixture mínimo: 5 velas de 1m
con valores conocidos → 1 vela de 5m con el resultado exacto esperado.

### Tratamiento de huecos de datos

**Decisión: política conservadora (Opción C, con herramienta de diagnóstico separada).**

- **Dataset oficial congelado**: un hueco de datos (minuto faltante por mantenimiento o corte del
  feed) **invalida ese período** para el dataset oficial. No se agrega, no se usa. No se rellena
  silenciosamente con velas sintéticas — eso ocultaría una deficiencia del dato real detrás de
  una decisión de diseño no auditable ("¿la estrategia ganó porque el mercado se movió así, o
  porque nosotros inventamos una vela?").
- **Dataset experimental (opcional, separado)**: para investigación de estrategias sensibles a
  continuidad, se puede generar una versión con relleno explícito (vela plana:
  Open=High=Low=Close=Close anterior, Volume=0), pero marcada como sintética y **excluida de
  cualquier métrica oficial** — mismo principio que `FriccionExtrema` en la segunda tanda de
  Fase 1 (documentado, no contado).

```
Binance 1m
      ↓
Detector de integridad (huecos, duplicados, orden)
      ↓
   ┌──────────────┴──────────────┐
Sin huecos                   Con huecos
      ↓                          ↓
Dataset oficial          Dataset rechazado/documentado
(congelado)                        │
                                    ↓ (opcional)
                          Dataset experimental
                          (relleno explícito, marcado
                          "sintético", fuera de métricas
                          oficiales)
```

## Punto 6 — Reproducibilidad

Estructura de directorio propuesta (paralela a `datasets/market/` y `datasets/scenarios/`):

```
datasets/
 └── reales/
      └── BTCUSDT/
           └── 2025/
                ├── 1m.csv
                └── metadata.json
```

`metadata.json` por dataset:

```json
{
  "instrumento": "BTCUSDT",
  "fuente": "Binance",
  "timezone": "UTC",
  "sesion": "24x7",
  "resolucion_base": "1m",
  "periodo": "2025-01-01T00:00:00Z / 2025-12-31T23:59:00Z",
  "fecha_descarga": "AAAA-MM-DD",
  "sha256": "..."
}
```

Reglas de congelación:

- Una vez descargado y validado, el CSV **no se vuelve a descargar automáticamente** para
  pruebas históricas — mismo principio de materialización ya usado en Fase 1
  (`REGENERAR_DATASETS=1` como excepción explícita, no comportamiento por defecto).
  Datasets derivados (5m, 1h, etc.) se recalculan desde el 1m congelado, no se descargan aparte.
- Cualquier regeneración queda registrada (nueva fecha de descarga, nuevo hash) — el archivo
  anterior no se sobreescribe en silencio.

### Primer dataset real congelado (Fase 2A, cierre)

```
datasets/reales/BTCUSDT/1m/BTCUSDT_2024-01-02_2025-01-02_1m.csv
datasets/reales/BTCUSDT/1m/metadata.json
```

El primer dataset real del laboratorio **no representa un año calendario**, sino una ventana
móvil de 366 días UTC obtenida desde 2024-01-02T00:00:00Z hasta 2025-01-02T00:00:00Z (2024 fue
bisiesto). El nombre del archivo refleja el rango exacto para evitar ambigüedad, en vez de usar
una etiqueta como "2024" o "2025" que sugeriría un año calendario que no es. 527.040 velas,
0 huecos, 0 duplicados, `sha256=f1a9dcbe72bdbca65c5a7de55c776c209a63f8b3ecd93c59a5fca958e4ebded4`
— trazable end-to-end desde la descarga original en `datos_reales/raw/` (ver `PLAN_FASE2A.md`).

## Punto adicional — Validador de calidad de datos

Antes de que cualquier dataset real entre al laboratorio, debe pasar un validador dedicado
(mismo patrón obligatorio que `ValidadorReconciliacionFinanciera`, pero para el dato de entrada,
no el resultado del backtest):

- Timestamps estrictamente ordenados y sin duplicados.
- Coherencia OHLC: `High >= Open`, `High >= Close`, `Low <= Open`, `Low <= Close`.
- Sin velas imposibles (`High < Low`, etc.).
- Detección de huecos (alimenta la política del Punto 5).

**Motivo**: un dato malo puede parecer un bug del motor si no se descarta primero en la frontera
de entrada.

## Resumen de decisiones

| Punto | Decisión |
|---|---|
| Fuente | Binance API pública, BTC/USDT Spot, gratuita |
| Formato | CSV `TimestampUtc,Open,High,Low,Close,Volume` |
| Timezone | UTC exclusivo en pipeline; metadata de sesión reservada para uso futuro |
| Resolución base | 1 minuto, única fuente primaria |
| Agregación | Determinista (Open=primero, High=máx, Low=mín, Close=último, Volume=suma), con fixtures propios |
| Huecos | Rechazan el período del dataset oficial; relleno sintético solo en dataset experimental separado, excluido de métricas |
| Congelación | `datasets/reales/{SIMBOLO}/{AÑO}/1m.csv` + `metadata.json` con hash y fecha; no se regenera automáticamente |
| Validación | Validador de integridad de datos obligatorio antes de ingresar al laboratorio |

## Fuera de alcance de este documento

- Implementación de descargador, agregador o validadores (código).
- Elección de fuente comercial (queda como etapa posterior si se necesita validar precisión
  institucional, spreads o microestructura).
- Extensión a Forex/acciones/índices (metadata de sesión queda reservada pero no se usa todavía).
- Cualquier cambio a `src/`, `SPEC.md`, o los contratos del motor.

## Siguiente paso

Este documento queda bloqueado a la espera de aprobación. Una vez aprobado, la implementación
seguirá el mismo ciclo que Fase 1: diseño de cada pieza (descargador → validador de integridad →
congelación → agregador con fixtures propios) con evidencia antes de avanzar a la siguiente,
sin tocar `src/` en ningún paso.
