# Decisiones — ValidadorCapacidad (Caso 4.4, D-096/D-097)

Estado: **D-096 y D-097 resueltas por auditoría — Caso 4.4 listo para especificación de
implementación**. Misma estructura usada en D-001 a D-095 (decisión, opciones, criterio,
evidencia). Ningún código se modifica en este documento — resuelve el orden lógico identificado en
`PROPUESTA_VALIDADOR_CAPACIDAD_CASO4_4_V1.md`: antes de decidir si `ValidadorCapacidad` debe
bloquear, hay que resolver si el dato que ya calcula (`RegistroIncapacidad`) se usa en algo.

Orden de resolución: D-096 primero (uso del registro), D-097 después (qué significa
"incapacidad" una vez que se decide exponerla) — D-097 presupone que D-096 elige alguna forma de
exposición, de lo contrario no hay nada que interpretar.

---

## D-096 — Uso del registro de incapacidades

**Estado**: 🟡 Pendiente de selección por el auditor.

**Decisión**: `RegistroIncapacidad`/`ResultadoBacktest.Incapacidades` ya existen y se calculan en
cada corrida (`BacktestRunner.cs:65-69`), pero ningún componente de `exploration/` los consume
(`PROPUESTA_VALIDADOR_CAPACIDAD_CASO4_4_V1.md` §2, verificado por búsqueda exhaustiva). ¿Qué debe
hacer Caso 4.4 con ese dato: exponerlo en reportes, introducir un modo estricto experimental
opcional, o bloquear por defecto?

**Evidencia de viabilidad de la Opción A**, verificada en código
(`exploration/laboratorio/protocolo/EjecutorProtocolo.cs:122-160`): `resultado1` (el
`ResultadoBacktest` completo devuelto por `BacktestRunner.Ejecutar`, línea 124) ya está disponible
en el mismo scope donde `EjecutarUnTimeframe` arma `ResultadoCorridaTimeframe` — línea 158 ya usa
ese mismo `resultado1` para calcular `metricasFinancieras` con `CalculadoraMetricasFinancieras.
Calcular(resultado1, entrada.CapitalInicial)`. Conectar `resultado1.Incapacidades` a un campo
nuevo de `ResultadoCorridaTimeframe` sigue exactamente el mismo patrón ya congelado (campo
opcional, poblado en la rama Success, mismo criterio D-072/D-077) — no requiere tocar
`BacktestRunner.cs`, `ValidadorCapacidad.cs`, ni ningún componente de `src/`.

### Opciones

- **A — Solo exposición/reporting**: conectar `Incapacidades` (ya calculado) hasta un reporte del
  laboratorio, sin cambiar ejecución. `ValidadorCapacidad`/`BacktestRunner` sin modificación —
  todo el cambio vive en `exploration/` (`EjecutorProtocolo.cs` + un generador de reporte).
  - Ventaja: cero riesgo de regresión sobre `src/`, D-059 permanece intacta sin reabrirse, resuelve
    la parte del hallazgo con menor incertidumbre (el dato huérfano).
  - Riesgo: no responde si alguna vez conviene bloquear — deja esa pregunta para una sub-fase
    futura, si la evidencia expuesta la justifica.
- **B — Modo estricto experimental opcional**: agregar una configuración nueva (mismo patrón D-061:
  parámetro opcional, default = comportamiento histórico) donde `ValidadorCapacidad` sí impide la
  ejecución de una orden sin capacidad, activable solo explícitamente.
  - Ventaja: no cambia comportamiento por defecto, coherente con D-091 (activación experimental
    explícita, default histórico preservado).
  - Riesgo: sin haber observado primero (Opción A) qué frecuencia/contexto tienen las incapacidades
    bajo la `Cantidad` ya corregida, diseñar un modo de bloqueo sería una decisión sin evidencia —
    mismo error que D-095 evitó al no asumir la corrección antes de diagnosticar.
- **C — Bloqueo por defecto**: cambiar el comportamiento histórico de `ValidadorCapacidad` para
  que bloquee siempre. Descartada salvo evidencia extraordinaria — rompería compatibilidad con
  cualquier corrida existente que no active nada explícitamente, contrario al patrón de
  extensión opcional usado en toda la evolución de Caso 2/Caso 4.

### Resolución adoptada

**Selección: A — solo exposición/reporting, como primera fase.** No se implementa B en este
ciclo — queda declarada como evolución posible, condicionada a lo que la evidencia expuesta por A
muestre (frecuencia, contexto, impacto económico de las incapacidades bajo `Cantidad` ya
corregida). Bloquear antes de observar reintroduciría una decisión operativa sin datos — mismo
principio de disciplina que motivó no asumir la causa de ningún hallazgo anterior de este proyecto
sin verificarla primero.

