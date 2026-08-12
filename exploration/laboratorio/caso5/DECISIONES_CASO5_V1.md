# Decisiones — Caso 5A: Evaluación Comparativa de Gestores de Riesgo

Estado: **D-108 a D-111 resueltas, con una precisión derivada de D-109** (identidad del gestor
activo separada del contrato funcional de `IGestorRiesgo`, ver sección D-109). Misma estructura
usada en D-001 a D-107 (decisión, opciones,
criterio, evidencia, resolución). Ningún código se modifica en este documento — las resoluciones
aquí registradas habilitan la especificación de implementación siguiente, no la reemplazan.

Contexto completo en `../PROPUESTA_CASO5_V1.md`. Verificación contra código existente, no
reconstruida de memoria (mismo criterio que abrió Caso 2/Caso 3/Caso 4, D-057).

---

## D-108 — Aislamiento del cálculo de cantidad vs. clasificación/normalización compartida

**Estado**: 🟢 Aprobada.

**Decisión**: cómo separar, dentro de lo que hoy es `GestorCapital.Ajustar`
(`src/Domain/Portfolio/GestorCapital.cs:30-72`), la parte específica de un gestor (fórmula de
cantidad) de la parte compartida por cualquier gestor (clasificación de intención + normalización
de Cross-Zero bajo sizing, D-092/D-095) — sin duplicar la parte compartida por cada gestor nuevo.

### Resolución adoptada

Se introduce una interfaz `IGestorRiesgo` cuyo único método calcula la cantidad para
Apertura/Aumento (equivalente a las líneas 37-39 actuales: `capitalDisponible`, `margenObjetivo`,
`cantidadCalculada` — reemplazadas por una llamada al gestor activo). `GestorCapital.Ajustar` deja
de contener la fórmula y pasa a **orquestar**: recibe el `IGestorRiesgo` activo, lo invoca para
obtener `cantidadCalculada`, y conserva intacta toda la lógica de clasificación de intención +
normalización de Cross-Zero (líneas 41-71) como código único, no perteneciente a ningún gestor.

Flujo resultante:
```
OrderRequest
     |
     v
Clasificador/Normalizador comun (GestorCapital, sin cambios de comportamiento)
     |
     v
IGestorRiesgo activo (calcula cantidad solo para Apertura/Aumento)
     |
     v
Cantidad final
     |
     v
Fill
```

**Por qué esta separación y no otra**: la clasificación de intención y la normalización de
Cross-Zero no dependen de qué fórmula produjo `cantidadCalculada` — dependen únicamente de si hay
sizing activo o no, y de la posición proyectada. Confirmado leyendo `GestorCapital.cs:41-71`: nada
en ese bloque referencia `PorcentajeRiesgo` ni ningún parámetro de Fixed Fractional. Es, en la
práctica, ya código independiente del gestor concreto — la interfaz solo lo hace explícito.

### Restricciones que aplican

- `IGestorRiesgo` no participa en la clasificación de intención ni en la normalización de
  Cross-Zero — un gestor nuevo no puede alterar ese comportamiento, solo el valor de
  `cantidadCalculada`.
- `Sizing=null → requests intacto` (D-061/D-069) permanece sin cambios: la ruta de salida
  temprana en `Ajustar` no invoca ningún `IGestorRiesgo`.
- El comportamiento con Fixed Fractional activo debe permanecer bit-a-bit idéntico al actual —
  la migración de la fórmula a `IGestorRiesgo` es un refactor de ubicación, no de lógica.

### Evidencia

- `GestorCapital.cs:30-72` (leído completo): confirma que las líneas 37-39 son la única porción
  que referencia `PorcentajeRiesgo`; el resto (41-71) opera solo sobre `intencion`,
  `cantidadEfectiva`, `posicionProyectada`.
- `BacktestRunner.cs:57`: único punto de invocación de `GestorCapital.Ajustar` en `src/` —
  confirma que el cambio de forma interna no afecta ningún otro consumidor.

---

## D-109 — Extensión de `ConfiguracionSizing`

**Estado**: 🟢 Aprobada.

**Decisión**: cómo representar "qué gestor está activo y con qué parámetros", dado que
`ConfiguracionSizing` hoy es un único record con un solo campo (`PorcentajeRiesgo`) acoplado 1:1 a
Fixed Fractional.

### Resolución adoptada

`ConfiguracionSizing` **no** se convierte en un enum de tipos (`Tipo = Kelly | Masaniello |
Fixed | ...`) — eso anticiparía gestores que Caso 5A no implementa todavía (D-110), mezclando en
un mismo contrato parámetros de gestores inexistentes.

