# Parametrización del Clasificador de Régimen — ADX+DI V1

Estado: **especificación de parámetros — Fase 1.4-B, Paso 2**. Resuelve reglas y valores, no
implementa código. Objetivo de esta fase: definir cómo pasa ADX+DI de "candidato experimental" a
"candidato oficializable" — no lo convierte todavía en `ClasificadorRegimenV1` (eso es el Paso 3,
posterior y condicionado a la aprobación de este documento).

Criterio de aprobación exigido: dado el mismo conjunto OHLC, dos implementaciones independientes de
este documento deberían producir la misma clasificación — ninguna regla puede quedar librada al
criterio del desarrollador que la implemente.

---

## 1. Identidad del clasificador

- **Clasificador candidato**: ADX + DI (Wilder)
- **Versión**: Propuesta v1 (sujeta a aprobación — no es `ClasificadorRegimenV1` todavía)
- **Precede a**: `ClasificadorAdxExperimental.cs` (Fase 1.4-A) — este documento define las reglas
  que la versión oficial deberá cumplir; el código experimental es la base técnica pero no es, por
  sí mismo, la versión congelable (produce 3 estados, D-028 exige 4 — ver sección 2.6).
- **Modelo de estados que debe producir** (D-028, aprobado): Alcista, Bajista, Lateral, Ambiguo.

---

## 2. Parámetros a definir

### 2.1 Periodo ADX

| Campo | Valor | Estado |
|---|---|---|
| `PeriodoAdx` | **14** | Propuesto |

**Justificación**: 14 es el periodo original definido por J. Welles Wilder en *New Concepts in
Technical Trading Systems* (1978), la fuente primaria del indicador ADX/DMI. No es un valor elegido
mirando el comportamiento de BTC/USDT — es la definición estándar del indicador tal como fue
publicada, la misma convención ya usada en la implementación exploratoria de Fase 1.4-A. Es
**estándar**, no experimental, en el sentido de que no requiere justificación adicional más allá de
"es la definición original del indicador que se está usando".

### 2.2 Ventana DI

| Campo | Valor | Estado |
|---|---|---|
| `PeriodoDI` | **igual a `PeriodoAdx` (14)** | Propuesto |
| Cálculo | Suavizado de Wilder sobre `DM+`/`DM-`/`TR` (ver fórmula abajo) | Estándar |
| Relación DI+/DI- | Comparación directa: `DI+ > DI-` ⇒ predominio alcista; `DI- > DI+` ⇒ predominio bajista | Estándar |

**Justificación de compartir el periodo con ADX**: en la formulación original de Wilder, DI+/DI- y
ADX comparten el mismo periodo de suavizado — no son dos parámetros independientes en el diseño
estándar del indicador. Separarlos sería una variación no estándar que requeriría su propia
justificación explícita; este documento no la introduce.

**Fórmula** (idéntica a la ya implementada en `ClasificadorAdxExperimental.cs`, aquí formalizada
como regla, no como código):
```
TR_i        = max(High_i - Low_i, |High_i - Close_{i-1}|, |Low_i - Close_{i-1}|)
DM+_i       = (High_i - High_{i-1} > Low_{i-1} - Low_i) AND (High_i - High_{i-1} > 0)
                  ? High_i - High_{i-1} : 0
DM-_i       = (Low_{i-1} - Low_i > High_i - High_{i-1}) AND (Low_{i-1} - Low_i > 0)
                  ? Low_{i-1} - Low_i : 0

TR_suavizado, DM+_suavizado, DM-_suavizado = suavizado de Wilder (periodo 14) sobre TR/DM+/DM-

DI+_i = 100 × DM+_suavizado_i / TR_suavizado_i
DI-_i = 100 × DM-_suavizado_i / TR_suavizado_i
DX_i  = 100 × |DI+_i - DI-_i| / (DI+_i + DI-_i)
ADX_i = promedio de Wilder de DX sobre periodo 14
```

### 2.3 Umbral de fuerza tendencial

| Campo | Valor | Estado |
|---|---|---|
| `UmbralAdxTendencia` | **25** | Propuesto |

**Justificación**: 25 es el umbral convencional citado en la literatura técnica derivada de Wilder
para distinguir "hay tendencia" de "no hay tendencia" (valores por debajo de 20-25 se consideran
comúnmente ausencia de tendencia; por encima de 25, tendencia presente). Igual que el periodo, no
fue ajustado mirando el resultado de BTC/USDT — es una convención externa preexistente. **No se
asume como definitivo**: queda marcado "Propuesto", sujeto a aprobación explícita, no "Pendiente"
sin valor, porque sí existe un criterio externo objetivo que lo respalda (a diferencia de D-018 en
general, donde no había ninguna convención externa disponible).

