# Propuesta — Caso 6: Recomendador Basado en Evidencia

Estado: **documento de apertura — previo a cualquier decisión, especificación, o implementación**.
Define qué significa "recomendar" en Caso 6, analiza si tiene sentido separar por perfiles de
usuario, evalúa si el corpus actual sostiene una primera versión, y plantea las preguntas
metodológicas que una decisión formal debe resolver antes de tocar código. Sigue el mismo ciclo que
toda fase anterior: propuesta → decisión → especificación → implementación → auditoría →
congelamiento.

**Punto de partida**: `AUDITORIA_INTEGRAL_POST_CASO5C_V1.md` (D-127) — 8/8 áreas aprobadas, sin
hallazgos, cierre de la Fase 0. Esa auditoría estableció que el sistema **puede** sostener una fase
de recomendador; no estableció que **deba** abrirse, ni resolvió nada de su diseño. Esta propuesta
retoma exactamente donde D-118/D-119/D-120 dejaron el marco: resueltas a nivel de principio desde
`DECISIONES_CASO5C_V1.md`, nunca implementadas, condicionadas explícitamente a "una decisión
explícita de una fase posterior" (D-118) y a la existencia de corpus real (D-119).

**No se implementa código en este documento. No se elige ningún perfil. No se decide si el
recomendador se abre.**

---

## 1. Qué significa "recomendar" en esta fase (heredado, no redefinido)

D-118/D-120 ya fijaron esto — esta propuesta no lo redecide, lo hace explícito para que el diseño
posterior no lo reinterprete:

**Recomendar = presentar configuraciones candidatas y sus características observadas, frente a un
objetivo declarado por el usuario, sin elegir por él.** La pregunta que responde el recomendador es
literalmente:

> "Según un objetivo declarado, ¿qué configuraciones existentes presentan características
> compatibles?"

**No es** (prohibido por D-118, sin excepción en esta propuesta):
- "Usar configuración X" (orden ejecutable).
- Ningún término que implique juicio de calidad absoluto: "mejor", "óptimo", "ganador",
  "configuración ideal", "recomendado" sin calificar el criterio. D-120 ya prohíbe la forma
  `"Gestor recomendado: X"` sin los 3 campos de contexto obligatorios.
- Una puntuación compuesta que combine 2+ métricas en un único número — D-118 lo prohíbe
  explícitamente salvo decisión futura aparte, y ninguna decisión de esta naturaleza se toma aquí.

**El formato de salida ya está fijado por D-120, no es una decisión abierta de esta propuesta**:

```
RecomendacionExperimental
{
    Contenido: string           // "Probar GestorX primero" / orden explícito, nunca imperativo absoluto
    CriterioUsado: string       // regla visible, nunca puntuación compuesta opaca
    EvidenciaUsada:
    {
        CantidadComparaciones: int
        CondicionesCubiertas: [ Estrategia, Timeframe, Dataset ][]
    }
    Limitaciones: string        // observacion historica, nunca promesa de comportamiento futuro
}
```

Cualquier diseño posterior debe producir esta estructura o una extensión de ella — no una
alternativa. El "tipo de salida esperada" que pide esta propuesta (§4) se resuelve dentro de este
formato, no al margen de él.

---

## 2. ¿Tiene sentido separar por perfiles de usuario?

**Análisis, no decisión** — la propuesta no asume que los 4 perfiles sugeridos (crecimiento,
preservación de capital, balanceado, personalizado) sean correctos ni necesarios.

### 2.1 — Ventaja técnica real: el corpus ya produce las métricas que un perfil necesitaría

`AnalisisDescriptivo.CalcularDistribucion` (Capa 2, ya congelado) produce
`EstadisticaDescriptiva` (n, mínimo, máximo, media, mediana) sobre 6 métricas ya persistidas por
comparación: `PnLTotal`, `DrawdownMaximoPct`, `ProfitFactor`, `ExposicionMaxima`, `CashFinal`,
`EquityFinal`. Un "perfil" no requeriría ningún cálculo nuevo — sería, en el mejor de los casos,
una forma de **elegir qué subconjunto de estas 6 métricas ya existentes se muestra primero o se
usa como filtro de compatibilidad**, nunca una fórmula nueva que las combine.

