# Especificación de Implementación — Recomendador Basado en Evidencia (Caso 6, D-128)

Estado: **especificación previa a implementación**. Traduce D-128
(`DECISIONES_CASO6_RECOMENDADOR_V1.md`) a diseño de código concreto: qué lee, qué calcula, qué
produce exactamente, cómo maneja múltiples configuraciones compatibles, y qué pruebas verifican
cada salvaguarda. **Ningún código se escribe en este documento.**

---

## 1. Qué devuelve exactamente el recomendador

**Nunca** `"Configuración recomendada: X"` — esa frase empuja hacia selección automática, tal como
señaló el auditor. La salida es siempre un **conjunto** de configuraciones compatibles con el
criterio declarado, presentadas sin orden de preferencia implícito, cada una con su propia
evidencia:

```csharp
public sealed record ConfiguracionCandidata(
    string Estrategia,
    string Timeframe,
    string IdentidadGestor,
    string NombreDataset,
    decimal ValorMetrica,           // el valor observado de la unica metrica del criterio
    int CantidadFilas,              // cuantas filas del corpus respaldan este valor (asimetria, D-128 punto 5)
    IReadOnlyList<string> CarpetasOrigen);

public sealed record RecomendacionExperimental(
    string Perfil,                  // "Crecimiento" | "PreservacionCapital" | "Personalizado"
    string CriterioUsado,           // ej. "DrawdownMaximoPct ascendente, sin combinar con otras metricas"
    int ConfiguracionesAnalizadas,  // total de combinaciones distintas en el corpus, con o sin evidencia
    int ConfiguracionesConEvidencia,// subconjunto con >=1 fila Estado=Success y metrica disponible (D-128 punto 5)
    IReadOnlyList<ConfiguracionCandidata> Candidatas, // TODAS las que cumplen el criterio, sin truncar ni ordenar por valor
    string Limitaciones);
```

Esta estructura es una extensión de `RecomendacionExperimental` ya fijada por D-120
(`Contenido`→se reparte en `CriterioUsado`+`Candidatas` para no colapsar el conjunto en una sola
frase; `EvidenciaUsada`→`ConfiguracionesAnalizadas`/`ConfiguracionesConEvidencia`/`CarpetasOrigen`
por candidata; `Limitaciones` se mantiene igual) — no una alternativa. `Contenido` como campo de
texto libre queda representado aquí por la combinación de `CriterioUsado` + el tamaño de
`Candidatas`, evitando que una frase generada dinámicamente resuma el conjunto (mismo motivo por el
que Capa 2/interpretativo nunca generan prosa dinámica, D-124 §5).

**Ejemplo de salida** (perfil Preservación de capital, criterio `DrawdownMaximoPct` ascendente):

```
Perfil: PreservacionCapital
CriterioUsado: "DrawdownMaximoPct ascendente, sin combinar con otras metricas"
ConfiguracionesAnalizadas: 54   (18 combinaciones x 3 datasets con matriz completa)
ConfiguracionesConEvidencia: 54
Candidatas (todas las que tienen DrawdownMaximoPct minimo dentro de su propio grupo de comparacion... )
  [ver §3 para el criterio exacto de "compatible"]
Limitaciones: "Observacion historica sobre backtest, no proyeccion de comportamiento futuro.
               Corpus limitado a BTCUSDT/ETHUSDT, 2024-2025 y 2022-2023. Algunas configuraciones
               tienen mas de 1 fila por repeticion de reproducibilidad (ver CantidadFilas)."
```

---

## 2. Cómo maneja múltiples configuraciones compatibles (sin ranking oculto)

**Regla fijada aquí, resolviendo la pregunta abierta por el auditor**: el recomendador nunca
selecciona un subconjunto "top-N" — eso sería un ranking disfrazado de límite técnico. En su lugar:

- **Umbral de compatibilidad, no ranking**: para el perfil elegido, se define un **umbral
  explícito** sobre la métrica del criterio (ej. "percentil 25 inferior" para Preservación de
  capital, o "por encima de la mediana" para Crecimiento) — **todas** las configuraciones que
  caen dentro del umbral se devuelven, sin ordenar entre sí por valor. El umbral es una regla fija,
  documentada, no calibrada observando el resultado de una ejecución particular (D-030).
- **Orden de presentación = orden de aparición en el corpus** (mismo criterio ya usado en
  `AnalisisDescriptivo`/`DetectorRelaciones`: nunca ordenar por valor de métrica, solo por orden de
  inserción) — evita que la posición en la lista comunique implícitamente una preferencia.
