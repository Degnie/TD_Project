# Propuesta — Caso 5A: Evaluación Comparativa de Gestores de Riesgo

Estado: **documento de apertura — previo a cualquier decisión o implementación**. Define la
pregunta que responde Caso 5A, sus límites, y las decisiones que deben resolverse antes de tocar
código, siguiendo el mismo ciclo que toda fase anterior: propuesta → decisión → implementación →
pruebas → auditoría → congelamiento.

**Alcance confirmado explícitamente por el auditor**: framework de gestores de riesgo
intercambiables (Fixed Fractional actual, Fixed Risk, Volatility Sizing, Kelly fraccionado,
Masaniello), evaluados de forma comparativa sobre las mismas estrategias/datasets — **no** la
gestión de exposición/drawdown/límites planteada inicialmente, que queda diferida (ver §7).

**Punto de partida**: `MAPA_EVOLUCION_V2.md` §0 — la validación integral confirmó que las 5 fases
congeladas no presentan bloqueos estructurales; esta fase amplía capacidad, no estabiliza núcleo.
Verificación contra código existente, no reconstruida de memoria (mismo criterio que abrió Caso
2/Caso 3/Caso 4, D-057).

---

## 1. Objetivo de Caso 5A

**Pregunta principal**: ¿puede el laboratorio evaluar la misma estrategia bajo distintas políticas
de gestión de capital, de forma comparable, sin modificar la lógica de decisión de la estrategia?

**No busca**:
- Recomendar automáticamente cuál gestor usar (eso sería Caso 5B/6, explícitamente fuera de
  alcance — ver §7).
- Optimizar los parámetros de ningún gestor (ningún `PorcentajeRiesgo`, `RiesgoPorOperacion`,
  fracción de Kelly, etc. se ajusta buscando mejor resultado — mismo principio D-030 ya aplicado
  en cada fase).
- Producir una recomendación financiera ni ranking de superioridad entre gestores (extiende
  D-014/D-047/D-076: comparar no implica declarar un ganador).

Caso 5A evalúa la **plataforma frente a un nuevo eje de variación** (política de capital, no
lógica de señal) — mismo principio que D-054 aplicó a EMA Cross en Caso 1 y que Caso 3A/3B
aplicaron a nuevas familias de estrategia, extendido aquí al lado económico del pipeline.

---

## 2. Punto de partida verificado en código

**Único punto de integración real**: `src/Application/BacktestRunner.cs:57`
```csharp
requests = GestorCapital.Ajustar(requests, portfolio, config.Sizing, config.Velas[n + 1].Close, instrumento.TasaMargen);
```
Es la única invocación de `GestorCapital.Ajustar` en todo `src/`. Cualquier framework de gestores
intercambiables se engancha aquí — no en la estrategia, no en `ValidadorBolsaRequests`, no en
`ResolutorVela`.

**Hallazgo central que determina el diseño** — `GestorCapital.Ajustar`
(`src/Domain/Portfolio/GestorCapital.cs:30-72`) mezcla hoy **dos responsabilidades distintas**:

1. **Cálculo de cantidad para Apertura/Aumento** (líneas 37-39): `capitalDisponible = Cash -
   Margin`; `margenObjetivo = capitalDisponible × PorcentajeRiesgo`; `cantidadCalculada =
   margenObjetivo / (precioReferencia × tasaMargen)`. **Esta es la parte específica de Fixed
   Fractional** — la que debe variar entre gestores.
2. **Clasificación de intención de orden + normalización de Cross-Zero bajo sizing activo**
   (líneas 41-71, D-092/D-095): clasifica cada `OrderRequest` de la bolsa contra una posición
   proyectada, aplica sizing solo a Apertura/Aumento, conserva magnitud real en
   Reducción/Cierre, normaliza Cross-Zero espurio. **Esta parte es compartida por cualquier
   gestor** — no depende de qué fórmula calcula `cantidadCalculada`, solo de que sizing esté
   activo o no.

