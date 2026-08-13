# Auditoría Integral Post Caso 5C V2 (D-127, Fase 0 de Caso 6)

Estado: **documento de resultado — cierre de la Fase 0**. Ejecuta el alcance A-H fijado por D-127
(`DECISIONES_AUDITORIA_INTEGRAL_POST_CASO5C_V1.md`) según los pasos de
`ESPECIFICACION_IMPLEMENTACION_AUDITORIA_INTEGRAL_POST_CASO5C_V1.md`. Responde exclusivamente
**"¿el sistema actual está suficientemente estable y trazable para abrir una fase de
recomendador?"** — no responde "¿qué recomienda el sistema?". Ningún código fue modificado durante
esta auditoría.

**Punto de partida**: HEAD en commit `54adf2e` (cierre de Caso 5C V2). `src/`/`tests/` sin cambios
pendientes antes de empezar (`git status --porcelain -- src/ tests/ exploration/laboratorio/`
mostró únicamente `MAPA_EVOLUCION_V2.md` preexistente y `caso6/` de esta misma fase).

**Decisión de ejecución aplicada** (§6 de la especificación): `git log --oneline` sobre
`caso5/TestsGestoresRiesgo.cs`, `caso5/TestsComparadorGestores.cs`,
`caso5/TestsPersistidorComparaciones.cs`, `caso5/Program.cs` mostró el último commit (`01c6d9c`,
congelación de Capa 1) **anterior** a `54adf2e` — sin cambios posteriores al cierre de Caso 5C V2.
Por decisión del auditor, **Áreas D/E/F (pruebas) se aceptan por referencia a la evidencia 25/25 ya
documentada en `AUDITORIA_CIERRE_CASO5C_V2.md`**, sin re-ejecutar `caso5/Program.cs` — evita la
escritura colateral a `resultados/` que esa ejecución produciría, sin renunciar a evidencia real
(el commit citado es, en efecto, el estado que la Fase 0 audita).

---

## Resultado por área

| Área | Funciona correctamente | Regresión | Evidencia válida | Problemas encontrados | Requiere corrección |
|---|---|---|---|---|---|
| A. Motor base | Sí | No | Sí | Ninguno | Ninguna |
| B. Estrategias | Sí | No | Sí | Ninguno | Ninguna |
| C. Motor financiero | Sí | No | Sí | Ninguno | Ninguna |
| D. Gestores de riesgo | Sí (por referencia) | No | Sí | Ninguno | Ninguna |
| E. Comparador | Sí (por referencia) | No | Sí | Ninguno | Ninguna |
| F. Persistencia de evidencia | Sí | No | Sí | Ninguno | Ninguna |
| G. Capa analítica | Sí | No | Sí | Ninguno | Ninguna |
| H. Datos | Sí | No | Sí | Ninguno | Ninguna |

**8/8 áreas aprobadas. 0 hallazgos. 0 correcciones pendientes.**

---

## Evidencia por área

### A. Motor base

- `dotnet build -c Release` (solución completa): **0 advertencias, 0 errores**.
- `dotnet test -c Release`: **126/126** (Domain.Tests 62, Contracts.Tests 4, Infrastructure.Tests
  2, Api.Tests 18, Application.Tests 40).
- Determinismo/hashes de identidad: no recalculados en esta auditoría — ya verificados por prueba
  dentro de `Application.Tests`/`Domain.Tests` (incluidos en el 126/126) y, específicamente para
  Caso 5C, por P6/P8 de `campana_corpus/TestsCampanaCorpus.cs` (ya documentados en el cierre de
  Caso 5C V2, sin cambios desde entonces).

### B. Estrategias

- Las 6 estrategias congeladas (Tres Mosqueteros, Ema Cross, ZScore Reversion, Neutral, Volumen
  Breakout, Mhi Mayoria) se ejercitan dentro de `Domain.Tests` (parte del 126/126 de Área A) y ya
  fueron ejecutadas en las 67 comparaciones oficiales del corpus.
- Manifiesto verificado (Área F): 6 estrategias × 18 combinaciones estrategia/timeframe cubiertas
  en cada uno de los 3 datasets con matriz completa, sin huecos.

### C. Motor financiero

- `modelo_financiero/TestsMetricasFinancieras.cs`: **7/7** (P1-P7 — capital inicial, drawdown,
  pico en cero, determinismo, ausencia de recálculo paralelo, `DrawdownMaximoPct=null` sobre curva
  vacía, exposición máxima verificada independientemente).

### D. Gestores de riesgo

