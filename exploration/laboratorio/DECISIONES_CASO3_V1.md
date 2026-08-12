# Decisiones — Caso 3: Generalización Experimental

Estado: **D-086 a D-090 resueltas e implementadas — Caso 3A cerrado en `caso3/AUDITORIA_CASO3A_
V1.md`**. Misma estructura usada en
D-001 a D-085 (decisión, opciones, criterio, evidencia). Ningún código se modifica en este
documento — las resoluciones aquí registradas habilitan la especificación de implementación
siguiente, no la reemplazan.

Contexto completo en `PROPUESTA_CASO3_V1.md`. Verificación contra código existente, no
reconstruida de memoria (mismo criterio que abrió Caso 2, D-057).

---

## D-086 — Alcance exacto de generalización de Caso 3A

**Estado**: 🟢 Aprobada e implementada — 2/2 familias (Z-Score Reversal, Estrategia Neutral).
**Selección: B — dos familias nuevas mínimo.**

**Decisión**: ¿qué debe demostrar el Caso 3 para considerar "generalizado" el laboratorio — una
sola familia estructuralmente distinta, o varias?

**Motivo de la selección**: una sola familia puede ser un caso aislado; cuatro familias
convertirían la fase en una campaña de investigación en vez de una validación de plataforma
(contrario al objetivo declarado en `PROPUESTA_CASO3_V1.md` §1: validar capacidad del laboratorio,
no construir un catálogo de estrategias). Dos familias permiten demostrar generalización real sin
comprometer un alcance desproporcionado.

**Evidencia**: EMA Cross (Fase 1.6-D, D-054) ya probó 1 familia distinta de las 2 originales
(Tres Mosqueteros/MHI, ambas "Patrón con martingala") — la familia "Tendencia sin martingala".
`PROPUESTA_CASO3_V1.md` §6 presenta 4 familias candidatas (A-Tendencia ya hecha, B-Reversión,
C-Estadística, D-Neutral), sin seleccionar ninguna.

**Opciones**:
- **A — Una familia nueva es suficiente**: integrar 1 candidato de B/C/D confirma o refuta la
  hipótesis con el mínimo esfuerzo — si generaliza, sirve de segundo punto de evidencia (además de
  EMA Cross); si no generaliza, el hallazgo aparece igual de rápido.
- **B — Dos familias nuevas mínimo**: una sola integración adicional podría generalizar "por
  casualidad" (ej. compartir una propiedad estructural con EMA Cross sin saberlo) — dos familias
  con estructuras distintas entre sí dan mayor confianza de que la plataforma generaliza, no solo
  que acepta un segundo caso parecido al primero.
- **C — Las 4 familias candidatas**: máxima cobertura, pero contradice el ritmo incremental usado
  en Caso 1/Caso 2 (cada Caso avanzó en sub-fases pequeñas, nunca todo de una vez).

**Criterio a aplicar**: la selección no debe comprometer una cantidad fija de código por adelantado
— debe permitir cerrar Caso 3A tras la primera integración si el resultado ya responde la pregunta
de la sección 1 de `PROPUESTA_CASO3_V1.md` con claridad, o continuar si aparecen supuestos ocultos
que ameriten una segunda prueba.

---

## D-087 — Criterio para seleccionar la primera familia nueva

**Estado**: 🟢 Aprobada. **Selección: A — máxima distancia estructural con lo ya probado.**

**Decisión**: ¿con qué criterio se elige, entre Reversión (B), Estadística (C) o Neutral (D), cuál
se integra primero?

**Motivo de la selección**: EMA Cross ya cubrió tendencia, sin martingala, sin cuadrantes — la
siguiente prueba debe maximizar información nueva, no repetir un perfil ya validado. La familia
elegida (y toda familia subsiguiente, dado D-086=2 familias) debe diferir de los supuestos ya
cubiertos por las 3 estrategias existentes: no color de vela puntual, no cuadrantes fijos, no
martingala — mismo criterio que motivó elegir EMA Cross sobre una segunda estrategia de patrón en
Fase 1.6-D.

**Evidencia**: las 3 familias no comparten ningún supuesto estructural entre sí ni con EMA Cross
de forma obvia — Reversión opera sobre extremos de precio, Estadística sobre propiedades de la
serie (no visuales), Neutral no reacciona al mercado en absoluto.

**Opciones**:
- **A — Máxima distancia estructural con lo ya probado**: elegir la familia que comparta *menos*
  supuestos con las 3 estrategias existentes (Tres Mosqueteros/MHI/EMA Cross) — maximiza la
  probabilidad de exponer un supuesto oculto del pipeline en el primer intento.
