# Evaluación de Clasificadores de Régimen Candidatos V1

Estado: **especificación de subfase — Fase 1.4-A del Caso 1**. Documento de diseño de la
metodología de comparación, no implementación de ningún clasificador todavía. No se calculan ADX,
EMA ni fórmulas concretas en esta fase — se define **cómo** se comparará cada candidato una vez
implementado, no el resultado de esa comparación.

Subfase de `ESPECIFICACION_ANALISIS_ESCENARIOS_MERCADO_V1.md` (Fase 1.4), abierta por decisión de
auditoría D-021: la selección de familia de clasificador no se hace por elección directa, sino por
comparación experimental de candidatos antes de congelar uno como oficial.

---

## 1. Objetivo

Determinar qué familia de clasificador de régimen de mercado será congelada como oficial para Fase
1.4, comparando **propiedades del clasificador como instrumento de medición** — nunca comparando
qué candidato produce mejores resultados para ninguna estrategia conocida.

```
Dataset
    ↓
Clasificador candidato (A, B, C — sin ejecutar ninguna estrategia)
    ↓
Segmentación
    ↓
Auditoría del clasificador (esta fase)
    ↓
Selección y congelamiento de UN clasificador oficial
    ↓
(Fase 1.4, continuación) Evaluación de estrategias dentro de los segmentos ya congelados
```

**Restricción obligatoria, heredada de D-021**: durante esta fase **no se ejecuta ninguna
estrategia**. Ningún dato de `InfoOperacionResuelta`, `PerfilMultiTf` ni `ReporteOperacional` entra
a esta evaluación — la comparación de candidatos es ciega respecto a cualquier resultado de
estrategia, exactamente la misma garantía estructural que D-016 exige del clasificador final.

---

## 2. Candidatos evaluados

| Candidato | Enfoque | Estado (auditoría, 2026-08-11) |
|---|---|---|
| **A — Medias móviles (EMA)** | Pendiente de una EMA, o posición relativa EMA corta/larga | 🟡 Candidato secundario — riesgo señalado: fuerte dependencia del timeframe (una EMA de N periodos representa una ventana temporal muy distinta en 1m que en 1D) |
| **B — Fuerza direccional (ADX + DI)** | Separa explícitamente "hay tendencia" de "no hay tendencia"; umbrales convencionales en literatura técnica (~25 para ADX) | 🟢 Candidato prioritario — ayuda a justificar el umbral sin mirar el resultado de BTC/USDT (D-018), pero mayor complejidad de implementación/explicación |
| **C — Retorno y volatilidad** | Estadístico puro: `PendienteNormalizada` + `RangoRelativo` sobre una ventana, ya esbozado en `ESPECIFICACION_ANALISIS_ESCENARIOS_MERCADO_V1.md §4` | 🟢 Candidato prioritario — menor superficie de implementación, fórmula ya escrita en el documento aprobado, pero menos intuitivo para un usuario que espera "tendencia" en sentido de trading clásico |

No se implementa ninguno de los tres en esta fase — la tabla es el inventario de qué se va a
comparar, no el resultado de la comparación.

---

## 3. Criterios de evaluación

**Regla central de esta sección**: se evalúan **características del clasificador como
instrumento**, nunca "cuál funciona mejor para Tres Mosqueteros" o cualquier otra estrategia. Cada
criterio se aplica igual a los 3 candidatos, sobre el mismo dataset congelado
(`BTCUSDT_2024-01-02_2025-01-02`, cualquier timeframe disponible: 1m, 2m, 5m, 10m, 15m, 30m, 1h,
2h, 4h, 8h, 12h, 1D, 1W — los 13 ya congelados en Fase 1.0/baseline).

### 3.1 Estabilidad temporal

**Pregunta**: ¿produce cambios excesivos de régimen?

Un clasificador que oscila entre Alcista/Bajista/Lateral vela a vela (o ventana a ventana) es
inestable — no describe un "régimen" sostenido, describe ruido de alta frecuencia. Métrica
propuesta para medir esto (sin fijar todavía el umbral de "excesivo"): frecuencia de cambio de
categoría por cada N velas evaluadas, para el mismo candidato aplicado en distintos timeframes.

### 3.2 Cobertura

**Pregunta**: ¿qué porcentaje del dataset queda clasificado?

Un clasificador que deja gran parte del dataset en "Ambiguo"/"Indeterminado" (D-020) tiene baja
utilidad práctica, incluso si es técnicamente correcto. Métrica: `% de velas clasificadas como
Alcista/Bajista/Lateral` vs. `% en Ambiguo/Indeterminado`, por candidato y por timeframe.

### 3.3 Consistencia multi-timeframe

**Pregunta**: ¿el comportamiento cambia demasiado al cambiar timeframe?

Reutiliza directamente la infraestructura ya construida en Fase 1.3 (`ComparadorMultiTimeframe`,
sección 4 de `ESPECIFICACION_ANALISIS_MULTITIMEFRAME_V1.md`: mínimo/máximo/amplitud, sin
clasificación cualitativa) — aplicada aquí no a métricas operacionales de una estrategia, sino a
`% de cobertura` o `frecuencia de cambio de régimen` del propio clasificador, medidos en cada uno
de los 13 timeframes ya congelados. Un candidato cuya clasificación en 1m no guarda relación alguna
con su clasificación en 1D sobre el mismo periodo de calendario es sospechoso de sobreajuste al
ruido de alta frecuencia.

