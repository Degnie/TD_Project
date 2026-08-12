# Especificación de Familia de Estrategia — Caso 3 V1

Estado: **documento de diseño — previo a implementación**. Resuelve, bajo el criterio ya aprobado
en D-087 (máxima distancia estructural), qué familia y qué estrategia concreta se implementa
primero de las 2 requeridas por D-086. Resuelve D-090 (ubicación de la metadata de capacidades).
No modifica código en este documento.

---

## 1. Selección de familia bajo D-087

**Perfil estructural verificado de las 3 estrategias existentes** (evidencia de código, no de
memoria):

| Eje | Tres Mosqueteros / MHI Mayoría | EMA Cross |
|---|---|---|
| Origen de la señal | Color de vela — patrón visual puntual sobre 1 vela | Cruce de EMA — indicador acumulado sobre N velas |
| Anclaje temporal | Cuadrantes fijos `N%5`, posición absoluta en el dataset | Ninguno — evalúa cada vela por su propio valor de EMA |
| Gestión de intentos | Martingala con reintentos (`maxMartingalas`) | Ninguna — sin reintentos |
| Horizonte de cierre | Fijo, `2+maxMartingalas` ciclos garantizados | Variable, sin límite — cierra por condición de mercado |
| Estado interno | Fase/contador discreto (`Ninguna`/`EsperandoCierre`/`EsperandoReapertura`) | Acumulador numérico persistente (EMA corta/larga, actualizado incrementalmente) |

**Evaluación de los 3 candidatos de `PROPUESTA_CASO3_V1.md` §6 contra estos 5 ejes** — cuántos ejes
comparte cada candidato con al menos una de las 3 estrategias ya cubiertas (menos ejes compartidos
= mayor distancia estructural = más alineado con D-087):

