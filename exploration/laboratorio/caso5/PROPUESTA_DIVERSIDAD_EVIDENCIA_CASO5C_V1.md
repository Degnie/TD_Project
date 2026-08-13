# Propuesta — Diversidad de Evidencia (Caso 5C, previo a decidir cómo cerrar la limitación de dataset)

Estado: **documento de apertura — previo a cualquier decisión, descarga, o modificación de
campaña**. Continúa directamente `AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V2.md` §5, que concluyó que
la limitación restante más severa del corpus (31 comparaciones, 6 estrategias, 3 gestores, pero **1
solo dataset/instrumento**) no es resoluble ejecutando más campañas sobre `BTCUSDT`. No es una fase
nueva del ciclo D-N implementada — plantea la decisión que debe resolverse antes de tocar código o
descargar ningún dato.

**No se descarga ningún dato en este documento. No se modifica `campana_corpus/`. No se elige
todavía entre las opciones planteadas.**

---

## 1. La brecha, en términos exactos

```
Corpus actual (31 comparaciones, Caso 5C V1 + V2)
        =
BTCUSDT
        +
un único rango temporal (2024-01-02 – 2025-01-02)
```

`AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V2.md` §4/§5 ya estableció que esto es una limitación
**estructural del repositorio**, no de la disciplina de campaña seguida hasta ahora — ninguna
tercera expansión sobre el mismo dataset la resuelve. Dos dimensiones de diversidad distintas
podrían cerrarla, y **no hay evidencia todavía de cuál importa más**:

- ¿Los patrones observados (ej. el perfil favorable de `VolatilitySizing`, la degeneración
  económica en timeframes cortos con `FixedFractional`/`FixedRisk`) cambian según el
  **instrumento**?
- ¿Esos mismos patrones cambian según el **período temporal**, incluso manteniendo el mismo
  instrumento?

Sin datos de ninguna de las dos dimensiones, no se puede saber cuál es la brecha más importante —
por eso esta propuesta no preselecciona una vía (mismo criterio que D-112, que dejó 3 opciones
abiertas hasta la ronda de decisiones).

---

## 2. Evidencia existente del pipeline de datos reales (verificado contra código)

**No hay que construir nada nuevo para obtener un segundo dataset real** — existe un pipeline
completo y ya usado una vez, en `exploration/laboratorio/datos_reales/`:

- `BinanceClient.cs` + `DescargadorVelas.cs`: descarga real contra la API de Binance, con
  paginación y reanudación (`PLAN_FASE2A.md` §3).
- `Program.cs` (`datos_reales/Program.cs:34-39`): `symbol`/`interval`/rango son `const string`/
  `DateTimeOffset` hardcodeados (`symbol = "BTCUSDT"`, `interval = "1m"`, rango fijo terminando en
  `2025-01-02`) — cambiar estos 3 valores y volver a ejecutar es el único cambio de código
  necesario para descargar un instrumento o rango distinto. La descarga real requiere opt-in
  explícito por variable de entorno (`DESCARGAR_BINANCE=DIA|ANIO`) — nunca ocurre por accidente al
  correr el proyecto.
- `ValidadorIntegridadDatos` (`datos_reales/Program.cs:62-84`): rechaza el dataset descargado si
  tiene huecos, duplicados, orden incorrecto, o velas inválidas — **política ya vigente de rechazo,
  no relleno automático** (`Program.cs:82`). No se "arregla" un dataset con problemas, se descarta.

**Separación entre descarga y evidencia congelada, ya existente, no a diseñar** (`PLAN_FASE2A.md`
§6, verificado en `datasets/reales/BTCUSDT/*/metadata.json`):

```
BinanceClient
        |
        v
CSV crudo + metadata.json (SHA-256 del crudo)   -- vive en datos_reales/raw/, registro de la descarga
        |
        v
Validacion de integridad (AptoParaCongelar)
        |
        v
Promocion MANUAL explicita a datasets/reales/{Simbolo}/1m/   -- paso humano, no automatico
        |
        v
Agregacion a otros timeframes, cada uno con su propio metadata.json
        (sourceSha256 del 1m + sha256 propio + aggregationVersion, ver ejemplo real abajo)
        |
        v
Campana Corpus (ComparadorGestores/PersistidorComparaciones) consume el dataset ya congelado
```

