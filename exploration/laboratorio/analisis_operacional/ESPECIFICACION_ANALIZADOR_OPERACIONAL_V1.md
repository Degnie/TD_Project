# Especificación del Analizador Operacional V1

Estado: **especificación — Fase 1.2 del Caso 1**. Documento de diseño, no implementación. No se
modifica `BacktestRunner`, `IStrategy`, DTOs, estrategias ni ningún contrato existente en esta fase.

---

## 1. Objetivo

Transformar el resultado técnico ya producido por el motor de backtest (`ResultadoBacktest`,
`InfoOperacionResuelta`, `PerfilMultiTf`) en información operacional comprensible para un usuario
del laboratorio, sin introducir interpretación financiera.

Pregunta que responde: **"¿Cómo se comportó esta estrategia bajo las condiciones evaluadas?"**

Pregunta que NO responde: "¿Cuánto dinero habría ganado realmente?" — eso pertenece a Caso 2.

---

## 2. Alcance

**Analiza**: operaciones completadas/incompletas, victorias/derrotas, resolución de intentos
(inicial/M1/M2), dependencia de escalado, distribución de rachas, exposición experimental,
completitud del dataset usado, comparación entre timeframes de una misma estrategia.

**No analiza**: rentabilidad, retorno financiero, Sharpe, riesgo monetario, costes reales, margen
real, comparación de estrategias por dinero. Estos elementos pertenecen a Caso 2 y no son parte del
catálogo de métricas oficiales de este documento.

**No implementa en esta fase**: el clasificador de escenarios de mercado (sección 4.5) — se define
qué información necesitará y qué métricas devolverá, pero no se construye el clasificador.

---

## 3. Modelo de datos de entrada

El analizador consume **exclusivamente** campos que ya existen y están verificados — no requiere
ningún cálculo nuevo del motor ni cambios en `BacktestRunner`/`IStrategy`/DTOs.

| Fuente | Campo | Tipo | Ya usado en |
|---|---|---|---|
| `PerfilMultiTf` | `Identidad` (dataset, timeframe, estrategia, capital, versión agregación, hashes, fecha) | `IdentidadExperimento` | Fichas de estrategia, baseline |
| `PerfilMultiTf` | `EstadoMotor` | `EstadoBacktest` | Integridad del motor |
| `PerfilMultiTf` | `OperacionesCompletadas`, `OperacionesGanadas`, `OperacionesPerdidas` | `int` | Métricas oficiales |
| `PerfilMultiTf` | `OperacionAbiertaAlCierre`, `CapitalComprometidoAlCierre` | `bool`, `decimal` | Categoría separada de ganada/perdida |
| `PerfilMultiTf` | `RachaNegativaMaxima`, `Racha2`, `Racha3`, `Racha4`, `Racha5Mas` | `int` | Distribución de rachas |
| `PerfilMultiTf` | `GanoInicial`, `GanoM1`, `GanoM2`, `PerdioAgotandoMartingalas` | `int` | Resolución de intentos |
| `PerfilMultiTf` | `MaxExposicion` | `decimal` | Peores escenarios |
| `PerfilMultiTf` | `ReconciliacionCoherente`, `ErroresReconciliacion` | `bool`, `IReadOnlyList<string>` | Integridad del motor |
| `PerfilMultiTf` | `VelasDisponibles`, `VelasUtilizadas`, `VelasExcluidas`, `PctUtilizado` | `int`, `int`, `int`, `decimal` | Completitud del dataset |
| `PerfilMultiTf` | `EquityInicial`, `EquityFinal`, `RetornoPct` | `decimal` | **Solo como dato derivado no oficial — sección 5** |

**Explícitamente NO incluido como entrada** (no existe hoy, no se construye en esta fase): PnL
atribuido por nivel de martingala (M0/M1/M2). El motor no calcula equity segmentado por nivel —
ver "Decisiones pendientes", sección 7.

Una corrida = una instancia de `PerfilMultiTf` (una combinación estrategia × timeframe × dataset).
El analizador opera sobre una lista de instancias para producir la vista multi-timeframe.

---

## 4. Catálogo de métricas

### 4.1 Resultado general de estrategia — "Eficiencia operacional"

