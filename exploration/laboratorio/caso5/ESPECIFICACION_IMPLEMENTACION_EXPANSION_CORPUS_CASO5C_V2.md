# Especificación de Implementación — Expansión de Corpus Comparativo V2 (Caso 5C)

Estado: **especificación previa a implementación**. Traduce
`PROPUESTA_EXPANSION_CORPUS_CASO5C_V2.md` a diseño de código concreto. **Ningún código se modifica
en este documento.** No cubre auditoría del corpus ampliado ni Capa 2 — ambas quedan condicionadas
a que esta expansión se ejecute primero.

---

## 1. Mecanismo de ejecución

**Se amplía `caso5/campana_corpus/` (V1, congelado en commit `c83c7a6`) — no se crea un ejecutable
nuevo.** La propuesta (§3) ya estableció que el mecanismo se reutiliza, no se reemplaza. Cambios
sobre los 2 archivos existentes:

- `ProgramCampanaCorpus.cs`: la matriz `estrategias`/`timeframes` se amplía; se agrega un bloque
  separado para la sub-campaña B (repetición) y uno para la sub-campaña C (evidencia parcial).
- `TestsCampanaCorpus.cs`: `VerificarEstructura` recibe los conteos de las 3 sub-campañas y valida
  cada una por separado (P1 ya no asume una única matriz 2×3×3).

**`CampanaCorpus.csproj`**: solo requiere agregar el `<Compile Include>` de las 4 estrategias
nuevas (`EstrategiaZScoreReversion.cs`, `EstrategiaNeutral.cs`, `EstrategiaVolumenBreakout.cs`,
`EstrategiaMhiMayoria.cs`, todas en `exploration/`, mismo patrón `Link="Fixtures\..."` que las 2 ya
linkeadas). Ningún otro cambio de `.csproj`.

---

## 2. Matriz exacta de las 19 comparaciones nuevas

### Sub-campaña A — Cobertura de estrategias (12 comparaciones, 36 corridas internas)

```csharp
(string Nombre, string[] Parametros, Func<Action<InfoOperacionResuelta>?, IStrategy> Crear)[] estrategiasNuevas =
{
    ("ZScore Reversion", new[] { "ventana=5", "umbralEntrada=2.0", "umbralSalida=0.5" },
        onOp => new EstrategiaZScoreReversion(ventana: 5, umbralEntrada: 2.0m, umbralSalida: 0.5m, onOperacionResuelta: onOp)),
    ("Neutral", new[] { "ciclo=10" },
        onOp => new EstrategiaNeutral(ciclo: 10, onOperacionResuelta: onOp)),
    ("Volumen Breakout", Array.Empty<string>(),
        onOp => new EstrategiaVolumenBreakout(onOperacionResuelta: onOp)),
    ("Mhi Mayoria", new[] { "maxMartingalas=2" },
        onOp => new EstrategiaMhiMayoria(maxMartingalas: 2, onOperacionResuelta: onOp)),
};
```

Iterada contra los mismos `timeframes = { "15m", "1h", "1D" }` y los mismos `Gestores()` de V1 —
4 × 3 = 12 comparaciones, 36 corridas. Todos los parámetros ya estaban congelados en
`caso3/TestsCaso3.cs`/`caso3/TestsEstrategiaNeutral.cs`/`caso3/TestsEstrategiaVolumenBreakout.cs`/
`Fase15.cs` antes de esta propuesta (§1 de la propuesta) — ninguno se calibra aquí.

### Sub-campaña B — Repetición exacta de V1 (6 comparaciones, 18 corridas internas)

Misma matriz `estrategias`/`timeframes`/`Gestores()` que V1 (`Tres Mosqueteros`/`Ema Cross` ×
`15m`/`1h`/`1D`), ejecutada una segunda vez, sin ningún cambio de parámetro. Se reutiliza el mismo
bloque de código de V1 sin modificarlo — la repetición es literalmente correr el bucle existente
otra vez.

### Sub-campaña C — Evidencia parcial (1 comparación, 3 corridas internas, todas `Failed`)

```csharp
var entradaFallo = new EntradaProtocolo(
    Estrategia: "Tres Mosqueteros", VersionEstrategia: "1.0", Parametros: new[] { "maxMartingalas=2" },
    CrearEstrategia: onOp => new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: onOp),
    Timeframes: new[] { "1D" }, DirDatasets: dirDatasets,
    NombreDataset: "DatasetInexistente_ParaCorpusDeFallo",
    CapitalInicial: 1000m, Instrumento: instrumento, Costes: costes);

var resultadoFallo = ComparadorGestores.Comparar(entradaFallo, Gestores());
var carpetaFallo = PersistidorComparaciones.Persistir(dirResultados, resultadoFallo);
```

Mismo mecanismo que P7 de `TestsComparadorGestores.cs` (Caso 5B, ya congelado) — un
`NombreDataset` inexistente hace que `EjecutorProtocolo.Ejecutar` produzca `Estado: Failed` en la
corrida, y `ComparadorGestores.Comparar` refleja eso en `FilaComparacionGestor.Estado` sin lanzar
excepción (D-114, comportamiento ya verificado, no nuevo).

**Precisión del auditor, incorporada aquí explícitamente**: `ComparadorGestores.Comparar` no
distingue "comparación exitosa" de "comparación fallida" a nivel de tipo — ambas producen el mismo
`ResultadoComparativoGestores`, la única diferencia es el valor de `Estado`/`Metricas` (`null`) en
cada fila. `PersistidorComparaciones.Persistir` tampoco distingue el caso — recibe el mismo tipo,
escribe el mismo formato de `IDENTIDAD_COMPARACION.json`/`COMPARACION_GESTORES_V1.md` que cualquier
otra comparación. **No se crea ningún camino paralelo para el caso de fallo** — la sub-campaña C no
requiere ningún cambio en `ComparadorGestores.cs`/`PersistidorComparaciones.cs`, solo ejecutar el
mismo mecanismo con una entrada que produce `Failed` en vez de `Success`. Esto es una confirmación
de comportamiento ya existente, no una funcionalidad nueva (ver §7).

