# Versión Experimental — Caso 4: Evolución Financiera

Estado: **documento de congelamiento oficial — cierre de Caso 4** (autorizado tras aprobación de
`AUDITORIA_CASO4_V1.md`). A partir de este documento, el Caso 4 queda congelado como
**V1 Experimental**. Mismo patrón que `VERSION_EXPERIMENTAL_CASO1_V1.md`/
`VERSION_EXPERIMENTAL_CASO2_V1.md`/`VERSION_EXPERIMENTAL_CASO3A_V1.md`.

---

## Identificación

- **Nombre**: Caso 4 — Evolución financiera
- **Versión**: V1 Experimental
- **Estado**: Congelado
- **Fecha de congelamiento**: 2026-08-12
- **Base de aprobación**: `AUDITORIA_CASO4_3_UNIDADES_EXPOSICION_V1.md` (sub-fase 4.3) +
  `AUDITORIA_CASO4_V1.md` (fase completa), ambas aprobadas por auditoría.

---

## Componentes incluidos

**Clasificación de intención de orden** (D-091/D-092): `ClasificadorIntencionOrden`
(`src/Domain/Portfolio/ClasificadorIntencionOrden.cs`), componente puro que deriva
`Apertura`/`Aumento`/`ReduccionParcial`/`CierreTotal`/`CrossZero` exclusivamente de
`PortfolioState`/`LotesVivos`, previo a `GestorCapital`, sin conocer configuración de sizing.

**Gestión de capital con clasificación previa** (D-084/D-095): `GestorCapital.Ajustar`
(`src/Domain/Portfolio/GestorCapital.cs`) clasifica secuencialmente cada `OrderRequest` de una
bolsa contra una posición proyectada local; aplica sizing únicamente a `Apertura`/`Aumento`;
normaliza `CrossZero` espurio a `CierreTotal` contra la posición real cuando sizing está activo,
preservando Cross-Zero genuino bajo `Sizing=null`.

**Sizing dimensional corregido** (D-085/D-093/D-094): `CantidadActivo = (CapitalDisponible ×
PorcentajeRiesgo) / (CloseReferencia × TasaMargen)`, con `CapitalDisponible = Cash − Margin`
(D-067, sin cambio) y `CloseReferencia` = `Close` de la vela siguiente.

**Observabilidad de incapacidades** (D-096/D-097): `ResultadoCorridaTimeframe.Incapacidades`
(`exploration/laboratorio/protocolo/EjecutorProtocolo.cs`, campo opcional trailing, mismo patrón
D-072), expuesto en `ReporteFinancieroGenerador.cs` §4 con lenguaje neutral, agrupado por `Side`.

**Módulo satélite** (D-098): `exploration/laboratorio/caso4/` (`Caso4.csproj`, `Program.cs`,
`TestsReporteIncapacidades.cs`), enlazando archivos de Caso 1/Caso 2/Caso 3 vía `<Compile Include>`
sin duplicar código, mismo patrón satélite usado en Caso 3A.

---

## Decisiones congeladas

D-084, D-085 (heredadas de Caso 2 como deuda técnica, resueltas en esta fase) y D-091 a D-098 (8
decisiones nuevas), registradas en `DECISIONES_CASO4_V1.md` y
`DECISIONES_UNIDADES_EXPOSICION_CASO4_V1.md`. Ninguna reasignada a contenido distinto del
originalmente registrado. Todas 🟢 Aprobadas e implementadas — ninguna queda como deuda técnica
pendiente dentro del alcance de Caso 4.

---

## Garantías

- **Causa raíz corregida, no enmascarada**: D-084 y D-085 se resolvieron modificando `src/` en su
  ubicación real (`GestorCapital.cs`), no duplicando lógica correcta en `exploration/` sobre un
  motor sin corregir (Opción B de D-091, explícitamente rechazada).
- **Comportamiento histórico preservado**: `Sizing=null` en todo baseline congelado (Caso 1, Caso
  2, Caso 3A) produce resultados bit-a-bit idénticos — la corrección solo se activa con
  configuración explícita (D-091, mismo patrón que D-061/D-069/D-079/D-082).
- **Separación de responsabilidades**: `ClasificadorIntencionOrden` permanece una consulta pura sin
  conocer sizing; la reinterpretación de Cross-Zero espurio vive exclusivamente en `GestorCapital`,
  el único componente consciente de si sizing está activo.
- **3 criterios de aceptación de D-095 verificados con evidencia directa**: cierre total exacto sin
  short residual, Cross-Zero genuino preservado bajo `Sizing=null`, comportamiento intacto sin
  sizing.
- **Lenguaje neutral en observabilidad**: el reporte de incapacidades nunca afirma
  "error"/"inválido"/"falló"/"debe descartarse" — declara explícitamente que el motor no bloquea ni
  modifica ninguna orden por esta razón (D-097, D-059/D-060 vigentes).
- **126/126 tests de producción** pasando, incluyendo actualización de contrato (no regresión) de
  P2/P4 de `GestorCapitalTests.cs` reflejando la fórmula dimensional corregida.
