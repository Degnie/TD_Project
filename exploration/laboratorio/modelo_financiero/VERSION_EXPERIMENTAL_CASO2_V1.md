# Versión Experimental — Caso 2: Modelo Financiero

Estado: **documento de congelamiento oficial — cierre de Caso 2, Paso 3** (autorizado tras
aprobación de las auditorías de cierre 2.1-2.4 y `DEUDA_TECNICA_CASO2_V1.md`). A partir de este
documento, el Caso 2 queda congelado como **V1 Experimental**. Mismo patrón que
`VERSION_EXPERIMENTAL_CASO1_V1.md`.

---

## Identificación

- **Nombre**: Caso 2 — Modelo financiero
- **Versión**: V1 Experimental
- **Estado**: Congelado
- **Fecha de congelamiento**: 2026-08-11
- **Base de aprobación**: auditorías de cierre de Caso 2.1, 2.2, 2.3, 2.4 + `DEUDA_TECNICA_
  CASO2_V1.md`, todas aprobadas por auditoría.

---

## Componentes incluidos

**Modelo económico base** (Caso 2.1, D-057-D-062): `Instrumento(Simbolo, TasaMargen)`
(`src/Domain/Shared/Instrumento.cs`), con `Instrumento.Default = ("N/A", 0.1m)` (D-057, D-061)
como única fuente del valor histórico. `RegistroIncapacidad` (`src/Domain/Broker/
RegistroIncapacidad.cs`) observa capacidad económica vía `ValidadorCapacidad`/
`CalculadoraReservaPreventiva`, evaluado sobre cada `OrderRequest` antes del `Fill`, sin bloquear
nunca la ejecución (D-059/D-060). `tasaMargen` propagado end-to-end hasta `ResolutorVela`/
`AplicadorFill`, garantizando una única trayectoria económica oficial (D-062).

**Modelo de costes** (Caso 2.2, D-063-D-065): `ConfiguracionCostes(TasaComision, TasaSlippage)`
(`src/Domain/Shared/ConfiguracionCostes.cs`), `Default=(0m,0m)`. `CostoTotal = Comision +
Slippage`, con `Comision = Cantidad × PrecioFill × TasaComision` y slippage aplicado solo a
órdenes Market (`Cantidad × PrecioFill × TasaSlippage`, D-063). Coste descontado de `Cash` en las
3 rutas de `AplicadorFill` (abrir/aumentar, reducir FIFO, Cross-Zero con prorateo, D-065).

**Gestión de capital** (Caso 2.3, D-066-D-071): `GestorCapital.Ajustar`
(`src/Domain/Portfolio/GestorCapital.cs`) como capa externa entre `Strategy.Observar` y
`ValidadorBolsaRequests`/`ValidadorCapacidad` (D-066, D-068, D-070). Modelo único: porcentaje de
capital disponible — `Cantidad = (Cash − Margin) × PorcentajeRiesgo`
(`ConfiguracionSizing(PorcentajeRiesgo)`, `Default=null`, D-067). Solo transforma `Cantidad` de
órdenes existentes, nunca crea ni elimina órdenes (D-071). **Gestión de capital por porcentaje
existe como capacidad implementada y probada (P1-P6, `GestorCapitalTests.cs`)** — la configuración
congelada del baseline de referencia (`baseline_financiero_final/`) **no activa sizing dinámico**,
por deuda técnica registrada en D-084 (`GestorCapital` no distingue apertura/cierre de posición al
recalcular `Cantidad`, produciendo residuos de lotes sin límite en corridas largas con reaperturas
de martingala — ver `DEUDA_TECNICA_CASO2_V1.md` §1.3).

**Métricas financieras** (Caso 2.4, D-072-D-078): `MetricasFinancieras` (record) y
`CalculadoraMetricasFinancieras.Calcular` (`exploration/laboratorio/modelo_financiero/`), derivadas
exclusivamente de `EquityCurve`/`Cash`/`Margin`/`Trades` (D-077, fuente única). Campos: Capital
inicial (D-072), Cash final, Equity final, PnL total, Drawdown máximo porcentual (`decimal?`,
`null` si `EquityCurve` vacía, D-073/D-078), Exposición máxima (`Max(PortfolioSnapshot.Margin)`,
D-075). Integrado en `EjecutorProtocolo.EjecutarUnTimeframe`, poblado solo en corridas `Success`.

**Configuración experimental**: `ConfiguracionExperimento(CapitalInicial, Velas, Warmup,
Instrumento?, Costes?, Sizing?)` — todos los campos económicos opcionales con default equivalente
al comportamiento histórico (D-061), preservando compatibilidad con el motor congelado de Caso 1.

