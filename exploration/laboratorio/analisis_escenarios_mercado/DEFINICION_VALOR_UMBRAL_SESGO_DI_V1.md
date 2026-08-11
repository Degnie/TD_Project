# Definición del Valor de UmbralSesgoDI V1

Estado: **especificación de subfase — Fase 1.4-B, Paso 3-A (segunda parte)**. Resuelve el
procedimiento para llegar a un valor numérico de `UmbralSesgoDI` sobre la fórmula ya aprobada por
D-031 (`|DI+ - DI-| / (DI+ + DI-)`), sin fijar el número en este documento a menos que el
procedimiento mismo lo produzca de forma verificable y no ajustada a BTC/USDT.

---

## 1. Cómo elegir el valor sin mirar resultados de estrategias

**Restricción heredada, sin excepción**: ninguna estrategia, `InfoOperacionResuelta`,
`PerfilMultiTf` ni `ReporteOperacional` participa en este procedimiento (D-016). El valor de
`UmbralSesgoDI` se deriva exclusivamente de la distribución de `|DI+-DI-|/(DI+ + DI-)` observada en
el dataset OHLC, nunca de "qué umbral hace que una estrategia se vea mejor en el segmento Ambiguo".

**Rango natural de la fórmula**: por construcción, `|DI+-DI-|/(DI+ + DI-)` está siempre entre 0 (DI+
y DI- exactamente iguales) y 1 (uno de los dos es cero). Esto ya acota el problema: no se trata de
elegir un número arbitrario, sino un punto de corte dentro de un rango conocido y normalizado — a
diferencia del umbral de tendencia ADX (0-100, sin cota superior natural tan directa en su
interpretación), la fórmula relativa ya trae consigo una escala interpretable.

**Procedimiento propuesto** (ejecutable, no una nueva ronda de opciones abiertas):

