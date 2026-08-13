# Hallazgo — Dataset Temporal Alternativo BTCUSDT 2022-01-01 → 2023-01-01

Estado: **documento de registro de evidencia — no es una decisión, no propone código**. Documenta
el resultado real de la descarga, validación, y congelación del segundo rango temporal para la Vía
B de D-121 (`DECISIONES_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md`), tras el rechazo del rango 2023
(`HALLAZGO_RECHAZO_DATASET_2023_CASO5C_V1.md`) y la exploración de disponibilidad que confirmó 2022
como candidato viable (D-122, `DECISIONES_RANGO_ALTERNATIVO_CASO5C_V1.md`). Mismo criterio de
trazabilidad usado para el rechazo — este documento no reporta un problema, reporta la aceptación
con el mismo nivel de detalle.

---

## Hallazgo

**`BTCUSDT_1m` para el rango `2022-01-01T00:00:00Z` – `2023-01-01T00:00:00Z`: APTO PARA CONGELAR.**

**Resultado de `ValidadorIntegridadDatos.Verificar`**:
- **Velas esperadas**: 525.600 (1 año completo a 1 minuto). **Velas recibidas**: 525.600 — coincide
  exactamente, sin faltantes.
- **Huecos**: 0. **Minutos faltantes**: 0.
- **Duplicados**: 0. **Errores de orden**: 0. **Errores estructurales** (OHLC inválido, volumen
  negativo): 0.

**Hash principal (CSV crudo `1m`)**:
`f53d7416c87ac389c9a82f0e3ec386c4b23b6637b0f71b826f65c77973f7524f` — verificado idéntico entre el
archivo generado por la descarga (`datos_reales/raw/BTCUSDT_1m_1anio_2023.csv`, nombre de archivo
crudo heredado del sufijo por año de fin, ver nota) y el archivo copiado a
`datasets/reales/BTCUSDT/1m_2022/BTCUSDT_2022-01-01_2023-01-01_1m.csv` (`sha256sum` recalculado
sobre el archivo ya congelado, coincide exactamente con el registrado en `metadata.json`).

**Nota sobre el nombre del archivo crudo**: el sufijo interno usa el año de `finUtc` (`2023`,
`datos_reales/Program.cs`), no el año de inicio del rango — `BTCUSDT_1m_1anio_2023.csv` corresponde
al rango `2022-01-01–2023-01-01`, no al rango 2023 ya rechazado (ese crudo, `BTCUSDT_1m_1anio_2024.csv`,
sigue existiendo por separado en `raw/`, sin promover). Ambos archivos coexisten en
`datos_reales/raw/` sin colisión, cada uno con su propio nombre.

---

## Relación con D-121/D-122

- **D-121** (`DECISIONES_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md`): fijó la Vía B (tiempo primero) —
  este dataset es la materialización de esa vía. Mismo instrumento (`BTCUSDT`), mismo pipeline,
  mismos costes/parámetros económicos ya congelados — la única dimensión que varía es el período
  temporal, preservando la capacidad de atribución causal que D-121 exigió como criterio decisivo.
- **D-122** (`DECISIONES_RANGO_ALTERNATIVO_CASO5C_V1.md`): fijó el mecanismo de exploración previa
  (Opción B) tras el rechazo de 2023. La exploración mensual sobre 2022 (12/12 bloques continuos,
  ejecutada antes de este hallazgo) predijo correctamente el resultado de la descarga completa —
  ningún hueco detectado en la exploración, ningún hueco detectado en la validación real. Confirma
  que la exploración cumple su propósito: reducir el riesgo de repetir el costo de una descarga
  completa rechazada, sin sustituir la validación real (que sí se ejecutó, obligatoriamente, sobre
  la descarga completa).

---

## Diferencia respecto al dataset 2024-2025

