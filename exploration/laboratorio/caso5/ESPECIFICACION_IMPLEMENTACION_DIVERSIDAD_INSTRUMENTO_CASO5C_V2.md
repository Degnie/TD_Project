# Especificación de Implementación — Diversidad de Instrumento (Caso 5C, D-125)

Estado: **especificación previa a implementación**. Traduce D-125 (`DECISIONES_DIVERSIDAD_
INSTRUMENTO_CASO5C_V2.md`) a pasos de código concretos. **Ningún dato se descarga en este
documento. Ningún código se modifica en este documento. No se confirma si `ETHUSDT` es viable —
eso es el primer paso técnico *después* de aprobar esta especificación, no algo que se resuelva
aquí.** No cubre análisis del corpus ampliado, comparación instrumento-vs-instrumento, ni ninguna
forma de recomendación.

---

## 1. Exploración de disponibilidad para el instrumento candidato

**Reutiliza `ExploradorDisponibilidad.ExplorarAsync` (`datos_reales/ExploradorDisponibilidad.cs`)
sin ningún cambio de código** — ya es genérico por `symbol`/`interval` (firma:
`ExplorarAsync(BinanceClient cliente, string symbol, string interval, DateTimeOffset inicioAnio,
DateTimeOffset finAnio)`), construido explícitamente para D-122 con ese propósito. Nada en su
implementación asume `BTCUSDT` — la constante vive únicamente en `datos_reales/Program.cs:24`,
fuera del componente.

**Rango a explorar**: `2024-01-02`–`2025-01-02` (el mismo rango ya congelado para `BTCUSDT`, no el
2022-2023) — obligado por D-121/D-125: si varía instrumento, el tiempo debe mantenerse fijo, o
cualquier diferencia observada entre ambos corpus sería inatribuible a una sola dimensión.

**Mecanismo de invocación**: mismo patrón ya implementado en `datos_reales/Program.cs:31-50`
(`EXPLORAR_DISPONIBILIDAD_ANIO`) — requiere una generalización mínima de una línea: hoy el símbolo
de la exploración es la constante fija `symbol="BTCUSDT"` (`Program.cs:24`), reutilizada tal cual
en la llamada de exploración (`Program.cs:43`). Para explorar `ETHUSDT` sin tocar el flujo de
descarga real de `BTCUSDT`, el símbolo a explorar debe leerse de una variable de entorno separada
opcional (`EXPLORAR_DISPONIBILIDAD_SYMBOL`, default `symbol` si no se define) — cambio de una línea
en el bloque de exploración existente, sin alterar `ExploradorDisponibilidad` ni el bloque de
descarga real (Etapa 4).

**Veredicto esperado, mismo criterio que D-122**: `ResultadoExploracion.TodosContinuos` (12/12
meses sin huecos) decide viabilidad — no hay umbral parcial, mismo criterio binario que ya rechazó
el rango 2023-2024 de `BTCUSDT` por un hueco real de 80 minutos
(`HALLAZGO_RECHAZO_DATASET_2023_CASO5C_V1.md`). Si `ETHUSDT` no pasa, **la especificación se
detiene ahí para ese instrumento** — D-125 prohíbe sustitución automática por "el activo que
funcione"; un segundo candidato requeriría una decisión aparte, explícita, no una búsqueda
silenciosa.

---

## 2. Separación descarga / exploración / congelación

**Ya garantizada estructuralmente, sin cambio necesario** — mismas 3 etapas ya separadas en
`datos_reales/Program.cs`, reafirmadas aquí porque D-125 las hereda de D-122 sin excepción:

1. **Exploración** (§1): `ExploradorDisponibilidad.ExplorarAsync` no recibe ningún parámetro de
   ruta de archivo — no puede escribir un dataset por construcción, no por disciplina de uso
   (`ExploradorDisponibilidad.cs:46-49`, comentario ya existente en el código).