- **4/4 pruebas de Caso 4.4** cubriendo flujo end-to-end desde `ResultadoBacktest` hasta el texto
  del reporte.
- **Sin abstracciones no solicitadas**: `ClasificadorIntencionOrden` es un componente concreto de
  responsabilidad única, mismo estilo que `ConsumidorFifo`/`ResolutorCrossZero`.

---

## Exclusiones (explícitas)

- **Sin calibración económica**: la corrección de D-085 es dimensional (`Cantidad` deja de mezclar
  unidad monetaria con unidad de activo), no una calibración de qué `PorcentajeRiesgo` o
  `CapitalInicial` son "razonables" para ninguna estrategia.
- **Modo estricto de `ValidadorCapacidad` no implementado**: D-096 seleccionó exclusivamente
  observación/reporte (Opción A); bloqueo o rechazo de órdenes por incapacidad queda deferred.
- **`ValidadorBolsaRequests` no modificado ni evaluado**: D-097 lo distingue conceptualmente de
  `ValidadorCapacidad`, pero Caso 4 no tocó su lógica.
- **`IStrategy` y las 5 estrategias existentes intactas**: ninguna estrategia fue modificada para
  conocer posición real ni cantidad calculada por sizing (P-002 preservado).
- **Referencia documental obsoleta no corregida**: `ReporteFinancieroGenerador.cs` §6 conserva el
  texto "D-085, no resuelta en Caso 2 V1" por instrucción explícita del auditor — registrada como
  deuda documental histórica, pendiente de un futuro mecanismo de errata/índice de evolución
  documental, no de una reapertura de `VERSION_EXPERIMENTAL_CASO2_V1.md`.
- **Caso 3B**: explícitamente diferido desde la apertura de Caso 4, ninguna decisión de esta fase
  lo activa.

Todo lo anterior queda registrado en `DECISIONES_CASO4_V1.md`,
`DECISIONES_UNIDADES_EXPOSICION_CASO4_V1.md`, `DECISIONES_VALIDADOR_CAPACIDAD_CASO4_V1.md` y
`AUDITORIA_CASO4_V1.md` — fuera de esta versión.

---

## Evidencia

- **126/126 tests de producción** (progresión 118 → 122 → 124 → 126 a lo largo de las sub-fases).
- **4/4 pruebas de Caso 4.4** (`caso4/Program.cs`, `TestsReporteIncapacidades.EjecutarTodos()`).
- **3 criterios de aceptación de D-095** verificados con evidencia directa contra escenarios
  exactos planteados por el auditor.
- **Pipeline Caso 1**: 7/7 tests, hash reproducible confirmado tras extender
  `ResultadoCorridaTimeframe` con campo opcional trailing.
- **3 baselines congelados** (Caso 1, Caso 2, Caso 3A): `git status --porcelain` vacío sobre las 3
  rutas en todo el ciclo de Caso 4.
- Auditorías de cierre: `AUDITORIA_CASO4_3_UNIDADES_EXPOSICION_V1.md` (D-093/D-094/D-095,
  D-085 resuelta), `AUDITORIA_CASO4_V1.md` (fase completa).

---

## Regla de evolución

Cualquier extensión que amplíe el alcance de Caso 4 — modo estricto de `ValidadorCapacidad`,
calibración de parámetros económicos, políticas de riesgo, optimización — requiere una **nueva
fase**, nunca una edición in-place de V1 (mismo principio que la regla de evolución de
`VERSION_EXPERIMENTAL_CASO1_V1.md`/`VERSION_EXPERIMENTAL_CASO2_V1.md`/
`VERSION_EXPERIMENTAL_CASO3A_V1.md`).

```
V1 Experimental — Caso 4 (congelada)
        ↓
  modo estricto / calibración / políticas de riesgo / optimización
        ↓
Caso 5 — o fase equivalente
```

---

## Fuera de alcance de este documento

No se implementó código. No se modifica ningún módulo. No se selecciona ni abre ninguna fase
siguiente (Caso 3B, Caso 5) — conforme a la restricción explícita de este cierre.

---

## Criterio de cierre de este documento

- ✓ Identificación formal (nombre, versión, estado, fecha) registrada.
- ✓ Componentes incluidos listados con archivo y decisión de origen (D-084/D-085/D-091 a D-098).
- ✓ Decisiones congeladas referenciadas, sin reasignaciones, todas aprobadas e implementadas.
- ✓ Garantías (causa raíz corregida, comportamiento histórico preservado, separación de
  responsabilidades, lenguaje neutral, no regresión) declaradas y respaldadas por evidencia ya
  verificada.
- ✓ Exclusiones declaradas explícitamente (sin calibración, sin modo estricto, referencia
  documental obsoleta conservada deliberadamente).
- ✓ Evidencia referenciada (126/126 + 4/4 + 3 criterios de aceptación + hash Caso 1 intacto + 3
  baselines intactos).
- ✓ Regla de evolución (nueva fase ante cambio de alcance) establecida.
- ✓ Ningún cambio de código adicional en este documento — verificado
  (`git status --porcelain -- src/ tests/` sin cambios).
- ⏳ Pendiente: preparación de commit y tag `caso4-v1-experimental`.
