# Propuesta — Caso 5B: Capa Comparativa de Gestores de Riesgo

Estado: **documento de apertura — previo a cualquier decisión o implementación**. Define la
pregunta que responde Caso 5B, sus límites, y las decisiones que deben resolverse antes de tocar
código, siguiendo el mismo ciclo que toda fase anterior: propuesta → decisión → implementación →
pruebas → auditoría → congelamiento.

**Punto de partida**: `caso5/AUDITORIA_CAPACIDAD_COMPARATIVA_V1.md` — verificó contra código que no
existe hoy ningún componente que compare múltiples gestores de riesgo bajo una misma estrategia/
dataset; solo ejecución individual (`EjecutorProtocolo.Ejecutar` recibe una única
`ConfiguracionSizing?` por invocación). La base necesaria (gestores intercambiables, identidad
reproducible, métricas financieras) ya está congelada en `caso5a-v1-experimental`.

---

## 1. Objetivo de Caso 5B

**Pregunta principal**: ¿puede el laboratorio comparar de forma reproducible múltiples gestores de
riesgo aplicados a una misma estrategia, dataset y configuración experimental?

**Incluye**:
- Ejecutar la misma combinación estrategia/dataset/timeframe/configuración económica bajo N
  gestores de riesgo distintos.
- Acumular los resultados de esas N corridas en una sola estructura, sin descartarlos entre
  corridas.
- Comparar las métricas ya existentes (`MetricasFinancieras`, D-111) de esas N corridas, lado a
  lado.
- Producir una salida estructurada (tabla), no solo texto suelto por corrida.

**No incluye** (extiende las exclusiones ya fijadas en `PROPUESTA_CASO5_V1.md` §7 y confirmadas
por `AUDITORIA_CAPACIDAD_COMPARATIVA_V1.md` §5):
- Recomendar automáticamente cuál gestor usar.
- Optimización o calibración de ningún parámetro.
- Aprendizaje automático o reglas de decisión.
- Selección automática de gestor por ninguna estrategia o corrida.
- Ranking de superioridad entre gestores — mismo principio que D-014/D-047/D-076 ya aplican a
  timeframes y regímenes: comparar no implica declarar un ganador.

El recomendador (lo que en el mapa de evolución V3 se llamó "Caso 5B/6") queda como consecuencia
posterior, condicionada a que esta capa produzca evidencia comparable suficiente — no se construye
en esta fase.

---

## 2. Punto de partida congelado

Verificado contra código, no reconstruido de memoria (mismo criterio D-057):

- **`IGestorRiesgo`** (`src/Domain/Portfolio/IGestorRiesgo.cs`, D-108): único método, calcula
  cantidad — los 3 gestores congelados (`GestorFixedFractional`, `GestorFixedRisk`,
  `GestorVolatilitySizing`) son intercambiables vía `ConfiguracionSizing.GestorActivo`.
- **`IIdentidadGestorRiesgo`** (D-109, precisión): cada gestor declara una identidad determinista y
  estable (`"fixed-fractional:v1:riesgo=0.1"`, etc.) — precondición necesaria para poder atribuir
  con certeza una fila de comparación a un gestor específico, sin ambigüedad.
- **`IdentidadExperimentoCompleta`/`HashConfiguracionEconomica`**: distingue de forma determinista
  dos corridas que solo difieren en el gestor activo.
- **`MetricasFinancieras`** (D-111): `ProfitFactor`, `CapitalLibreMinimo`, `DrawdownMaximoPct`,
  `ExposicionMaxima` (=`MargenMaximoUtilizado`), `PnLTotal`, `CashFinal`, `EquityFinal` — ya
  calculadas desde una única fuente oficial (D-072/D-077), listas para comparar sin recalcular
  nada.
- **`EjecutorProtocolo.Ejecutar`** (`exploration/laboratorio/protocolo/EjecutorProtocolo.cs:82`):
  único punto de entrada real, recibe una `EntradaProtocolo` con un `ConfiguracionSizing?` — Caso
  5B no reemplaza este método, lo invoca N veces (una por gestor) desde el componente nuevo.

