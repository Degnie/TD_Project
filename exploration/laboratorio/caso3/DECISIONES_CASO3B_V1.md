# Decisiones — Caso 3B: Generalización Experimental — Multi-Condición

Estado: **D-099 abierta**. Misma estructura usada en D-001 a D-098 (decisión, opciones, criterio,
evidencia). Ningún código se modifica en este documento — las resoluciones aquí registradas
habilitan la especificación de implementación siguiente, no la reemplazan.

Contexto completo en `PROPUESTA_CASO3B_V1.md`. Verificación contra código existente, no
reconstruida de memoria (mismo criterio que abrió Caso 2/Caso 3A/Caso 4, D-057).

---

## D-099 — Definición de "multi-condición"

**Estado**: 🟢 Aprobada. **Selección: C — condiciones jerárquicas.**

**Decisión**: ¿qué significa, exactamente, que una estrategia de Caso 3B decida "por múltiples
condiciones"? Debe resolverse en términos de la relación lógica/temporal entre condiciones —
**sin elegir todavía una estrategia concreta ni qué condiciones combinar** (eso corresponde a una
decisión posterior, una vez fijada la semántica).

**Por qué esta pregunta precede a cualquier otra**: `EVALUACION_SEGUNDA_FAMILIA_CASO3_V1.md` §2
identificó el eje no cubierto como "decisión basada en múltiples fuentes/condiciones combinadas",
pero no especificó la relación entre esas fuentes — el ejemplo conceptual usado ahí ("tendencia +
volumen") es compatible con varias semánticas distintas (¿deben cumplirse ambas a la vez? ¿basta
una? ¿una habilita la evaluación de la otra?). Sin fijar esto primero, cualquier especificación de
implementación estaría adivinando el diseño en vez de derivarlo de una decisión explícita — mismo
principio que ya aplicó D-092 en Caso 4 (definir la semántica antes que el componente).

### Opciones

- **A — Simultáneas, todas obligatorias (AND puro)**: la señal se emite únicamente si **todas**
  las N condiciones se cumplen en la misma vela/evaluación. Cada condición se evalúa de forma
  independiente entre sí (ninguna depende del resultado de otra).
  - Ventajas: la más simple de verificar de forma aislada (criterio de éxito de
    `PROPUESTA_CASO3B_V1.md` §6) — cada condición es una función pura `DataSlice → bool`,
    la combinación es una conjunción lógica trivial de N resultados booleanos.
  - Riesgos: es la semántica menos distintiva frente a lo ya probado — una condición compuesta por
    AND de sub-condiciones independientes es, estructuralmente, cercana a evaluar "una condición
    más específica", no necesariamente un eje nuevo de complejidad de decisión.
- **B — Alternativas, cualquiera basta (OR)**: la señal se emite si **al menos una** de las N
  condiciones se cumple. Misma independencia entre condiciones que A, pero combinadas por
  disyunción.
  - Ventajas: igual de simple de verificar que A; síntoma distinto (más señales, no menos) — útil
    como contraste si se implementaran ambas, pero no aporta un eje estructural nuevo respecto a A
    (misma independencia entre condiciones, solo cambia el operador de combinación).
  - Riesgos: mismo riesgo que A — no prueba nada que AND no probara ya sobre la capacidad del
    pipeline de aceptar una estrategia con lógica de decisión compuesta.
- **C — Jerárquicas (una condición habilita la evaluación de la siguiente)**: la segunda condición
  solo se evalúa si la primera ya se cumplió — ej. "si hay señal de tendencia, entonces evaluar
  condición de volumen; si no hay señal de tendencia, no evaluar volumen en absoluto".
  - Ventajas: introduce dependencia real entre condiciones (a diferencia de A/B, donde todas se
    evalúan siempre de forma independiente) — es el primer candidato que prueba si `Observar`
    puede tener una estructura de decisión con ramas condicionadas, no solo una combinación plana
    de resultados booleanos.
  - Riesgos: mayor superficie de diseño (orden de evaluación, qué pasa si la condición habilitante
    nunca se cumple) — más decisiones de diseño que fijar en la especificación siguiente.
