# Especificación de Implementación — Caso 5C Capa 2 (Análisis Descriptivo del Corpus)

Estado: **especificación previa a implementación**. Traduce `DECISIONES_CASO5C_CAPA2_V1.md` (D-123)
a diseño de código concreto. **Ningún código se modifica en este documento.** No implementa ninguna
forma de recomendación, ranking, ni selección — D-118/D-119/D-120 permanecen en estado de principio,
sin activación aquí.

---

## 1. Mecanismo de ejecución y fuente de datos

**Ejecutable separado, no una extensión de `caso5/Program.cs` ni de `campana_corpus/`** — mismo
criterio ya usado en `campana_corpus/` respecto a `Caso5.csproj` (`ESPECIFICACION_IMPLEMENTACION_
CAMPANA_CORPUS_CASO5C_V1.md` §1): un componente de análisis es una responsabilidad distinta de
generar evidencia (`campana_corpus/`) o de verificarla (`Caso5.csproj`).

**Ubicación**: `exploration/laboratorio/caso5/analisis_corpus/`
- `AnalisisCorpus.csproj` — sin `<ProjectReference>` a `src/` más allá de lo que ya requieren los
  tipos leídos (ninguno directamente — ver §2, el análisis no reconstruye `MetricasFinancieras` en
  memoria, lee texto ya persistido).
- `ProgramAnalisisCorpus.cs` — punto de entrada top-level statements, mismo estilo que
  `campana_corpus/ProgramCampanaCorpus.cs`.
- `LectorCorpus.cs` — componente nuevo, lee `caso5/resultados/` del disco.
- `AnalisisDescriptivo.cs` — componente nuevo, calcula las 4 estructuras de salida (§4) a partir de
  lo que `LectorCorpus` devuelve.

**Fuente de datos — exclusivamente lo ya persistido, ninguna ejecución nueva**: `LectorCorpus` lee
`IDENTIDAD_COMPARACION.json` + `COMPARACION_GESTORES_V1.md` de cada carpeta bajo `caso5/resultados/`
— **nunca llama a `ComparadorGestores.Comparar`, `EjecutorProtocolo.Ejecutar`, ni ningún componente
que ejecute un backtest**. Esto no es una preferencia de diseño, es la única forma de cumplir la
autorización explícita ("ninguna ejecución nueva como parte del análisis") — la evidencia que Capa 2
describe es exactamente la que Capa 1 ya congeló, ni un cálculo más.

**Por qué hace falta leer el `.md`, no solo el `.json`**: `PersistidorComparaciones.Persistir`
(Caso 5C Capa 1, congelado, verificado P5) **no incluye ninguna métrica numérica en
`IDENTIDAD_COMPARACION.json`** — el JSON solo tiene `estrategia`/`timeframe`/`nombreDataset`/
`gestores[].identidad`/`gestores[].estado`/`fechaGeneracionUtc`. Las métricas
(`PnLTotal`/`DrawdownMaximoPct`/`ProfitFactor`/`ExposicionMaxima`/`CashFinal`/`EquityFinal`) existen
únicamente como texto en `COMPARACION_GESTORES_V1.md`, con el formato fijo que
`RenderizadorComparacionGestores.Generar` produce (`ComparadorGestores.cs:76-104`, congelado). El
análisis descriptivo debe parsear ese formato de texto — no hay otra fuente.

---

## 2. `LectorCorpus` — lectura, sin interpretación

```csharp
namespace TD_Project.Caso5.AnalisisCorpus;

public sealed record FilaCorpus(
    string Estrategia,
    string Timeframe,
    string NombreDataset,
    string IdentidadGestor,
    string Estado,               // texto crudo de EstadoCorridaTimeframe, sin reinterpretar
    decimal? PnLTotal,
    decimal? DrawdownMaximoPct,
    decimal? ProfitFactor,
    decimal? ExposicionMaxima,
    decimal? CashFinal,
    decimal? EquityFinal,
    string CarpetaOrigen);       // ruta completa — trazabilidad obligatoria (D-120, EvidenciaUsada)

public static class LectorCorpus
{
    // Lee cada carpeta de dirResultados, une IDENTIDAD_COMPARACION.json (estructura) +
    // COMPARACION_GESTORES_V1.md (metricas por gestor, texto). Una fila por gestor por
    // comparacion (misma granularidad que FilaComparacionGestor, D-114). Metricas null si el
    // gestor no tuvo Success (mismo criterio D-072/D-077 ya aplicado en ComparadorGestores).
    public static IReadOnlyList<FilaCorpus> Leer(string dirResultados);
}
```

