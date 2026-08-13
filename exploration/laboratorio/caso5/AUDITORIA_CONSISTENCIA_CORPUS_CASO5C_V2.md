# Auditoría — Consistencia del Corpus Ampliado (Caso 5C, post D-126)

Estado: **documento de auditoría — verifica que el manifiesto actualizado representa
correctamente el estado físico del corpus, no analiza el contenido de ninguna comparación**.
Continúa `DECISIONES_INCORPORACION_ETHUSDT_CASO5C_V1.md` (D-126). **No ejecuta
`AnalisisDescriptivo`, `DetectorRelaciones`, ninguna interpretación, ni ninguna comparación
BTCUSDT-vs-ETHUSDT como resultado analítico** — esta auditoría valida el objeto sobre el cual se
analizará, no lo analiza.

**Nota de precisión previa**: entre el cierre de D-126 y esta auditoría, la ejecución de
`caso5/Program.cs` para verificar regresión (10/10+8/8+7/7) generó 1 carpeta adicional
(`TresMosqueteros_1D_20260813T060224Z`, mismo patrón de escritura interrumpida ya conocido). Fue
incorporada a la categoría `escritura-interrumpida` (10→11) como **actualización de consistencia
del artefacto de gobierno**, no como nueva evidencia experimental, antes de iniciar esta auditoría
— para que la verificación partiera de un manifiesto que sí reflejara el disco en el momento de
auditar.

---

## 1. ¿El manifiesto representa correctamente el estado físico del corpus?

**Sí, verificado por conjunto, no por suma aritmética de cantidades declaradas.**

```
Carpetas fisicas en disco (caso5/resultados/):        172
Comparaciones oficiales (comparaciones[]):              67
Excluidas con lista explicita de carpetas:              105
  - escritura-interrumpida:                               11
  - prueba-tecnica-preexistente:                            1
  - repeticion-completa-v1-abc-verificada-reproducible:    25
  - repeticion-tecnica-subcampana-e:                       68
Excluidas sin lista explicita (cantidad declarada):      25
  - primera-ejecucion-interrumpida-v2:                     25
```

**Verificación por unión de conjuntos** (no por suma, que sobrecontaría si hubiera solapamiento):
`disco − (oficiales ∪ excluidas-con-lista) = 0` — cada una de las 147 carpetas con lista explícita
(67 oficiales + 105 excluidas) fue confirmada existente físicamente, sin overlap entre las 2
listas (`oficiales ∩ excluidas = ∅`). Las 25 restantes de `primera-ejecucion-interrumpida-v2`
(categoría documentada solo por cantidad y rango de timestamp desde su creación original, sin lista
carpeta por carpeta) completan las 172 sin dejar ninguna carpeta sin clasificar.

**Ninguna carpeta física quedó fuera de toda clasificación.**

---

## 2. ¿Las 67 comparaciones oficiales tienen cobertura, trazabilidad, e identidad experimental?

### 2.1 — Cobertura por dataset

| Dataset | Filas | Combinaciones únicas (Estrategia×Timeframe) | Origen |
|---|---|---|---|
| `BTCUSDT_2024-01-02_2025-01-02` | 30 | 18 (matriz completa) | V1 (6) + V2 (24) |
| `BTCUSDT_2022-01-01_2023-01-01` | 18 | 18 (matriz completa) | SubcampanaD |
| `ETHUSDT_2024-01-02_2025-01-02` | 18 | 18 (matriz completa) | SubcampanaE |
| `DatasetInexistente_ParaCorpusDeFallo` | 1 | 1 (deliberado, evidencia parcial) | V2 |

**Los 3 datasets con matriz completa (BTCUSDT 2024-2025, BTCUSDT 2022-2023, ETHUSDT 2024-2025)
cubren exactamente las mismas 18 combinaciones (6 estrategias × 3 timeframes) cada uno** — sin
huecos de cobertura en ninguno de los 3, condición necesaria para que una futura comparación entre
ellos (Capa 2/interpretativo) no encuentre una combinación presente en un lado y ausente en otro.

