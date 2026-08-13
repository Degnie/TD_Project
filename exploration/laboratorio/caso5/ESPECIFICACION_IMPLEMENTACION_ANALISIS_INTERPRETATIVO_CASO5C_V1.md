# Especificación de Implementación — Análisis Interpretativo Limitado (Caso 5C)

Estado: **especificación previa a implementación**. Traduce `DECISIONES_EVOLUCION_POST_CAPA2_V1.md`
(D-124) a diseño de código concreto. **Ningún código se modifica en este documento.** No implementa
ninguna forma de recomendación, ranking, selección, ni regla operativa — D-118/D-119/D-120
permanecen en estado de principio, sin activación aquí.

---

## 1. Relación con Capa 2 — extensión, no reimplementación

**Ejecutable separado, no una extensión de `analisis_corpus/`** — mismo criterio ya aplicado entre
`campana_corpus/` y `analisis_corpus/`: una responsabilidad nueva (interpretación limitada) merece
su propio punto de entrada, aunque **reutilice directamente** los tipos de lectura ya construidos.

**Ubicación**: `exploration/laboratorio/caso5/analisis_interpretativo/`
- `AnalisisInterpretativo.csproj` — sin `OutputType=Exe` propio de ejecución de backtests; mismo
  patrón minimalista que `AnalisisCorpus.csproj` (sin `<ProjectReference>` a `src/`), más
  `<Compile Include>` de los 3 archivos de `analisis_corpus/` que sí reutiliza (ver abajo).
- `ProgramAnalisisInterpretativo.cs` — punto de entrada top-level statements.
- `DetectorRelaciones.cs` — componente nuevo (§3).
- `TestsAnalisisInterpretativo.cs` — pruebas P1-P8 (§6).

**Reutilización explícita, sin duplicar**: `FilaCorpus`, `LectorCorpus`, y las estructuras de Capa 2
(`CoberturaAnalizada`, `EstadisticaDescriptiva`) se **referencian directamente** vía
`<Compile Include="..\analisis_corpus\FilaCorpus.cs">`-equivalente (en la práctica, `LectorCorpus.cs`
contiene el record `FilaCorpus` en el mismo archivo — se linkea el archivo completo) — nunca se
copia su lógica. La capa interpretativa **no vuelve a leer el manifiesto por su cuenta con lógica
propia**: recibe la lista de `FilaCorpus` ya construida por `LectorCorpus.Leer`, igual que
`AnalisisDescriptivo` la recibe.

**Fuente de datos — idéntica a Capa 2, sin ejecución nueva**: exactamente el mismo principio que
`ESPECIFICACION_IMPLEMENTACION_CASO5C_CAPA2_V1.md` §2 ya estableció y que D-124 reafirma
explícitamente — `DetectorRelaciones` nunca llama a `ComparadorGestores.Comparar`,
`EjecutorProtocolo.Ejecutar`, ni ningún componente que ejecute un backtest. Consume únicamente lo
que `LectorCorpus.Leer(dirResultados, rutaManifiesto)` ya devuelve.

---

## 2. Qué agrega esta capa que Capa 2 no tenía