**Precedente arquitectónico directo, no anticipado en `AUDITORIA_CAPACIDAD_COMPARATIVA_V1.md`**:
`ComparadorMultiTimeframe` (`exploration/laboratorio/analisis_multitimeframe/
PerfilMultiTimeframe.cs:40`) ya resuelve un problema estructuralmente idéntico — comparar N
resultados de una misma estrategia que solo difieren en **un eje** (ahí: timeframe; aquí: gestor de
riesgo). Su forma: `Comparar(estrategia, perfilesEnOrden) → PerfilMultiTimeframe { Filas,
Consistencia, MejorResultadoObservado, MayorEvidencia }`. Principios que ya aplica y que Caso 5B
debería heredar sin reabrir la pregunta:
- **Orden de entrada = orden de presentación** — nunca reordena por valor de métrica, evita
  ranking implícito (mismo principio D-014/D-047 que Caso 5B ya declaró en §1).
- **"Mejor resultado observado" es una observación puntual, no una recomendación** — se reporta
  sin implicar que ese eje "gane" en general.
- **No recalcula ninguna métrica** — solo agrupa y presenta lo que `AnalizadorOperacional`/
  `CalculadoraMetricasFinancieras` ya calcularon.

**Diferencia relevante que impide una reutilización literal**: `ComparadorMultiTimeframe` consume
`ReporteOperacional`, acoplado a `ResolucionDeIntentos`/martingala (D-055) — inadecuado como fuente
única para comparar gestores, ya que 4 de las 6 estrategias no usan martingala. Un comparador de
gestores debería anclarse en `MetricasFinancieras` como fuente principal (ya señalado como
advertencia en `PROPUESTA_CASO5_V1.md` §3, no resuelta hasta ahora) — la decisión de qué fuente(s)
usar es candidata a D-113/D-114 (ver §4).

---

## 3. Arquitectura candidata (sin seleccionar)

**Opción A — Ejecución múltiple dentro del protocolo**: `EjecutorProtocolo` (o una variante) recibe
una lista de gestores en vez de un único `ConfiguracionSizing?`, itera internamente.
```
EjecutorProtocolo
    |
    +-- Gestor A
    +-- Gestor B
    +-- Gestor C
```
Ventaja: un solo punto de entrada. Riesgo: mezcla la responsabilidad de "ejecutar una corrida" con
"orquestar múltiples corridas" — `EjecutorProtocolo.Ejecutar` ya orquesta multi-timeframe (líneas
84-98) con una estructura interna no trivial; agregarle un segundo eje de iteración (gestor) además
del ya existente (timeframe) puede producir una combinatoria de responsabilidades no evaluada.

**Opción B — Ejecutor comparativo separado**: un componente nuevo, mismo patrón exacto que
`ComparadorMultiTimeframe`, que invoca `EjecutorProtocolo.Ejecutar` una vez por gestor y agrega los
resultados.
```
ComparadorGestores
        |
        +-- invoca EjecutorProtocolo.Ejecutar (Gestor A)
        +-- invoca EjecutorProtocolo.Ejecutar (Gestor B)
        +-- invoca EjecutorProtocolo.Ejecutar (Gestor C)
        +-- acumula MetricasFinancieras por gestor
```
Ventaja: no toca `EjecutorProtocolo` ni `EntradaProtocolo` — mismo nivel de aislamiento que ya tiene
`ComparadorMultiTimeframe` respecto a `BacktestRunner` (D-008/D-009, separación de capas). Sigue el
precedente ya existente en el proyecto, en vez de introducir un patrón nuevo.

**Opción C — Capa de laboratorio encima de resultados existentes**: no ejecuta nada — recibe N
`ResultadoProtocolo` ya generados (por quien sea que los generó) y solo los compara.
```
N ResultadoProtocolo ya generados  →  Comparador (solo agrega/compara, no ejecuta)
```
Ventaja: máxima separación entre "generar evidencia" y "comparar evidencia". Riesgo: no garantiza
por construcción que las N corridas comparadas compartan estrategia/dataset/timeframe/configuración
económica salvo el gestor — ese control experimental (§6) tendría que verificarse externamente,
no por diseño del propio componente.

Ninguna opción se selecciona en este documento — corresponde a D-112.

---

## 4. Decisiones futuras esperables (numeración reservada desde D-112)

