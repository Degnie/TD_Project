# Especificación de Métricas por Escenario de Mercado V1

Estado: **especificación — Fase 1.5-A, Paso 3 del Caso 1**. Documento de diseño, no
implementación. No se calcula ningún porcentaje, winrate ni comparación todavía — "primero definir
qué se mide, después calcular" (auditoría de cierre del Paso 2). No se modifica
`AsignadorOperacionRegimen.cs` (Paso 2, cerrado), `InfoOperacionResuelta`, `ClasificadorRegimenV1.cs`
ni `AnalizadorOperacional.cs` (Fase 1.2, congelado).

---

## 1. Objetivo

Definir cómo se agrupan y qué catálogo de métricas se calcula sobre `IReadOnlyList<OperacionConRegimen>`
(salida del Paso 2), reutilizando exactamente el catálogo ya congelado de Fase 1.2
(`ESPECIFICACION_ANALIZADOR_OPERACIONAL_V1.md §4`) y el patrón estructural de Fase 1.3
(`PerfilMultiTimeframe`/`ComparadorMultiTimeframe`), sustituyendo "agrupado por timeframe" por
"agrupado por régimen".

```
InfoOperacionResuelta → AsignadorOperacionRegimen → [ESTE PASO: agrupación + métricas] → Reporte
```

No se define en este documento ninguna fórmula nueva — D-015 (analizadores por capas, sin
recalcular) sigue vigente.

---

## 2. Pregunta pendiente — D-044: dimensión principal de agrupación

**No se resuelve en este documento.** Se presenta en el chat, conforme al método ya establecido en
todas las fases anteriores, para decisión explícita del auditor antes de escribir código.

Cada `OperacionConRegimen` (Paso 2) trae **dos** campos de régimen — `RegimenEntrada` y
`RegimenResolucion` — que casi siempre difieren (verificado en Paso 1: por el desfase RN-13, ni
siquiera las operaciones sin martingala comparten vela de entrada y resolución). El catálogo de
métricas de la sección 3 puede agruparse por cualquiera de los dos, o exponer ambos por separado.
Esta es exactamente D-044, ya adelantada por la auditoría del Paso 2 con una recomendación
preliminar (Opción C) pero **explícitamente no fijada como decisión final** — se retoma aquí con
el análisis de costo/beneficio completo, antes de construir cualquier tabla.

**Opción A — Agrupar solo por `RegimenEntrada`.**
- Responde: "¿cómo se comporta la estrategia cuando decide entrar bajo este escenario?"
- Alineado con la lógica de señal — el régimen de entrada es lo único que la estrategia "ve" al
  decidir, ya que no tiene conocimiento del futuro (RN-13 ya impone esto en el motor).
- Costo: es la agrupación más simple de las tres (una sola tabla, mismo formato que
  `PerfilMultiTimeframe`), pero pierde visibilidad sobre el régimen en que efectivamente se
  resolvió el resultado financiero/operacional — dos operaciones con el mismo `RegimenEntrada`
  pueden haberse resuelto en regímenes completamente distintos si una usó martingala y la otra no.

**Opción B — Agrupar solo por `RegimenResolucion`.**
- Responde: "¿en qué entorno terminaron las operaciones?"
- Alineado con el resultado (`Gano`/`MartingalasUsadas` son atributos de la operación ya cerrada,
  coherente con agrupar por el régimen vigente en ese mismo instante).
- Costo: misma simplicidad que A, mismo problema simétrico — pierde visibilidad sobre bajo qué
  condición decidió actuar la estrategia.

**Opción C — Ambas dimensiones, sin combinar en una sola tabla.**
- Responde: "¿cómo cambia la relación entre contexto inicial y contexto final?" — y también
  responde A y B por separado, sin forzar una sola lectura.
