# Versión Experimental — Caso 5A: Evaluación Comparativa de Gestores de Riesgo

Estado: **documento de congelamiento oficial — cierre de Caso 5A** (autorizado tras aprobación de
`AUDITORIA_CASO5A_V1.md`). A partir de este documento, el Caso 5A queda congelado como
**V1 Experimental**, independiente de Caso 3B/Caso 4 — primera fase que introduce una capacidad
transversal de gestión de capital intercambiable, no una nueva familia de estrategias. Mismo
patrón que `VERSION_EXPERIMENTAL_CASO4_V1.md`/`caso3/VERSION_EXPERIMENTAL_CASO3B_V1.md`, con una
diferencia estructural explícita: Caso 5A **sí modifica `src/`**, a diferencia de Caso 3B — con
autorización explícita en cada paso del ciclo, documentada en `DECISIONES_CASO5_V1.md` y
`ESPECIFICACION_IMPLEMENTACION_GESTORES_RIESGO_V1.md`.

---

## Identificación

- **Nombre**: Caso 5A — Evaluación comparativa de gestores de riesgo
- **Versión**: V1 Experimental
- **Estado**: Congelado
- **Fecha de congelamiento**: 2026-08-12
- **Base de aprobación**: `AUDITORIA_CASO5A_V1.md`, aprobada por auditoría.

---

## Componentes incluidos

**Separación de responsabilidades** (D-108): `IGestorRiesgo` (`src/Domain/Portfolio/
IGestorRiesgo.cs`) — único método, calcula cantidad solo para Apertura/Aumento. `GestorCapital.
Ajustar` (`src/Domain/Portfolio/GestorCapital.cs`) pasa a orquestar: invoca al gestor activo y
conserva íntegra la clasificación de intención + normalización de Cross-Zero (D-092/D-095), sin
duplicar esa lógica por gestor.

**Identidad experimental separada del cálculo** (precisión derivada de D-109):
`IIdentidadGestorRiesgo` (`src/Domain/Portfolio/IIdentidadGestorRiesgo.cs`) — contrato aparte,
`ObtenerIdentidadConfiguracion(): string`, determinista y basado en configuración declarada, nunca
en resultado de ejecución. `IdentidadExperimentoCompleta.CalcularHashConfiguracionEconomica`
consume solo este contrato, sin conocer tipos concretos de gestor — falla explícitamente si el
gestor activo no lo implementa, en vez de producir un hash aproximado.

**Configuración por elección** (D-109): `ConfiguracionSizing` (`src/Domain/Shared/
ConfiguracionSizing.cs`) pasa de `record(decimal PorcentajeRiesgo)` a
`record(IGestorRiesgo GestorActivo)` — describe qué gestor está activo, no contiene lógica de
cálculo. `Default => null` (sizing inactivo) preservado sin cambios (D-061/D-069).

**3 gestores implementados** (D-110), todos en `src/Domain/Portfolio/`:
- `GestorFixedFractional` — migración literal de la fórmula histórica (`MargenObjetivo =
  CapitalDisponible × PorcentajeRiesgo`, `Cantidad = MargenObjetivo / (Precio × TasaMargen)`),
  gestor de control/referencia obligatorio en toda comparación.
- `GestorFixedRisk` — monto de riesgo monetario fijo por operación, independiente del capital
  disponible.
- `GestorVolatilitySizing` — exposición inversamente proporcional a la volatilidad reciente
  (ventana de Closes vía `DataSlice`, mismo patrón O(N) sobre ventana fija que
  `EstrategiaZScoreReversion`), con warmup → cantidad `0m`.

**Ampliación de firma de `GestorCapital.Ajustar`**: acepta `DataSlice` (el mismo que
`BacktestRunner` ya construye para la Strategy) — único cambio de firma necesario para que
`GestorVolatilitySizing` acceda a la ventana de precios que un `precioReferencia` escalar no podía
proveer.

**Métricas nuevas** (D-111, `exploration/laboratorio/modelo_financiero/MetricasFinancieras.cs`/
`CalculadoraMetricasFinancieras.cs`): `ProfitFactor: decimal?`, `CapitalLibreMinimo: decimal`.
`MargenMaximoUtilizado` documentado como equivalente de `ExposicionMaxima` ya existente, sin
duplicar campo.

**Pruebas**: `exploration/laboratorio/caso5/TestsGestoresRiesgo.cs` (10 pruebas, módulo satélite
nuevo `Caso5.csproj`).

