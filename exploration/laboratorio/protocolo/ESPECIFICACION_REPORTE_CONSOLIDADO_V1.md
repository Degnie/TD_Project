# Especificación del Reporte Experimental Consolidado V1

Estado: **especificación — Fase 1.6-B del Caso 1**. Documento de diseño, no implementación. Aplica
D-048 (Opción C: resumen + anexos) y D-049 (identidad experimental compuesta diferida a Fase
1.6-C — este documento no la calcula ni la requiere para producirse). No modifica
`ComparadorMultiTimeframe`, `ReporteEscenariosGenerador`, `AnalizadorOperacional.cs`,
`ClasificadorRegimenV1.cs` ni ningún otro módulo congelado de Fases 1.0-1.5-B.

---

## 1. Objetivo

Definir el documento único `REPORTE_EXPERIMENTAL_ESTRATEGIA_V1.md` que un usuario abre primero para
entender el comportamiento de una estrategia, con trazabilidad completa hacia la evidencia detallada
sin obligarlo a leerla toda. Responde a los dos perfiles de lector que motivaron D-048: el usuario
explorador (necesita visión general) y el auditor/desarrollador (necesita reproducibilidad
completa).

```
PerfilMultiTf (por timeframe) → ComparadorMultiTimeframe → [Resumen, este documento]
                              ↘ AsignadorOperacionRegimen → MetricasPorEscenario → ReporteEscenariosGenerador → [Anexo por timeframe]
```

---

## 2. Qué entra al resumen (Opción C — la pregunta que D-048 dejó abierta)

La auditoría aprobó Opción C pero señaló explícitamente: "requiere definir qué entra al resumen".
Se resuelve aquí con un criterio verificable, no arbitrario: **el resumen contiene únicamente datos
ya agregados que existen sin necesidad de abrir ningún anexo — nada que requiera elegir "cuál
timeframe mostrar" para una métrica que varía por timeframe.**

Aplicando ese criterio a lo que ya existe:

| Dato | ¿Entra al resumen? | Por qué |
|---|---|---|
| Identificación (nombre, versión, tipo, fecha) | Sí | Un solo valor por estrategia, no varía por timeframe |
| Validación experimental (estado, determinismo, reconciliación) | Sí, agregado | Se muestra si las N corridas (una por timeframe) pasaron o no — detalle de cuál corrida específica falló, si aplica, va al anexo correspondiente |
| Resultado operacional global | **No entra como un solo número** | `EficienciaOperacionalPct` varía por timeframe (Fase 1.3 ya demostró que 1D=88.52% y 1m=87.08% son cifras distintas, no intercambiables) — mostrar "el" resultado operacional sin decir de qué timeframe sería exactamente el tipo de ranking implícito que D-014 prohíbe |
| Multi-timeframe (`PerfilMultiTimeframe`) | Sí, completo | Ya es en sí mismo un resumen multi-timeframe (Fase 1.3) — no agrega nada nuevo, solo se reutiliza tal cual |
| Escenarios de mercado | **No entra el detalle** — entra solo una referencia | `MetricasPorEscenario` es por timeframe (mismo hallazgo de D-048/Fase 1.5-B) — el resumen no puede mostrar "la" tabla de régimen sin elegir un timeframe, lo cual violaría el mismo principio que motivó D-048. Se listan los N anexos disponibles, sin reproducir sus tablas. |
| Limitaciones | Sí | Texto fijo, no varía por timeframe (heredado de Fase 1.2 §9/1.0) |

**Consecuencia directa**: la sección "3. Resultado operacional global" que proponía la auditoría se
reemplaza por **"3. Resultado operacional — remite a la sección 4"**, porque un "resultado global"
de una estrategia que se comporta distinto por timeframe no tiene una única cifra representativa sin
elegir una — y elegir una sería la selección retrospectiva/ranking que el proyecto lleva prohibiendo
desde D-009/D-014. Se señala aquí como corrección explícita a la estructura propuesta en la
autorización de fase, no como desviación silenciosa.

---

## 3. Estructura del documento

```
REPORTE_EXPERIMENTAL_ESTRATEGIA_V1.md — {Estrategia}

1. Identificación
   Nombre, versión, tipo (Patrón/Tendencia/etc. — D-003), fecha de generación del reporte,
   dataset (nombre + SourceSha256 + TimeframeSha256 por timeframe evaluado).

2. Validación experimental
   Por cada timeframe evaluado: Estado del motor (EstadoBacktest), determinismo (2 corridas
   comparadas, sección 5 del protocolo), ReconciliacionCoherente. Tabla simple, una fila por
   timeframe — dato ya existente en PerfilMultiTf, sin cálculo nuevo.

3. Resultado operacional
   Remite a la sección 4 (ver sección 2 de este documento — no hay una única cifra global sin
   elegir un timeframe).

4. Multi-timeframe
   PerfilMultiTimeframe completo (Fase 1.3), sin cambios: tabla de FilaTimeframe, Consistencia
   (mín/máx/amplitud), MejorResultadoObservado y MayorEvidencia mostrados por separado, nunca
   combinados en un ranking único (D-014).

5. Escenarios de mercado
   Lista de anexos disponibles (uno por timeframe evaluado, sección 4 de este documento) — no
   reproduce las tablas aquí. Cada anexo es la salida literal de ReporteEscenariosGenerador.Generar()
   para ese timeframe, sin modificar su contenido.

6. Limitaciones
   Texto heredado, sin cambios: modelo económico incompleto, sin costes reales, sin interpretación
   financiera (mismo texto ya usado en Fase 1.2 §9 / catálogo de estrategias).

Anexos (documentos separados, uno por timeframe evaluado):
   REPORTE_EXPERIMENTAL_ESTRATEGIA_V1_ANEXO_{timeframe}.md
   = salida literal de ReporteEscenariosGenerador.Generar() (Fase 1.5-B), sin modificar.
```