2. **Descarga cruda** (Etapa 4, `Program.cs:52-129`): opt-in explícito vía `DESCARGAR_BINANCE`,
   nunca se combina con exploración en la misma invocación (ya impuesto por el código actual,
   `Program.cs:27-29`). Requiere el mismo cambio de constante que cualquier expansión de
   instrumento: `symbol = "ETHUSDT"` en `Program.cs:24` — único cambio de código en esta etapa.
   Sufijo de archivo crudo debe incluir el símbolo para no colisionar con
   `datos_reales/raw/BTCUSDT_1m_1anio_2025.csv`: `sufijo = $"1anio_{finUtc.Year}"` ya incluye el
   año pero no el símbolo — como `rutaCsv`/`rutaMetadata` (`Program.cs:81-82`) ya interpolan
   `{symbol}` como primer componente del nombre, el archivo resultante
   (`ETHUSDT_1m_1anio_2025.csv`) ya es distinto sin cambio adicional — verificado, no requiere
   generalización.
3. **Congelación manual** (§6 `PLAN_FASE2A.md`, sin cambio de proceso): copiar el CSV validado a
   `datasets/reales/ETHUSDT/1m/` — carpeta nueva por instrumento (paralela a `BTCUSDT/`, no una
   subcarpeta de ella), luego `agregador/` genera los 13 timeframes derivados. Ningún paso de esta
   etapa es automático; cada congelación requiere confirmación manual explícita del auditor sobre
   el veredicto de `ValidadorIntegridadDatos`, igual que las 2 congelaciones previas.

---

## 3. Estructura del nuevo dataset

```
datasets/reales/
 ├── BTCUSDT/              <- SIN TOCAR (1m..1D, 2024-2025, ya congelado)
 ├── BTCUSDT_2022/          <- SIN TOCAR (vista de compatibilidad, Sub-campaña D)
 └── ETHUSDT/               <- NUEVO, mismo patrón interno que BTCUSDT/
      ├── 1m/                <- CSV crudo validado + metadata.json (source)
      ├── 1D/ ...            <- 13 timeframes derivados, cada uno con su metadata.json
      └── ...
```

**Carpeta por instrumento al mismo nivel que `BTCUSDT/`, no una subcarpeta ni un sufijo** —
distinto del caso de diversidad temporal (`BTCUSDT_2022/`, sufijo porque el instrumento no
cambiaba). Aquí el instrumento sí cambia, así que es la dimensión que debe reflejarse en el nombre
de la carpeta raíz, consistente con cómo `campana_corpus/` ya resuelve `dirDatasets` como una ruta
completa por variable (§5), no con una convención de sufijo que mezclaría dos dimensiones distintas
bajo el mismo nombre de carpeta base.

**Nombre de archivo dentro de cada carpeta**: `ETHUSDT_2024-01-02_2025-01-02_{timeframe}.csv` —
mismo patrón ya usado por ambos datasets existentes, con símbolo e instrumento correctos.

**`agregador/Program.cs`**: ya generalizado para rango como variable (`sufijoRango`, hecho durante
la implementación de diversidad temporal) — requiere el mismo tipo de generalización para símbolo
si `symbol="BTCUSDT"` sigue hardcodeado en la construcción de nombres de archivo del agregador; se
confirma contra el código actual antes de implementar (no asumido aquí), y si ya está parametrizado
por el nombre del CSV de entrada (como el resto del pipeline), no requiere cambio adicional.

---

## 4. Compatibilidad con el manifiesto del corpus

**Sin cambio de mecanismo** — `MANIFIESTO_CORPUS_CASO5C_V1.json` ya declara comparaciones por
contenido (carpeta + origen + nota opcional), agnóstico de qué instrumento contiene cada
comparación. Las 18 nuevas comparaciones de `ETHUSDT` se añaden a `comparaciones[]` con
`"origen": "SubcampanaE"` (continuando la nomenclatura de letra ya usada: V1, V2, SubcampanaD),
siguiendo exactamente el mismo criterio de inspección de contenido (JSON/config/hashes/estado/
estrategia/timeframe/periodo/gestor) ya aplicado 3 veces — nunca por timestamp ni por asunción de
que "la carpeta más reciente es la oficial".

