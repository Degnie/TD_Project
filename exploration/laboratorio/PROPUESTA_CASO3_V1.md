# Propuesta — Caso 3: Generalización Experimental (Caso 3A)

Estado: **documento de apertura — previo a cualquier decisión o implementación**. Define la
pregunta que responde el Caso 3, sus límites y las decisiones que deben resolverse antes de tocar
código, siguiendo el mismo ciclo que Caso 1 y Caso 2: especificación → decisión → implementación →
pruebas → auditoría → congelamiento. No abre implementación. No resuelve deuda técnica salvo la que
quede explícitamente dentro del alcance declarado en la sección 4.

**Punto de partida**: `INDICE_DECISIONES_GLOBAL_V1.md` — ninguna deuda de Caso 1/Caso 2 bloquea el
uso de ambos como referencia estable para esta fase.

---

## 1. Objetivo del Caso 3

**Pregunta principal**: ¿el laboratorio experimental puede evaluar estrategias estructuralmente
diferentes a las originales manteniendo reproducibilidad, trazabilidad y separación entre
estrategia, economía y análisis?

**No busca**:
- Encontrar la estrategia "ganadora".
- Optimizar parámetros.
- Producir recomendaciones financieras.

El Caso 3 evalúa la **plataforma**, no las estrategias que se le ofrecen como caso de prueba —
mismo principio que D-054 aplicó a EMA Cross en Caso 1 ("valida generalidad del pipeline, nunca
evalúa rentabilidad"), extendido aquí como el objetivo central de una fase completa, no de una
estrategia aislada.

---

## 2. Punto de partida congelado

**Caso 1** (`caso1-v1-experimental`) aporta: estrategias iniciales (Tres Mosqueteros, MHI Mayoría,
EMA Cross), clasificador de régimen, pipeline experimental (`EjecutorProtocolo`), reportes
(`ReporteConsolidadoGenerador`, `ReporteEscenariosGenerador`).

**Caso 2** (`caso2-v1-experimental`) aporta: modelo económico (`Instrumento`, `AplicadorFill`),
costes (`ConfiguracionCostes`), métricas financieras (`MetricasFinancieras`,
`CalculadoraMetricasFinancieras`), identidad económica (`HashCompuesto` +
`HashConfiguracionEconomica`, D-082).

Ambos se consideran **infraestructura estable** — el Caso 3 los consume sin modificarlos (mismo
criterio P-001 de Caso 2: motor congelado, no se reabre sin una decisión explícita). El único
contrato que una estrategia nueva de Caso 3 debe implementar es el ya existente:

```csharp
public interface IStrategy
{
    IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice);
}
```

---

## 3. Hipótesis de Caso 3

**Hipótesis principal**: si una estrategia nueva no comparte los supuestos estructurales de las
estrategias originales, el laboratorio debe poder evaluarla sin modificaciones específicas de esa
estrategia.

**Evidencia parcial ya existente**: EMA Cross (Fase 1.6-D, D-054) ya demostró generalidad en 3
ejes concretos, verificados en código (`EstrategiaEmaCross.cs`):
- **Sin cuadrantes** (`N%5`) — señal por cruce de EMA corta/larga en vez de posición absoluta en el
  dataset.
- **Sin martingala/reintentos** — cierre por cruce contrario, sin escalón de reapertura.
- **Sin límite de velas por operación** — a diferencia de Tres Mosqueteros/MHI (resolución fija en
  N ciclos), EMA Cross puede mantener una posición abierta un número arbitrario de velas.

El pipeline (`EjecutorProtocolo`, `PerfilMultiTf`, `ClasificadorRegimenV1`,
`CalculadoraMetricasFinancieras`) aceptó EMA Cross sin ningún cambio de código en su momento — esa
es la evidencia base sobre la que Caso 3 extiende la pregunta a más familias, no una repetición del
experimento.

**Lo que EMA Cross no probó** (y Caso 3 debe): que el catálogo de **métricas** generaliza. D-055
ya documentó que `GanoM1`/`GanoM2`/`PctResueltasPorMartingala` colapsan a 0 para EMA Cross — el
pipeline aceptó la estrategia, pero una parte del catálogo de salida quedó sin información
interpretable. Caso 3 hereda esa pregunta sin resolverla de antemano.

---

## 4. Deudas técnicas que Caso 3 puede activar

No se resuelven todas — solo se declara cuáles quedan dentro del alcance de esta fase.

**D-055 — Métricas dependientes de martingala.** Activación: si Caso 3 incorpora varias familias
de estrategias sin martingala (candidatos B/C de la sección 6). Alcance posible: separar métricas
universales (aplicables a cualquier `IStrategy`) de métricas específicas de estrategia
(martingala-dependientes), sin eliminar estas últimas — mismo criterio D-035 (clasificadores
oficiales no eliminan a los experimentales, coexisten).

**D-044 — Entrada × resolución.** Activación: solo si el objetivo pasa a ser estudiar interacción
estrategia/régimen explícitamente. No incluido por defecto en el alcance inicial de Caso 3A.

**D-084 — `GestorCapital` no distingue apertura/cierre.** No incluido en Caso 3A salvo que una
estrategia nueva requiera sizing activo para ser evaluable con sentido. Mantener como frontera de
un futuro Caso 3B (evolución financiera) — activar `GestorCapital` en Caso 3A sin resolver D-084
reproduciría el mismo hallazgo ya documentado (residuos de lotes en corridas largas).

---

## 5. Criterios de éxito

- **Nueva estrategia integrada sin tocar**: `IStrategy` (la interfaz), el motor (`MatchingEngine`,
  `AplicadorFill`, `ConsumidorFifo`, `ResolutorVela`), ni ningún archivo de `src/` en general.
  Integrarla debe requerir únicamente una nueva clase que implemente `IStrategy`, mismo patrón que
  `EstrategiaEmaCross.cs`.
- **El pipeline conserva** identidad experimental (`HashCompuesto`/`HashConfiguracionEconomica`),
  reproducibilidad (determinismo verificado por `EjecutorProtocolo`), reportes existentes
  (`ReporteConsolidadoGenerador`, `ReporteFinancieroGenerador`) y métricas ya congeladas — ninguna
  estrategia nueva debe requerir modificar un reporte o métrica ya congelados de Caso 1/Caso 2 para
  producir salida válida (puede producir salida *parcialmente vacía o no aplicable*, nunca forzar
  un cambio retroactivo).
- **Nuevos supuestos detectados quedan documentados**, no silenciados — mismo principio que D-055
  documentó explícitamente en vez de ocultar el colapso de métricas a 0.

---

## 6. Candidatos iniciales de validación

No se selecciona ninguno todavía — se presentan como familias posibles, a decidir en el
documento de decisiones que sigue a esta propuesta.

- **A — Tendencia**: ya realizado (EMA Cross, Fase 1.6-D). Sirve como referencia de comparación,
  no como candidato nuevo.
- **B — Reversión**: familia nueva, no explorada — señal basada en sobre-extensión de precio
  (ej. bandas, RSI extremo) esperando reversión a la media.
- **C — Señal estadística**: estructura distinta de A/B — señal basada en propiedades estadísticas
  de la serie (ej. desviación, z-score) en vez de patrón visual o indicador de tendencia.
- **D — Estrategia sin mercado**: test de neutralidad — una estrategia que no reacciona a ninguna
  condición real del dataset (ej. señal aleatoria o fija), útil para verificar que el pipeline no
  produce resultados espurios cuando no hay señal genuina que evaluar.

---

## 7. Exclusiones

Fuera de alcance de Caso 3 (mismo criterio de exclusión que Caso 1 §D-002 y Caso 2 §5 de
`DEUDA_TECNICA_CASO2_V1.md`):

- Optimización automática de parámetros.
- Búsqueda de parámetros (grid search, calibración contra resultados).
- Ranking de estrategias — ninguna comparación implica superioridad (extiende D-014/D-047/D-076).
- Capital real, ejecución live, integración con exchange.
- IA generativa para señales — ninguna estrategia de esta fase decide su lógica mediante un modelo
  entrenado o generado; toda señal es una regla determinista explícita, igual que las 4 estrategias
  existentes.

---

## 8. Decisiones nuevas

Numeración reservada desde **D-086**. Ninguna decisión se resuelve dentro de esta propuesta — este
documento abre la fase, el siguiente (`DECISIONES_CASO3_V1.md`, o equivalente) resuelve cada punto
abierto de las secciones 4 y 6 con la misma disciplina de Caso 1/Caso 2: opciones, evidencia,
criterio, selección explícita del auditor.

---

## 9. Criterios de cierre del Caso 3

El cierre debe responder:

- ¿El laboratorio generaliza? (evaluado contra los criterios de éxito de la sección 5)
- ¿Qué supuestos ocultos quedan detectados? (documentados, no corregidos silenciosamente — mismo
  principio D-055/D-062)
- ¿Qué partes del sistema son realmente genéricas? (`IStrategy`, pipeline, motor económico)
- ¿Qué partes siguen acopladas a las estrategias originales? (ej. catálogo de métricas de
  martingala, si D-055 no se resuelve completamente en esta fase)

---

## Fuera de alcance de este documento

No se implementó código. No se modifica ningún módulo de Caso 1 ni Caso 2. No se resuelve D-055,
D-044 ni D-084 — solo se declara su posible activación condicional. No se selecciona ningún
candidato de la sección 6 — queda para el documento de decisiones siguiente.

---

## Próximo documento

Documento de decisiones de Caso 3 (numeración D-086 en adelante), resolviendo: selección de
candidato(s) de la sección 6, alcance exacto de D-055 si se activa, y estructura de carpeta/
proyecto satélite para el código nuevo (mismo patrón `exploration/laboratorio/<nombre>/`).
