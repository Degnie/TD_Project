# Auditoría de Cierre — Caso 5C V2: Corpus Ampliado, Análisis Descriptivo e Interpretativo

Estado: **documento de cierre de fase — Caso 5C V2 completa**. Consolida evidencia verificada del
ciclo propuesta → decisión → especificación → implementación → pruebas → auditoría para D-123 a
D-126, sobre la base ya congelada de Capa 1 (`caso5c-capa1-v1-experimental`) y de diversidad
temporal (D-121/D-122). D-118 a D-120 (recomendación) quedan fuera de este cierre — resueltas solo
a nivel de principio, sin implementación. Mismo patrón que `AUDITORIA_CASO5C_CAPA1_V1.md`.

---

## 1. Objetivo de la fase V2

**Objetivo**: responder si el corpus persistido por Capa 1 puede (a) declararse formalmente como
conjunto oficial distinguido de trazas de desarrollo, (b) describirse sin interpretación, (c)
interpretarse de forma limitada sin cruzar a recomendación, y (d) ampliarse a una segunda dimensión
de diversidad de evidencia (instrumento) sin comprometer ninguna de las garantías anteriores.

**Incluye**:
- Manifiesto declarado (`MANIFIESTO_CORPUS_CASO5C_V1.json`), construido por inspección de
  contenido, nunca por timestamp.
- Análisis descriptivo (Capa 2, D-123): cobertura, distribuciones, comparación entre datasets,
  casos atípicos.
- Análisis interpretativo limitado (D-124): relaciones observadas, agrupaciones por patrón,
  condiciones de aparición, consistencia entre conjuntos — siempre conjunto completo, nunca una
  combinación destacada.
- Diversidad de instrumento (D-125/D-126): incorporación de `ETHUSDT` bajo el mismo rango temporal
  ya congelado para `BTCUSDT`, mismo mecanismo de exploración/validación/congelación/campaña ya
  usado para diversidad temporal.

**No incluye** (confirmado por ausencia de código y por prueba, no solo por declaración):
- Ranking, puntuación compuesta, ni "mejor gestor/instrumento/estrategia" en ningún artefacto.
- Selección automática de configuración.
- Recomendación en cualquier forma (D-118/D-119/D-120 sin activar).
- Modificación de `ComparadorGestores.cs`, `PersistidorComparaciones.cs`,
  `RenderizadorComparacionGestores.cs`, `EjecutorProtocolo.cs` (Capa 1, congelados).

---

## 2. Relación con las fases previas

Caso 5C V2 no reconstruye ni duplica infraestructura — extiende lo que Capa 1 y la diversidad
temporal ya congelaron:

| Fase previa aporta | V2 lo usa para |
|---|---|
| `PersistidorComparaciones`/`IDENTIDAD_COMPARACION.json` (Capa 1) | Ser la única fuente de evidencia leída por `LectorCorpus` — sin reejecutar ninguna comparación |
| `COMPARACION_GESTORES_V1.md` (Capa 1) | Ser la fuente exacta de las métricas parseadas por `LectorCorpus` |
| `ExploradorDisponibilidad`/`ValidadorIntegridadDatos` (D-122) | Explorar y validar `ETHUSDT` sin ningún cambio de contrato — solo una variable de entorno adicional para elegir símbolo |
| Vista de compatibilidad `BTCUSDT_2022/` (D-121) | Precedente directo del patrón de carpeta paralela usado para `datasets/reales/ETHUSDT/` |

---

## 3. Ciclo D-123 a D-126, verificado

