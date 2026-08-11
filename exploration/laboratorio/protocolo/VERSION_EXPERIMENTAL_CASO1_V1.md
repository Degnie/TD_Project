# Versión Experimental — Caso 1: Laboratorio de Estrategias

Estado: **documento de congelamiento oficial — Fase 1.7, Paso 3 del Caso 1** (autorizado tras
aprobación de `AUDITORIA_FINAL_CASO1_V1.md` y `DEUDA_TECNICA_CASO1_V1.md`). A partir de este
documento, el Caso 1 queda congelado como **V1 Experimental**.

---

## Identificación

- **Nombre**: Caso 1 — Laboratorio de estrategias
- **Versión**: V1 Experimental
- **Estado**: Congelado
- **Fecha de congelamiento**: 2026-08-11
- **Base de aprobación**: `AUDITORIA_FINAL_CASO1_V1.md` (D-056) + `DEUDA_TECNICA_CASO1_V1.md`, ambos aprobados por auditoría en Fase 1.7.

---

## Componentes incluidos

**Dataset oficial**: `BTCUSDT_2024-01-02_2025-01-02` (real, congelado desde Fase 1.0/2C), hash de
origen `f1a9dcbe72bd...`, mismo dataset usado por las 3 estrategias validadas.

**Estrategias validadas**:
- **Tres Mosqueteros** (`EstrategiaTresMosqueteros.cs`) — Patrón, con martingala (D-003).
- **MHI Mayoría** (`EstrategiaMhiMayoria.cs`) — Patrón, con martingala (D-003).
- **EMA Cross** (`EstrategiaEmaCross.cs`) — Tendencia, sin martingala, validación de generalidad del pipeline (D-054).

**Clasificador de régimen**: `ClasificadorRegimenV1.cs` — congelado (D-034), 4 estados
(Alcista/Bajista/Lateral/Ambiguo, D-028), `UmbralSesgoDI=0.153467` (D-033), `Version="V1"` (D-052).

**Pipeline experimental**: `EjecutorProtocolo.cs` (`VersionProtocolo="V1"`) — orquesta
Backtest → PerfilMultiTf → AnalizadorOperacional/ComparadorMultiTimeframe/ClasificadorRegimenV1 →
AsignadorOperacionRegimen → MetricasPorEscenario → ReporteEscenariosGenerador, con identidad
experimental compuesta (`IdentidadExperimentoCompleta`, D-049) y estados explícitos por corrida
(Success/Failed/Incomplete, nunca ocultando fallos parciales).

**Reportes**: `ReporteConsolidadoGenerador.cs` (resumen + anexos, D-048/D-051) y
`ReporteEscenariosGenerador.cs` (vista por régimen de entrada y de resolución, D-038/D-047), ambos
sin sección de conclusión comparativa ni ranking entre estrategias/timeframes/regímenes.

**Métricas operacionales**: catálogo de Fase 1.2 (`GanoInicial`/`GanoM1`/`GanoM2`/
`PctResueltasPorMartingala`/`PerdioAgotando`, Eficiencia operacional por timeframe, Racha negativa
máxima) más el catálogo por régimen de mercado (Fase 1.5, `MetricasPorEscenario.cs`). Sujeto a la
limitación conocida y ya registrada en `DEUDA_TECNICA_CASO1_V1.md` §3 (D-055: parte del catálogo
no aplica a estrategias sin martingala).

---

## Garantías

- **Reproducibilidad**: dada la misma `EntradaProtocolo` (estrategia, versión, parámetros,
  timeframes, dataset), dos ejecuciones producen el mismo `IdentidadExperimentoCompleta.HashCompuesto`
  y el mismo resultado operacional — verificado en Fase 1.7 §2 con 3 ejecuciones independientes
  (Tres Mosqueteros) y con la prueba permanente `VerificarIntegracionEmaCross` (EMA Cross).