---

## Decisiones congeladas

D-108 a D-111 (4 decisiones) más la precisión derivada de D-109, registradas en
`DECISIONES_CASO5_V1.md`. Ninguna reasignada a contenido distinto del originalmente registrado.
Todas 🟢 Aprobadas e implementadas — ninguna queda como deuda técnica bloqueante dentro del alcance
de Caso 5A (3 métricas de D-111 quedan diferidas explícitamente, no bloqueantes, ver Exclusiones).

---

## Garantías

- **`src/` modificado con autorización explícita en cada paso**: a diferencia de toda fase anterior
  desde Caso 3A, Caso 5A sí toca el núcleo (`GestorCapital`, `ConfiguracionSizing`,
  `BacktestRunner`) — cada cambio fue precedido por propuesta → decisión → especificación →
  autorización expresa del auditor, nunca implementado por iniciativa propia.
- **Migración de Fixed Fractional sin cambio de comportamiento**: verificado que
  `HashCompuesto`/`HashConfiguracionEconomica` son reproducibles con `GestorFixedFractional` (P1),
  y que la fórmula migrada es carácter por carácter la misma que existía inline en `GestorCapital`
  antes de esta fase.
- **D-092/D-095 preservadas para cualquier gestor**: verificado explícitamente que la clasificación
  de intención (P2) y la normalización de Cross-Zero (P3) producen resultados idénticos con los 3
  gestores — la única diferencia entre ellos es el valor de `cantidadCalculada`.
- **Aislamiento estrategia/gestor confirmado**: la misma estrategia (EMA Cross) emite exactamente
  la misma secuencia de señales bajo los 3 gestores (P7) — ningún gestor de riesgo influye en la
  lógica de decisión de ninguna estrategia.
- **Identidad experimental reproducible y estable**: dos instancias equivalentes del mismo gestor
  producen el mismo texto de identidad exacto (P10); un gestor sin `IIdentidadGestorRiesgo` hace
  fallar el cálculo del hash económico en vez de producir un resultado silenciosamente incorrecto.
- **Sin optimización de parámetros**: ningún parámetro de ningún gestor (`PorcentajeRiesgo`,
  `MontoRiesgoFijo`, `PorcentajeRiesgoBase`/`DesviacionReferencia`) fue ajustado observando
  resultados — todos fijados por convención declarada (D-030).
- **Kelly/Masaniello explícitamente diferidos, no descartados**: comparten el bloqueo metodológico
  de probabilidad-de-acierto ya identificado en Caso 2.3 — no resuelto automáticamente por abrir
  esta fase.
- **Divergencias entre especificación e implementación real, corregidas y documentadas, no
  ocultas**: extensión de firma de `GestorCapital.Ajustar` (necesaria para Volatility Sizing),
  descubrimiento de 5 consumidores reales de `ConfiguracionSizing`/`PorcentajeRiesgo` no
  capturados por la búsqueda textual inicial, alcance reducido de `RachaPositivaMaxima` (D-111) —
  todos registrados en `AUDITORIA_CASO5A_V1.md` §4, ninguno requirió una decisión D-N nueva más
  allá de la precisión de D-109.

---

## Exclusiones (explícitas)

- **Kelly fraccionado y Masaniello**: fuera de esta versión — bloqueo metodológico de Caso 2.3 no
  resuelto, candidatos de una sub-fase posterior.
- **Sistema recomendador de gestores**: fuera de esta versión — reservado como Caso 5B, requiere
  que Caso 5A produzca evidencia comparativa primero.
- **Límites de exposición/drawdown/circuit breakers**: fuera de esta versión — framing original de
  "Caso 5" antes de esta propuesta, diferido explícitamente a una fase posterior distinta.
- **Portfolio multi-instrumento**: fuera de esta versión — motor actual asume un solo instrumento
  por corrida, sin modelo de exposición agregada.
- **`RachaPositivaMaxima`, duración de drawdown, riesgo de ruina** (D-111): diferidas, no
  bloqueantes — la primera requiere tocar `PerfilMultiTf.cs`, fuera del alcance autorizado de esta
  fase; las otras dos no tienen fuente de dato tan directa como las métricas ya implementadas.
- **`LaboratorioSintetico.csproj`**: falla de compilación preexistente, no causada por Caso 5A
  (verificado por `git log`) — no corregida dentro de esta fase.
- **`IStrategy` y las 6 estrategias existentes intactas**: ninguna modificación de código, ninguna
  recibe portfolio/cash/sizing en su firma.