**Candidatos de métrica por concepto, sin fijar ninguno todavía**:
- Concepto "priorizar potencial de rendimiento": podría mirar `PnLTotal`/`ProfitFactor` — ambos ya
  existen, ninguno requiere cálculo nuevo.
- Concepto "priorizar control de riesgo": podría mirar `DrawdownMaximoPct`/`ExposicionMaxima` —
  igualmente ya existentes.
- Concepto "equilibrio": aquí está el riesgo metodológico central (ver 2.3) — "equilibrio" entre 2+
  métricas exige, por definición, algún mecanismo de combinación, que es exactamente lo que D-118
  prohíbe como puntuación compuesta opaca.
- Concepto "personalizado": no introduce ninguna métrica nueva — es, en el mejor caso, una forma de
  que el usuario elija cuál(es) de las 6 métricas ya existentes usar como criterio explícito
  (`CriterioUsado`, ya un campo obligatorio de D-120).

### 2.2 — Ventaja de comunicación

Un perfil, si se implementa bien, no cambia qué datos existen — cambia **qué subconjunto de
evidencia ya existente se muestra primero y bajo qué etiqueta** al usuario. Esto podría hacer la
salida más legible sin violar D-118, siempre que:
- El perfil sea una forma de **filtrar/ordenar por un criterio explícito ya declarado** (mismo
  criterio ya permitido por D-118 para "ordenar por criterio explícito"), no una fórmula nueva.
- El nombre del perfil no sustituya la declaración del criterio real — "preservación de capital"
  no puede ser una caja negra; debe traducirse siempre a algo tan explícito como
  `CriterioUsado: "DrawdownMaximoPct ascendente"`.

### 2.3 — Riesgo metodológico central: un "modo" puede esconder una decisión subjetiva

Este es el punto que la propuesta debe tratar con más cuidado, porque es donde un perfil deja de
ser una forma de presentación y se convierte en una selección disfrazada:

- **"Balanceado" es el caso más peligroso**: cualquier noción de equilibrio entre 2+ métricas
  (ej. "buen PnL con drawdown moderado") requiere, explícita o implícitamente, una función que las
  combine — pesos, umbrales, o un orden de prioridad. Esa función **es** una puntuación compuesta,
  prohibida por D-118 salvo decisión futura aparte. Ofrecer un perfil "balanceado" sin resolver
  primero, como decisión explícita y visible, cuál es exactamente esa función, sería introducir
  selección automática por la puerta trasera de un nombre inocuo.
- **Cualquier perfil predefinido por el sistema (no declarado por el usuario en cada consulta)
  fija implícitamente qué es "bueno"** — incluso "priorizar rendimiento" es una preferencia de
  valor, no un hecho técnico. D-118 exige que el sistema nunca decida por el usuario; un catálogo
  cerrado de perfiles con nombres pre-cargados de juicio ("crecimiento", "preservación") empieza a
  decidir *qué preferencias son las válidas a ofrecer*, lo cual es distinto de solo responder a lo
  que el usuario declare.
- **Mitigación posible, no decidida aquí**: si se abren perfiles, cada uno debería ser
  transparente por diseño — equivalente a una plantilla de `CriterioUsado` explícito y de una sola
  métrica (o un orden lexicográfico de métricas ya declarado, nunca un peso numérico oculto). El
  perfil "personalizado" (usuario elige la métrica) sería el más seguro de los 4 precisamente
  porque no incorpora ningún juicio del sistema — solo expone el mecanismo ya permitido de
  "criterio explícito, una métrica declarada, sin combinación".

### 2.4 — Conclusión de este análisis (no una decisión)

Separar por perfiles **tiene sentido solo si cada perfil es una plantilla de criterio explícito
(D-118) sobre métricas ya existentes**, nunca una fórmula de combinación nueva. El perfil
"balanceado", tal como se sugiere conceptualmente, no puede implementarse sin antes resolver como
decisión aparte y explícita qué significa "equilibrio" en términos de una regla visible — de lo
contrario debe descartarse o reformularse como "el usuario elige el orden de 2+ criterios
explícitos, presentados por separado, nunca combinados en un número". Esta pregunta queda abierta
para D-128, no resuelta aquí.

---

## 3. ¿El corpus actual es suficiente para una primera versión?

**67 comparaciones, 2 instrumentos (`BTCUSDT` 49, `ETHUSDT` 18), 3 datasets con matriz completa (18
combinaciones cada uno), 6 estrategias, 3 gestores, 3 timeframes.**

