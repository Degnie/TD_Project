# Deuda Técnica — Caso 1: Laboratorio de Estrategias

Estado: **documento maestro — Fase 1.7, Paso 2 del Caso 1** (autorizado por D-056, Auditoría de
revisión Fase 1.7). Regla de este documento: **documentar, no resolver**. Ninguna de las
limitaciones listadas aquí se corrige en este paso — quedan registradas como conocidas y
conscientes, disponibles para que una fase futura decida si/cuándo resolverlas.

---

## 1. Modelo económico — fuera de Caso 1, pendiente de Caso 2

El laboratorio no implementa modelo económico real: sin ROI comparable entre estrategias, sin
Sharpe, sin costes de transacción, sin slippage, sin gestión de capital variable, sin Masaniello,
sin sizing dinámico (D-002, D-009). `EquityInicial`/`EquityFinal`/`RetornoPct` existen en las
fichas y reportes, pero están explícitamente etiquetados como "datos derivados del modelo actual,
no comparables financieramente" — nunca se presentan como respuesta a "¿cuánto dinero genera?".

**Qué falta para resolverlo**: un modelo financiero explícito (Caso 2) — no forma parte del
alcance de este documento ni de Fase 1.7.

## 2. Métricas financieras "no oficiales"

Toda cifra con apariencia financiera (equity, retorno %) en el laboratorio actual proviene de un
modelo de posición fijo (tamaño=1, capital inicial=1000, sin reinversión real) que nunca fue
diseñado ni validado como modelo económico — es un subproducto del motor de backtesting, no una
métrica financiera oficial. Ningún reporte generado la usa para comparar estrategias entre sí
(D-009, D-014).

**Qué falta para resolverlo**: definición formal de qué constituye una métrica financiera oficial,
y bajo qué modelo económico se calcula (Caso 2).

## 3. Métricas dependientes de estructura de estrategia (D-055)

El catálogo de métricas de resolución de intentos (`GanoInicial`/`GanoM1`/`GanoM2`/
`PctResueltasPorMartingala`/`PerdioAgotando`) asume implícitamente que toda estrategia usa
martingala. Confirmado con evidencia real: EMA Cross (Fase 1.6-D, estrategia sin martingala)
produce `GanoM1=0`/`GanoM2=0`/`%Marting=0.0%` en las 6 corridas del dataset real
(`catalogo_estrategias/EMA_CROSS.md`) — no porque la estrategia "no necesitó escalar" (lectura
válida para una estrategia con martingala), sino porque el concepto no aplica. La partición
exhaustiva sigue siendo matemáticamente correcta (suma 100%), pero una porción del catálogo queda
sin información interpretable para estrategias de este tipo.

**Decisión explícita de no resolver ahora**: registrada en D-055 — "Fase 1.6-D busca validar el
pipeline, no rediseñar el modelo de métricas... Una futura fase puede resolver: métricas
aplicables según tipo de estrategia." No se modificó `ReporteConsolidadoGenerador.cs`, el catálogo
de métricas ni ningún contrato existente.

**Qué falta para resolverlo**: un catálogo de métricas condicionado al tipo de estrategia (con
martingala / sin martingala, u otra taxonomía), o una forma de declarar explícitamente "esta
métrica no aplica" en vez de mostrar 0.

## 4. Clasificaciones no resueltas de fondo

- **D-044** (dimensión principal de agrupación del reporte por régimen): D-048 evitó el problema
  mostrando entrada y resolución como vistas separadas, ninguna declarada "principal" — la
  pregunta original (¿cuál importa más para interpretar una estrategia?) sigue abierta.
- **D-018** (umbral numérico de régimen fuera de ADX/SesgoDI): no aplica al clasificador oficial
  congelado (`ClasificadorRegimenV1`, ya tiene los suyos), pero sigue abierto para cualquier
  clasificador experimental futuro que use otro enfoque.
- **D-011** (métrica principal de comparación multi-timeframe distinta de Eficiencia operacional):
  `ComparadorMultiTimeframe` sigue usando solo Eficiencia operacional — ninguna alternativa
  evaluada ni implementada.
- **D-012** (umbral de "muestra reducida"): ningún módulo marca o filtra automáticamente por
  tamaño de muestra pequeño — la responsabilidad de notar esto (ej. EMA Cross en 1D con 9
  operaciones) recae en quien lee el reporte, no en el sistema.
- **D-020** (categoría "Indeterminado" distinta de "Ambiguo"): `Escenario` sigue con 4 valores
  únicamente (Alcista/Bajista/Lateral/Ambiguo) — no existe una categoría separada para "el
  clasificador no pudo evaluar" vs. "el clasificador evaluó y el resultado es ambiguo".
- **D-019** (tamaño de ventana de clasificación más allá del periodo de ADX ya fijado): sin
  explorar — el único tamaño de ventana usado es el que ya fija el cálculo de ADX congelado.
- **D-013** (cobertura de timeframes evaluados más allá de los 6/12 actuales): sin extender.

## 5. Automatización futura — ideas registradas, no implementadas

- **Ejecución batch**: correr el protocolo sobre múltiples estrategias/timeframes en una sola
  invocación (hoy `Program.cs` fija una estrategia y una lista de timeframes por corrida,
  requiere edición manual para cambiar de estrategia).
- **Almacenamiento histórico**: las carpetas de `resultados/{Estrategia}_{timestamp}/` se
  acumulan en disco sin índice ni comparación entre ejecuciones pasadas — no hay forma
  automática de listar o comparar corridas anteriores.
- **Interfaz de usuario**: toda interacción es vía consola/archivos — sin UI para lanzar
  corridas, explorar reportes o comparar estrategias.

Ninguna de estas tres ideas tiene diseño ni especificación — son notas de dirección posible, no
compromisos de fase futura.

---

## Fuera de alcance de este documento

Este documento no resuelve ninguna de las limitaciones listadas. No se modifica código, no se
abren documentos de especificación nuevos para estos puntos, no se abre ninguna discusión de
Masaniello, sizing, riesgo financiero, capital, costes ni simulación monetaria — conforme a la
restricción explícita de Fase 1.7.

---

## Próximo documento de esta fase (no incluido aquí)

- `VERSION_EXPERIMENTAL_CASO1_V1.md` — definición formal de la versión congelada (punto 5 del
  alcance de Fase 1.7).

---

## Criterio de cierre de este documento (Paso 2 de Fase 1.7)

- ✓ Modelo económico incompleto registrado como fuera de Caso 1 (sección 1).
- ✓ Métricas financieras no oficiales registradas explícitamente (sección 2).
- ✓ D-055 (métricas dependientes de martingala) registrado con evidencia real del dataset
  (sección 3).
- ✓ Clasificaciones no resueltas de fondo listadas una por una, sin resolverlas (sección 4).
- ✓ Ideas de automatización futura registradas sin diseño ni compromiso (sección 5).
- ✓ Ningún cambio de código — verificado (`git status --porcelain -- src/ tests/` vacío).
- ⏳ Auditoría revisa este documento — pendiente de confirmación antes de generar
  `VERSION_EXPERIMENTAL_CASO1_V1.md`.