- Costo real, verificado contra la estructura de datos: no es solo "dos tablas en vez de una". Para
  mantener la partición exhaustiva (obligatoria desde Fase 1.2/1.3) en **cada** dimensión por
  separado, el reporte necesita dos verificaciones de integridad independientes (suma de filas por
  `RegimenEntrada` = total, suma de filas por `RegimenResolucion` = total) — el doble de superficie
  de prueba de la sección 5. Además, introduce una pregunta adicional no trivial: si se quisiera en
  el futuro cruzar ambas dimensiones (ej. "¿cuántas operaciones entraron en Alcista y resolvieron
  en Bajista?"), esa es una tabla de contingencia 4×4 (16 celdas + categoría "sin régimen"), que
  este documento **no propone construir ahora** — se señala solo como el tipo de pregunta que
  Opción C deja abierta para una fase futura, no como parte del alcance de este Paso 3.

**Relación con D-041** (ya aprobada: "la operación no se asocia a una única vela temporal"):
D-041 estableció que hay que *conservar* ambos regímenes — eso ya está resuelto y hecho en el Paso
2 (`OperacionConRegimen` guarda ambos). D-044 es una pregunta distinta y posterior: entre lo ya
conservado, ¿cuál se usa como **eje principal de agrupación** del reporte? Elegir A o B no pierde
el dato descartado (sigue disponible en `OperacionConRegimen` para análisis futuros) — solo decide
qué tabla ve el usuario en el reporte principal de esta fase.

**No hay una opción técnicamente inválida** — a diferencia de decisiones previas donde una opción
violaba una regla ya aprobada (ej. Opción B de Fase 1.4-B sobre selección retrospectiva), aquí las
tres son consistentes con D-036/D-039/D-041/D-043. La diferencia es de alcance y de qué pregunta
queda respondida en esta fase frente a una futura.

---

## 3. Catálogo de métricas (heredado de Fase 1.2, sin cambios de fórmula)

Independientemente de qué se decida en la sección 2, el catálogo por celda de agrupación es
idéntico al ya congelado — no se define ninguna métrica nueva:

| Heredado de Fase 1.2 (§4) | Qué mide | Cambio para esta fase |
|---|---|---|
| §4.1 Resultado general / Eficiencia operacional | `Ganadas / Completadas * 100` | Ninguno — mismo cálculo, agrupado por régimen en vez de por corrida completa |
| §4.2 Resolución de intentos | Victoria inicial / M1 / M2 / Pérdida agotando | Ninguno |
| §4.3 Dependencia de martingala | Solo porcentaje, sin clasificación cualitativa (D-005 sigue sin resolver, no se reabre aquí) | Ninguno |
| §4.6 Peores escenarios | Mayor racha negativa, exposición máxima | **Requiere aclaración** — ver sección 4 |

**Tamaño de muestra obligatorio** (D-010, Fase 1.3, extendido aquí): cada fila de régimen debe
mostrar la cantidad de operaciones que la sustentan, igual que ya es obligatorio por timeframe. Es
más crítico todavía en este análisis — Fase 1.3 mostró que 1D tiene solo 61 operaciones; algunas
combinaciones régimen×timeframe pueden tener muestras aún menores (ej. un timeframe con pocas
operaciones total, cuyo régimen Ambiguo cubre solo 2 o 3 de ellas).

---

## 4. Advertencia sobre "Peores escenarios" al agrupar por régimen

`RachaNegativaMaxima`/`MaxExposicion` (Fase 1.2, §4.6) se calculan hoy sobre la secuencia completa
de una corrida (`PerfilMultiTf.Medir`, recorre `operaciones` en orden). Al agrupar por régimen, una
racha negativa **no respeta los límites de un régimen** — una racha de 4 pérdidas consecutivas
puede empezar en un tramo Lateral y terminar en un tramo Ambiguo. Recalcular "racha negativa dentro
de un régimen" (es decir, solo contar pérdidas consecutivas *entre* operaciones que comparten el
mismo régimen, ignorando las intercaladas de otro régimen) sería una fórmula nueva, no heredada —
fuera del alcance de "solo agrupar, no recalcular" (D-015).

