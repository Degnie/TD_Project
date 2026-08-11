# Resultado — Evaluación de Clasificadores de Régimen Candidatos V1

Estado: **evidencia experimental — Fase 1.4-A, Paso 4**. Presenta evidencia para una
decisión posterior de selección; no concluye cuál candidato es "el mejor". Ninguna
estrategia fue ejecutada para producir este informe (D-016/D-021). Todos los parámetros
usados son **Configuración exploratoria** (D-022), no oficial — ver cada archivo de
clasificador para el detalle de los valores usados.

Dataset: `BTCUSDT_2024-01-02_2025-01-02` (BTC/USDT Spot, Binance, hash verificado en Fase 1.0/baseline).
Timeframes evaluados: 1m, 5m, 15m, 1h, 4h, 1D.

---

## 1. Estabilidad temporal y cobertura, por candidato y timeframe

*(§3.1 — % de cambios de régimen entre ventanas consecutivas; §3.2 — % de ventanas
clasificadas como Alcista/Bajista/Lateral, excluyendo Ambiguo.)*

| Candidato | TF | Cobertura % | Cambios de régimen % | Ventanas | Determinista |
|---|---|---|---|---|---|
| C — Retorno+Volatilidad | 1m | 83.50% | 69.85% | 200 | sí |
| A — EMA | 1m | 100.00% | 0.00% | 527020 | sí |
| B — ADX+DI | 1m | 100.00% | 5.18% | 527013 | sí |
| C — Retorno+Volatilidad | 5m | 83.50% | 69.85% | 200 | sí |
| A — EMA | 5m | 100.00% | 0.01% | 105388 | sí |
| B — ADX+DI | 5m | 100.00% | 4.55% | 105381 | sí |
| C — Retorno+Volatilidad | 15m | 81.00% | 68.84% | 200 | sí |
| A — EMA | 15m | 100.00% | 0.05% | 35116 | sí |
| B — ADX+DI | 15m | 100.00% | 4.77% | 35109 | sí |
| C — Retorno+Volatilidad | 1h | 82.35% | 68.47% | 204 | sí |
| A — EMA | 1h | 100.00% | 0.30% | 8764 | sí |
| B — ADX+DI | 1h | 100.00% | 5.14% | 8757 | sí |
| C — Retorno+Volatilidad | 4h | 86.76% | 67.43% | 219 | sí |
| A — EMA | 4h | 100.00% | 3.86% | 2176 | sí |
| B — ADX+DI | 4h | 100.00% | 5.81% | 2169 | sí |
| C — Retorno+Volatilidad | 1D | 87.67% | 61.11% | 73 | sí |
| A — EMA | 1D | 100.00% | 20.00% | 346 | sí |
| B — ADX+DI | 1D | 100.00% | 5.33% | 339 | sí |

---

## 1bis. Distribución por régimen y duración (revisión pendiente §1/§2 de auditoría)

*(% de ventanas por categoría; duración media = tramo medio, en ventanas, antes de que el
candidato cambie de escenario; "Tramos=1" = cantidad de tramos que duran exactamente 1
ventana, posible indicador de fragmentación/ruido si es una proporción alta del total de
tramos.)*