| Campo | Descripción | Fórmula | Fuente | Ejemplo |
|---|---|---|---|---|
| Intentos totales | Operaciones completadas + abierta al cierre (si aplica) | `OperacionesCompletadas + (OperacionAbiertaAlCierre ? 1 : 0)` | `PerfilMultiTf` | 82476 |
| Intentos completados | Operaciones resueltas (ganada o perdida) dentro del dataset | `OperacionesCompletadas` | `PerfilMultiTf.OperacionesCompletadas` | 82475 |
| Intentos incompletos | Operación abierta al cierre del dataset (categoría separada, nunca ganada/perdida) | `OperacionAbiertaAlCierre ? 1 : 0` | `PerfilMultiTf.OperacionAbiertaAlCierre` | 0 o 1 |
| Victorias | Operaciones completadas con resultado ganador | `OperacionesGanadas` | `PerfilMultiTf.OperacionesGanadas` | 71816 |
| Derrotas | Operaciones completadas con resultado perdedor | `OperacionesPerdidas` | `PerfilMultiTf.OperacionesPerdidas` | 10659 |
| **Eficiencia operacional** | Proporción de operaciones completadas que resultaron ganadoras | `OperacionesGanadas / OperacionesCompletadas * 100` | Derivado | 87.08% |

**Nombre aprobado**: "Eficiencia operacional". Prohibido usar "rentabilidad", "rendimiento
financiero" o "retorno" para este campo — ver sección 5 y "Interpretación prohibida" en sección 6.

### 4.2 Resolución de intentos

Formaliza `GanoInicial`/`GanoM1`/`GanoM2`/`PerdioAgotandoMartingalas`, ya calculados por el motor
pero no expuestos hasta ahora en ningún reporte por defecto.

| Campo | Descripción | Fórmula | Fuente |
|---|---|---|---|
| Victoria inicial | % de operaciones completadas ganadas sin ningún reintento | `GanoInicial / OperacionesCompletadas * 100` | `PerfilMultiTf.GanoInicial` |
| Recuperación M1 | % de operaciones completadas ganadas en el primer reintento | `GanoM1 / OperacionesCompletadas * 100` | `PerfilMultiTf.GanoM1` |
| Recuperación M2 | % de operaciones completadas ganadas en el segundo reintento | `GanoM2 / OperacionesCompletadas * 100` | `PerfilMultiTf.GanoM2` |
| Pérdida agotando | % de operaciones completadas que perdieron tras agotar todos los reintentos permitidos | `PerdioAgotandoMartingalas / OperacionesCompletadas * 100` | `PerfilMultiTf.PerdioAgotandoMartingalas` |

Los 4 porcentajes suman 100% de `OperacionesCompletadas` por construcción (partición exhaustiva:
toda operación completada gana en algún nivel o pierde agotando).

Ejemplo (formato del brief):
```
Victoria inicial:     65%
Recuperación M1:      25%
Recuperación M2:       7%
Pérdida agotando:      3%
```

### 4.3 Dependencia de martingala — solo porcentaje, sin clasificación (D-005)

**Decisión de auditoría (D-005, 2026-08-11)**: la clasificación cualitativa (baja/media/alta)
propuesta inicialmente **no se aprueba** como regla oficial. Los umbrales originales (25%/40%)
fueron inferidos únicamente de la muestra actual (34%-41% en las 12 corridas del catálogo), lo cual
arriesga adaptar la escala al comportamiento de las estrategias ya conocidas y penalizar o
favorecer estrategias futuras de forma no objetiva.

**Alcance aprobado para esta fase**: el reporte muestra únicamente el porcentaje de
`PctResueltasPorMartingala` (= Recuperación M1 + Recuperación M2, fórmula ya implementada en
`PerfilMultiTf.cs:38`), **sin** traducirlo a una etiqueta cualitativa.

Formato aprobado:
```
Dependencia de escalado:

Victoria inicial:      61%
Recuperación M1:       29%
Recuperación M2:        7%
Pérdida agotando:       3%
```

Formato **no** aprobado todavía (no implementar):
```
Dependencia:            Alta          [NO APROBADO — D-005 pendiente]
```

**Decisión pendiente para una futura Fase 1.x**: cómo definir la clasificación cualitativa, entre
tres opciones registradas por auditoría:

- **Opción A — Estadística histórica**: basada en la distribución del catálogo (percentiles,
  desviación estándar, comparación contra población de estrategias evaluadas).
- **Opción B — Regla fija del usuario**: ej. "alta dependencia = más del 50% de victorias
  requieren escalado", definida explícitamente por el usuario/auditor, no inferida de datos.
