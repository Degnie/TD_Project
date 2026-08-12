# Decisiones — Caso 5C: Capa de Análisis y Recomendación Experimental

Estado: **D-116 a D-120 resueltas**. Misma estructura usada en D-001 a D-115 (decisión, opciones,
criterio, evidencia, resolución). Ningún código se modifica en este documento — las resoluciones
aquí registradas habilitan la especificación de implementación siguiente, no la reemplazan.

Contexto completo en `PROPUESTA_CASO5C_V1.md`. Verificación contra código existente, no
reconstruida de memoria (mismo criterio que abrió Caso 2/Caso 3/Caso 4/Caso 5A/Caso 5B, D-057).

**Esta ronda de decisiones cubre exclusivamente la Capa 1 (persistencia de evidencia) y el marco
de la Capa 2 (semántica de recomendación)** — no implementa la Capa 2, solo fija sus límites. Mismo
criterio que la propuesta ya declaró en §3: acumular evidencia es independiente de razonar sobre
ella, y esta ronda resuelve la primera capa completa antes de tocar la segunda.

---

## D-116 — Mecanismo de persistencia de evidencia (Capa 1)

**Estado**: 🟢 Aprobada.

**Decisión**: cómo y dónde se persiste cada `ResultadoComparativoGestores` que se ejecute, y si
ese mecanismo vive dentro de `ComparadorGestores` (Caso 5B) o en un componente nuevo que lo envuelve
(Caso 5C).

### Resolución adoptada

**Extensión directa del patrón ya verificado en `protocolo/resultados/`** — no se inventa un
mecanismo nuevo. Carpeta timestamped por comparación ejecutada
(`caso5/resultados/{Estrategia}_{Timeframe}_{timestamp}/`), con:
- `IDENTIDAD_COMPARACION.json` — identidad completa de la comparación (estrategia, dataset,
  timeframe, y por cada gestor: su `IdentidadGestor` vía `IIdentidadGestorRiesgo`, mismo criterio
  de identidad reproducible que D-109 ya estableció).
- `COMPARACION_GESTORES_V1.md` — la tabla generada por `RenderizadorComparacionGestores.Generar`
  (Caso 5B), sin modificación de ese componente.

**La persistencia NO vive dentro de `ComparadorGestores.cs`** (Caso 5B no se reabre) — vive en un
componente nuevo de Caso 5C, `PersistidorComparaciones`, que recibe un `ResultadoComparativoGestores`
ya calculado y lo escribe a disco. Verificado contra código
(`protocolo/Program.cs:8,51,53,56,69`): el patrón de persistencia en todo el proyecto vive siempre
en la capa de orquestación/ejecutable (`Program.cs` de cada módulo), nunca dentro del componente de
cálculo puro — `ComparadorGestores.Comparar` sigue siendo una función pura que no toca disco, mismo
principio que ya se aplicó al separar `CalculadoraMetricasFinancieras`/`ReporteFinancieroGenerador`.

**Excluido de git**: `caso5/resultados/` — evidencia regenerable, mismo criterio ya aplicado a
`protocolo/resultados/` (`.gitignore`) y a `validacion_integral/datasets_generados/`.

### Restricciones que aplican

- `ComparadorGestores.cs` (Caso 5B, congelado) no se modifica — `PersistidorComparaciones` es un
  componente que lo envuelve, no una extensión de su contrato.
- La persistencia nunca falla silenciosamente ni bloquea la comparación en memoria — si escribir a
  disco falla, el `ResultadoComparativoGestores` ya calculado sigue siendo válido y utilizable en
  memoria (mismo principio D-059/D-096 de nunca convertir una falla de una capa secundaria en un
  bloqueo de la capa principal).

### Evidencia

- `protocolo/Program.cs:8,51,53,56,69`: patrón de persistencia verificado — carpeta timestamped,
  `Directory.CreateDirectory`, `File.WriteAllText` por artefacto, todo en la capa de ejecutable.