1. Calcular la serie `|DI+-DI-|/(DI+ + DI-)` para las ventanas donde `ADX < 25` (zona de "no hay
   tendencia" — sección 2.5 de `PARAMETRIZACION_CLASIFICADOR_REGIMEN_V1.md`), sobre el dataset
   real ya congelado (`BTCUSDT_2024-01-02_2025-01-02`), en los 6 timeframes ya usados en Fase 1.4-A
   para mantener comparabilidad con la evidencia existente.
2. Tomar la **mediana** de esa serie como valor de `UmbralSesgoDI`, no un percentil elegido a mano
   para producir una distribución "razonable" — la mediana es el punto que divide la zona de "sin
   tendencia" en dos mitades iguales por construcción estadística, no por ajuste discrecional. Esto
   corresponde a la Opción C (adaptativo) de `DEFINICION_UMBRAL_SESGO_DI_V1.md §3`, con la
   salvaguarda de la sección 4 de ese mismo documento: la regla ("mediana, siempre, sin
   excepciones") se fija aquí, antes de calcularla, y se aplica igual sin importar qué valor
   resulte — no se elige el estadístico (media, mediana, percentil 60, etc.) después de ver qué
   número da cada uno.
3. El valor resultante se calcula **una sola vez**, se documenta con el número exacto obtenido y el
   procedimiento que lo produjo, y no se recalcula buscando "un número más limpio" o "más redondo".

**Por qué mediana y no percentil arbitrario**: elegir "percentil 60" o "percentil 73" sin
justificación sería indistinguible de elegir un número a mano — la mediana (percentil 50) es la
única elección dentro de esa familia que no requiere una justificación adicional de *por qué ese
percentil y no otro*, porque es el punto de corte estadísticamente neutro por definición.

---

## 2. Naturaleza del valor: fijo, basado en distribución histórica, o adaptativo

**Respuesta**: **basado en distribución histórica, congelado como valor fijo tras calcularse una
vez**. No es "adaptativo" en el sentido de recalcularse cada vez que se ejecuta el clasificador
(eso variaría el resultado entre corridas y rompería determinismo/reproducibilidad, violando
`ESPECIFICACION_ANALISIS_ESCENARIOS_MERCADO_V1.md §3.5`) — es adaptativo solo en su **origen** (se
deriva de datos reales, no de una convención externa inexistente para este parámetro), pero una vez
calculado por el procedimiento de la sección 1, se congela como constante numérica exacta, igual
que `PeriodoAdxExploratorio`/`UmbralAdxTendenciaExploratorio` ya son constantes en el código.

Esto es coherente con D-030: un valor "Pendiente" (sin convención externa, como este) puede pasar a
"Propuesto" si existe un procedimiento objetivo, no discrecional, y documentado — no necesita ser
descartado solo por no tener precedente en literatura externa.

---

## 3. Cómo validar que no genera demasiados Ambiguos, casi ningún Ambiguo, o fragmentación excesiva

Reutiliza directamente el método ya definido en `DEFINICION_UMBRAL_SESGO_DI_V1.md §5` (no se
inventa un mecanismo nuevo, consistente con D-015): una vez calculado el valor candidato por el
procedimiento de la sección 1, se ejecuta `EvaluadorClasificadores.cs` (ya construido en Fase
1.4-A) sobre el candidato ADX+DI extendido a 4 estados, midiendo cobertura, distribución por
régimen, duración media y fragmentación en los mismos 6 timeframes.

**Criterios de sensatez** (umbral de alerta, no de rechazo automático — coherente con D-025: la
razonabilidad se juzga en conjunto, no por una sola cifra aislada):

| Señal de alerta | Qué indicaría |
|---|---|
| `% Ambiguo` < 1% en todos los timeframes | El umbral es tan permisivo que Ambiguo es prácticamente inexistente — equivalente en la práctica a no tener el cuarto estado (mismo problema que motivó D-028 para el modelo de 3 estados). |
| `% Ambiguo` > 50% en algún timeframe | El umbral es tan estricto que Ambiguo domina sobre Lateral — sugiere que la fórmula relativa está capturando algo distinto de "disputa direccional real". |
| Fragmentación de Ambiguo (tramos de 1 ventana) desproporcionadamente alta frente a Lateral | El estado Ambiguo estaría capturando ruido de alta frecuencia en vez de episodios sostenidos de disputa direccional. |

**Si alguna señal de alerta se activa**: no se ajusta el umbral buscando "arreglar" el número — se
documenta como hallazgo (Observación / posible limitación del método, misma clasificación que D-025
usó para EMA) y se decide explícitamente si el procedimiento de la sección 1 necesita revisión
metodológica (ej. calcular la mediana sobre otro subconjunto de ventanas) — nunca se sustituye la
mediana por un número elegido a mano para pasar la validación.

---

## Fuera de alcance (respetado)

No se ejecuta el procedimiento sobre el dataset real en este documento — se define el método, no se
corre todavía (correrlo y obtener el número exacto es el siguiente paso, condicionado a la
aprobación de este procedimiento). No se implementa código. No se ejecuta ninguna estrategia.

---

## Criterio de cierre de la definición de valor

- ✓ Procedimiento concreto y ejecutable para elegir el valor sin mirar resultados de estrategias
  (sección 1) — mediana de la serie `|DI+-DI-|/(DI+ + DI-)` sobre la zona `ADX < 25` del dataset
  real, regla fijada antes de calcularse.
- ✓ Naturaleza del valor resuelta: basado en distribución histórica, congelado tras un único
  cálculo (sección 2).
- ✓ Validación contra los 3 riesgos pedidos (demasiado Ambiguo, casi ningún Ambiguo, fragmentación)
  definida reutilizando `EvaluadorClasificadores.cs` (sección 3), sin mecanismo nuevo.
- ⏳ Aprobación de este procedimiento — pendiente antes de ejecutarlo sobre el dataset real y
  obtener el valor numérico final de `UmbralSesgoDI`.