- **Opción C — Eliminar la categoría cualitativa**: mantener solamente porcentajes, sin
  clasificación. Actualmente es la opción más limpia según auditoría, aunque no se descarta
  ninguna de las tres.

No genera conclusiones financieras (ej. no traduce el porcentaje a "mayor riesgo de pérdida
monetaria").

### 4.4 Análisis por timeframe

Estructura jerárquica por estrategia:

```
Estrategia
 ├── 1m   → Eficiencia operacional, Resolución de intentos, Dependencia de escalado
 ├── 2m   → (dataset disponible, no evaluado por el motor en Fase 2C — ver limitaciones)
 ├── 5m   → Eficiencia operacional, Resolución de intentos, Dependencia de escalado
 ├── 10m  → (dataset disponible, no evaluado por el motor en Fase 2C — ver limitaciones)
 ├── 15m  → Eficiencia operacional, Resolución de intentos, Dependencia de escalado
 ├── 30m  → (dataset disponible, no evaluado por el motor en Fase 2C — ver limitaciones)
 ├── 1h   → Eficiencia operacional, Resolución de intentos, Dependencia de escalado
 ├── 2h/4h/8h/12h → parcialmente evaluado (solo 4h en Fase 2C) o pendiente
 ├── 1D   → Eficiencia operacional, Resolución de intentos, Dependencia de escalado
 └── 1W   → (dataset disponible con 2 velas parciales excluidas de backtest, no evaluado en Fase 2C)
```

Cada nodo evaluado expone las tres métricas de las secciones 4.1-4.3 más la completitud del
dataset (`VelasDisponibles`/`VelasUtilizadas`/`PctUtilizado`) para ese timeframe específico.

### 4.5 Análisis por escenario de mercado — definición, no implementación

**Fuera de implementación en esta fase.** Se define únicamente qué información necesitará el
futuro clasificador y qué métricas devolverá, para que el diseño de Fase 1.2 no quede ciego a este
requisito futuro:

- **Información que necesitará**: una función de clasificación `Vela → EscenarioMercado` (o
  `SecuenciaDeVelas → EscenarioMercado`) que etiquete tramos del dataset como Alcista/Bajista/
  Lateral (u otras categorías a definir). No existe hoy en el sistema — los generadores sintéticos
  de Fase 1.5 (`GeneradorTendencia.cs`, `GeneradorLateral.cs`, etc.) construyen datasets con un
  régimen conocido de antemano, pero no hay un clasificador que etiquete un dataset real ya
  descargado (como BTC/USDT) por tramos.
- **Métricas que devolverá** (una vez implementado): las mismas de la sección 4.4 (eficiencia
  operacional, resolución de intentos, dependencia de escalado) pero agrupadas por escenario de
  mercado en vez de por timeframe — mismo catálogo de métricas, distinto criterio de agrupación.
- **Decisión pendiente**: cómo definir los límites de un "escenario" dentro de un dataset continuo
  (ventana fija, detección de cambio de régimen, etc.) — no se resuelve en este documento.

### 4.6 Peores escenarios observados

| Campo | Descripción | Fuente |
|---|---|---|
| Mayor racha negativa | Longitud máxima de operaciones completadas perdidas consecutivas | `PerfilMultiTf.RachaNegativaMaxima` |
| Mayor cantidad de martingalas consecutivas | Máximo nivel de reintento alcanzado (implícito: si `GanoM2 > 0` o `PerdioAgotandoMartingalas > 0` con `maxMartingalas = 2`, la estrategia llegó al tope configurado) | `PerfilMultiTf.GanoM2`, `PerfilMultiTf.PerdioAgotandoMartingalas`, configuración `maxMartingalas` |
| Mayor exposición experimental | Máxima cantidad de lotes vivos simultáneos observada durante la corrida | `PerfilMultiTf.MaxExposicion` |
| Escenarios donde falla | Lista cualitativa ya documentada por estrategia (ver fichas, sección "Limitaciones") | `catalogo_estrategias/*.md` |

**No se convierte en predicción futura**: estos valores describen lo ya observado en la corrida
evaluada, no proyectan comportamiento en datos no vistos.

### 4.7 Nivel de confianza de la métrica (mejora futura — no bloqueante)

**Clasificación de auditoría**: Mejora futura, no bloquea el cierre de Fase 1.2.

Observación registrada: ninguna métrica del catálogo (secciones 4.1-4.6) indica hoy el tamaño de
muestra que la sustenta. Una "Eficiencia operacional: 87%" sobre 82,475 operaciones no tiene el
mismo peso experimental que la misma cifra sobre 35 operaciones — esto será especialmente relevante
cuando se incorporen estrategias con pocos eventos (timeframes largos, mercados con baja frecuencia
de señal).

Ejemplo de formato propuesto para una futura implementación (no oficial en V1):
```
Eficiencia operacional:  87%
Muestras:                82475 operaciones
Confianza experimental:  Mayor

vs.

Eficiencia operacional:  84%
Muestras:                35 operaciones
Confianza experimental:  Baja
```

No se define en este documento ni el umbral de "confianza baja/mayor" ni la fórmula — igual que
D-005, cualquier clasificación cualitativa sobre una métrica requiere la misma cautela contra
inferir reglas universales de la muestra actual. Queda registrado como mejora futura, a evaluar en
una fase posterior junto con D-005.

---

## 5. Datos derivados del modelo actual (no financieros)

`EquityInicial`, `EquityFinal`, `RetornoPct` se mantienen disponibles como **dato derivado
experimental**, exactamente con la misma clasificación y las mismas advertencias ya establecidas
en el catálogo de estrategias (Fase 1.1) y en Fase 2D:

- No se usan para ranking de estrategias.
- No entran al catálogo de métricas oficiales (sección 4).
- Se muestran en una sección separada del reporte (sección 6, punto 8), nunca mezclados con
  eficiencia operacional u otras métricas oficiales.
- Un valor de `EquityFinal` negativo no se reinterpreta como pérdida financiera real — sigue
  siendo, como ya se estableció, consecuencia del modelo de posición actual (tamaño fijo, sin
  modelo de riesgo/margen completo), no una observación nueva de esta fase.

---

## 6. Clasificación de cada elemento del catálogo

Todo campo del catálogo de métricas (sección 4) se clasifica en una de cuatro categorías:

**Dato observado** — valor leído directamente de `PerfilMultiTf`, sin transformación.
```
Cantidad de operaciones completadas: 82475
```

**Métrica calculada** — valor derivado por fórmula a partir de datos observados.
```
Eficiencia operacional: 87.08%
```

**Interpretación permitida** — enunciado cualitativo sobre comportamiento operacional, sin
traducción monetaria y sin clasificar el porcentaje en una escala no aprobada (D-005).
```
El 37.2% de las operaciones completadas requirió escalado (M1 o M2) para resolverse.
```

**Interpretación prohibida** — cualquier enunciado que traduzca una métrica operacional a
resultado financiero, ganancia real o conclusión de rentabilidad.
```
La estrategia genera un 72% de ganancias reales.          [PROHIBIDO]
Esta estrategia es más rentable que la otra.               [PROHIBIDO]
Con 87% de eficiencia se recupera la inversión.             [PROHIBIDO]
```

Toda métrica del catálogo (sección 4) debe quedar etiquetada con una de las primeras tres
categorías en su implementación futura; la cuarta categoría existe solo como lista de ejemplos a
evitar, no como salida válida del analizador.

---

## 7. Formato futuro del reporte

Estructura de salida esperada (diseño, no implementación):

```
Reporte de Estrategia

1. Identidad
   (Dataset, Timeframe, Estrategia, Capital, Hashes, Fecha — de IdentidadExperimento)

2. Resultado general
   (Eficiencia operacional, intentos totales/completados/incompletos, victorias/derrotas)

3. Resolución de intentos
   (Victoria inicial / M1 / M2 / Pérdida agotando)

4. Dependencia de martingala
   (Clasificación baja/media/alta con el % que la sustenta)

5. Multi-timeframe
   (Tabla comparativa de 2-4 por cada timeframe evaluado)

6. Escenarios de mercado
   (Placeholder — "clasificador no implementado, ver ESPECIFICACION_ANALIZADOR_OPERACIONAL_V1.md §4.5")

7. Peores escenarios
   (Mayor racha negativa, mayor exposición, escenarios de fallo conocidos)

8. Datos derivados del modelo actual (no financieros)
   (EquityInicial, EquityFinal, Retorno% — con nota de no comparabilidad)

9. Limitaciones
   (Heredadas de Fase 2D/1.0/1.1 — modelo económico incompleto, sin costes reales, etc.)
```

---

## Decisiones registradas por auditoría (2026-08-11)

**D-005 — Dependencia de martingala**: ⏳ Pendiente. Aprobado mostrar únicamente el porcentaje
(sección 4.3). No aprobada la clasificación cualitativa baja/media/alta — los umbrales originales
(25%/40%) fueron inferidos solo de la muestra actual (34%-41%), con riesgo de adaptar la escala al
comportamiento de las estrategias conocidas. Tres opciones registradas para una futura Fase 1.x:
(A) estadística histórica sobre la distribución del catálogo, (B) regla fija definida por el
usuario, (C) eliminar la categoría cualitativa y mantener solo porcentajes — actualmente la más
limpia según auditoría, sin descartar las otras dos.

**D-006 — Definición de escenario de mercado**: ⏳ Correctamente pendiente. No debe definirse en
Fase 1.2 — el clasificador de régimen de mercado es una pieza experimental independiente que debe
resolver primero "¿cómo definimos objetivamente alcista/bajista/lateral?" antes de "¿cómo medimos
la estrategia dentro de ellos?". Orden obligatorio para una fase futura: Dataset → Clasificador de
régimen → Segmentos de mercado → Evaluación de estrategia. No invertir el orden.

**D-007 — Cobertura real de timeframes**: ✅ Aprobado. Queda formalizada la distinción entre
"timeframes derivados disponibles" (13 — existen, tienen integridad, tienen hash, ver baseline V1)
y "timeframes evaluados por backtest" (12 combinaciones estrategia×timeframe realmente ejecutadas
en Fase 2C, según evidencia registrada). No se permite interpretar "el sistema soporta todos los
timeframes" como equivalente a "la estrategia fue validada en todos los timeframes" — distinción ya
reflejada en la sección 4.4 y en "Decisiones pendientes" punto 4 (ahora resuelto por D-007: la
distinción queda como regla permanente del laboratorio, no como limitación a resolver).

**Nivel de confianza de la métrica** (sección 4.7): clasificado como Mejora futura, no bloquea el
cierre de esta fase.

---

## Fuera de alcance (respetado)

No se realizaron cambios en `BacktestRunner`, `IStrategy`, DTOs, estrategias, ni implementación de
indicadores de mercado, gestión de riesgo, modelo financiero o ranking de estrategias por dinero.
Este documento es la única salida de esta fase.

---

## Criterio de cierre de Fase 1.2

- ✓ Especificación formal del analizador operacional creada.
- ✓ Métricas definidas matemáticamente (sección 4, con fórmula y fuente por cada campo).
- ✓ Cada métrica tiene fuente de datos identificada (todas provienen de `PerfilMultiTf`, ya
  verificado — ninguna requiere cálculo nuevo del motor).
- ✓ Interpretación operacional separada de la financiera (secciones 5 y 6).
- ✓ Formato futuro del reporte diseñado (sección 7).
- ✅ Auditoría aprueba la especificación — **Diseño aprobado** (2026-08-11). D-005 y D-006 quedan
  pendientes para una fase posterior sin bloquear el cierre; D-007 queda aprobada. Implementación
  autorizada a continuación bajo el orden Paso 1 (modelos de lectura) → Paso 2 (pruebas unitarias
  con resultados conocidos) → Paso 3 (comparación motor vs. analizador, sin modificar la fuente
  original).
- ✅ **Fase 1.2 cerrada por auditoría (2026-08-11)** — implementación completada en
  `exploration/laboratorio/analisis_operacional/` (`AnalizadorOperacional.cs`, `Tests.cs`,
  `Program.cs`), 6/6 pruebas pasan reproduciendo cifras ya publicadas en el catálogo, 0 cambios en
  `src/`/`tests/`. Decisiones registradas en el cierre:
  - **D-008 — Analizador operacional separado del motor**: aprobado. El laboratorio mantiene la
    separación Motor (ejecutar) → Analizador (interpretar) → Reporte (comunicar) como regla
    permanente.
  - **D-009 — Métricas financieras fuera del analizador operacional**: aprobado. El analizador no
    debe evolucionar hacia simulador financiero; esa capa es responsabilidad de un futuro Caso 2.
  - **O-001 — Falta integración automática (pipeline Backtest → PerfilMultiTf → Analizador →
    Reporte)**: observación no bloqueante, clasificada como mejora futura, no pertenece a esta fase.
  - **O-002 — Falta persistencia del reporte (JSON/Markdown/visual)**: observación no bloqueante,
    clasificada para una futura Fase 1.5.
