# Auditoría de Cierre — Caso 3B: Generalización Experimental — Multi-Condición

Estado: **documento de cierre de sub-fase — Caso 3B completo**. Consolida evidencia verificada del
ciclo especificación → decisión → implementación → pruebas → auditoría para D-099 a D-107. Mismo
patrón que las auditorías de cierre de Caso 3A (`AUDITORIA_CASO3A_V1.md`) y Caso 4
(`AUDITORIA_CASO4_3_UNIDADES_EXPOSICION_V1.md`).

---

## 1. Alcance auditado

Documentos de origen: `PROPUESTA_CASO3B_V1.md`, `DECISIONES_CASO3B_V1.md` (D-099 a D-107),
`ESPECIFICACION_ESTRATEGIA_VOLUMEN_BREAKOUT_V1.md`. Implementación:
`exploration/EstrategiaVolumenBreakout.cs`,
`exploration/laboratorio/caso3/TestsEstrategiaVolumenBreakout.cs`.

---

## 2. Origen y continuidad con Caso 3A

Caso 3A (`caso3a-v1-experimental`) verificó que el laboratorio generaliza a familias
estructuralmente distintas (Z-Score Reversal, Estrategia Neutral). `EVALUACION_SEGUNDA_FAMILIA_
CASO3_V1.md` identificó un eje no cubierto por ninguna de las 5 estrategias hasta ese momento —
decisión basada en múltiples condiciones combinadas — y registró el Candidato E (multi-condición)
como diferido, no descartado. Caso 3B retoma exactamente ese candidato diferido, con la pregunta
reformulada: ¿el laboratorio generaliza a una estrategia cuya decisión de entrada depende de una
**jerarquía** de condiciones (una habilita la evaluación de la siguiente), no solo de una condición
aislada?

---

## 3. Decisiones D-099 a D-107 — resumen

| Decisión | Resolución |
|---|---|
| D-099 | Semántica de multi-condición: jerárquica (Opción C) — primaria habilita evaluación de secundaria, rechazando AND/OR (independencia plana) y acumulativa (riesgo de calibración) |
| D-100 | Representación interna: objetos internos de condición con estado propio (`CondicionVolumen`/`CondicionBreakout`), rechazando inline (baja trazabilidad) y pipeline genérico (sobre-diseño no solicitado) |
| D-101 | Observabilidad: estructural, derivada de D-100, sin metadata nueva en `IStrategy` — mismo criterio que D-088 (Caso 3A) |
| D-102 | Familia concreta: Candidato H — volumen (contexto) + precio (breakout), único candidato sin reutilizar ningún indicador de una estrategia congelada, a diferencia de G (tendencia, cercano a EMA Cross) e I (riesgo de duplicar `ClasificadorRegimenV1`) |
| D-103 | Condiciones: P3 (múltiplo fijo sobre ventana) para volumen, S2 (ruptura de rango/breakout) para precio |
| D-104 | Diseño de implementación: `EstrategiaVolumenBreakout`, objetos internos con estado propio, observabilidad vía callback existente, hereda sin martingala/una posición máxima de Z-Score/Neutral |
| D-105 | Parámetros: `N=20` (ambas ventanas), múltiplo `1.5×`, extremos excluyen la vela actual, operador estricto, sin confirmación N+1 — **ampliada** tras D-107 para incluir ruptura simétrica a la baja |
| D-106 | Especificación de implementación y pruebas — aprobada, brevemente bloqueada por el hallazgo de D-107, luego desbloqueada |
| D-107 | Cierre por señal contraria — la misma jerarquía volumen+breakout evaluada en sentido opuesto a la posición abierta, rechazando cierre por pérdida de volumen (Opción A) y stop de precio aislado (Opción B) |

