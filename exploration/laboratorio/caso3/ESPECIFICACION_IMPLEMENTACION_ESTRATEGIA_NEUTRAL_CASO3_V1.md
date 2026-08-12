# Especificación de Implementación — Estrategia Neutral (Caso 3A, segunda familia)

Estado: **documento de diseño implementable — previo a implementación**. Traduce
`EVALUACION_SEGUNDA_FAMILIA_CASO3_V1.md` (candidato D aprobado) a un diseño concreto. Precisión
del auditor incorporada: la familia debe ser un **control experimental determinista**, no una
simulación de ruido — se descarta explícitamente cualquier fuente de aleatoriedad. No modifica
código en este documento.

---

## 1. Qué significa exactamente "sin mercado"

**Definición operativa**: la estrategia genera órdenes según una regla fija que no lee ningún dato
de la vela (`Candle.Open`/`High`/`Low`/`Close`/`Volume`) para decidir dirección ni momento — solo
usa la posición secuencial de la vela (`DataSlice.N`), un dato estructural del dataset, no una
condición de mercado. Esto es distinto de "aleatoria": la regla es 100% determinista y reproducible
sin semilla, verificable a mano contando velas.

**Regla concreta**: cada `Ciclo` velas (parámetro fijo, ver sección 2), abrir `Buy`; `Ciclo/2`
velas después (redondeado hacia abajo), cerrar y abrir `Sell`; repetir. Sin lectura de precio para
decidir dirección — el precio de la vela solo se usa (indirectamente, por el motor, no por la
estrategia) para calcular el resultado de ganancia/pérdida al resolver la operación, exactamente
igual que las 4 estrategias existentes usan el precio de resolución, no de decisión.

**Por qué esto y no una alternancia trivial "abre y cierra cada vela"**: una regla de ciclo fijo
(análoga en estructura a los cuadrantes `N%5` de Tres Mosqueteros/MHI, pero sin ninguna
interpretación de color de vela) permite que el pipeline produzca operaciones completas
(entrada+resolución) con la misma cadencia que las estrategias reales, sin que ningún componente
del reporte trate "0 operaciones" o "1 operación por vela" como un caso degenerado no
representativo.

---

## 2. Regla determinista de generación de órdenes

**Parámetro congelado**: `Ciclo = 10` (arbitrario, elegido por ser distinto de `N%5` de Tres
Mosqueteros/MHI — evita que un lector confunda esta estrategia con una variante de las de patrón).
No calibrado, no ajustable tras ver resultados — mismo criterio D-030.

**Lógica** (sin martingala, sin reintentos — mismo perfil que EMA Cross/Z-Score en este eje):

```
Si N % Ciclo == 0 y no hay posición abierta:
    abrir Buy
Si N % Ciclo == Ciclo/2 y hay posición Buy abierta:
    cerrar (Sell) y abrir Sell
Si N % Ciclo == 0 (excepto la primera vez) y hay posición Sell abierta:
    cerrar (Buy) y abrir Buy
```

Alternancia estricta Buy → Sell → Buy → ..., cadencia fija de `Ciclo/2 = 5` velas por posición —
ningún dato de la vela decide cuándo o en qué dirección abrir, solo `N`.

**Ganancia**: mismo criterio que EMA Cross/Z-Score — comparación contra el precio de entrada real
(`Buy` gana si `Close_resolución >= Close_entrada`, `Sell` si `<=`). El resultado de ganar/perder sí
depende del mercado (inevitable: el motor resuelve el `Fill` contra el precio real) — lo que **no**
depende del mercado es la decisión de abrir/cerrar en sí, que es la definición de "neutral" fijada
en la sección 1.

---

## 3. ¿Produce operaciones o permanece siempre neutral?

**Produce operaciones**, con cadencia fija y conocida de antemano (`Ciclo/2` velas por posición) —
"neutral" describe la **ausencia de hipótesis de mercado en la señal**, no la ausencia de
actividad. Una estrategia que nunca opera no generaría evidencia útil para las preguntas de
`EVALUACION_SEGUNDA_FAMILIA_CASO3_V1.md` §4 (identidad experimental, ejecución, reportes,
particiones, métricas, determinismo) — todas requieren operaciones reales que fluyan por el
pipeline completo.

---

## 4. Cómo se interpreta en reportes