| Decisión | Propuesta | Decisión | Especificación | Implementación | Auditoría |
|---|---|---|---|---|---|
| D-123 (Capa 2 descriptiva) | `PROPUESTA_CASO5C_CAPA2_V1.md` | `DECISIONES_CASO5C_CAPA2_V1.md` | `ESPECIFICACION_IMPLEMENTACION_CASO5C_CAPA2_V1.md` | `analisis_corpus/` | `RESULTADO_ANALISIS_CORPUS_CASO5C_CAPA2_V1.md` |
| D-124 (análisis interpretativo) | `PROPUESTA_EVOLUCION_POST_CAPA2_V1.md` | `DECISIONES_EVOLUCION_POST_CAPA2_V1.md` | `ESPECIFICACION_IMPLEMENTACION_ANALISIS_INTERPRETATIVO_CASO5C_V1.md` | `analisis_interpretativo/` | `AUDITORIA_ANALISIS_INTERPRETATIVO_CASO5C_V1.md` |
| D-125 (diversidad de instrumento) | `PROPUESTA_DIVERSIDAD_INSTRUMENTO_CASO5C_V2.md` | `DECISIONES_DIVERSIDAD_INSTRUMENTO_CASO5C_V2.md` | `ESPECIFICACION_IMPLEMENTACION_DIVERSIDAD_INSTRUMENTO_CASO5C_V2.md` | Sub-campaña E (`campana_corpus/`) + dataset `ETHUSDT/` | `AUDITORIA_SUBCAMPANA_E_CASO5C_V1.md` |
| D-126 (incorporación al corpus oficial) | — (auditoría de Sub-campaña E dejó la pregunta abierta) | `DECISIONES_INCORPORACION_ETHUSDT_CASO5C_V1.md` | — (actualización mecánica, sin especificación separada) | Manifiesto 49→67 | `AUDITORIA_CONSISTENCIA_CORPUS_CASO5C_V2.md` |

**Las 4 decisiones están 🟢 Aprobadas e implementadas**, ninguna reasignada a contenido distinto del
originalmente registrado. Cada ciclo completó sus 6 etapas (propuesta/decisión/especificación/
implementación/pruebas/auditoría) antes de avanzar a la siguiente decisión — verificado por el
orden cronológico de los documentos listados.

---

## 4. Hallazgos durante la fase, y cómo se trataron

**Discrepancia de carpetas físicas (106→108→172 vs manifiesto)**: en 3 momentos distintos de esta
fase aparecieron carpetas físicas no contempladas por el manifiesto vigente en ese momento (52 vs
25 candidatas en la ronda de Capa 2; 2 carpetas de escritura interrumpida durante la propia
implementación; 1 carpeta adicional generada por la verificación de regresión posterior a D-126).
**En los 3 casos**: se clasificó por inspección de contenido (identidad + métricas), nunca por
timestamp ni por asunción de "el programa repite lo mismo" — documentado en
`CLASIFICACION_PROPUESTA_CARPETAS_SUBCAMPANA_E_V1.md` y en la nota de consistencia previa de
`AUDITORIA_CONSISTENCIA_CORPUS_CASO5C_V2.md`.

**2 defectos de infraestructura en el texto fijo de `Limitaciones`** (Capa 2 e interpretativo):
afirmación "instrumento único (BTCUSDT)" codificada como literal, desactualizada al incorporar
`ETHUSDT`; la primera corrección introdujo un segundo defecto (contar
`DatasetInexistente_ParaCorpusDeFallo` como instrumento). Ambos corregidos y documentados en
`RESULTADO_ANALISIS_CORPUS_CASO5C_V2.md` §0 — mismo patrón que el defecto de "3 periodos" ya
corregido en la fase de Capa 2 original. Ninguno de los 2 alteró metodología ni criterios de
interpretación.

**Ninguno de estos hallazgos requirió reabrir una decisión ya registrada** — todos se resolvieron
como correcciones de consistencia dentro del alcance ya autorizado, con reporte explícito antes de
aplicar cada corrección.

---

## 5. Garantías verificadas

- **Capa 1 intacta**: sin modificación en ningún archivo de `ComparadorGestores.cs`/
  `PersistidorComparaciones.cs`/`RenderizadorComparacionGestores.cs`/`EjecutorProtocolo.cs` —
  verificado por P9 (`analisis_corpus`)/P8 (`analisis_interpretativo`), ausencia estructural de
  referencias a componentes de ejecución.
