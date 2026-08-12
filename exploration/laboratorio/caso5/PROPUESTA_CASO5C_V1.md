# Propuesta — Caso 5C: Capa de Análisis y Recomendación Experimental

Estado: **documento de apertura — previo a cualquier decisión o implementación**. Define la
pregunta que responde Caso 5C, sus límites, y las decisiones que deben resolverse antes de tocar
código, siguiendo el mismo ciclo que toda fase anterior: propuesta → decisión → implementación →
pruebas → auditoría → congelamiento.

**Alcance confirmado explícitamente por el auditor**: capa de recomendación experimental
explicable basada en comparaciones históricas — **no** selección automática del "mejor gestor".

---

## 0. Verificación previa — ¿existe evidencia acumulada suficiente?

Antes de definir qué significa "recomendar", se auditó contra el estado real del repositorio si ya
existe un corpus de comparaciones sobre el cual razonar, o si `ComparadorGestores` (Caso 5B) es hoy
solo una capacidad bajo demanda sin historial.

**Resultado, verificado exhaustivamente (grep de `File\.|Directory\.|JsonSerializer|StreamWriter`
en `caso5/`, sin coincidencias)**: **no existe ninguna evidencia acumulada.**
`ComparadorGestores.Comparar` devuelve un `ResultadoComparativoGestores` en memoria;
`RenderizadorComparacionGestores.Generar` produce un `string`; ninguno de los dos escribe a disco.
Las 8 pruebas de `TestsComparadorGestores.cs` ejecutan, verifican en memoria y descartan el
resultado al terminar el proceso. Las combinaciones ejecutadas hasta ahora en Caso 5A/5B son
puntuales de verificación (1 dataset, 1-2 estrategias, hasta 3 gestores) — no un corpus con
diversidad de datasets/timeframes/escenarios.

**Consecuencia directa**: Caso 5C no puede empezar por "diseñar cómo se recomienda" — antes
necesita que exista algo que persista comparaciones. Esta propuesta cubre ambas capas de forma
explícitamente separada (§3), para no mezclar "cómo se acumula evidencia" con "cómo se razona
sobre ella".

**Precedente reutilizable, ya verificado en código**: `exploration/laboratorio/protocolo/
resultados/` — `EjecutorProtocolo`/el laboratorio ya materializan corridas individuales en carpetas
timestamped (`{Estrategia}_{timestamp}/`) con `IDENTIDAD_EXPERIMENTAL.json` + reportes en
Markdown, excluidas de git (`.gitignore:5`, evidencia regenerable). El mismo patrón de
persistencia — carpeta timestamped, JSON de identidad, reporte legible — es aplicable a
`ResultadoComparativoGestores` sin inventar un mecanismo nuevo.

---

## 1. Objetivo de Caso 5C

**Pregunta principal**: dado un historial de comparaciones de gestores ya ejecutadas y persistidas,
¿puede el laboratorio producir una recomendación experimental explicable — no una selección
automática — sobre qué gestor probar primero bajo condiciones dadas?

**Qué significa "recomendar" en esta fase** (D-N a resolver, ver §4): el auditor fija como criterio
inicial evitar la opción de selección automática hasta tener evidencia suficiente. Las opciones a
decidir formalmente:
- **Sugerir candidatos para probar** — presenta 1 o más gestores como punto de partida razonable,
  sin descartar los demás.
- **Ordenar configuraciones por criterios explícitos y declarados** — un orden derivado de reglas
  visibles (ej. "menor drawdown observado"), no de una función de puntuación opaca.
- **Seleccionar automáticamente** — el sistema decide y aplica un gestor sin intervención humana.
  **Descartada de entrada por el auditor** para esta fase — candidata solo si una fase muy
  posterior acumula evidencia y validación suficientes.

**No incluye**:
- Selección automática de gestor por ninguna estrategia o corrida (§1, descartada explícitamente).
- Aprendizaje automático, modelos predictivos, o cualquier forma de ajuste de pesos sobre datos
  observados.
- Adaptación en vivo (cambiar de gestor durante una corrida en curso).
- Optimización o calibración de parámetros de ningún gestor (D-030, heredado sin cambios).
- Cualquier afirmación sobre comportamiento futuro de mercado — una recomendación es sobre
  **comportamiento ya observado bajo condiciones ya ejecutadas**, nunca una promesa (mismo límite
  que D-016 ya aplica al clasificador de régimen: no mezclar histórico con promesa futura).

---

## 2. Riesgo central — sobreajuste y falsa autoridad de la recomendación

Pregunta que debe quedar resuelta antes de cualquier implementación, porque determina la forma
completa del componente, no un detalle de presentación:

**Una recomendación inválida** se parece a: *"Fixed Risk ganó en BTCUSDT 1H 2024"* — una afirmación
puntual, sin contexto de cuántas condiciones se probaron, disfrazada de generalización.

**Una recomendación válida** se parece a: *"Bajo estas N condiciones observadas (estrategia X,
timeframe Y, M escenarios distintos), Fixed Risk mostró este perfil relativo — menor drawdown en
K de M casos, mismo profit factor promedio"* — declara explícitamente el tamaño y la diversidad de
la evidencia sobre la que se basa, mismo principio que D-010 (Caso 1) ya exige para toda
comparación: tamaño de muestra obligatorio, nunca una métrica sola.

**Consecuencia de diseño**: cualquier salida de Caso 5C debe declarar, junto a la recomendación,
sobre cuántas comparaciones se basa y bajo qué condiciones — nunca presentar una observación de una
sola corrida con la misma autoridad que una de cien.

---

## 3. Dos capas distintas, a no confundir

