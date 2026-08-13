# Propuesta — Caso 5C Capa 2 (previo a decidir si se abre, y bajo qué forma)

Estado: **documento de apertura — previo a cualquier decisión, implementación, o análisis del
corpus**. Continúa directamente `AUDITORIA_DIVERSIDAD_TEMPORAL_CASO5C_V1.md`, que dejó pendiente
esta evaluación explícita. No es una fase nueva del ciclo D-N implementada — plantea la decisión que
debe resolverse antes de escribir ningún componente de análisis.

**No se implementa ningún componente de Capa 2 en este documento. No se calcula ninguna
agregación/estadística sobre el corpus. No se elige todavía entre las opciones planteadas.**

---

## 1. Dónde está el proyecto ahora

La cadena de infraestructura está completa y demostrada, con verificación mecánica en cada eslabón,
no solo por diseño:

```
Datos congelados (ValidadorIntegridadDatos, SHA-256)
        ↓
Ejecución reproducible (EjecutorProtocolo, HashCompuesto)
        ↓
Comparación estructurada entre gestores (Caso 5B, ComparadorGestores)
        ↓
Persistencia de evidencia (Caso 5C Capa 1, PersistidorComparaciones)
        ↓
Comparación entre períodos temporales (Sub-campaña D, HashCompuesto distingue el período,
HashConfiguracionEconomica invariante)
```

**Corpus acumulado**: 49 comparaciones (147 corridas individuales) — 6 estrategias × 3 timeframes ×
3 gestores, sobre 2 períodos temporales (2024-2025, 2022-2023) de 1 instrumento (`BTCUSDT`).

**Lo que ya no es la pregunta**: si el sistema puede generar y persistir evidencia comparativa de
forma reproducible y con identidad experimental verificable. Eso está resuelto desde Caso 5C Capa 1
y reafirmado por la Sub-campaña D.

**Lo que sí es la pregunta**: si esa evidencia acumulada es de un tipo y volumen que justifica
construir un componente que la analice — y, si es así, qué nivel de interpretación es legítimo
construir encima sin sobrepasar lo que el corpus realmente sostiene.

---

## 2. La limitación que sigue abierta, sin ambigüedad

```
49 comparaciones
        =
BTCUSDT
        +
2 períodos temporales (2022-2023, 2024-2025)
```

Un solo instrumento. `AUDITORIA_DIVERSIDAD_TEMPORAL_CASO5C_V1.md` §4 ya lo dejó explícito: la
incorporación del segundo período mejora la dimensión temporal, pero no dice nada sobre si los
patrones observados (ej. degeneración económica de `FixedFractional`/`FixedRisk` en timeframes
cortos, perfil de `VolatilitySizing`) son:

- propios de `BTCUSDT` específicamente;
- propios de estos dos períodos particulares (aunque repetirse en ambos es una señal, no una
  prueba de generalidad — 2 puntos no son una serie, ver `AUDITORIA_DIVERSIDAD_TEMPORAL_
  CASO5C_V1.md` §4);
- generalizables a otros instrumentos, sobre los que no existe ningún dato todavía.

Esta limitación es la misma que `AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V2.md` §5 identificó como
irresoluble con más campañas sobre el mismo dataset — y sigue siendo irresoluble con más
comparaciones sobre `BTCUSDT`, sin importar cuántos timeframes o repeticiones se agreguen.

---

## 3. Restricciones ya congeladas que cualquier opción debe respetar

Estas no se reabren aquí — ya están decididas en `DECISIONES_CASO5C_V1.md` y aplican
independientemente de qué opción se elija en esta propuesta:

- **D-118**: selección automática de gestor queda excluida de forma **definitiva y no condicional**
  a la madurez del corpus — es un límite de rol (el sistema es un analista experimental, no un
  decisor operativo), no una cuestión de "cuánta evidencia hay". Ninguna opción de esta propuesta
  puede implementar un recomendador que actúe o elija sin intervención humana.
- **D-119**: sin evidencia suficiente, el sistema **no recomienda** — nunca una recomendación de
  baja confianza sin advertirlo. D-119 nombra explícitamente "diversidad de datasets/escenarios" y
  "diversidad temporal" como dimensiones de suficiencia — la limitación de instrumento único
  (§2 de este documento) es precisamente una de las dimensiones que D-119 ya anticipó como posible
  bloqueo.
- **D-120**: toda recomendación (si алгúna opción llegara a producir una) requiere
  `CriterioUsado` visible + `EvidenciaUsada` declarada + `Limitaciones` explícitas — ninguna
  salida tipo `"Gestor recomendado: X"` sin ese contexto es válida.
- **D-030**: ningún parámetro/umbral se calibra observando el corpus actual — si una opción
  requiere un umbral numérico (ej. mínimo de comparaciones para "suficiente"), debe declararse como
  punto de partida conservador, no como valor derivado de los 49 casos ya generados.

---

## 4. Opciones

### Opción A — Abrir Capa 2 ahora: análisis descriptivo del corpus

Construir un componente que resuma el corpus de 49 comparaciones sin producir ninguna
recomendación: agregaciones (ej. cuántas comparaciones por combinación estrategia/timeframe/
gestor/período), distribución de resultados (ej. rango de `DrawdownMaximoPct` observado por
gestor), estabilidad entre períodos (ej. ¿el mismo patrón aparece en ambos períodos o solo en uno?),
presencia de patrones ya notados en las auditorías (degeneración en timeframes cortos, ausencia de
actividad de `ZScoreReversion`).