- **D — Acumulativas (score/conteo, señal por umbral de condiciones cumplidas)**: cada condición
  aporta un peso o cuenta; la señal se emite si la suma/conteo supera un umbral (ej. "al menos 2 de
  3 condiciones", o una suma ponderada).
  - Ventajas: es la semántica más distinta de todo lo ya probado — ninguna de las 5 estrategias
    existentes tiene una noción de "grado" o "cuántas señales parciales se acumularon"; todas son
    binarias (hay señal / no hay señal) en su condición de entrada.
  - Riesgos: introduce el concepto de umbral configurable, que empieza a acercarse a un parámetro
    calibrable (riesgo de confundir "estructura de decisión nueva" con "espacio de calibración
    nuevo", algo que `PROPUESTA_CASO3B_V1.md` §8 excluye explícitamente — optimización de
    umbrales queda fuera de alcance).

### Restricciones que aplican a cualquier opción seleccionada

- La composición de condiciones debe ser verificable de forma aislada (`PROPUESTA_CASO3B_V1.md`
  §6) — cada condición individual y la lógica de combinación deben poder probarse sin ejecutar un
  backtest completo.
- No se fija en esta decisión si la implementación requiere un nuevo tipo/estructura o si se
  resuelve inline dentro de `Observar` — eso corresponde a una decisión posterior (D-100,
  "Representación interna de condiciones", condicionada a esta).
- `IStrategy` no se modifica bajo ninguna opción — todas son compatibles con el contrato actual
  (`IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice)`, `src/Domain/Strategy/IStrategy.cs`),
  la relación entre condiciones es lógica interna de la implementación concreta, no un cambio de
  contrato.

### Evidencia

- `EVALUACION_SEGUNDA_FAMILIA_CASO3_V1.md` §2 (Candidato E): origen del eje "composición de
  condiciones, no cada condición por separado" como lo no cubierto por las 4/5 estrategias
  existentes.
- `src/Domain/Strategy/IStrategy.cs`: contrato actual, una única evaluación por vela sin
  restricción sobre la lógica interna — ninguna opción de A-D requiere tocarlo.
- Las 5 estrategias congeladas (Tres Mosqueteros, MHI Mayoría, EMA Cross, Z-Score Reversal,
  Estrategia Neutral): todas evalúan una única condición de entrada — ninguna aporta precedente de
  código para ninguna de las 4 opciones, esta decisión no tiene un patrón ya "reubicado" como sí lo
  tuvo D-092 (clasificación de intención, que ya existía en `AplicadorFill`).

### Resolución adoptada

**Selección: C — jerárquicas.** La condición primaria habilita la evaluación de la condición
secundaria; si la primaria no se cumple, la secundaria no se evalúa en absoluto. El orden importa,
existe dependencia real entre condiciones, y la primaria cambia el espacio de evaluación de la
siguiente — mismo criterio que distingue esta opción de A/B (independencia plana) y de D (riesgo
de deslizarse hacia calibración de pesos/umbrales, fuera de alcance de Caso 3B).

**Esquema conceptual**: `Contexto válido → evaluar oportunidad → emitir señal`. No se fija todavía
cuántos niveles jerárquicos tiene la estrategia concreta de Caso 3B (mínimo 2 según la definición,
sin techo fijado aquí) ni qué condiciones específicas ocupan cada nivel — eso corresponde a una
decisión posterior, una vez resueltos D-100/D-101.

**Explícitamente rechazadas**:
- **A (AND puro)** y **B (OR)**: ambas mantienen evaluación independiente entre condiciones —
  estructuralmente cercanas a una intersección/unión de filtros, no aportan un eje de complejidad
  de decisión distinto al ya cubierto por composición de señales existente.
- **D (Acumulativa)**: valor experimental reconocido, pero introduce cantidad + peso + umbral de
  condiciones, desplazando la pregunta hacia calibración de parámetros — fuera del alcance de
  Caso 3B (`PROPUESTA_CASO3B_V1.md` §8).

**Consecuencia**: D-099 no autoriza implementación todavía. Habilita D-100 (representación interna
de la cadena jerárquica) y D-101 (observabilidad de qué condición se evaluó/bloqueó la señal) como
las siguientes decisiones necesarias antes de cualquier especificación de implementación.

---

## D-100 — Representación interna de la cadena jerárquica

**Estado**: 🟢 Aprobada. **Selección: objetos internos de condición.**

**Decisión**: ¿cómo se representa, en código, una cadena jerárquica de condiciones (D-099) sin
modificar `IStrategy`? La pregunta no es si existe una solución — varias son posibles — sino cuál
se adopta y por qué, evitando asumir la respuesta antes de compararla contra al menos las
alternativas evidentes.

**Restricción heredada**: `IStrategy` no se modifica salvo evidencia directa de que el contrato
actual (`IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice)`) no alcanza — mismo principio
aplicado en D-092 (Caso 4) antes de introducir `ClasificadorIntencionOrden`, y confirmado
explícitamente en la resolución de D-099.

### Opciones esperables (sin selección todavía)

- **Composición inline dentro de la estrategia de laboratorio**: la lógica jerárquica vive
  enteramente dentro del método `Observar` de la clase concreta (ej. `if` anidados o early-return
  sobre la condición primaria antes de evaluar la secundaria) — sin ningún tipo/estructura auxiliar
  nueva. Análogo a cómo las 5 estrategias existentes ya implementan su lógica de decisión, solo que
  con una rama condicionada en vez de una única condición plana.
- **Objetos internos de condición**: introducir un tipo pequeño, privado o interno a la estrategia
  de Caso 3B (no compartido con `src/Domain`), que represente cada condición como una unidad
  evaluable de forma aislada (ej. `Func<DataSlice, bool>` con nombre, o un `record` simple) — motivado
  por el criterio de éxito de `PROPUESTA_CASO3B_V1.md` §6 (verificable de forma aislada sin
  ejecutar un backtest completo).
- **Pipeline interno de evaluación**: una estructura más general (ej. lista ordenada de
  condiciones con una regla de corta-circuito jerárquico) que podría, en principio, reutilizarse
  si una futura fase agrega más niveles — riesgo de sobre-diseñar para una necesidad hipotética no
  confirmada por esta única estrategia (evaluar contra el criterio de "sin abstracciones no
  solicitadas" ya aplicado en fases anteriores).

**Criterio a aplicar**: la opción elegida debe resolver primero D-101 (observabilidad) como parte
de la misma comparación — una representación que no permita distinguir qué condición se evaluó o
bloqueó la señal no cumple el criterio de auditabilidad exigido por la propuesta, independientemente
de su simplicidad.

### Resolución adoptada

**Selección: objetos internos de condición.** Estructura conceptual:

```
EstrategiaMultiCondicion
        |
        +-- CondicionPrimaria
        |
        +-- CondicionSecundaria
        |
        +-- ReglaJerarquica
```

La estrategia sigue siendo la única unidad que implementa `IStrategy` y decide qué `OrderRequest`
emitir — internamente delega la evaluación de cada condición a un objeto propio, privado a la
estrategia de Caso 3B, no compartido con `src/Domain`.

**Explícitamente rechazadas**:
- **Inline dentro de `Observar`**: funciona para 2 niveles, pero degrada rápidamente en `if`
  anidados con baja trazabilidad y sin posibilidad de probar cada condición de forma aislada —
  aceptable para una condición plana (como las 5 estrategias existentes), no para una familia
  cuyo objetivo explícito es la estructura de decisión (`PROPUESTA_CASO3B_V1.md` §2).
- **Pipeline interno de evaluación**: abstracción de nivel superior (motor de condiciones + flujo
  genérico + composición dinámica) sin evidencia actual de que se necesite — posible evolución
  futura si una fase distinta agrega más niveles jerárquicos, no una necesidad de Caso 3B. Mismo
  criterio de "sin abstracciones no solicitadas" ya aplicado en fases anteriores.

**Consecuencia**: cada condición (`CondicionPrimaria`, `CondicionSecundaria`) es una unidad
evaluable de forma aislada, cumpliendo el criterio de éxito de `PROPUESTA_CASO3B_V1.md` §6.
`ReglaJerarquica` encapsula la dependencia (D-099): la secundaria solo se evalúa si la primaria se
cumplió.

---

## D-101 — Observabilidad de la cadena jerárquica

**Estado**: 🟢 Aprobada. **Selección: observabilidad derivada de la estructura interna de D-100,
sin metadata nueva expuesta en `IStrategy`.**

**Decisión**: ¿cómo se expone, para cada evaluación de `Observar`, cuál condición (primaria/
secundaria) fue evaluada y cuál bloqueó o permitió la señal? Sin esto, una estrategia jerárquica es
opaca desde afuera — no se puede distinguir "no hubo señal porque la condición primaria no se
cumplió" de "no hubo señal porque, cumplida la primaria, la secundaria no se cumplió" — dos
resultados observacionalmente idénticos (`Observar` devuelve lista vacía) pero con causas distintas.

**Relación con D-100**: esta decisión no es independiente — la forma de representar la cadena
(D-100) determina qué información hay disponible para exponer. No se resuelve en abstracto antes
de D-100, se resuelven en conjunto.

**Restricción heredada**: mismo criterio que D-088 (Caso 3A) — la observabilidad no debe forzar un
cambio en `IStrategy` ni en el contrato de ejecución; si requiere metadata, debe ser externa y
justificada por un consumidor concreto (ej. las pruebas de la especificación de implementación
siguiente), no agregada especulativamente.

### Resolución adoptada

**Selección: la observabilidad sale de la estructura de evaluación de D-100, no de una metadata
externa.** Cada condición devuelve internamente un resultado evaluable — esquema conceptual:

```csharp
ResultadoCondicion
{
    Cumple,
    Motivo,
    ValorObservado
}
```

Esto no implica exponerlo al motor ni a `IStrategy` — la capa de laboratorio/reporte (pruebas,
diagnóstico) puede consumir ese resultado directamente desde los objetos internos de D-100, sin
que la estrategia deba declarar ninguna capacidad adicional en su contrato de ejecución.

**Explícitamente rechazado**: agregar `IStrategy.UsaMultiplesCondiciones` o una
`MetadataCondiciones` externa (paralela a `CaracteristicasEstrategia` de D-088/D-090) — no existe
todavía un consumidor concreto que la requiera. Mismo principio que D-088 ya aplicó: la
aplicabilidad/observabilidad debe surgir de una necesidad demostrada, no asumirse por adelantado.

**Consecuencia**: D-101 quedó resuelta en conjunto con D-100 (como se anticipó en "Relación con
D-100") — la estructura de objetos internos de condición ya provee el punto de extracción que
D-101 necesitaba, sin decisión de diseño adicional.

---

## D-102 — Selección de la familia concreta

**Estado**: 🔵 Pendiente de resolución.

**Decisión**: ¿cuál es la estrategia multi-condición concreta de Caso 3B? Debe responder: cuál es
la condición primaria, cuál la secundaria, por qué la relación entre ambas es genuinamente
jerárquica (no una reformulación de AND), y qué parámetros quedan fuera de calibración (mismo
límite ya fijado en `PROPUESTA_CASO3B_V1.md` §8 y reforzado al rechazar la Opción D de D-099).

**Restricción de diseño**: no implementar todavía una arquitectura genérica reutilizable para
futuras familias — D-100 ya rechazó el "pipeline interno de evaluación" por la misma razón; D-102
selecciona una familia concreta con 2 niveles fijos (primaria/secundaria), no un framework de N
niveles.

**Verificación previa (código existente)**: `Candle`
(`src/Domain/Shared/Candle.cs:4-10`) expone `Open`/`High`/`Low`/`Close`/`Volume` — ninguna de las 5
estrategias congeladas (Tres Mosqueteros, MHI Mayoría, EMA Cross, Z-Score Reversal, Estrategia
Neutral) usa `Volume` como condición de entrada. Confirmado por grep exhaustivo sobre
`exploration/*.cs`.

### Candidatos (sin selección todavía)

- **Candidato G — Contexto de tendencia + oportunidad de entrada**: condición primaria = régimen de
  tendencia (ej. EMA corta sobre/bajo EMA larga, mismo indicador ya validado por
  `EstrategiaEmaCross.cs`, pero usado aquí como *filtro habilitante*, no como señal de entrada
  directa); condición secundaria = una oportunidad puntual evaluada solo si la primaria se cumple
  (ej. reversión de corto plazo dentro de la tendencia ya confirmada). Ejemplo conceptual ya
  anticipado en `EVALUACION_SEGUNDA_FAMILIA_CASO3_V1.md` §2 ("tendencia + volumen"), adaptado aquí
  a jerarquía en vez de combinación plana.
  - Riesgo: la condición primaria reutiliza el mismo indicador que EMA Cross — debe verificarse
    que el *uso jerárquico* (filtro, no señal) sea suficientemente distinto para no ser una
    variación menor de una estrategia ya congelada.
- **Candidato H — Contexto de volumen + señal de precio**: condición primaria = volumen relativo
  respecto a una ventana reciente (eje no cubierto por ninguna estrategia existente); condición
  secundaria = señal de precio evaluada solo si el volumen confirma "contexto activo" (ej. z-score
  o cruce, evaluado únicamente cuando hay volumen suficiente).
  - Ventaja: `Volume` es un eje genuinamente sin explorar en el laboratorio — mayor distancia
    estructural que el Candidato G.
  - Riesgo: requiere definir qué significa "volumen suficiente" sin que ese umbral se convierta en
    un parámetro de calibración (mismo riesgo ya señalado para la Opción D rechazada de D-099) —
    debe fijarse por convención externa antes de ejecutar, no ajustarse mirando resultados (mismo
    criterio D-030).
- **Candidato I — Contexto de volatilidad + señal direccional**: condición primaria = régimen de
  volatilidad (ej. amplitud de rango `High-Low` relativa a una ventana); condición secundaria =
  señal direccional evaluada solo si la volatilidad está dentro de un rango operable.
  - Riesgo: más cercano conceptualmente al `ClasificadorRegimenV1` de Caso 1 (D-028/D-034,
    congelado) — debe verificarse que no reproduce esa clasificación ya existente bajo otro nombre,
    o si la reproduce, declararlo explícitamente en vez de presentarlo como un eje nuevo.

**Criterio a aplicar**: la familia elegida debe declarar explícitamente qué supuesto del pipeline
espera poner a prueba (mismo criterio ya exigido por D-087 en Caso 3A) y confirmar, antes de
implementar, que la condición primaria elegida no duplica sin declararlo un mecanismo ya congelado
(`ClasificadorRegimenV1`, `EstrategiaEmaCross`).

### Resolución adoptada

**Selección: Candidato H — Contexto de volumen + señal de precio.**

**Motivo**: es el único candidato donde la condición primaria (volumen) no reutiliza, ni siquiera
como filtro, un indicador ya validado por una estrategia congelada — a diferencia de G (reutiliza
el indicador de tendencia de `EstrategiaEmaCross.cs`) e I (riesgo de reproducir
`ClasificadorRegimenV1`, D-028/D-034). `Volume` es un campo de `Candle` presente desde Caso 1
(`src/Domain/Shared/Candle.cs:9`) pero nunca leído por ninguna de las 5 estrategias existentes —
confirmado por grep, no asumido.

**Estructura jerárquica confirmada**:
```
¿Existe contexto suficiente de participación (volumen)?
        ↓ (solo si se cumple)
¿La señal de precio es válida?
        ↓
orden
```
La condición primaria (volumen) no genera la operación por sí sola — únicamente habilita la
evaluación de la condición secundaria (precio), cumpliendo D-099.

**Explícitamente rechazadas**:
- **G (tendencia + entrada)**: diferencia insuficiente frente a `EstrategiaEmaCross.cs` — "EMA
  Cross + una condición adicional" es una variación incremental, no una familia estructural nueva.
- **I (volatilidad + dirección)**: proximidad conceptual con `ClasificadorRegimenV1` — riesgo de
  terminar probando una variante de algo ya congelado en vez de un eje nuevo.

**Restricción explícita para D-103**: la condición primaria debe fijarse por convención previa
(ventana fija, regla estadística no optimizada, umbral definido antes de ejecutar pruebas) —
nunca por ajuste histórico ni búsqueda del mejor parámetro. Mismo criterio D-030 ya aplicado a
Z-Score Reversal (`Ventana=20`/`UmbralEntrada=2.0` fijados por convención externa, no calibrados).

---

## D-103 — Definición de condiciones concretas (familia H)

**Estado**: 🔵 Pendiente de resolución.

**Decisión**: dentro del Candidato H (D-102), ¿qué mide exactamente la condición primaria
(volumen/contexto) y qué mide exactamente la condición secundaria (precio)? Debe resolver ambas
partes juntas, más por qué la relación entre ellas es genuinamente jerárquica y no una
reformulación de una estrategia ya existente.

**No se crea código, estrategia, ni metadata en esta decisión** — solo se fija la definición
conceptual y el criterio de no-calibración; la implementación corresponde a la especificación
siguiente.

### Condición primaria — volumen/contexto

Debe resolver: qué mide, cómo se calcula, con qué ventana (fija, no optimizada), y qué umbral
determina "contexto suficiente".

- **Opción P1 — Volumen relativo a media móvil simple**: `Volume` de la vela actual comparado
  contra el promedio de una ventana reciente (ej. `Volume > Media(Volume, N)`) — condición binaria
  simple, misma familia de cálculo O(1) por vela que ya usa `EstrategiaZScoreReversion.cs` (suma
  deslizante), evitando reintroducir el bug O(n²) ya corregido en EMA Cross.
- **Opción P2 — Percentil histórico fijo**: `Volume` de la vela actual comparado contra un
  percentil pre-calculado sobre todo el dataset (ej. "por encima del percentil 70") — requiere
  conocer la distribución completa antes de iniciar la corrida, lo que introduce una dependencia
  distinta a las estrategias existentes (todas operan vela a vela sin mirar el dataset completo por
  adelantado) — riesgo de romper el patrón de evaluación estrictamente secuencial ya usado en todo
  el laboratorio.
- **Opción P3 — Múltiplo fijo sobre ventana**: variante de P1 con umbral no unitario (ej.
  `Volume > 1.5 × Media(Volume, N)`) — mismo cálculo O(1), introduce un segundo número fijo
  (multiplicador) además del tamaño de ventana.

### Condición secundaria — señal de precio

Debe resolver: qué señal genera la entrada, y cómo se distingue de la lógica ya usada por
`EstrategiaZScoreReversion.cs` (z-score sobre ventana de `Close`) para no ser una repetición sin
declarar.

- **Opción S1 — Cruce de EMA corta/larga**: mismo mecanismo que `EstrategiaEmaCross.cs`, pero
  evaluado únicamente cuando la condición primaria ya se cumplió — reutiliza un indicador de
  Caso 1, con el riesgo de acercarse al mismo problema ya señalado en el Candidato G rechazado
  (aunque aquí el volumen, no la tendencia, es la condición primaria).
- **Opción S2 — Ruptura de rango reciente (breakout)**: `Close` de la vela actual supera el
  máximo (o mínimo) de una ventana reciente de `High`/`Low` — eje de señal no usado por ninguna de
  las 5 estrategias existentes (que usan color de vela, cruce de EMA, o z-score de `Close`, nunca
  ruptura de rango) — mayor distancia estructural que S1.
- **Opción S3 — Variante del z-score de `EstrategiaZScoreReversion.cs`**: reutilizar el mismo
  mecanismo de reversión a la media, evaluado solo bajo contexto de volumen — debe declararse
  explícitamente como reutilización deliberada si se elige, no presentarse como señal nueva
  (riesgo ya anticipado por el criterio de D-102).

**Criterio a aplicar**: la combinación P×S elegida debe maximizar distancia estructural total
(mismo criterio D-087) — preferir la combinación que no reutilice, ni en primaria ni en secundaria,
un mecanismo ya congelado, salvo que se declare explícitamente como reutilización deliberada y se
justifique por qué el contexto jerárquico la vuelve una prueba distinta.

### Resolución adoptada

**Selección: P3 (múltiplo fijo sobre ventana) para la condición primaria; S2 (ruptura de rango /
breakout) para la condición secundaria.**

**Condición primaria — P3**: `Volumen actual > Media(Volumen, N) × Factor`. Determinista,
interpretable, sin optimización — el mismo patrón de cálculo O(1) por vela que ya usa
`EstrategiaZScoreReversion.cs` (suma deslizante), con un multiplicador fijo adicional que no
introduce dependencia de todo el dataset por adelantado.

**Explícitamente rechazadas**:
- **P1 (media móvil simple, sin multiplicador)**: válida, pero demasiado cercana a los patrones ya
  usados en el laboratorio (medias móviles, ventanas deslizantes, filtros estadísticos) — menor
  distancia experimental que P3.
- **P2 (percentil histórico)**: rompe el patrón de "estado actual + ventana local" que favorece el
  laboratorio — introduce una referencia histórica acumulada y una distribución completa que
  aumenta la complejidad interpretativa más de lo necesario, acercándose a un modelado estadístico
  más avanzado que el requerido por D-102.

**Condición secundaria — S2**: `Close` de la vela actual rompe el máximo (o mínimo) de una ventana
reciente de `High`/`Low` — evaluada únicamente si la condición primaria (P3) ya se cumplió.

**Explícitamente rechazadas**:
- **S1 (cruce de EMA)**: "EMA Cross + filtro previo" sería demasiado cercano a una estrategia ya
  congelada — mismo riesgo ya señalado para el Candidato G rechazado en D-102.
- **S3 (variante de z-score)**: Caso 3A ya exploró z-score explícitamente
  (`EstrategiaZScoreReversion.cs`) — reutilizarlo aquí requeriría justificarse como composición
  deliberada, no como nueva familia, y no es el objetivo de Caso 3B.

**Estructura conceptual confirmada**: `contexto de volumen válido (P3) → precio rompe rango (S2) →
entrada` — combina participación de mercado (volumen) con expansión de precio (breakout), sin
depender de tendencia, medias móviles de precio, ni reversión estadística — ningún mecanismo ya
congelado se reutiliza en ninguno de los dos niveles.

**Restricción confirmada para la especificación de implementación**: D-103 no fija valores
concretos (ventana de volumen, factor multiplicador, ventana de rango, definición exacta de
ruptura — ej. estricta `>` vs. `≥`, con o sin cierre de la vela de ruptura) — deben fijarse por
convención previa a las pruebas, nunca por ajuste observando resultados (D-030).

---

## D-104 — Diseño de implementación de la estrategia H

**Estado**: 🟢 Aprobada. **Selección: `EstrategiaVolumenBreakout`, objetos internos con estado
propio, observabilidad vía callback existente, hereda sin martingala/sin posiciones simultáneas de
Z-Score/Neutral.**

**Decisión**: ¿cómo se traduce la familia H (D-102/D-103) en una clase concreta que implemente
`IStrategy`? Debe resolver: nombre de la estrategia, estructura exacta de los objetos internos de
condición (D-100), punto de integración con `IStrategy.Observar`, y qué expone la observabilidad
(D-101) para cada uno de los 3 resultados posibles de una evaluación: (a) condición primaria no
cumplida, (b) primaria cumplida pero secundaria no, (c) ambas cumplidas → orden emitida.

**No se fijan en esta decisión**: valores numéricos concretos (ventana, factor) — corresponden a
la especificación de implementación siguiente, no a esta decisión de diseño. D-104 resuelve forma,
no magnitud.

**Restricciones heredadas, confirmadas explícitamente**: sin modificar `src/`, sin modificar
`IStrategy`, sin activar Caso 4 (`GestorCapital`/sizing/`ValidadorCapacidad`), sin modificar ningún
reporte o métrica financiera ya congelados.

### Puntos a resolver

- **Nombre de la estrategia**: debe reflejar la estructura jerárquica (contexto + ruptura), no solo
  el indicador usado — evitar un nombre que sugiera equivalencia con una estrategia existente.
- **Objetos internos** (D-100): forma concreta de `CondicionPrimaria`/`CondicionSecundaria`/
  `ReglaJerarquica` — ¿son tipos (`record`/`class`) instanciados una vez en el constructor con
  estado propio (ventanas deslizantes, mismo patrón que `EstrategiaZScoreReversion.cs`), o
  funciones evaluadas por vela sin estado propio, con el estado viviendo en la clase contenedora?
- **Integración con `Observar`**: el método debe seguir devolviendo `IReadOnlyList<OrderRequest>`
  sin cambios de firma — la pregunta es solo la organización interna (delegar a los objetos de
  condición vs. lógica plana), no el contrato externo.
- **Observabilidad (D-101)**: `ResultadoCondicion` debe estar disponible para pruebas — vía qué
  mecanismo concreto (ej. campo público de solo lectura tras cada `Observar`, callback opcional
  como el ya usado por `InfoOperacionResuelta`/`_onOperacionResuelta` en las estrategias
  existentes, u otro) — reutilizar el patrón de callback ya establecido evita introducir un
  mecanismo de instrumentación nuevo sin necesidad.
- **Sin posiciones simultáneas ni martingala**: confirmar si la familia H hereda estas
  restricciones de las familias sin martingala ya existentes (Z-Score, Neutral) — si D-055 se
  activa o no depende de esto, mismo criterio de D-093 en Caso 4 (dependiente de otra decisión, no
  asumido).

### Resolución adoptada

**Nombre: `EstrategiaVolumenBreakout`.** Describe la composición real (volumen + breakout) sin
sugerir equivalencia con `EstrategiaEmaCross`/`EstrategiaZScoreReversion`/ninguna estrategia de
tendencia genérica.

**Objetos internos: estado propio encapsulado por condición.**
```
EstrategiaVolumenBreakout
        |
        +-- CondicionVolumen
        |       |
        |       +-- ventana
        |       +-- volumen acumulado
        |
        +-- CondicionBreakout
                |
                +-- máximos/mínimos ventana
```
Motivo: cada condición tiene memoria propia (volumen necesita ventana deslizante, rango necesita
extremos históricos recientes) — separarlas evita mezclar el estado de ambas dentro de la
estrategia principal, manteniendo cada una probable de forma aislada (criterio de éxito de
`PROPUESTA_CASO3B_V1.md` §6).

**Explícitamente rechazada**: funciones puras sin estado, con el estado viviendo en la clase
contenedora — no por ser incorrecta, sino porque "condición + ventana temporal" implica estado
inevitable; esconder ese estado en la estrategia principal en vez de encapsularlo en cada condición
reduce claridad sin ganar nada a cambio.

**Observabilidad: reutilización del patrón de callback ya existente** (mismo mecanismo que
`InfoOperacionResuelta`/`_onOperacionResuelta`) — sin nuevo sistema de eventos, sin metadata nueva,
sin cambio de contrato. El resultado de evaluación se expresa como:
```
ResultadoEvaluacionCondiciones
{
    Primaria:   Cumple / No cumple
    Secundaria: Cumple / No cumple
    Acción:     Orden emitida / Ninguna
}
```
Cubre los 3 resultados posibles ya identificados en la decisión (primaria no cumplida / secundaria
no cumplida / orden emitida).

**Martingala y posiciones: `UsaMartingala = false`, una posición máxima abierta** — hereda
explícitamente el patrón experimental ya establecido por Z-Score Reversal y Estrategia Neutral.
Motivo: Caso 3B evalúa la estructura de decisión (jerarquía de condiciones), no gestión de
posiciones — activar escalado, múltiples posiciones o sizing contaminaría la comparación con un eje
que esta fase no busca probar (mismo principio que `PROPUESTA_CASO3B_V1.md` §3 ya aplicó al
excluir Caso 4 del alcance).

**Confirmado sin fijar**: ventana de volumen, factor multiplicador, ventana de breakout — quedan
para D-105.

---

## D-105 — Parámetros convencionales de `EstrategiaVolumenBreakout`

**Estado**: 🟢 Aprobada. **Selección: `N=20` (volumen y breakout), múltiplo `1.5×`, máximo excluye
la vela actual, comparación estricta `>`, señal inmediata sin confirmación N+1.**

**Decisión**: ¿qué valores numéricos fijos usa `EstrategiaVolumenBreakout`, definidos por
convención externa antes de ejecutar cualquier prueba — nunca ajustados observando resultados
(D-030, mismo criterio ya aplicado a `Ventana=20`/`UmbralEntrada=2.0`/`UmbralSalida=0.5` de
`EstrategiaZScoreReversion`, motivados ahí por referencia estadística externa: Bandas de Bollinger
estándar y ~95% de una distribución normal, no calibración sobre el dataset)?

**Restricción explícita**: cada valor debe justificarse por una convención citable (norma de
mercado, práctica estándar documentada, o una elección deliberadamente neutral declarada como tal)
— no por "es el valor que produjo mejores resultados en una prueba preliminar". Si ningún valor
tiene una convención externa citable, debe declararse explícitamente como elección neutral
arbitraria (ej. "ventana=20 por ser el mismo tamaño ya usado en Z-Score, no por una razón propia
del breakout") en vez de presentarse con una justificación inventada.

### Volumen (`CondicionVolumen`)

- **Tamaño de ventana**: cuántas velas componen `Media(Volumen, N)`.
  - Candidato: mismo `N=20` que `EstrategiaZScoreReversion` — ventaja de reutilizar un tamaño ya
    congelado y no elegido para esta familia específicamente (reduce grados de libertad), riesgo de
    no tener relación conceptual propia con "contexto de participación de volumen" (que podría
    razonablemente pedir una ventana distinta a la de precio).
  - Candidato alternativo: convención de mercado citable para volumen (ej. ventanas de 10 o 14
    períodos, comunes en osciladores de volumen estándar) — requiere declarar la convención
    exacta que se está citando.
- **Múltiplo fijo**: factor sobre la media (ej. `1.5×`, `2.0×`).
  - Debe declararse por qué ese múltiplo y no otro — ej. "2.0× es un salto claramente
    distinguible del ruido normal de la serie" es una justificación conceptual aceptable si se
    declara como tal; "elegido tras observar que producía más señales" no lo es (D-030).

### Breakout (`CondicionBreakout`)

- **Ventana de rango**: cuántas velas componen el máximo/mínimo de referencia (`High`/`Low`).
  - Puede coincidir o no con la ventana de volumen — deben declararse como decisiones
    independientes, no asumir que deben ser el mismo número solo por conveniencia.
- **Criterio exacto de ruptura**, con 3 sub-preguntas a resolver explícitamente:
  - **Máximo anterior vs. máximo incluyendo la vela actual**: si la vela actual participa en el
    cálculo del máximo de referencia, una ruptura nunca podría detectarse (la vela actual nunca
    puede superar un máximo que ella misma ayudó a formar) — probablemente el máximo debe excluir
    la vela actual, pero debe confirmarse explícitamente, no asumirse por omisión.
  - **Comparación estricta (`>`) vs. no estricta (`≥`)**: afecta si un empate exacto con el máximo
    previo cuenta como ruptura.
  - **Confirmación N+1 (esperar una vela adicional de cierre por encima del rango) vs. señal
    inmediata en la misma vela de ruptura**: una estrategia de confirmación introduce latencia
    deliberada (ninguna de las 5 estrategias existentes espera confirmación de N+1 velas antes de
    señalizar) — debe decidirse si esto es parte de lo que Caso 3B quiere probar o una complejidad
    no solicitada para esta fase.

**Criterio a aplicar**: preferir, entre candidatos empatados en validez conceptual, el que menos
grados de libertad nuevos introduce — mismo principio general aplicado en D-100 al rechazar el
"pipeline interno de evaluación" por sobre-diseño no solicitado.

### Resolución adoptada

**Volumen**: ventana `N=20` — continuidad metodológica con `EstrategiaZScoreReversion` (misma
ventana ya congelada en el laboratorio, no elegida por rendimiento esperado; documentado
explícitamente como convención experimental, no como parámetro óptimo). Múltiplo fijo `1.5×`:
`VolumenActual > MediaVolumen20 × 1.5` — evita reaccionar a ruido normal, representa un aumento
visible de participación, regla simple y auditable. Sin búsqueda de factor ni comparación de
valores alternativos.

**Breakout**: ventana de rango `N=20` velas previas — declarado explícitamente como coincidencia
por simplicidad experimental con la ventana de volumen, no como relación obligatoria entre ambas
(siguen siendo decisiones independientes). `Máximo = máximo de las 20 velas anteriores`, `Mínimo =
mínimo de las 20 velas anteriores`, ambos **excluyen la vela actual** — incluirla sería circular
(la vela actual nunca podría superar/perforar un extremo que ella misma ayudó a formar). Operador
estricto (`>`/`<`), no `≥`/`≤` — una igualdad no representa expansión de precio. **Señal inmediata**
en la vela actual, sin confirmación adicional N+1 — misma cadencia de evaluación por vela que las 5
estrategias existentes, ninguna introduce latencia de confirmación.

**Ampliación bidireccional (post D-107, sin abrir D-108)**: D-105 se extiende para incluir la
ruptura simétrica a la baja — misma ventana, mismo volumen, mismo operador estricto, misma
exclusión de la vela actual, sin ninguna condición nueva. No es una hipótesis distinta: es la
misma regla de breakout evaluada en la dirección opuesta, requerida para que D-107 (cierre por
señal contraria) tenga una semántica concreta en vez de asumida.

**Regla completa de decisión**:
```
Vela actual
  Long:
    1. VolumenActual > MediaVolumen20 × 1.5
         ↓ (si no se cumple: no evaluar breakout, sin señal)
    2. Close_actual > Máximo20Anterior
         ↓ (si no se cumple: sin señal)
    3. Señal Long

  Short:
    1. VolumenActual > MediaVolumen20 × 1.5
         ↓ (si no se cumple: no evaluar breakout, sin señal)
    2. Close_actual < Mínimo20Anterior
         ↓ (si no se cumple: sin señal)
    3. Señal Short
```
Ambas ramas comparten la misma `CondicionVolumen` (una sola evaluación de volumen por vela, no
duplicada) — solo la condición de breakout se evalúa en ambos sentidos.

**Exclusiones confirmadas**: optimización de ventana, de múltiplo, de criterio de breakout, y
búsqueda del mejor período — ninguna forma parte de D-105 ni de ninguna decisión posterior de
Caso 3B.

---

## D-106 — Especificación de implementación y pruebas

**Estado**: 🟡 Especificación aprobada, implementación bloqueada por D-107.

**Decisión**: `ESPECIFICACION_ESTRATEGIA_VOLUMEN_BREAKOUT_V1.md` — ubicación
(`exploration/EstrategiaVolumenBreakout.cs`, pruebas en `caso3/TestsEstrategiaVolumenBreakout.cs`,
mismo módulo satélite reutilizado), estructura de objetos internos (D-100), algoritmo de
`Observar`, observabilidad (`ResultadoEvaluacionCondiciones`, D-101), tratamiento de warmup, y
batería de pruebas P1-P12.

**Hallazgo durante la revisión de la especificación**: ninguna decisión D-099 a D-105 definió el
criterio de cierre de la posición — todas resolvieron la condición jerárquica de *entrada*. La
especificación había propuesto una regla de cierre (§4.1: pérdida de la condición primaria) como
detalle operativo sin decisión formal — el auditor identificó correctamente que esto cambia el
comportamiento económico de la estrategia (afecta duración de posiciones, PnL, exposición) y por
tanto requiere una decisión D-N propia, no debe resolverse silenciosamente dentro de una
especificación de implementación.

**Consecuencia**: D-106 queda aprobada en todo lo que no depende del criterio de cierre
(ubicación, objetos internos, algoritmo de entrada, observabilidad, warmup, estructura de
pruebas P1-P5/P8-P12) — **bloqueada para implementación** hasta resolver D-107. La sección 4.1 de
la especificación y las pruebas P6/P7 (que dependen del criterio de cierre) quedan pendientes de
actualizarse tras D-107.

---

## D-107 — Semántica de cierre de `EstrategiaVolumenBreakout`

**Estado**: 🔵 Pendiente de resolución.

**Decisión**: ¿bajo qué condición se cierra una posición abierta por `EstrategiaVolumenBreakout`?
No se implementa código, `IStrategy` no se modifica, ninguna decisión previa (D-099 a D-106) se
reabre — D-107 completa exclusivamente la semántica de salida que quedó sin definir.

**Por qué es una decisión propia y no un detalle de D-106**: el criterio de cierre determina
cuánto tiempo permanece abierta una posición, lo cual afecta directamente PnL, exposición y
duración — un cambio de comportamiento económico observable de la estrategia, no una decisión de
representación interna (D-100) ni de observabilidad (D-101). Mismo criterio que distinguió, en
Caso 4, una decisión de diseño estructural (D-095) de un defecto de test sin decisión asociada.

### Opciones

- **A — Cierre por pérdida de contexto de volumen**: si `CondicionVolumen.Evaluar == false` en una
  vela posterior con posición abierta, cerrar. Ventaja: mantiene simetría con la hipótesis
  central de la familia — "el volumen habilita la oportunidad", si el contexto desaparece, la
  premisa que motivó la entrada ya no aplica. Riesgo: una caída de volumen no implica
  necesariamente que la ruptura de precio haya terminado — podría cerrar posiciones válidas
  prematuramente solo porque el volumen momentáneamente bajó.
- **B — Cierre por ruptura inversa del precio**: con posición Long abierta, si `Close` rompe el
  mínimo de la ventana de rango, cerrar (stop simétrico al breakout de entrada). Ventaja: la
  salida depende del mismo eje que motivó la entrada (precio). Riesgo: acerca la estrategia a un
  sistema clásico de breakout con stop — introduce una segunda condición de breakout (a la baja)
  no contemplada por D-099/D-102/D-103, que definieron jerarquía de entrada, no un par
  entrada/salida simétrico.
- **C — Mantener posición hasta señal contraria explícita**: sin regla de cierre basada en
  volumen ni en precio — la posición permanece abierta indefinidamente hasta que una nueva
  evaluación jerárquica completa (primaria + secundaria cumplidas) en sentido contrario la
  cierre/revierta. Ventaja: no agrega ninguna hipótesis experimental nueva — Caso 3B valida la
  estructura jerárquica de *entrada* (el objetivo declarado en `PROPUESTA_CASO3B_V1.md` §2), no
  una política completa de gestión de posiciones; A y B introducirían una segunda hipótesis
  (sensibilidad al volumen como señal de salida, o simetría del breakout) que no formó parte del
  Candidato H evaluado en D-102. Riesgo: puede dejar posiciones abiertas por periodos largos o
  indefinidos si no aparece señal contraria — mismo riesgo que ya aceptaron explícitamente EMA
  Cross y Z-Score/Estrategia Neutral respecto a duración no acotada de posición (ninguna de las 5
  estrategias existentes tiene duración fija salvo Tres Mosqueteros/MHI, que usan un mecanismo de
  martingala explícitamente no heredado por esta familia, D-104).

**Resolución adoptada**: **Opción C — cierre por señal contraria explícita**, generada por la
propia lógica de entrada evaluada en sentido inverso (no por pérdida de volumen ni por un stop de
precio ad-hoc).

**Precisión requerida antes de considerar D-107 completa** (hallazgo verificado durante la
revisión de la especificación): "señal contraria" no tenía semántica concreta, porque D-102/D-103/
D-105 solo habían definido la jerarquía volumen→breakout en sentido alcista. Resuelto extendiendo
D-105 (ver arriba) para que el breakout se evalúe en ambas direcciones bajo la misma regla — no se
trata de una nueva hipótesis ni de una nueva familia, es la extensión simétrica de la regla ya
aprobada.

**Definición final**:
- **Long abierto**: se cierra cuando aparece volumen válido + breakout bajista (`Close <
  Mínimo20Anterior`) → resultado `Long → Short` (reversión). No requiere ninguna lógica nueva del
  lado del motor — se procesa con `AplicadorFill` ya existente, sin modificar `src/`.
- **Short abierto**: se cierra cuando aparece volumen válido + breakout alcista (`Close >
  Máximo20Anterior`) → resultado `Short → Long`.

**Precisión de mecanismo (post-implementación, ver `AUDITORIA_CASO3B_V1.md` hallazgo 1)**: esta
decisión fijó *qué* señal cierra la posición, no *cómo* se emite en código. La verificación contra
`AplicadorFill` real mostró que, con `Cantidad=1m` fija, una única `OrderRequest` de esa magnitud
nunca activa `ResolutorCrossZero` (que exige `magnitudFill > magnitudPosicion`, no `==`) — produce
un `CierreTotal`, no una reversión. La implementación emite 2 `OrderRequest` explícitas (cierre +
apertura) en la misma llamada a `Observar`, mismo patrón ya usado por `EstrategiaNeutral`. D-107 en
sí (la señal que determina el cierre) no cambia — solo se corrige cómo se traduce a código, un
detalle de implementación, no una nueva decisión de semántica.

**Explícitamente rechazadas**:
- **A (pérdida de contexto de volumen)**: una caída de volumen no implica necesariamente que la
  ruptura de precio haya terminado — riesgo de cerrar posiciones válidas prematuramente.
- **B unilateral (solo stop de precio sin extender la jerarquía)**: habría introducido un criterio
  de salida asimétrico respecto a la regla de entrada, sin la misma condición de volumen que la
  habilita — descartada en favor de la versión simétrica bajo la misma regla completa (que es, en
  esencia, lo que terminó adoptando C tras la precisión).
- **Estrategia unilateral (solo Long, sin breakout bajista)**: habría requerido una decisión D-N
  adicional para definir cómo cerrar sin señal contraria (tiempo, pérdida de contexto, salida
  artificial) — cada una introduce una hipótesis experimental no prevista por el Candidato H. La
  versión simétrica evita esa decisión adicional por construcción.

**Consecuencia arquitectónica confirmada**: sin stop por volumen, sin salida por pérdida de rango
en el sentido de la Opción A/B originales, sin gestión temporal, sin ajuste dinámico. Hereda
`UsaMartingala = false` y la restricción de máximo una posición abierta (D-104) — la señal
contraria revierte la posición existente, no abre una segunda posición simultánea.

---

## Resumen de decisiones

| Decisión | Selección | Estado |
|---|---|---|
| D-099 | Condiciones jerárquicas — primaria habilita evaluación de secundaria (Opción C) | 🟢 Aprobada |
| D-100 | Objetos internos de condición (`CondicionPrimaria`/`CondicionSecundaria`/`ReglaJerarquica`) | 🟢 Aprobada |
| D-101 | Observabilidad estructural (`ResultadoCondicion`), sin metadata nueva en `IStrategy` | 🟢 Aprobada |
| D-102 | Candidato H — volumen (contexto) + señal de precio | 🟢 Aprobada |
| D-103 | P3 (múltiplo fijo sobre ventana) + S2 (ruptura de rango / breakout) | 🟢 Aprobada |
| D-104 | `EstrategiaVolumenBreakout`, objetos internos con estado propio, callback existente, sin martingala/1 posición | 🟢 Aprobada |
| D-105 | `N=20` (ambas ventanas), múltiplo `1.5×`, breakout bidireccional (máximo/mínimo), ambos excluyen vela actual, operador estricto, sin confirmación N+1 | 🟢 Aprobada (ampliada tras D-107) |
| D-106 | Especificación de implementación y pruebas | 🟡 Actualizándose — desbloqueada por D-107 |
| D-107 | Cierre por señal contraria (ruptura jerárquica inversa bajo la misma condición de volumen) | 🟢 Aprobada |

---

## Fuera de alcance de este documento

No se modifica código en este documento. `ESPECIFICACION_ESTRATEGIA_VOLUMEN_BREAKOUT_V1.md`
(D-106) queda desbloqueada tras D-105/D-107 — requiere actualizarse (bidireccionalidad, cierre por
señal contraria) antes de proceder a implementar.

---

## Próximo paso

Resolución de D-107 por el auditor. Tras eso: actualizar §4.1/P6/P7 de
`ESPECIFICACION_ESTRATEGIA_VOLUMEN_BREAKOUT_V1.md` con el criterio de cierre elegido, y proceder a
implementar `exploration/EstrategiaVolumenBreakout.cs` +
`exploration/laboratorio/caso3/TestsEstrategiaVolumenBreakout.cs`.
