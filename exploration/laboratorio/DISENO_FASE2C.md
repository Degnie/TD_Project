# Documento de diseño — Fase 2C: Evaluación de estrategias sobre datos reales multi-timeframe

Estado: **propuesta, sin implementar**. Deriva de `DISENO_FASE2B.md` (agregación, cerrado) y
reutiliza directamente la infraestructura de Fase 1.5 (`PerfilEstrategia.cs`, `Fase15.cs`,
`InfoOperacionResuelta`). No modifica `src/`, `SPEC.md`, ni la lógica de
`EstrategiaTresMosqueteros`/`EstrategiaMhiMayoria`. Todo vive en `exploration/laboratorio/`.

## Objetivo

Responder **"¿cómo cambia el comportamiento de una misma estrategia cuando cambia la escala
temporal?"**, no "¿qué timeframe gana más?". Es una evaluación de comportamiento, igual que
Fase 1.5 lo fue para escenarios sintéticos — ahora sobre datos reales (BTCUSDT, Fase 2A/2B) y con
una dimensión nueva (timeframe) en vez de forma de mercado.

Explícitamente **no** es una búsqueda de rentabilidad ni una optimización de parámetros.

## Alcance

- Estrategias: `EstrategiaTresMosqueteros` y `EstrategiaMhiMayoria`, sin modificar su lógica.
- Timeframes iniciales: **1m, 5m, 15m, 1h, 4h, 1D** (subconjunto de los 12 ya generados en
  Fase 2B — microestructura, cercanos al origen de las estrategias, y cambio de régimen). Los 6
  restantes (2m, 10m, 30m, 2h, 8h, 12h, 1W) quedan disponibles para una segunda tanda, no en el
  alcance inicial.
- Dataset: `datasets/reales/BTCUSDT/{timeframe}/BTCUSDT_2024-01-02_2025-01-02_{timeframe}.csv`,
  ya congelados con hash en Fase 2B.

## Punto 1 — Tratamiento de velas parciales

**Decisión: excluir del backtest (no del dataset).**

La capa de datos (Fase 2B) conserva las velas parciales con su metadata de completitud — esa
decisión no cambia. Pero una vela parcial no representa la misma unidad temporal que una
completa (ej. una semana de 8.640 minutos no es comparable a una de 10.080): incluirla podría
generar una señal o un Fill que no reproduce fielmente el timeframe, introduciendo sesgo no
atribuible a la estrategia ni al timeframe real.

Filtro aplicado antes de construir el `ConfiguracionExperimento`: `velas.Where(v => !v.EsParcial)`.
El reporte de cada corrida debe declarar explícitamente:

```
Timeframe: 1W
Velas disponibles:  53
Velas utilizadas:   51
Velas excluidas:     2
```

Esto se repite por cada combinación estrategia × timeframe — no es un dato global del dataset,
sino parte del reporte de esa corrida específica (aunque el conteo de excluidas sea el mismo
para ambas estrategias sobre el mismo timeframe, ya que depende del dataset, no de la estrategia).

## Punto 2 — Agrupación de operaciones lógicas

**Decisión: reutilizar `InfoOperacionResuelta` vía `onOperacionResuelta`, sin heurísticas
externas.** Mismo mecanismo que Fase 1.5, sin cambios: la estrategia es la única fuente de
verdad sobre qué intentos (inicial + martingalas) pertenecen a la misma operación lógica. No se
reconstruye ese vínculo desde `Trade`/`Side`/timestamps por fuera.

Esto garantiza que las métricas de racha negativa y uso de martingala se calculen con el mismo
criterio en datos sintéticos (Fase 1.5) y datos reales (Fase 2C) — comparables entre sí si en el
futuro se quiere contrastar ambos.

## Punto 3 — Métricas obligatorias

Reutiliza `PerfilEstrategia` de Fase 1.5 sin cambios de forma — ya cubre exactamente esta lista:

**Rendimiento**: `EquityInicial`, `EquityFinal`, `RetornoPct`, `TotalOperaciones`.

**Riesgo**: `RachaNegativaMaxima` (ya calculada por streak de operaciones perdidas). La
distribución por longitud de racha (2/3/4/5+) que pide el mensaje no está en `PerfilEstrategia`
hoy — se agrega como extensión de reporte (no de la clase, ver Punto 5) reutilizando el mismo
patrón ya escrito en `AnalisisRiesgo.Reportar` de la Fase 0/exploración inicial.