### 2.4 Dirección

| Condición | Escenario |
|---|---|
| `ADX ≥ UmbralAdxTendencia` **y** `DI+ > DI-` | Alcista |
| `ADX ≥ UmbralAdxTendencia` **y** `DI- > DI+` | Bajista |
| `ADX ≥ UmbralAdxTendencia` **y** `DI+ = DI-` (empate exacto) | Ver sección 3 (tratamiento de bordes) |

No hay alternativa considerada aquí — `DI+ > DI-`/`DI- > DI+` es la regla estándar y no admite
variantes razonables sin cambiar el indicador subyacente.

### 2.5 Lateral

**Definición matemática**: `ADX < UmbralAdxTendencia` **y** `|DI+ - DI-|` por debajo de un umbral
de separación (ver 2.6) — es decir, ausencia de fuerza direccional **y** ausencia de sesgo entre
DI+/DI-. Corresponde a la definición conceptual ya fijada en `DEFINICION_ESTADOS_REGIMEN_V1.md §2`:
"ausencia de señal + ausencia de ruido".

### 2.6 Ambiguo — obligatorio, no decorativo

Este es el punto que la implementación exploratoria de Fase 1.4-A **no resolvía** (Riesgo 2 de
`DECISION_CLASIFICADOR_REGIMEN_V1.md`, ahora bloqueante por D-028/D-029).

**Definición matemática propuesta**: `ADX < UmbralAdxTendencia` **y** `|DI+ - DI-| ≥ UmbralSesgoDI`
— es decir, el ADX dice "no hay fuerza de tendencia suficiente", pero DI+/DI- no están cerca entre
sí, indicando que la dirección sigue siendo disputada/inestable dentro de la ventana de cálculo, en
vez de estar en un genuino equilibrio. Corresponde a la definición ya fijada en
`DEFINICION_ESTADOS_REGIMEN_V1.md §2`: "ausencia de señal + presencia de ruido/contradicción".

| Campo | Valor | Estado |
|---|---|---|
| `UmbralSesgoDI` | **No fijado en este documento** | Pendiente |

**Por qué queda pendiente y no propuesto**: a diferencia del periodo ADX (2.1) y el umbral de
tendencia (2.3), no existe una convención externa de literatura ampliamente citada para separar
"Lateral" de "Ambiguo" dentro de la zona de ADX bajo — esa distinción es una extensión de este
laboratorio (D-028), no parte del indicador ADX/DMI estándar de Wilder. Proponer un valor aquí sin
respaldo externo caería en el mismo riesgo que D-018 ya identificó para otros candidatos: fijar un
número mirando el comportamiento ya conocido de BTC/USDT. Este documento dimensiona el problema
matemáticamente (la fórmula existe) pero **no fija `UmbralSesgoDI`** — queda como decisión pendiente
adicional antes de implementar, distinta de D-018/D-019 pero de la misma naturaleza.

---

## 3. Tratamiento de bordes

| Caso | Regla |
|---|---|
| `ADX` exactamente igual a `UmbralAdxTendencia` | Se incluye en la rama "hay tendencia" (`ADX ≥ Umbral`, no `ADX > Umbral`) — evita que un valor límite quede sin clasificar por partición no exhaustiva. |
| `DI+` exactamente igual a `DI-` (empate exacto) mientras `ADX ≥ UmbralAdxTendencia` | Se clasifica como **Ambiguo**, no como Alcista ni Bajista — hay fuerza de tendencia confirmada por ADX, pero la dirección no está determinada; forzar un lado arbitrariamente (ej. "Alcista por defecto") introduciría un sesgo direccional no justificado por los datos. |
| `TR_suavizado = 0` (rango verdadero nulo, ej. vela sin movimiento) | Ventana excluida del cálculo de esa iteración — misma regla ya implementada en `ClasificadorAdxExperimental.cs` (guarda contra división por cero), no requiere cambio. |
| Ventana de calentamiento (primeras `2 × PeriodoAdx` velas) | Sin clasificación — no hay suficientes datos para un ADX válido. Consistente con la regla general de "ninguna ventana se descarta silenciosamente" (`ESPECIFICACION_ANALISIS_ESCENARIOS_MERCADO_V1.md §5`): estas velas no se fuerzan a ninguna categoría, se documentan explícitamente como fuera de cobertura por insuficiencia de historia, no como "Ambiguo" (son conceptualmente distintas — la primera es limitación de arranque, no de evidencia de mercado). |

