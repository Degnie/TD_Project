# Auditoría — Prueba Integral del Sistema (Validación de Madurez, Caso 1 a Caso 4)

Estado: **documento de cierre de la validación integral**. Consolida evidencia de una ejecución
completa end-to-end del laboratorio experimental sobre datos sintéticos controlados. No es una
fase de evolución — no abre ni modifica ninguna decisión D-001 a D-107, no toca `src/`, `tests/`,
`IStrategy` ni ningún baseline congelado.

Origen: `PROPUESTA_PRUEBA_INTEGRAL_V1.md`. Implementación: `exploration/laboratorio/
validacion_integral/` (`ValidacionIntegral.csproj`, `GeneradorDatasetSintetico.cs`,
`TestsValidacionIntegral.cs`, `Program.cs`).

---

## 1. Dataset sintético utilizado

5 escenarios, `1D` como único timeframe, **300 velas** cada uno, generados de forma **determinista
(sin `Random`)** por `GeneradorDatasetSintetico.cs` — cada escenario es una función pura de índice
de vela, regenerable exactamente. Formato base de 6 columnas (`TimestampUtcMs,Open,High,Low,Close,
Volume`), mismo formato ya leído por `LectorDerivado`/`EjecutorProtocolo` sin ningún cambio de
código. Timestamps consecutivos desde `2024-01-02T00:00:00Z` en pasos de 1 día.

| Escenario | Diseño | Verificado que ejercita |
|---|---|---|
| 1 — Alcista | Tendencia ascendente monótona (`Close = 100 + i×2`), picos periódicos de volumen (`i%10==0`, `Volume=100` vs. base `10`) coincidiendo con ruptura de máximo | Señales Long, apertura, mantenimiento, cierre |
| 2 — Bajista | Simétrico al 1, tendencia descendente + picos de volumen en rupturas de mínimo | Señales Short, cierre de Long, reversión inversa |
| 3 — Lateral | Rango estrecho fijo (`Close≈500`) con ruido periódico acotado (`(i%7)×0.3-0.9`, determinista, no aleatorio), volumen constante | Ausencia de señales espurias, estabilidad sin oportunidad |
| 4 — Cambio brusco de régimen | Tendencia alcista sostenida (mitad 1) → inversión abrupta de pendiente en el punto medio (mitad 2), con salto de volatilidad y de volumen en la vela de quiebre | Transición de régimen, Cross-Zero/reversión, cierre+apertura consecutiva |
| 5 — Económico extremo | Reutiliza el Escenario 1 (garantiza al menos una orden) — la condición extrema vive en `EntradaProtocolo` (`CapitalInicial=1m`, `Instrumento("SINT",0.1m)`, `Costes(0.001,0.001)`), no en el dataset | Sizing, costes, margen, incapacidad, reportes bajo escasez |

**Hallazgo corregido durante la generación** (ver §6): la primera versión de Escenario 1/2 usaba
crecimiento **lineal** de volumen (`10 + i×0.5`) en vez de picos periódicos — verificado
matemáticamente que un crecimiento lineal moderado nunca cruza un múltiplo fijo (`1.5×`, D-105)
sobre su propia media móvil. Corregido a picos puntuales antes de considerar el dataset válido.

---

## 2. Escenarios ejecutados

Los 5 escenarios se generaron y escribieron a disco (`validacion_integral/datasets_generados/`,
excluido de git — evidencia regenerable, no versionada como dato binario, mismo criterio ya
aplicado a otros artefactos de corrida temporal en el proyecto) antes de cada ejecución de la
matriz. Ningún escenario requirió ajuste de parámetros del motor ni de ninguna estrategia — solo
la corrección de generación descrita en §1/§6.

---

## 3. Estrategias evaluadas

Las 6 estrategias congeladas del laboratorio, con matriz **dirigida** (16 combinaciones, no las 30
posibles) — cada combinación justificada por qué capacidad valida, no ejecutada por cobertura
exhaustiva:

| Estrategia | Escenarios | Motivo |
|---|---|---|
| Tres Mosqueteros (Caso 1) | 1, 3, 5 | Compatibilidad histórica, martingala en tendencia y en ausencia de ella, capital extremo |
| MHI Mayoría (Caso 1) | 3 | Segunda estrategia de martingala, control en lateral |
| EMA Cross (Caso 1, D-054) | 1, 2, 4 | Tendencia sin martingala, mantiene posición sin límite de velas, cruce en el quiebre |
| Z-Score Reversal (Caso 3A) | 3, 4 | Reversión estadística en su hábitat natural (lateral) y bajo cambio brusco |
| Estrategia Neutral (Caso 3A) | 1, 2, 3 | Control experimental — independencia del mercado bajo 3 condiciones distintas |
| VolumenBreakout (Caso 3B) | 1, 2, 4, 5 | Entrada Long, entrada Short, reversión (D-107), condiciones económicas extremas |

Adicionalmente, **Escenario 1 duplicado** con Tres Mosqueteros — una corrida sin costes/sizing
(equivalente a Caso 1), otra con `Costes(0.001,0.001)` + `Sizing(0.1)` activos (equivalente a
Caso 4) — para aislar el efecto del modelo económico sobre las mismas señales estratégicas.

---

## 4. Resultados obtenidos

**16/16 combinaciones de la matriz dirigida: `Estado=Success`.** Ninguna corrida terminó en
`Failed`, ninguna excepción no controlada. Resumen (CashFinal/EquityFinal/PnLTotal/Incapacidades
por combinación, evidencia completa en la salida de `Program.cs`, sección `MatrizDirigida`):

- **Tres Mosqueteros** — Alcista: `CashFinal=10120.00` (PnL positivo, martingala favorecida por
  tendencia). Lateral: `CashFinal=9999.10` (PnL levemente negativo, coherente con costes de
  intentos fallidos sin tendencia). Extremo: `CashFinal=24.88`, **120 incapacidades registradas**,
  corrida sigue `Success`.
- **MHI Mayoría** — Lateral: `CashFinal=10000.90`.
- **EMA Cross** — Alcista/Bajista: `CashFinal=10000.00` (posición mantenida abierta al cierre de
  la corrida, `PnLTotal=0` porque el trade nunca se cerró, `Equity` refleja la posición abierta
  distinta a `Cash` — mismo comportamiento documentado desde Caso 1 D-054). Cambio de régimen:
  `CashFinal=9862.40`, `EquityFinal=10823.00` (cruce detectado en el quiebre).
- **Z-Score Reversal** — Lateral: `CashFinal=10000.00` (mantiene posición). Cambio de régimen:
  `CashFinal=9860.60`, `EquityFinal=9159.00`.
- **Estrategia Neutral** — comportamiento consistente en los 3 escenarios (cadencia fija, PnL
  pequeño en cualquier dirección de mercado, confirmando independencia del mercado ya establecida
  en Caso 3A bajo datos nuevos).
- **VolumenBreakout** — Alcista: `CashFinal=9985.95`, `EquityFinal=10557.50` (entrada Long
  confirmada). Bajista: `CashFinal=9004.05`, `EquityFinal=10557.50` (entrada Short confirmada).
  Cambio de régimen: `CashFinal=9860.00`, `EquityFinal=10847.00` (reversión Long→Short ejecutada).
  Extremo: `CashFinal=-13.33` (negativo, esperado bajo capital=1 con margen consumido),
  **1 incapacidad registrada**, corrida sigue `Success`.

**Escenario 1 duplicado**: `CashFinal=10120.00` sin economía vs. `CashFinal=15175.28` con
costes+sizing activos — confirma que activar el modelo económico cambia el resultado manteniendo
idéntico el dataset y la estrategia.

**Reproducibilidad**: 2 ejecuciones de `EjecutorProtocolo.Ejecutar` con la misma `EntradaProtocolo`
(VolumenBreakout × Escenario 4) — `HashCompuesto` idéntico, `HashConfiguracionEconomica` idéntico,
texto completo del reporte financiero idéntico.