En su lugar, `ConfiguracionSizing` pasa a describir una **elección**, no una lógica: contiene el
gestor activo (una instancia de `IGestorRiesgo` ya parametrizada, o un identificador + parámetros
propios de ese gestor) — la lógica de cálculo vive exclusivamente en la implementación de
`IGestorRiesgo`, nunca en el record de configuración.

```
ConfiguracionSizing
    GestorActivo: IGestorRiesgo   (o equivalente minimo que lo identifique + parametrice)
```

**Por qué no un campo por gestor**: un record con `PorcentajeRiesgo`, `RiesgoPorOperacion`,
`FraccionKelly`, etc. todos opcionales permitiría estados inválidos (dos gestores "parametrizados"
a la vez sin que ninguno esté realmente activo) y crecería con cada gestor nuevo — contrario a
D-108, que ya aisló el cálculo específico en `IGestorRiesgo`.

### Restricciones que aplican

- `ConfiguracionSizing.Default => null` se preserva sin cambios (D-061/D-069): sizing inactivo
  sigue siendo `null`, no una instancia de gestor con parámetros neutros.
- Ningún gestor implementado en D-110 introduce un campo en `ConfiguracionSizing` fuera de su
  propia parametrización — el contrato base no crece por gestor.

### Evidencia

- `ConfiguracionSizing.cs` (leído completo): confirma el estado actual de un solo campo, sin
  noción de tipo/variante.
- Precedente rechazado explícitamente por el auditor: enum de tipos con campos por gestor.

### Precisión derivada de D-109 (detectada en la especificación de implementación, no en esta
decisión — mismo patrón que la precisión de D-107 en Caso 3B: no reabre la arquitectura, completa
un punto que quedó sin resolver)

**Hallazgo**: `IdentidadExperimentoCompleta.CalcularHashConfiguracionEconomica`
(`exploration/laboratorio/protocolo/IdentidadExperimentoCompleta.cs:61`) serializa
`ConfiguracionSizing.PorcentajeRiesgo` a texto para construir `HashConfiguracionEconomica`
(D-082) — el hash determinista de la configuración económica de cada corrida. Con `PorcentajeRiesgo`
reemplazado por `GestorActivo: IGestorRiesgo`, ese campo deja de existir, y `IGestorRiesgo`
(D-108: responsabilidad única, calcular cantidad) no tiene representación textual estable.

**Precisión**: la identidad del gestor activo forma parte de la identidad experimental económica,
pero **no pertenece al contrato funcional de `IGestorRiesgo`**. D-108 permanece intacta —
`IGestorRiesgo` conserva una única responsabilidad (calcular cantidad), no se le agrega una
segunda. En su lugar, se introduce un contrato separado:

```
IGestorRiesgo                    IIdentidadGestorRiesgo
    CalcularCantidad(...)            ObtenerIdentidadConfiguracion(): string
```

Un gestor concreto implementa ambos (`GestorFixedFractional : IGestorRiesgo,
IIdentidadGestorRiesgo`), pero son capacidades conceptualmente distintas — una calcula, la otra se
identifica. `IdentidadExperimentoCompleta` consume solo `IIdentidadGestorRiesgo`, sin conocer
ningún tipo concreto de gestor (rechaza explícitamente el pattern-matching-por-tipo: cada gestor
nuevo no debe obligar a modificar la capa de identidad).

**Formato de identidad**: determinista, estable, basado en configuración declarada — nunca en
resultado de ejecución. Ejemplos: `fixed-fractional:v1:riesgo=0.1`,
`fixed-risk:v1:monto=100`, `volatility-sizing:v1:ventana=20:desviacion=2`. Nunca contiene
retorno/drawdown/métricas ni ningún valor calculado durante el backtest.

**Ajuste a `IdentidadExperimentoCompleta.CalcularHashConfiguracionEconomica`**: `sizing is null →
"sin-sizing"` (sin cambio); `sizing activo → sizing.GestorActivo` debe resolverse vía
`IIdentidadGestorRiesgo` — si el gestor activo no implementa esa interfaz, la construcción de
identidad **falla explícitamente** (excepción, no un valor inventado ni un hash silenciosamente
distinto) — mismo principio D-055/D-062/D-095 de no ocultar un supuesto no satisfecho.

**Estado**: 🟢 Precisión aprobada. No es una decisión nueva (no recibe número D-N) — es una
ampliación de D-109 ya cerrada, resuelta antes de tocar código.

---

## D-110 — Alcance inicial de gestores a implementar en Caso 5A