**`ResultadoGeneral`** (universal): aplica sin cambios — `EficienciaOperacionalPct` para esta
estrategia es el dato central de la prueba de control: con una regla de apertura/cierre
independiente del precio, la eficiencia esperada (sobre una muestra suficientemente grande, en un
mercado sin tendencia direccional fuerte) debería aproximarse a un valor sin sesgo estructural
propio del pipeline — un desvío sistemático y grande sería una señal de que algo en el
motor/reporte introduce sesgo, no de que la estrategia "funciona".

**`ResolucionDeIntentos`**: no aplica (`UsaMartingala=false`, mismo mecanismo D-088/D-090 ya
implementado) — presentado como "no aplica" vía `PresentadorResolucionIntentos`, sin cambios
adicionales a ese componente.

**Nota obligatoria en cualquier reporte/ficha que documente esta estrategia**: debe declarar
explícitamente que es un control experimental, nunca una estrategia candidata — mismo tipo de nota
que D-054 exigió para EMA Cross ("valida generalidad del pipeline, nunca evalúa rentabilidad"),
aplicado aquí con mayor énfasis: esta estrategia no tiene ninguna hipótesis de mercado que evaluar,
ni siquiera en principio.

---

## 5. Metadata de capacidades

`CaracteristicasEstrategia(UsaMartingala: false)` — mismo mecanismo ya implementado en
`protocolo/EjecutorProtocolo.cs`, sin cambios al record ni a su ubicación (D-090 ya resuelta y
cerrada, no se reabre).

---

## 6. Pruebas obligatorias antes de cerrar

- **P1 — Determinismo estructural**: la secuencia de aperturas/cierres depende únicamente de `N`,
  verificado ejecutando la estrategia sobre 2 datasets con velas de **precio distinto** pero mismo
  número de velas — la secuencia de `Side`/timing de órdenes debe ser idéntica byte a byte entre
  ambas corridas (solo el resultado de ganar/perder puede diferir, nunca cuándo/qué dirección se
  abre).
- **P2 — Cadencia fija**: cada posición dura exactamente `Ciclo/2` velas, verificado contando
  timestamps de entrada/resolución en una corrida sintética.
- **P3 — Sin lectura de precio para decisión**: prueba explícita de que `Open`/`High`/`Low`/
  `Volume` de la vela no influyen en la decisión — mismo dataset con esos 4 campos alterados
  arbitrariamente (manteniendo `Close` y `Timestamp`) produce exactamente la misma secuencia de
  órdenes.
- **P4 — Sin aleatoriedad**: dos instancias de la estrategia, construidas por separado, sobre el
  mismo dataset, producen resultados idénticos sin necesidad de fijar ninguna semilla — confirma
  que no hay `Random` ni fuente de entropía en la implementación (verificable por inspección del
  código, más esta prueba como evidencia adicional).
- **P5 — Metadata correcta**: `CaracteristicasEstrategia.UsaMartingala == false`; el reporte
  muestra "no aplica" en `ResolucionDeIntentos`.
- **P6 — Rendimiento sobre 1m**: la corrida completa sobre ~500,000 velas termina en tiempo
  razonable — la lógica es O(1) por vela (una comparación de módulo), no se espera ningún problema,
  pero se verifica igual, mismo criterio que P5 de Z-Score.
- **P7 — Integración en el pipeline sin cambios de código**: `EjecutorProtocolo`/
  `ReporteConsolidadoGenerador`/`ReporteEscenariosGenerador`/`MetricasPorEscenario`/
  `ClasificadorRegimenV1` aceptan la estrategia sin ningún cambio adicional — mismo criterio de
  éxito que D-054 verificó para EMA Cross.
- **P8 — Regresión de Caso 1/Caso 2**: hash de `baseline_final/`/`baseline_financiero_final/` sin
  cambio, 107/107 producción sin cambio.

---

## Fuera de alcance de este documento

No se implementa código. No se activa D-044 ni D-084. No se agrega metadata más allá de
`UsaMartingala` — ningún campo nuevo en `CaracteristicasEstrategia` tiene consumidor concreto
todavía.

---

## Próximo paso

Autorización de implementación bajo el alcance de este documento — mismo patrón que Z-Score:
`exploration/EstrategiaNeutral.cs` (o nombre equivalente), pruebas en `caso3/`, sin tocar `src/`ni
`tests/`, P1-P8 como criterio de cierre.