**Parseo del `.md`**: por línea, `"  {NombreCampo}: {valor}"` dentro del bloque de cada
`"Gestor: {identidad}"` — el formato es fijo y congelado (`RenderizadorComparacionGestores`, D-115),
así que el parseo es determinista, no heurístico. `"null"` (string literal que el renderizador ya
escribe para `DrawdownMaximoPct`/`ProfitFactor` ausentes) se lee como `decimal?` nulo. El bloque
`"Métricas: (no disponibles — corrida no exitosa)"` produce una fila con las 6 métricas en `null`.

**Ninguna fila se descarta, reordena, ni filtra por valor** — `LectorCorpus` es una traducción
mecánica de disco a estructura en memoria, no un primer paso de análisis. Cualquier filtrado
(ej. "solo timeframe 1D") ocurre en `AnalisisDescriptivo` o en la capa de presentación, nunca aquí,
y siempre declarado en la salida (D-120: `CondicionesCubiertas`/cobertura visible).

**Qué carpetas se excluyen**: ninguna decidida por contenido — `LectorCorpus` no descarta una
comparación porque sus métricas sean "malas" o "buenas" (eso sería exactamente el tipo de selección
que D-123 prohíbe). La única exclusión posible es estructural: una carpeta sin
`IDENTIDAD_COMPARACION.json` válido no es evidencia de Capa 1, se ignora con una advertencia
explícita en la salida de cobertura, nunca en silencio.

**Precisión — evidencia incompleta no es ausencia de evidencia**: una fila cuyo `Estado` sea
`Failed`/`Incomplete` (métricas en `null`, mismo criterio D-072/D-077) **se incluye en
`CoberturaAnalizada` igual que cualquier otra fila** — cuenta en `TotalFilas` y en los
diccionarios `ComparacionesPor*`, participa en `CarpetasIgnoradas` solo si le falta el JSON
estructural, nunca porque su corrida no fue exitosa. `LectorCorpus` **no elimina silenciosamente
ninguna comparación fallida/incompleta** — D-119 necesita poder distinguir "esta combinación no
tiene evidencia" (ninguna carpeta) de "esta combinación se intentó y no fue exitosa" (carpeta
presente, `Estado` distinto de `Success`, métricas `null`), y solo `LectorCorpus` tiene el dato
crudo para preservar esa distinción. Los cálculos que sí requieren valor numérico
(`CalcularDistribucion`/`CompararPeriodos`/`EstadisticaDescriptiva`) simplemente excluyen las filas
con métrica `null` de su cálculo — pero la fila sigue contada en `CoberturaAnalizada`, visible como
evidencia existente aunque no aritméticamente utilizable.

---

## 3. `AnalisisDescriptivo` — las 4 estructuras autorizadas

Nombres deliberadamente descriptivos (no `Ganador`/`MejorGestor`/`Ranking`/`Score`), por la misma
razón que el auditor señaló: el lenguaje del código debe preservar los límites metodológicos que
D-123 fija.

