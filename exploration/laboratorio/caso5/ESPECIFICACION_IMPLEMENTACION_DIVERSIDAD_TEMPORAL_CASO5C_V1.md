# Especificación de Implementación — Diversidad Temporal (Caso 5C, Vía B de D-121)

Estado: **especificación previa a implementación**. Traduce D-121 (`DECISIONES_DIVERSIDAD_
EVIDENCIA_CASO5C_V1.md`, Vía B — tiempo primero) a pasos de código concretos. **Ningún dato se
descarga en este documento. Ningún código se modifica en este documento.** No cubre análisis de
resultados, comparación entre períodos, ni Capa 2 — solo generación de evidencia comparable.

---

## 1. Rango temporal elegido

**`2023-01-02` – `2024-01-02`** (1 año completo, inmediatamente anterior al rango ya congelado
`2024-01-02`–`2025-01-02`) — contiguo sin solaparse, mismo tamaño de ventana (1 año) que el dataset
actual, para que la única diferencia estructural entre ambos corpus sea el período, no también la
duración de la ventana.

**Justificación de "por qué este rango y no otro"**: cualquier año anterior a 2024 cumpliría el
criterio de D-121 (mismo instrumento, período distinto) — se elige el año inmediatamente anterior
porque es el que Binance garantiza con mayor certeza tener datos completos de spot para `BTCUSDT`
(el par existe en Binance desde 2017), minimizando el riesgo de que
`ValidadorIntegridadDatos.Verificar` rechace el dataset por huecos de un listado reciente o
discontinuado. No se investiga si años anteriores (2022, 2021...) también estarían disponibles —
innecesario para esta expansión, y quedaría como candidato de una futura Vía B adicional si se
decidiera en otro momento.

---

## 2. Generación del dataset — 2 pasos, mismo pipeline ya existente

### Paso 1 — Descarga cruda (`datos_reales/`)

**`datos_reales/Program.cs:34-39` requiere 2 cambios de constante, nada más**:
```csharp
const string symbol = "BTCUSDT";           // sin cambio
const string interval = "1m";              // sin cambio
var finUtc = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero);   // antes: 2025-01-02
var inicioUtc = finUtc.AddYears(-1);        // ya calcula 2023-01-02 automaticamente via DESCARGAR_BINANCE=ANIO
```

**Riesgo de colisión de archivo, verificado contra código**: `rutaCsv`/`rutaMetadata`
(`Program.cs:42-43`) se nombran `{symbol}_{interval}_{sufijo}.csv` donde `sufijo` es `"1anio"` o
`"1dia_prueba"` — **no incluye el rango de fechas**. Descargar el nuevo período sobrescribiría
`datos_reales/raw/BTCUSDT_1m_1anio.csv` del período ya usado para congelar el dataset actual (si
ese archivo crudo todavía existe localmente) o, si no existe, simplemente generaría un archivo con
el mismo nombre que tendría el próximo período que se descargue en el futuro. **Esto no afecta el
dataset ya congelado en `datasets/reales/`** (que es inmutable una vez promovido, §6 de
`PLAN_FASE2A.md`) — el archivo en riesgo es solo el crudo intermedio en `raw/`, que ya es "registro
de la descarga original", no la fuente de verdad. Aun así, para no perder trazabilidad del crudo
2024-2025 si todavía se necesitara, el `sufijo` debe incluir el año antes de ejecutar esta descarga:
`sufijo = "1anio_2023"` (cambio de una línea en `Program.cs:41`, aplicado como parte de esta
especificación, no como generalización especulativa del componente).

**Ejecución real**: `DESCARGAR_BINANCE=ANIO dotnet run -c Release` desde `datos_reales/` — ~526
requests paginados (mismo volumen que la descarga original, según el comentario ya existente en
`DescargadorVelas.cs:19`), reanudable si se corta a mitad camino (`DescargadorVelas.cs:26-35`).