- `.gitignore:5` (`exploration/laboratorio/protocolo/resultados/`): precedente de exclusión de
  evidencia regenerable.

---

## D-117 — Información válida como insumo de análisis

**Estado**: 🟢 Aprobada.

**Decisión**: qué campos del corpus acumulado (una vez que D-116 permite que exista) son insumo
válido para cualquier análisis futuro de Capa 2.

### Resolución adoptada

**Insumo válido, exclusivamente derivado de lo que `IDENTIDAD_COMPARACION.json` ya persiste (D-116)
y de `MetricasFinancieras` (D-114, heredada de Caso 5B)**:
- Estrategia, timeframe, identidad de dataset (hash).
- Identidad de cada gestor comparado (`IIdentidadGestorRiesgo.ObtenerIdentidadConfiguracion()`).
- Métricas financieras de cada fila (`PnLTotal`, `DrawdownMaximoPct`, `ProfitFactor`,
  `ExposicionMaxima`, `CashFinal`, `EquityFinal` — mismos campos que D-114 ya fijó como fuente
  única).
- Estado de la corrida (`Success`/`Failed`/`Incomplete`) — una comparación con corridas fallidas es
  insumo parcial válido, no se descarta completa (mismo criterio que D-114/P7 de Caso 5B).

**Explícitamente NO es insumo válido**:
- Régimen de mercado inferido (`ClasificadorRegimenV1` o cualquier clasificador) — ninguna
  comparación de gestores se etiqueta ni se filtra por régimen en esta fase; mezclar ambos ejes es
  una ampliación de alcance no autorizada aquí.
- Cualquier dato no ya presente en la identidad experimental o en `MetricasFinancieras` — no se
  deriva ni infiere ningún campo nuevo sobre el corpus (D-072/D-077 extendido a esta capa: ningún
  análisis futuro recalcula lo que el motor ya calculó).
- Ninguna fuente externa al propio corpus acumulado por este mismo sistema (sin datos de mercado
  externos, sin fuentes de terceros).

### Restricciones que aplican

- Esta decisión fija el **insumo permitido**, no implementa ningún análisis — la Capa 2 (D-118 a
  D-120) define qué se hace con este insumo, no se resuelve aquí.

### Evidencia

- `DECISIONES_CASO5B_V1.md`, D-114: fuente única ya establecida (`MetricasFinancieras`, exclusión
  de `ReporteOperacional`) — D-117 hereda esa misma fuente sin reabrirla.
- `PROPUESTA_CASO5C_V1.md` §1: exclusión ya declarada de régimen de mercado/predicción como insumo.

---

## D-118 — Semántica de "recomendar"

**Estado**: 🟢 Aprobada.

**Decisión**: cuál de las opciones no descartadas de `PROPUESTA_CASO5C_V1.md` §1 define qué hace
Caso 5C cuando produce una recomendación.

### Resolución adoptada

**Selección automática queda excluida de forma definitiva de Caso 5C** — no por complejidad de
implementación, sino porque cambia el rol del sistema: hasta Caso 5B, el sistema es un analista
experimental (ejecuta, compara, presenta evidencia); permitir que el sistema elija y aplique un
gestor sin intervención humana lo convierte en un decisor operativo — un salto que requiere
evidencia y controles muy superiores a los que esta fase puede establecer. Esta exclusión no es
condicional a que "todavía no hay suficiente evidencia" — es un límite de rol, no un límite de
madurez del corpus. Reabrirla en el futuro requeriría una decisión explícita de una fase posterior
que declare por qué el rol del sistema debe cambiar, no una acumulación gradual de evidencia dentro
de Caso 5C.

**Quedan vivas, conviviendo como salidas distintas del mismo componente de análisis**:
- **Sugerir candidatos**: presenta 1 o más gestores como punto de partida razonable para probar,
  sin descartar los demás ni implicar que los no sugeridos son inferiores.