---

## 4. Relación entre resumen y anexos — mecanismo concreto

El resumen (sección 3 de este documento) enlaza a cada anexo por nombre de archivo, no por
contenido embebido. Esto evita que el resumen y el anexo puedan divergir (un solo lugar de verdad
por timeframe: el anexo). El resumen en su sección 5 solo lista:

```
5. Escenarios de mercado

   Evaluado en 6 timeframes. Ver anexo correspondiente para el detalle completo (vista por régimen
   de entrada, vista por régimen de resolución, nota de correlación≠causalidad):

   - 1m  → REPORTE_EXPERIMENTAL_ESTRATEGIA_V1_ANEXO_1m.md
   - 5m  → REPORTE_EXPERIMENTAL_ESTRATEGIA_V1_ANEXO_5m.md
   - 15m → REPORTE_EXPERIMENTAL_ESTRATEGIA_V1_ANEXO_15m.md
   - 1h  → REPORTE_EXPERIMENTAL_ESTRATEGIA_V1_ANEXO_1h.md
   - 4h  → REPORTE_EXPERIMENTAL_ESTRATEGIA_V1_ANEXO_4h.md
   - 1D  → REPORTE_EXPERIMENTAL_ESTRATEGIA_V1_ANEXO_1D.md
```

Ningún anexo se genera si el timeframe no fue evaluado (mismo criterio ya vigente en Fase
1.2/§4.4: distinguir "timeframe disponible" de "timeframe evaluado", D-007).

---

## 5. Integridad verificable entre resumen y anexos

Extiende la verificación ya existente (Fase 1.5-B §8, `ReporteEscenariosGenerador` ya la hace
visible dentro de cada anexo): el total de operaciones mostrado en cada anexo (bloque 1 de ese
anexo) debe coincidir con `IntentosCompletados` de la fila correspondiente en la tabla de
Multi-timeframe (sección 4 de este documento, bloque 4). Esta es una verificación cruzada nueva de
este nivel (resumen ↔ anexo), no existía antes porque no existía un resumen que agregara varios
anexos.

---

## 6. Qué NO define este documento

- No calcula la identidad experimental compuesta (D-049) — ese cálculo se define e implementa en
  Fase 1.6-C, no aquí. Este documento no depende de que exista para poder generarse.
- No construye el orquestador que ejecuta las 6 corridas y ensambla el documento — eso es Fase
  1.6-C (pipeline).
- No evalúa ninguna estrategia nueva (Fase 1.6-D).
- No introduce ranking entre timeframes ni entre regímenes — todas las restricciones ya vigentes
  (D-009/D-014/D-044/D-047) se heredan sin excepción.

---

## Fuera de alcance (respetado)

No se implementa código en este documento. No se modifica `ComparadorMultiTimeframe`,
`ReporteEscenariosGenerador`, `MetricasPorEscenario.cs`, `AnalizadorOperacional.cs` ni
`ClasificadorRegimenV1.cs`. No se calcula la identidad experimental compuesta (D-049, diferida a
Fase 1.6-C). No se genera ningún ranking ni conclusión comparativa entre timeframes o regímenes.

---

## Criterio de cierre de Fase 1.6-B (diseño)

- ✓ Objetivo definido con los dos perfiles de lector que motivaron D-048 (sección 1).
- ✓ Criterio verificable de qué entra al resumen — no arbitrario, basado en si el dato varía por
  timeframe (sección 2), con corrección explícita a la sección "Resultado operacional global"
  propuesta originalmente (reemplazada por una remisión a la sección 4, no eliminada en silencio).
- ✓ Estructura del documento definida en 6 secciones + anexos, reutilizando literalmente
  `PerfilMultiTimeframe` (sección 4) y `ReporteEscenariosGenerador` (anexos) sin modificarlos
  (sección 3).
- ✓ Mecanismo concreto de enlace resumen↔anexo definido (por nombre de archivo, un solo lugar de
  verdad por timeframe) — sección 4.
- ✓ Verificación de integridad cruzada nueva (resumen ↔ anexo) definida (sección 5).
- ✓ Alcance delimitado frente a Fase 1.6-C/D (sección 6).
- ⏳ Auditoría aprueba la especificación — pendiente de confirmación explícita antes de iniciar
  código.