**Validación**: sin cambios — `ValidadorIntegridadDatos.Verificar` corre automáticamente
(`Program.cs:62`) y bloquea la congelación (`Environment.Exit(1)`) si hay huecos, duplicados, orden
incorrecto, o velas inválidas. Si el 2023 de `BTCUSDT` tuviera algún problema de esta naturaleza,
**la especificación se detiene ahí** — no se "repara" el dataset (política ya vigente, no nueva).

### Paso 2 — Congelación manual + agregación a timeframes (`agregador/`)

**Congelación manual** (§6 de `PLAN_FASE2A.md`, sin cambio de proceso): copiar el CSV validado de
`datos_reales/raw/` a `datasets/reales/BTCUSDT/1m_2023/` — **nueva subcarpeta, no
`datasets/reales/BTCUSDT/1m/`**, para no mezclar ni sobrescribir el dataset 2024-2025 ya congelado
y consumido por Caso 5C V1/V2 (ver §3 sobre nomenclatura).

**`agregador/Program.cs` requiere generalización, no solo cambio de constante** — a diferencia de
`datos_reales/Program.cs`, el rango `2024-01-02_2025-01-02` está hardcodeado directamente en la
construcción de cada nombre de archivo (`Program.cs:24,61,85`), no en una variable aparte. Cambio
mínimo: extraer esas 3 ocurrencias a una variable `sufijoRango` (ej. derivada del nombre del CSV de
entrada, o parametrizada igual que `symbol`/`interval` en `datos_reales/Program.cs`), sin alterar
`AgregadorMultiTimeframe.Agregar`/`EscritorDerivado` (ya genéricos, reciben rutas y velas como
parámetro — verificado, no requieren cambio).

**Ejecución real**: `GENERAR_TODOS_TIMEFRAMES=1 dotnet run -c Release` desde `agregador/`, apuntando
al nuevo `dirBase1m` — genera los 13 timeframes derivados con su propio `metadata.json`
(`sourceSha256`/`sha256`/`aggregationVersion`, mismo formato que el dataset actual, sin cambio de
esquema).

**Verificación manual del agregador** (`Program.cs:98-126`, ya existente): corre automáticamente
sobre el nuevo dataset — compara las primeras 60 velas 1m contra la primera vela 1h agregada,
bloquea (`Environment.Exit(1)`) si no coinciden. Ninguna generalización nueva de esta verificación
es necesaria — ya opera sobre las velas recibidas como parámetro, no sobre el rango hardcodeado.

---

## 3. Ubicación final

```
datasets/reales/BTCUSDT/
 ├── 1m/          <- dataset 2024-2025 ya congelado, SIN TOCAR
 ├── 1D/ ...       <- 13 timeframes 2024-2025 ya congelados, SIN TOCAR
 ├── 1m_2023/      <- NUEVO: CSV crudo validado + metadata.json (source)
 ├── 1D_2023/      <- NUEVO: 13 timeframes derivados, cada uno con su metadata.json
 └── ...
```

**Por qué sufijo `_2023` en el nombre de carpeta, no una carpeta `2023/` separada por instrumento**:
mantiene `BTCUSDT/` como único punto de entrada para el símbolo (consistente con cómo
`campana_corpus/`/`Caso5.csproj` ya resuelven `dirDatasets` apuntando a `datasets/reales/BTCUSDT/`,
§5) — evita introducir una segunda convención de estructura de carpetas para el mismo instrumento.

**Nombre de archivo dentro de cada carpeta**: `BTCUSDT_2023-01-02_2024-01-02_{timeframe}.csv` —
mismo patrón que el dataset actual (`BTCUSDT_2024-01-02_2025-01-02_{timeframe}.csv`), con el rango
correcto, generado automáticamente por la generalización de §2 Paso 2.

---

## 4. Integración con `campana_corpus/`