---

## Decisiones congeladas

D-057 a D-085 (29 decisiones), registradas en `DECISIONES_MODELO_ECONOMICO_V1.md`. Ninguna
reasignada a contenido distinto del originalmente registrado — la única discrepancia de numeración
detectada durante la fase fue resuelta creando D-077/D-078 como decisiones nuevas, no
reutilizando D-072/D-074 (mismo principio que corrigió D-043/D-053 en Caso 1). D-079 a D-083 son
aprobadas y activas; D-084/D-085 son deuda técnica registrada (🟡), explícitamente no resueltas en
V1.

---

## Garantías

- **Reproducibilidad**: dada la misma `ConfiguracionExperimento` (incluyendo `Instrumento`,
  `Costes`, `Sizing`), dos ejecuciones producen el mismo resultado económico — verificado por P7
  (determinismo) en `TestsMetricasFinancieras` y por el determinismo ya garantizado de
  `EjecutorProtocolo` (Caso 1) extendido sin ruptura.
- **Consistencia económica**: `EquityCurve` (trayectoria oficial, RN-11) y `CashFinal`/`Trades`
  (estado final) provienen de la misma configuración de `Instrumento`/`Costes` en todo punto del
  motor — verificado explícitamente por P3 de `ModeloEconomicoBaseTests` tras la corrección D-062
  (antes de la cual ambas rutas podían divergir).
- **Trazabilidad**: toda decisión de diseño relevante está numerada (D-057 a D-078) y registrada en
  documento verificable, con justificación y evidencia de código.
- **No regresión sobre Caso 1**: el baseline `caso1-v1-experimental`
  (`A48CCC57DA1919F533F4D532FDC0F945705681DCDA813B385BBFE7F44F40998E`) permanece bit-a-bit idéntico
  tras la implementación completa de Caso 2.1-2.4 — verificado en cada sub-fase, no solo al final.
- **107/107 tests de producción** pasando sin modificación de ningún test pre-existente de Caso 1.
- **Separación estrategia/economía (P-002)**: ninguna `IStrategy` existente fue modificada para
  conocer `Cash`/`Margin`/`Equity` — `GestorCapital` y el modelo de costes operan como capas
  externas al conocimiento de la estrategia, verificado por ausencia de cambios en
  `EstrategiaTresMosqueteros.cs`/`EstrategiaMhiMayoria.cs`/`EstrategiaEmaCross.cs`.

---

## Exclusiones (explícitas)

- **Sin duración de drawdown** (D-074): definida conceptualmente, sin DTO/cálculo/prueba en V1.
- **Sin Masaniello**: evaluado y descartado para esta versión (`EVALUACION_MODELOS_GESTION_
  RIESGO_V1.md`) — requiere modelo probabilístico, horizonte y objetivo aún no definidos.
- **Sin sizing basado en Equity ni sizing adaptativo**: `GestorCapital` implementa únicamente
  porcentaje de `Cash − Margin` fijo; ninguna variante que use racha, winrate histórico o régimen
  de mercado está implementada ni permitida en su forma actual (D-067).
- **Sin spread ni funding** (D-063): limitación estructural del motor (sin libro de órdenes), no
  solo de alcance.
- **Sin métricas de rendimiento ajustado por riesgo**: sin Sharpe, Sortino, Profit Factor, Calmar
  ni equivalentes.
- **Sin ranking financiero** (D-076): ningún reporte generado por esta versión declara una
  estrategia financieramente superior a otra.
- **Sin optimización ni recomendación de inversión**: ningún parámetro económico
  (`TasaMargen`/`Costes`/`PorcentajeRiesgo`) es calculado o ajustado por el sistema — todos son
  input explícito del experimento (Sección 5, `DEUDA_TECNICA_CASO2_V1.md`).
- **Sin sizing activo en estrategias existentes**: `Sizing=null` en toda configuración congelada de
  referencia de V1 — activar `GestorCapital` con reaperturas/martingala tiene un defecto conocido
  (D-084) no resuelto. Sizing solo puede activarse hoy fuera de una configuración congelada, con
  conocimiento explícito de esa limitación.
- **Sin calibración de capital**: `CapitalInicial=1000` se mantiene por continuidad experimental
  con Caso 1, sin ajustarlo para que la relación con la `Cantidad` nominal fija de las estrategias
  "parezca" financieramente razonable (D-085). Ningún valor de `CapitalInicial` en esta versión
  debe interpretarse como calibrado o recomendado.