**A favor de suficiencia para una primera versión limitada**:
- D-119 ya no exige un umbral numérico fijo en esta fase — exige que el sistema declare
  explícitamente cuándo no hay evidencia suficiente y se abstenga, no que el corpus alcance un
  tamaño mínimo predeterminado. Un recomendador que **a veces no recomienda** (por ejemplo, para
  combinaciones estrategia/timeframe con solo 1 comparación disponible) es una salida válida bajo
  D-119, no un fallo de esta fase.
- Las 6 métricas necesarias (§2.1) ya están completas y verificadas para las 67 comparaciones —
  no hay ningún dato faltante que bloquee un cálculo de distribución.

**Limitaciones que deben declararse, no resolverse aquí**:
- **Cobertura desigual por combinación**: algunas combinaciones estrategia/timeframe/gestor tienen
  solo 1 fila por dataset (la mayoría), otras (Tres Mosqueteros/Ema Cross en `BTCUSDT` 2024-2025)
  tienen 3 por repetición deliberada de reproducibilidad — cualquier `CantidadComparaciones` en
  `EvidenciaUsada` debe ser honesto sobre esta asimetría, no presentarla como si N comparaciones
  independientes respaldaran cada recomendación por igual.
- **Solo 2 instrumentos, 2 rangos temporales**: cualquier recomendación debe declarar sobre qué
  subconjunto exacto del corpus se basa (mismo principio ya aplicado en Capa 2/interpretativo:
  "ningún patrón se extiende a instrumentos no representados").
- **ZScore Reversion sin actividad en todo el corpus**: 27 filas con `PnLTotal=0` — un
  recomendador ingenuo podría, sin declaración de limitación, presentar esto como "riesgo cero"
  cuando en realidad es ausencia de operación, no evidencia de bajo riesgo. D-120 exige que
  `Limitaciones` cubra explícitamente este tipo de caso.
- **Ningún patrón fue validado fuera de muestra**: todas las 67 comparaciones son backtests sobre
  datos históricos ya usados también para análisis descriptivo/interpretativo — no hay separación
  entrenamiento/validación. Esto no bloquea una primera versión (el recomendador no ajusta
  parámetros, D-030), pero sí limita qué tan fuerte puede ser cualquier `CriterioUsado` — debe
  leerse como "esto es lo que se observó", nunca como "esto es lo que funcionará".

**Conclusión de este análisis**: el corpus es suficiente para una primera versión **limitada y
honesta sobre sus huecos** — no para una recomendación con alta confianza declarada. Corresponde a
D-119 decidir, en la especificación posterior, el criterio operativo exacto de "suficiente" por
combinación (ej. mínimo de filas, o presencia en al menos 1 dataset por instrumento).

---

## 4. Tipo de salida esperada (ejemplo conceptual, dentro del formato ya fijado por D-120)

```
RecomendacionExperimental
{
    Contenido: "Bajo el criterio declarado (DrawdownMaximoPct ascendente), 3 configuraciones
                del corpus muestran los valores mas bajos observados"
    CriterioUsado: "DrawdownMaximoPct ascendente, sin combinar con otras metricas"
    EvidenciaUsada:
    {
        CantidadComparaciones: 12
        CondicionesCubiertas: [
            (Volumen Breakout, 15m, BTCUSDT_2024-01-02_2025-01-02),
            (Volumen Breakout, 1h, ETHUSDT_2024-01-02_2025-01-02),
            ...
        ]
    }
    Limitaciones: "Observacion historica sobre backtest, no proyeccion de comportamiento futuro.
                   Corpus limitado a BTCUSDT/ETHUSDT, 2024-2025 y 2022-2023. Volumen Breakout
                   presente en 12 de 18 combinaciones posibles del corpus."
}
```

**Nunca**: `"Usar Volumen Breakout con Fixed Fractional en 1h"` sin los 3 campos de contexto —
prohibido por D-120, no una omisión aceptable de esta fase.

---

## 5. Separación respecto a capas adyacentes (confirmada, no reabierta)

```
Recomendador (esta fase, si se abre)
    "Segun un objetivo declarado, ¿que configuraciones existentes
     presentan caracteristicas compatibles?"
        |
        v  [fuera de alcance de Caso 6 Fase 1]
Selector automatico de configuracion
    "¿Que configuracion debe elegir el sistema?"
        |
        v  [fuera de alcance de Caso 6 Fase 1]
Optimizacion automatica
    "¿Que nuevos parametros deberia buscar el sistema?"
```