- **Si el umbral no produce ninguna candidata** (corpus insuficiente para ese perfil/criterio):
  `Candidatas` vacío, y `Limitaciones` lo declara explícitamente — no se relaja el umbral ni se
  devuelve "la menos mala" (D-119: sin evidencia suficiente, no recomendar).
- **Si el umbral produce todas las configuraciones** (ningún filtro efectivo): también es una
  salida válida — el recomendador no fuerza un subconjunto artificialmente pequeño.

**Definición exacta del umbral por perfil** (a fijar aquí, no calibrado sobre datos):
- **Crecimiento**: métrica `PnLTotal`, umbral = valor ≥ mediana del grupo de comparación (definido
  en §3). Configuraciones con `PnLTotal` en o por encima de la mediana observada.
- **Preservación de capital**: métrica `DrawdownMaximoPct`, umbral = valor ≤ mediana del grupo de
  comparación.
- **Personalizado**: el usuario declara la métrica (una de las 6) y la dirección (`Ascendente`/
  `Descendente`); mismo umbral de mediana aplicado sobre esa métrica.

**Por qué mediana y no un porcentaje arbitrario**: la mediana es una propiedad ya calculada por
`EstadisticaDescriptiva` (Capa 2, sin cálculo nuevo), divide el grupo en dos mitades por
construcción (nunca 0 ni 100% de las candidatas salvo empate), y no requiere ningún parámetro
adicional que deba justificarse o calibrarse — mismo principio de "usar lo que ya existe" aplicado
en todo el proyecto.

---

## 3. Qué significa "grupo de comparación" — distinto de la unidad de recomendación

**Precisión metodológica fijada aquí, tras revisión explícita del auditor**: el "grupo de
comparación" (contra qué conjunto se calcula el umbral) y la "unidad de recomendación" (qué
aparece como candidata en el resultado, §1) son 2 conceptos distintos y **deben permanecer
separados** — no se puede usar la misma clave para ambos, porque eso anularía el mecanismo del
umbral (ver nota al final de esta sección).

```
Corpus (FilaCorpus, 1 fila = 1 estrategia + 1 timeframe + 1 dataset + 1 gestor)
 |
 +--> GrupoComparacion = (Estrategia, Timeframe, Dataset)      <- SIN gestor
 |        |
 |        +-- reune las filas de los distintos gestores dentro de esa combinacion
 |        +-- calcula la mediana de la metrica sobre esas filas (2 o 3 valores, uno por gestor)
 |        +-- el umbral se evalua contra esa mediana
 |
 +--> ConfiguracionCandidata = (Estrategia, Timeframe, Dataset, Gestor)   <- CON gestor (§1)
          |
          +-- cada fila atomica que cruzo el umbral de su GrupoComparacion
          +-- se reporta con su gestor especifico, nunca agregado ni promediado
```