- **B — Reversión** (ej. bandas de volatilidad, RSI extremo esperando retorno a la media): origen
  de señal por indicador acumulado (comparte eje con EMA Cross), sin cuadrantes, sin martingala
  necesariamente, horizonte variable o fijo según diseño, estado interno numérico similar a EMA
  Cross. **Comparte 2-3 ejes con EMA Cross** — mismo perfil general ("indicador acumulado, sin
  martingala"), solo cambia la lógica de la señal (reversión vs. tendencia). Menor distancia
  estructural real de lo que su nombre sugiere.
- **C — Señal estadística** (ej. z-score de la serie de precios, desviación respecto a una media
  móvil con umbral estadístico): origen de señal por propiedad estadística de la serie (distinto de
  "color de vela" y de "cruce de indicador técnico" — es una tercera categoría), sin cuadrantes,
  sin martingala, horizonte de cierre puede definirse por criterio estadístico (ej. reversión a
  banda de confianza) en vez de por cruce o por ciclo fijo, estado interno puede requerir ventana
  deslizante (media y desviación móviles) — mecanismo de acumulación distinto al de EMA Cross
  (EMA es exponencial de horizonte infinito; z-score típico usa ventana finita deslizante).
  **Comparte como máximo 1 eje** (ausencia de martingala) con lo ya probado — mayor distancia
  estructural verificable.
- **D — Estrategia sin mercado** (señal fija o aleatoria, no reacciona a ninguna condición real del
  dataset): origen de señal nulo/aleatorio (máxima distancia posible en ese eje), sin cuadrantes,
  sin martingala, horizonte de cierre puede fijarse arbitrariamente, sin estado interno relevante.
  **Comparte 0-1 ejes**, pero por una razón distinta a C — no es que explore una lógica de mercado
  nueva, es que no explora ninguna. Máxima distancia estructural, pero de menor valor informativo
  para el objetivo de `PROPUESTA_CASO3_V1.md` §1 (evaluar si el laboratorio generaliza a
  *estrategias de mercado* estructuralmente distintas, no a ausencia de estrategia).

**Selección**: **C — Señal estadística**, primera de las 2 familias requeridas por D-086.

**Motivo**: máxima distancia estructural *significativa* — D no aporta información sobre
generalización de lógica de mercado (es un caso degenerado, útil como control pero no como
validación de generalidad), mientras que B comparte demasiado con EMA Cross para tensionar
supuestos nuevos. C introduce: ventana deslizante (mecanismo de estado no probado hasta ahora),
umbral estadístico como condición de entrada/salida (no visual, no técnico-acumulativo), y una
categoría de indicador (dispersión estadística) ausente en las 3 estrategias existentes.

**D queda como candidato reservado para la segunda familia** (D-086 exige 2) — su valor como
"control de neutralidad" es más útil *después* de tener una segunda estrategia de mercado real
(C) con la que comparar, no como la única familia nueva evaluada.

---

## 2. Estrategia concreta propuesta — "Reversión a la Media por Z-Score"

**Descripción funcional**: calcula media móvil y desviación estándar de `Close` sobre una ventana
deslizante de `N` velas; entra en la dirección opuesta al precio cuando el z-score
(`(Close - Media) / DesviaciónEstándar`) excede un umbral (ej. `|z| > 2`), apostando a que el
precio revierte hacia la media. Cierra cuando el z-score vuelve a cruzar un umbral cercano a `0`
(reversión completada) o cuando alcanza el lado opuesto (falla de reversión, gestionable como
parámetro de diseño — a decidir en la implementación, no en esta especificación).

**Por qué cumple D-087** (verificación cruzada contra el perfil de la sección 1):
- Señal: estadística (z-score), no color de vela ni cruce de EMA — tercera categoría de origen de
  señal en el laboratorio.
- Sin cuadrantes: evalúa cada vela por su propio z-score, igual que EMA Cross, pero el mecanismo de
  cálculo (ventana deslizante con recálculo de desviación) es distinto de un acumulador exponencial.
- Sin martingala: mismo perfil que EMA Cross en este eje (ya cubierto, no es donde C aporta
  distancia — la distancia de C está en el eje de señal/estado, no en gestión de intentos).
- Horizonte: variable, pero con una condición de cierre estructuralmente distinta de EMA Cross
  (reversión a un umbral cercano a la media, no cruce de dos líneas).
- Estado interno: ventana deslizante (requiere mantener las últimas `N` velas o un acumulador
  incremental de suma/suma de cuadrados) — mecanismo de estado nuevo en el laboratorio.

**Parámetros esperados** (a fijar en la implementación, siguiendo D-030: convención externa de
literatura, no calibrados sobre el dataset): `Ventana` (tamaño de la ventana deslizante),
`UmbralEntrada` (z-score que dispara la señal), `UmbralSalida` (z-score de cierre por reversión
completada).

---

## 3. Metadata de capacidades — resolución de D-090

**Selección**: **C — clase de laboratorio**, un registro en código junto a la infraestructura
existente del laboratorio, no en el catálogo `.md` ni en un archivo paralelo por estrategia.

**Motivo**: `EntradaProtocolo.CrearEstrategia` (`protocolo/EjecutorProtocolo.cs`) ya asocia una
estrategia con su fábrica de instanciación en código — agregar `CaracteristicasEstrategia` como un
segundo campo junto a `CrearEstrategia` en el mismo punto de construcción de `EntradaProtocolo`
seguiría el mismo patrón, sin introducir un segundo formato de archivo a mantener sincronizado
(descarta la Opción B de D-088/D-090: archivo paralelo por estrategia) y sin mezclar datos
consumidos por código con documentación legible por humanos (descarta la Opción A: extender las
fichas `.md`).

**Diseño conceptual** (a confirmar en el ciclo de decisión siguiente al implementar, no fijado
aquí como código):

```csharp
public sealed record CaracteristicasEstrategia(bool UsaMartingala);
```

Deliberadamente mínimo — solo el campo que D-088 necesita resolver ahora (`UsaMartingala`).
`UsaSizingPropio`/`UsaEstadoInternoPersistente` mencionados como ejemplo conceptual en D-088 no se
incluyen todavía: no hay una métrica del catálogo actual que dependa de ellos, y agregar campos sin
un consumidor concreto violaría el mismo principio que evitó extender `IStrategy`
innecesariamente — no anticipar estructura sin necesidad demostrada.

**Consumo esperado**: `AnalizadorOperacional.Analizar` (o el generador de reporte que lo consuma)
recibe `CaracteristicasEstrategia` junto al `PerfilMultiTf` ya existente, y usa
`UsaMartingala=false` para mostrar "no aplica" en `ResolucionDeIntentos` en vez de `0.0%` — sin
modificar las fórmulas existentes en `AnalizadorOperacional.cs:62-67`, solo la capa de presentación
(reporte), consistente con el criterio ya aprobado en D-088.

---

## 4. Métricas existentes que aplican

- **`ResultadoGeneral`** (universal): aplica sin cambios — `IntentosCompletados`, `Victorias`,
  `Derrotas`, `EficienciaOperacionalPct` son significativos para cualquier estrategia, incluida la
  de reversión.
- **`ResolucionDeIntentos`**: no aplica (mismo caso que EMA Cross) — con `UsaMartingala=false`
  declarado, el reporte debe mostrar "no aplica" en vez de `0.0%` (D-088/D-090).
- **`PeoresEscenarios`**: aplica — `MayorRachaNegativa`, `MayorExposicionExperimental` son
  significativos independientemente de martingala.
- **`MetricasFinancieras`** (Caso 2): aplica sin cambios — deriva de `ResultadoBacktest`, no de la
  lógica de la estrategia.
- **Métricas por escenario de mercado** (`MetricasPorEscenario`, Caso 1 Fase 1.5): aplica sin
  cambios — opera sobre `InfoOperacionResuelta` y clasificación de régimen, ninguno de los dos
  depende de martingala.

---

## 5. Activación de D-055

**Parcial**. Con esta estrategia (segunda familia sin martingala, tras EMA Cross), D-055 queda
activada en el sentido que D-089 anticipó: hay ahora evidencia suficiente (2 estrategias) para
justificar resolver la presentación de `ResolucionDeIntentos` (vía D-088/D-090, sección 3 de este
documento) como parte del cierre de Caso 3A — pero no se resuelve el catálogo de métricas de fondo
(no se elimina ni se rediseña `ResolucionDeIntentos`, solo se agrega la capa de "no aplica" en el
reporte). El **rediseño completo** del catálogo (Opción C de D-088, "nuevo catálogo de métricas
universales") sigue fuera de alcance de Caso 3A.

---

## Fuera de alcance de este documento

No se implementa código. No se fija el valor final de `Ventana`/`UmbralEntrada`/`UmbralSalida` —
quedan para la implementación, con la misma convención D-030 (referencia externa, no calibración).
No se selecciona la segunda familia (candidato D, reservado) — se abre en un documento posterior,
después de evaluar los hallazgos de la primera integración (mismo ritmo incremental de Caso 1/Caso
2: nunca implementar 2 unidades de trabajo a la vez sin auditar la primera).

---

## Próximo paso

Autorización de implementación de "Reversión a la Media por Z-Score" + `CaracteristicasEstrategia`
(D-090) + ajuste de reporte para "no aplica" (D-088), con pruebas obligatorias antes de cerrar —
mismo patrón que cada sub-fase de Caso 2.
