# Deuda Técnica — Caso 2: Modelo Financiero

Estado: **documento maestro — cierre de Caso 2, Paso 2** (autorizado tras Auditoría de cierre —
Caso 2.4). Regla de este documento: **documentar, no resolver**. Ninguna de las limitaciones
listadas aquí se corrige en este paso — quedan registradas como conocidas y conscientes,
disponibles para que una fase futura decida si/cuándo resolverlas. Mismo criterio que
`DEUDA_TECNICA_CASO1_V1.md`.

---

## 1. Pendientes excluidos de Caso 2

### 1.1 No implementados por decisión

- **D-074 — Duración del drawdown**: definida conceptualmente (inicio/fin/duración en velas), no
  implementada en V1. Sin DTO, cálculo ni prueba en ningún archivo de `modelo_financiero/`. Motivo
  de exclusión: mismo criterio que D-063 excluyó spread/funding — cada métrica requiere su propio
  alcance explícito, no se infiere de una definición conceptual (ver aclaración registrada en
  `DECISIONES_MODELO_ECONOMICO_V1.md` §D-074).
- **Masaniello**: evaluado en `EVALUACION_MODELOS_GESTION_RIESGO_V1.md`, no implementado. Requiere
  modelo probabilístico definido, horizonte experimental, métrica de objetivo y validación
  independiente — ninguno de los cuatro existe hoy (D-067).
- **Modelos alternativos de sizing**: sizing basado en `Equity` (`Cantidad = Equity × Riesgo%`) fue
  explícitamente descartado de esta fase durante la corrección de D-067 — requiere acceso a valor
  razonable de posiciones abiertas, definición temporal del precio usado y reglas de PnL no
  realizado, ninguna resuelta por D-067. Cualquier modelo de sizing adaptativo (por racha, winrate
  histórico, régimen de mercado) está explícitamente prohibido para `GestorCapital` en su forma
  actual (restricciones aprobadas en D-067).
- **Métricas financieras adicionales**: Sharpe, Sortino, Profit Factor, Calmar u otras métricas de
  riesgo/rendimiento no evaluadas ni diseñadas — `ESPECIFICACION_METRICAS_FINANCIERAS_V1.md`
  acotó el alcance a la taxonomía contable/riesgo básica (D-072/D-073/D-075), sin extenderse a
  métricas de rendimiento ajustado por riesgo.

### 1.2 No resueltos técnicamente

- **Spread y funding** (D-063): el motor no tiene modelo bid/ask (`MatchingEngine` opera sobre OHLC
  por vela, no libro de órdenes) — no es una exclusión de alcance sino una limitación estructural
  del motor actual. Modelarlos requeriría rediseñar la ejecución, fuera de lo que Caso 2 autorizó
  tocar (motor congelado, P-001).
- **Sizing por Equity**: además de ser exclusión de decisión (1.1), tiene una limitación técnica de
  fondo — `PortfolioState`, en el punto de integración de `GestorCapital` (antes de
  `ResolutorVela.Resolver`), no expone `Equity` porque ese cálculo depende del `Close` de la vela
  siguiente, aún no conocida en ese momento del ciclo (D-067). No es solo una decisión pendiente:
  es una restricción real de en qué punto del ciclo del motor esa información existe.

### 1.3 Hallazgos de arquitectura detectados al generar el baseline financiero

- **D-084 — Semántica de órdenes para sizing** (Categoría: Gestión de capital / Arquitectura):
  `GestorCapital` recibe `OrderRequest` sin una semántica explícita de intención
  (apertura/cierre/reducción/reversión) — aplica sizing uniforme sobre todas las órdenes recibidas
  (`GestorCapital.Ajustar`, `src/Domain/Portfolio/GestorCapital.cs:19-21`). Detectado al generar el
  baseline financiero con `Sizing` activo en timeframe 1m (~82,475 operaciones): la orden de cierre
  recibe una `Cantidad` recalculada distinta de la que realmente abrió el lote, y `AplicadorFill`/
  `ConsumidorFifo` interpretan la discrepancia como cierre parcial — el lote nunca se remueve por
  completo, dejando un residuo que crece sin límite a lo largo de la corrida. No es un fallo del
  `GestorCapital` implementado ni de D-067 — es que el contrato `OrderRequest` no distingue
  intención, y `GestorCapital` no tiene forma de saber si debe o no recalcular una cantidad de
  cierre. Resolución explícitamente fuera de Caso 2 V1 — no se aprobó: modificar `GestorCapital`
  parcialmente, introducir heurísticas para detectar cierres, comparar por lado/precio para inferir
  intención, ni cambiar `OrderRequest` sin una nueva decisión (cualquiera sería una solución
  implícita a un problema de arquitectura). Consecuencia: el baseline financiero congelado
  (`baseline_financiero_final/`) usa `Sizing=null` — la infraestructura de `GestorCapital` queda
  implementada y probada (P1-P6), pero no activa en la configuración de referencia.
