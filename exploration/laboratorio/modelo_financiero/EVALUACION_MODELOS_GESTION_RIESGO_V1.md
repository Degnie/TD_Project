# Evaluación de Modelos de Gestión de Capital Candidatos — V1

Estado: **especificación de subfase — Caso 2.3 del Caso 2**. Documento de diseño de la metodología
de comparación, no implementación de ningún modelo todavía. No se elige ganador en este documento
— se define cómo se compararán los candidatos, mismo patrón que
`EVALUACION_CLASIFICADORES_REGIMEN_V1.md` aplicó a los clasificadores de régimen en Caso 1 (D-021).

Subfase de `ESPECIFICACION_GESTION_CAPITAL_V1.md` (D-067: no elegir modelo de sizing por
comparación directa, sino evaluar candidatos primero).

---

## 1. Objetivo

Determinar qué modelo de gestión de capital será congelado como oficial para Caso 2.3, comparando
**propiedades del modelo como mecanismo de cálculo de tamaño** — nunca comparando qué candidato
produce mejor resultado económico para ninguna estrategia conocida (mismo principio que D-021/D-054
aplicaron en Caso 1: comparar el instrumento, no el resultado).

```
Estrategia (señal: dirección, sin tamaño)
    ↓
Modelo de sizing candidato (A, B, C — sin ejecutar backtest real todavía)
    ↓
Auditoría del modelo (esta fase)
    ↓
Selección y congelamiento de UN modelo oficial
    ↓
(Caso 2.3, continuación) Implementación de la capa de gestión de capital
```

**Restricción heredada de D-069**: ningún candidato se evalúa modificando las estrategias
existentes ni alterando `baseline_final/` — la comparación es sobre el mecanismo en abstracto, no
sobre una corrida real de Tres Mosqueteros/MHI Mayoría/EMA Cross con sizing activado.

---

## 2. Candidatos evaluados

| Candidato | Mecanismo | Estado |
|---|---|---|
| **A — Capital fijo** | `Cantidad` constante, igual al comportamiento actual (`1m` en las 3 estrategias existentes) | 🟢 Candidato base — es el statu quo, no requiere ningún cambio para producirse |
| **B — Porcentaje de capital** | `Cantidad` proporcional a `Cash`/`Equity` disponible en el momento de la orden (ej. `Cantidad = Equity * TasaExposicion`) | 🟡 Candidato — requiere definir qué capital se usa (`Cash` vs `Equity`), en qué momento se mide, y el porcentaje |
| **C — Masaniello** | Tamaño calculado a partir de número de operaciones restantes, probabilidad de acierto estimada y objetivo de capital | 🟡 Candidato experimental — requiere un modelo estadístico que hoy no existe en `src/` ni `exploration/` (confirmado en `ESPECIFICACION_MODELO_ECONOMICO_V1.md` §1: cero menciones de Masaniello en producción) |

No se implementa ninguno de los tres en esta fase — la tabla es el inventario de qué se va a
comparar, no el resultado de la comparación.

---

## 3. Criterios de evaluación

Cada criterio se aplica igual a los 3 candidatos, sobre las mismas preguntas — sin ejecutar ningún
backtest real todavía.

### 3.1 Trazabilidad

**Pregunta**: ¿es posible reconstruir, para una operación dada, exactamente por qué `Cantidad`
tomó el valor que tomó?

- **A — Capital fijo**: trazabilidad máxima — el valor es una constante conocida de antemano, sin
  dependencia de estado.
- **B — Porcentaje de capital**: trazable si se registra el `Cash`/`Equity` exacto en el momento
  del cálculo (mismo principio que `IdentidadExperimentoCompleta`, D-049, exige para toda
  configuración) — requiere que el motor deje evidencia de ese estado, no solo el resultado.
- **C — Masaniello**: trazabilidad depende de que la probabilidad estimada y el número de
  operaciones restantes también queden registrados — más superficie de estado a trazar que B.

### 3.2 Dependencia de supuestos

**Pregunta**: ¿qué debe asumirse como verdadero para que el modelo tenga sentido?

