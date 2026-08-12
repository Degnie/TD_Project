# Auditoría de Cierre — Caso 3A: Generalización Experimental

Estado: **documento de cierre de fase — Caso 3A completo**. Consolida evidencia verificada de las
dos familias requeridas por D-086 (Z-Score Reversal, Estrategia Neutral) y responde los 4 criterios
de cierre fijados en `PROPUESTA_CASO3_V1.md` §9. Mismo patrón que `AUDITORIA_ZSCORE_REVERSAL_
CASO3_V1.md`, ahora a nivel de fase completa.

---

## 1. Alcance auditado

Documentos de origen: `PROPUESTA_CASO3_V1.md`, `DECISIONES_CASO3_V1.md` (D-086 a D-090),
`ESPECIFICACION_FAMILIA_ESTRATEGIA_CASO3_V1.md`, `ESPECIFICACION_IMPLEMENTACION_ZSCORE_
REVERSAL_V1.md`, `EVALUACION_SEGUNDA_FAMILIA_CASO3_V1.md`, `ESPECIFICACION_IMPLEMENTACION_
ESTRATEGIA_NEUTRAL_CASO3_V1.md`, `AUDITORIA_ZSCORE_REVERSAL_CASO3_V1.md` (cierre de la primera
familia, ya aprobado).

Implementación acumulada: `exploration/EstrategiaZScoreReversion.cs`,
`exploration/EstrategiaNeutral.cs`, `exploration/laboratorio/protocolo/EjecutorProtocolo.cs`
(extendido), `exploration/laboratorio/caso3/` (`PresentadorResolucionIntentos.cs`,
`TestsCaso3.cs`, `TestsEstrategiaNeutral.cs`, `Caso3.csproj`, `Program.cs`).

---

## 2. D-086 — Alcance de generalización

**Estado: ✅ Completo, 2/2 familias.**

| Familia | Eje estructural nuevo | Estado |
|---|---|---|
| Z-Score Reversal | señal estadística (ventana deslizante, sin patrón visual) | ✅ Implementada, auditada |
| Estrategia Neutral | control sin hipótesis de mercado (decide solo por `N`) | ✅ Implementada, verificada |

Ambas familias, sumadas a EMA Cross (Fase 1.6-D), cubren 3 ejes de generalización sin repetir
supuestos entre sí: tendencia (EMA Cross), estadística de la serie (Z-Score), independencia total
del mercado (Neutral). Ninguna comparte martingala, cuadrantes fijos ni color de vela puntual con
Tres Mosqueteros/MHI.

---

## 3. D-087 — Máxima distancia estructural

**Estado: ✅ Cumplido en ambas selecciones.**

Tabla estructural final (7 ejes, `EVALUACION_SEGUNDA_FAMILIA_CASO3_V1.md`):

| Estrategia | Origen de señal | Anclaje | Martingala | Horizonte de cierre | Estado interno | Relación cross-asset | Dirección |
|---|---|---|---|---|---|---|---|
| Tres Mosqueteros | patrón de vela | cuadrante `N%5` | sí | fijo | ninguno | no | ambas |
| MHI Mayoría | patrón de vela | cuadrante `N%5` | sí | fijo | ninguno | no | ambas |
| EMA Cross | cruce de medias | ninguno | no | variable | medias móviles | no | ambas |
| Z-Score Reversal | desviación estadística | ninguno | no | variable | ventana deslizante | no | ambas |
| Neutral | ninguno (posición `N`) | ciclo fijo | no | fijo | contador de ciclo | no | ambas |

Cada familia nueva comparte el mínimo de ejes posible con las 4 estrategias previas al momento de
su selección — Neutral en particular no comparte "origen de señal" con ninguna otra (es la única
sin ningún origen de señal de mercado).

---

## 4. D-088/D-089/D-090 — Metadata y presentación "no aplica"

**Estado: ✅ Implementado y verificado en ambas familias.**

`CaracteristicasEstrategia(bool UsaMartingala)` (`protocolo/EjecutorProtocolo.cs`), externo a
`IStrategy`, opcional con default `null`. Ambas familias declaran `UsaMartingala: false`.
`PresentadorResolucionIntentos.Formatear` distingue los 3 estados (`false` → "no aplica"; `true` →
valores reales; `null` → valores reales sin asumir aplicabilidad) — verificado independientemente
por Z-Score (P7) y Neutral (P5).

**D-089**: confirmado como "no bloqueante para cierre" — la fase cierra con D-055 documentada con
más evidencia (ahora 3 estrategias sin martingala: EMA Cross, Z-Score, Neutral) pero sin rediseño
del catálogo de métricas. Ninguna de las dos familias nuevas requirió tocar
`AnalizadorOperacional.cs` ni sus fórmulas — confirmado por ausencia de diff en ese archivo en
ambos ciclos.

**D-090**: implementada una sola vez (en la primera familia) y reutilizada sin cambios por la
segunda — confirma que la ubicación elegida (clase de laboratorio en `EjecutorProtocolo.cs`) es
estable entre familias distintas, no específica de Z-Score.

---

## 5. Restricciones de diseño respetadas

- **Sin calibración post-hoc**: `Ventana=20`/`UmbralEntrada=2.0`/`UmbralSalida=0.5` (Z-Score) y
  `Ciclo=10` (Neutral) fijados antes de ejecutar, no ajustados tras ver resultados.