**`LectorCorpus`/`AnalisisDescriptivo`/`DetectorRelaciones` no requieren ningún cambio** — ya leen
`NombreDataset`/`CarpetaOrigen` como strings agnósticos, sin ninguna lógica condicionada a
`BTCUSDT` en particular (confirmado por inspección: Capa 2 y análisis interpretativo solo verifican
que el corpus contiene exclusivamente `BTCUSDT` como *hallazgo descriptivo* en el texto de
`Limitaciones`, no como una restricción estructural en el código). Ese texto fijo de `Limitaciones`
sí deberá actualizarse una vez el corpus se amplíe — fuera de alcance de esta especificación
(pertenece a la fase de auditoría posterior a la campaña, no a la generación de evidencia).

**Total del corpus tras esta expansión, si `ETHUSDT` pasa validación**: 49 (V1+V2+SubcampanaD) + 18
(SubcampanaE) = **67 comparaciones acumuladas**.

---

## 5. Adaptación de la campaña comparable

**`campana_corpus/ProgramCampanaCorpus.cs` requiere el mismo tipo de extensión ya usada para
Sub-campaña D (`ProgramCampanaCorpus.cs:60-61,172,185`), no una reescritura**:

- Nueva variable `dirDatasetsEth` apuntando a `datasets/reales/ETHUSDT/` (mismo patrón que
  `dirDatasets2022:60`).
- Nueva variable `nombreDatasetEth = "ETHUSDT_2024-01-02_2025-01-02"` (mismo patrón que
  `nombreDataset2022:61`).
- Nuevo bloque **Sub-campaña E**: `EjecutarMatriz(estrategiasTodas, dirDatasetsEth,
  nombreDatasetEth)` (mismo patrón que `carpetasD:172`) — **misma matriz completa ya usada 2 veces**
  (6 estrategias × 3 timeframes × 3 gestores = 18 comparaciones, 54 corridas internas), para que la
  futura auditoría compare exactamente las mismas 18 combinaciones entre instrumentos, sin huecos
  de cobertura en un lado que no existan en el otro.
- `entradaEth = entrada2024 with { DirDatasets = dirDatasetsEth, NombreDataset = nombreDatasetEth }`
  (mismo patrón `with` que `entrada2022:185`) — **ningún parámetro económico cambia**
  (`TasaMargen`/costes heredados de `entrada2024` sin modificación, D-030/D-125).

**No se repite ninguna sub-campaña de evidencia parcial** (equivalente a la C de V2) — mismo
razonamiento que Sub-campaña D: ya representada en el corpus general, repetirla con instrumento
distinto no aporta una dimensión nueva.

---

## 6. Pruebas — no se modifica BTCUSDT, no se alteran parámetros, no se filtran resultados, no se introduce selección

Ubicación: extender `caso5/campana_corpus/TestsCampanaCorpus.cs` (mismo archivo, mismo criterio de
extensión ya usado para P1-P5 de Sub-campaña D).

1. **P1 — Compatibilidad de formato confirmada antes de la campaña completa**: leer 1 archivo del
   dataset `ETHUSDT` recién generado con `LectorDerivado`/`EjecutorProtocolo`, confirmar
   `Estado: Success` para al menos 1 estrategia, antes de comprometerse a las 18 comparaciones.
2. **P2 — Identidad de dataset distinta y estable**: `IdentidadExperimentoCompleta` sobre una
   `EntradaProtocolo` con `NombreDataset: "ETHUSDT_2024-01-02_2025-01-02"` produce un
   `DatasetSourceSha256` distinto tanto del dataset `BTCUSDT` 2024-2025 como del `BTCUSDT_2022` —
   estable entre 2 ejecuciones consecutivas contra el mismo dataset `ETHUSDT`.
3. **P3 — Matriz completa de la Sub-campaña E**: `6 × 3 × 3 == 54` ejecuciones internas, 18
   comparaciones persistidas.