**Martingala**: `GanoInicial`, `GanoM1`, `GanoM2`, `PerdioAgotandoMartingalas`,
`PctOperacionesResueltasPorMartingala` — ya expuestos.

**Calidad operacional**: `MaxExposicion`, `ReconciliacionCoherente`/`ErroresReconciliacion` (ya
expuestos); determinismo se verifica una vez por timeframe (no por combinación estrategia×
timeframe — es una propiedad del motor+dataset, no de la estrategia) con el mismo patrón de
doble-corrida ya usado en `Fase15.VerificarDeterminismo`. "Trades abiertos al cierre" (posición
viva al terminar el dataset) es un dato nuevo: se agrega leyendo
`resultado.PortfolioSnapshots[^1].LotesVivos.Count` — no requiere cambios a `PerfilEstrategia`,
se reporta aparte porque es informativo, no parte del perfil de riesgo/rendimiento.

## Punto 4 — Comparación entre timeframes

**Explícitamente no se declara un "ganador".** El reporte es una matriz descriptiva:

```
Estrategia          Timeframe   Retorno%   Operaciones   RachaMax   %Martingala   Parcial excl.
Tres Mosqueteros     1m           ...          ...          ...         ...            0
Tres Mosqueteros     5m           ...          ...          ...         ...            0
Tres Mosqueteros     15m          ...          ...          ...         ...            0
Tres Mosqueteros     1h           ...          ...          ...         ...            0
Tres Mosqueteros     4h           ...          ...          ...         ...            0
Tres Mosqueteros     1D           ...          ...          ...         ...            0
MHI Mayoría          1m           ...          ...          ...         ...            0
...
```

Igual que en Fase 1.5, cada fila con retorno negativo o racha larga debe llevar una explicación
cualitativa (por qué, no solo cuánto) — mismo patrón de `RazonPerdida` ya escrito, adaptado a
"por qué esta estrategia se comporta así en este timeframe" en vez de "en este tipo de mercado".
Ejemplo de la forma esperada (no el contenido real, que depende de la corrida):
*"Tres Mosqueteros en 1D genera pocas operaciones porque el cuadrante de 5 velas diarias abarca
casi un mes calendario — la señal de color de una sola vela diaria es más sensible a eventos
puntuales que a estructura de tendencia."*

## Punto 5 — Formato de reporte

Mismo patrón que `ReportarFase15` en `Program.cs` de `laboratorio/`, extendido con:
1. Bloque de completitud del dataset por timeframe (Punto 1) antes de la matriz.
2. Matriz comparativa (Punto 4).
3. Distribución de rachas por longitud (2/3/4/5+) por combinación estrategia×timeframe.
4. Bloque de integridad del motor: reconciliación financiera, determinismo, trades abiertos al
   cierre — igual clasificación `[BUG]`/`[OBSERVACIÓN DE ESTRATEGIA]` ya usada en Fase 1.5, sin
   mezclar ambas categorías.

## Fuera de alcance de este documento

- Los 6 timeframes restantes (2m, 10m, 30m, 2h, 8h, 12h, 1W) — segunda tanda, después de revisar
  esta primera matriz.
- Cualquier ajuste de parámetros de las estrategias (`maxMartingalas`, tamaño de posición, etc.).
- Comparación contra datos sintéticos de Fase 1.5 (posible análisis futuro, no de esta fase).
- Cualquier cambio a `src/`, `SPEC.md`, o los contratos del motor.

## Orden de implementación propuesto (una vez aprobado este plan)

1. Filtro de velas parciales + carga de los 6 datasets reales ya congelados (sin agregar nada
   nuevo — Fase 2B ya generó estos archivos).
2. Extender el reporte con distribución de rachas y trades abiertos al cierre (reutilizando
   `PerfilEstrategia`/`InfoOperacionResuelta`, sin tocar su forma).
3. Correr las 2 estrategias × 6 timeframes = 12 combinaciones, con el reporte de completitud
   declarado por corrida.
4. Cierre de Fase 2C con reporte de resultados (mismo patrón que Fases 1/1.5/2A/2B) antes de
   considerar la segunda tanda de timeframes.