- **`AplicadorFill`/`ResolutorCrossZero`/`ConsumidorFifo`**: sin ninguna modificación.

Todo lo anterior queda registrado en `DECISIONES_CASO5_V1.md`,
`ESPECIFICACION_IMPLEMENTACION_GESTORES_RIESGO_V1.md` y `AUDITORIA_CASO5A_V1.md` — fuera de esta
versión.

---

## Evidencia

- **10/10 pruebas Caso 5A** (`caso5/Program.cs`, `TestsGestoresRiesgo.EjecutarTodos()`).
- **126/126 tests de producción** sin regresión, incluyendo `GestorCapitalTests.cs` migrado (mismas
  aserciones, mismos valores esperados que antes de esta fase).
- **14/15 `.csproj` satélite de `exploration/laboratorio/` compilan limpio** tras la migración de
  los 5 consumidores de `ConfiguracionSizing`/`PorcentajeRiesgo` detectados — el único que no
  compila (`LaboratorioSintetico.csproj`) es una falla preexistente ajena a esta fase.
- **`HashCompuesto` de Caso 1**: `A48CCC57DA1919F533F4D532FDC0F945705681DCDA813B385BBFE7F44F40998E`
  — verificado idéntico tras la implementación completa de Caso 5A.
- **5 baselines congelados** (`caso1-v1-experimental`, `caso2-v1-experimental`,
  `caso3a-v1-experimental`, `caso3b-v1-experimental`, `caso4-v1-experimental`): sin regenerar ni
  alterar.
- **`src/` y `tests/` modificados exclusivamente dentro del alcance autorizado**: `git status
  --porcelain -- src/ tests/` muestra únicamente los archivos de Caso 5A (`GestorCapital.cs`,
  `ConfiguracionSizing.cs`, `BacktestRunner.cs`, 5 archivos nuevos de gestores/interfaces,
  `GestorCapitalTests.cs`) — ningún archivo fuera de lo decidido en D-108/D-109/D-110.
- Auditoría de cierre: `caso5/AUDITORIA_CASO5A_V1.md`.

---

## Regla de evolución

Cualquier extensión que amplíe el alcance de Caso 5A — Kelly/Masaniello, límites de exposición,
sistema recomendador, portfolio multi-instrumento, corrección de `RachaPositivaMaxima`/duración de
drawdown/riesgo de ruina — requiere una **nueva fase**, nunca una edición in-place de V1 (mismo
principio que la regla de evolución de `VERSION_EXPERIMENTAL_CASO3B_V1.md`/
`caso4/VERSION_EXPERIMENTAL_CASO4_V1.md`).

```
V1 Experimental — Caso 5A (congelada)
        |
        v
  Kelly/Masaniello resueltos / limites de exposicion / recomendador / metricas diferidas
        |
        v
Caso 5B — o fase equivalente
```

---

## Fuera de alcance de este documento

No se implementó código adicional. No se modifica ningún módulo. No se selecciona ni abre ninguna
fase siguiente (Caso 5B, gestión avanzada de exposición, portfolio multi-instrumento) — conforme a
la restricción explícita de este cierre.

---

## Criterio de cierre de este documento

- ✓ Identificación formal (nombre, versión, estado, fecha) registrada.
- ✓ Componentes incluidos listados con archivo y decisión de origen (D-108 a D-111 + precisión de
  D-109).
- ✓ Decisiones congeladas referenciadas, sin reasignaciones, todas aprobadas e implementadas.
- ✓ Garantías (migración sin cambio de comportamiento, D-092/D-095 preservadas, aislamiento
  estrategia/gestor, identidad reproducible y estable, sin calibración, Kelly/Masaniello
  diferidos, divergencias corregidas y documentadas) declaradas y respaldadas por evidencia ya
  verificada.
- ✓ Exclusiones declaradas explícitamente (Kelly/Masaniello, recomendador, límites de exposición,
  multi-instrumento, métricas diferidas de D-111, `LaboratorioSintetico.csproj`).
- ✓ Evidencia referenciada (10/10 + 126/126 + 14/15 satélites, hash Caso 1 intacto, 5 baselines
  intactos, alcance de `src/`/`tests/` modificado confirmado exhaustivo y autorizado).
- ✓ Regla de evolución (nueva fase ante cambio de alcance) establecida.
- ⏳ Pendiente: preparación de commit y tag `caso5a-v1-experimental`.
