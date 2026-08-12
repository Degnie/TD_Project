# Especificación de Implementación — Campaña de Generación de Corpus Comparativo (Caso 5C)

Estado: **especificación previa a implementación**. Traduce `PROPUESTA_CAMPANA_CORPUS_CASO5C_V1.md`
a diseño de código concreto. **Ningún código se modifica en este documento.** No cubre auditoría del
corpus ni Capa 2 — ambas quedan condicionadas a que esta campaña se ejecute primero.

---

## 1. Mecanismo de ejecución

**Ejecutable separado, no una extensión de `caso5/Program.cs`** — mismo criterio que
`modelo_financiero/baseline_financiero/` (Caso 2/D-081): un `.csproj` de campaña dedicado evita que
`caso5/Program.cs` (runner de las 25 pruebas de Caso 5A/5B/5C-Capa1) mezcle su responsabilidad de
verificación con la de generación de evidencia.

**Ubicación**: `exploration/laboratorio/caso5/campana_corpus/`
- `CampanaCorpus.csproj` — mismo patrón de `<Compile Include>`/`<ProjectReference>` que
  `Caso5.csproj`, con las rutas relativas ajustadas un nivel más profundo (`..\..\` en vez de
  `..\`, mismo ajuste que `baseline_financiero/BaselineFinanciero.csproj` hizo respecto a
  `Caso2.csproj`/`Caso4.csproj`), más `<Compile Include="..\ComparadorGestores.cs" Link="Fixtures\
  ComparadorGestores.cs" />` y `<Compile Include="..\PersistidorComparaciones.cs" Link="Fixtures\
  PersistidorComparaciones.cs" />` (los 2 componentes de Caso 5B/5C Capa 1, congelados, consumidos
  sin modificación).
- `ProgramCampanaCorpus.cs` — único punto de entrada, sin clase intermedia (mismo estilo top-level
  statements que `protocolo/Program.cs`/`baseline_financiero/ProgramBaselineFinanciero.cs`).

**Por qué no vive dentro de `Caso5.csproj`**: un proyecto `OutputType=Exe` tiene un único punto de
entrada — agregar un segundo `Program.cs` ahí produciría un conflicto de compilación. La separación
en un `.csproj` propio es la única forma de tener un ejecutable de campaña distinto del runner de
pruebas, sin tocar ninguno de los 2 archivos congelados de Caso 5B/5C Capa 1.

---

## 2. Origen de las entradas

**Estrategias**: `EstrategiaTresMosqueteros`/`EstrategiaEmaCross`, linkeadas igual que en
`Caso5.csproj` — mismos parámetros ya usados en pruebas congeladas (`maxMartingalas=2` para Tres
Mosqueteros; EMA Cross sin parámetros adicionales, mismo criterio que Caso 5A/P7).

**Dataset**: `datasets/reales/BTCUSDT/`, `NombreDataset: "BTCUSDT_2024-01-02_2025-01-02"` — mismo
identificador ya usado en toda comparación de Caso 5A/5B/5C Capa 1, sin variación (§1 de la
propuesta: no existe un segundo dataset real en el repositorio).

**Gestores**: instanciados una vez por combinación con los mismos valores ya congelados:
`new GestorFixedFractional(0.1m)`, `new GestorFixedRisk(50m)`,
`new GestorVolatilitySizing(20, 0.1m, 2m)` — idénticos a los usados en
`TestsComparadorGestores.cs`/`TestsGestoresRiesgo.cs`, sin ningún valor nuevo introducido para la
campaña.

**Economía**: `Instrumento("BTCUSDT", TasaMargen: 0.1m)`, `ConfiguracionCostes(0.001m, 0.001m)`,
`CapitalInicial: 1000m` — mismos valores usados en toda `EntradaBase` de `TestsComparadorGestores.cs`,
sin variación entre las 6 comparaciones (D-113: solo estrategia/timeframe/gestor varían dentro de la
matriz; la configuración económica es constante en toda la campaña, no un eje adicional).

---

## 3. Matriz fija de 18 ejecuciones

```csharp
// spec: PROPUESTA_CAMPANA_CORPUS_CASO5C_V1.md §2 — matriz declarada por completo antes de
// ejecutar. No se agrega, quita, ni reordena ninguna combinacion en funcion de un resultado
// intermedio.
private static readonly string[] Timeframes = { "15m", "1h", "1D" };

private static readonly (string Nombre, Func<Action<ResultadoOperacion>, IStrategy> Crear)[] Estrategias =
{
    ("Tres Mosqueteros", onOp => new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: onOp)),
    ("Ema Cross", onOp => new EstrategiaEmaCross(onOperacionResuelta: onOp)),
};