**Estado**: 🟢 Aprobada.

**Decisión**: cuáles de los candidatos A (Fixed Fractional) / B (Fixed Risk) / C (Volatility
Sizing) / D (Kelly fraccionado) / E (Masaniello) entran en el primer ciclo de implementación de
Caso 5A.

### Resolución adoptada

**Entran, en este orden de implementación**:
1. **A — Fixed Fractional**: se convierte en la implementación de referencia de `IGestorRiesgo`
   (control obligatorio de toda comparación) — no por ser superior, sino por ser el baseline ya
   validado en 5 fases congeladas.
2. **B — Fixed Risk**: monto fijo de riesgo por operación, no porcentaje de capital. Se prioriza
   antes que C porque responde una pregunta distinta y más fundamental que la de C: ¿importa más
   controlar capital proporcional o controlar exposición monetaria absoluta? — una pregunta de
   diseño económico, no de adaptación a régimen.
3. **C — Volatility Sizing**: exposición adaptada a volatilidad reciente. Entra después de B
   porque depende de una pregunta distinta (adaptación a régimen, no elección de unidad de
   riesgo) — evaluarla después de fijar B evita mezclar ambos ejes en la misma ronda de
   comparación.

**Quedan fuera de este ciclo, como candidatos de una sub-fase posterior**:
- **D — Kelly fraccionado** y **E — Masaniello**: comparten el bloqueo metodológico ya
  identificado en Caso 2.3 (`EVALUACION_MODELOS_GESTION_RIESGO_V1.md`) — dependen de una
  probabilidad de acierto que ninguna estrategia del catálogo provee de forma validada. No se
  reabren automáticamente por abrir Caso 5A; su inclusión requiere primero resolver, en una
  decisión propia, de dónde sale esa probabilidad como **valor fijo declarado por convención**
  (nunca estimado en tiempo real desde resultados parciales, lo que rompería reproducibilidad).
- Martingala avanzada / cualquier variante no listada en §4 de la propuesta: fuera de alcance,
  no evaluada.

### Restricciones que aplican

- El bloqueo de D/E no se resuelve implícitamente por Caso 5A — cualquier intento futuro de
  incluirlos debe abrir una decisión explícita sobre la fuente de la probabilidad, con el mismo
  criterio que D-016 ya exige para el clasificador de régimen (no mezclar histórico con promesa
  futura).
- A es obligatorio en toda comparación como control — ningún resultado de B o C se reporta sin
  el resultado equivalente de A sobre el mismo dataset/estrategia/configuración económica.

### Evidencia

- `EVALUACION_MODELOS_GESTION_RIESGO_V1.md` (Caso 2.3): origen documentado del bloqueo de
  Masaniello, extendido aquí a Kelly por compartir la misma dependencia de probabilidad estimada.
- `PROPUESTA_CASO5_V1.md` §4: evidencia previa por candidato, ya distingue A/B/C (vía clara) de
  D/E (bloqueo compartido).

---

## D-111 — Métricas de comparación

**Estado**: 🟢 Aprobada.

**Decisión**: qué campos nuevos se necesitan para comparar gestores de forma significativa, más
allá de lo que `MetricasFinancieras`/`AnalizadorOperacional` ya exponen, y dónde viven.

### Resolución adoptada

Se agrega un conjunto mínimo de métricas nuevas, agrupadas por categoría (a ubicar en la
especificación de implementación siguiente — probablemente extensión de `MetricasFinancieras`
para las de capital/riesgo, y un componente nuevo para las de consistencia/supervivencia, sin
recalcular nada que el motor ya calcule, D-077):

- **Retorno**: retorno absoluto, retorno porcentual (derivables de `PnLTotal`/`CapitalInicial`,
  ya existentes — confirmar si ya alcanzan o si se necesita un campo explícito).
- **Riesgo**: máximo drawdown (ya existe: `DrawdownMaximoPct`), duración del drawdown (nueva),
  peor pérdida consecutiva (ya existe: `MayorRachaNegativa`, con la advertencia de acoplamiento a
  `ResolucionDeIntentos` documentada en `PROPUESTA_CASO5_V1.md` §3 — a resolver en la
  especificación).
- **Consistencia**: cantidad de operaciones (ya existe), porcentaje de operaciones positivas (ya
  existe: `EficienciaOperacionalPct`), profit factor (nuevo), ratio beneficio/pérdida (nuevo).
- **Exposición**: exposición máxima (ya existe: `ExposicionMaxima`), margen máximo utilizado
  (nuevo), capital libre mínimo (nuevo).
