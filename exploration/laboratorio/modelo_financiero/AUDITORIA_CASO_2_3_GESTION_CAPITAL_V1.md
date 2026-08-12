# Auditoría de Cierre — Caso 2.3: Gestión de Capital

Estado: **documento de cierre — Caso 2.3 del Caso 2, completo**. Cierra formalmente Caso 2.3
(2.1 Modelo económico base, 2.2 Modelo de costes, 2.3 Gestión de capital) antes de abrir Caso 2.4
(métricas financieras). No agrega capacidades nuevas — responde si Caso 2.3 quedó suficientemente
definido, documentado, probado y separado de Caso 1 para congelarse como referencia.

---

## 1. Inventario documental — Caso 2.0 a 2.3

Verificado por lectura directa de `exploration/laboratorio/modelo_financiero/` (8 documentos):

| Documento | Contenido | Decisiones que registra |
|---|---|---|
| `ESPECIFICACION_MODELO_ECONOMICO_V1.md` | Caso 2.0 — inventario del motor económico ya congelado (Equity/Posiciones/RN-08-11), Principios (P-001/P-002/P-003) | D-057, D-058, D-059 |
| `ESPECIFICACION_MODELO_ECONOMICO_BASE_V1.md` | Caso 2.1 — diseño e implementación de `Instrumento`/`Incapacidades` | D-057 a D-062 (con D-061/D-062 originadas durante implementación) |
| `ESPECIFICACION_MODELO_COSTES_V1.md` | Caso 2.2 — diseño e implementación de `ConfiguracionCostes`/`CostoFriccionReal` real | D-063, D-064, D-065 |
| `EVALUACION_MODELOS_GESTION_RIESGO_V1.md` | Caso 2.3 — comparación de candidatos de sizing (sin elegir ganador) | Ninguna decisión, solo evidencia para D-067 |
| `ESPECIFICACION_ARQUITECTURA_GESTOR_CAPITAL_V1.md` | Caso 2.3 — resolución de D-066/D-068 (dónde vive el gestor) | D-066, D-068 |
| `ESPECIFICACION_GESTION_CAPITAL_V1.md` | Caso 2.3 — documento histórico de apertura de preguntas (marcado como tal, remite a los documentos que resolvieron cada una) | Ninguna (histórico) |
| `ESPECIFICACION_GESTOR_CAPITAL_PORCENTAJE_V1.md` | Caso 2.3 — diseño e implementación final de `GestorCapital`/`ConfiguracionSizing` | D-067 (fórmula corregida), D-071 |
| `DECISIONES_MODELO_ECONOMICO_V1.md` | Registro formal consolidado de las 15 decisiones (D-057 a D-071) | Todas |

**Verificación de completitud**: `grep -c "^## D-0" DECISIONES_MODELO_ECONOMICO_V1.md` → 15
encabezados de decisión, D-057 a D-071 sin saltos — coincide exactamente con el rango citado en
todas las auditorías de revisión de esta fase.

### 1.1 Decisiones cerradas (15)

| D-XXX | Selección final |
|---|---|
| D-057 | `TasaMargen` pertenece al instrumento |
| D-058 | Unidad monetaria abstracta (no USDT real) |
| D-059 | Incapacidad se registra, nunca bloquea |
| D-060 | Capacidad se evalúa antes de aplicar la orden |
| D-061 | Contratos existentes: parámetro opcional, default histórico |
| D-062 | `tasaMargen` propagado también a `ResolutorVela` (corrección durante implementación) |
| D-063 | Costes V1 = Comisión + Slippage (no spread, no funding) |
| D-064 | Coste pertenece al experimento, no al instrumento |
| D-065 | Coste modifica `Cash`/`Equity` (PnL neto), aplicado por tramo en Cross-Zero |
| D-066 | `GestorCapital` como capa externa, no dentro de `IStrategy` |
| D-067 | Porcentaje de `Cash − Margin` (corregido desde `Equity`, no implementable en el punto de integración) |
| D-068 | `GestorCapital` propone, `ValidadorCapacidad` valida |
| D-069 | Sizing nuevo = nueva versión experimental, nunca modifica Caso 1 |
| D-070 | Arquitectura formal: `Strategy → GestorCapital → ValidadorCapacidad → Motor` |
| D-071 | `GestorCapital` transforma órdenes existentes, nunca crea/elimina |

**Patrón recurrente observado en 3 de las 15 decisiones** (D-062, slippage en D-063, D-067):
un valor o fórmula diseñado en abstracto resultó no representar una diferencia real o no ser
implementable en el punto exacto del motor donde debía aplicarse, detectado siempre por una prueba
obligatoria (P2/P3 según el caso), nunca asumido como correcto de antemano. Registrado como
principio de trabajo válido para Caso 2.4: todo parámetro económico nuevo requiere verificar que
la ruta de cálculo lo consume realmente, no solo que la fórmula es matemáticamente coherente.

---

## 2. Auditoría de código — verificación contra `src/` real

**Archivos nuevos** (7): `Domain/Shared/Instrumento.cs`, `Domain/Shared/ConfiguracionCostes.cs`,
`Domain/Shared/ConfiguracionSizing.cs`, `Domain/Broker/RegistroIncapacidad.cs`,
`Domain/Portfolio/GestorCapital.cs`, más 4 archivos de test nuevos en `tests/Application.Tests/`.