### 3.4 Explicabilidad

**Pregunta**: ¿un usuario puede entender por qué una zona fue clasificada?

Criterio cualitativo, no numérico: para cada candidato, ¿el motivo de una clasificación puede
enunciarse en una frase comprensible sin conocimiento técnico previo? (ej. "el precio subió X% en
esta ventana" es más explicable que "el ADX cruzó 25 con +DI sobre -DI" para un usuario sin
formación técnica). Se documenta como evaluación descriptiva por candidato, no como score.

### 3.5 Reproducibilidad

**Pregunta**: ¿otro auditor puede obtener la misma clasificación?

Mismo estándar que cualquier resultado del laboratorio desde Fase 1.0 (determinismo, hash,
trazabilidad): dado el mismo dataset y la misma versión de parámetros (aunque los parámetros
todavía no estén fijados, D-018/D-019), ¿el candidato produce una clasificación 100% determinista?
Se verifica con el mismo método ya usado en Fase 1.0 — múltiples corridas idénticas, comparación
byte a byte de la salida.

---

## 4. Qué NO se evalúa en esta fase

- **Rendimiento de ninguna estrategia dentro de los segmentos** — prohibido explícitamente
  (sección 1). Evaluar esto aquí sería la selección retrospectiva que Fase 1.4 existe para
  prevenir.
- **Umbrales numéricos finales de ningún candidato** (D-018, D-019 siguen pendientes) — la
  comparación de esta fase puede operar con valores de referencia provisionales solo para poder
  ejecutar los 3 candidatos y comparar sus propiedades (secciones 3.1-3.3), pero ningún valor
  provisional usado aquí se congela como definitivo sin una decisión explícita posterior.
- **Cuál candidato "predice mejor" el mercado** — el clasificador no es un sistema de predicción,
  es una herramienta de segmentación descriptiva; "acierto" no es un criterio de esta evaluación.

**D-022 — Parámetros exploratorios no son parámetros oficiales** (auditoría, 2026-08-11): ✅
Aprobado. Todo parámetro usado durante esta fase (umbral de ADX, periodo de EMA, tamaño de ventana
de retorno/volatilidad, etc.) debe quedar explícitamente etiquetado en el código y en el informe
como **"Configuración exploratoria"**, nunca como "Configuración oficial". No se permite concluir
en esta fase enunciados como "ADX=25 es el correcto" o "EMA 50 es la correcta" — eso pertenece a una
fase de congelamiento independiente y posterior a la selección del candidato (D-018/D-019 siguen
pendientes después de esta subfase, no se resuelven aquí).

**O-005 — Sensibilidad del clasificador** (observación, no bloqueante): a evaluar en una fase
futura si un pequeño cambio de parámetros (ej. ADX 24 vs. ADX 26) produce una clasificación
completamente distinta. Un clasificador demasiado sensible a su parametrización es frágil incluso si
sus 5 criterios de la sección 3 salen bien en la configuración exploratoria usada. Clasificado como
mejora futura, no bloquea el cierre de Fase 1.4-A.

---

## 5. Entregable de esta fase

Un reporte comparativo (no implementado en este documento) que muestre, para los 3 candidatos, los
5 criterios de la sección 3 lado a lado — mismo principio de "mostrar tres dimensiones separadas sin
fusionarlas en un ranking" ya establecido en D-014 (Fase 1.3), aplicado aquí a la comparación de
clasificadores en vez de a la comparación de timeframes:

```
Candidato   Estabilidad   Cobertura   Consist.MultiTF   Explicabilidad   Reproducibilidad
A (EMA)     ...           ...         ...               (descriptivo)    ...
B (ADX+DI)  ...           ...         ...               (descriptivo)    ...
C (Ret+Vol) ...           ...         ...               (descriptivo)    ...
```

**No se calcula un puntaje único combinado.** La selección final (fuera del alcance de este
documento — pertenece a la decisión de cierre de Fase 1.4-A) se toma revisando las 5 dimensiones
por separado, con criterio explícito documentado de por qué se prioriza una sobre otra si hay
conflicto entre candidatos — no por un promedio ponderado que oculte el trade-off.

---

## Fuera de alcance (respetado)

No se implementa ningún clasificador (A, B ni C) en esta fase. No se ejecuta ninguna estrategia. No
se fijan umbrales definitivos. No se selecciona todavía el candidato oficial — este documento define
la metodología de comparación, no su resultado.

---

## Criterio de cierre de Fase 1.4-A (metodología)

- ✓ Candidatos inventariados con su estado de auditoría (sección 2).
- ✓ 5 criterios de evaluación definidos, todos sobre propiedades del clasificador, ninguno sobre
  resultado de estrategia (sección 3).
- ✓ Restricción explícita de qué no se evalúa en esta fase, para prevenir selección retrospectiva
  por la puerta trasera (sección 4).
- ✓ Formato de entregable definido sin puntaje único combinado, mismo principio que D-014 (sección
  5).
- ⏳ Auditoría aprueba esta metodología — pendiente de confirmación explícita antes de implementar
  los 3 candidatos y ejecutar la comparación real.
