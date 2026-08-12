# Propuesta — Caso 3B: Generalización Experimental — Multi-Condición

Estado: **documento de apertura — previo a cualquier decisión o implementación**. Define la
pregunta que responde el Caso 3B, sus límites y las decisiones que deben resolverse antes de tocar
código, siguiendo el mismo ciclo que Caso 1/Caso 2/Caso 3A/Caso 4: especificación → decisión →
implementación → pruebas → auditoría → congelamiento. No abre implementación. No reabre ningún
`src/` congelado salvo lo que quede explícitamente dentro del alcance declarado en la sección 4.

**Punto de partida**: `MAPA_EVOLUCION_V2.md` §3.B y `INDICE_DECISIONES_GLOBAL_V1.md` — ninguna
deuda de Caso 1/Caso 2/Caso 3A/Caso 4 bloquea el uso de las 4 versiones congeladas como referencia
estable para esta fase.

---

## 1. Origen de la pregunta

Caso 3A (`caso3a-v1-experimental`) respondió: *¿el framework puede soportar familias
estructuralmente distintas de estrategia?* — sí, verificado con Z-Score Reversal (señal
estadística) y Estrategia Neutral (control sin mercado), ambas integradas implementando únicamente
`IStrategy`, sin tocar `MatchingEngine`/`AplicadorFill`/`ConsumidorFifo`/`ResolutorVela`.

`EVALUACION_SEGUNDA_FAMILIA_CASO3_V1.md` §2 identificó un eje **no cubierto por ninguna de las 4
estrategias existentes en ese momento**: decisión basada en múltiples fuentes/condiciones
combinadas — todas evalúan una única condición de entrada (color de vela, cruce de EMA, z-score,
o ausencia de condición en el caso Neutral). Ese documento evaluó 3 candidatos para la segunda
familia de Caso 3A (D, E, F), seleccionó **D — Estrategia sin mercado**, y registró explícitamente:

> "**E queda registrado como candidato futuro**, fuera de Caso 3A — no descartado por falta de
> valor, sino porque introduce una decisión de diseño (qué condiciones combinar) que D-086 no
> exige resolver para completar el requisito de 2 familias, y que merecería su propia
> especificación si se retoma."

Caso 3B retoma exactamente ese candidato diferido — no es una decisión nueva sin antecedente, es
la continuación de un punto ya identificado y dejado pendiente con evidencia.

---

## 2. Objetivo del Caso 3B

**Pregunta principal**: ¿el laboratorio experimental puede evaluar una estrategia cuya decisión de
entrada depende de **múltiples condiciones independientes evaluadas simultáneamente**, manteniendo
reproducibilidad, trazabilidad y separación entre estrategia, economía y análisis?

**No busca**:
- Encontrar la combinación de condiciones "ganadora".
- Optimizar qué condiciones combinar o sus umbrales.
- Producir recomendaciones financieras.

Mismo principio que Caso 3A: Caso 3B evalúa la **plataforma** frente a un nuevo eje estructural
(composición de condiciones), no la estrategia de ejemplo que se usa para probarlo.

---

## 3. Punto de partida congelado

**Caso 1** (`caso1-v1-experimental`), **Caso 2** (`caso2-v1-experimental`), **Caso 3A**
(`caso3a-v1-experimental`) y **Caso 4** (`caso4-v1-experimental`) se consideran infraestructura
estable — Caso 3B los consume sin modificarlos. El único contrato que la estrategia nueva debe
implementar es el ya existente:

```csharp
public interface IStrategy
{
    IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice);
}
```

