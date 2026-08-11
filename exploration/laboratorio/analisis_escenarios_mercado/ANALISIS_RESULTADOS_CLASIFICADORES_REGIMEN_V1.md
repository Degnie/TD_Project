# Análisis de Resultados — Clasificadores de Régimen Candidatos V1

Estado: **análisis previo a selección — Fase 1.4-A**. Responde exclusivamente las 5 preguntas
pedidas por auditoría (estabilidad, cobertura, distribución, interpretabilidad, riesgos). **No
responde "cuál gana"** — la selección del clasificador oficial es una decisión separada y posterior
a este documento (D-023). Evidencia fuente: `RESULTADO_EVALUACION_CLASIFICADORES_REGIMEN_V1.md`,
generado por `Program.cs` sobre el dataset real `BTCUSDT_2024-01-02_2025-01-02`, 6 timeframes, 18
combinaciones, determinismo confirmado en todas.

---

## 1. ¿Qué tan estable es cada candidato?

Medido como `% de cambios de régimen entre ventanas consecutivas` (§3.1) y su amplitud entre
timeframes (D-024, criterio obligatorio: la variación entre timeframes es en sí misma parte del
análisis de estabilidad).

| Candidato | Cambios de régimen (rango 1m→1D) | Amplitud entre timeframes |
|---|---|---|
| A — EMA | 0.00% – 20.00% | **20.00pp** |
| B — ADX+DI | 4.55% – 5.81% | 1.26pp |
| C — Retorno+Volatilidad | 61.11% – 69.85% | 8.74pp |

**Lectura**: A es el más estable dentro de cada timeframe corto (0.00%-0.30% en 1m-1h) pero el
menos estable *entre* timeframes — su comportamiento en 1D (20.00%) no guarda relación con su
comportamiento en 1m (0.00%). B es el más estable en ambos sentidos: bajo y con poca variación
entre escalas. C tiene el % de cambios más alto en términos absolutos en todos los timeframes, pero
es también el más consistente en su propio nivel de "alta variabilidad" (8.74pp de amplitud, menor
que A).

**No se concluye** cuál de estas tres formas de estabilidad es preferible — eso depende de qué se
espere de un clasificador de régimen, decisión que no corresponde a este documento.

---

## 2. ¿Qué cobertura genera?

Medido como `% de ventanas clasificadas como Alcista/Bajista/Lateral (excluye Ambiguo)` (§3.2).

| Candidato | Cobertura (rango 1m→1D) | Amplitud |
|---|---|---|
| A — EMA | 100.00% – 100.00% | 0.00pp |
| B — ADX+DI | 100.00% – 100.00% | 0.00pp |
| C — Retorno+Volatilidad | 81.00% – 87.67% | 6.67pp |

**Lectura literal (cantidad)**: A y B cubren el 100% del dataset en los 6 timeframes — nunca dejan
una ventana sin clasificar. C cubre entre 81% y 88%, porque es el único candidato con categoría
"Ambiguo" explícita en esta implementación exploratoria (ver sección 3, riesgo de A).

**Advertencia obligatoria**: esta cifra de cobertura, por sí sola, **no es comparable entre los tres
candidatos** sin la sección 3 (distribución por régimen) — un candidato puede cubrir el 100% del
dataset y aun así tener una utilidad analítica baja si casi toda esa cobertura cae en una sola
categoría. Ver el hallazgo central de la siguiente sección.

---

## 3. ¿Cómo distribuye los regímenes?

Medido como `%` de ventanas en cada categoría (Alcista/Bajista/Lateral/Ambiguo), más duración media
del tramo (en ventanas) y cantidad de tramos de longitud 1 (fragmentación).