```csharp
// Cobertura — que combinaciones de la matriz declarada tienen evidencia, y cuantas.
public sealed record CoberturaAnalizada(
    int TotalFilas,
    IReadOnlyDictionary<string, int> ComparacionesPorEstrategia,
    IReadOnlyDictionary<string, int> ComparacionesPorTimeframe,
    IReadOnlyDictionary<string, int> ComparacionesPorGestor,
    IReadOnlyDictionary<string, int> ComparacionesPorDataset,   // clave = NombreDataset (distingue periodo)
    IReadOnlyList<string> CarpetasIgnoradas);                   // estructura invalida, nunca silencioso

// Distribucion de una metrica, agrupada — nunca colapsada a un solo valor "representativo".
public sealed record DistribucionMetrica(
    string NombreMetrica,
    string AgrupadoPor,          // "Gestor" | "Timeframe" | "Dataset" — una sola dimension por instancia
    IReadOnlyDictionary<string, EstadisticaDescriptiva> PorGrupo);

public sealed record EstadisticaDescriptiva(
    int Cantidad,                // cuantas filas (con metrica no-null) sostienen esta entrada
    decimal Minimo,
    decimal Maximo,
    decimal Media,
    decimal Mediana);

// Comparacion temporal descriptiva — presencia/ausencia de un patron en cada periodo, nunca un
// veredicto de "es robusto"/"es confiable".
public sealed record ComparacionPeriodos(
    string NombreMetrica,
    string Gestor,
    IReadOnlyDictionary<string, EstadisticaDescriptiva> PorDataset);  // 1 entrada por NombreDataset

// Resumen — agrega las 3 estructuras anteriores mas los casos atipicos, con cobertura declarada
// (D-120: EvidenciaUsada). Es el unico tipo pensado como salida final compuesta.
public sealed record ResumenCorpus(
    CoberturaAnalizada Cobertura,
    IReadOnlyList<DistribucionMetrica> Distribuciones,
    IReadOnlyList<ComparacionPeriodos> ComparacionesTemporal,
    IReadOnlyList<CasoAtipico> CasosAtipicos,
    string Limitaciones);        // texto fijo (ver §5) — declaracion obligatoria, no opcional

// Caso atipico - hecho observable, sin evaluacion de si es deseable o no.
public sealed record CasoAtipico(
    string Descripcion,          // ej. "ZScoreReversion: 0 operaciones en 9/9 corridas (2 periodos)"
    IReadOnlyList<string> CarpetasOrigen);
```

**Por qué `DistribucionMetrica` no acepta agrupar por más de una dimensión a la vez**: agrupar por
2+ dimensiones simultáneas (ej. gestor × timeframe × dataset en una sola tabla) empieza a producir
una superficie que invita a leer "la mejor celda" como si fuera una recomendación — mantener una
dimensión por instancia obliga a que cualquier cruce de dimensiones sea una composición explícita de
varias llamadas, cada una con su propio contexto declarado, no una tabla única que sugiera
comparación implícita.

**Por qué `EstadisticaDescriptiva` no incluye desviación estándar ni percentiles**: no porque estén
prohibidos en principio, sino porque D-123 no los pidió y no hay necesidad demostrada — mínimo
razonable que puede ampliarse si una fase futura lo requiere (D-030: no se agrega superficie sin
justificación).

---

## 4. Análisis permitidos — mapeo directo a D-123

| Permitido por D-123 | Estructura | Método |
|---|---|---|
| Cobertura | `CoberturaAnalizada` | `AnalisisDescriptivo.CalcularCobertura(IReadOnlyList<FilaCorpus>)` |
| Distribución de métricas | `DistribucionMetrica` | `AnalisisDescriptivo.CalcularDistribucion(IReadOnlyList<FilaCorpus>, nombreMetrica, agrupadoPor)` |
| Comparación temporal descriptiva | `ComparacionPeriodos` | `AnalisisDescriptivo.CompararPeriodos(IReadOnlyList<FilaCorpus>, nombreMetrica, gestor)` |
| Identificación de casos atípicos | `CasoAtipico` | `AnalisisDescriptivo.DetectarCasosAtipicos(IReadOnlyList<FilaCorpus>)` — reglas fijas, no calibradas (§5) |

**`DetectarCasosAtipicos` — reglas explícitas, no un umbral inventado**: detecta exactamente 2
condiciones ya nombradas en auditorías previas, ninguna nueva:
- **Sin actividad**: `PnLTotal == 0 && CashFinal == CapitalInicial` en todas las corridas `Success`
  de una combinación estrategia/dataset (mismo patrón ya documentado para `ZScoreReversion` en
  `AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V2.md`/`AUDITORIA_DIVERSIDAD_TEMPORAL_CASO5C_V1.md`).
- **Degeneración de drawdown**: `DrawdownMaximoPct >= 0.99m` (99%) en alguna fila — mismo umbral
  fáctico ya usado como descripción (no como criterio de exclusión) en las auditorías V1/V2/
  diversidad temporal.