**Archivos modificados** (6): `Application/ConfiguracionExperimento.cs`,
`Application/ResultadoBacktest.cs`, `Application/BacktestRunner.cs`,
`Domain/Matching/MatchingEngine.cs`, `Domain/Portfolio/AplicadorFill.cs`,
`Domain/VelaResolution/ResolutorVela.cs`.

**Verificado, no solo citado**: todo campo nuevo (`Instrumento`, `Costes`, `Sizing` en
`ConfiguracionExperimento`; `Incapacidades` en `ResultadoBacktest`) es opcional con valor por
defecto igual al comportamiento histórico — ningún call site existente en `src/`, `tests/` o
`exploration/` requirió modificación de firma.

**No modificado, confirmado por lectura directa**: `IStrategy` (interfaz sin cambios, 1 método),
las 3 estrategias del catálogo (`EstrategiaTresMosqueteros.cs`, `EstrategiaMhiMayoria.cs`,
`EstrategiaEmaCross.cs`), `CalculadoraLotes`, `ConsumidorFifo`, `ResolutorCrossZero`,
`CalculadoraRealizedPnL` (FIFO/Cross-Zero/RealizedPnL intactos), `ValidadorCapacidad`,
`CalculadoraReservaPreventiva` (usados, no editados).

---

## 3. Auditoría de reproducibilidad

**Baseline de Caso 1** (`caso1-v1-experimental`, Tres Mosqueteros 1m+1D): hash
`A48CCC57DA1919F533F4D532FDC0F945705681DCDA813B385BBFE7F44F40998E` — verificado idéntico en 3
puntos distintos de esta fase (cierre de Caso 2.1, cierre de Caso 2.2, cierre de Caso 2.3), cada
uno mediante ejecución real del pipeline (`EjecutorProtocolo`, no cálculo manual). Ningún cambio
de Caso 2.0-2.3 alteró la evidencia congelada de Caso 1.

**Suite de tests**: creció de 90 (al inicio de Caso 2.1) a 107 (al cierre de Caso 2.3) — 17 tests
nuevos, 0 tests existentes modificados, 0 tests fallando en la corrida final.

| Punto de cierre | Total tests | Nuevos de esta sub-fase |
|---|---|---|
| Caso 2.1 (modelo base) | 96 | 6 (`ModeloEconomicoBaseTests`) |
| Caso 2.2 (costes) | 101 | 5 (`ModeloCostesTests`) |
| Caso 2.3 (gestión de capital) | 107 | 6 (`GestorCapitalTests`) |

**Determinismo verificado por diseño**, no solo por prueba puntual: cada sub-fase incluyó una
prueba P-determinismo (misma entrada → mismo resultado en dos ejecuciones), y el hash compuesto de
`IdentidadExperimentoCompleta` cambia automáticamente si `Instrumento`/`Costes`/`Sizing` difieren
(D-069) — sin mecanismo paralelo de versionado necesario.

---

## 4. Auditoría de límites — Caso 2.3 vs. Caso 1 vs. Caso 2.4

Verificado contra código, no solo contra intención declarada:

**Responde** (Caso 2.3):
- ¿Cuánto capital consume una operación bajo una política de sizing definida? — `GestorCapital.Ajustar`.
- ¿La reserva de capacidad refleja la exposición real que se ejecutará? — orden verificado:
  `GestorCapital` antes de `ValidadorCapacidad`.
- ¿Cambiar sizing altera la identidad experimental? — sí, automáticamente (D-069).

**No responde** (pertenece a Caso 2.4, explícitamente no implementado):
- ¿Cuál es el drawdown monetario de una corrida? — ningún módulo lo calcula.
- ¿Cuál es el retorno absoluto/relativo comparable entre estrategias? — no existe cálculo.
- ¿Qué estrategia tiene mejor riesgo/retorno? — ningún reporte compara estrategias por resultado
  económico (D-014/D-047 siguen vigentes: sin ranking).

**No responde** (pertenece a una fase posterior a Caso 2.4, explícitamente fuera de Caso 2.3):
- ¿Cuánta probabilidad de acierto tiene una estrategia? — Masaniello no implementado, ningún
  modelo estadístico existe en `src/` (confirmado por búsqueda exhaustiva, `ESPECIFICACION_MODELO_
  ECONOMICO_V1.md` §1).

---

## Fuera de alcance de este documento

No se implementó código. No se abrió Caso 2.4 ni Masaniello. No se modificó ningún archivo de
`src/`/`tests/` fuera de lo ya cerrado en las 3 sub-fases anteriores.

---

## Criterio de cierre de este documento

- ✓ 15 decisiones (D-057 a D-071) verificadas contra `DECISIONES_MODELO_ECONOMICO_V1.md`, sin
  huecos de numeración.
- ✓ Patrón recurrente de 3 correcciones detectadas por pruebas (no asumidas) registrado como
  principio de trabajo para Caso 2.4.
- ✓ Código verificado directamente: qué se agregó, qué se modificó, qué se dejó intacto.
- ✓ Reproducibilidad verificada por ejecución real repetida (3 puntos), no por cita — hash de
  Caso 1 sin alteración en las 3 corridas.
- ✓ Crecimiento de suite de tests documentado (90 → 107), 0 regresiones.
- ✓ Límites de Caso 2.3 vs. Caso 1 vs. Caso 2.4 verificados por ausencia/presencia de cálculo en
  el código.

**Caso 2.3 — Gestión de capital: cerrado.**