**Grupo de comparación = `(Estrategia, Timeframe, Dataset)`, sin gestor**. Para "Ema Cross / 1h /
`BTCUSDT_2024-2025`", el recomendador reúne las filas de los 3 gestores dentro de esa combinación
exacta — la mediana se calcula sobre esos 3 valores, y el umbral determina cuáles de los 3
(reportados individualmente, con su gestor) son candidatas. Esto responde a la pregunta natural del
usuario ("dado que uso esta estrategia en este timeframe con este dataset, ¿qué gestor(es)
mostraron menor drawdown respecto a los demás?"), sin mezclar instrumentos ni periodos en el mismo
cálculo de mediana.

**El dataset (instrumento/período) se mantiene como parte de la clave del grupo, nunca colapsado**:
dos filas de `Ema Cross/1h` sobre `BTCUSDT_2024-2025` y sobre `ETHUSDT_2024-2025` pertenecen a
grupos de comparación **distintos** — cada uno con su propia mediana y sus propias candidatas. El
recomendador nunca combina evidencia de 2 datasets en un solo cálculo de mediana (mismo principio
ya aplicado en Capa 2 `ComparacionPeriodos`: presencia/ausencia factual por dataset, nunca
fusionado).

**Por qué el grupo de comparación NO puede incluir el gestor en su clave**: si
`GrupoComparacion` incluyera `Gestor`, cada grupo tendría exactamente 1 fila (el gestor ya
identifica una fila única dentro de estrategia+timeframe+dataset) — la mediana de 1 valor es ese
mismo valor, y el umbral "incluir si ≤ mediana" se cumpliría siempre. El filtro dejaría de
descartar cualquier fila: el recomendador devolvería literalmente todo el corpus con evidencia, sin
ningún criterio de compatibilidad real aplicado. La separación de claves (grupo sin gestor,
candidata con gestor) es la única forma en que el umbral compara algo — comparar los gestores
disponibles entre sí dentro de la misma configuración experimental, que es su propósito declarado
en D-128.

**Nunca se agrega ni promedia entre gestores**: `ConfiguracionCandidata` reporta el valor exacto de
la fila atómica que cruzó el umbral — ninguna candidata combina los 3 gestores en un solo número
(eso introduciría una métrica derivada nueva, no autorizada, y ocultaría qué gestor específico
produjo el valor — riesgo identificado explícitamente por el auditor).

---

## 4. Arquitectura (reutilización por referencia, mismo patrón ya usado 2 veces)

```
caso6/recomendador/
 ├── Recomendador.csproj      <- <Compile Include> de LectorCorpus.cs/AnalisisDescriptivo.cs
 │                                (analisis_corpus/), mismo patron que AnalisisInterpretativo.csproj
 ├── MotorRecomendacion.cs    <- CalcularCandidatas, GenerarRecomendacion
 ├── ProgramRecomendador.cs   <- punto de entrada, ejecuta pruebas P1-Pn, luego corre sobre corpus real
 └── TestsRecomendador.cs     <- P1-P8 (ver §6)
```

**`MotorRecomendacion.cs`** — únicas 2 funciones públicas:

```csharp
public static class MotorRecomendacion
{
    public static RecomendacionExperimental Recomendar(
        IReadOnlyList<FilaCorpus> filas, string perfil, string? metricaPersonalizada = null);

    // Clave del GRUPO DE COMPARACION (§3) = (Estrategia, Timeframe, NombreDataset) — SIN gestor.
    // Para cada grupo: reune las filas de los distintos gestores presentes, calcula la mediana
    // de la metrica sobre esos valores (reutiliza el mismo calculo de AnalisisDescriptivo.
    // CalcularEstadistica), y evalua el umbral fila por fila.
    // Clave de cada CONFIGURACIONCANDIDATA devuelta (§1) = (Estrategia, Timeframe, NombreDataset,
    // Gestor) — CON gestor: solo las filas atomicas que cruzaron el umbral de su propio grupo,
    // reportadas individualmente, nunca agregadas ni promediadas entre si.
    // Orden de salida = orden de aparicion en `filas` de entrada, nunca por ValorMetrica (§2).
    private static IReadOnlyList<ConfiguracionCandidata> CalcularCandidatas(
        IReadOnlyList<FilaCorpus> filas, string metrica, string direccion);
}
```

**No se crea ningún tipo nuevo de lectura de corpus** — `MotorRecomendacion.Recomendar` recibe
`IReadOnlyList<FilaCorpus>` ya leído por `LectorCorpus.Leer` (mismo mecanismo de Capa 2/
interpretativo), nunca abre archivos ni conoce la ruta del manifiesto directamente.

**Filtro de evidencia mínima** (D-128 punto 5): antes de calcular cualquier grupo de comparación,
se descartan filas con `Estado != "Success"` o `PnLTotal is null` — mismo filtro exacto ya usado en
`AnalisisDescriptivo.Resumir` para distinguir evidencia real de evidencia parcial deliberada.

---

## 5. Qué no debe incluir esta implementación

- Ningún método que reciba un conjunto de candidatas y devuelva 1 sola (eso sería el selector
  automático explícitamente excluido, D-118).
- Ningún parámetro de peso o ponderación entre métricas — el perfil "Personalizado" acepta 1 sola
  métrica, nunca una lista con pesos.
- Ninguna llamada a `EjecutorProtocolo`, `ComparadorGestores`, ni ningún componente de ejecución —
  el recomendador es puramente de lectura sobre `FilaCorpus` ya materializado.
- Ningún umbral calibrado observando el resultado de esta implementación sobre el corpus actual —
  la mediana (§2) es la única regla, fija antes de ver el resultado.
- Ninguna estrategia fuera de las 6 ya presentes en el corpus (D-128 punto 1).
- Ningún perfil "Balanceado" (D-128 punto 3).

---

## 6. Pruebas (P1-P8, mismo rigor que Capa 2/interpretativo)

1. **P1 — Cobertura del grupo de comparación**: para una combinación conocida
   (Estrategia/Timeframe/Dataset) con 3 gestores en el corpus, `CalcularCandidatas` evalúa las 3
   filas contra 1 sola mediana (la del grupo), y cada una que cruce el umbral aparece en
   `Candidatas` con su propio `Gestor` — ninguna fila perdida ni inventada, ninguna fusionada.
2. **P2 — Umbral de mediana correcto, calculado sobre el grupo sin gestor**: fixture con 3 filas
   de gestores distintos, mismo `(Estrategia, Timeframe, Dataset)`, valores conocidos de
   `DrawdownMaximoPct` (bajo/medio/alto) — la mediana debe calcularse sobre los 3 valores como un
   solo conjunto (no 3 medianas de 1 elemento cada una); perfil Preservación de capital debe
   incluir las candidatas bajo+medio (≤ mediana) y excluir alto — verificado contra el cálculo
   manual del fixture.
3. **P3 — Sin ranking dentro de `Candidatas`**: el orden de `Candidatas` es el orden de aparición
   en `filas` de entrada, no el orden por `ValorMetrica` — mismo patrón que P3 de
   `analisis_interpretativo` (orden de aparición, no de valor).
4. **P4 — Grupos de comparación nunca mezclan datasets**: 2 filas con mismo
   `(Estrategia, Timeframe)` pero distinto `NombreDataset` producen 2 grupos de comparación
   separados (2 medianas independientes), cada uno evaluado solo contra sus propios gestores —
   verificado con fixture de 2 datasets, cada uno con 2-3 gestores.
5. **P5 — Evidencia insuficiente produce `Candidatas` vacío, no relajación de umbral**: fixture
   donde ninguna fila tiene `Estado=Success`/`PnLTotal` disponible para un perfil dado — el
   resultado declara `ConfiguracionesConEvidencia: 0` y `Candidatas` vacío, no un umbral relajado.
6. **P9 — El grupo de comparación nunca incluye el gestor en su clave**: fixture con 3 gestores en
   la misma combinación Estrategia/Timeframe/Dataset y valores de métrica todos distintos entre sí
   — si el grupo incluyera erróneamente el gestor en su clave, cada grupo tendría 1 fila y el
   umbral incluiría las 3 siempre (defecto ya identificado y evitado en §3); esta prueba falla
   explícitamente si `CalcularCandidatas` excluye 0 de las 3 filas cuando el fixture está diseñado
   para que el umbral de mediana deba excluir exactamente 1.
6. **P6 — Ausencia estructural de selección/ranking (reflexión)**: mismo mecanismo ya usado en
   Capa 2/interpretativo — ningún tipo (`ConfiguracionCandidata`, `RecomendacionExperimental`) ni
   método público de `MotorRecomendacion` contiene un término prohibido (`mejor`, `ganador`,
   `ranking`, `score`, `recomend` en el sentido de selección única, `top`, `optim`).
7. **P7 — Ausencia léxica de lenguaje prescriptivo/absoluto**: sobre el código fuente de
   `MotorRecomendacion.cs`/`ProgramRecomendador.cs`, ausencia de los mismos términos ya prohibidos
   en D-124/P6 de `analisis_interpretativo` (`debe`, `recomendado`, `mejor`, `óptimo`, `usar`,
   `elegir`, `preferible`), extendido con `configuración recomendada` como frase compuesta
   explícitamente prohibida (D-128, punto adicional del auditor).
8. **P8 — Trazabilidad completa**: cada `ConfiguracionCandidata.CarpetasOrigen` referenciada existe
   físicamente y aparece en `MANIFIESTO_CORPUS_CASO5C_V1.json` — mismo mecanismo que P7 de
   `analisis_interpretativo`, ejecutado sobre el corpus real de 67 comparaciones.

Suite de producción (`dotnet test -c Release`) debe permanecer en 126/126. Suite de `caso5/
Caso5.csproj` debe permanecer en 25/25 — esta implementación vive en `caso6/recomendador/`, que
`Caso5.csproj` no compila.

---

## 7. Qué no debe incluir esta especificación (fuera de alcance heredado)

- Ningún cambio en `LectorCorpus`/`AnalisisDescriptivo`/`DetectorRelaciones` — reutilizados por
  referencia, sin modificación (mismo patrón ya usado 2 veces).
- Ninguna estrategia nueva, ningún dataset nuevo.
- Ningún mecanismo de "top-N" ni de límite de cantidad de candidatas devueltas.
- Ningún perfil "Balanceado".
- Ninguna ejecución de campaña ni escritura a `caso5/resultados/`.

---

## Próximo paso

Autorización explícita del auditor para implementar: `caso6/recomendador/` con la estructura de
§4, las pruebas P1-P8 de §6, y la ejecución real sobre el corpus de 67 comparaciones. Tras esto, el
siguiente documento es una auditoría de cierre del Recomendador V1
(`AUDITORIA_RECOMENDADOR_CASO6_V1.md`), verificando cada punto de D-128 contra la implementación
real, mismo patrón que `AUDITORIA_ANALISIS_INTERPRETATIVO_CASO5C_V1.md`.