- **B — Menor complejidad de implementación primero**: elegir la familia más simple de codificar
  (candidato natural: D-Neutral, señal fija o aleatoria) para validar el mecanismo de integración
  con el menor riesgo de introducir un bug de la estrategia misma que se confunda con un hallazgo
  del pipeline.
- **C — Relevancia para D-055**: elegir la familia que más presione el catálogo de métricas
  dependientes de martingala (cualquiera de B/C/D, todas sin martingala) — prioriza resolver la
  deuda técnica ya identificada sobre explorar estructura nueva por sí misma.

**Criterio a aplicar**: la selección debe declarar explícitamente qué supuesto del pipeline se
espera poner a prueba con la familia elegida — no elegir "porque es la más simple" sin more razón,
ni "porque es interesante" sin conexión a un criterio de éxito de `PROPUESTA_CASO3_V1.md` §5.

---

## D-088 — Tratamiento de métricas cuando una estrategia no tiene martingala

**Estado**: 🟢 Aprobada. **Selección: A — marcar "no aplica" explícitamente, resuelta mediante una
tercera dirección: metadata declarativa externa a `IStrategy` (ni autodeclaración en la interfaz de
ejecución, ni inferencia desde resultados observados).**

**Decisión**: ¿cómo debe presentarse `ResolucionDeIntentos` (`GanoInicial`/`GanoM1`/`GanoM2`/
`PctResueltasPorMartingala`, `analisis_operacional/AnalizadorOperacional.cs:21-26`) para una
estrategia sin martingala?

**Motivo de la selección — por qué no las 2 sub-opciones originales**:
- **Inferir desde datos producidos** (Opción 2 evaluada): rechazada como mecanismo definitivo,
  aunque útil como diagnóstico. `MartingalasUsadas == 0` en toda una corrida no distingue "la
  estrategia no usa martingala" de "la estrategia usa martingala pero nunca tuvo que aplicarla en
  esa corrida particular" (ej. dataset muy favorable). Inferir aplicabilidad solo desde resultados
  mezclaría dos conceptos distintos — capacidad estructural de la estrategia vs. comportamiento
  observado en una ejecución concreta — produciendo una clasificación dependiente del dataset,
  contrario al objetivo de D-088 (aplicabilidad debe ser una propiedad de la estrategia, no de la
  corrida).
- **Extender `IStrategy` con `UsaMartingala: bool`**: rechazada — `IStrategy` debe mantenerse como
  contrato mínimo de ejecución (`PROPUESTA_CASO3_V1.md` §2), no un contenedor de propiedades
  analíticas. Una estrategia debe poder ejecutar sin declarar información adicional sobre su lógica
  interna.

**Resolución adoptada**: crear una metadata experimental separada del contrato `IStrategy`, ubicada
en el catálogo experimental (no en la interfaz de ejecución) — ej. conceptualmente
`CaracteristicasEstrategia { UsaMartingala, UsaSizingPropio, UsaEstadoInternoPersistente, ... }`.
Los resultados (`InfoOperacionResuelta`) responden "qué ocurrió"; la metadata responde "qué puede
hacer la estrategia" — son capas diferentes, y D-088 exige mantenerlas separadas.

**Aplicación para Caso 3**: las métricas que dependan de una capacidad específica (ej. martingala)
deben distinguir entre valor observado y capacidad aplicable. La aplicabilidad viene de la metadata
declarativa externa, no de los resultados ni de `IStrategy`. Esto probablemente activa D-055
(existe ahora un mecanismo concreto para resolverlo), pero no requiere rehacer Caso 1 — las
fórmulas existentes (`GanoInicial`/`GanoM1`/`GanoM2`/`PctResueltasPorMartingala`) no cambian, solo
se decide cómo se presentan cuando la metadata indica "no aplica".

**Nueva decisión derivada**: la ubicación exacta de esa metadata (catálogo `.md`, archivo
estructurado paralelo, o clase de laboratorio) no se resuelve aquí — ver D-090.

**Evidencia verificada en código**: los 4 campos ya están en un record separado
(`ResolucionDeIntentos`) de `ResultadoGeneral` (`IntentosCompletados`/`Victorias`/`Derrotas`/
`EficienciaOperacionalPct`, universal) — la separación estructural que D-055 propone ya existe
parcialmente a nivel de tipo. El problema no es falta de dato: `InfoOperacionResuelta.
MartingalasUsadas` siempre existe (`EstrategiaEmaCross.cs` lo fija en `0` explícitamente, D-055 ya
lo documentó como hallazgo, no como bug). El problema es de **interpretación**: `GanoM1=0` para
Tres Mosqueteros es un dato real (0 operaciones se resolvieron en la primera martingala), mientras
que `GanoM1=0` para EMA Cross significa "el concepto no aplica" — ambos casos producen el mismo
valor numérico sin distinguirse en el reporte actual.

