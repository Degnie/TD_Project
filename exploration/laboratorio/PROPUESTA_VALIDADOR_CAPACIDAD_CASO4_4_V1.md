# Propuesta — Caso 4.4: ValidadorCapacidad (Observación vs. Bloqueo Económico)

Estado: **documento de apertura — previo a cualquier decisión o implementación**. Última sub-fase
prevista en `PROPUESTA_CASO4_V1.md` §6 (punto 3, "Revisión de `ValidadorCapacidad`"). No abre
implementación. No resuelve D-059/D-060 — solo evalúa si la corrección dimensional de Caso 4.1-4.3
cambia el criterio que las originó.

**Punto de partida**: `AUDITORIA_CASO4_3_UNIDADES_EXPOSICION_V1.md` — D-084/D-085/D-091-D-095
resueltas, ninguna deuda bloquea usar Caso 4.1-4.3 como referencia estable para esta sub-fase.

---

## 1. Objetivo de Caso 4.4

**Pregunta principal**: ahora que `Cantidad` es dimensionalmente correcta (D-093/D-094/D-095), ¿el
significado operativo de "incapacidad detectada" sigue siendo el correcto, o la corrección
dimensional cambia lo que esa detección debería hacer?

**No busca**:
- Decidir automáticamente que el motor debe bloquear órdenes.
- Optimizar ni recomendar niveles de capital.
- Resolver D-055 ni D-044 (fuera de alcance de Caso 4).

Caso 4.4 evalúa **si la pregunta de D-059/D-060 cambió**, no reabre esas decisiones para
reinterpretarlas sin evidencia nueva — mismo principio que rigió D-089 (Caso 3: una decisión
anterior puede activarse por evidencia nueva, sin asumir su resultado de antemano).

---

## 2. Punto de partida congelado — evidencia verificada en código

**`ValidadorCapacidad.Validar`** (`src/Domain/Broker/ValidadorCapacidad.cs:10-15`): retorna `bool`,
sin efecto secundario. Compara `CashDisponiblePrevio = Cash − compromisosVigentes` contra la
reserva calculada por `CalculadoraReservaPreventiva.Calcular` (`request.Cantidad × precioBase ×
TasaMargen`, `CalculadoraReservaPreventiva.cs:19`).

**`RegistroIncapacidad`** (`src/Domain/Broker/RegistroIncapacidad.cs:7-11`): record puramente
observacional — `Timestamp`, `Request`, `ReservaRequerida`, `CashDisponible`. Nunca bloquea ni
altera la orden (D-059/D-060, ya congeladas, no se reabren aquí).

**Hallazgo relevante para esta propuesta, verificado por búsqueda exhaustiva**: `Incapacidades`
(el campo opcional de `ResultadoBacktest`, `src/Application/ResultadoBacktest.cs:19`) **no es
consumido por ningún componente de `exploration/`** — ni `EjecutorProtocolo`, ni
`ReporteFinancieroGenerador`, ni `ReporteConsolidadoGenerador`, ni `MetricasFinancieras`. El dato
se calcula en `BacktestRunner` (`src/Application/BacktestRunner.cs:65-69`) y queda huérfano: existe
en el resultado del motor, pero ningún artefacto experimental del laboratorio lo lee, lo muestra ni
lo reporta. Esto es distinto de "decidido que no se muestra" — es simplemente que ningún documento
de Caso 1/Caso 2/Caso 3 abordó esta pregunta, porque hasta Caso 4 nunca hubo sizing activo en una
corrida congelada (todos los baselines usan `Sizing=null`, y sin sizing activo la desproporción
`Cantidad`/`CapitalInicial` que D-085 documentó hacía que la incapacidad fuera casi el estado
normal, no una señal útil).

**Consecuencia de este hallazgo**: la pregunta de Caso 4.4 no es únicamente "¿debe bloquear?" —
hay una pregunta previa más básica: **¿debe siquiera mostrarse?** Con `Cantidad` ahora
dimensionalmente correcta (D-093/D-094/D-095), una corrida con sizing activo produce menos
incapacidades espurias que antes — la señal que `RegistroIncapacidad` produce hoy es, por primera
vez desde que existe (Caso 2, D-059), potencialmente informativa en vez de ruido casi constante.

---

## 3. Qué cambió realmente entre Caso 2 y Caso 4.3

| | Caso 2 (D-059/D-060 original) | Caso 4.3 (post D-093/D-094/D-095) |
|---|---|---|
| `Cantidad` bajo sizing activo | Dimensionalmente incorrecta (mezcla monetario/activo) | Dimensionalmente correcta |
| Frecuencia esperada de incapacidad | Alta — casi cualquier corrida con sizing activo la disparaba, por el defecto de unidades | Depende del capital/parámetros reales del experimento, ya no de un defecto estructural |
| Utilidad de `RegistroIncapacidad` como señal | Baja — mezclada con ruido del defecto dimensional | Potencialmente alta — ahora refleja escasez de capital real, no un error de conversión |

**Esto no decide automáticamente bloquear** — decide que la pregunta merece revisión con evidencia
nueva, exactamente el criterio que motivó abrir esta propuesta en vez de asumir una respuesta.

---

## 4. Preguntas que Caso 4.4 debe resolver (sin resolverlas aquí)