- **Sin interpretación financiera real**: ninguna cifra de `MetricasFinancieras` en esta versión
  (incluidas las del baseline congelado) representa una simulación de capital real, una proyección
  de retorno ni una recomendación de dimensionamiento de posición — advertencia explícita
  registrada en cada reporte financiero generado (D-085, D-058).

Todo lo anterior queda registrado en `DEUDA_TECNICA_CASO2_V1.md` — fuera de esta versión.

---

## Evidencia

**Baseline financiero de referencia**: `exploration/laboratorio/modelo_financiero/
baseline_financiero_final/` (`IDENTIDAD_EXPERIMENTAL.json`, `REPORTE_FINANCIERO_V1.md`,
`HASH_EVIDENCIA.txt`, `ANEXOS/`). Generado vía `ProgramBaselineFinanciero.cs` (D-081), con
`Instrumento`/`Costes` activos y `Sizing=null` (D-084).

- **HashCompuesto**: `A48CCC57DA1919F533F4D532FDC0F945705681DCDA813B385BBFE7F44F40998E` —
  idéntico al baseline congelado de Caso 1 (`baseline_final/`), confirmando que la configuración
  económica no altera la identidad estratégica (D-082).
- **HashConfiguracionEconomica**: `FEBD8B24F4DDBD3F5AC78BFD8354E824731C6EF35A0D4A9A229CD9EDF74EF3A3`
  — distinto del hash de configuración default, identificando de forma única
  `Instrumento(BTCUSDT, 0.1)` + `ConfiguracionCostes(0.001, 0.001)` + `Sizing=null` (D-082).
- **Reproducibilidad**: verificada mediante 2 corridas independientes dentro del mismo proceso —
  mismo `HashCompuesto` y mismo `HashConfiguracionEconomica`.
- **Advertencia obligatoria D-085**: presente en `REPORTE_FINANCIERO_V1.md` §5 y
  `HASH_EVIDENCIA.txt` — las métricas financieras absolutas reflejan la configuración histórica de
  tamaño de posición, no una simulación de capital real ni una recomendación de dimensionamiento.
- **Regresión**: 107/107 tests de producción, 7/7 `TestsEjecutorProtocolo` (pipeline), 7/7
  `TestsMetricasFinancieras` — sin cambios respecto al estado de cierre de Caso 2.4.

---

## Regla de evolución

Cualquier modificación que cambie comportamiento económico — nuevo modelo de sizing, nuevo
componente de coste, cambio de fórmula de margen, implementación de D-074 — requiere una **nueva
versión experimental** (V2 del modelo financiero), nunca una edición in-place de V1 (mismo
principio que la regla de evolución de `VERSION_EXPERIMENTAL_CASO1_V1.md`, y consistente con
D-069: sizing nuevo en una estrategia ya produce automáticamente una nueva identidad experimental
vía `HashCompuesto`, sin código adicional).

```
V1 Experimental — Modelo financiero (congelada)
        ↓
  cambio de modelo económico/costes/sizing/métricas
        ↓
V2 Experimental — Modelo financiero
```

---

## Fuera de alcance de este documento

No se implementó código. No se modifica ningún módulo. No se abre implementación de D-074,
Masaniello, sizing avanzado ni métricas adicionales — conforme a la restricción explícita de este
cierre.

---

## Criterio de cierre de este documento

- ✓ Identificación formal (nombre, versión, estado, fecha) registrada.
- ✓ Componentes incluidos listados con archivo y decisión de origen (2.1-2.4), incluyendo la
  aclaración de que gestión de capital está implementada pero no activa en el baseline (D-084).
- ✓ Decisiones congeladas referenciadas (D-057 a D-085), sin reasignaciones — D-084/D-085
  explícitamente marcadas como deuda técnica, no como aprobadas activas.
- ✓ Garantías (reproducibilidad, consistencia económica, trazabilidad, no regresión, separación
  P-002) declaradas y respaldadas por evidencia ya verificada en las auditorías de cierre 2.1-2.4.
- ✓ Exclusiones declaradas explícitamente, incluyendo sizing activo/calibración de capital/
  interpretación financiera real, remitiendo a `DEUDA_TECNICA_CASO2_V1.md`.
- ✓ Evidencia del baseline financiero referenciada (hash, reproducibilidad, advertencia D-085,
  regresión).
- ✓ Regla de evolución (nueva versión ante cambio de comportamiento económico) establecida.
- ✓ Ningún cambio de código — verificado (`git status --porcelain -- src/ tests/` sin cambios
  respecto al cierre de Caso 2.4).
- ✓ Auditoría del baseline financiero aprobada con observaciones documentadas — D-079 a D-085
  revisadas.
- ⏳ Pendiente: preparación de commits y tag de Caso 2.