**Opciones**:
- **A — Marcar "no aplica" explícitamente**: agregar un indicador (ej. `UsaMartingala: bool` en la
  estrategia, o inferido de que todo `MartingalasUsadas` observado es `0`) que el reporte lea para
  mostrar "N/A" en vez de "0.0%" cuando la estrategia no tiene martingala — mismo principio D-078
  (Caso 2: `null` ≠ `0`, "no disponible" es un estado distinto de "cero real").
  Requiere decidir si el indicador es una propiedad explícita de `IStrategy` o una inferencia sobre
  los datos ya producidos.
- **B — Documentar la limitación sin cambiar código**: mantener el catálogo tal cual, con una nota
  en el reporte generado ("estas métricas asumen martingala; para estrategias sin reintentos,
  interprete 0% como 'no aplica'") — cero cambio de código, resuelve la ambigüedad solo a nivel de
  lectura humana, no de dato estructurado.
- **C — Nuevo catálogo de métricas universales, separado del de martingala**: extraer
  `ResultadoGeneral` (ya universal) como el único catálogo obligatorio, y mover
  `ResolucionDeIntentos` a un catálogo opcional que solo se puebla/muestra si la estrategia declara
  usar martingala — cambio de mayor alcance, toca `AnalizadorOperacional.cs` y potencialmente el
  generador de reportes.

**Criterio a aplicar**: la solución elegida no debe requerir que `IStrategy` (la interfaz mínima)
exponga información nueva sobre su lógica interna, salvo que se decida explícitamente que vale la
pena — coherente con P-002 (separación estrategia/economía) extendido aquí a separación
estrategia/reporte: el reporte no debería necesitar que la estrategia "se autodeclare" para
interpretarse correctamente, si es evitable.

---

## D-089 — ¿Caso 3 incorpora la resolución de D-055 en su alcance?

**Estado**: 🟢 Aprobada. **Selección: intermedia entre A y B — D-055 no bloquea el inicio de Caso
3A, pero sí puede bloquear su cierre si se incorporan suficientes familias sin martingala.**

**Decisión**: ¿D-055 se resuelve dentro de Caso 3A (bloqueante para su cierre), o permanece como
deuda técnica observada pero no resuelta, igual que quedó en el cierre de Caso 1?

**Motivo de la selección**: con D-086 (2 familias) y D-087 (máxima distancia estructural) ya
resueltas, Caso 3A integrará EMA Cross (ya existente) + 2 familias nuevas, las 3 sin martingala —
suficiente evidencia acumulada para que la pregunta "¿las métricas del laboratorio son generales?"
deje de ser opcional. Iniciar Caso 3A no requiere D-055 resuelta (correr y evaluar la primera
familia nueva no depende de ello), pero **cerrar** Caso 3A sin resolver D-055/D-088 dejaría la
misma deuda observada por tercera vez sin actuar, contradiciendo el propio criterio de cierre de
`PROPUESTA_CASO3_V1.md` §9 ("¿qué partes siguen acopladas?").

**Evidencia**: `INDICE_DECISIONES_GLOBAL_V1.md` clasifica D-055 como "🟡 Medio — depende, resolver
si Caso 3 introduce nuevas familias de estrategias". `PROPUESTA_CASO3_V1.md` §4 ya declara la
condición de activación (varias familias sin martingala) pero no decide si activarla.

**Opciones**:
- **A — D-055 resuelto es criterio de cierre de Caso 3A**: el Caso 3 no se considera cerrado hasta
  que D-088 esté implementada y verificada contra al menos 2 estrategias sin martingala (EMA Cross
  + la familia nueva de D-087) — más ambicioso, pero coherente con "no dejar la misma deuda
  observada dos veces sin actuar".
- **B — D-055 permanece observada, no bloqueante**: Caso 3A se cierra con la nueva familia
  integrada y evaluada, documentando D-055 con más evidencia (ahora 2 estrategias sin martingala en
  vez de 1) pero sin resolverla — la resolución queda para una fase específica de "catálogo de
  métricas universal" si se justifica después.

**Criterio a aplicar**: depende directamente de D-086 (si Caso 3A integra 1 o varias familias) y de
D-088 (qué tan grande es el cambio de código que D-055 requeriría) — recomendable resolver D-086 y
D-088 primero, y decidir D-089 en función de esas dos respuestas, no de forma aislada.

---

## D-090 — Ubicación de la metadata de capacidades de estrategia

**Estado**: 🟢 Aprobada e implementada. **Selección: C — clase de laboratorio.** Resuelta en
`ESPECIFICACION_FAMILIA_ESTRATEGIA_CASO3_V1.md` §3, implementada en
`protocolo/EjecutorProtocolo.cs` (`CaracteristicasEstrategia(bool UsaMartingala)`, consumido vía
`EntradaProtocolo.Caracteristicas`/`ResultadoProtocolo.Caracteristicas`, opcional, default `null`).
Verificado por P7/P8 de `caso3/TestsCaso3.cs` — auditoría de cierre en
`caso3/AUDITORIA_ZSCORE_REVERSAL_CASO3_V1.md`.

**Decisión**: ¿dónde vive `CaracteristicasEstrategia` (o equivalente) — el mecanismo declarativo
que D-088 exige, externo a `IStrategy`?

**Evidencia**: el catálogo experimental hoy es texto estructurado (`catalogo_estrategias/
EMA_CROSS.md`, `TRES_MOSQUETEROS.md`, `MHI_MAYORIA.md`) — fichas con secciones fijas
(Identificación, Definición lógica, Supuestos experimentales, Métricas evaluadas, etc.), leídas por
humanos, no consumidas por código. La metadata de D-088 necesita ser leída por
`AnalizadorOperacional`/generadores de reporte en tiempo de ejecución — un formato distinto del que
el catálogo usa hoy.

**Opciones**:
- **A — Catálogo de estrategias** (extender las fichas `.md` existentes con una sección
  estructurada de capacidades): mantiene todo en un solo lugar, pero mezcla documentación legible
  por humanos con datos que el código necesita parsear.
- **B — Archivo estructurado paralelo** (ej. `CaracteristicasEstrategia.json`/`.cs` por estrategia,
  junto a la ficha): separa lectura humana de consumo por código, requiere mantener 2 archivos
  sincronizados por estrategia.
- **C — Atributo/clase de laboratorio** (ej. un diccionario o registro en código, similar a cómo
  `EntradaProtocolo.CrearEstrategia` ya asocia una estrategia con su fábrica): vive junto al código
  del laboratorio, un único lugar de verdad consumible directamente sin parseo adicional.

**Criterio a aplicar**: no resuelto aquí — corresponde a la especificación de implementación de la
primera familia nueva (D-087 ya resuelta), donde además se decide el formato exacto de
`CaracteristicasEstrategia`.

---

## Resumen de decisiones

| Decisión | Selección | Estado |
|---|---|---|
| D-086 | 2 familias nuevas mínimo (2/2 implementadas: Z-Score Reversal, Estrategia Neutral) | 🟢 Aprobada e implementada |
| D-087 | Máxima distancia estructural con lo ya probado | 🟢 Aprobada, verificada en ambas familias |
| D-088 | Metadata externa a `IStrategy`, no inferencia desde resultados | 🟢 Aprobada e implementada |
| D-089 | D-055 no bloquea inicio de Caso 3A; confirmado no bloqueante para su cierre | 🟢 Aprobada, cerrada |
| D-090 | Clase de laboratorio (`CaracteristicasEstrategia` en `EjecutorProtocolo.cs`) | 🟢 Aprobada e implementada |

**No incluidas en este documento** (explícitamente fuera, por `PROPUESTA_CASO3_V1.md` §4): D-044
(entrada × resolución) y D-084 (`GestorCapital`) — ambas permanecen con su condición de activación
ya declarada, sin resolverse aquí.

---

## Fuera de alcance de este documento

No se modifica código en este documento. No se selecciona ningún candidato concreto de familia B/C/D
de `PROPUESTA_CASO3_V1.md` §6 — D-087 fija el *criterio* (máxima distancia estructural), no el
candidato específico, que se elige en la especificación de implementación siguiente. D-090 queda
explícitamente sin resolver.

---

## Próximo paso

Ambas familias (Z-Score Reversal, Estrategia Neutral) implementadas y auditadas individualmente
(`caso3/AUDITORIA_ZSCORE_REVERSAL_CASO3_V1.md`) y en conjunto
(`caso3/AUDITORIA_CASO3A_V1.md`) — D-086 completo, 2/2. Pendiente de revisión del auditor antes de
decidir el cierre formal de Caso 3A (versión experimental, congelamiento) o la apertura de una
fase siguiente.