**El recomendador opera exclusivamente sobre configuraciones ya existentes en el corpus** —
combinaciones estrategia/timeframe/gestor/dataset ya ejecutadas y persistidas. No genera ninguna
configuración nueva, no prueba ningún parámetro no ya presente en el corpus (eso sería
optimización, explícitamente fuera). No elige ni ejecuta nada (eso sería selección automática,
explícitamente fuera, D-118).

---

## 6. Preguntas metodológicas para D-128 (no resueltas aquí)

1. **¿El recomendador trabaja sobre configuraciones existentes o puede crear nuevas?** — el
   análisis de §5 sugiere que debe limitarse a las ya existentes en esta primera fase (consistente
   con D-118/D-030), pero la decisión formal corresponde a D-128.
2. **¿Los perfiles de usuario son necesarios?** — §2 concluye que solo tendrían sentido como
   plantillas de criterio explícito de una sola métrica (o de un orden declarado de varias, nunca
   combinadas), y que "balanceado" en particular no puede implementarse sin resolver antes qué
   significa exactamente "equilibrio" como regla visible. D-128 debe decidir si se abren perfiles
   en esta primera versión, cuáles, y con qué definición exacta de cada uno.
3. **¿Cómo se evita convertir preferencias humanas en reglas ocultas?** — el criterio identificado
   en §2.3: cada perfil (si existe) debe declarar su `CriterioUsado` de forma tan explícita como si
   no existiera el nombre del perfil — el nombre es una etiqueta de UI, nunca una fuente adicional
   de lógica no visible.
4. **¿Cómo se comunica incertidumbre?** — ya resuelto en el marco (D-119: no recomendar sin
   evidencia suficiente, nunca una recomendación débil silenciosa; D-120: `Limitaciones` obligatorio
   y no genérico). D-128 debe fijar el umbral operativo exacto de "suficiente" por combinación.
5. **Qué queda permitido y qué queda prohibido** — a fijar explícitamente en D-128, heredando sin
   excepción D-118 (sin selección automática), D-119 (sin recomendación de baja confianza
   silenciosa), D-120 (formato obligatorio con los 4 campos), D-030 (sin calibración de parámetros
   observando resultados).

---

## 7. Restricciones heredadas

- **D-118**: selección automática excluida por rol, no por madurez — esta fase no la reabre.
- **D-119**: sin evidencia suficiente, no se recomienda — nunca un punto intermedio silencioso.
- **D-120**: toda recomendación incluye `Contenido`/`CriterioUsado`/`EvidenciaUsada`/`Limitaciones`
  — estructura ya fijada, no rediseñada aquí.
- **D-127**: esta propuesta no sustituye ni repite la auditoría integral ya cerrada — la toma como
  condición ya satisfecha, no como algo que deba re-verificarse en esta fase.
- **D-030**: ningún parámetro de estrategia/gestor se calibra ni ajusta — el recomendador lee
  evidencia ya generada, no ejecuta backtests nuevos ni ajusta configuraciones.
- **D-014/D-025/D-026/D-047/D-076**: ninguna comparación colapsa a un ganador único — extendido
  aquí explícitamente a cualquier `CriterioUsado` que combine métricas.

---

## Fuera de alcance de este documento

No se elige ningún perfil. No se decide si el recomendador se abre. No se diseña ningún algoritmo
de filtrado/ordenamiento. No se especifica ningún tipo ni método de código. No se resuelve el
umbral operativo de "evidencia suficiente" (D-119) — queda para D-128 o para la especificación
posterior. No se implementa nada.

---

## Próximo documento

Si esta propuesta se aprueba: `DECISIONES_CASO6_RECOMENDADOR_V1.md` (candidata **D-128**),
resolviendo las 5 preguntas de §6 — en particular si se abren perfiles y con qué definición exacta,
y el umbral operativo de suficiencia de evidencia por combinación. Después,
`ESPECIFICACION_IMPLEMENTACION_RECOMENDADOR_CASO6_V1.md`, traduciendo D-128 a diseño de código
(reutilizando `LectorCorpus`/`AnalisisDescriptivo` por referencia, mismo patrón ya usado 2 veces en
Caso 5C), antes de cualquier implementación.