**Las 30 filas de `BTCUSDT_2024-01-02_2025-01-02` sobre solo 18 combinaciones** reflejan 12
duplicados internos **esperados y ya documentados**: V1 (6 combinaciones: Tres Mosqueteros/Ema
Cross × 3 timeframes) se repite dentro de V2 como "Sub-campaña B — repetición exacta de la matriz
V1" (mismo criterio ya verificado en auditorías previas, no un error de esta verificación).

### 2.2 — Trazabilidad

**Las 67 carpetas oficiales existen físicamente, cada una con sus 2 archivos requeridos**
(`IDENTIDAD_COMPARACION.json`, `COMPARACION_GESTORES_V1.md`) — verificado por inspección directa,
0 carpetas faltantes, 0 archivos faltantes. **Las 67 tienen exactamente 3/3 gestores en estado
`Success`**, salvo la única comparación deliberadamente parcial ya documentada (Sub-campaña C,
`DatasetInexistente_ParaCorpusDeFallo`, 3 filas en estado `Incomplete`, sin métricas — evidencia de
fallo intencional, no un defecto).

**Ninguna comparación oficial está duplicada dentro de `comparaciones[]`** (0 nombres de carpeta
repetidos en la lista).

### 2.3 — Identidad experimental

Ya verificada por prueba durante la ejecución (no re-verificada aquí por cálculo, para no
introducir un análisis nuevo fuera del alcance de esta auditoría):

- **Sub-campaña D** (`AUDITORIA_DIVERSIDAD_TEMPORAL_CASO5C_V1.md`, P6 del log de ejecución):
  `HashCompuesto` distingue el dataset temporal, `HashConfiguracionEconomica` no depende del
  período, reproducibilidad confirmada.
- **Sub-campaña E** (`AUDITORIA_SUBCAMPANA_E_CASO5C_V1.md` §2-§3, P8 del log de ejecución):
  `HashCompuesto` distingue el instrumento, `HashConfiguracionEconomica` idéntico entre `BTCUSDT`/
  `ETHUSDT`, reproducibilidad confirmada.

**Esta auditoría no recalcula estos hashes** — los toma como evidencia ya producida y documentada
en las auditorías previas correspondientes, consistente con el alcance de "validar el objeto antes
de analizarlo", no repetir verificaciones ya cerradas.

---

## 3. ¿Las exclusiones están separadas correctamente?

**Sí — 0 overlap entre `comparaciones[]` (oficiales) y cualquier categoría de `excluidos`**
(verificado por intersección de conjuntos, resultado vacío).

| Categoría | Cantidad | Carpetas con `volatility-sizing` completo | Relación con corpus oficial |
|---|---|---|---|
| `primera-ejecucion-interrumpida-v2` | 25 | N/A (primera pasada completa, descartada por reintento) | Ninguna — reemplazada por V2 oficial |
| `escritura-interrumpida` | 11 | 0/11 (todas con 2/3 gestores) | Ninguna — evidencia incompleta |
| `prueba-tecnica-preexistente` | 1 | N/A (generada por test unitario) | Ninguna — no es evidencia experimental |
| `repeticion-completa-v1-abc-verificada-reproducible` | 25 | 25/25 (verificadas idénticas a V2 oficial) | Ninguna — duplicado verificado, no cuenta doble |
| `repeticion-tecnica-subcampana-e` | 68 | 68/68 (verificadas idénticas a la oficial equivalente) | Ninguna — duplicado verificado, no cuenta doble |

**Cada categoría mantiene la razón por la que su evidencia no es evidencia oficial adicional** —
ninguna carpeta excluida representa una combinación experimental que falte en el corpus oficial;
todas son o repeticiones verificadas (mismo contenido que una comparación ya oficial) o evidencia
incompleta (menos de 3 gestores), nunca una combinación nueva descartada sin explicación.