- **D-085 — Escala económica histórica de estrategias** (Categoría: Modelo económico /
  Compatibilidad histórica): las estrategias congeladas de Caso 1 (`EstrategiaTresMosqueteros`,
  etc.) usan `Cantidad=1` fija, sin relación dimensional explícita con `CapitalInicial`. Caso 1
  nunca expuso esto porque nunca calculó métricas monetarias absolutas — solo
  `EquityInicial`/`EquityFinal`/`RetornoPct` derivados, ya marcados "no comparables
  financieramente" (`DEUDA_TECNICA_CASO1_V1.md` §1-2). Caso 2.4 es la primera vez que el proyecto
  expone `CashFinal`/`EquityFinal` como cifras absolutas, y esa exposición reveló el desajuste
  preexistente (`Margin` de una sola orden ≈ 9× `CapitalInicial=1000` con los parámetros históricos
  BTCUSDT). No se recalibra `CapitalInicial` del baseline para "normalizar" el resultado — eso
  cambiaría la interpretación económica de un parámetro que pertenecía al contexto experimental de
  Caso 1, no solo a la configuración del baseline. El reporte financiero (`REPORTE_FINANCIERO_
  V1.md` §5) incluye una advertencia obligatoria al respecto.

---

## 2. Mejoras futuras

Notas de dirección posible, sin diseño ni compromiso de fase — mismo criterio que sección 5 de
`DEUDA_TECNICA_CASO1_V1.md`:

- **Automatización de reportes financieros**: hoy `MetricasFinancieras` se calcula por corrida
  dentro de `EjecutorProtocolo`, sin generador de reporte dedicado que las presente junto a
  `PerfilMultiTf`/comparativas multi-timeframe.
- **Nuevas vistas**: comparación visual de `EquityCurve` entre estrategias, gráfico de drawdown en
  el tiempo — ninguna existe, todo el acceso a `MetricasFinancieras` es programático.
- **Análisis histórico**: sin almacenamiento ni comparación de `MetricasFinancieras` entre
  ejecuciones pasadas (mismo vacío que Caso 1 sección 5 — "Almacenamiento histórico" — extendido
  aquí al dominio financiero).
- **Nuevas políticas de gestión de capital**: `GestorCapital` soporta hoy un único modelo
  (porcentaje de capital disponible, D-067). La arquitectura (D-070) fue diseñada para admitir
  modelos intercambiables, pero ningún segundo modelo fue implementado ni evaluado más allá de
  Masaniello (descartado por ahora, ver 1.1).

---

## 3. Riesgos conocidos

Registrados explícitamente para que ningún reporte o discusión futura los pierda de vista:

- **Dinero simulado ≠ dinero real**: toda cifra de `MetricasFinancieras` es una unidad monetaria
  experimental (D-058), no una proyección de retorno real. Ningún reporte debe usar lenguaje que
  sugiera lo contrario (Sección 5 de `ESPECIFICACION_METRICAS_FINANCIERAS_V1.md`).
- **Porcentaje de capital no implica optimización**: `PorcentajeRiesgo` es un parámetro
  experimental fijo elegido por quien diseña el experimento — no fue calibrado, ajustado ni
  optimizado contra resultados históricos (D-067, restricciones aprobadas). Un valor "que funcionó
  mejor" en una corrida no es una recomendación de valor óptimo.
- **Métricas financieras no generan ranking**: D-076 prohíbe explícitamente ordenar o declarar una
  estrategia superior a otra en base a `MetricasFinancieras` — mismo principio que D-014/D-047 en
  Caso 1, extendido al dominio financiero.
- **Resultados dependen de parámetros experimentales**: `Instrumento.TasaMargen`,
  `ConfiguracionCostes`, `ConfiguracionSizing` son todos parámetros libres del experimento (D-057,
  D-064, D-067) — cambiar cualquiera de ellos cambia el resultado financiero sin que eso implique
  que la estrategia mejoró o empeoró en algún sentido absoluto. Ningún resultado financiero es
  comparable entre corridas con configuración económica distinta sin declarar esa configuración.