**No se modifica `ComparadorGestores`/`PersistidorComparaciones`** — reciben `EntradaProtocolo` con
`DirDatasets`/`NombreDataset` como parámetros, agnósticos de qué dataset apuntan (mismo criterio ya
establecido en Caso 5B/5C Capa 1).

**`campana_corpus/ProgramCampanaCorpus.cs` requiere 2 cambios, no una reescritura**:
- `dirDatasets` pasa a apuntar a `datasets/reales/BTCUSDT/` (sin cambio de ruta base — cada
  timeframe se resuelve dentro, igual que hoy) **más** una segunda variable `dirDatasets2023`
  apuntando a las carpetas `*_2023`.
- `NombreDataset: "BTCUSDT_2024-01-02_2025-01-02"` se reemplaza por
  `NombreDataset: "BTCUSDT_2023-01-02_2024-01-02"` **solo** en el bloque de la nueva sub-campaña
  temporal (ver §5) — el resto del código (sub-campañas V1/A/B/C de la expansión ya congelada)
  permanece exactamente igual, sin tocar el dataset 2024-2025.

**Verificación previa a ejecutar cualquier campaña sobre el dataset nuevo**: confirmar que
`EjecutorProtocolo`/`LectorDerivado` (que ya leen el formato CSV de timeframes derivados, sin
cambios desde Caso 1) aceptan el nuevo dataset sin modificación — se espera que sí, porque el
formato de archivo generado por `EscritorDerivado` es idéntico (mismas columnas, mismo esquema de
metadata), pero se confirma con una ejecución de prueba antes de la campaña completa (ver §7, P1).

---

## 5. Campañas que se ejecutarán sobre el nuevo rango

**Misma estructura que V1/V2, sin ninguna sub-campaña de novedad conceptual** — una única
sub-campaña temporal (**Sub-campaña D**, continuando la nomenclatura ya usada en V2):

```
6 estrategias (las mismas 6 ya usadas en V2: Tres Mosqueteros, Ema Cross,
               ZScore Reversion, Neutral, Volumen Breakout, Mhi Mayoria)
    |
    +-- 15m / 1h / 1D   (mismos 3 timeframes ya usados en V1/V2)
          +-- FixedFractional / FixedRisk / VolatilitySizing   (mismos 3 gestores)
```

6 estrategias × 3 timeframes × 3 gestores = 18 comparaciones nuevas, 54 corridas internas —
deliberadamente la **misma matriz completa** que ya se usó para el dataset 2024-2025 (V1 + sub-
campaña A combinadas), para que la futura auditoría pueda comparar exactamente las mismas 18
combinaciones estrategia×timeframe entre ambos períodos, sin huecos de cobertura en un lado que no
existan en el otro.

**No se repite aquí la sub-campaña de evidencia parcial (equivalente a la C de V2)** — ya está
representada en el corpus general con el dataset 2024-2025; repetirla con el dataset 2023 no
aportaría una dimensión nueva (un dataset inexistente falla igual sin importar qué año se le pida).

**Total del corpus tras esta expansión**: 31 (V1+V2) + 18 (Vía B) = **49 comparaciones
acumuladas**.

---

## 6. Qué no debe incluir esta implementación

- Ningún análisis de si los resultados 2023 difieren de 2024 — eso es tarea de la auditoría
  posterior, no de la generación.
- Ninguna comparación directa entre períodos calculada por código (ej. ningún componente que reciba
  ambos corpus y calcule una diferencia) — cada campaña persiste su corpus de forma independiente,
  igual que V1/V2 nunca compararon entre sí en código, solo en la auditoría posterior (texto).
- Ninguna recomendación ni conclusión sobre qué período es "mejor" o más representativo.
- Ningún cambio de parámetros económicos (`TasaMargen`, costes, `CapitalInicial`) ni de gestores
  para el dataset 2023 — se usan exactamente los mismos valores ya congelados (D-030, reafirmado en
  D-121 §Restricciones).
- Ninguna descarga de un tercer rango o de un instrumento distinto — fuera del alcance de esta
  especificación (Vía A queda pendiente de una propuesta futura separada, D-121).

