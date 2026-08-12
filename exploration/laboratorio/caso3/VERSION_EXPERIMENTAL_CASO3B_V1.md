# Versión Experimental — Caso 3B: Generalización Experimental — Multi-Condición

Estado: **documento de congelamiento oficial — cierre de Caso 3B** (autorizado tras aprobación de
`AUDITORIA_CASO3B_V1.md`). A partir de este documento, el Caso 3B queda congelado como
**V1 Experimental**, independiente de `caso3a-v1-experimental` — comparten objetivo general
(generalización experimental de estrategias) pero son experimentos distintos (Caso 3A: distancia
estructural y control experimental; Caso 3B: composición jerárquica de condiciones). Mismo patrón
que `VERSION_EXPERIMENTAL_CASO3A_V1.md`/`caso4/VERSION_EXPERIMENTAL_CASO4_V1.md`.

---

## Identificación

- **Nombre**: Caso 3B — Generalización experimental, composición jerárquica de condiciones
- **Versión**: V1 Experimental
- **Estado**: Congelado
- **Fecha de congelamiento**: 2026-08-12
- **Base de aprobación**: `AUDITORIA_CASO3B_V1.md`, aprobada por auditoría.

---

## Componentes incluidos

**Clasificación jerárquica de condiciones** (D-099/D-100/D-101): patrón de decisión donde una
condición primaria habilita la evaluación de una condición secundaria — objetos internos de
condición con estado propio (`CondicionVolumen`, `CondicionBreakout`,
`exploration/EstrategiaVolumenBreakout.cs`), observabilidad estructural
(`ResultadoEvaluacionCondiciones`) vía callback, sin metadata nueva en `IStrategy`.

**`EstrategiaVolumenBreakout`** (D-102 a D-107): familia concreta — condición primaria = volumen
actual sobre media móvil de ventana 20 × múltiplo 1.5 (`CondicionVolumen`); condición secundaria =
ruptura del máximo/mínimo de una ventana de 20 velas previas, excluyendo la vela actual
(`CondicionBreakout`, bidireccional). Cierre exclusivamente por señal contraria (D-107) — la misma
regla jerárquica evaluada en sentido opuesto a la posición abierta, emitida como 2 `OrderRequest`
(cierre + apertura) en la misma llamada a `Observar`, mismo patrón ya usado por `EstrategiaNeutral`.
Sin martingala, una posición máxima abierta (D-104).

**Pruebas**: `exploration/laboratorio/caso3/TestsEstrategiaVolumenBreakout.cs` (14 pruebas,
integradas al módulo satélite existente de Caso 3, sin `.csproj` nuevo).

---

## Decisiones congeladas

D-099 a D-107 (9 decisiones), registradas en `DECISIONES_CASO3B_V1.md`. Ninguna reasignada a
contenido distinto del originalmente registrado. Todas 🟢 Aprobadas e implementadas — ninguna queda
como deuda técnica pendiente dentro del alcance de Caso 3B.

---

## Garantías

- **Jerarquía real, no combinación plana**: verificado que la condición secundaria nunca se evalúa
  si la primaria no se cumple (`Secundaria == null` en el resultado reportado, P1) — distingue esta
  familia de un simple AND/OR de condiciones independientes.
- **Bidireccionalidad simétrica**: la misma regla de breakout se aplica en ambos sentidos (D-105
  ampliada), sin introducir una segunda hipótesis experimental — verificado con P2/P3 (entrada
  Long/Short) y P7/P8 (reversión en ambos sentidos).
- **Reversión verificada con evidencia directa**: P7/P8 confirman la posición neta final contra
  `AplicadorFill` real (no solo la `OrderRequest` emitida), mismo criterio de evidencia que D-095
  en Caso 4.
- **Sin optimización de parámetros**: `N=20`, múltiplo `1.5×`, exclusión de la vela actual y
  operador estricto fijados por convención declarada antes de ejecutar pruebas (D-105), nunca por
  ajuste observando resultados.
- **Generalización del pipeline confirmada de nuevo**: integrada implementando únicamente
  `IStrategy`, sin modificar `MatchingEngine`, `AplicadorFill`, `ConsumidorFifo`, `ResolutorVela`,
  ni ningún archivo de `src/` — tercera familia consecutiva (tras Z-Score y Neutral) que confirma
  esta propiedad del laboratorio.
- **No regresión sobre Caso 1**: `HashCompuesto` (`A48CCC57DA1919F533F4D532FDC0F945705681DCDA813B3
  85BBFE7F44F40998E`) permanece idéntico tras la implementación completa de Caso 3B.
- **No regresión sobre Caso 2/Caso 3A/Caso 4**: ninguno de los 4 baselines fue regenerado ni
  alterado — verificado por `git status --porcelain` vacío sobre esas rutas en toda la fase.
- **14/14 pruebas de Caso 3B + 30/30 del módulo `caso3` completo** (incluyendo Z-Score y Neutral,
  sin regresión), **126/126 tests de producción** sin cambio.