| Candidato | TF | Alcista % | Bajista % | Lateral % | Ambiguo % | Duración media | Tramos=1 |
|---|---|---|---|---|---|---|---|
| A — EMA | 1m | 0.00% | 0.00% | **100.00%** | 0.00% | 105,404.00 | 2 |
| A — EMA | 5m | 0.00% | 0.00% | **100.00%** | 0.00% | 11,709.78 | 4 |
| A — EMA | 15m | 0.00% | 0.04% | **99.96%** | 0.00% | 2,065.65 | 5 |
| A — EMA | 1h | 0.16% | 0.22% | **99.62%** | 0.00% | 324.59 | 7 |
| A — EMA | 4h | 2.94% | 1.61% | **95.45%** | 0.00% | 25.60 | 25 |
| A — EMA | 1D | 31.50% | 9.25% | 59.25% | 0.00% | 4.94 | 20 |
| B — ADX+DI | 1m | 20.71% | 21.92% | 57.36% | 0.00% | 19.29 | 2,771 |
| B — ADX+DI | 5m | 19.39% | 21.12% | 59.49% | 0.00% | 21.95 | 466 |
| B — ADX+DI | 15m | 21.11% | 23.89% | 55.01% | 0.00% | 20.97 | 169 |
| B — ADX+DI | 1h | 25.23% | 26.21% | 48.57% | 0.00% | 19.42 | 46 |
| B — ADX+DI | 4h | 30.43% | 27.48% | 42.09% | 0.00% | 17.08 | 16 |
| B — ADX+DI | 1D | 27.73% | 19.76% | 52.51% | 0.00% | 17.84 | 1 |
| C — Retorno+Volatilidad | 1m | 40.50% | 32.50% | 10.50% | 16.50% | 1.43 | 101 |
| C — Retorno+Volatilidad | 5m | 40.50% | 32.50% | 10.50% | 16.50% | 1.43 | 101 |
| C — Retorno+Volatilidad | 15m | 39.50% | 30.00% | 11.50% | 19.00% | 1.45 | 95 |
| C — Retorno+Volatilidad | 1h | 41.67% | 33.82% | 6.86% | 17.65% | 1.46 | 99 |
| C — Retorno+Volatilidad | 4h | 42.47% | 32.88% | 11.42% | 13.24% | 1.48 | 103 |
| C — Retorno+Volatilidad | 1D | 52.05% | 35.62% | 0.00% | 12.33% | 1.62 | 30 |

**Hallazgo central (el que motivó la revisión de auditoría, ahora cuantificado)**: el candidato A
clasifica el dataset como **prácticamente 100% Lateral en 1m, 5m, 15m y 1h** (99.62%-100.00%), con
una duración media de tramo de más de 100,000 ventanas en 1m — es decir, en la práctica **no
detecta ningún cambio de régimen** en esas escalas con la configuración exploratoria usada. Solo en
4h y especialmente 1D empieza a producir una distribución con presencia real de Alcista/Bajista
(31.50%/9.25% en 1D). Esto es exactamente el caso "95% lateral, gran estabilidad, poca utilidad
analítica" señalado como ejemplo por auditoría — y aquí no es hipotético, es el resultado medido en
1m-1h con A.

**Candidato B**: distribución más balanceada y estable entre timeframes — Lateral entre 42% y 59%,
Alcista/Bajista repartidos de forma razonablemente simétrica en todos los timeframes evaluados.
Fragmentación alta en 1m (2,771 tramos de una sola ventana) que decrece fuertemente al subir de
escala (46 en 1h, 1 en 1D).

**Candidato C**: distribución más estable proporcionalmente entre timeframes (Alcista ~40-52%,
Bajista ~30-36% en todos), con una categoría Ambiguo consistente (12-19%). Duración media de tramo
muy baja (1.4-1.6 ventanas) — cada "ventana" ya es un tramo grande de velas (por diseño, la ventana
exploratoria es de ~1/200 del dataset), así que esto no es directamente comparable con A/B sin
ajustar por el tamaño de la unidad de muestreo (ver "Riesgos", punto 3).

---

## 4. ¿Qué tan interpretable es?

Trasladado sin cambios desde `RESULTADO_EVALUACION_CLASIFICADORES_REGIMEN_V1.md §3` (evaluación
cualitativa, no numérica, ya registrada):