- **Ordenar por criterio explícito y declarado**: un orden derivado de una regla visible y
  nombrada (ej. "ordenado por menor `DrawdownMaximoPct` promedio observado") — el criterio de
  ordenamiento es siempre parte visible de la salida, nunca una función de puntuación opaca.

Ambas formas deben cumplir D-120 (declaración obligatoria de evidencia) — ninguna de las dos es una
recomendación válida sin ese contexto adjunto.

### Restricciones que aplican

- Ningún componente de Caso 5C aplica un gestor a una corrida real — toda salida es información
  para que un humano decida, nunca una acción automática (heredado de `PROPUESTA_CASO5C_V1.md` §6,
  ya reafirmado aquí como decisión formal).
- "Ordenar por criterio explícito" no puede combinar más de una métrica en un único número (eso
  sería una puntuación compuesta, no un criterio explícito) salvo que una decisión futura lo
  autorice explícitamente — mismo principio D-014/D-025/D-026/D-047/D-076 ya aplicado a toda
  comparación de timeframe/régimen en fases anteriores.

### Evidencia

- `PROPUESTA_CASO5C_V1.md` §1: las 3 opciones originales, con selección automática ya marcada como
  "descartada de entrada por el auditor".
- Precisión explícita del auditor en la revisión de la propuesta: la exclusión es sobre rol del
  sistema, no sobre complejidad — incorporada aquí como parte del razonamiento de la decisión, no
  solo como nota externa.

---

## D-119 — Umbral de suficiencia de evidencia

**Estado**: 🟢 Aprobada (principio; valores numéricos diferidos).

**Decisión**: qué hace el sistema cuando el corpus acumulado (D-116) no es suficientemente diverso
para sostener una recomendación.

### Resolución adoptada

**Regla central, sin excepciones**: sin evidencia suficiente, el sistema **no recomienda** — nunca
produce una recomendación de baja confianza sin advertirlo. La ausencia de recomendación es una
salida válida y esperada, no un caso de error.

**Dimensiones que la suficiencia debe considerar** (sin fijar valores numéricos en esta decisión,
diferido a la especificación de implementación o a una decisión posterior si se requiere calibrar
con criterio externo, D-030):
- Número mínimo de comparaciones acumuladas para la combinación estrategia/timeframe/gestor en
  cuestión.
- Diversidad de estrategias cubiertas.
- Diversidad de datasets/escenarios cubiertos — evita que una sola condición de mercado
  (ej. solo tendencia alcista) se presente como evidencia general.
- Diversidad temporal — evita que comparaciones todas ejecutadas en una ventana estrecha se lean
  como una tendencia robusta.
- Consistencia del resultado observado entre las comparaciones acumuladas — si el mismo gestor
  gana en unas condiciones y pierde en otras sin patrón aparente, eso es información relevante que
  debe impedir una recomendación fuerte, no ocultarse.

**Por qué no fijar valores aquí**: hacerlo ahora, sin ningún corpus real todavía acumulado (D-116
recién habilita que exista), sería inventar un umbral sin evidencia sobre la que calibrarlo — mismo
riesgo que D-030 ya previene para cualquier parámetro sin referencia externa. La especificación de
implementación puede proponer un valor inicial conservador, explícitamente marcado como punto de
partida ajustable, no como resultado final de esta decisión.

### Restricciones que aplican

- Ningún mecanismo de "recomendación con advertencia de baja confianza" sustituye esta regla —
  la opción binaria es recomendar (con evidencia declarada, D-120) o no recomendar, nunca un punto
  intermedio silencioso.
- El criterio de suficiencia debe ser visible en la salida cuando el sistema decide no recomendar —
  debe declarar qué faltó (ej. "solo 2 comparaciones disponibles, mínimo requerido: N"), no solo
  negarse sin explicación.

### Evidencia

- Precisión explícita del auditor en la revisión de la propuesta: "sin evidencia suficiente → no
  recomendar", nunca "poca evidencia → recomendación débil silenciosa" — incorporada aquí como
  regla central, no como nota adicional.