**Hallazgo intermedio correctamente escalado, no resuelto en silencio**: al revisar
`ESPECIFICACION_ESTRATEGIA_VOLUMEN_BREAKOUT_V1.md` antes de implementar, se detectó que D-099 a
D-105 solo habían definido la jerarquía en sentido alcista — "cierre por señal contraria" (D-107)
no tenía semántica concreta sin una ruptura bajista definida. Se escaló como pregunta abierta
(no se asumió una respuesta) y se resolvió ampliando D-105 con la ruptura simétrica, sin abrir una
decisión D-108 — mismo criterio que D-062/D-083/D-084/D-095 en fases anteriores: una precisión que
completa una decisión existente no es, por sí sola, una decisión estructural nueva.

---

## 4. Hallazgos de implementación (post-D-107, verificados y corregidos)

**Hallazgo 1 — Mecanismo de reversión**: la especificación original asumía que una `OrderRequest`
de magnitud mayor a la posición existente activaría `ResolutorCrossZero`. Verificado contra
`AplicadorFill` real que, con `Cantidad=1m` fija, una sola orden de igual magnitud produce
`CierreTotal`, no `CrossZero` (que exige `magnitudFill > magnitudPosicion`). Corregido adoptando el
mismo patrón que `EstrategiaNeutral` ya usa en su punto de reversión: 2 `OrderRequest` explícitas
(cierre + apertura) en la misma llamada a `Observar`. No requirió ningún cambio en `src/` — el
motor procesa ambas órdenes con la lógica ya existente. `ESPECIFICACION_ESTRATEGIA_VOLUMEN_
BREAKOUT_V1.md` §4 y `DECISIONES_CASO3B_V1.md` (D-107) actualizados para reflejar el mecanismo
real.

**Hallazgo 2 — Complejidad de `CondicionBreakout`**: la especificación afirmaba cálculo O(1) por
vela para toda la estrategia. La implementación real de `CondicionBreakout.Evaluar` usa
`Queue<decimal>.Max()`/`.Min()`, que es O(N) por evaluación (`N=20` fijo) — no O(1) como sí lo es
`CondicionVolumen` (suma acumulada). No representa degradación observable con `N=20` fijo (P12
confirma rendimiento sobre 100k velas) y no requiere corrección de código, solo de documentación —
corregido en `ESPECIFICACION_ESTRATEGIA_VOLUMEN_BREAKOUT_V1.md` §2 a "ventana deslizante con coste
lineal sobre una ventana fija de tamaño 20".

**Ninguno de los dos hallazgos abrió una decisión D-108**: ambos son correcciones de precisión
entre especificación e implementación real, no cambios de hipótesis experimental, familia
seleccionada, parámetros, arquitectura ni alcance de Caso 3B.

---

## 5. Evidencia de pruebas

**14/14 pruebas de Caso 3B** (`caso3/TestsEstrategiaVolumenBreakout.cs`): P1 (jerarquía —
`Secundaria=null` cuando la primaria falla), P2/P3 (entrada Long/Short), P4 (volumen sin breakout),
P5 (exclusión de la vela actual en ambos extremos), P6 (operador estricto), P7/P8 (reversión
Long→Short y Short→Long, verificadas con evidencia directa contra `AplicadorFill` real — posición
neta final exacta, no solo la `OrderRequest` emitida, mismo criterio de evidencia que D-095 en
Caso 4), P9 (sin posiciones simultáneas en la misma dirección), P10 (determinismo), P11 (metadata),
P12 (rendimiento), P13 (integración en el pipeline con dataset real), P14 (regresión).

**30/30 pruebas del módulo `caso3` completo**: 8 Z-Score + 8 Neutral + 14 VolumenBreakout, sin
ninguna regresión sobre las familias de Caso 3A.

**126/126 tests de producción**: sin cambio — Caso 3B no toca `src/`/`tests/`.

**Pipeline de Caso 1**: 7/7, `HashCompuesto = A48CCC57DA1919F533F4D532FDC0F945705681DCDA813B385BB
FE7F44F40998E` idéntico al congelado en `caso1-v1-experimental` — el campo/estructura nueva de
Caso 3B no afecta identidad experimental de corridas ajenas.

---