---

## 4. ¿El corpus ampliado mantiene las garantías previas?

- **Sin selección por resultado**: los criterios de inclusión/exclusión aplicados en esta
  actualización (cobertura completa, 3/3 gestores, identidad experimental verificada,
  reproducibilidad) son los mismos 4 ya fijados en D-126 — ninguno depende de qué mostraron las
  métricas financieras.
- **Sin mezcla de dimensiones**: cada uno de los 3 datasets con matriz completa varía una sola
  dimensión respecto a la matriz base (`BTCUSDT` 2024-2025): Sub-campaña D varía tiempo, Sub-
  campaña E varía instrumento — nunca ambas a la vez (mismo rango 2024-2025 en Sub-campaña E,
  mismo instrumento `BTCUSDT` en Sub-campaña D).
- **Sin recalibración de parámetros**: `TasaMargen=0.1m`, costes `0.001m`/`0.001m`, capital inicial
  `1000m` — sin cambio en ninguna de las 67 comparaciones oficiales (D-030 intacta).
- **D-118/D-119/D-120 intactas**: ningún mecanismo de ranking, selección, ni recomendación fue
  introducido por esta actualización — el manifiesto es una lista de datos declarados, no un
  cálculo ni una conclusión.

---

## 5. Qué no se hizo (restricción respetada)

- No se ejecutó `AnalisisDescriptivo.Resumir` ni `DetectorRelaciones` sobre el corpus ampliado.
- No se generó ninguna comparación `BTCUSDT` vs `ETHUSDT` como resultado analítico — las únicas
  cifras de este documento son de cobertura/trazabilidad (conteos de filas, combinaciones,
  archivos), no de métricas financieras.
- No se modificó ninguna de las 67 comparaciones oficiales.
- No se recalculó ningún hash — se citan los ya producidos y documentados en auditorías previas.
- No se activó ninguna forma de recomendación.

---

## 6. Estado consolidado tras esta auditoría

```
Manifiesto refleja el disco                    ✅ verificado por conjunto (0 huerfanas)
Cobertura de las 67 oficiales                   ✅ 3 datasets, 18 combinaciones cada uno, sin huecos
Trazabilidad de las 67 oficiales                ✅ archivos completos, 0 faltantes
Identidad experimental (D/E)                    ✅ ya verificada por prueba, referenciada aqui
Exclusiones separadas sin overlap               ✅ 0 interseccion con oficiales
Garantias previas (D-030/D-118/119/120/121/125) ✅ intactas
Analisis sobre corpus ampliado                  ⏳ pendiente — proximo paso, fuera de este documento
```

---

## Fuera de alcance de este documento

No se ejecuta ningún análisis descriptivo ni interpretativo sobre las 67 comparaciones. No se
compara `BTCUSDT` vs `ETHUSDT` en términos de resultados. No se decide si procede ejecutar Capa 2/
análisis interpretativo sobre el corpus ampliado — eso requeriría su propia autorización explícita,
posterior a esta auditoría.

---

## Conclusión

El manifiesto actualizado (`totalOficial: 67`) representa correctamente el estado físico de
`caso5/resultados/` (172 carpetas, verificado por conjunto sin huérfanas). Las 67 comparaciones
oficiales tienen cobertura completa en sus 3 datasets con matriz completa (18 combinaciones cada
uno, sin huecos), trazabilidad íntegra (0 archivos faltantes), e identidad experimental ya
verificada por prueba (instrumento y período como dimensiones aisladas, configuración económica
constante). Las exclusiones están separadas sin overlap con el corpus oficial, cada una con su
razón documentada. Ninguna garantía previa (D-030, D-118/119/120, D-121, D-125) fue comprometida
por la ampliación. El corpus está en condiciones de ser el objeto de un análisis descriptivo/
interpretativo posterior — esa ejecución queda como decisión separada, no incluida aquí.
