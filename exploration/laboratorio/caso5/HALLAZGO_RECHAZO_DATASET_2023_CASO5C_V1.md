# Hallazgo — Rechazo de Dataset BTCUSDT 2023-01-02 → 2024-01-02

Estado: **documento de registro de hallazgo — no es una decisión, no propone código, no descarga
nada nuevo**. Documenta el resultado real de ejecutar
`ESPECIFICACION_IMPLEMENTACION_DIVERSIDAD_TEMPORAL_CASO5C_V1.md` §2 Paso 1 (descarga cruda del
rango elegido para la Vía B de D-121), antes de decidir cómo continuar.

---

## Hallazgo

**`BTCUSDT_1m` para el rango `2023-01-02T00:00:00Z` – `2024-01-02T00:00:00Z`: NO APTO PARA
CONGELAR.**

**Motivo**: hueco real de 80 minutos en los datos servidos por la API pública de Binance
(`GET /api/v3/klines`), detectado por `ValidadorIntegridadDatos.Verificar`:

- **Intervalo faltante (UTC)**: `2023-03-24T12:39:00Z` – `2023-03-24T14:00:00Z`.
- **Timestamps crudos**: `DesdeMs=1679661540000`, `HastaMs=1679666400000`.
- **Minutos faltantes**: 80.
- **Duplicados**: 0. **Errores de orden**: 0. **Errores estructurales** (OHLC inválido, volumen
  negativo): 0 — el único problema detectado es el hueco de continuidad.
- **Velas esperadas**: 525.600 (1 año completo a 1 minuto). **Velas recibidas**: 525.520.

**Artefacto generado**: `datos_reales/raw/BTCUSDT_1m_1anio_2024.csv` (525.520 filas + encabezado,
~45MB) — queda en `raw/` como registro de la descarga, según el diseño ya vigente
(`datos_reales/Program.cs`, mensaje de rechazo: "Dataset NO apto. Queda solo en raw/, documentado,
sin promover a datasets/reales/"). **No se generó ningún `metadata.json` para este dataset** — la
metadata solo se escribe para datasets ya validados (`DescargadorVelas.EscribirMetadataValidada`
se invoca únicamente si `AptoParaCongelar == true`).

---

## Interpretación correcta

**No es un fallo del descargador, del agregador, ni del validador.** Es el comportamiento esperado
y correcto del pipeline: `ValidadorIntegridadDatos` fue diseñado exactamente para detectar
discontinuidades como esta y bloquear la congelación antes de que un dataset incompleto llegue a
`datasets/reales/` (`PLAN_FASE2A.md` §5-§6, política ya vigente desde antes de Caso 5C). El sistema
hizo lo que debía hacer: **detectó correctamente un dataset no apto**.

El hueco en sí es un hecho externo — una discontinuidad real en el histórico que Binance sirve para
`BTCUSDT` 1m en ese intervalo específico, no una consecuencia de ningún código de este proyecto.

---

## Qué no se hizo (restricción respetada)

- No se aplicó relleno sintético ni interpolación del hueco de 80 minutos.
- No se eliminaron velas de forma silenciosa para "limpiar" el conteo.
- No se modificó `ValidadorIntegridadDatos` para relajar su criterio de aceptación.
- No se promovió el CSV rechazado a `datasets/reales/`.
- No se intentó otro rango todavía — esa decisión queda para un documento posterior.

**El comportamiento actual (rechazo estricto, sin reparación automática) es parte de la garantía
experimental del proyecto, no un obstáculo a evitar.**

---

## Estado del working tree tras este hallazgo

- `datos_reales/Program.cs`: modificado (rango 2023, sufijo de archivo con año) — cambio de
  código ya realizado como parte de `ESPECIFICACION_IMPLEMENTACION_DIVERSIDAD_TEMPORAL_CASO5C_
  V1.md` §2, previo a este hallazgo. Sigue siendo válido para intentar un rango alternativo (solo
  requiere ajustar `finUtc`).
- `agregador/Program.cs`: modificado (generalización de `rangoDataset`/`sufijoCarpeta` vía
  `RANGO_DATASET`) — **no se ejecutó sobre ningún dataset 2023**, porque el Paso 1 (descarga)
  nunca llegó a producir un CSV apto para congelar. Verificado que correrlo sin `RANGO_DATASET`
  sigue apuntando exactamente al dataset 2024-2025 original, sin alterarlo (ver nota de
  trazabilidad abajo).
- `datasets/reales/BTCUSDT/` (dataset 2024-2025 ya congelado): **intacto**. Nota de trazabilidad:
  durante la verificación de que `agregador/Program.cs` seguía apuntando correctamente al dataset
  original por defecto, una ejecución de prueba regeneró `5m/metadata.json` con un
  `generatedUtc` distinto (mismo `sha256`, contenido del CSV idéntico — confirma determinismo).
  Ese cambio no debía persistir y fue revertido inmediatamente con `git checkout --` antes de
  continuar; `git status --porcelain -- exploration/laboratorio/datasets/reales/BTCUSDT/` está
  vacío al momento de este documento.
- `datos_reales/raw/BTCUSDT_1m_1anio_2024.csv`: generado, no apto, no promovido. Excluido de git
  (`.gitignore` actualizado: `exploration/laboratorio/datos_reales/raw/` y
  `exploration/laboratorio/datos_reales/metadata/` agregadas, mismo criterio que
  `protocolo/resultados/`/`caso5/resultados/` — evidencia regenerable, no fuente).

---

## Fuera de alcance de este documento

No se decide qué rango alternativo probar. No se descarga ningún dato nuevo. No se modifica
`ValidadorIntegridadDatos`. No se continúa con la Vía B de D-121 hasta que exista una decisión
explícita sobre cómo proceder.

---

## Próximo documento

Una decisión específica — **D-122 (candidata)**: selección de rango temporal alternativo para
diversidad Caso 5C, con al menos 3 opciones a evaluar sin preseleccionar:
- **A** — Probar otro año completo distinto (ej. 2022-01-02 – 2023-01-02).
- **B** — Búsqueda previa de disponibilidad/continuidad por rangos cortos (ej. por mes o trimestre)
  antes de comprometerse a descargar un año completo, reduciendo el coste de descubrir huecos
  después de una descarga larga.
- **C** — Otra estrategia a definir en la ronda de decisiones.
