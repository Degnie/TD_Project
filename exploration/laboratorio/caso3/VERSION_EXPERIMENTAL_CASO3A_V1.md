# Versión Experimental — Caso 3A: Generalización Experimental

Estado: **documento de congelamiento oficial — cierre de Caso 3A** (autorizado tras aprobación de
`AUDITORIA_CASO3A_V1.md`). A partir de este documento, el Caso 3A queda congelado como
**V1 Experimental**. Mismo patrón que `VERSION_EXPERIMENTAL_CASO1_V1.md`/
`VERSION_EXPERIMENTAL_CASO2_V1.md`.

---

## Identificación

- **Nombre**: Caso 3A — Generalización experimental
- **Versión**: V1 Experimental
- **Estado**: Congelado
- **Fecha de congelamiento**: 2026-08-12
- **Base de aprobación**: `AUDITORIA_ZSCORE_REVERSAL_CASO3_V1.md` (primera familia) +
  `AUDITORIA_CASO3A_V1.md` (fase completa), ambas aprobadas por auditoría.

---

## Componentes incluidos

**Familia 1 — Z-Score Reversal** (D-086/D-087): `EstrategiaZScoreReversion`
(`exploration/EstrategiaZScoreReversion.cs`), señal por desviación estadística sobre ventana
deslizante O(1) (`Ventana=20`, `UmbralEntrada=2.0`, `UmbralSalida=0.5`, congelados antes de
ejecutar, D-030), sin martingala, sin posiciones simultáneas.

**Familia 2 — Estrategia Neutral** (D-086/D-087): `EstrategiaNeutral`
(`exploration/EstrategiaNeutral.cs`), control experimental determinista sin ninguna hipótesis de
mercado — decide exclusivamente por `DataSlice.N % Ciclo` (`Ciclo=10`), nunca lee
`Open`/`High`/`Low`/`Volume`, sin `Random` ni semilla. Verificada empíricamente por P3
(independencia del mercado) y P4 (ausencia de aleatoriedad).

**Metadata de capacidades** (D-088/D-090): `CaracteristicasEstrategia(bool UsaMartingala)`
(`exploration/laboratorio/protocolo/EjecutorProtocolo.cs`), externa a `IStrategy`, consumida vía
`EntradaProtocolo.Caracteristicas`/`ResultadoProtocolo.Caracteristicas`, opcional con default
`null`. `PresentadorResolucionIntentos.Formatear` (`caso3/PresentadorResolucionIntentos.cs`)
distingue 3 estados: `false` → "no aplica", `true` → valores reales, `null` → valores reales sin
asumir aplicabilidad — sin modificar `AnalizadorOperacional.cs`.

**Módulo satélite**: `exploration/laboratorio/caso3/` (`Caso3.csproj`, `Program.cs`,
`TestsCaso3.cs`, `TestsEstrategiaNeutral.cs`, `PresentadorResolucionIntentos.cs`), enlazando
archivos de Caso 1/Caso 2 vía `<Compile Include>` sin duplicar código, mismo patrón satélite usado
en fases anteriores.

---

## Decisiones congeladas

D-086 a D-090 (5 decisiones), registradas en `DECISIONES_CASO3_V1.md`. Ninguna reasignada a
contenido distinto del originalmente registrado. Todas 🟢 Aprobadas e implementadas — ninguna
queda como deuda técnica pendiente dentro del alcance de Caso 3A (a diferencia de D-084/D-085 en
Caso 2, que sí cerraron como deuda técnica explícita).

---

## Garantías

- **Reproducibilidad**: ambas familias son 100% deterministas — verificado por prueba dedicada en
  cada una (P6 de Z-Score, P4 de Neutral: dos instancias independientes producen secuencias
  idénticas sin coordinar semilla).
- **Independencia del mercado (Neutral)**: verificado empíricamente, no solo por diseño — P3
  altera `Open`/`High`/`Low`/`Volume` arbitrariamente manteniendo `Close`/`Timestamp`, y la
  secuencia de órdenes resultante es idéntica byte a byte.
- **Generalización del pipeline**: ambas familias se integraron implementando únicamente
  `IStrategy` — sin modificar `MatchingEngine`, `AplicadorFill`, `ConsumidorFifo`,
  `ResolutorVela`, ni ningún archivo de `src/`.
- **No regresión sobre Caso 1**: el baseline `caso1-v1-experimental`
  (`A48CCC57DA1919F533F4D532FDC0F945705681DCDA813B385BBFE7F44F40998E`) permanece bit-a-bit
  idéntico tras la implementación completa de Caso 3A.
- **No regresión sobre Caso 2**: `baseline_financiero_final/` no fue regenerado ni alterado en
  ningún punto del ciclo — verificado por `git status --porcelain` vacío sobre esa ruta en toda la
  fase.
- **107/107 tests de producción** pasando sin modificación de ningún test pre-existente.
- **16/16 pruebas de Caso 3A** (8 Z-Score + 8 Neutral), cubriendo determinismo, independencia del
  mercado, ausencia de aleatoriedad, metadata, rendimiento, integración real en el pipeline
  (`EjecutorProtocolo` con dataset BTCUSDT real) y regresión.