**C rechazada explícitamente**: ningún cambio de comportamiento por defecto sin evidencia
extraordinaria que lo justifique — ninguna existe todavía, dado que ni siquiera se ha observado el
dato en un reporte real.

---

## D-097 — Semántica de incapacidad

**Estado**: 🟢 Aprobada. **Selección: incapacidad = restricción económica observable, no error de
orden.**

**Decisión**: una vez expuesto, ¿qué significa una `RegistroIncapacidad` para quien lee el
reporte? No debe asumirse que "incapacidad" equivale automáticamente a "error" — el dato es una
observación económica, no una falla del pipeline (D-059 ya estableció esto a nivel de
comportamiento; D-097 lo establece a nivel de interpretación en el reporte).

**Evidencia**: `RegistroIncapacidad` (`src/Domain/Broker/RegistroIncapacidad.cs:7-11`) contiene
`Timestamp`, `Request` (la orden que careció de capacidad), `ReservaRequerida`, `CashDisponible` —
datos suficientes para reconstruir *cuánto* faltó, pero el registro no declara *por qué* importa
eso ni qué debería inferir un lector.

### Opciones de interpretación (no mutuamente excluyentes, a discutir)

- **Orden inválida**: la orden en sí es incorrecta o mal formada — descartada como interpretación
  primaria, ya verificado que `ValidadorBolsaRequests` (validación de forma) es un componente
  distinto y anterior en el ciclo (`BacktestRunner.cs:57`, antes de `ValidadorCapacidad`, línea
  65) — una orden puede ser válida en forma y aun así carecer de capacidad.
- **Riesgo elevado**: la estrategia/configuración está pidiendo más exposición de la que el capital
  sostiene — interpretación coherente con D-093 (`PorcentajeRiesgo` como fracción de margen), una
  incapacidad sería el síntoma de un `PorcentajeRiesgo`/`CapitalInicial` desalineados.
- **Falta de capital**: lectura literal — el experimento, tal como está configurado, no tiene
  suficiente `Cash` para la estrategia que se le pide correr. Más una observación sobre el
  experimento que sobre la estrategia o el motor.
- **Información para análisis**: la interpretación más neutral — el dato no prescribe ninguna
  acción, solo permite que un lector humano decida si es relevante para su pregunta de
  investigación, mismo espíritu que D-055 (métricas universales vs. específicas, sin forzar
  significado).

**Criterio a aplicar**: la interpretación elegida condiciona cómo se presenta el dato en el
reporte (una tabla neutral de eventos vs. una advertencia con lenguaje de "riesgo") — debe
resolverse antes de diseñar el formato exacto del reporte que D-096=A produce.

### Resolución adoptada

**Definición oficial**: `RegistroIncapacidad` representa una solicitud operativamente válida cuya
ejecución habría requerido más capacidad económica disponible de la que el modelo permite en ese
momento. No representa error de formato, error de estrategia, ni orden inválida.

**Separación de validadores confirmada como evidencia de la definición**:
`ValidadorBolsaRequests` responde "¿la orden está correctamente formada?";
`ValidadorCapacidad` responde "¿el capital disponible soporta esta orden?" — ambos pueden producir
resultados distintos sin contradicción (`OrderRequest` válida + capacidad insuficiente →
`RegistroIncapacidad`, sin que la orden en sí tenga ningún defecto).

**Tratamiento**: la incapacidad se mantiene como dato económico observable, no como excepción del
motor — no bloquea, no modifica `Fill`, no altera `Trade`, no cambia la trayectoria histórica
(D-059/D-060 permanecen intactas, no reabiertas).

**Consecuencia para el reporte de 4.4**: puede exponer cantidad de incapacidades, tipo, timeframe,
contexto de la corrida — sin afirmar que la estrategia falló, que el resultado es inválido, ni que
la corrida debe descartarse. El lenguaje del reporte debe ser neutral (tabla de eventos), no de
advertencia/alarma.

---

## Resumen de decisiones

| Decisión | Selección | Estado |
|---|---|---|
| D-096 | Solo exposición/reporting como primera fase (Opción A); B queda como evolución posible, no implementada | 🟢 Aprobada |
| D-097 | Incapacidad = restricción económica observable, no error de orden | 🟢 Aprobada |

---

## Fuera de alcance de este documento