**Tratamiento propuesto para esta fase**: la sección "Peores escenarios" del reporte por régimen
(§4.6 heredada) queda **excluida** del catálogo agrupado por régimen en esta versión — se mantiene
disponible únicamente en su forma original (Fase 1.2, sobre la corrida completa, sin segmentar).
Documentarlo así evita construir una métrica nueva sin aprobación explícita. Si se requiere en el
futuro, sería una decisión y una fórmula nuevas, con su propio documento.

---

## 5. Verificación de integridad (partición exhaustiva)

Heredada de Fase 1.2/1.3, extendida con la categoría nueva de este análisis:

```
Σ (operaciones por régimen, incluyendo "Sin régimen") == OperacionesCompletadas de la corrida
```

Aplicada sobre la dimensión elegida en D-044 (o sobre ambas dimensiones por separado, si se aprueba
Opción C — sección 2). "Sin régimen" (Paso 2, `Escenario? = null`) es una categoría más de la
partición, no se excluye del conteo ni se mezcla con "Ambiguo".

---

## 6. Presentación — reglas heredadas, sin novedad

Estas reglas ya fueron fijadas en `ESPECIFICACION_REPORTE_ESCENARIOS_MERCADO_V1.md §5/§7` y no se
reabren aquí, solo se listan como restricción vigente sobre el catálogo de la sección 3:

- Sin ranking financiero entre regímenes (D-014/D-009).
- Ambiguo se presenta como fila etiquetada, no oculta, con nota de "evidencia insuficiente" (§5 de
  la especificación de reporte).
- Nota obligatoria de correlación ≠ causalidad en cualquier tabla de esta sección (D-037).
- "Sin régimen" (Paso 2) se presenta igual de explícito que Ambiguo — no se combinan ni se ocultan
  entre sí; son dos categorías distintas (Paso 2, sección 3: ausencia de dato vs. estado calculado).

---

## Fuera de alcance (respetado)

No se implementa código en este documento. No se calcula ningún porcentaje, eficiencia operacional,
resolución de intentos ni comparación entre regímenes. No se selecciona la dimensión de agrupación
(D-044, sección 2) — pendiente de decisión explícita. No se define la fórmula de "racha negativa
por régimen" (sección 4) — queda fuera de esta versión, disponible solo sin segmentar.

---

## Pregunta pendiente para decisión del auditor

**D-044 — ¿Cuál es la dimensión principal de agrupación del reporte por régimen: `RegimenEntrada`
(Opción A), `RegimenResolucion` (Opción B), o ambas por separado (Opción C)?** — ver sección 2 para
el análisis de costo/beneficio de cada una, incluyendo el costo real verificado de Opción C (doble
verificación de integridad, pregunta de cruce 4×4 explícitamente fuera de alcance si se elige).

---

## Criterio de cierre de Fase 1.5-A, Paso 3 (diseño)

- ✓ Objetivo definido: agrupar `OperacionConRegimen` reutilizando el catálogo ya congelado de Fase
  1.2, mismo patrón estructural que Fase 1.3 (sección 1).
- ⏳ D-044 presentada con las tres opciones y su costo real, no seleccionada en este documento
  (sección 2).
- ✓ Catálogo de métricas heredado identificado, sin fórmulas nuevas (sección 3), con tamaño de
  muestra obligatorio (extensión de D-010).
- ✓ Caso límite de "Peores escenarios" (racha negativa no respeta límites de régimen) identificado
  y resuelto por exclusión explícita, no por invención de fórmula nueva (sección 4).
- ✓ Verificación de integridad extendida con la categoría "Sin régimen" (sección 5).
- ✓ Reglas de presentación heredadas listadas sin reabrir su discusión (sección 6).
- ⏳ Auditoría resuelve D-044 y aprueba la especificación — pendiente de confirmación explícita
  antes de iniciar código.
