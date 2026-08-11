# Plan técnico — Fase 2A: Descargador histórico + Validador de integridad

Estado: **plan, sin implementar**. Deriva de las decisiones ya aprobadas en `DISENO_FASE2.md`.
No modifica `src/`, `SPEC.md`, ni el motor. Todo lo descrito vive en
`exploration/laboratorio/datos_reales/`.

## 1. Estructura de proyecto

```
exploration/
 └── laboratorio/
      └── datos_reales/
           ├── DatosReales.csproj              (satellite project, mismo patrón que LaboratorioSintetico.csproj)
           ├── BinanceClient.cs                 (llamada HTTP cruda al endpoint klines)
           ├── DescargadorVelas.cs              (paginación, ensamblado, escritura de CSV crudo)
           ├── ValidadorIntegridadDatos.cs       (continuidad, orden, duplicados, OHLC, volumen)
           ├── Program.cs                        (entry point: descarga + valida + congela)
           ├── raw/
           │    └── BTCUSDT_1m_2025.csv
           └── metadata/
                └── BTCUSDT_1m_2025.json
```

`exploration/Exploration.csproj` necesitará un nuevo `<Compile Remove="laboratorio\datos_reales\**\*.cs" />`
preventivo (mismo problema recurrente de top-level-statements documentado en Fase 1) — se agrega
antes de compilar por primera vez, no después de romper el build.

**Nota de ubicación**: el diseño aprobado en `DISENO_FASE2.md` (Punto 6) proponía
`datasets/reales/BTCUSDT/2025/1m.csv` como destino final del dataset *congelado y validado*. La
estructura `datos_reales/raw/` de este plan es la zona de trabajo del descargador — el CSV crudo
aterriza ahí primero; solo pasa a `datasets/reales/` después de aprobar el validador de
integridad (ver sección 5). Son dos carpetas con propósitos distintos, no una duplicación.

## 2. Formato exacto del CSV crudo

Binance devuelve 12 campos por vela (kline); el laboratorio solo necesita 6, ya definidos en
`DISENO_FASE2.md`:

```
TimestampUtc,Open,High,Low,Close,Volume
```

Mapeo desde la respuesta de Binance (`GET /api/v3/klines`):

| Columna CSV | Campo Binance (índice) | Tipo origen | Transformación |
|---|---|---|---|
| TimestampUtc | `[0]` Open time | int64 (ms epoch) | se guarda tal cual, en milisegundos UTC |
| Open | `[1]` | string | `decimal.Parse` directo, sin redondeo |
| High | `[2]` | string | ídem |
| Low | `[3]` | string | ídem |
| Close | `[4]` | string | ídem |
| Volume | `[5]` | string | ídem |

Campos `[6]`–`[11]` (Close time, quote volume, número de trades, taker volumes, "ignore") **se
descartan** — no forman parte del contrato `Candle` del motor y no se necesitan para OHLCV.

## 3. Paginación de Binance API

Datos confirmados contra la documentación oficial (`GET /api/v3/klines`):

- Límite máximo: **1000 velas por request** (parámetro `limit`, default 500 — se pedirá 1000
  siempre).
- Parámetros de rango: `startTime`/`endTime` en milisegundos epoch UTC.
- Un año de velas de 1 minuto ≈ 525 600 velas → **≈ 526 requests** para completar el año.
- Peso de rate limit: 2 por request (weight-based, no request-count-based) — a este volumen no
  se acerca a los límites de IP de Binance, no se requiere lógica de backoff agresiva, pero sí un
  retraso fijo pequeño entre requests como buena práctica (ponytail: sleep fijo entre llamadas,
  no un rate-limiter completo — subir a backoff exponencial solo si Binance empieza a devolver
  429/418).

Algoritmo de paginación:

```
cursor = inicioUtc (ms)
mientras cursor < finUtc:
    respuesta = GET klines(symbol=BTCUSDT, interval=1m, startTime=cursor, limit=1000)
    si respuesta vacía: cortar (no hay más datos)
    escribir velas de la respuesta al CSV crudo
    cursor = timestamp de la última vela recibida + 1 minuto (en ms)
```

No se reintenta automáticamente sobre errores HTTP más allá de reintentos simples con backoff —
si Binance corta la descarga a mitad de camino, el descargador debe poder **reanudar desde el
último timestamp escrito**, no volver a empezar desde cero (relevante para 526 requests
secuenciales).

## 4. Metadata congelada

`metadata/BTCUSDT_1m_2025.json`, generado automáticamente al finalizar la descarga:

```json
{
  "instrumento": "BTCUSDT",
  "mercado": "Spot",
  "fuente": "Binance",
  "intervalo": "1m",
  "inicioUtc": "2025-01-01T00:00:00Z",
  "finUtc": "2025-12-31T23:59:00Z",
  "descargaUtc": "AAAA-MM-DDTHH:MM:SSZ",
  "velasEsperadas": 525600,
  "velasDescargadas": 0,
  "sha256": null
}
```