- **Supervivencia**: riesgo de ruina (nuevo — requiere definición explícita de "ruina" en esta
  fase, ej. capital por debajo de un umbral irrecuperable), proximidad a incapacidad (relacionado
  con `ValidadorCapacidad`/`CalculadoraReservaPreventiva` ya existentes, D-084 — a confirmar si se
  reutiliza esa fuente o se define una nueva métrica).

**Por qué no recalcular fuera de la capa oficial**: cada métrica que ya existe
(`DrawdownMaximoPct`, `MayorRachaNegativa`, `ExposicionMaxima`, `EficienciaOperacionalPct`) se
reutiliza tal cual — el comparador de gestores no reimplementa su cálculo (D-072/D-077). Las
métricas nuevas se definen en la especificación de implementación con la misma fuente única de
verdad que ya rige cada capa (financiera vs. operacional).

### Restricciones que aplican

- Ninguna métrica nueva se calcula fuera de la capa que ya posee los datos fuente
  (`MetricasFinancieras` para capital/equity, `AnalizadorOperacional` para conteo de
  operaciones/rachas) — mismo criterio D-072/D-077.
- La definición exacta de "riesgo de ruina" y "proximidad a incapacidad" se fija en la
  especificación de implementación, no en esta decisión — aquí solo se aprueba que ambas métricas
  entran al alcance.

### Evidencia

- `MetricasFinancieras.cs`, `AnalizadorOperacional`/`ReporteOperacional` (leídos en la
  investigación previa a `PROPUESTA_CASO5_V1.md`): confirman qué campos existen hoy.
- `PROPUESTA_CASO5_V1.md` §3: advertencia sobre `ResolucionDeIntentos` y martingala, no resuelta
  aquí, trasladada a la especificación de implementación.

### Precisión de alcance de implementación (detectada durante la implementación, no reabre D-111)

**Alcance reducido efectivamente implementado**: `ProfitFactor` y `CapitalLibreMinimo` (nuevos,
`MetricasFinancieras`), `MargenMaximoUtilizado` (aprobado como equivalente de `ExposicionMaxima`
ya existente — `PortfolioSnapshots.Max(s => s.Margin)`, mismo cálculo, sin duplicar campo).

**`RachaPositivaMaxima` permanece dentro del alcance conceptual de D-111, pero su implementación
se difiere** — no se calcula en esta ronda. Motivo: vive naturalmente en
`exploration/laboratorio/evaluacion_multi_tf/PerfilMultiTf.cs` (capa de evaluación agregada
multi-timeframe), fuera de los 4 archivos que Caso 5A autorizó modificar (`GestorCapital`,
`ConfiguracionSizing`, `BacktestRunner`, `IdentidadExperimentoCompleta`). Introducirla ahora
mezclaría dos objetivos distintos — validar gestores de riesgo vs. ampliar capacidades generales
de análisis operacional — que deben mantenerse separados para conservar la trazabilidad
experimental de esta fase. Queda diferida junto con duración de drawdown y riesgo de ruina — **no
se registra como deuda bloqueante**: las métricas ya implementadas (retorno, profit factor,
drawdown existente, margen máximo, capital libre, exposición) son suficientes para la comparación
inicial de gestores que Caso 5A busca habilitar.

---

## Criterio adicional de Caso 5A — control experimental

Además de lo ya fijado en `PROPUESTA_CASO5_V1.md` §6, toda comparación entre gestores debe
mantener constantes: misma estrategia + mismo dataset + mismo timeframe + misma configuración
económica, variando **únicamente** el gestor de riesgo activo. Cualquier diferencia observada en
las métricas de D-111 debe ser atribuible solo al cambio de gestor — ninguna corrida de
comparación puede variar dos ejes a la vez.

---

## Fuera de alcance de este documento

No se implementó código. No se modifica ningún módulo de `src/`. D-108 a D-111 quedan resueltas a
nivel de diseño — la especificación de implementación siguiente traduce cada resolución a
estructura de código concreta (nombres de tipos, firmas, ubicación exacta de archivos), sin
reabrir ninguna de las decisiones aquí fijadas.

---

## Próximo documento

`ESPECIFICACION_IMPLEMENTACION_CASO5A_V1.md`, traduciendo D-108 (interfaz `IGestorRiesgo` +
orquestación en `GestorCapital`), D-109 (forma exacta de `ConfiguracionSizing` extendido), D-110
(implementaciones concretas de A/B/C), y D-111 (ubicación exacta de cada métrica nueva) a diseño
de código, previo a cualquier implementación.
