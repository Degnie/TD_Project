# Propuesta — Prueba Integral del Sistema (Validación de Madurez, no Evolución)

Estado: **documento de apertura — previo a cualquier decisión o implementación**. Define el diseño
exacto de la prueba integral solicitada: estructura del módulo, formato del dataset sintético,
matriz escenario×estrategia, y qué mide cada validación — siguiendo el mismo ciclo que toda fase
anterior: propuesta → decisión → implementación → auditoría. No implementa nada todavía.

**Naturaleza de esta fase**: validación, no evolución. No se abre numeración D-N nueva salvo que
aparezca una contradicción de diseño genuina durante la ejecución (instrucción explícita del
auditor: "si aparece una contradicción de diseño, detener y documentar antes de corregir").

---

## 1. Hallazgo de diseño previo a las opciones — `EjecutorProtocolo` exige datasets en disco

Verificado en código (`exploration/laboratorio/protocolo/EjecutorProtocolo.cs:113-122`):
`EjecutorProtocolo.EjecutarUnTimeframe` **siempre lee** `{DirDatasets}/{Timeframe}/
{NombreDataset}_{Timeframe}.csv` + `metadata.json` desde disco — no existe ninguna vía de
inyección de velas en memoria. Si el archivo no existe, la corrida queda `Incomplete`, no lanza
excepción.

**Consecuencia para el diseño**: para usar `EjecutorProtocolo` (necesario para obtener
`IdentidadExperimentoCompleta`/hashes, `MetricasFinancieras`, `Incapacidades` y el reporte
financiero — todo lo que la validación pide auditar), los 5 escenarios sintéticos deben
**materializarse como archivos CSV físicos** antes de cada corrida, formato base de 6 columnas
(`InicioUtcMs, Open, High, Low, Close, Volume`, sin encabezado) — mismo formato ya usado en
`exploration/laboratorio/datasets/reales/BTCUSDT/`. Esto no es una limitación de esta prueba: es
el contrato real de `EjecutorProtocolo`, verificado, no asumido.

---

## 2. Ubicación y estructura del módulo

Nuevo módulo satélite, mismo patrón que `caso3/`/`caso4/`: `exploration/laboratorio/
validacion_integral/`.

```
validacion_integral/
 ├── ValidacionIntegral.csproj   — enlaza las 6 estrategias + EjecutorProtocolo.cs +
 │                                  IdentidadExperimentoCompleta.cs + LectorDerivado.cs +
 │                                  MetricasFinancieras.cs/CalculadoraMetricasFinancieras.cs
 ├── Program.cs                  — genera los datasets sintéticos (si no existen), ejecuta
 │                                  TestsValidacionIntegral.EjecutarTodos()
 ├── GeneradorDatasetSintetico.cs — escribe CSV+metadata.json por escenario (ver §4)
 ├── datasets/                   — CSV sintéticos generados (excluidos de git, mismo criterio
 │                                  que exploration/laboratorio/protocolo/resultados/ en
 │                                  Protocolo.csproj: <Compile Remove>, y .gitignore si aplica)
 └── TestsValidacionIntegral.cs  — ejecuta la matriz escenario×estrategia, valida integridad,
                                    reproducibilidad, capas y pruebas negativas
```

**Por qué módulo nuevo y no reutilizar `caso3/`/`caso4/`**: esta prueba no pertenece a la evidencia
de ninguna decisión D-099-D-107 (Caso 3B) ni D-091-D-098 (Caso 4) — es transversal a las 5 fases
congeladas. Mezclarla en un módulo existente contaminaría la evidencia de esa fase con evidencia de
una auditoría distinta, mismo criterio que motivó D-098 (aislamiento estructural) en Caso 4.

**Datasets sintéticos generados, no versionados como CSV en el repo**: se generan
programáticamente en cada ejecución (determinista, sin `Random`) — evita versionar archivos de
datos binarios/grandes y mantiene el dataset como código auditable (el generador es la fuente de
verdad, no el CSV).

---

## 3. Dataset sintético — formato y generación

Mismo patrón de generación ya usado en `TestsEstrategiaNeutral.cs`/`TestsEstrategiaVolumenBreakout.cs`
(`DatasetSintetico`/`DatasetBase`, generador paramétrico por función `Func<int, decimal>` o
`Action<Candle[], int>` sobre índice de vela), extendido para escribir el resultado como CSV real
(`InicioUtcMs, Open, High, Low, Close, Volume`, timestamps consecutivos en milisegundos) +
`metadata.json` mínimo (`sha256` calculado sobre el CSV generado, `intervalo`, `velas` = conteo).

**Timeframe único**: `1D` — suficiente para ejercitar el pipeline completo, evita la complejidad de
generar y sincronizar múltiples timeframes derivados (`LectorDerivado` solo se necesita para
datasets derivados de 10 columnas; el formato base de 6 columnas no lo requiere).

