# Evaluación de Segunda Familia — Caso 3A (D-086, segunda de 2)

Estado: **documento de evaluación — previo a selección e implementación**. Compara candidatos para
la segunda familia requerida por D-086 contra las **4** estrategias ya existentes (no solo las 3
originales), bajo el criterio ya aprobado en D-087 (máxima distancia estructural). No selecciona
ni implementa — presenta evidencia para que la auditoría decida.

---

## 1. Perfil estructural actualizado (4 estrategias existentes)

Tabla ampliada respecto a la usada para seleccionar Z-Score (`ESPECIFICACION_FAMILIA_ESTRATEGIA_
CASO3_V1.md` §1), verificada contra código real:

| Eje | Tres Mosqueteros / MHI | EMA Cross | Z-Score Reversal |
|---|---|---|---|
| Origen de la señal | Color de vela — patrón visual puntual | Cruce de EMA — indicador acumulado (tendencia) | Z-score — propiedad estadística de la serie (dispersión) |
| Anclaje temporal | Cuadrantes fijos `N%5` | Ninguno | Ninguno |
| Gestión de intentos | Martingala con reintentos | Ninguna | Ninguna |
| Horizonte de cierre | Fijo, `2+maxMartingalas` ciclos | Variable, por cruce contrario | Variable, por reversión a umbral |
| Estado interno | Fase/contador discreto | Acumulador exponencial (EMA, horizonte infinito) | Ventana deslizante finita (suma/suma de cuadrados) |
| Relación entre activos | Ninguna — 1 solo instrumento | Ninguna | Ninguna |
| Dirección de la apuesta | Sigue el color observado | Sigue la tendencia (momentum) | Apuesta contra el extremo (reversión) |

**Ejes ya cubiertos por al menos una de las 4 estrategias**: señal por patrón visual, señal por
indicador de tendencia, señal por dispersión estadística; anclaje fijo y anclaje nulo; con y sin
martingala; horizonte fijo y horizonte variable (por dos mecanismos distintos: cruce y reversión a
umbral); estado discreto, acumulador exponencial, y ventana finita; dirección momentum y dirección
contraria.

**Ejes NO cubiertos por ninguna de las 4**: relación entre múltiples activos/series (todas operan
sobre 1 solo instrumento, aislado), decisión basada en múltiples fuentes/condiciones combinadas
(todas usan una única condición de entrada), y ausencia total de reacción al mercado (control de
neutralidad, candidato D original — nunca implementado, sigue disponible).

---

## 2. Candidatos evaluados

### Candidato D — Estrategia sin mercado (reservado desde `ESPECIFICACION_FAMILIA_ESTRATEGIA_CASO3_V1.md`)

Señal fija o aleatoria, no reacciona a ninguna condición real del dataset.

- **Ejes que comparte con las 4 existentes**: ninguno de los 7 ejes de la tabla aplica de forma
  significativa — no tiene "origen de señal" real, no tiene "dirección de apuesta" basada en
  lógica de mercado.
- **Qué información nueva aporta**: sirve como **control experimental** — si el pipeline/reportes/
  métricas producen resultados coherentes (ej. `EficienciaOperacionalPct` cercano a 50% para una
  señal aleatoria simétrica, sin errores ni valores degenerados), confirma que el laboratorio no
  introduce sesgo estructural propio. Si algo en el pipeline dependiera implícitamente de que toda
  estrategia "reaccione al mercado" de alguna forma, D lo expondría.
- **Qué supuestos rompe**: que toda estrategia evaluada tiene una hipótesis de mercado — D no
  tiene ninguna, es deliberadamente vacía de contenido predictivo.
- **¿Activa D-055?**: no adicionalmente — D sigue sin martingala, mismo caso ya cubierto por EMA
  Cross/Z-Score. No aporta nueva evidencia a D-055 más allá de la ya acumulada.
- **¿Requiere nueva metadata?**: no — `CaracteristicasEstrategia(UsaMartingala: false)` es
  suficiente, mismo mecanismo ya implementado.
- **Riesgo experimental que cubre y Z-Score no cubrió**: valida que el laboratorio mide lo que dice
  medir, no que produce resultados plausibles por casualidad — un tipo de riesgo (validez del
  instrumento de medición) completamente distinto del que Z-Score cubrió (generalidad de la
  lógica de señal).

### Candidato E — Señal multi-condición (nuevo, no estaba en la lista original de `PROPUESTA_CASO3_V1.md`)

Ejemplo conceptual: combinar 2 condiciones independientes (ej. una de tendencia + una de volumen)
que deben cumplirse simultáneamente para generar la señal — no explorado en `PROPUESTA_CASO3_V1.md`
§6 originalmente, pero surge como candidato natural al identificar el eje "decisión basada en
múltiples fuentes" como no cubierto (sección 1).