- **Divergencias entre especificación e implementación real, corregidas y documentadas, no
  ocultas**: mecanismo de reversión (2 órdenes explícitas, no una sola vía Cross-Zero) y
  complejidad real de `CondicionBreakout` (O(N) por ventana fija, no O(1)) — ambos hallazgos
  registrados en `AUDITORIA_CASO3B_V1.md` §4, ninguno requirió una decisión D-N nueva.
- **Sin abstracciones no solicitadas**: se rechazó explícitamente un "pipeline interno de
  evaluación" genérico (D-100) en favor de objetos concretos de condición, mismo criterio de
  simplicidad ya aplicado en fases anteriores.

---

## Exclusiones (explícitas)

- **Sin calibración**: ningún parámetro (ventanas, múltiplo, criterio de ruptura) fue ajustado
  observando resultados — todos fijados por convención declarada en D-105.
- **Sin activación de Caso 4**: `GestorCapital`/sizing/`ValidadorCapacidad`/
  `ClasificadorIntencionOrden` no intervienen en ninguna prueba ni corrida de Caso 3B — todas
  corren con `Sizing=null` implícito.
- **`IStrategy` y las 5 estrategias existentes intactas**: ninguna modificación de código.
- **D-055 no activada adicionalmente**: `EstrategiaVolumenBreakout` hereda `UsaMartingala=false`,
  mismo perfil ya cubierto por Z-Score/Neutral, sin aportar evidencia nueva.
- **D-044 no activada**: Caso 3B no estudia interacción estrategia/régimen.
- **Sin tercer nivel jerárquico ni condiciones adicionales**: la familia implementada usa
  exactamente 2 niveles (primaria/secundaria), D-100 rechazó explícitamente diseñar para N niveles
  sin necesidad demostrada.
- **Candidato F (multi-instrumento) sigue descartado**: no forma parte de Caso 3B, descartado
  originalmente en `EVALUACION_SEGUNDA_FAMILIA_CASO3_V1.md` por requerir tocar `src/`.

Todo lo anterior queda registrado en `DECISIONES_CASO3B_V1.md` y `AUDITORIA_CASO3B_V1.md` — fuera
de esta versión.

---

## Evidencia

- **14/14 pruebas Caso 3B** (`caso3/Program.cs`, `TestsEstrategiaVolumenBreakout.EjecutarTodos()`).
- **30/30 pruebas del módulo `caso3` completo** (8 Z-Score + 8 Neutral + 14 VolumenBreakout).
- **126/126 tests de producción** sin cambio.
- **HashCompuesto de Caso 1**: `A48CCC57DA1919F533F4D532FDC0F945705681DCDA813B385BBFE7F44F40998E`
  — verificado idéntico tras la implementación completa de Caso 3B.
- **4 baselines congelados** (`caso1-v1-experimental`, `caso2-v1-experimental`,
  `caso3a-v1-experimental`, `caso4-v1-experimental`): sin regenerar ni alterar — confirmado por
  `git status --porcelain` vacío en toda la fase.
- **`git status --porcelain -- src/ tests/`**: vacío en todo el ciclo de Caso 3B.
- Auditoría de cierre: `caso3/AUDITORIA_CASO3B_V1.md`.

---

## Regla de evolución

Cualquier extensión que amplíe el alcance de Caso 3B — tercer nivel jerárquico, nuevas condiciones,
activación de D-044/D-055 más allá de lo ya declarado, activación de Caso 4 dentro de esta familia
— requiere una **nueva fase**, nunca una edición in-place de V1 (mismo principio que la regla de
evolución de `VERSION_EXPERIMENTAL_CASO3A_V1.md`/`caso4/VERSION_EXPERIMENTAL_CASO4_V1.md`).

```
V1 Experimental — Caso 3B (congelada)
        ↓
  tercer nivel jerarquico / nuevas condiciones / D-044 / D-055 / Caso 4 activados
        ↓
Caso 3C — o fase equivalente
```

---

## Fuera de alcance de este documento

No se implementó código. No se modifica ningún módulo. No se selecciona ni abre ninguna fase
siguiente (Caso 3C, Caso 5) — conforme a la restricción explícita de este cierre.

---

## Criterio de cierre de este documento

- ✓ Identificación formal (nombre, versión, estado, fecha) registrada.
- ✓ Componentes incluidos listados con archivo y decisión de origen (D-099 a D-107).
- ✓ Decisiones congeladas referenciadas, sin reasignaciones, todas aprobadas e implementadas.
- ✓ Garantías (jerarquía real, bidireccionalidad, evidencia directa, sin calibración,
  generalización del pipeline, no regresión, divergencias corregidas y documentadas) declaradas y
  respaldadas por evidencia ya verificada.
- ✓ Exclusiones declaradas explícitamente (sin calibración, sin Caso 4, D-044/D-055 no activadas,
  sin tercer nivel, Candidato F descartado).
- ✓ Evidencia referenciada (14/14 + 30/30 + 126/126, hash Caso 1 intacto, 4 baselines intactos).
- ✓ Regla de evolución (nueva fase ante cambio de alcance) establecida.
- ✓ Ningún cambio de código adicional — verificado (`git status --porcelain -- src/ tests/` sin
  cambios).
- ⏳ Pendiente: preparación de commit y tag `caso3b-v1-experimental`.