**Consecuencia de diseño**: un framework de gestores intercambiables no debe duplicar la parte 2
por cada gestor nuevo — debe aislar la parte 1 (el cálculo de cantidad) como lo único que varía,
manteniendo la parte 2 (clasificación/normalización) como código único y compartido. Esta
distinción es la pregunta central de la primera decisión de esta fase (ver §5).

**`ConfiguracionSizing` hoy** (`src/Domain/Shared/ConfiguracionSizing.cs`): un solo campo
(`PorcentajeRiesgo: decimal`), acoplado 1:1 al mecanismo de Fixed Fractional — sin ningún campo de
"tipo de gestor". Necesita extenderse o reemplazarse; no hay forma de introducir un segundo gestor
sin tocar este contrato.

**Estrategias y `DataSlice`/`IStrategy` confirmados ciegos a sizing**: `IStrategy.Observar`
recibe únicamente `DataSlice` (`N`, `VelaActual`, `VelasHastaN` — sin ninguna referencia a
`PortfolioState`/`Cash`/`Sizing`). Ninguna de las 6 estrategias recibe portfolio, cash, ni
configuración de sizing en su constructor. Confirma que introducir gestores de riesgo distintos
no requiere, ni debe requerir, tocar `IStrategy` ni ninguna estrategia existente.

**Sin precedente de contrato intercambiable en config** (fuera de `IStrategy` mismo): esta seria
la primera vez que el proyecto introduce una interfaz con múltiples implementaciones seleccionables
en tiempo de configuración. El precedente metodológico más cercano,
`ClasificadorRegimenV1`, evaluó candidatos por documento pero terminó congelando una única clase
estática — no dejó un contrato polimórfico vivo. Caso 5A es, en ese sentido, un diseño nuevo, no
una extensión de un patrón ya usado.

---

## 3. Insumos de evaluación — qué existe y qué falta

Comparar gestores requiere poder medir "qué tan bien se comportó" cada uno sobre la misma
estrategia/dataset. Verificado contra código:

**Ya existe** (`MetricasFinancieras`, `exploration/laboratorio/modelo_financiero/
MetricasFinancieras.cs`): `CapitalInicial`, `CashFinal`, `EquityFinal`, `PnLTotal`,
`DrawdownMaximoPct`, `ExposicionMaxima`.

**Ya existe** (`AnalizadorOperacional`/`ReporteOperacional`): cantidad de operaciones
(`IntentosCompletados`, `Victorias`, `Derrotas`, `EficienciaOperacionalPct`), `MayorRachaNegativa`.

**No existe en ningún lado**: ratio ganancia/pérdida (profit factor), racha de **victorias**
consecutivas (solo existe la peor racha negativa), serie completa de rachas (no solo la peor).
Un comparador de gestores probablemente necesita estos campos — su ausencia no bloquea la
apertura de la fase, pero determina que una de las primeras decisiones deba fijar qué métricas
nuevas se agregan y dónde viven (mismo criterio D-072/D-077: fuente oficial de datos, sin
recalcular fuera de su capa).

**Advertencia detectada, no resuelta aquí**: `AnalizadorOperacional.Analizar` consume
`ResolucionDeIntentos`, un tipo acoplado al modelo de martingala de 2 reintentos (Tres
Mosqueteros/MHI). Las estrategias sin martingala (EMA Cross, Z-Score, Neutral, VolumenBreakout)
ya producen "no aplica" en esa sección (D-088, Caso 3A). Si el comparador de gestores necesita
evaluar sobre estrategias sin martingala (probable, ya que son 4 de las 6), debe apoyarse en
`MetricasFinancieras` como fuente principal, no en `ResolucionDeIntentos` — a confirmar
explícitamente en la especificación de implementación, no asumir.

---

## 4. Candidatos de gestor — evidencia previa por candidato

- **A — Fixed Fractional (statu quo)**: ya implementado, es el `GestorCapital` actual. Sirve como
  control de referencia obligatorio en toda comparación.