Capa 2 (`AnalisisDescriptivo`) se limitó deliberadamente a **una sola dimensión de agrupación por
instancia** (`DistribucionMetrica.AgrupadoPor` acepta un solo valor: Gestor, Timeframe, Dataset, o
Estrategia — nunca una combinación). Esa restricción fue intencional
(`ESPECIFICACION_IMPLEMENTACION_CASO5C_CAPA2_V1.md` §3: "evita crear una tabla que invite a leer
'la mejor celda'").

La capa interpretativa **sí permite cruces de 2+ dimensiones**, pero con una salvaguarda estructural
que reemplaza la que Capa 2 lograba con una sola dimensión: **todo cruce se presenta como el
conjunto completo de combinaciones observadas, nunca como una combinación aislada o destacada**. La
diferencia no es "menos restricción" — es la misma restricción (no destacar una celda) aplicada de
forma distinta, porque ahora el espacio de combinaciones es más grande.

---

## 3. `DetectorRelaciones` — las 4 capacidades autorizadas por D-124

```csharp
namespace TD_Project.Caso5.AnalisisInterpretativo;

// Cruce de 2+ dimensiones — SIEMPRE el conjunto completo de combinaciones observadas en el corpus,
// nunca una combinacion aislada. "Dimension" = Estrategia | Timeframe | Gestor | Dataset (mismo
// vocabulario que DistribucionMetrica.AgrupadoPor de Capa 2, ahora combinable).
public sealed record CombinacionObservada(
    IReadOnlyDictionary<string, string> Dimensiones,  // ej. {"Estrategia":"Tres Mosqueteros","Timeframe":"15m"}
    EstadisticaDescriptiva? Estadistica,               // null si ninguna fila de esta combinacion tiene metrica
    int CantidadFilas,
    IReadOnlyList<string> CarpetasOrigen);

public sealed record RelacionObservada(
    string NombreMetrica,
    IReadOnlyList<string> DimensionesCruzadas,         // ej. ["Estrategia","Timeframe"]
    IReadOnlyList<CombinacionObservada> Combinaciones); // TODAS las combinaciones presentes, sin ordenar por valor

// Agrupacion de comparaciones segun presencia/ausencia de un patron YA NOMBRADO (nunca un patron
// nuevo inferido aqui) — extension directa de AnalisisDescriptivo.DetectarCasosAtipicos (Capa 2),
// ahora exponiendo ambos lados (donde aparece Y donde no aparece), no solo donde aparece.
public sealed record AgrupacionPorPatron(
    string NombrePatron,               // ej. "DrawdownMaximoPct>=99%", "SinActividad" — mismo texto que Capa 2 ya usa
    IReadOnlyList<string> DondeAparece,        // descripciones factuales, formato "Estrategia/Timeframe/Dataset/Gestor"
    IReadOnlyList<string> DondeNoAparece);     // idem, complemento dentro del mismo universo de filas evaluadas

// Condiciones bajo las que aparece una metrica en un rango dado — responde "en que condiciones
// aparece esta evidencia", no "que condicion es mejor".
public sealed record CondicionesDeAparicion(
    string NombreMetrica,
    string CondicionValor,             // ej. "DrawdownMaximoPct >= 0.99"
    IReadOnlyList<CombinacionObservada> Combinaciones);

// Comparacion de consistencia de un patron entre 2+ datasets — SOLO presencia/ausencia del mismo
// conjunto de condiciones, nunca una palabra evaluativa (robusto/confiable/garantizado).
public sealed record ConsistenciaEntrePeriodos(
    string NombrePatron,
    IReadOnlyDictionary<string, IReadOnlyList<string>> CondicionesPorDataset); // combinaciones donde aparece, por dataset

public static class DetectorRelaciones
{
    // D-124 "detectar relaciones observadas": cruce de 2+ dimensiones sobre una metrica.
    public static RelacionObservada CruzarDimensiones(
        IReadOnlyList<FilaCorpus> filas, string nombreMetrica, IReadOnlyList<string> dimensiones);

    // D-124 "agrupar comportamientos": patron ya nombrado en Capa 2 (mismo texto), expone ambos lados.
    public static AgrupacionPorPatron AgruparPorPatron(
        IReadOnlyList<FilaCorpus> filas, string nombrePatron, Func<FilaCorpus, bool> predicadoPatron);

    // D-124 "describir condiciones donde aparece cierta evidencia".
    public static CondicionesDeAparicion DescribirCondicionesDeAparicion(
        IReadOnlyList<FilaCorpus> filas, string nombreMetrica, Func<decimal, bool> condicion, string condicionTexto);

    // D-124 "comparar estabilidad de patrones observados" — factual, sin calificativo.
    public static ConsistenciaEntrePeriodos CompararConsistencia(
        IReadOnlyList<FilaCorpus> filas, string nombrePatron, Func<FilaCorpus, bool> predicadoPatron);
}
```

**Por qué `CombinacionObservada.Dimensiones` es un diccionario de texto, no un tipo fuertemente
tipado por dimensión**: permite que `CruzarDimensiones` acepte cualquier subconjunto de las 4
dimensiones sin necesitar un tipo nuevo por cada combinación posible (2, 3, o 4 dimensiones a la
vez) — mismo principio de generalidad ya usado en `DistribucionMetrica.AgrupadoPor` (string, no
enum), extendido a múltiples claves.

**Por qué `AgruparPorPatron`/`CompararConsistencia` reciben el patrón como predicado, no lo
calculan internamente**: el patrón debe venir **ya nombrado y ya definido en prosa** por una
auditoría o resultado previo (D-124: "agrupar observaciones existentes, no crear reglas") — el
componente de detección no decide qué cuenta como patrón, solo aplica una definición ya fijada
externamente (ej. `DrawdownMaximoPct >= 0.99m`, mismo umbral fáctico que `AnalisisDescriptivo.
DetectarCasosAtipicos` ya usa en Capa 2, referenciado aquí, no reinventado).

---

## 4. Salvaguardas — cómo el código impide ranking/selección/reglas operativas

Extendiendo las salvaguardas ya verificadas en Capa 2 (`ESPECIFICACION_IMPLEMENTACION_CASO5C_CAPA2_
V1.md` §5), con 2 salvaguardas nuevas específicas al riesgo que esta capa introduce (cruces
multi-dimensionales, lenguaje prescriptivo):

1. **Ningún tipo de salida tiene un campo que identifique "la mejor combinación"**: ni
   `RelacionObservada`, ni `AgrupacionPorPatron`, ni `CondicionesDeAparicion`, ni
   `ConsistenciaEntrePeriodos` exponen un campo `Mejor`/`Recomendada`/`Optima` — toda estructura es
   una lista/diccionario que preserva todas las combinaciones observadas por igual.
2. **`CruzarDimensiones` nunca devuelve una sola combinación** — su tipo de retorno
   (`RelacionObservada.Combinaciones`) es siempre una lista; no existe una sobrecarga ni un modo que
   devuelva "la combinación con mejor/peor valor de la métrica".
3. **Ningún método ordena combinaciones por valor de métrica** — mismo principio ya verificado en
   Capa 2 (P6): el orden de las listas/diccionarios de salida es el orden de aparición de las filas
   de entrada, nunca un `.OrderBy`/`.OrderByDescending` sobre un valor numérico.
4. **Prohibición léxica de lenguaje prescriptivo** (salvaguarda nueva, específica de esta capa):
   ningún string literal generado por `DetectorRelaciones` puede contener las palabras "debe",
   "debería", "recomendado", "óptimo", "mejor", "usar", "elegir", "preferible" — verificado por
   prueba sobre el conjunto de plantillas de texto usadas internamente (no sobre el corpus, que es
   dato, no código).
5. **`AgruparPorPatron`/`CompararConsistencia` no aceptan un patrón sin nombre**: el parámetro
   `nombrePatron`/`NombrePatron` es obligatorio y no vacío — refuerza que todo patrón detectado
   tiene una etiqueta factual explícita, nunca una inferencia anónima.
6. **Ningún método combina 2+ métricas en un solo número** — mismo principio ya heredado de Capa 2,
   sin excepción en esta capa.
7. **Toda salida declara su propio alcance** (extensión del principio `ResumenCorpus.Limitaciones`
   de Capa 2, ahora obligatorio aquí también) — cada uno de los 4 tipos de salida (§3) debe
   acompañarse, al presentarse (consola o documento), de una nota de limitaciones que incluya
   explícitamente: instrumento único (BTCUSDT), y que la observación es histórica, no proyección.

---

## 5. Separación entre detección de relación e interpretación humana

Punto señalado explícitamente por el auditor en la revisión de D-124: el sistema calcula
coincidencias/distribuciones condicionadas/presencia de patrones — **la interpretación de qué
significan esas coincidencias sigue siendo responsabilidad humana**, ejercida en el documento de
resultado (equivalente a `RESULTADO_ANALISIS_CORPUS_CASO5C_CAPA2_V1.md`, pero para esta capa), no en
el código.

Esto se traduce en una regla de diseño concreta: **`DetectorRelaciones` nunca genera prosa
interpretativa** — solo estructuras de datos (§3) y, cuando mucho, descripciones factuales
templadas (ej. `"{Estrategia}/{Timeframe}/{Dataset}: {Metrica}={Valor}"`, mismo estilo ya usado en
`CasoAtipico.Descripcion` de Capa 2). Cualquier oración que sintetice "qué significa" una relación
(ej. "esto sugiere que...", "esto indica...") se escribe, si acaso, en el documento de resultado
posterior — nunca en una plantilla de código que se ejecute automáticamente sobre el corpus.

---

## 6. Pruebas

Mismo patrón P-series ya usado en `analisis_corpus/TestsAnalisisCorpus.cs`.

- **P1 — `CruzarDimensiones` no pierde ni inventa combinaciones**: fixture con N filas conocidas
  sobre 2 dimensiones — el resultado contiene exactamente las combinaciones presentes en el
  fixture, ninguna adicional, ninguna faltante.
- **P2 — `CruzarDimensiones` nunca devuelve una sola combinación destacada**: para cualquier fixture
  con 2+ combinaciones distintas, `RelacionObservada.Combinaciones.Count` coincide con el número de
  combinaciones únicas presentes — nunca 1 salvo que el fixture solo tenga 1.
- **P3 — Orden de combinaciones es orden de aparición, no de valor** (mismo principio que P6 de
  Capa 2): fixture con métricas en orden deliberadamente "invertido" respecto al orden de
  inserción — el orden de salida coincide con el de entrada.
- **P4 — `AgruparPorPatron` expone ambos lados** (dónde aparece y dónde no) sobre un fixture con
  patrón conocido en un subconjunto de filas — `DondeAparece`/`DondeNoAparece` particionan
  exactamente el universo de filas evaluadas, sin solapamiento ni omisión.
- **P5 — Ausencia estructural de ranking/selección en los tipos de salida** (reflexión, mismo
  principio que Capa 2/P5): ningún tipo público en `analisis_interpretativo/` tiene una propiedad
  cuyo nombre contenga "Mejor"/"Ganador"/"Ranking"/"Score"/"Recomend"/"Optim" (case-insensitive).
- **P6 — Ausencia léxica de lenguaje prescriptivo**: análisis textual sobre las plantillas de string
  usadas en `DetectorRelaciones.cs` — ninguna contiene "debe", "debería", "recomendado", "óptimo",
  "mejor", "usar", "elegir", "preferible" (case-insensitive, sobre el código fuente, no sobre datos
  del corpus).
- **P7 — Trazabilidad completa a evidencia origen**: para cualquier `CombinacionObservada`
  producida sobre el corpus real, cada `CarpetasOrigen` no está vacío y cada carpeta referenciada
  existe físicamente y aparece en `MANIFIESTO_CORPUS_CASO5C_V1.json`.
- **P8 — Ausencia estructural de llamadas a componentes de ejecución** (mismo principio que P9 de
  Capa 2): análisis textual confirmando que `analisis_interpretativo/*.cs` no referencia
  `ComparadorGestores.Comparar`, `EjecutorProtocolo.Ejecutar`, `CrearEstrategia`, ni `IStrategy`.

---

## 7. Qué no cambia

- `analisis_corpus/LectorCorpus.cs`, `AnalisisDescriptivo.cs`: sin modificación — se reutilizan por
  referencia de compilación (`<Compile Include>`), nunca se copian ni se alteran.
- `ComparadorGestores.cs`, `PersistidorComparaciones.cs`, `EjecutorProtocolo.cs`: sin modificación,
  ni siquiera referenciados en tiempo de ejecución (mismo principio ya aplicado en Capa 2).
- `MANIFIESTO_CORPUS_CASO5C_V1.json`: sin modificación — se consume tal cual, vía `LectorCorpus`.
- `campana_corpus/`: sin modificación — esta capa no genera evidencia nueva, no ejecuta campañas.

---

## Fuera de alcance de este documento

No se implementa ningún componente de recomendación, ranking, ni selección (D-118 permanece
excluida). No se fija ningún umbral de "suficiencia de evidencia" (D-119 sigue en estado de
principio). No se decide todavía el formato final de presentación (consola, archivo, u otro). No se
ejecuta ningún análisis real sobre el corpus en este documento — solo se especifica cómo se
calcularía. No se define ningún patrón nuevo — los patrones que `AgruparPorPatron`/
`CompararConsistencia` puedan usar en la implementación deben ser los ya nombrados en auditorías
previas (drawdown extremo, ausencia de actividad), no patrones inventados en esta fase.

---

## Próximo documento

Implementación de `analisis_interpretativo/` según esta especificación, seguida de su ejecución real
sobre el corpus de 49 comparaciones y un documento de resultado (probablemente
`RESULTADO_ANALISIS_INTERPRETATIVO_CASO5C_V1.md`, formato a definir cuando exista contenido real que
describir — mismo patrón ya seguido para Capa 2).