- D-010 (Caso 1): precedente de exigir tamaño de muestra obligatorio en toda comparación, ahora
  extendido a una precondición de ejecución, no solo de presentación.

---

## D-120 — Formato de la recomendación y su declaración de evidencia

**Estado**: 🟢 Aprobada.

**Decisión**: qué campos son obligatorios en cualquier salida de Capa 2 que constituya una
recomendación (sugerencia u orden explícito, D-118).

### Resolución adoptada

Toda recomendación (cuando D-119 determina que hay evidencia suficiente para emitirla) debe
incluir, como estructura mínima:

```
RecomendacionExperimental
{
    Contenido: string           // "Probar GestorX primero" / orden explícito de gestores
    CriterioUsado: string       // regla visible, nunca "puntuación compuesta" opaca (D-118)
    EvidenciaUsada:
    {
        CantidadComparaciones: int
        CondicionesCubiertas: [ Estrategia, Timeframe, Dataset ][]   // lista, no solo un conteo
    }
    Limitaciones: string        // declaracion explicita de que es observacion historica,
                                 // nunca promesa de comportamiento futuro (D-016 extendido)
}
```

**Nunca válido**: `"Gestor recomendado: X"` sin ninguno de los 3 campos de contexto — esa forma
queda explícitamente prohibida como salida de Caso 5C, mismo criterio que el ejemplo de §2 de la
propuesta ("Fixed Risk ganó en BTCUSDT 1H 2024" vs. "bajo N condiciones mostró este perfil").

**`Limitaciones` es un campo obligatorio, no opcional**: toda recomendación declara explícitamente
que es una observación histórica sobre condiciones ya ejecutadas, nunca una garantía sobre
comportamiento futuro de mercado — mismo límite que D-016 ya exige para el clasificador de régimen,
extendido aquí de forma textual y obligatoria en cada salida, no solo como principio de diseño.

### Restricciones que aplican

- Ningún consumidor de `RecomendacionExperimental` puede mostrar `Contenido` sin
  `EvidenciaUsada`/`Limitaciones` adjuntos — la estructura es indivisible en cualquier
  presentación (texto, tabla, o cualquier formato futuro).

### Evidencia

- `PROPUESTA_CASO5C_V1.md` §2: ejemplo explícito de recomendación inválida vs. válida, origen
  textual de esta decisión.
- Precisión del auditor en la revisión: "Recomendación + Evidencia usada + Limitaciones. Nunca:
  Gestor recomendado: X sin contexto" — incorporada aquí como estructura obligatoria, no como
  sugerencia.

---

## Fuera de alcance de este documento

No se implementó código. No se modifica ningún módulo de `src/` ni `ComparadorGestores.cs` (Caso
5B, congelado). D-116 a D-120 quedan resueltas a nivel de diseño — D-116/D-117 son suficientes
para implementar la Capa 1 completa (persistencia); D-118/D-119/D-120 fijan el marco de la Capa 2
sin implementarla — ningún análisis ni recomendación se construye en este documento. La
especificación de implementación siguiente puede cubrir solo Capa 1, dejando Capa 2 para una
sub-fase posterior explícita, dado que ambas capas son independientes (`PROPUESTA_CASO5C_V1.md` §3).

---

## Próximo documento

`ESPECIFICACION_IMPLEMENTACION_PERSISTENCIA_EVIDENCIA_V1.md` (o nombre equivalente), traduciendo
D-116 (`PersistidorComparaciones`, formato exacto de `IDENTIDAD_COMPARACION.json`) y D-117 (qué
campos exactos persiste) a diseño de código — cubriendo únicamente Capa 1. La especificación de
Capa 2 (análisis/recomendación, D-118 a D-120) queda para un documento posterior, una vez exista
corpus real acumulado sobre el cual verificar el diseño de la recomendación, no antes.