---

## 3. Persistencia

Sin cambios respecto a V1/Capa 1 — las 19 comparaciones nuevas se persisten en
`caso5/resultados/`, mismo formato, mismo mecanismo (`PersistidorComparaciones.Persistir`, sin
modificar). La sub-campaña C produce una carpeta `TresMosqueteros_1D_{timestamp}/` indistinguible
en estructura de cualquier otra — su `IDENTIDAD_COMPARACION.json` mostrará
`"estado": "Failed"` en las 3 filas de `gestores`, y su `COMPARACION_GESTORES_V1.md` mostrará
"Métricas: (no disponibles — corrida no exitosa)" en cada gestor (mismo texto que
`RenderizadorComparacionGestores.Generar` ya produce para ese caso, Caso 5B, sin modificación).

---

## 4. Pruebas

Ubicación: mismo archivo `caso5/campana_corpus/TestsCampanaCorpus.cs`, ampliando
`VerificarEstructura` (no se crea un archivo nuevo):

1. **P1 — Matriz de sub-campaña A**: `4 estrategias × 3 timeframes × 3 gestores == 36` ejecuciones
   internas.
2. **P2 — Matriz de sub-campaña B**: `2 estrategias × 3 timeframes × 3 gestores == 18` ejecuciones
   internas (idéntica a V1, confirmando que la repetición no cambió la matriz).
3. **P3 — Ausencia estructural de selección por resultado**: misma verificación textual de V1,
   ampliada para cubrir el cuerpo de los 3 bloques (sub-campañas A, B, C), no solo el primero.
4. **P4 (post-ejecución, en `ProgramCampanaCorpus.cs`, no en el archivo de tests)** — cobertura
   completa: 12 + 6 + 1 = 19 carpetas nuevas persistidas, todas existentes, ninguna duplicada
   (mismo patrón que P3 de V1).
5. **P5 (post-ejecución)** — la comparación de la sub-campaña C tiene efectivamente
   `Estado: Failed` en sus 3 filas y `Metricas: null` en las 3 — confirma que la evidencia parcial
   quedó representada como se buscaba, no como un `Success` accidental.

Suite de producción (`dotnet test -c Release`) debe permanecer en 126/126. Suite de
`caso5/Caso5.csproj` debe permanecer en 25/25 — la expansión vive en `campana_corpus/`, que ya está
excluido del glob de `Caso5.csproj` (§1 de la especificación V1).

---

## 5. Confirmación de que Capa 1 no cambia

- `ComparadorGestores.cs` (Caso 5B): sin modificación — verificable por el mismo P7 de
  `TestsPersistidorComparaciones.cs` ya congelado (reflexión sobre la firma pública de `Comparar`).
- `PersistidorComparaciones.cs` (Caso 5C Capa 1): sin modificación — la sub-campaña C es la prueba
  directa de que el componente ya soporta evidencia parcial sin cambios (§2).
- `RenderizadorComparacionGestores.cs` (Caso 5B): sin modificación.
- D-116/D-117: no reabiertas — el formato de `IDENTIDAD_COMPARACION.json`/
  `COMPARACION_GESTORES_V1.md` es idéntico para las 19 comparaciones nuevas y las 6 de V1.

---

## 6. Qué no debe incluir el código de expansión

- Ninguna condición que decida ejecutar/saltar una sub-campaña o una combinación según el resultado
  de otra ya ejecutada (mismo criterio que P3, ampliado de P2 de V1).
- Ningún cálculo de "mejor gestor", ranking, ni comparación entre la ejecución original de V1 y su
  repetición en la sub-campaña B — esa comparación (¿el perfil se mantuvo estable?) es tarea de la
  auditoría posterior, no del código de campaña.
- Ninguna modificación de las 6 estrategias, los 3 gestores, `ComparadorGestores`,
  `PersistidorComparaciones`, `RenderizadorComparacionGestores`.
- Ningún dataset sintético ni instrumento inventado para simular la diversidad que §1 de la
  propuesta ya declaró irresoluble con lo disponible — la sub-campaña C usa un dataset
  **inexistente** (fuerza `Failed`), no uno sintético que fuerce un resultado económico particular.

---

## 7. Fuera de alcance de esta especificación

No se implementó código. No se ejecuta la expansión. No se audita ningún corpus. No se resuelve la
limitación de diversidad de dataset/instrumento (declarada expresamente irresoluble en
`PROPUESTA_EXPANSION_CORPUS_CASO5C_V2.md` §1, reafirmada aquí). No se decide si una sub-campaña D
(más timeframes) se agrega — fuera del alcance mínimo ya fijado por la propuesta.

---

## Próximo paso

Autorización explícita del auditor para implementar: ampliación de `CampanaCorpus.csproj` (4
`<Compile Include>` nuevos), ampliación de `ProgramCampanaCorpus.cs` (3 bloques: A/B/C) y de
`TestsCampanaCorpus.cs` (P1-P3 previas + P4/P5 post-ejecución), ejecutar las 3 sub-campañas reales
(19 comparaciones nuevas), y confirmar 126/126 producción + 25/25 `caso5/Caso5.csproj` sin
regresión. Tras esto, el siguiente documento es una segunda auditoría de corpus (nombre a decidir:
`AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V2.md` o extensión de la V1), evaluando el corpus acumulado de
25 comparaciones (6 de V1 + 19 de V2) frente a las mismas 5 preguntas que la auditoría V1 ya
respondió.