**Capa 1 — Acumulación de evidencia** (precondición, §0): persistir cada
`ResultadoComparativoGestores` que se ejecute, con su identidad experimental completa, en un
formato legible y regenerable — extensión directa del precedente de `protocolo/resultados/`.

**Capa 2 — Análisis sobre el corpus acumulado**: dado un conjunto de comparaciones persistidas,
producir una recomendación experimental — la capa que responde la pregunta de §1.

Esta separación es deliberada: la Capa 1 puede construirse y verificarse de forma completamente
independiente de la Capa 2 (persistir comparaciones es útil incluso sin ningún análisis encima), y
mezclar ambas en un solo diseño arriesga acoplar "cómo se guarda" con "cómo se interpreta" — mismo
principio D-072/D-077 de separación cálculo/presentación ya aplicado en todo el proyecto, extendido
aquí a "acumulación/interpretación".

---

## 4. Decisiones nuevas — numeración reservada desde D-116

Ninguna decisión se resuelve en esta propuesta — el siguiente documento
(`DECISIONES_CASO5C_V1.md`) resuelve cada punto con la misma disciplina de fases anteriores.

**D-116 (candidata) — Mecanismo de persistencia de comparaciones (Capa 1)**: formato y ubicación
de la evidencia acumulada — extensión del patrón de `protocolo/resultados/` (carpeta timestamped +
JSON de identidad + reporte legible) aplicado a `ResultadoComparativoGestores`, vs. una estructura
nueva. Debe decidir también si la persistencia vive dentro de `ComparadorGestores` (Caso 5B,
requeriría reabrir esa fase) o en un componente nuevo que lo envuelve (Caso 5C, sin tocar Caso 5B).

**D-117 (candidata) — Qué información puede usar una recomendación**: qué campos del corpus
acumulado son insumo válido — estrategia, timeframe, características declaradas del dataset,
métricas comparativas (D-114), y **explícitamente no**: régimen de mercado inferido, cualquier
forma de predicción, ni ningún dato no ya presente en `MetricasFinancieras`/identidad experimental.

**D-118 (candidata) — Semántica de "recomendar"**: cuál de las 2 opciones no descartadas en §1
(sugerir candidatos vs. ordenar por criterio explícito declarado) — o si ambas conviven como
salidas distintas del mismo componente.

**D-119 (candidata) — Umbral de suficiencia de evidencia**: cuántas comparaciones/condiciones
distintas debe haber acumulado el corpus antes de que el sistema produzca una recomendación en vez
de negarse a hacerlo — mismo principio D-010 aplicado como precondición de ejecución, no solo como
advertencia textual. Debe fijar qué hace el sistema cuando la evidencia es insuficiente: no debe
inventar una recomendación con baja confianza silenciosa.

**D-120 (candidata) — Formato de la recomendación y su declaración de evidencia**: cómo se
presenta una recomendación válida (§2) — estructura de datos, y qué campos son obligatorios
(tamaño de muestra, condiciones cubiertas, nunca solo una conclusión aislada).

---

## 5. Restricciones heredadas (sin relajar)

- **`IStrategy`, las 6 estrategias, `AplicadorFill`, `ResolutorCrossZero`, `GestorCapital`,
  `IGestorRiesgo`, `EjecutorProtocolo`, `EntradaProtocolo`** — sin modificación, mismo criterio que
  Caso 5A/5B ya mantuvieron.
- **`ComparadorGestores`/`ResultadoComparativoGestores`/`RenderizadorComparacionGestores` (Caso
  5B)**: no se modifican salvo que D-116 decida explícitamente que la persistencia vive ahí —
  decisión a tomar, no asumida.
- **Sin selección automática de gestor** (§1) — ninguna corrida real cambia de gestor por decisión
  del sistema.
- **Sin optimización ni calibración** de ningún parámetro (D-030).
- **Sin Kelly ni Masaniello** — bloqueo metodológico de Caso 2.3 no resuelto (D-110, heredado).
- **Sin predicción de mercado ni promesa sobre el futuro** (D-016 extendido, §1/§2).
- **Ningún baseline congelado se toca** (`caso1` a `caso5b-v1-experimental`).

---

## 6. Criterios de éxito iniciales

- El sistema puede acumular N comparaciones de `ComparadorGestores` ejecutadas en momentos
  distintos, sin perder ninguna al reiniciar el proceso (Capa 1).
- Dado un corpus acumulado con diversidad suficiente (D-119), el sistema puede responder: *"bajo
  estas condiciones, estos gestores mostraron este perfil relativo, basado en N comparaciones"* —
  nunca *"este es el mejor gestor"*.
- Con evidencia insuficiente, el sistema declara explícitamente esa insuficiencia en vez de
  producir una recomendación de baja confianza sin advertirlo.
- Ninguna recomendación aplica un gestor a una corrida real — es información para que un humano
  decida, nunca una acción automática.

---

## Fuera de alcance de este documento

No se implementó código. No se selecciona ningún mecanismo de persistencia. No se resuelve D-116 a
D-120 — solo se declara su existencia y el problema que cada una debe resolver. No se decide si
Capa 1 y Capa 2 se implementan en el mismo ciclo o en sub-fases separadas (candidato natural de
D-116, dado que Capa 1 es independiente y verificable por sí sola).

---

## Próximo documento

`DECISIONES_CASO5C_V1.md` (numeración D-116 en adelante), resolviendo: mecanismo de persistencia
(D-116), información válida como insumo (D-117), semántica de "recomendar" (D-118), umbral de
suficiencia de evidencia (D-119), y formato de la recomendación con su declaración de evidencia
(D-120).