- **A — Capital fijo**: ningún supuesto adicional — funciona igual sin importar el capital.
- **B — Porcentaje de capital**: supuesto único y explícito (el porcentaje elegido), sin necesidad
  de estimar comportamiento futuro.
- **C — Masaniello**: depende de estimar una probabilidad de acierto *antes* de operar — un
  supuesto sobre el futuro que ninguna estrategia del catálogo actual provee de forma validada
  (Tres Mosqueteros/MHI Mayoría/EMA Cross no exponen una probabilidad de acierto declarada, solo
  operan). Introducir Masaniello requeriría inventar ese número o derivarlo de resultados pasados,
  lo cual mezclaría datos históricos con una promesa sobre el futuro — mismo riesgo metodológico
  que D-016 prohíbe para el clasificador de régimen (no usar información no disponible en el
  momento de decidir).

### 3.3 Reproducibilidad

**Pregunta**: ¿dos ejecuciones con la misma entrada producen el mismo resultado?

- **A — Capital fijo**: determinista por construcción.
- **B — Porcentaje de capital**: determinista si `Cash`/`Equity` en el momento del cálculo es
  determinista — ya lo es, verificado en Caso 2.1 (P4/P5 de `ModeloEconomicoBaseTests.cs`/
  `ModeloCostesTests.cs`, mismo hash entre ejecuciones repetidas).
- **C — Masaniello**: determinista solo si la probabilidad de acierto es un valor fijo declarado
  (no estimado en tiempo real a partir de resultados parciales de la misma corrida, lo cual
  introduciría una dependencia circular entre resultado y parámetro).

### 3.4 Compatibilidad con el motor existente

**Pregunta**: ¿qué tan grande es el cambio requerido en `src/` para implementar el candidato?

- **A — Capital fijo**: cero cambios — es el comportamiento actual.
- **B — Porcentaje de capital**: requiere que el punto donde se construye `OrderRequest` (hoy
  dentro de cada `Strategy`) tenga acceso a `Cash`/`Equity` — no existe ese acceso hoy
  (`DataSlice`, que es lo único que `Strategy.Observar` recibe, no expone estado del portfolio,
  confirmado por lectura de `src/Domain/Strategy/`). Depende de cómo se resuelva D-066.
- **C — Masaniello**: mismo requisito de acceso a capital que B, más un modelo estadístico
  completo (probabilidad, número de operaciones, objetivo) que no existe en ningún lado del
  código — mayor superficie de implementación.

### 3.5 Capacidad de explicar al usuario

**Pregunta**: ¿puede describirse el mecanismo en una frase simple, sin ambigüedad?

- **A — Capital fijo**: "cada operación usa la misma cantidad, sin importar el capital actual" —
  sin ambigüedad.
- **B — Porcentaje de capital**: "cada operación usa un X% del capital disponible al momento de
  abrirla" — clara, siempre que se declare explícitamente qué capital (`Cash` vs `Equity`) y en
  qué momento se mide.
- **C — Masaniello**: requiere explicar probabilidad estimada, número de operaciones objetivo y la
  fórmula de progresión — la explicación en sí exige que el usuario entienda un modelo estadístico
  completo, no una regla simple.

---

## 4. Resultado de esta evaluación

**No se selecciona ganador en este documento** — conforme a D-067. La tabla comparativa (Sección
3) queda como evidencia para que la auditoría decida en un documento posterior de decisión, mismo
patrón que Caso 1 separó `EVALUACION_CLASIFICADORES_REGIMEN_V1.md` (comparación) de
`DECISION_CLASIFICADOR_REGIMEN_V1.md` (elección).

---

## Fuera de alcance de este documento

No se implementa ningún candidato. No se modifican estrategias existentes. No se altera
`baseline_final/`. No se selecciona modelo oficial — eso corresponde a un documento de decisión
posterior, después de que la auditoría revise esta comparación.

---

## Próximo paso

Auditoría revisa la comparación y decide: (a) seleccionar un candidato para Caso 2.3 V1, o (b)
pedir criterios adicionales antes de decidir — mismo patrón que
`ANALISIS_RESULTADOS_CLASIFICADORES_REGIMEN_V1.md` siguió a la evaluación de clasificadores en
Caso 1.