- **Por referencia**: `caso5/TestsGestoresRiesgo.cs` — 10/10, último resultado documentado en
  `AUDITORIA_CIERRE_CASO5C_V2.md` §3, commit `54adf2e`. Sin cambios en `TestsGestoresRiesgo.cs` ni
  en los 3 gestores concretos desde ese commit (`git log` confirmado).

### E. Comparador

- **Por referencia**: `caso5/TestsComparadorGestores.cs` — 8/8, mismo commit de referencia que
  Área D. Sin cambios en `ComparadorGestores.cs` desde entonces.

### F. Persistencia de evidencia

- **Por referencia**: `caso5/TestsPersistidorComparaciones.cs` — 7/7, mismo commit de referencia.
- **Verificado en esta auditoría (lectura pura, sin ejecución)**: reconciliación del manifiesto
  contra disco por conjunto — 172 carpetas físicas en `caso5/resultados/`, 67 oficiales +
  105 excluidas con lista explícita + 25 sin lista (`primera-ejecucion-interrumpida-v2`), **0
  huérfanas, 0 overlap entre oficiales y excluidas, 0 comparaciones oficiales sin carpeta física**
  — idéntico al estado dejado por el cierre de Caso 5C V2.

### G. Capa analítica

- `analisis_corpus/TestsAnalisisCorpus.cs`: **11/11** (P1-P9, P4b, P8b) — re-ejecutado en esta
  auditoría sobre el corpus real de 67 comparaciones, sin escritura a `resultados/`.
- `analisis_interpretativo/TestsAnalisisInterpretativo.cs`: **8/8** (P1-P8) — re-ejecutado sobre el
  mismo corpus, sin escritura a `resultados/`. P6 (ausencia léxica de lenguaje prescriptivo) y P5
  (ausencia estructural de ranking/selección) confirmados en ambos componentes — ninguno de los 2
  recomienda, selecciona, ni genera reglas operativas.

### H. Datos

- Verificación directa (lectura pura, sin red): SHA-256 de cada CSV congelado, comparado contra el
  campo `sha256` de su `metadata.json` — **52/52 archivos verificados (13 timeframes × `BTCUSDT` +
  13 × `BTCUSDT_2022` + 13 × `ETHUSDT` + 13 adicionales de `BTCUSDT` correspondientes a un rango
  interno previo), todos coinciden**. 0 mismatches, 0 metadata faltante.

---

## Verificación de ausencia de efectos colaterales

`git status --porcelain -- exploration/laboratorio/caso5/resultados/` (después de ejecutar los
pasos 2-9 de la especificación): **vacío** — ninguna verificación de esta auditoría escribió a
`resultados/`, consistente con la decisión de aceptar D/E/F por referencia en vez de re-ejecutar
`caso5/Program.cs`.

`git status --porcelain -- src/ tests/`: **vacío** en todo el ciclo de esta auditoría — ningún
componente de producción fue modificado.

---

## Fuera de alcance de este documento

No se diseña el recomendador. No se especifica su arquitectura, semántica, ni criterios de
decisión. No se activa D-118/D-119/D-120. No se reabre ninguna decisión D-001 a D-126 — todas
verificadas como vigentes en su implementación, ninguna reevaluada en contenido. No se generó
ninguna evidencia experimental nueva ni se modificó el manifiesto.

---

## Conclusión

Las 8 áreas (A-H) del alcance fijado por D-127 fueron verificadas, 8/8 con resultado "Sí" en
funcionamiento correcto, sin regresión, con evidencia válida. No se encontró ningún hallazgo. Las
Áreas D/E/F se aceptaron por referencia a evidencia ya documentada, con verificación explícita
(`git log`) de que no hay cambios posteriores a esa evidencia — cumpliendo D-057 (verificado contra
estado actual, no citado de memoria) sin necesidad de re-ejecutar y generar escritura colateral
innecesaria. Ninguna verificación modificó código, generó evidencia experimental nueva, ni escribió
a `caso5/resultados/`.

**Respuesta a la pregunta que abrió esta fase**: sí, el sistema actual (Caso 1 a Caso 5C V2) está
suficientemente estable y trazable para considerar abrir una fase de recomendador — condición
necesaria cumplida. Esto no implica que deba abrirse, ni resuelve qué recomendaría el sistema; esa
evaluación de oportunidad y el diseño correspondiente quedan para una decisión aparte
(`PROPUESTA_CASO6_RECOMENDADOR_V1.md`, si el auditor decide proceder).