---

## 4. Escenarios — diseño concreto

| # | Nombre | Generador | Objetivo verificado |
|---|---|---|---|
| 1 | Alcista | Tendencia ascendente monótona + rupturas periódicas de máximo + volumen creciente | Señales Long, apertura, mantenimiento, cierre correcto |
| 2 | Bajista | Simétrico al 1, tendencia descendente + rupturas de mínimo + volumen suficiente | Señales Short, cierre de Long existente, reversión inversa |
| 3 | Lateral | Rango estrecho fijo ± ruido pequeño acotado (sin tendencia neta) | Ausencia de señales espurias, estabilidad sin oportunidad clara |
| 4 | Cambio brusco de régimen | Tendencia alcista sostenida → inversión abrupta de dirección en un punto fijo conocido, con salto de volatilidad | Transición Cross-Zero/reversión, cierre+apertura consecutiva en la misma vela o velas contiguas |
| 5 | Económico extremo | Reutiliza el generador del Escenario 1 (para garantizar que al menos una orden se emita) + `CapitalInicial=1m`, `Instrumento("SINT", 0.1m)`, `Costes(0.001m, 0.001m)` — plantilla ya verificada en `TestsReporteIncapacidades.cs` que garantiza `RegistroIncapacidad.Count > 0` | Sizing corregido, costes, margen, incapacidad registrada (no bloqueante), reportes financieros bajo escasez |

Cada escenario: **300 velas** (suficiente para que `EstrategiaZScoreReversion`/
`EstrategiaVolumenBreakout` superen su warmup de ventana 20, sin ser tan largo que dificulte
inspección manual del resultado).

**Determinismo garantizado**: ningún generador usa `Random` — todos son funciones puras de índice
de vela, mismo criterio ya verificado en `EstrategiaNeutral` (D-086/D-087, "sin aleatoriedad").

---

## 5. Matriz escenario × estrategia

No las 30 combinaciones (5 escenarios × 6 estrategias) — selección dirigida por lo que cada
combinación puede realmente ejercitar, evitando ejecutar combinaciones sin valor de verificación
adicional (mismo criterio de "sin trabajo no solicitado" aplicado en todo el proyecto):

| Estrategia | Escenarios | Motivo |
|---|---|---|
| Tres Mosqueteros (`maxMartingalas=2`) | 1, 3, 5 | Caso 1: compatibilidad histórica, martingala, capital extremo |
| MHI Mayoría (`maxMartingalas=2`) | 3 | Segunda estrategia de patrón/martingala de Caso 1, control en lateral |
| EMA Cross (`10,30`) | 1, 2, 4 | Tendencia, sin martingala, mantiene posición sin límite de velas — ideal para 1/2/4 |
| Z-Score Reversal (`20, 2.0, 0.5`) | 3, 4 | Reversión a la media: lateral (su hábitat natural) y cambio brusco |
| Estrategia Neutral (`ciclo=10`) | 1, 2, 3 | Control experimental — debe comportarse igual en cualquier escenario (independencia del mercado ya verificada en Caso 3A, esta prueba la re-confirma bajo datos nuevos) |
| VolumenBreakout (`20, 1.5, 20`) | 1, 2, 4, 5 | Long (1), Short (2), reversión (4 — su caso de uso central), condiciones económicas (5) |

Con costes/sizing activos (Caso 2/Caso 4) en **al menos una corrida por estrategia** — no solo en
el Escenario 5 — para validar "costes afectan métricas" de forma aislada del caso extremo:
Escenario 1 se corre dos veces por cada estrategia asignada a él — una con `Costes=null`/
`Sizing=null` (equivalente a Caso 1), otra con `Costes(0.001, 0.001)` + `Sizing(0.1)` activos
(equivalente a Caso 4) — permite comparar directamente el efecto de activar el modelo económico
sobre el mismo dataset.

---

## 6. Validaciones — qué mide cada una y con qué mecanismo ya existente

**Integridad** (por corrida): velas procesadas (`Count` del CSV generado), `OrderRequest` totales
(capturables vía el mismo callback `onEvaluacion`/`onOperacionResuelta` ya soportado por todas las
estrategias — sin modificar ninguna), Fills (derivables de `MetricasFinancieras`/`Trades`, ya
expuestos), posición final, `CashFinal`/`EquityFinal` (`MetricasFinancieras`, sin cálculo nuevo).

**Reproducibilidad**: `EjecutorProtocolo.Ejecutar` con la misma `EntradaProtocolo` invocado 2
veces, comparar `Identidad.HashCompuesto` + `Identidad.HashConfiguracionEconomica` + reporte
generado por `ReporteFinancieroGenerador` — mismo patrón exacto ya usado en
`TestsReporteIncapacidades.VerificarDeterminismo`, sin código nuevo en `src/`.