- **A — EMA**: interpretabilidad alta en el sentido conceptual ("el precio subió/bajó de forma
  sostenida"), pero la sección 3 de este documento muestra que esa interpretabilidad conceptual no
  se traduce en utilidad práctica en timeframes cortos — un usuario podría entender la regla y aun
  así no poder usarla, porque casi nunca se activa.
- **B — ADX+DI**: interpretabilidad media-baja (requiere explicar qué mide un índice direccional
  promediado), pero su distribución (sección 3) es la más práctica de leer de los tres.
- **C — Retorno+Volatilidad**: interpretabilidad alta ("el precio subió X% con un rango de Y%"),
  con la salvedad de que la categoría "Ambiguo" requiere explicar también qué significa un caso sin
  tendencia clara pero con alta dispersión (sección 5 de la especificación).

---

## 5. ¿Qué riesgos presenta cada candidato?

1. **A — EMA**: riesgo confirmado, no solo teórico — con la configuración exploratoria usada, es
   prácticamente inútil como clasificador en 1m-1h (99.6%+ Lateral). No se sabe todavía si esto es
   una propiedad estructural del enfoque EMA o un artefacto del umbral exploratorio (0.5%) — no se
   ajusta ese umbral en este documento (sería exactamente el error que D-018/D-022 prohíben:
   ajustar parámetros mirando el resultado). El riesgo de dependencia de timeframe, señalado antes
   de correr el experimento, queda confirmado cuantitativamente.
2. **B — ADX+DI**: riesgo de fragmentación en timeframes cortos (2,771 tramos de 1 ventana en 1m) —
   posible ruido de alta frecuencia en el indicador a esa escala, aunque se estabiliza fuertemente
   en timeframes largos. Requiere ventana de calentamiento mayor (2×periodo) que reduce las
   ventanas evaluables frente a A y C.
3. **C — Retorno+Volatilidad**: el tamaño de la ventana exploratoria (~1/200 del dataset por
   timeframe) hace que "duración media" no sea comparable directamente contra A/B sin normalizar por
   el tamaño de la unidad de muestreo — este documento no resuelve esa normalización, la deja
   como problema abierto para el análisis de selección. La categoría Ambiguo es la única de los
   tres candidatos con este mecanismo, lo cual complica comparar "cobertura" en un pie de igualdad
   con A/B (sección 2).

---

## Decisiones registradas por auditoría (2026-08-11)

**D-025 — Un clasificador que no discrimina regímenes no debe evaluarse únicamente por
estabilidad**: ✅ Aprobado. Extiende la lectura del hallazgo de la sección 1: la estabilidad
extrema de A en 1m-1h (0.00%-0.30% de cambios de régimen) no es una virtud aislada — es
consecuencia directa de que casi todo el dataset cae en una sola categoría (Lateral, sección 3), lo
cual no cumple la finalidad analítica de un clasificador de régimen. La evaluación de un
clasificador debe considerar conjuntamente estabilidad, cobertura, distribución y capacidad
discriminativa — nunca una dimensión aislada de las demás.

**D-026 — Las métricas dependientes de escala deben normalizarse antes de comparar clasificadores
multi-timeframe**: ✅ Aprobado. Confirma el riesgo ya señalado en la sección 5, punto 3, sobre C:
antes de usar duración media, fragmentación o frecuencia de cambios como criterio comparativo entre
timeframes, debe definirse una unidad común (ej. tiempo real transcurrido, % del periodo evaluado,
horas/días) — no se implementa en este documento ni en Fase 1.4-A.

---

## Preguntas explícitamente no respondidas (fuera de alcance de este documento)

- Cuál candidato es "el mejor" o "el más adecuado" (D-023, decisión posterior).
- Si el hallazgo de A es corregible ajustando su umbral exploratorio, o si es una limitación
  estructural del enfoque EMA — requeriría una nueva corrida con otro valor de umbral, lo cual
  sería en sí mismo una decisión de diseño (no de análisis) y no se ejecuta aquí sin aprobación
  explícita.
- Cómo normalizar "duración media" entre candidatos con tamaños de ventana de muestreo distintos.
- O-005 (sensibilidad del clasificador ante cambios pequeños de parámetro) — sigue como mejora
  futura, no evaluada en este documento.