private static IReadOnlyList<IGestorRiesgo> Gestores() => new IGestorRiesgo[]
{
    new GestorFixedFractional(0.1m),
    new GestorFixedRisk(50m),
    new GestorVolatilitySizing(20, 0.1m, 2m),
};
```

**Bucle de ejecución**: 2 estrategias × 3 timeframes = 6 llamadas a `ComparadorGestores.Comparar`
(cada una internamente ejecuta las 3 corridas de gestor, D-113) = 18 ejecuciones de
`EjecutorProtocolo.Ejecutar` en total. `Gestores()` se reconstruye igual en cada llamada — mismos 3
gestores, mismos parámetros, sin estado compartido entre combinaciones (evita que un gestor con
estado interno de una comparación contamine la siguiente; ninguno de los 3 gestores actuales tiene
estado mutable, pero la reconstrucción es la misma disciplina ya aplicada en
`TestsComparadorGestores.cs`).

**Orden de iteración**: estrategia externa, timeframe interno — sin significado experimental
(D-115 ya estableció que el orden de una lista nunca implica ranking); es simplemente el orden de
ejecución, documentado para que la reproducción manual sea directa.

---

## 4. Qué no debe incluir el código de campaña

- Ninguna condición que decida ejecutar o saltar una combinación según el resultado de otra ya
  ejecutada (verificado por P-de-la-campaña, ver §9).
- Ningún cálculo de "mejor gestor", ranking, ni criterio de orden sobre los 6 resultados.
- Ninguna llamada a un método de `ComparadorGestores`/`PersistidorComparaciones` distinto de
  `Comparar`/`Persistir` (ambos son, respectivamente, el único método público de cada clase — P6/P7
  de Caso 5B/5C Capa 1 ya lo verifican por reflexión; la campaña no necesita repetir esa
  verificación, solo no violarla).
- Ninguna modificación de `EstrategiaTresMosqueteros.cs`/`EstrategiaEmaCross.cs`.

---

## 5. Ubicación de resultados

Mismo directorio que Caso 5C Capa 1: `caso5/resultados/` — la campaña no introduce una carpeta
paralela. Cada una de las 6 llamadas a `PersistidorComparaciones.Persistir` genera su propia carpeta
timestamped (`{Estrategia}_{Timeframe}_{timestamp}/`), indistinguible en estructura de cualquier
otra comparación ya persistida (incluidas las de pruebas) — la campaña no necesita un formato
distinto, ya que D-116/D-117 no varían.

**Nota para la auditoría futura**: como `caso5/resultados/` también contiene carpetas de
`TestsPersistidorComparaciones.cs` (7 pruebas, cada corrida de test genera sus propias carpetas), la
auditoría del corpus deberá poder distinguir "corpus de campaña" de "corpus de pruebas técnicas" —
la forma de distinguirlos (por rango de timestamp, por convención de nombre, o limpiando
`caso5/resultados/` antes de correr la campaña) se decide al ejecutar, no en esta especificación.
Recomendación no vinculante: limpiar `caso5/resultados/` inmediatamente antes de ejecutar la
campaña, dejando el directorio con únicamente las 6 carpetas de campaña al finalizar.

---

## 6. Reproducibilidad

- Igual que D-116/D-117 (Caso 5C Capa 1) ya garantizan: cada comparación de la campaña, si se
  re-ejecuta con la misma matriz, produce el mismo `IDENTIDAD_COMPARACION.json` salvo
  `fechaGeneracionUtc`.
- La campaña no introduce ningún nuevo mecanismo de identidad — reutiliza `IIdentidadGestorRiesgo`
  y `HashCompuesto`/`HashConfiguracionEconomica` ya congelados.
- El código de la campaña (`ProgramCampanaCorpus.cs`) es en sí mismo la documentación de qué se
  ejecutó — no requiere un archivo de configuración separado, mismo criterio que
  `protocolo/Program.cs`/`baseline_financiero/ProgramBaselineFinanciero.cs`.

---

## 7. Qué queda fuera (reafirmado)

- Análisis del corpus generado — la campaña solo produce evidencia, no la interpreta.
- Recomendación, ranking, selección de gestor.
- Modificación de estrategias, gestores, `ComparadorGestores`, `PersistidorComparaciones`.
- Ampliación de `Caso5.csproj` con estrategias adicionales.
- Ampliación de la matriz más allá de las 6 comparaciones declaradas en §3 — una campaña con más
  cobertura es una decisión futura separada, no una extensión in-place de esta.

---

## 8. Pruebas necesarias

A diferencia de Caso 5A/5B/5C Capa 1, esta campaña no es un componente reutilizable con contrato
público — es una ejecución puntual y documentada. Aun así, requiere una verificación mínima de que
respeta su propia regla central (§4, sin selección por resultado):

Ubicación: `caso5/campana_corpus/TestsCampanaCorpus.cs`, mismo patrón runner manual, agregado como
parte de `ProgramCampanaCorpus.cs` (se ejecuta antes de la campaña real, no después — si la
verificación estructural falla, la campaña no corre).

1. **P1 — Matriz fija de 18 ejecuciones**: `Estrategias.Length * Timeframes.Length *
   Gestores().Count == 18`.
2. **P2 — Ausencia estructural de selección por resultado**: reflexión sobre
   `ProgramCampanaCorpus`/el tipo que contiene el bucle de ejecución — ningún método público
   distinto de la ejecución secuencial de la matriz (mismo patrón que P6 de
   `TestsComparadorGestores.cs`/P7 de `TestsPersistidorComparaciones.cs`); en particular, ninguna
   rama condicional dentro del bucle depende de `ResultadoComparativoGestores`/
   `FilaComparacionGestor` de una iteración anterior.
3. **P3 — Las 6 comparaciones se persisten**: tras ejecutar, `caso5/resultados/` contiene
   exactamente 6 carpetas nuevas con el patrón `{Estrategia}_{Timeframe}_*` para las 2 estrategias
   × 3 timeframes de §3 (verificación posterior a la ejecución real, no simulada).

Suite de producción (`dotnet test -c Release`) debe permanecer en 126/126. Suite de `caso5/Caso5.csproj`
debe permanecer en 25/25 — la campaña vive en un `.csproj` separado y no la afecta.

---

## 9. Fuera de alcance de esta especificación

No se implementó código. No se ejecuta la campaña. No se audita ningún corpus. No se decide si
`caso5/resultados/` se limpia antes de ejecutar (recomendación no vinculante en §5, decisión de
ejecución). No se especifica `AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V1.md` — depende de que esta
campaña produzca el corpus real primero.

---

## Próximo paso

Autorización explícita del auditor para implementar: `CampanaCorpus.csproj`,
`ProgramCampanaCorpus.cs` (matriz + bucle + P1/P2), ejecutar la campaña real (genera las 6
comparaciones + verifica P3), y confirmar 126/126 producción + 25/25 `caso5/Caso5.csproj` sin
regresión. Tras esto, el siguiente documento es
`caso5/AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V1.md`, con las preguntas ya fijadas por el auditor.