**Auditoría de capas** (verificación estructural, no numérica):
- *Estrategia no conoce economía*: verificado por construcción — ninguna de las 6 estrategias
  recibe `PortfolioState`/`Cash`/`Sizing` en su constructor ni en `Observar` (contrato `IStrategy`
  ya lo garantiza, esta prueba lo confirma por inspección, no requiere un test nuevo de tipo).
- *Motor no modifica decisiones estratégicas*: comparar la secuencia de `Side` de las
  `OrderRequest` emitidas por la estrategia (capturadas vía callback) contra los `Fills`
  resultantes — incluyendo casos con Sizing activo (D-095: `GestorCapital` puede normalizar
  cantidad pero no debe alterar `Side`).
  - `ponytail: no existe hoy un canal directo para capturar Cantidad post-GestorCapital sin instrumentar BacktestRunner; si se necesita, evaluar solo si aparece evidencia concreta de discrepancia, no de antemano.`
- *Sizing no altera cierres incorrectamente*: reutiliza directamente los 3 criterios de aceptación
  ya verificados en D-095 (Caso 4) — no se re-diseñan, se re-ejecutan como regresión.
- *Costes afectan métricas*: comparación directa Escenario 1 con/sin costes (§5) —
  `MetricasFinancieras.CashFinal` debe diferir.
- *Incapacidad se registra correctamente*: Escenario 5, `Incapacidades.Count > 0` + verificación
  de lenguaje neutral en el reporte (mismo patrón que P2 de `TestsReporteIncapacidades.cs`).

**Pruebas negativas** (verificar respuesta del sistema, nunca corregir automáticamente):
- Capital insuficiente: Escenario 5 (ya lo cubre).
- Reversión rápida: Escenario 4 con VolumenBreakout (ya lo cubre, mismo mecanismo que P7/P8 de
  Caso 3B).
- Órdenes consecutivas en la misma vela: revisar si algún escenario+estrategia produce 2
  `OrderRequest` en una sola llamada a `Observar` (ya ocurre en la reversión de VolumenBreakout,
  Escenario 4) — confirmar que el motor las procesa en el orden emitido, sin necesitar código
  nuevo.
- Ausencia de señales: Escenario 3 con Neutral vs. Escenario 3 con Z-Score (dos formas distintas de
  "sin oportunidad": Neutral siempre opera por cadencia fija, Z-Score puede no cruzar umbral).
- Datos extremos: Escenario 5 (precio alto + capital bajo simultáneos).

---

## 7. Restricciones confirmadas (heredadas, sin relajar)

- Sin modificar `src/`, `IStrategy`, ninguna de las 6 estrategias, ningún baseline congelado
  (`caso1-v1-experimental` a `caso4-v1-experimental`/`caso3b-v1-experimental`).
- Sin modificar ninguna decisión D-001 a D-107.
- Sin calibrar: los parámetros de cada escenario/estrategia se fijan por diseño explícito antes de
  ejecutar (tabla §5/§4), nunca ajustados tras ver resultados.
- Sin introducir capacidades nuevas: todo mecanismo usado (callbacks, `EjecutorProtocolo`,
  comparación de hashes) ya existe; no se propone ningún tipo/componente nuevo en `src/`.
- Si aparece una contradicción de diseño durante la ejecución: detener, documentar como hallazgo
  explícito en `AUDITORIA_PRUEBA_INTEGRAL_SISTEMA_V1.md`, no corregir en el mismo ciclo salvo
  autorización explícita — mismo patrón que D-095 en Caso 4 (hallazgo detectado → reportado →
  autorizado → resuelto en un ciclo separado).

---

## 8. Entregable final

`exploration/laboratorio/AUDITORIA_PRUEBA_INTEGRAL_SISTEMA_V1.md` con las 8 secciones exactas ya
especificadas por el auditor: dataset sintético utilizado, escenarios ejecutados, estrategias
evaluadas, resultados obtenidos, errores encontrados, contradicciones detectadas, elementos
validados, recomendaciones futuras.

---

## Fuera de alcance de este documento

No se implementa código. No se genera ningún dataset todavía. No se ejecuta ninguna corrida. No se
abre ninguna decisión D-N (a menos que la ejecución revele una contradicción genuina, en cuyo caso
se documentará y se detendrá para autorización, no se resolverá aquí).

---

## Próximo paso

Aprobación explícita del auditor sobre: (a) el diseño del módulo `validacion_integral/`, (b) los 5
escenarios y su generación determinista, (c) la matriz escenario×estrategia de §5 (o su ajuste), y
(d) el criterio de "detener ante contradicción" de §7. Tras aprobación: implementación del
generador de datasets, ejecución de la matriz, y redacción del documento de auditoría final.