## 6. Confirmación de no regresión

- **4 baselines congelados** (`caso1-v1-experimental`, `caso2-v1-experimental`,
  `caso3a-v1-experimental`, `caso4-v1-experimental`): `git status --porcelain` vacío sobre las 4
  rutas en todo el ciclo de Caso 3B.
- **`src/`, `tests/`**: sin ningún cambio — verificado explícitamente antes y después de la
  implementación.
- **`IStrategy`**: sin modificación — `EstrategiaVolumenBreakout` implementa el contrato existente
  sin extenderlo.
- **Las 5 estrategias existentes** (Tres Mosqueteros, MHI Mayoría, EMA Cross, Z-Score Reversal,
  Estrategia Neutral): sin ningún cambio de código.
- **Caso 4** (`GestorCapital`/sizing/`ValidadorCapacidad`/`ClasificadorIntencionOrden`): no
  activado en ningún punto de Caso 3B — todas las pruebas corren con `Sizing=null` implícito (no
  se pasó configuración de sizing en ninguna entrada de `EjecutorProtocolo`).
- **`ResolutorCrossZero`/`AplicadorFill`/`ConsumidorFifo`**: sin ninguna modificación — el hallazgo
  1 (§4) se resolvió adaptando cómo la estrategia emite órdenes, no el motor.

---

## 7. Decisiones activadas por esta sub-fase

**D-055** (métricas dependientes de martingala): no activada adicionalmente — `EstrategiaVolumen
Breakout` hereda `UsaMartingala=false` (D-104), mismo perfil ya cubierto por Z-Score/Neutral, sin
aportar evidencia nueva a D-055.

**D-044** (interacción estrategia/régimen): no activada — Caso 3B no estudia esa interacción.

**Ninguna decisión nueva más allá de D-099 a D-107** se abre en este documento.

---

## 8. Estado final — Decisiones de Caso 3B

| Decisión | Estado |
|---|---|
| D-099 | ✅ Semántica jerárquica |
| D-100 | ✅ Objetos internos de condición |
| D-101 | ✅ Observabilidad estructural |
| D-102 | ✅ Familia H (volumen + breakout) |
| D-103 | ✅ Condiciones concretas (P3 + S2) |
| D-104 | ✅ Diseño de implementación |
| D-105 | ✅ Parámetros (ampliada, bidireccional) |
| D-106 | ✅ Especificación (actualizada post-implementación) |
| D-107 | ✅ Cierre por señal contraria |

**Ninguna deuda técnica bloqueante queda abierta dentro del alcance de Caso 3B.**

---

## Fuera de alcance de este documento

No se decide si Caso 3B se congela junto a Caso 3A bajo un tag común, se congela como
`caso3b-v1-experimental` independiente, o si se abre una fase siguiente. No se recalibra ningún
parámetro. No se activa Caso 4 ni D-044/D-055 más allá de lo ya declarado en §7.

---

## Criterio de cierre de esta sub-fase

- ✓ D-099 a D-107: cada una con opciones evaluadas, evidencia y selección explícita del auditor.
- ✓ Hallazgo de precisión de D-107 (ambigüedad de "señal contraria") escalado y resuelto sin
  asumir respuesta, sin abrir decisión D-108 innecesaria.
- ✓ 2 hallazgos de implementación (mecanismo de reversión, complejidad real) detectados,
  corregidos y documentados — ninguno oculto, ninguno requirió cambiar hipótesis o alcance.
- ✓ 14/14 pruebas Caso 3B + 30/30 módulo completo + 126/126 producción + hash Caso 1 intacto + 4
  baselines congelados sin alterar.
- ✓ Ninguna restricción de alcance relajada: `src/`, `IStrategy`, estrategias existentes, Caso 4,
  `ResolutorCrossZero`/`AplicadorFill`/`ConsumidorFifo` sin modificación.
- ⏳ Pendiente de tu decisión: congelar Caso 3B (solo o junto a Caso 3A) o abrir nueva fase.