Ambas reglas son **detección de hechos ya documentados en prosa por auditorías previas**, trasladados
a código para que la salida los liste automáticamente — no son umbrales nuevos calibrados sobre el
corpus (D-030 no aplica aquí porque no se está fijando ningún punto de corte de "bueno/malo", solo
automatizando una observación textual ya hecha 2 veces manualmente).

---

## 5. Salvaguardas — cómo el código impide ranking/selección/recomendación

No son comentarios de intención — son restricciones estructurales verificables por prueba (mismo
principio que P3 de `campana_corpus` verificó ausencia de selección por resultado vía análisis
textual del código fuente):

1. **Ningún tipo de salida tiene un campo que identifique "el mejor" de nada**: ni
   `CoberturaAnalizada`, ni `DistribucionMetrica`, ni `ComparacionPeriodos`, ni `ResumenCorpus`
   exponen un solo gestor/estrategia/timeframe destacado — toda estructura es un diccionario o lista
   que preserva todas las entradas por igual, sin campo `Mejor`/`Recomendado`/`Top`.
2. **Ningún método ordena por valor de métrica antes de devolver el resultado** — los
   `IReadOnlyDictionary`/`IReadOnlyList` se construyen en el orden de aparición de los datos de
   entrada (mismo principio D-112 ya aplicado a `ComparadorGestores.Filas`), nunca ordenados por
   `DrawdownMaximoPct`, `ProfitFactor`, etc.
3. **`ResumenCorpus.Limitaciones` es obligatorio y no vacío** — construido a partir de una plantilla
   fija (ver contenido mínimo abajo), nunca opcional ni generado dinámicamente a partir de qué tan
   "buenos" se ven los resultados.
4. **Ningún método combina 2+ métricas en un solo número** — `EstadisticaDescriptiva` reporta cada
   métrica solicitada por separado; no existe ningún cálculo tipo "índice compuesto" o "score".
5. **Ningún método filtra corridas por valor de resultado** — `DetectarCasosAtipicos` (§4) reporta
   qué existe, no descarta ni prioriza combinaciones en función de si el resultado es favorable.

**Contenido mínimo de `ResumenCorpus.Limitaciones`** (texto fijo, con los números de la ejecución
interpolados, no redactado libremente cada vez):
```
Corpus descriptivo: {TotalFilas} filas sobre {cantidad de NombreDataset distintos} periodo(s)
temporal(es), instrumento unico (BTCUSDT). Esta salida describe unicamente lo que el corpus
persistido contiene — no constituye recomendacion, ranking, ni evaluacion de que gestor/estrategia
es preferible (D-118/D-119/D-120). Ningun patron aqui descrito se extiende a instrumentos no
representados en el corpus.
```

---

## 6. Pruebas

Mismo patrón P-series ya usado en `TestsComparadorGestores.cs`/`TestsCampanaCorpus.cs` — casos
ejecutables, no un framework nuevo.

- **P1 — `LectorCorpus` no pierde ni inventa filas**: sobre un directorio de prueba con N carpetas
  fixture (JSON + MD construidos a mano, valores conocidos), `Leer` devuelve exactamente
  N × (gestores por comparación) filas, con valores idénticos a los fixtures.
- **P2 — Parseo de métricas coincide con el formato congelado**: fixture con
  `COMPARACION_GESTORES_V1.md` generado por `RenderizadorComparacionGestores.Generar` real (no a
  mano) sobre un `ResultadoComparativoGestores` de prueba — confirma que `LectorCorpus` interpreta
  exactamente lo que Capa 1 ya escribe, sin desincronización de formato.
- **P3 — Métricas ausentes se leen como `null`, no como error ni como 0**: fixture con una corrida
  no exitosa (bloque `"Métricas: (no disponibles...)"`) — las 6 métricas de esa fila son `null`.
- **P4 — Cobertura no omite ninguna carpeta válida ni oculta las inválidas**: fixture con 1 carpeta
  sin `IDENTIDAD_COMPARACION.json` — aparece en `CarpetasIgnoradas`, no interrumpe el análisis de
  las demás.