**4.1 — ¿Cuál es el objetivo de esta sub-fase?** Cuatro direcciones posibles, no mutuamente
excluyentes, a evaluar en el documento de decisiones:
- Bloqueo real: `ValidadorCapacidad` impide que una orden sin capacidad se ejecute.
- Rechazo de orden: la orden se descarta de la bolsa (similar a `ValidadorBolsaRequests`), sin
  llegar al `Fill`.
- Ajuste de cantidad: la orden se recorta a lo que sí cabe (mismo espíritu que D-095, pero aplicado
  a capacidad en vez de a intención).
- Solo mejorar la clasificación/reporte: `Incapacidades` deja de estar huérfano — se propaga hasta
  `EjecutorProtocolo`/reportes, sin cambiar el comportamiento de ejecución.

**4.2 — ¿Qué contratos se afectan?** Verificado como superficie mínima conocida: `OrderRequest`
(sin campo de capacidad hoy), `BacktestRunner.cs` (punto de evaluación, línea 65), `ResultadoBacktest`
(ya tiene `Incapacidades`, pero nadie lo consume — sección 2), y cualquier reporte de
`exploration/laboratorio/` que decida empezar a leerlo. Ninguna estrategia (`IStrategy`) se ve
afectada bajo ninguna de las 4 direcciones de 4.1 (P-002 se mantiene: la estrategia no necesita
saber si su orden tuvo capacidad).

**4.3 — ¿Cómo convive con D-059?** D-059/D-060 ya establecieron "observar, no bloquear" como
decisión congelada de Caso 2 — reabrirlas requiere evidencia de que el contexto cambió, no solo
preferencia nueva. Tres posturas a evaluar:
- Mantener modo observación como único comportamiento (D-059 permanece intacta, Caso 4.4 solo
  conecta el dato huérfano a un reporte).
- Introducir un modo estricto **experimental**, activable explícitamente (mismo patrón D-061:
  parámetro opcional con default = comportamiento histórico), sin cambiar el default.
- Cambiar el comportamiento por defecto — requeriría evidencia mucho más fuerte, dado que rompería
  compatibilidad con cualquier corrida futura que no active el modo nuevo explícitamil, y
  contradice el patrón de "extensión opcional" usado en toda la evolución de Caso 2/Caso 4 hasta
  ahora.

**4.4 — ¿Cómo se preserva compatibilidad?** Mismo criterio que toda sub-fase anterior: baselines
congelados (Caso 1, Caso 2, Caso 3A) sin regenerar ni alterar; `Sizing=null` sin cambio de
comportamiento; ninguna corrida histórica cambia de resultado salvo que active explícitamente la
funcionalidad nueva.

---

## 5. Candidatos de dirección (sin selección — para discusión del documento de decisiones)

- **A — Mínimo viable**: conectar `Incapacidades` (ya calculado, hoy huérfano) a
  `EjecutorProtocolo`/un reporte — sin tocar `ValidadorCapacidad` ni el comportamiento de
  ejecución. Resuelve la parte más segura del hallazgo de la sección 2 sin abrir la pregunta de
  bloqueo.
- **B — Modo estricto experimental**: agregar un modo opcional donde `ValidadorCapacidad` sí
  impide la ejecución, activable solo por configuración explícita nueva — D-059 permanece como
  default.
- **C — Ajuste de cantidad por capacidad**: extender la lógica de normalización de D-095 (que ya
  ajusta `Cantidad` contra la posición real) para también ajustarla contra la capacidad
  disponible — mayor similitud arquitectónica con lo que Caso 4.1-4.3 ya construyó, pero mayor
  superficie de cambio y de preguntas nuevas (¿reducir a cero es rechazar? ¿reducir parcialmente
  reintroduce D-085 de otra forma?).

---

## 6. Exclusiones

- No se decide si D-059/D-060 cambian de valor por defecto — cualquier cambio de comportamiento
  requiere activación explícita (sección 4.3).
- No se recalibra `CapitalInicial` ni ningún `PorcentajeRiesgo` de ningún experimento.
- No se resuelve D-055 ni D-044 — fuera de alcance de Caso 4 completo.
- No se optimiza ni recomienda capital — mismo principio que excluyó optimización en toda fase
  anterior (D-002, D-014/D-047/D-076).

---

## 7. Decisiones nuevas

Numeración reservada desde **D-096**. Ninguna decisión se resuelve dentro de esta propuesta.

---

## 8. Criterios de cierre de Caso 4.4

- ¿`Incapacidades` dejó de estar huérfano, o se decidió explícitamente que debe permanecer así?
- ¿D-059/D-060 se reabrieron con evidencia suficiente, o se confirmó que siguen siendo la decisión
  correcta bajo el nuevo contexto dimensional?
- ¿Qué dirección (A/B/C de la sección 5, u otra) se seleccionó, con qué criterio?
- ¿Los 3 baselines congelados y `Sizing=null` permanecen sin cambio de comportamiento?

---

## Fuera de alcance de este documento

No se implementó código. No se modifica `ValidadorCapacidad.cs`, `RegistroIncapacidad.cs`,
`ResultadoBacktest.cs`, `BacktestRunner.cs`, ni ningún reporte de `exploration/`. No se selecciona
ninguna dirección de la sección 5 — queda para el documento de decisiones siguiente.

---

## Próximo documento

`DECISIONES_VALIDADOR_CAPACIDAD_CASO4_4_V1.md` (numeración D-096 en adelante), resolviendo: si
Caso 4.4 se abre con alcance completo o se limita a la dirección A (mínimo viable) como primer
paso incremental, y si D-059/D-060 requieren revisión formal o se confirman intactas.