**Explícitamente fuera de esta fase** (a diferencia de lo que podría sugerir el nombre "evolución
financiera continuada"): Caso 3B **no** activa `GestorCapital`/sizing dinámico, `ClasificadorIntencionOrden`,
ni ninguna corrección de Caso 4 — mismo criterio que `PROPUESTA_CASO3_V1.md` §4 ya aplicó a D-084
en su momento ("activar `GestorCapital` en Caso 3A sin resolver D-084 reproduciría el mismo
hallazgo ya documentado"). Aquí la razón es distinta pero el principio es el mismo: mezclar un eje
de generalización de estrategia con un eje de modelo económico en la misma fase amplía el alcance
sin necesidad — `EVALUACION_SEGUNDA_FAMILIA_CASO3_V1.md` ya lo señaló como candidato de diseño
puro de estrategia, no financiero.

---

## 4. Hipótesis de Caso 3B

**Hipótesis principal**: si una estrategia evalúa dos o más condiciones independientes que deben
cumplirse simultáneamente antes de emitir una señal, el laboratorio debe poder evaluarla sin
modificaciones al pipeline, al catálogo de métricas, ni a ningún componente de `src/`.

**Lo que Caso 3A no probó** (y Caso 3B debe): las 4 estrategias congeladas hasta ahora (Tres
Mosqueteros, MHI Mayoría, EMA Cross, Z-Score Reversal, Estrategia Neutral) evalúan todas una única
condición aislada dentro de `Observar` — nunca combinan ≥2 fuentes de datos o indicadores que deban
coincidir. Caso 3B extiende la pregunta de "¿generaliza a otra *lógica* de señal?" (ya respondida 2
veces) a "¿generaliza a otra *estructura de decisión* dentro de la señal?" — una pregunta distinta,
no una repetición.

---

## 5. Deudas técnicas que Caso 3B puede activar

No se resuelven todas — solo se declara cuáles quedan dentro del alcance de esta fase.

**D-055 — Métricas dependientes de martingala.** Activación: depende del diseño exacto de la
estrategia (con o sin martingala) — `EVALUACION_SEGUNDA_FAMILIA_CASO3_V1.md` ya señaló que esto
"no es inherente al candidato". Si la estrategia de Caso 3B no usa martingala, no aporta evidencia
nueva a D-055 más allá de la ya acumulada por EMA Cross/Z-Score/Neutral.

**D-044 — Entrada × resolución.** No incluido por defecto — Caso 3B no estudia interacción
estrategia/régimen salvo que se decida explícitamente lo contrario en el documento de decisiones.

**D-084/D-085 (Caso 4)**: ya resueltas, pero su **activación** (uso de `Sizing != null`) queda
fuera de Caso 3B por diseño de alcance (sección 3) — no porque exista una limitación técnica.

**Metadata nueva** (ej. `UsaMultiplesCondiciones`): `EVALUACION_SEGUNDA_FAMILIA_CASO3_V1.md` ya
adelantó que "no hay consumidor concreto de esa información en el catálogo de métricas actual,
mismo principio que evitó agregar `UsaSizingPropio`/`UsaEstadoInternoPersistente` sin necesidad
demostrada (D-088)" — no se justifica agregarla solo por abrir esta fase; debe surgir de una
necesidad concreta detectada durante el diseño, no asumirse de antemano.

---

## 6. Criterios de éxito

- **Estrategia integrada sin tocar**: `IStrategy`, el motor (`MatchingEngine`, `AplicadorFill`,
  `ConsumidorFifo`, `ResolutorVela`), `GestorCapital`, `ClasificadorIntencionOrden`, ni ningún
  archivo de `src/` en general. Integrarla debe requerir únicamente una nueva clase que implemente
  `IStrategy`, mismo patrón que `EstrategiaZScoreReversion.cs`/`EstrategiaNeutral.cs`.
- **El pipeline conserva** identidad experimental, reproducibilidad, reportes existentes y métricas
  ya congeladas — ninguna estrategia nueva debe requerir modificar un reporte o métrica ya
  congelados de Caso 1/Caso 2/Caso 3A/Caso 4 para producir salida válida.
- **La composición de condiciones queda verificable de forma aislada**: debe ser posible probar
  cada condición por separado y la combinación, sin depender de ejecutar todo un backtest para
  confirmar la lógica de decisión — mismo criterio de prueba unitaria ya aplicado a
  `ClasificadorIntencionOrden` en Caso 4.
- **Nuevos supuestos detectados quedan documentados**, no silenciados — mismo principio D-055/D-062.

---

## 7. Candidato de esta fase

**Candidato E — Señal multi-condición**, único candidato de esta propuesta (a diferencia de
`PROPUESTA_CASO3_V1.md` §6, que presentó 4 candidatos para elegir 1 de 2 familias). No se presentan
alternativas porque el origen de esta fase es específicamente retomar el candidato ya diferido —
si el auditor prefiere evaluar otro eje, correspondería a una propuesta distinta, no a esta.

**Diseño no resuelto todavía** (corresponde al documento de decisiones siguiente):
- Qué condiciones combinar exactamente (ej. tendencia + volumen, como ejemplo conceptual usado en
  `EVALUACION_SEGUNDA_FAMILIA_CASO3_V1.md`, sin que esto fije la elección final).
- Cuántas condiciones (mínimo 2, sin techo fijado aquí).
- Si las condiciones deben ser estructuralmente distintas entre sí (ej. una de tendencia + una de
  volatilidad) o pueden ser del mismo tipo (ej. dos umbrales sobre el mismo indicador) — afecta
  directamente qué tan "nueva" es la evidencia que aporta la fase.
- Con o sin martingala (afecta activación de D-055, sección 5).

---

## 8. Exclusiones

Fuera de alcance de Caso 3B (mismo criterio de exclusión que Caso 1 §D-002, Caso 2 §5 de
`DEUDA_TECNICA_CASO2_V1.md`, Caso 3A §7, Caso 4 §Exclusiones):

- Optimización automática de qué condiciones combinar o sus umbrales.
- Búsqueda de parámetros (grid search, calibración contra resultados).
- Ranking de estrategias — ninguna comparación implica superioridad (extiende D-014/D-047/D-076).
- Capital real, ejecución live, integración con exchange.
- IA generativa para señales — toda condición es una regla determinista explícita, igual que las 5
  estrategias existentes.
- Activación de sizing/`GestorCapital`/Caso 4 (sección 3).
- Múltiples instrumentos/correlación entre series (Candidato F, descartado en
  `EVALUACION_SEGUNDA_FAMILIA_CASO3_V1.md` por requerir tocar `src/`) — sigue descartado, no forma
  parte de Caso 3B.

---

## 9. Decisiones nuevas

Numeración reservada desde **D-099**. Ninguna decisión se resuelve dentro de esta propuesta — el
siguiente documento (`DECISIONES_CASO3B_V1.md`, o equivalente) resuelve cada punto abierto de las
secciones 5 y 7 con la misma disciplina de fases anteriores: opciones, evidencia, criterio,
selección explícita del auditor.

---

## 10. Criterios de cierre del Caso 3B

El cierre debe responder:

- ¿El laboratorio generaliza a decisión multi-condición? (evaluado contra los criterios de éxito de
  la sección 6)
- ¿Qué supuestos ocultos quedan detectados? (documentados, no corregidos silenciosamente)
- ¿La composición de condiciones introdujo acoplamiento no anticipado en algún componente
  compartido? (ej. `EjecutorProtocolo`, catálogo de métricas)
- ¿Se justificó o no agregar metadata nueva de capacidades? (sección 5)

---

## Fuera de alcance de este documento

No se implementó código. No se modifica ningún módulo de Caso 1/Caso 2/Caso 3A/Caso 4. No se
resuelve D-055 ni D-044 — solo se declara su posible activación condicional. No se fija el diseño
exacto de las condiciones a combinar — queda para el documento de decisiones siguiente.

---

## Próximo documento

Documento de decisiones de Caso 3B (numeración D-099 en adelante), resolviendo: diseño exacto de
las condiciones a combinar (sección 7), alcance de D-055 si se activa, y estructura de carpeta/
proyecto satélite para el código nuevo (mismo patrón `exploration/laboratorio/caso3/`, reutilizando
el módulo ya existente en vez de crear uno nuevo, salvo que el diseño exija lo contrario).