| Candidato | TF | Alcista % | Bajista % | Lateral % | Ambiguo % | Duración media (ventanas) | Tramos de 1 ventana |
|---|---|---|---|---|---|---|---|
| C — Retorno+Volatilidad | 1m | 40.50% | 32.50% | 10.50% | 16.50% | 1.43 | 101 |
| A — EMA | 1m | 0.00% | 0.00% | 100.00% | 0.00% | 105404.00 | 2 |
| B — ADX+DI | 1m | 20.71% | 21.92% | 57.36% | 0.00% | 19.29 | 2771 |
| C — Retorno+Volatilidad | 5m | 40.50% | 32.50% | 10.50% | 16.50% | 1.43 | 101 |
| A — EMA | 5m | 0.00% | 0.00% | 100.00% | 0.00% | 11709.78 | 4 |
| B — ADX+DI | 5m | 19.39% | 21.12% | 59.49% | 0.00% | 21.95 | 466 |
| C — Retorno+Volatilidad | 15m | 39.50% | 30.00% | 11.50% | 19.00% | 1.45 | 95 |
| A — EMA | 15m | 0.00% | 0.04% | 99.96% | 0.00% | 2065.65 | 5 |
| B — ADX+DI | 15m | 21.11% | 23.89% | 55.01% | 0.00% | 20.97 | 169 |
| C — Retorno+Volatilidad | 1h | 41.67% | 33.82% | 6.86% | 17.65% | 1.46 | 99 |
| A — EMA | 1h | 0.16% | 0.22% | 99.62% | 0.00% | 324.59 | 7 |
| B — ADX+DI | 1h | 25.23% | 26.21% | 48.57% | 0.00% | 19.42 | 46 |
| C — Retorno+Volatilidad | 4h | 42.47% | 32.88% | 11.42% | 13.24% | 1.48 | 103 |
| A — EMA | 4h | 2.94% | 1.61% | 95.45% | 0.00% | 25.60 | 25 |
| B — ADX+DI | 4h | 30.43% | 27.48% | 42.09% | 0.00% | 17.08 | 16 |
| C — Retorno+Volatilidad | 1D | 52.05% | 35.62% | 0.00% | 12.33% | 1.62 | 30 |
| A — EMA | 1D | 31.50% | 9.25% | 59.25% | 0.00% | 4.94 | 20 |
| B — ADX+DI | 1D | 27.73% | 19.76% | 52.51% | 0.00% | 17.84 | 1 |

---

## 2. Consistencia multi-timeframe (§3.3)

Por candidato, rango de cobertura y de cambios de régimen a través de los timeframes
evaluados — mismo formato mínimo/máximo/amplitud ya usado en Fase 1.3 (D-014), sin
clasificación cualitativa.

| Candidato | Cobertura mín-máx | Amplitud cobertura | Cambios régimen mín-máx | Amplitud cambios |
|---|---|---|---|---|
| C — Retorno+Volatilidad | 81.00% – 87.67% | 6.67pp | 61.11% – 69.85% | 8.74pp |
| A — EMA | 100.00% – 100.00% | 0.00pp | 0.00% – 20.00% | 20.00pp |
| B — ADX+DI | 100.00% – 100.00% | 0.00pp | 4.55% – 5.81% | 1.26pp |

---

## 3. Explicabilidad (§3.4 — descriptivo, no numérico)

- **A — EMA**: alta. "El precio subió/bajó de forma sostenida según el promedio móvil" es
  comprensible sin formación técnica previa.
- **B — ADX+DI**: media-baja. Requiere explicar qué mide un índice direccional promediado;
  el motivo de una clasificación no es legible directamente del precio.
- **C — Retorno+Volatilidad**: alta. "El precio subió X% en esta ventana, con un rango de
  Y%" es una frase directa sobre el precio observado, sin indicador intermedio.

---

## 4. Reproducibilidad (§3.5)

Los 3 candidatos fueron ejecutados dos veces sobre la misma entrada por cada timeframe y
comparados campo a campo (inicio, fin, escenario de cada ventana). Resultado:
**determinismo confirmado en las 18 combinaciones (3 candidatos × 6 timeframes)**.

---

## 5. Evidencia, no conclusión

Este documento presenta las 5 dimensiones por separado, sin puntaje único combinado
(EVALUACION_CLASIFICADORES_REGIMEN_V1.md §5). La selección del candidato oficial es una
decisión posterior, no automática ni derivada de este informe.

Observaciones objetivas para esa decisión futura (sin declarar ganador):
- El candidato A (EMA) no distingue "Ambiguo" de "Lateral" en esta implementación
  exploratoria — su cobertura reportada no es comparable 1:1 con B/C en ese sentido.
- El candidato B (ADX+DI) requiere una ventana de calentamiento mayor (2×periodo) antes de
  producir su primera clasificación, reduciendo el número de ventanas evaluables frente a
  A y C en el mismo dataset.
- El candidato C (Retorno+Volatilidad) es el único con categoría "Ambiguo" explícita en
  esta implementación exploratoria, consistente con el diseño de
  ESPECIFICACION_ANALISIS_ESCENARIOS_MERCADO_V1.md §5.