- **B — Fixed Risk**: arriesgar un monto fijo por operación (ej. "100 unidades monetarias por
  operación", no un porcentaje de capital). Requiere una noción de "riesgo por operación" que hoy
  no existe explícitamente — a definir en la especificación.
- **C — Volatility Sizing**: exposición adaptada según volatilidad reciente. Requiere una medida
  de volatilidad — el laboratorio ya tiene precedente de ventana deslizante O(1) sobre `Close`
  (`EstrategiaZScoreReversion`) reutilizable conceptualmente, aunque para volatilidad de
  precio, no de volumen.
- **D — Kelly fraccionado**: tamaño basado en ventaja estadística estimada. Mismo riesgo
  metodológico que Masaniello (ver E) — depende de una probabilidad/expectativa de acierto.
- **E — Masaniello**: **ya evaluado en Caso 2.3** (`EVALUACION_MODELOS_GESTION_RIESGO_V1.md`,
  entonces candidato C) y **no implementado** — razón documentada: depende de estimar una
  probabilidad de acierto antes de operar, que "ninguna estrategia del catálogo actual provee de
  forma validada" — inventar ese número mezclaría datos históricos con una promesa sobre el
  futuro (mismo riesgo que D-016 prohíbe para el clasificador de régimen). Ese documento también
  advirtió: Masaniello solo es determinista si la probabilidad es un valor fijo declarado, nunca
  estimado en tiempo real desde resultados parciales de la misma corrida (rompería
  reproducibilidad). **Caso 5A no puede retomar Masaniello/Kelly sin resolver primero de dónde
  sale esa probabilidad como parámetro fijo por convención** — mismo bloqueo de 2.3, no resuelto
  automáticamente por abrir esta fase.

**Consecuencia**: A/B/C tienen una vía de implementación clara sin decisión adicional de fondo
(más allá de "cuál fórmula exacta"). D/E comparten el mismo bloqueo metodológico ya identificado
en Caso 2.3 — su inclusión en el alcance inicial de Caso 5A depende de resolver explícitamente esa
pregunta, no de asumir que ahora sí es viable solo porque se reabre el tema.

---

## 5. Decisiones nuevas — numeración reservada desde D-108

Ninguna decisión se resuelve en esta propuesta — el siguiente documento
(`DECISIONES_CASO5_V1.md`) resuelve cada punto con la misma disciplina de fases anteriores:
opciones, evidencia, criterio, selección explícita del auditor.

**D-108 (candidata) — Aislamiento del cálculo de cantidad vs. clasificación/normalización
compartida**: cómo se separa, en código, la parte 1 (fórmula de cantidad, varía por gestor) de la
parte 2 (clasificación de intención + normalización Cross-Zero, compartida) dentro de lo que hoy
es `GestorCapital.Ajustar` — sin duplicar la parte 2 por cada gestor nuevo. Determina si se
introduce una interfaz (ej. `IGestorRiesgo` con un método que solo calcula cantidad), y si
`GestorCapital` pasa a orquestar (llama al gestor activo + aplica clasificación/normalización) en
vez de contener ambas responsabilidades.

**D-109 (candidata) — Extensión de `ConfiguracionSizing`**: cómo se representa "qué gestor está
activo y con qué parámetros" — nuevo contrato (posiblemente jerárquico: un tipo base + una
variante por gestor) vs. extender el record plano actual con campos opcionales por gestor
(riesgo de mezclar parámetros de gestores no relacionados en un mismo record). Debe preservar
`Default = null` como sizing inactivo, comportamiento histórico bit-a-bit (D-061/D-069 vigentes).

**D-110 (candidata) — Alcance inicial de gestores a implementar**: cuáles de A/B/C/D/E entran en
el primer ciclo de implementación — probablemente A (control) + 1-2 candidatos sin el bloqueo
metodológico de D/E, dejando Kelly/Masaniello como sub-fase posterior condicionada a resolver
explícitamente la fuente de la probabilidad de acierto (mismo patrón de fases incrementales ya
usado en Caso 4.1-4.4).