---

## 4. Dependencia del timeframe

**No se asume equivalencia automática entre escalas.** El periodo 14 (velas) representa una ventana
temporal distinta según el timeframe:

| Timeframe | `PeriodoAdx = 14` equivale a |
|---|---|
| 1m | 14 minutos |
| 15m | 3.5 horas |
| 1h | 14 horas |
| 1D | 14 días |

Esto es una propiedad conocida y aceptada del enfoque (ya señalada como riesgo del candidato A/EMA
en la evaluación, y aplicable en menor medida a cualquier indicador basado en un periodo fijo de
velas). **Este documento no ajusta el periodo por timeframe** — usar el mismo `PeriodoAdx = 14` en
los 13 timeframes disponibles es la propuesta, consistente con que 14 es la definición estándar del
indicador, no un parámetro libre a recalibrar por escala. Si la evidencia de una futura validación
mostrara que esto degrada la calidad de clasificación en timeframes extremos (1m o 1W), sería una
observación a registrar para una versión posterior (`v2`), no una razón para ajustar `v1` ahora sin
evidencia.

**Nota de consistencia con Fase 1.4-A**: la evaluación ya ejecutada (`RESULTADO_EVALUACION_CLASIFICADORES_REGIMEN_V1.md`)
usó `PeriodoAdx = 14` uniforme en los 6 timeframes evaluados y mostró la menor amplitud de variación
entre escalas de los 3 candidatos (1.26pp) — evidencia a favor de no diferenciar el periodo por
timeframe, aunque esa evaluación se hizo con el modelo de 3 estados, no de 4 (D-028 es posterior).

---

## 5. Parámetros oficiales vs. exploratorios (D-022)

| Parámetro | Valor | Estado |
|---|---|---|
| Periodo ADX | 14 | Propuesto (estándar Wilder) |
| Periodo DI | 14 (= Periodo ADX) | Propuesto (estándar Wilder) |
| Umbral de tendencia (ADX) | 25 | Propuesto (convención de literatura) |
| Regla de dirección | `DI+ > DI-` / `DI- > DI+` | Propuesto (estándar, sin alternativa considerada) |
| Regla de Lateral | `ADX < 25` y `\|DI+-DI-\| < UmbralSesgoDI` | Propuesto (fórmula), **valor de `UmbralSesgoDI` pendiente** |
| Regla de Ambiguo | `ADX < 25` y `\|DI+-DI-\| ≥ UmbralSesgoDI` | Propuesto (fórmula), **valor de `UmbralSesgoDI` pendiente** |
| `UmbralSesgoDI` | — | **Pendiente** (sección 2.6 — sin convención externa disponible) |
| Empate ADX = Umbral | Incluido en "hay tendencia" | Propuesto |
| Empate DI+ = DI- con tendencia | Ambiguo | Propuesto |
| Periodo por timeframe | Uniforme (14 velas en todos) | Propuesto |

**Ningún valor de esta tabla es oficial todavía** — "Propuesto" significa que tiene una
justificación explícita y puede pasar a Paso 3 si se aprueba; no significa que ya esté congelado.

---

## Fuera de alcance (respetado)

No se implementa código en este documento. No se ejecuta ninguna evaluación contra estrategias. No
se ajusta ningún valor mirando el resultado ya conocido de BTC/USDT — los valores propuestos
(periodo 14, umbral 25) provienen de convención externa (Wilder, literatura técnica), no de
observación del dataset.

---

## Criterio de cierre del Paso 2

- ✓ Identidad del clasificador candidato definida (sección 1).
- ✓ Los 6 parámetros pedidos resueltos con valor + justificación + estado (sección 2), con
  `UmbralSesgoDI` explícitamente marcado pendiente por ausencia de convención externa, no omitido.
- ✓ Ambiguo definido matemáticamente, no decorativo (sección 2.6).
- ✓ Tratamiento de bordes definido para los 4 casos identificables (sección 3).
- ✓ Dependencia del timeframe abordada explícitamente, sin asumir equivalencia automática (sección
  4), con referencia a evidencia ya generada en Fase 1.4-A.
- ✓ Tabla de parámetros oficiales vs. exploratorios completa (sección 5), consistente con D-022.
- ⏳ Auditoría de esta parametrización — pendiente antes de proceder al Paso 3 (creación de
  `ClasificadorRegimenV1`). El valor de `UmbralSesgoDI` deberá resolverse (por convención externa,
  si existe, o por decisión explícita documentada) antes de que el Paso 3 pueda completarse.