Ejemplo real verificado (`datasets/reales/BTCUSDT/1D/metadata.json`):
```json
{
  "sourceDataset": "BTCUSDT_2024-01-02_2025-01-02_1m.csv",
  "sourceSha256": "f1a9dcbe72bdbca65c5a7de55c776c209a63f8b3ecd93c59a5fca958e4ebded4",
  "sourceTimeframe": "1m", "targetTimeframe": "1D", "aggregationVersion": "1.0",
  "velaCount": 366, "velasCompletas": 366, "velasParciales": 0,
  "sha256": "1356dd242e5a67546389c27f9b0e48a0694cfac8bf1c0e17e092875884310e22"
}
```

**Consecuencia directa para esta propuesta**: el dataset descargado **ya es**, por diseño previo a
Caso 5C, un artefacto identificado por hash — nunca una dependencia dinámica de la ejecución
experimental. La precaución del auditor (no usar Binance como "fuente de verdad" dentro de la
campaña) ya está satisfecha por el pipeline existente; `campana_corpus/` seguiría leyendo
únicamente de `datasets/reales/`, igual que hoy, sin ninguna llamada de red en su propio código.
**Ninguna repetición futura de una comparación cambiaría porque el proveedor externo actualizó
datos** — el dataset consumido es el archivo congelado con su hash, no una consulta en vivo.

---

## 3. Vía A — Diversidad de instrumento

**Ejemplo concreto**: `ETHUSDT`, mismo rango temporal (`2024-01-02`–`2025-01-02`), mismo pipeline.

**Qué permite observar**:
- Si los patrones económicos observados en el corpus actual (degeneración con
  `FixedFractional`/`FixedRisk` en timeframes cortos, perfil favorable de `VolatilitySizing`) se
  mantienen entre activos distintos, o son específicos de `BTCUSDT`.
- Si la comparación relativa entre gestores depende de la estructura/liquidez/volatilidad propia
  del instrumento.

**Preguntas nuevas que introduce, sin resolver aquí**:
- Disponibilidad del histórico completo en Binance para el instrumento elegido en el mismo rango.
- Compatibilidad de formato — el pipeline ya es genérico por símbolo (`BinanceClient`/
  `DescargadorVelas` no están hardcodeados a BTC más allá de la constante en `Program.cs`), pero no
  se ha verificado contra un segundo símbolo real todavía.
- Diferencias de escala de precio/volumen entre instrumentos — si eso requiere algún ajuste en
  `Instrumento`/`ConfiguracionCostes` de la campaña, o si los mismos valores ya congelados
  (`TasaMargen=0.1m`, costes `0.001m`/`0.001m`) siguen siendo válidos sin recalibrar (D-030: no se
  ajustarían observando resultados, se mantendrían o se declararían explícitamente como una
  decisión aparte).

---

## 4. Vía B — Diversidad temporal

**Ejemplo concreto**: `BTCUSDT` en un rango anterior (ej. `2023-01-01`–`2024-01-01`), mismo
instrumento, mismo pipeline.

**Qué permite observar**:
- Estabilidad temporal — si el perfil relativo entre gestores se mantiene entre dos períodos de
  mercado distintos del mismo instrumento.
- Sensibilidad a régimen de mercado del período (el laboratorio ya tiene un clasificador de
  régimen congelado, `ClasificadorRegimenV1` — aunque D-117 ya excluyó explícitamente el régimen
  inferido como insumo de Capa 2, nada impide que la auditoría *describa* si el nuevo período cubre
  un régimen distinto, como contexto).
- Si los mismos patrones observados en 2024 (degeneración en timeframes cortos, etc.) se repiten en
  2023, o son un artefacto de ese año específico.

**Qué NO resuelve**: sigue siendo el mismo instrumento — no responde si un patrón depende de la
estructura propia de `BTCUSDT` frente a otro activo.

**Ventaja operativa sobre la Vía A**: no introduce ninguna pregunta nueva de compatibilidad de
formato/escala — es literalmente la misma descarga ya ejecutada una vez, con un rango distinto.