- **Ejes que comparte**: origen de señal técnico (similar a EMA Cross si una condición es de
  tendencia), pero el eje realmente nuevo es la **composición** de condiciones, no cada condición
  por separado.
- **Qué información nueva aporta**: prueba si el laboratorio generaliza a estrategias cuya
  `Observar` evalúa más de una fuente de datos/condición antes de decidir — las 4 estrategias
  actuales, incluida Z-Score, evalúan una única condición (color, cruce, z-score).
- **Qué supuestos rompe**: que una estrategia siempre tiene una única condición de entrada
  evaluable de forma aislada.
- **¿Activa D-055?**: depende del diseño (con o sin martingala) — no es inherente al candidato.
- **¿Requiere nueva metadata?**: potencialmente sí, si se considera relevante declarar "usa
  múltiples condiciones" como una capacidad — pero **no hay consumidor concreto** de esa
  información en el catálogo de métricas actual, mismo principio que evitó agregar
  `UsaSizingPropio`/`UsaEstadoInternoPersistente` sin necesidad demostrada (D-088). No se
  justificaría agregar metadata nueva solo por este candidato.
- **Riesgo experimental que cubre**: complejidad de la lógica de decisión interna de `Observar` —
  distinto del riesgo de "origen de la señal" que las 4 estrategias ya prueban.

### Candidato F — Múltiples instrumentos/correlación (nuevo, descartado tras evaluación)

Ejemplo conceptual: señal basada en la relación entre 2 series (ej. spread entre dos activos).

- **Por qué se descarta sin más análisis**: `DataSlice`/`BacktestRunner`/todo el motor operan sobre
  **1 solo instrumento por corrida** — verificado en `src/Domain/Shared/DataSlice.cs` (una sola
  `IReadOnlyList<Candle>`) y en `ConfiguracionExperimento` (una sola `Instrumento?`). Evaluar esto
  requeriría tocar el motor (`src/`) para aceptar múltiples series simultáneas — **prohibido
  explícitamente** para Caso 3A (motor congelado, D-015). Descartado sin evaluación adicional, no
  por falta de interés sino por violar la restricción de alcance ya fijada.

---

## 3. Comparación resumida

| Candidato | Distancia estructural real | Activa D-055 | Requiere metadata nueva | Riesgo experimental cubierto |
|---|---|---|---|---|
| D — Sin mercado | Alta (ausencia total de lógica de mercado) | No, ya cubierto | No | Validez del instrumento de medición |
| E — Multi-condición | Media-alta (composición de condiciones, no origen de señal) | Depende del diseño | No justificado todavía | Complejidad de decisión interna |
| F — Multi-instrumento | Descartado — requiere tocar `src/` | — | — | — |

---

## 4. Recomendación

**Candidato D — Estrategia sin mercado**, por dos motivos verificables:

1. Es el único de los 3 evaluados que no requiere ninguna decisión de diseño adicional (E deja
   abierta la composición exacta de condiciones; D es, por definición, la ausencia de lógica) — el
   más simple de fijar sin introducir un segundo ciclo de especificación.
2. Cubre un **tipo de riesgo distinto** al que las 3 estrategias sin martingala ya prueban (EMA
   Cross, Z-Score): no prueba "¿generaliza a otra lógica de señal?" (ya respondido 2 veces), prueba
   "¿el laboratorio mide correctamente incluso cuando no hay señal genuina que medir?" — una
   pregunta que D-086 (2 familias) no había cubierto todavía y que es más barata de responder ahora
   que después.

**E queda registrado como candidato futuro**, fuera de Caso 3A — no descartado por falta de valor,
sino porque introduce una decisión de diseño (qué condiciones combinar) que D-086 no exige resolver
para completar el requisito de 2 familias, y que merecería su propia especificación si se retoma.

---

## Fuera de alcance de este documento

No se implementa código. No se fija el mecanismo exacto de aleatoriedad/señal fija de D (ej. semilla
determinista para reproducibilidad, ver P6/determinismo ya exigido en el patrón de pruebas de Caso
3) — corresponde a la especificación de implementación siguiente, mismo patrón que Z-Score.

---

## Próximo paso

Si se aprueba D como segunda familia: `ESPECIFICACION_IMPLEMENTACION_ESTRATEGIA_NEUTRAL_V1.md`,
resolviendo el mecanismo de señal (fija vs. aleatoria con semilla determinista — el determinismo ya
exigido por el patrón de pruebas de Caso 3 obliga a que, si es aleatoria, use una semilla fija) y
las pruebas obligatorias equivalentes a P1-P8 de Z-Score.