- **`PorcentajeRiesgo` del baseline es calibración dimensional, no recomendación** (D-083): el
  valor `0.000002` usado en el intento de baseline con sizing activo corrige que `Cantidad` opera
  en unidades del activo, no en fracción monetaria directa (`Margin ≈ CapitalInicial ×
  PorcentajeRiesgo × Precio × TasaMargen`) — es un "valor de configuración de referencia compatible
  con la escala del modelo", nunca un valor óptimo o recomendado. La elección se validó solo contra
  exposición inicial, compatibilidad dimensional y reproducibilidad — nunca contra PnL, drawdown o
  rentabilidad.

---

## 4. Decisiones futuras asociadas

- **D-074**: implementación de duración del drawdown, pendiente de una decisión de alcance propia
  antes de cualquier código.
- **Futuras decisiones de sizing avanzado**: cualquier modelo más allá de porcentaje de capital
  (Masaniello, sizing por `Equity`, sizing adaptativo) requiere su propia decisión numerada — no se
  incorpora por extensión silenciosa de D-067 (mismo principio que D-069 aplicó a estrategias con
  sizing nuevo).
- **Futuras métricas**: cualquier métrica de rendimiento/riesgo ajustado (Sharpe, Sortino, Profit
  Factor, etc.) requiere su propia especificación y decisión, siguiendo el mismo patrón D-072-D-078
  (fuente oficial de datos, tratamiento de no disponibles, sin ranking).
- **D-084**: definir una semántica explícita de intención (apertura/cierre/reducción/reversión) en
  el contrato de órdenes, o un mecanismo equivalente, antes de activar sizing dinámico en corridas
  con reaperturas/martingala. Sin esta decisión, `GestorCapital` no debe activarse en ninguna
  configuración congelada de referencia.
- **D-085**: definir una relación dimensional explícita entre `CapitalInicial`, unidad del activo y
  tamaño de posición — posiblemente junto con la resolución de sizing avanzado, dado que ambos
  tocan cómo se determina `Cantidad`. No implica modificar `EstrategiaTresMosqueteros` ni ninguna
  estrategia congelada de Caso 1 sin una decisión explícita separada.

---

## 5. Límite de Caso 2

**Caso 2 entrega**: modelo económico experimental (D-057-D-062) + gestión de capital básica
(D-066-D-071) + medición financiera (D-072-D-078).

**Caso 2 no entrega**: optimización financiera, ni sistema de inversión automático. Ningún
componente de Caso 2 selecciona, recomienda, ajusta ni optimiza parámetros en base a resultados —
toda configuración económica (`Instrumento`, `ConfiguracionCostes`, `ConfiguracionSizing`) es un
input explícito de quien diseña el experimento, nunca una salida calculada por el sistema.

---

## Fuera de alcance de este documento

Este documento no resuelve ninguna de las limitaciones listadas. No se modifica código, no se
abren documentos de especificación nuevos para estos puntos, no se abre ninguna implementación de
D-074, Masaniello, sizing avanzado ni métricas adicionales — conforme a la restricción explícita
de este cierre.

---

## Próximo documento de esta fase (no incluido aquí)

- `VERSION_EXPERIMENTAL_CASO2_V1.md` — definición formal de la versión congelada del modelo
  financiero (mismo criterio que Caso 1).

---

## Criterio de cierre de este documento

- ✓ Pendientes excluidos de Caso 2 separados en "no implementados por decisión" vs. "no resueltos
  técnicamente" (sección 1).
- ✓ Hallazgos de arquitectura detectados al generar el baseline financiero (D-084, D-085)
  registrados con evidencia y causa raíz, no ocultados mediante ajuste silencioso (sección 1.3).
- ✓ Mejoras futuras registradas sin diseño ni compromiso (sección 2).
- ✓ Riesgos conocidos registrados explícitamente, incluyendo dinero simulado ≠ real, sizing sin
  optimización (D-083 como calibración dimensional, no recomendación), sin ranking, dependencia de
  configuración experimental (sección 3).
- ✓ Decisiones futuras asociadas referenciadas por número, incluyendo D-084/D-085 (sección 4).
- ✓ Frontera de Caso 2 declarada explícitamente: qué entrega y qué no entrega (sección 5).
- ✓ Ningún cambio de código — verificado (`git status --porcelain -- src/ tests/` sin cambios
  respecto al cierre de Caso 2.4).
- ✓ Auditoría del baseline financiero aprobada — D-079 a D-085 revisadas y confirmadas.
- ⏳ Pendiente actualizar `VERSION_EXPERIMENTAL_CASO2_V1.md` antes del cierre formal.