---

## 7. Pruebas de identidad y trazabilidad

Ubicación: extender `caso5/campana_corpus/TestsCampanaCorpus.cs` con las pruebas específicas de
esta expansión (no un archivo nuevo, mismo criterio que V2 amplió el mismo archivo de V1):

1. **P1 — Compatibilidad de formato confirmada antes de la campaña completa**: leer 1 archivo del
   dataset 2023 recién generado (ej. `1D_2023`) con `LectorDerivado`/`EjecutorProtocolo` y confirmar
   que produce un `ResultadoCorridaTimeframe` con `Estado: Success` para al menos 1 estrategia —
   antes de comprometerse a las 18 comparaciones completas.
2. **P2 — Identidad de dataset distinta y estable**: `IdentidadExperimentoCompleta` calculada sobre
   una `EntradaProtocolo` con `NombreDataset: "BTCUSDT_2023-01-02_2024-01-02"` produce un
   `DatasetSourceSha256` distinto del ya congelado para 2024-2025 (confirma que el sistema
   distingue ambos datasets por hash, no solo por el string de nombre) — y estable entre 2
   ejecuciones consecutivas contra el mismo dataset 2023 (mismo hash ambas veces).
3. **P3 — Matriz completa de la sub-campaña D**: `6 × 3 × 3 == 54` ejecuciones internas, 18
   comparaciones persistidas (mismo patrón P1/P4 de V1/V2).
4. **P4 — Metadata de agregación coherente**: cada `metadata.json` de los timeframes derivados 2023
   tiene `sourceDataset`/`sourceSha256` apuntando al CSV 1m 2023 (no al 2024-2025 por error de
   copia/reutilización de variable).
5. **P5 — Dataset 2024-2025 intacto**: `git status --porcelain -- exploration/laboratorio/
   datasets/reales/BTCUSDT/1m/ exploration/laboratorio/datasets/reales/BTCUSDT/1D/` (y los demás 12
   timeframes ya congelados) vacío tras la ejecución completa de esta especificación — ninguna
   escritura accidental sobre el dataset original.

Suite de producción (`dotnet test -c Release`) debe permanecer en 126/126. Suite de
`caso5/Caso5.csproj` debe permanecer en 25/25 — esta expansión vive en `campana_corpus/`/
`datos_reales/`/`agregador/`, ninguno de los cuales `Caso5.csproj` compila.

---

## 8. Fuera de alcance de esta especificación

No se descargó ningún dato. No se ejecutó ninguna comparación. No se audita el corpus resultante —
queda para un documento posterior (`AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V3.md` o nombre
equivalente, a decidir tras la ejecución). No se decide todavía si/cuándo se abre la Vía A
(instrumento) — D-121 ya estableció que queda pendiente de una futura propuesta, no de esta
especificación. No se generaliza `agregador/Program.cs` más allá de lo estrictamente necesario para
soportar un segundo rango (sin convertirlo en una herramienta de propósito general parametrizable
por CLI — eso sería una ampliación no solicitada).

---

## Próximo paso

Autorización explícita del auditor para implementar: cambio de constantes en
`datos_reales/Program.cs` (símbolo sin cambio, rango 2023, sufijo de archivo crudo con año),
generalización mínima de `agregador/Program.cs` (rango como variable, no hardcodeado 3 veces),
ejecución real de la descarga (`DESCARGAR_BINANCE=ANIO`) y agregación
(`GENERAR_TODOS_TIMEFRAMES=1`), congelación manual a `datasets/reales/BTCUSDT/*_2023/`, extensión de
`campana_corpus/` con la Sub-campaña D (18 comparaciones), y las 5 pruebas de §7. Tras esto, el
siguiente documento es una auditoría de corpus que compare el dataset 2023 recién generado contra
el corpus 2024-2025 ya existente (49 comparaciones acumuladas en total).