- **Neutral es un control determinista, no ruido**: sin `Random`, sin semilla — verificado por P4
  (dos instancias independientes producen secuencias idénticas) y por inspección directa del
  código (`EstrategiaNeutral.cs` no importa `System.Random` ni ningún generador).
- **Neutral permanece concreta**: no se creó ninguna abstracción tipo `EstrategiaTemporalGenerica`
  — `EstrategiaNeutral` es una clase standalone, mismo estilo que las 4 estrategias previas.
- **Independencia del mercado verificada empíricamente**: P3 de Neutral altera
  `Open`/`High`/`Low`/`Volume` arbitrariamente manteniendo `Close`/`Timestamp`, y la secuencia de
  órdenes resultante es idéntica byte a byte — la propiedad central de la familia D queda
  verificada por prueba, no solo por diseño.

---

## 6. Pruebas

**16/16** pruebas de Caso 3 (8 Z-Score + 8 Neutral, `caso3/Program.cs`). Ningún hallazgo nuevo
durante la implementación de Neutral (a diferencia de Z-Score, cuyo hallazgo de arnés de prueba ya
quedó documentado y cerrado en `AUDITORIA_ZSCORE_REVERSAL_CASO3_V1.md` §6, sin decisión nueva).

---

## 7. Regresiones verificadas

- **107/107** tests de producción (`src/`/`tests/`), sin cambios acumulados en toda la fase.
- **7/7** pruebas del pipeline de Caso 1, `HashCompuesto` de `baseline_final/` intacto:
  `A48CCC57DA1919F533F4D532FDC0F945705681DCDA813B385BBFE7F44F40998E`.
- `baseline_financiero_final/` (Caso 2) no regenerada ni alterada — `git status --porcelain`
  vacío sobre esa carpeta en todo el ciclo.
- `git status --porcelain -- src/ tests/` vacío en todo el ciclo de ambas familias.

---

## 8. Decisiones activadas por esta fase

**D-044**: no activada — ninguna familia estudia interacción estrategia/régimen.

**D-084**: no activada — `GestorCapital`/sizing no interviene en Z-Score ni en Neutral.

**D-055**: permanece parcialmente resuelta según el alcance fijado en D-089 (presentación "no
aplica" implementada y probada 2 veces de forma independiente; rediseño completo del catálogo de
métricas queda fuera de alcance de Caso 3A, como deuda técnica documentada, no silenciada).

**Ninguna decisión nueva se abre en este documento** — no apareció ninguna limitación estructural
nueva durante la implementación de la segunda familia.

---

## 9. Respuesta a los criterios de cierre (`PROPUESTA_CASO3_V1.md` §9)

**¿El laboratorio generaliza?** Sí — 2 familias estructuralmente distintas entre sí y de las 3
originales se integraron sin modificar `IStrategy`, el motor, ni ningún reporte/métrica congelados
de Caso 1/Caso 2. Cumple los criterios de éxito de `PROPUESTA_CASO3_V1.md` §5 en ambos casos.

**¿Qué supuestos ocultos quedan detectados?** Ninguno nuevo respecto a los ya documentados en Caso
1/Caso 2 (D-055 sigue siendo el único supuesto de acoplamiento conocido, ya evidenciado 3 veces).

**¿Qué partes del sistema son realmente genéricas?** `IStrategy`, `EjecutorProtocolo`,
`ClasificadorRegimenV1`, `CalculadoraMetricasFinancieras`, `ReporteEscenariosGenerador` — todos
aceptaron ambas familias sin cambio de código propio (solo extensión aditiva y opcional de
`EntradaProtocolo`/`ResultadoProtocolo` para D-090, ya hecha antes de la segunda familia).

**¿Qué partes siguen acopladas a las estrategias originales?** `ResolucionDeIntentos`/
`AnalizadorOperacional.cs` — las fórmulas siguen asumiendo martingala; la capa de presentación
(`PresentadorResolucionIntentos`) compensa esto sin resolver el acoplamiento de fondo. Documentado,
no bloqueante (D-089).

---

## Fuera de alcance de este documento

No se decide todavía si Caso 3A se declara formalmente congelado (versión experimental) ni si se
abre Caso 3B. No se resuelve D-055 de forma definitiva. No se reabre D-044 ni D-084.

---

## Criterio de cierre de esta fase

- ✓ D-086: 2/2 familias implementadas y auditadas.
- ✓ D-087: máxima distancia estructural verificada con tabla de 7 ejes contra las 4 estrategias
  previas.
- ✓ D-088/D-090: metadata externa implementada una vez, reutilizada sin cambios por la segunda
  familia.
- ✓ D-089: confirmado no bloqueante, D-055 documentada con evidencia ampliada.
- ✓ 16/16 pruebas Caso 3 + 107/107 producción + hash de baseline Caso 1 intacto + baseline
  financiero Caso 2 intacto.
- ✓ Los 4 criterios de cierre de `PROPUESTA_CASO3_V1.md` §9 respondidos con evidencia verificada.
- ⏳ Auditoría revisa este documento — pendiente de confirmación antes de decidir el cierre formal
  de Caso 3A (versión experimental) o la apertura de una fase siguiente.