**D-111 (candidata) — Métricas de comparación**: qué campos nuevos se necesitan (profit factor,
racha de victorias, serie de rachas) y dónde viven — nueva sección de `MetricasFinancieras`,
extensión de `AnalizadorOperacional`, o un tercer componente nuevo — sin recalcular nada que el
motor ya calcule (D-077).

---

## 6. Criterios de éxito

- **Ningún gestor nuevo requiere tocar `IStrategy`, ninguna de las 6 estrategias, ni el motor**
  (`MatchingEngine`/`AplicadorFill`/`ConsumidorFifo`/`ResolutorVela`/`ResolutorCrossZero`) — mismo
  criterio ya aplicado en cada fase desde Caso 1.
- **El comportamiento histórico permanece bit-a-bit idéntico** cuando no se activa un gestor
  nuevo — los 4 baselines congelados (`caso1` a `caso4-v1-experimental`, `caso3b-v1-experimental`)
  siguen produciendo resultados idénticos, verificado explícitamente antes de cerrar cada
  sub-fase.
- **Comparación reproducible**: misma estrategia + mismo dataset + gestores distintos → cada
  corrida reproducible por separado (mismo criterio de hash ya usado en toda fase), y la
  comparación entre gestores debe declarar explícitamente que no implica ranking de superioridad
  (D-014/D-047/D-076).
- **Ningún parámetro de ningún gestor se calibra** — cada uno fijado por convención declarada
  antes de ejecutar pruebas, nunca ajustado observando resultados (D-030).
- **Kelly/Masaniello no se implementan sin resolver primero su bloqueo metodológico** (§4) — si
  D-110 decide diferirlos, quedan como candidatos de una sub-fase posterior, no como deuda oculta.

---

## 7. Exclusiones explícitas

Confirmadas por el auditor, mismo criterio de exclusión que toda fase anterior:

- **Sistema recomendador de gestores** (ranking automático, "qué gestor conviene a esta
  estrategia") — es Caso 5B/6, requiere que Caso 5A produzca evidencia comparativa primero. No se
  abre en esta fase.
- **Portfolio multi-instrumento** — requiere modelo de exposición agregada/correlación/
  contabilidad de portfolio que no existe; multiplicaría complejidad sin una capa de riesgo
  madura todavía (motor actual asume un solo instrumento por corrida, verificado en
  Caso 3 — Candidato F descartado por la misma razón).
- **Optimización/calibración automática de parámetros de ningún gestor** — mantiene la disciplina
  de parámetros por convención ya establecida en todo el proyecto (D-030).
- **Trading real, ejecución en broker, latencia, datos de mercado en vivo** — capas previas no
  construidas (modelo de ejecución real, gestión de errores externos).
- **Límites de exposición/drawdown/circuit breakers/riesgo dinámico** (el alcance originalmente
  considerado para "Caso 5" antes de esta propuesta) — queda diferido explícitamente, candidato de
  una fase posterior una vez exista evidencia comparativa de gestores base.

---

## 8. Criterios de cierre de Caso 5A

El cierre debe responder:
- ¿El laboratorio generaliza a política de capital intercambiable sin tocar la estrategia?
  (evaluado contra §6)
- ¿Qué supuestos ocultos quedan detectados? (documentados, no corregidos silenciosamente —
  mismo principio D-055/D-062/D-095)
- ¿Qué gestores del alcance inicial (D-110) quedaron implementados y cuáles diferidos, y por qué?
- ¿La separación de responsabilidades de D-108 se mantuvo limpia, o algún gestor requirió una
  excepción no anticipada?

---

## Fuera de alcance de este documento

No se implementó código. No se modifica ningún módulo de `src/`. No se resuelve D-108 a D-111 —
solo se declara su existencia y el problema que cada una debe resolver. No se selecciona ningún
gestor de la sección 4 todavía.

---

## Próximo documento

`DECISIONES_CASO5_V1.md` (numeración D-108 en adelante), resolviendo: aislamiento de
responsabilidades dentro de `GestorCapital` (D-108), extensión de `ConfiguracionSizing` (D-109),
alcance inicial de gestores (D-110), y métricas de comparación necesarias (D-111).