**Total**: 33 hallazgos registrados, **33/33 sin contradicción** en la ejecución final (tras la
corrección de §6).

---

## 5. Errores encontrados

**Ninguno en el motor, en `src/`, ni en ninguna estrategia congelada.** El único error encontrado
perteneció al instrumento de la propia prueba (ver §6) — corregido dentro del alcance de esta
validación, sin tocar ningún componente congelado.

---

## 6. Contradicciones detectadas y resolución

**Contradicción detectada durante la primera ejecución**: la verificación `AuditoriaCapas` sobre
`VolumenBreakout × Escenario5EconomicoExtremo` reportó `Incapacidades=0`, inconsistente con la
expectativa declarada en la propia prueba (§6 de la propuesta: "garantiza `RegistroIncapacidad.
Count > 0`"). Siguiendo la instrucción explícita del auditor ("detectar → aislar → documentar →
decidir", no corregir directamente), se detuvo la ejecución del ciclo de corrección automática y
se aisló la causa raíz antes de tocar código:

1. Verificado que el dataset de Escenario 5 es idéntico al de Escenario 1 (por diseño, §1).
2. Verificado en la matriz dirigida que `VolumenBreakout × Escenario1Alcista` también mostraba
   `PnLTotal=0.00` con `CashFinal=10000.00` exacto — indicando que la estrategia **nunca emitió
   ninguna orden** en ese escenario, no que las órdenes se procesaran sin incapacidad.
3. Cálculo aislado (fuera del motor, verificación matemática directa): con `Volume = 10 + i×0.5`
   (crecimiento lineal) y ventana de 20 velas, la condición `VolumenActual > Media20 × 1.5` nunca
   se cumple para ningún `i` — la brecha entre la vela actual y la media de sus 20 predecesoras
   crece más lento que la propia media bajo una progresión lineal moderada. Verificado
   numéricamente para los primeros 30 índices, confirmado analíticamente.
4. Confirmado que el Escenario 4 (que sí usa saltos abruptos de volumen en el punto de quiebre, no
   una progresión lineal) sí cruzaba el umbral — descartando que el defecto estuviera en
   `EstrategiaVolumenBreakout`/`CondicionVolumen` (D-100/D-103/D-105), que se comportaron
   exactamente según su especificación congelada.

**Causa raíz aislada**: defecto de diseño en `GeneradorDatasetSintetico.Escenario1Alcista`/
`Escenario2Bajista` — un generador experimental de esta misma validación, no un componente del
laboratorio congelado. No requirió ninguna decisión D-N (no es una contradicción del sistema
auditado, es un defecto del arnés de prueba) — mismo criterio ya aplicado en Caso 4.2 al distinguir
un defecto de test fake de una limitación real del motor.

**Resolución**: `Escenario1Alcista`/`Escenario2Bajista` corregidos para usar picos periódicos de
volumen (`Volume=100` cada 10 velas vs. base `10`) en vez de crecimiento lineal — mismo patrón
temporal ya usado para las rupturas de precio (`i%10==0`). Verificado matemáticamente que el nuevo
patrón sí cruza el múltiplo `1.5×` antes de re-ejecutar. Documentado con comentario explícito en el
código (`GeneradorDatasetSintetico.cs`, sección Escenario 1) explicando qué se corrigió y por qué,
para que la corrección misma quede auditable.

**Ninguna otra contradicción** apareció en la re-ejecución (33/33 hallazgos sin contradicción).

---

## 7. Elementos validados

- **Carga de datos**: `LectorDerivado`/`EjecutorProtocolo` procesaron los 5 datasets sintéticos sin
  ningún cambio de código — formato base de 6 columnas confirmado compatible.
- **Ejecución de estrategias**: las 6 estrategias respondieron correctamente a sus condiciones de
  entrada/salida diseñadas específicamente para ejercitarlas (Long, Short, reversión, cadencia
  fija, control estadístico, martingala).
- **Generación de órdenes / resolución de fills / gestión de posiciones**: confirmado a través de
  `MetricasFinancieras` (`CashFinal`/`EquityFinal`/`PnLTotal`) consistentes con el comportamiento
  esperado de cada estrategia en cada escenario.
- **Costes**: Escenario 1 duplicado confirma que activarlos cambia `CashFinal` manteniendo
  constante estrategia y dataset.
- **Modelo económico / sizing**: Escenario 1 duplicado (sizing activo) y matriz Escenario 5
  (capital extremo) ejecutados sin error; los 3 criterios de aceptación de D-095 se dan por
  cubiertos como regresión ya verificada (`GestorCapitalTests.cs`, 126/126 tests de producción),
  no re-diseñados en esta prueba.
- **Métricas financieras**: pobladas en toda corrida `Success`, nunca `null` de forma inesperada.
- **Reportes**: `ReporteFinancieroGenerador` produjo texto idéntico entre 2 corridas idénticas.
- **Identidad experimental**: `HashCompuesto`/`HashConfiguracionEconomica` reproducibles bit a bit.
- **Auditoría y reproducibilidad**: confirmadas explícitamente (§4).
- **Separación de capas**: confirmada por construcción (ninguna estrategia conoce `PortfolioState`/
  `Cash`/`Sizing`, contrato `IStrategy` no los expone) y por comportamiento observado (el motor no
  alteró `Side` de ninguna orden emitida, sizing activo no rompió ninguna corrida).
- **Incapacidad**: se registra sin bloquear la corrida (`Success` con `Incapacidades>0` en 2
  combinaciones de la matriz), lenguaje y mecanismo consistentes con D-096/D-097.
- **126/126 tests de producción** sin cambio tras toda la validación — `src/`/`tests/` intactos.
- **4 baselines congelados** (`caso1-v1-experimental` a `caso4-v1-experimental`, incluyendo
  `caso3b-v1-experimental`) — `git status --porcelain` vacío sobre todas las rutas relevantes en
  todo el ciclo de esta validación.

---

## 8. Recomendaciones futuras

- **Escenario 3 (lateral) con VolumenBreakout no se incluyó en la matriz dirigida** — se priorizó
  Neutral/Z-Score para esa combinación (§3). Si una fase futura quiere verificar específicamente
  que VolumenBreakout permanece inactivo en ausencia de contexto de volumen, sería una extensión
  puntual de esta misma matriz, no una fase nueva.
- **El generador de datos sintéticos corregido (§6) queda disponible como utilidad reutilizable**
  para cualquier validación futura que necesite ejercitar condiciones de volumen — su defecto
  original (crecimiento lineal vs. picos puntuales) es un aprendizaje de diseño experimental que
  vale la pena documentar como principio general: condiciones de umbral relativo a una media móvil
  requieren cambios abruptos, no tendencias suaves, para dispararse con certeza.
- **No se identificó ninguna deuda técnica nueva** en `src/` ni en ninguna estrategia — esta
  validación no aporta evidencia a favor ni en contra de abrir ninguna de las 3 direcciones
  descritas en `MAPA_EVOLUCION_V2.md` §3; simplemente confirma que las 5 fases congeladas siguen
  siendo una base estable sobre la cual decidir.

---

## Criterio de cierre

- ✓ Todas las capas ejecutaron correctamente (16/16 combinaciones `Success`, 0 excepciones no
  controladas).
- ✓ Resultados reproducibles (hashes + reporte idénticos entre 2 corridas).
- ✓ No aparecen inconsistencias no documentadas — la única contradicción detectada fue aislada,
  documentada y corregida dentro del alcance de esta misma validación (defecto del generador de
  datos de prueba, no del sistema auditado).
- ✓ El hallazgo se documentó antes de modificar código (§6, ciclo detectar→aislar→documentar→
  decidir respetado).
- ✓ Ningún baseline congelado, ninguna decisión D-001 a D-107, ningún archivo de `src/`/`tests/`
  modificado.
- ⏳ Pendiente de tu revisión y decisión sobre la siguiente fase.