4. **P4 — Metadata de agregación coherente**: cada `metadata.json` de los timeframes derivados
   `ETHUSDT` apunta a `sourceDataset`/`sourceSha256` del CSV 1m `ETHUSDT` (nunca a `BTCUSDT` por
   error de reutilización de variable).
5. **P5 — `BTCUSDT` intacto, ambos datasets**: `git status --porcelain -- exploration/laboratorio/
   datasets/reales/BTCUSDT/ exploration/laboratorio/datasets/reales/BTCUSDT_2022/` vacío tras la
   ejecución completa — ninguna escritura accidental sobre ninguno de los 2 datasets `BTCUSDT` ya
   congelados.
6. **P6 — Parámetros económicos sin cambio**: `entradaEth.TasaMargen`/costes son
   `==` byte a byte a los de `entrada2024` (comparación directa de campos, no solo de tipo) —
   confirma que la extensión de §5 no introdujo ningún ajuste, ni accidental ni deliberado, para el
   nuevo instrumento (D-030/D-125).
7. **P7 — Ausencia estructural de criterios de selección**: mismo mecanismo de reflexión ya usado
   en Capa 2 (P5) y análisis interpretativo (P5) — ningún tipo nuevo introducido por esta expansión
   (`EntradaProtocolo`, `IdentidadExperimentoCompleta`, resultados de campaña) contiene un campo con
   términos prohibidos (`mejor`/`ganador`/`ranking`/`score`/`recomend`/`elegir`/`preferido`) — la
   extensión de código no añade ningún tipo nuevo, así que esta prueba confirma negativamente
   (ausencia de tipos nuevos) tanto como positivamente (los tipos reutilizados siguen limpios).

Suite de producción (`dotnet test -c Release`) debe permanecer en 126/126. Suite de
`caso5/Caso5.csproj` debe permanecer en 25/25 — esta expansión vive en `campana_corpus/`/
`datos_reales/`/`agregador/`, ninguno de los cuales `Caso5.csproj` compila.

---

## 7. Qué no debe incluir esta implementación

- Ninguna descarga real, en este documento — la especificación se aprueba primero, la exploración y
  descarga ocurren después, como pasos separados y explícitamente autorizados.
- Ninguna elección de `ETHUSDT` por resultado esperado, ni descarte por resultado desfavorable —
  la única causa de rechazo es integridad de datos (§1), igual que D-121/D-122/D-125 exigen.
- Ningún cambio de parámetros económicos ni de gestores para el dataset `ETHUSDT` — mismos valores
  congelados que `BTCUSDT` (verificado por P6).
- Ningún análisis del corpus ampliado, ninguna comparación instrumento-vs-instrumento calculada por
  código, ninguna conclusión sobre qué instrumento es "mejor" o más representativo — tarea de una
  auditoría posterior, no de esta generación.
- Ninguna activación ni acercamiento a D-118/D-119/D-120.
- Ningún segundo instrumento candidato si `ETHUSDT` falla la exploración — requeriría una decisión
  aparte, explícita, no una sustitución automática.

---

## Próximo paso

Autorización explícita del auditor para implementar: generalización mínima de la exploración
(`EXPLORAR_DISPONIBILIDAD_SYMBOL`, 1 línea en `datos_reales/Program.cs`), ejecución de la
exploración de disponibilidad para `ETHUSDT` sobre el rango 2024-01-02–2025-01-02 (§1). **Solo si
el veredicto es viable**: cambio de constante `symbol="ETHUSDT"`, descarga real
(`DESCARGAR_BINANCE=ANIO`), congelación manual, agregación, extensión de `campana_corpus/` con la
Sub-campaña E (18 comparaciones), y las 7 pruebas de §6. Si el veredicto no es viable, el siguiente
documento es un hallazgo de rechazo (mismo patrón que
`HALLAZGO_RECHAZO_DATASET_2023_CASO5C_V1.md`), no una implementación.