`velasEsperadas` se calcula desde el rango solicitado (para que el validador de continuidad
tenga una cifra de referencia independiente del propio archivo). `sha256` y `velasDescargadas` se
completan al final de la descarga, sobre el CSV crudo ya escrito. Este metadata es el equivalente
al `seed` de los datasets sintéticos — de ahí el campo `seed: null` explícito en el ejemplo
original: no aplica a datos reales, se deja documentado como ausente, no se omite en silencio.

## 5. Validador de integridad (`ValidadorIntegridadDatos`)

Corre sobre el CSV crudo, **antes** de que el dataset se considere apto para copiarse a
`datasets/reales/` (ver `DISENO_FASE2.md`). Mismo patrón que `ValidadorReconciliacionFinanciera`
de Fase 1: un veredicto + lista de errores, ejecutable como fixture, no inspección manual.

Checks, en orden:

1. **Orden temporal estricto**: cada `TimestampUtc` > el anterior. Cualquier inversión es error
   inmediato (corta la validación, no tiene sentido seguir).
2. **Sin duplicados**: ningún `TimestampUtc` se repite.
3. **Continuidad**: la diferencia entre timestamps consecutivos es siempre exactamente 60 000 ms
   (1 minuto). Cualquier salto mayor se registra como hueco — lista de rangos
   `(desde, hasta, minutosFaltantes)`, no solo un booleano.
4. **OHLC válido**: `High >= Open`, `High >= Close`, `Low <= Open`, `Low <= Close`, `High >= Low`.
5. **Volumen válido**: `Volume >= 0`.

Salida: `Veredicto(bool AptoParaCongelar, IReadOnlyList<string> Errores, IReadOnlyList<(long Desde, long Hasta, int MinutosFaltantes)> Huecos)`.

Aplicando la política ya aprobada en `DISENO_FASE2.md` (huecos → Opción C):

- Si `Huecos` no está vacío, el dataset **no se copia** a `datasets/reales/` tal cual. Se
  documenta el reporte de huecos (log a texto, mismo directorio que el CSV crudo) para decisión
  manual — igual que `FriccionExtrema` quedó documentado y excluido en Fase 1: no se descarta el
  trabajo, se marca y se deja fuera del dataset oficial.
- Errores de orden, duplicados u OHLC inválido son bloqueantes sin excepción — no aplica la
  distinción "oficial vs. experimental" que sí aplica a huecos, porque no representan una
  característica del mercado real sino un defecto de la descarga/transformación.

## 6. Congelación

Una vez que `ValidadorIntegridadDatos` da `AptoParaCongelar = true`:

1. Se calcula el SHA256 del CSV crudo y se completa en el `metadata.json`.
2. Se copia (no se mueve) el CSV validado a `datasets/reales/BTCUSDT/2025/1m.csv` junto con su
   metadata — el crudo en `datos_reales/raw/` queda como registro de la descarga original.
3. A partir de ese punto, el archivo en `datasets/reales/` es inmutable: no se regenera
   automáticamente. Igual que `REGENERAR_DATASETS=1` en Fase 1, cualquier nueva descarga requiere
   una acción explícita y produce un nuevo hash/fecha, sin sobreescribir en silencio.

## 7. Fixture de validación del propio descargador/validador

Antes de correr contra Binance real, `ValidadorIntegridadDatos` necesita sus propios casos de
prueba deterministas (mismo principio "test-first" de toda la auditoría anterior):

- CSV con continuidad perfecta → `AptoParaCongelar = true`, sin huecos ni errores.
- CSV con un hueco de 2 minutos → huecos no vacíos, `AptoParaCongelar = false`.
- CSV con timestamp duplicado → error bloqueante.
- CSV con timestamps fuera de orden → error bloqueante.
- CSV con una vela `High < Low` → error bloqueante.
- CSV con `Volume` negativo → error bloqueante.

Estos fixtures son datos sintéticos pequeños escritos a mano (no requieren Binance), y son el
primer entregable ejecutable antes de tocar la red.

## Fuera de alcance de este plan

- Fase 2B (agregador multi-timeframe): planificación separada, después de tener un CSV 1m
  congelado y validado.
- Fase 2C (integración con `EjecutorLaboratorio`/`BacktestRunner`): posterior a 2B.
- Cualquier cambio a `src/`, `SPEC.md`, o los contratos del motor.

## Orden de implementación propuesto (una vez aprobado este plan)

1. `ValidadorIntegridadDatos` + sus 6 fixtures sintéticos (sección 7) — sin red, sin Binance.
2. `BinanceClient` + `DescargadorVelas` con paginación y reanudación — probado primero contra un
   rango chico (ej. 1 día) antes de lanzar la descarga completa de 1 año.
3. Descarga real de BTCUSDT 1m 2025, validación, reporte de huecos si los hay, congelación.
4. Cierre de Fase 2A con reporte de resultados (igual patrón que Fase 1/1.5) antes de pasar a 2B.