| | 2024-2025 (original) | 2022 (nuevo) |
|---|---|---|
| Instrumento | BTCUSDT | BTCUSDT — **idéntico** |
| Fuente | Binance Spot | Binance Spot — **idéntico** |
| Intervalo base | 1m | 1m — **idéntico** |
| Rango | 2024-01-02 – 2025-01-02 | 2022-01-01 – 2023-01-01 — **distinto**, dimensión que D-121 varía deliberadamente |
| Velas 1m | 527.040 | 525.600 — distinto (2024 es año bisiesto, 1 día más) |
| Huecos detectados | 0 | 0 — ambos sin discontinuidades |
| SHA-256 (1m) | `f1a9dcbe72bdbca65c5a7de55c776c209a63f8b3ecd93c59a5fca958e4ebded4` | `f53d7416c87ac389c9a82f0e3ec386c4b23b6637b0f71b826f65c77973f7524f` |
| Ubicación | `datasets/reales/BTCUSDT/1m/` | `datasets/reales/BTCUSDT/1m_2022/` |
| Timeframes derivados | 13 (mismo conjunto) | 13 (mismo conjunto) |

**Nota sobre la diferencia de conteo de velas**: 2024 es año bisiesto (366 días,
`velaCount` de `1D` = 366 en el dataset original) frente a 2022 (365 días, `velaCount` de `1D_2022`
= 365, verificado en `datasets/reales/BTCUSDT/1D_2022/metadata.json`). Esta diferencia es un hecho
del calendario, no una discontinuidad de datos — ambos datasets están completos para su propio año.

**Ambos datasets coexisten sin mezcla** — ningún archivo del dataset 2024-2025 fue modificado
(`git status --porcelain -- exploration/laboratorio/datasets/reales/BTCUSDT/1m/
.../1D/ .../1h/ .../15m/` vacío, verificado tras la congelación del dataset 2022).

---

## Evidencia de agregación (13 timeframes)

Cada timeframe derivado tiene su propio `metadata.json` con `sourceSha256` apuntando correctamente
al CSV `1m_2022` (no al `1m` de 2024-2025) — verificado en 3 muestras:

| Timeframe | Velas | Parciales | `sourceSha256` coincide con `1m_2022` |
|---|---|---|---|
| `15m_2022` | 35.040 | 0 | ✅ |
| `1h_2022` | 8.760 | 0 | ✅ |
| `1D_2022` | 365 | 0 | ✅ |
| `1W_2022` | 53 | 2 (bordes de calendario, esperado) | — |

**Verificación manual del agregador** (primeras 60 velas 1m vs. primera vela 1h agregada
directamente): `Coincide: True` — mismo mecanismo de verificación ya usado para el dataset
2024-2025 (`agregador/Program.cs:98-126`, sin modificación de esa lógica).

---

## Qué no se hizo (restricción respetada)

- No se ejecutó ninguna comparación de gestores ni campaña sobre el dataset 2022.
- No se modificó `ComparadorGestores`, `PersistidorComparaciones`, ni ningún gestor.
- No se recalibró ningún parámetro económico observando el dataset nuevo (D-030).
- No se tocó el dataset 2024-2025 (verificado, `git status --porcelain` vacío sobre esas rutas).

---

## Fuera de alcance de este documento

No se ejecuta la sub-campaña temporal (Sub-campaña D, ya descrita en
`ESPECIFICACION_IMPLEMENTACION_DIVERSIDAD_TEMPORAL_CASO5C_V1.md` §5). No se audita ningún resultado
comparativo. No se actualiza todavía el índice global de decisiones — queda para el commit
administrativo siguiente.

---

## Próximo paso

Commit del dataset 2022 (CSV + 13 `metadata.json`) y de los cambios de código que lo produjeron
(`datos_reales/Program.cs`, `agregador/Program.cs`, `datos_reales/ExploradorDisponibilidad.cs`,
`datos_reales/FixturesExploradorDisponibilidad.cs`), siguiendo la misma política de versionado ya
aplicada al dataset 2024-2025 (commiteado como fuente, no excluido por `.gitignore`). Tras el
commit, actualización del índice de Caso 5C si corresponde. La ejecución de la sub-campaña temporal
sobre este dataset queda para un paso posterior, explícitamente no autorizado todavía.