- **`src/`/`tests/` intactos**: `git status --porcelain -- src/ tests/` vacío en las 4 decisiones.
- **Manifiesto reconciliado con disco por conjunto**: 172 carpetas físicas, 0 sin clasificar
  (verificado 2 veces — antes y después del ajuste puntual post-D-126).
- **Identidad experimental verificada por hash**: P6 (Sub-campaña D)/P8 (Sub-campaña E) — el
  dataset/instrumento es el único eje que cambia `HashCompuesto`, `HashConfiguracionEconomica` no
  depende de periodo ni instrumento, reproducibilidad confirmada por doble ejecución en ambos casos.
- **Ausencia estructural de ranking/selección**: P5/P6 en ambos componentes de análisis — ningún
  tipo de salida contiene campos con términos prohibidos, ninguna plantilla de texto contiene
  lenguaje prescriptivo. Reconocido explícitamente como barrera, no garantía completa, compensado
  por redundancia estructural.
- **Parámetros económicos sin cambio en las 67 comparaciones oficiales**: verificado por
  `HashConfiguracionEconomica` idéntico entre instrumentos y periodos (D-030).
- **Instrumento como única dimensión variada en Sub-campaña E**: mismo rango temporal
  (2024-01-02–2025-01-02) que la matriz base `BTCUSDT`, mismas estrategias/gestores/parámetros.

---

## 6. Estado consolidado

```
Caso 5C Capa 1 (evidencia)                       ✅ congelada (V1 Experimental)
Corpus declarado por manifiesto                  ✅ 67 comparaciones (BTCUSDT 49 + ETHUSDT 18)
Diversidad temporal (D-121/D-122)                ✅ incorporada (Sub-campana D, 18 comparaciones)
Capa 2 descriptiva (D-123)                       ✅ ejecutada sobre corpus ampliado
Analisis interpretativo limitado (D-124)         ✅ ejecutado sobre corpus ampliado
Diversidad de instrumento (D-125/D-126)          ✅ incorporada (Sub-campana E, 18 comparaciones)
Recomendacion / seleccion automatica             ❌ no existe (D-118/D-119/D-120 intactas)
```

Suites de regresión sin cambios en toda la fase: 126/126 producción, 25/25 Caso5 Capa1/5A/5B, 11/11
`analisis_corpus`, 8/8 `analisis_interpretativo`, 9/9 `campana_corpus` (incluyendo P6/P7 Sub-
campaña D y P8/P9 Sub-campaña E).

---

## 7. Fuera de alcance de este documento

No se evalúa si la evidencia acumulada (67 comparaciones, 2 instrumentos, 2 periodos, 2 niveles de
análisis) es suficiente para abrir una fase de recomendación — D-118/D-119/D-120 permanecen sin
resolver más allá del principio ya fijado. No se elige un tercer instrumento ni periodo adicional.
No se abre ninguna nueva capacidad analítica — evaluada y descartada explícitamente por el auditor
en esta misma fase, por falta de una pregunta metodológica concreta que la justifique.

---

## Conclusión

Caso 5C V2 completa el ciclo formal para las 4 decisiones que definieron esta fase (D-123 a D-126),
cada una con su propuesta, decisión, especificación (donde aplicó), implementación verificada por
prueba, y auditoría de cierre. El corpus pasó de 49 a 67 comparaciones oficiales mediante un
mecanismo de declaración por contenido ya usado 4 veces sin excepción. Los 2 niveles de análisis
(descriptivo e interpretativo) operan correctamente sobre el corpus ampliado, con 2 defectos de
consistencia de texto detectados y corregidos durante el propio proceso — evidencia de que la
infraestructura fue validada contra un cambio real del corpus, no solo contra su estado inicial.
Ninguna garantía estructural (ausencia de ranking, selección, recomendación; parámetros económicos
constantes; Capa 1/`src/`/`tests/` intactos) fue comprometida. D-118/D-119/D-120 permanecen
exactamente en el mismo estado de principio que tenían antes de abrir esta fase. Queda pendiente
únicamente el commit conjunto de todo lo generado, sujeto a autorización explícita separada de este
documento.