- **Determinismo**: cada corrida de `EjecutorProtocolo` verifica determinismo internamente (2
  ejecuciones comparadas campo por campo) antes de reportar `Success`.
- **Trazabilidad**: toda decisión de diseño relevante está numerada (D-001 a D-056) y registrada en
  un documento verificable — las únicas 2 excepciones detectadas (D-043, D-053) fueron corregidas
  en `AUDITORIA_FINAL_CASO1_V1.md` §1.5.
- **Separación Caso 1 / Caso 2**: ningún módulo congelado en esta versión calcula rentabilidad real,
  retorno esperado, capital a invertir ni recomendación de estrategia — verificado por ausencia de
  cálculo en el código, no solo por intención declarada (`AUDITORIA_FINAL_CASO1_V1.md` §3).

---

## Exclusiones (explícitas)

- **Sin modelo financiero real**: sin ROI comparable, sin Sharpe, sin costes de transacción, sin
  slippage (D-002, D-009).
- **Sin rentabilidad real**: `EquityInicial`/`EquityFinal`/`RetornoPct` son datos derivados del
  modelo de posición actual, explícitamente no comparables financieramente
  (`DEUDA_TECNICA_CASO1_V1.md` §2).
- **Sin optimización**: ningún parámetro de ninguna estrategia fue calibrado sobre el dataset con
  el objetivo de maximizar resultado — parámetros son convención externa (D-030 aplicado también a
  EMA 12/26).
- **Sin sizing ni gestión de capital**: sin Masaniello, sin tamaño de operación variable, sin
  gestión de riesgo monetario (D-002).
- **Sin recomendación de inversión**: ningún reporte generado por esta versión contiene lenguaje de
  ranking o recomendación entre estrategias, timeframes o regímenes (D-014/D-047, verificado por
  prueba `VerificarSinFrasesProhibidas`).

Todo lo anterior pertenece a Caso 2 (modelo financiero) — fuera de esta versión.

---

## Regla de evolución

Cualquier modificación que cambie comportamiento experimental — nueva estrategia, cambio de
clasificador oficial, cambio de catálogo de métricas, cambio de protocolo o pipeline — requiere una
**nueva versión experimental** (V2), nunca una edición in-place de V1 (mismo principio que D-017 y
D-046 ya aplican a artefactos individuales, extendido aquí a la versión completa del Caso 1).

```
V1 Experimental (congelada)
        ↓
  cambio metodológico
        ↓
V2 Experimental
```

Cambios que **no** alteran comportamiento experimental (ej. metadata de identificación pura, ya
precedentado en D-052) no requieren nueva versión, siempre que se demuestre equivalencia mediante
prueba de igualdad de resultados antes/después — mismo criterio que D-052 estableció para el
clasificador, generalizado aquí como regla de la versión completa.

---

## Fuera de alcance de este documento

No se implementó código. No se modifica ningún módulo. No se abre ninguna discusión de Masaniello,
sizing, riesgo financiero, capital, costes ni simulación monetaria — conforme a la restricción
explícita de Fase 1.7.

---

## Criterio de cierre de este documento (Paso 3 de Fase 1.7)

- ✓ Identificación formal (nombre, versión, estado, fecha) registrada.
- ✓ Componentes incluidos listados con archivo y decisión de origen.
- ✓ Garantías (reproducibilidad, determinismo, trazabilidad, separación Caso 1/Caso 2) declaradas
  y respaldadas por evidencia ya verificada en `AUDITORIA_FINAL_CASO1_V1.md`.
- ✓ Exclusiones declaradas explícitamente, remitiendo a Caso 2.
- ✓ Regla de evolución (nueva versión ante cambio de comportamiento) establecida.
- ✓ Ningún cambio de código — verificado (`git status --porcelain -- src/ tests/` vacío).
- ⏳ Auditoría revisa este documento — pendiente de confirmación para declarar Fase 1.7 cerrada y
  Caso 1 formalmente congelado como V1 Experimental.