- **Sin abstracciones no solicitadas**: ambas familias son clases concretas y standalone, mismo
  estilo que las 3 estrategias originales y EMA Cross — no se creó ninguna interfaz ni clase base
  genérica adicional.

---

## Exclusiones (explícitas)

- **D-055 no resuelta completamente**: la presentación "no aplica" está implementada y verificada
  2 veces de forma independiente, pero el rediseño del catálogo de métricas
  (`AnalizadorOperacional.cs`) permanece sin tocar — deuda técnica documentada, no bloqueante
  (D-089).
- **D-044 no activada**: ninguna familia de Caso 3A estudia interacción estrategia/régimen.
- **D-084 no activada**: `GestorCapital`/sizing dinámico no interviene en ninguna de las dos
  familias — ambas se ejecutan con la configuración económica de Caso 2 sin activar sizing.
- **Sin ranking ni comparación de superioridad**: ninguna evaluación de Caso 3A declara una
  familia mejor que otra (extiende D-014/D-047/D-076).
- **Sin optimización ni calibración de parámetros**: `Ventana`/`UmbralEntrada`/`UmbralSalida`
  (Z-Score) y `Ciclo` (Neutral) fueron fijados antes de ejecutar y nunca ajustados tras ver
  resultados.
- **Sin más familias en esta versión**: D-086 exigió un mínimo de 2; una tercera familia (ej.
  candidato E, multi-condición, evaluado y diferido en `EVALUACION_SEGUNDA_FAMILIA_CASO3_V1.md`)
  queda fuera de V1, disponible como punto de partida de una futura fase si se decide abrir.

Todo lo anterior queda registrado en `DECISIONES_CASO3_V1.md` y `AUDITORIA_CASO3A_V1.md` — fuera
de esta versión.

---

## Evidencia

- **16/16 pruebas Caso 3A** pasando (`caso3/Program.cs`, agregando `TestsCaso3.EjecutarTodos()` +
  `TestsEstrategiaNeutral.EjecutarTodos()`).
- **107/107 tests de producción** sin cambio.
- **HashCompuesto de Caso 1**: `A48CCC57DA1919F533F4D532FDC0F945705681DCDA813B385BBFE7F44F40998E`
  — verificado idéntico tras la implementación completa de ambas familias.
- **`baseline_financiero_final/` (Caso 2)**: sin regenerar ni alterar — confirmado por
  `git status --porcelain` vacío en toda la fase.
- **`git status --porcelain -- src/ tests/`**: vacío en todo el ciclo de Caso 3A.
- Auditorías de cierre: `caso3/AUDITORIA_ZSCORE_REVERSAL_CASO3_V1.md` (primera familia),
  `caso3/AUDITORIA_CASO3A_V1.md` (fase completa, responde los 4 criterios de cierre de
  `PROPUESTA_CASO3_V1.md` §9).

---

## Regla de evolución

Cualquier extensión que amplíe el alcance de Caso 3A — nueva familia adicional, resolución completa
de D-055, activación de D-044 o D-084 dentro del laboratorio de estrategias — requiere una **nueva
fase** (Caso 3B o equivalente), nunca una edición in-place de V1 (mismo principio que la regla de
evolución de `VERSION_EXPERIMENTAL_CASO1_V1.md`/`VERSION_EXPERIMENTAL_CASO2_V1.md`).

```
V1 Experimental — Caso 3A (congelada)
        ↓
  nueva familia / D-055 completa / D-044 / D-084 activados
        ↓
Caso 3B — Profundización experimental (o fase equivalente)
```

---

## Fuera de alcance de este documento

No se implementó código. No se modifica ningún módulo. No se selecciona ni abre ninguna fase
siguiente (Caso 3B, Caso 4, Caso 5) — conforme a la restricción explícita de este cierre.

---

## Criterio de cierre de este documento

- ✓ Identificación formal (nombre, versión, estado, fecha) registrada.
- ✓ Componentes incluidos listados con archivo y decisión de origen (D-086 a D-090).
- ✓ Decisiones congeladas referenciadas (D-086 a D-090), sin reasignaciones, todas aprobadas e
  implementadas.
- ✓ Garantías (reproducibilidad, independencia del mercado, generalización, no regresión Caso
  1/Caso 2, sin abstracciones no solicitadas) declaradas y respaldadas por evidencia ya verificada.
- ✓ Exclusiones declaradas explícitamente (D-055/D-044/D-084 no activadas, sin ranking, sin
  calibración, sin tercera familia), remitiendo a `DECISIONES_CASO3_V1.md`/`AUDITORIA_CASO3A_V1.md`.
- ✓ Evidencia referenciada (16/16 + 107/107, hash Caso 1 intacto, baseline Caso 2 intacto).
- ✓ Regla de evolución (nueva fase ante cambio de alcance) establecida.
- ✓ Ningún cambio de código adicional — verificado (`git status --porcelain -- src/ tests/` sin
  cambios).
- ⏳ Pendiente: preparación de commits y tag de Caso 3A.