---

## 5. Posibilidad combinada — Vía C

Ejecutar ambas vías (un segundo instrumento + un segundo rango temporal), produciendo una matriz de
diversidad de 2 dimensiones en vez de 1. Mayor cobertura, pero:
- Duplica el trabajo de descarga/validación/congelación antes de cualquier campaña.
- Pospone más la posibilidad de auditar si *alguna* diversidad adicional ya cambia las
  conclusiones — si la Vía A o la Vía B por sí sola ya revela que los patrones son estables (o que
  no lo son), eso podría informar si vale la pena ejecutar la otra antes de comprometerse a ambas.

---

## 6. Decisión pendiente — D-121 (candidata)

**No se resuelve en este documento.** Queda para `DECISIONES_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md`
(o nombre equivalente), con la misma estructura que toda decisión anterior (opciones, criterio,
evidencia, resolución):

**D-121 (candidata) — ¿Qué dimensión de diversidad incorporar primero?**
- **Opción A** — Instrumento primero (§3).
- **Opción B** — Tiempo primero (§4).
- **Opción C** — Ambas, con una matriz definida por adelantado (§5).

Ninguna opción se preselecciona aquí.

---

## 7. Restricciones (heredadas y nuevas)

- **El dataset descargado se convierte en artefacto identificado antes de tocar cualquier
  campaña** — ningún componente de `campana_corpus/`/`ComparadorGestores`/`PersistidorComparaciones`
  llamaría a Binance directamente ni en ningún momento; siguen leyendo exclusivamente de
  `datasets/reales/`, exactamente como hoy (§2, ya garantizado por el pipeline existente, no una
  restricción nueva a implementar).
- **Congelación es manual, no automática** (`PLAN_FASE2A.md` §6, ya vigente) — ninguna descarga se
  promueve a `datasets/reales/` sin ese paso explícito, evitando que una campaña futura dependa de
  un dataset todavía no validado.
- **Ningún parámetro económico (`TasaMargen`, costes, `CapitalInicial`) se recalibra observando el
  nuevo dataset** (D-030) — si un instrumento nuevo requiere valores distintos por razones
  estructurales (no por ajuste a resultados), eso es una decisión aparte, explícita, no una
  consecuencia automática de esta propuesta.
- **No se generan datasets sintéticos para simular diversidad de mercado** — reafirmado
  explícitamente por el auditor en la revisión de `AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V2.md`, y
  ya evitado por diseño: ambas vías (A y B) usan el mismo pipeline de datos reales, no generación
  artificial.
- **Ningún baseline congelado se toca** (`caso1` a `caso5c-capa1-v1-experimental`).
- **`ComparadorGestores`/`PersistidorComparaciones`/`RenderizadorComparacionGestores` (Caso
  5B/5C Capa 1): sin modificación** — un dataset nuevo se consume exactamente igual que
  `BTCUSDT_2024-01-02_2025-01-02` hoy, sin ningún cambio de contrato.
- **No se descarga nada en esta propuesta ni en la ronda de decisiones que le sigue** — la
  descarga real solo ocurre después de D-121 resuelta y de una especificación de implementación
  posterior.

---

## 8. Fuera de alcance de este documento

No se descargó ningún dato. No se modificó `campana_corpus/`, `ComparadorGestores`,
`PersistidorComparaciones`, ni `datos_reales/`. No se resuelve D-121. No se decide si el instrumento
de la Vía A es `ETHUSDT` específicamente, ni qué rango exacto tomaría la Vía B — esos detalles,
igual que en toda propuesta previa de este proyecto, se fijan en la especificación de
implementación posterior a la decisión, no aquí.

---

## Próximo documento

`DECISIONES_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md`, resolviendo D-121 (Instrumento / Tiempo / Ambas).
Tras esa decisión, una especificación de implementación cubriría: símbolo/rango exacto a descargar,
verificación de compatibilidad del pipeline con el nuevo dataset, y solo entonces la ejecución real
de la descarga — antes de volver a tocar `campana_corpus/` con una nueva campaña que consuma el
dataset ampliado.