Ninguna decisión se resuelve en esta propuesta — el siguiente documento
(`DECISIONES_CASO5B_V1.md`) resuelve cada punto con la misma disciplina de fases anteriores.

**D-112 (candidata) — Ubicación arquitectónica de la comparación**: Opción A, B o C de §3 (o una
variante), evaluando explícitamente contra el precedente de `ComparadorMultiTimeframe`.

**D-113 (candidata) — Unidad de comparación**: qué se mantiene fijo y qué varía en una corrida
comparativa (estrategia + dataset + timeframe + configuración económica fijos, gestor variable,
mismo criterio ya fijado en `DECISIONES_CASO5_V1.md` "Criterio adicional — control experimental")
— y si el comparador debe validar ese control por construcción o asumirlo del llamador.

**D-114 (candidata) — Estructura de resultado comparativo**: forma concreta del tipo que acumula N
resultados (campos, si vive junto a `MetricasFinancieras` o en un módulo nuevo), y qué fuente(s)
usa — `MetricasFinancieras` únicamente, o también algo de `AnalizadorOperacional`/
`ReporteOperacional` pese a su acoplamiento a martingala (advertencia heredada de
`PROPUESTA_CASO5_V1.md` §3).

**D-115 (candidata) — Formato de salida**: tabla en texto (mismo estilo que
`ReporteFinancieroGenerador`), estructura de datos consumible por otro componente, o ambas — y si
incluye alguna forma de "observación puntual" (mismo patrón que `MejorResultadoObservado` de
`ComparadorMultiTimeframe`) sin que constituya una recomendación.

---

## 5. Restricciones heredadas (sin relajar)

- **`IStrategy` y las 6 estrategias existentes intactas** — ninguna estrategia recibe ni conoce
  qué gestor está activo, ni antes ni después de esta fase.
- **`GestorCapital`, `IGestorRiesgo`, `ConfiguracionSizing` sin modificación de contrato** — Caso
  5B consume lo que Caso 5A ya congeló, no lo extiende.
- **Sin mezclar recomendador**: ninguna forma de "elegir automáticamente" ni de declarar un gestor
  superior a otro entra en el alcance de esta fase (§1).
- **Sin optimización ni calibración** de ningún parámetro de ningún gestor (D-030).
- **Sin Kelly ni Masaniello** — siguen fuera, bloqueo metodológico de Caso 2.3 no resuelto (D-110).
- **Reproducibilidad de hashes preservada**: cualquier corrida individual que el comparador invoque
  debe seguir produciendo el mismo `HashCompuesto`/`HashConfiguracionEconomica` que produciría
  fuera del comparador — el comparador agrega evidencia, no la altera.
- **Ningún baseline congelado se toca** (`caso1` a `caso5a-v1-experimental`).

---

## 6. Criterios de éxito iniciales

El sistema debe poder responder, con evidencia estructurada y no solo con lectura manual de
múltiples salidas de texto:

> "Para esta misma estrategia + dataset + timeframe, ¿cómo se comportan Fixed Fractional, Fixed
> Risk y Volatility Sizing?"

mostrando lado a lado, para cada gestor:
- identidad de configuración (`IIdentidadGestorRiesgo.ObtenerIdentidadConfiguracion()`);
- métricas financieras relevantes (mínimo: `PnLTotal`, `DrawdownMaximoPct`, `ProfitFactor`,
  `ExposicionMaxima`);
- confirmación de que estrategia, dataset, timeframe y configuración económica fueron idénticos
  entre las corridas comparadas (control experimental, §5).

Sin producir, en ningún punto de esta salida, una conclusión de "cuál es mejor" — solo evidencia
comparable, mismo límite que D-014/D-047/D-076 ya establecen para timeframes y regímenes.

---

## Fuera de alcance de este documento

No se implementó código. No se selecciona ninguna de las 3 opciones de §3. No se resuelve D-112 a
D-115 — solo se declara su existencia y el problema que cada una debe resolver. No se diseña ningún
recomendador ni criterio de selección automática.

---

## Próximo documento

`DECISIONES_CASO5B_V1.md` (numeración D-112 en adelante), resolviendo: ubicación arquitectónica
(D-112), unidad de comparación (D-113), estructura de resultado comparativo (D-114), formato de
salida (D-115).