**A favor**: la infraestructura de comparación/persistencia está madura; un análisis puramente
descriptivo (sin ranking, sin sugerencia) no cruza la línea de D-118/D-119 — describe lo que el
corpus contiene, no lo que se debería hacer con esa información.

**En contra**: con 1 solo instrumento, cualquier agregación corre el riesgo de leerse como "este es
el comportamiento de estas estrategias/gestores" cuando en realidad es "este es su comportamiento
sobre BTCUSDT en estos 2 períodos". El riesgo no es técnico (D-120 ya exige declarar limitaciones),
es de interpretación por quien lea el resultado — un análisis descriptivo bien construido puede
mitigarlo, pero no lo elimina mientras el corpus tenga un solo instrumento.

### Opción B — Ampliar evidencia antes: nuevo instrumento

Aplicar el mismo patrón ya usado para la diversidad temporal (D-121/D-122/Sub-campaña D) a la
diversidad de instrumento: descargar y congelar un segundo instrumento (ej. `ETHUSDT`), manteniendo
el período original (2024-2025, por la misma razón de atribución causal que D-121 ya fijó: si se
abre Vía A después de Vía B, debe reusar el rango original para no variar 2 dimensiones a la vez) y
la misma matriz experimental (6 estrategias × 3 timeframes × 3 gestores).

**A favor**: cierra la limitación estructural más severa identificada desde `AUDITORIA_
CORPUS_COMPARATIVO_CASO5C_V2.md` §5, con el mismo nivel de disciplina y verificación mecánica ya
aplicado a la diversidad temporal (vista de compatibilidad si aplica, verificación SHA-256,
`ValidadorIntegridadDatos`, P6-equivalente de identidad experimental). Es la vía que D-121 ya dejó
planteada como pendiente ("Vía A, pospuesta no descartada").

**En contra**: pospone la pregunta de qué es Capa 2 una vez más — el proyecto lleva 2 expansiones de
corpus (V2, diversidad temporal) sin construir ningún componente de análisis todavía. No resuelve la
pregunta de fondo (¿qué nivel de interpretación es legítimo?), solo la aplaza con más datos, que es
exactamente el patrón que ya ocurrió una vez (V1 → V2 → diversidad temporal, cada uno como "antes de
auditar/decidir, ampliemos primero").

### Opción C — Capa 2 limitada: consulta/visualización sin inferencia

Construir únicamente una capa de consulta sobre el corpus persistido — "qué ocurrió" y "bajo qué
condiciones ocurrió" (ej. filtrar por estrategia/timeframe/gestor/período y mostrar las métricas
crudas ya persistidas), sin ningún cálculo agregado, sin comparar entre filas, sin ranking. Es
estrictamente más conservador que la Opción A: no agrega, no resume, no compara — solo permite
navegar la evidencia ya persistida de forma más accesible que abrir carpetas de `resultados/`
manualmente.

**A favor**: riesgo de sobre-interpretación mínimo — no hay ninguna síntesis que pueda leerse como
conclusión. Compatible con cualquier volumen o diversidad de corpus, no depende de que el
instrumento único deje de ser una limitación.

**En contra**: aporta poco valor nuevo sobre lo que `PersistidorComparaciones`/
`RenderizadorComparacionGestores` (Caso 5C Capa 1, Caso 5B) ya producen — cada comparación ya se
persiste en formato legible (`COMPARACION_GESTORES_V1.md`). Una capa de consulta sin ninguna
agregación es principalmente una conveniencia de navegación, no un avance de Capa 2 en el sentido en
que D-118/D-119/D-120 la definieron (que sí contemplan agregación de evidencia, solo no
recomendación automática).

---

## 5. Lo que estas 3 opciones tienen en común

Ninguna de las 3 implementa selección/recomendación automática (D-118 ya lo excluye
permanentemente). Ninguna fija todavía un umbral numérico de "suficiencia" sin declarar que es un
punto de partida conservador (D-030). Ninguna opción es mutuamente excluyente de forma permanente:
podría abrirse la Opción C ahora (bajo riesgo) y evaluar A o B después, o abrir A con el corpus
actual y declarar explícitamente sus limitaciones (D-120 ya exige esto de cualquier salida que
raye en recomendación) en vez de esperar a que B cierre la brecha de instrumento.

---

## Fuera de alcance de este documento

No se elige ninguna opción. No se implementa ningún componente de análisis, consulta, ni
recomendación. No se descarga ningún dato nuevo. No se fija ningún umbral numérico de suficiencia de
evidencia (D-119). No se reabre D-118 (selección automática sigue excluida en cualquier escenario).

---

## Próximo documento

Depende de la decisión: si se elige Opción A o C, `ESPECIFICACION_IMPLEMENTACION_CASO5C_CAPA2_V1.md`
(o `_CONSULTA_V1.md` si es C). Si se elige Opción B, un documento equivalente a
`PROPUESTA_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md` pero para instrumento en vez de tiempo — probablemente
`PROPUESTA_DIVERSIDAD_INSTRUMENTO_CASO5C_V1.md`, reutilizando el mismo patrón de D-121/D-122
(exploración de disponibilidad antes de comprometerse a una descarga completa).