- **P4b — Evidencia incompleta cuenta como evidencia, no como ausencia**: fixture con una
  comparación en `Estado: Incomplete` (métricas `null`, JSON estructural válido) — la fila aparece
  en `TotalFilas`/`ComparacionesPor*` de `CoberturaAnalizada` igual que cualquier fila `Success`, y
  queda excluida únicamente de los cálculos aritméticos (`CalcularDistribucion`/`CompararPeriodos`)
  por no tener valor numérico, nunca invisibilizada de la cobertura.
- **P5 — Ausencia estructural de ranking en los tipos de salida** (reflexión, mismo principio que
  Caso 5B/P5 "ausencia estructural de ranking en los tipos de resultado"): ningún tipo en
  `analisis_corpus/` tiene una propiedad cuyo nombre contenga "Mejor"/"Ganador"/"Ranking"/"Score"/
  "Recomend" (case-insensitive) — verificado sobre los tipos públicos vía `System.Reflection`.
- **P6 — Ausencia estructural de ordenamiento por valor**: sobre un fixture con métricas conocidas
  en orden deliberadamente "invertido" respecto al orden de inserción, `CalcularDistribucion`/
  `CompararPeriodos` devuelven las claves en el mismo orden en que aparecieron los datos de entrada,
  no en orden de valor.
- **P7 — `DetectarCasosAtipicos` reproduce los 2 hallazgos ya documentados**: sobre el corpus real
  persistido (`caso5/resultados/`), detecta que `ZScoreReversion` aparece como caso "sin actividad"
  en ambos datasets, y que existe al menos 1 caso de `DrawdownMaximoPct >= 0.99` en timeframes
  cortos — confirma que el análisis automatizado coincide con lo que las auditorías manuales ya
  encontraron, no con un resultado distinto.
- **P8 — `ResumenCorpus.Limitaciones` nunca vacío ni ausente**: para cualquier corpus de entrada
  (incluido un corpus vacío — 0 carpetas), el campo `Limitaciones` contiene la plantilla fija con
  los números correctos interpolados.
- **P9 — Ninguna llamada a componentes de ejecución**: análisis textual/reflexión sobre
  `analisis_corpus/*.cs` confirmando ausencia de referencias a `ComparadorGestores.Comparar`,
  `EjecutorProtocolo.Ejecutar`, o cualquier `CrearEstrategia`/`IStrategy` — mismo principio que P3 de
  `campana_corpus` (verificación textual, no solo por diseño).

---

## 7. Qué no cambia

- `ComparadorGestores.cs`, `PersistidorComparaciones.cs`, `RenderizadorComparacionGestores`: sin
  modificación — Capa 2 los consume solo como formato de lectura (parseo de lo que ya escriben),
  nunca los invoca ni los referencia en tiempo de ejecución.
- `campana_corpus/`: sin modificación — Capa 2 no genera evidencia nueva, no ejecuta campañas.
- `caso5/resultados/`: sin escritura desde este componente — `analisis_corpus/` es exclusivamente de
  lectura; cualquier salida (`ResumenCorpus`) se presenta en memoria o como reporte separado (fuera
  de alcance de esta especificación decidir el formato de presentación final — puede resolverse como
  parte de la implementación si D-123 no lo restringe, dado que no es una decisión sobre contenido
  analítico sino sobre presentación).

---

## Fuera de alcance de este documento

No se implementa ningún componente de recomendación, ranking, ni selección (D-118 permanece
excluida). No se fija ningún umbral de "suficiencia de evidencia" (D-119 sigue en estado de
principio). No se decide todavía el formato final de presentación de `ResumenCorpus` (consola,
archivo, u otro) más allá de que debe declarar `Limitaciones` siempre. No se ejecuta ningún análisis
real sobre el corpus en este documento — solo se especifica cómo se calcularía.

---

## Próximo documento

Implementación de `analisis_corpus/` según esta especificación, seguida de su ejecución real sobre
el corpus de 49 comparaciones y un documento de resultado (probablemente
`RESULTADO_ANALISIS_CORPUS_CASO5C_CAPA2_V1.md`, formato a definir cuando exista contenido real que
describir).