No se modifica código. No se implementa la Opción B (modo estricto) en esta sub-fase. No se toca
`ValidadorCapacidad.cs`, `RegistroIncapacidad.cs`, `BacktestRunner.cs` — la exposición vive
enteramente en `exploration/laboratorio/protocolo/EjecutorProtocolo.cs` y un generador de reporte
nuevo o existente.

---

---

## D-098 — Ubicación de las pruebas de Caso 4.4

**Estado**: 🟢 Aprobada e implementada. **Selección: módulo satélite propio
`exploration/laboratorio/caso4/`, mismo patrón que `caso3/Caso3.csproj`.**

**Decisión**: `TestsReporteIncapacidades.cs` requiere `EjecutorProtocolo`/`EstrategiaTresMosqueteros`,
no referenciados en `ModeloFinanciero.csproj` (compartido con `Program.cs`/
`TestsMetricasFinancieras.cs` de Caso 2, congelados). ¿Dónde viven las pruebas de 4.4?

**Motivo**: Caso 4 ya tiene decisiones propias (D-091 a D-098), especificaciones propias, auditoría
propia y evidencia de regresión independiente — mantener un módulo satélite propio es más coherente
que seguir acumulando responsabilidades en `ModeloFinanciero.csproj`, y evita cualquier riesgo de
tocar un `.csproj` compartido con artefactos ya congelados de Caso 2.

**Rechazadas**: ampliar `ModeloFinanciero.csproj` (mezclaría Caso 2 y Caso 4 en el mismo proyecto
de evidencia, reduciendo trazabilidad); runner temporal desechable (las pruebas de 4.4 tienen valor
histórico permanente — `Incapacidades calculadas → expuestas → reportadas` — a diferencia de las
verificaciones puntuales de P8 en 4.2/4.3, que eran diagnósticos de un momento).

**Implementado**: `exploration/laboratorio/caso4/Caso4.csproj` (mismo patrón de `Compile
Include`/`Link` que `caso3/Caso3.csproj`, más `ReporteFinancieroGenerador.cs`), `Program.cs`,
`TestsReporteIncapacidades.cs`.

---

## Resumen de decisiones (completo)

| Decisión | Selección | Estado |
|---|---|---|
| D-096 | Solo exposición/reporting como primera fase (Opción A) | 🟢 Aprobada e implementada |
| D-097 | Incapacidad = restricción económica observable, no error de orden | 🟢 Aprobada |
| D-098 | Módulo satélite propio `caso4/` (mismo patrón que `caso3/`) | 🟢 Aprobada e implementada |

---

## Implementación y verificación (4.4)

**Cambios**: `exploration/laboratorio/protocolo/EjecutorProtocolo.cs` — `ResultadoCorridaTimeframe`
extendido con `IReadOnlyList<RegistroIncapacidad>? Incapacidades = null` (campo opcional al final,
mismo patrón D-072), poblado en la rama Success con `resultado1.IncapacidadesEfectivas`
(`EjecutorProtocolo.cs:166`). `exploration/laboratorio/modelo_financiero/
ReporteFinancieroGenerador.cs` — nueva sección 4 "Restricciones de capacidad observadas
(D-096/D-097)", agrupación por `Side` (Buy/Sell, el único eje que `RegistroIncapacidad` soporta sin
inventar taxonomía), reserva promedio/máxima; secciones 4-5 originales renumeradas a 5-6.

**Pruebas**: `caso4/TestsReporteIncapacidades.cs`, 4/4 (P1 sin incapacidades, P2 con incapacidades
— dato verificado end-to-end desde `ResultadoBacktest` hasta el texto del reporte, P3 determinismo,
P4 regresión de secciones existentes). 126/126 tests de producción sin cambio. Pipeline de Caso 1
(`protocolo/Program.cs`): 7/7, `HashCompuesto` sin cambio (campo opcional no afecta identidad).
Baselines congelados (Caso 1, Caso 2, Caso 3A): `git status --porcelain` vacío. `src/`/`tests/`:
sin cambios adicionales a los ya reportados en D-091-D-095 — 4.4 vive enteramente en
`exploration/laboratorio/`.

**No modificado**: `ValidadorCapacidad.cs`, `RegistroIncapacidad.cs`, `BacktestRunner.cs`,
`ResultadoBacktest.cs` — la exposición reutilizó datos ya calculados desde Caso 2, sin tocar el
motor.

---

## Próximo paso

Caso 4.4 implementado y verificado. Auditoría consolidada de Caso 4 completo (D-084 a D-098)
pendiente, antes de decidir congelamiento o apertura de nuevas fases.
