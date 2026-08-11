# Protocolo de Evaluación Experimental de Estrategias V1

Estado: **especificación — Fase 1.6-A del Caso 1**. Documento de diseño, no implementación. Define
el contrato que cualquier estrategia nueva debe cumplir para entrar al laboratorio, integrando los
módulos ya construidos y congelados en Fases 1.0-1.5-B, sin recalcular ni redefinir ninguno de
ellos. No se modifica ningún módulo existente (`AnalizadorOperacional.cs`,
`ComparadorMultiTimeframe`, `ClasificadorRegimenV1.cs`, `AsignadorOperacionRegimen.cs`,
`MetricasPorEscenario.cs`, `ReporteEscenariosGenerador.cs`) ni ningún contrato de `src/`.

---

## 1. Objetivo

Responder: **"¿podemos evaluar cualquier estrategia nueva del laboratorio con el mismo estándar,
sin reinterpretar manualmente los resultados?"**

No responde: "¿debo invertir dinero en esta estrategia?" — eso pertenece a un futuro Caso 2, fuera
de alcance de este documento y de todo el Caso 1 (regla ya vigente desde Fase 1.2 §2).

---

## 2. Verificación previa — qué existe hoy y qué falta integrar

Antes de definir el protocolo, se verificó el estado real de cada módulo (no se asume nada de la
descripción de la fase):

| Módulo | Estado | Ubicación |
|---|---|---|
| Dataset congelado + hash | ✅ Congelado (Fase 1.0) | `baseline/BASELINE_EXPERIMENTAL_V1.md` |
| Backtest reproducible | ✅ Motor ya validado (`src/Application/BacktestRunner.cs`) | `src/` |
| Perfil operacional | ✅ Congelado (Fase 1.2) | `analisis_operacional/AnalizadorOperacional.cs` |
| Catálogo de estrategia (ficha) | ✅ Plantilla congelada (Fase 1.1, D-004) | `catalogo_estrategias/*.md` |
| Análisis multi-timeframe | ✅ Congelado (Fase 1.3) | `analisis_multitimeframe/PerfilMultiTimeframe.cs` |
| Clasificación de régimen | ✅ Congelado (Fase 1.4-B, D-034) | `analisis_escenarios_mercado/ClasificadorRegimenV1.cs` |
| Vinculación operación↔régimen | ✅ Cerrado (Fase 1.5-A) | `reporte_escenarios_mercado/AsignadorOperacionRegimen.cs` |
| Métricas por escenario | ✅ Cerrado (Fase 1.5-A, Paso 3) | `reporte_escenarios_mercado/MetricasPorEscenario.cs` |
| Reporte de escenarios | ✅ Cerrado (Fase 1.5-B) | `reporte_escenarios_mercado/ReporteEscenariosGenerador.cs` |

**Hallazgo verificado, no asumido**: cada módulo existe y está probado, pero cada uno se ejecuta
hoy desde un `Program.cs` propio de su carpeta (`evaluacion_multi_tf/Program.cs`,
`analisis_operacional/Program.cs`, `analisis_multitimeframe/Program.cs`,
`analisis_escenarios_mercado/Program.cs`, `reporte_escenarios_mercado/Program.cs`), cada uno con su
propia lectura de dataset y su propia construcción de `IdentidadExperimento` — no hay un punto de
entrada único. Fase 1.6-A define el contrato de qué debe producir ese punto de entrada; Fase 1.6-C
(posterior, fuera de este documento) construye el `Program.cs` unificado.

---

## 3. Entrada obligatoria

Todo protocolo de evaluación exige, antes de ejecutar nada, que la estrategia declare:

| Campo | Origen | Ya existe hoy como |
|---|---|---|
| Identidad de la estrategia (nombre) | Declarado por quien la agrega | Parámetro string en `CorrerUna` (`evaluacion_multi_tf/Program.cs`) — hoy no formalizado en un tipo |
| Versión de la estrategia | Nuevo — no existe campo de versión hoy | Ver sección 7 (decisión pendiente) |
| Dataset (nombre + hash) | Ya existe | `IdentidadExperimento.Dataset`, `SourceSha256`, `TimeframeSha256` |
| Timeframes a evaluar | Ya existe, pero declarado ad-hoc por array literal en cada `Program.cs` | `timeframesIniciales` (`evaluacion_multi_tf/Program.cs:10`) |
| Parámetros de la estrategia (ej. `maxMartingalas`) | Ya existe como argumento de constructor | Constructor de `IStrategy` (`EstrategiaTresMosqueteros(maxMartingalas: 2, ...)`) |
| Clasificador de régimen (versión) | Ya existe, congelado | `ClasificadorRegimenV1.PeriodoAdx/UmbralAdxTendencia/UmbralSesgoDI` (constantes públicas) |
| Fecha de ejecución | Ya existe | `IdentidadExperimento.FechaEjecucionUtc` |
| Hash de evidencia | **No existe hoy como campo único** — existen hashes parciales (`SourceSha256`, `TimeframeSha256`) pero ningún hash que identifique la *corrida completa* (estrategia + parámetros + dataset + clasificador) | Ver sección 7 |

**Restricción heredada, no nueva**: la estrategia debe implementar `IStrategy` sin modificaciones
(`src/Domain/Strategy/IStrategy.cs`) y emitir `InfoOperacionResuelta` con
`TimestampEntrada`/`TimestampResolucion` (D-040) para poder participar del análisis por régimen —
una estrategia que no emita este callback puede evaluarse con Fase 1.2/1.3 pero no con Fase 1.5-A/B.
Esto no es una restricción nueva de este documento, es una consecuencia directa de cómo ya está
construido el análisis de régimen.

---

## 4. Proceso obligatorio

Reafirma el orden ya impuesto por la arquitectura de capas existente (D-015: cada capa consume la
anterior, ninguna recalcula) — este protocolo no inventa un orden nuevo, documenta el que ya existe
como secuencia de ejecución obligatoria y no reordenable:

```
Dataset congelado (Fase 1.0, hash verificado)
        ↓
Estrategia congelada (implementa IStrategy, sin cambios durante la corrida)
        ↓
Backtest (BacktestRunner.Ejecutar — src/, sin modificar)
        ↓
Validación de determinismo (correr 2 veces, comparar output — mismo criterio de Fase 1.0/1.4-B)
        ↓
Perfil operacional (PerfilMultiTf.Medir — Fase 1.2, por cada timeframe evaluado)
        ↓
Análisis multi-timeframe (ComparadorMultiTimeframe.Comparar — Fase 1.3, sobre la lista completa de perfiles)
        ↓
Clasificación de régimen (ClasificadorRegimenV1.Clasificar — Fase 1.4-B, por cada timeframe, sobre las mismas velas del backtest)
        ↓
Vinculación operación↔régimen (AsignadorOperacionRegimen.Asignar — Fase 1.5-A, por cada timeframe)
        ↓
Métricas por escenario (MetricasPorEscenario.Calcular — Fase 1.5-A, por cada timeframe)
        ↓
Reporte (ReporteEscenariosGenerador.Generar — Fase 1.5-B, por cada timeframe; consolidación multi-timeframe en Fase 1.6-B)
```

**No se permite saltar pasos ni ejecutar el clasificador de régimen usando información del
resultado de la estrategia** (D-016, sigue vigente) — el orden es prescriptivo, no solo descriptivo.

---

## 5. Validación de determinismo — criterio explícito, heredado

Mismo criterio ya usado en Fase 1.0 (`BASELINE_EXPERIMENTAL_V1.md`) y Fase 1.4-B
(`TestsClasificadorRegimenV1.VerificarDeterminismo`): correr el backtest completo dos veces sobre
el mismo dataset y la misma configuración, comparar campo por campo. Una estrategia que no produzca
resultados idénticos en ambas corridas no pasa el protocolo — no se define aquí ninguna tolerancia
ni excepción.

---

## 6. Qué NO define este documento (explícitamente fuera de alcance)

- No define la estructura del reporte consolidado — eso es Fase 1.6-B, posterior y separada.
- No define el `Program.cs` unificado ni ningún pipeline automatizado — eso es Fase 1.6-C.
- No evalúa ninguna estrategia nueva — eso es Fase 1.6-D, y solo después de que este protocolo esté
  aprobado y Fase 1.6-B/C estén implementadas.
- No introduce ninguna métrica financiera, de riesgo, ni de optimización de parámetros — restricción
  ya vigente desde Fase 1.2 §2, reafirmada aquí sin cambios.
- No modifica ningún módulo congelado de Fases 1.0-1.5-B.

---

## 7. Decisiones pendientes — presentadas, no resueltas en este documento

Dos preguntas de diseño surgieron al verificar el estado real de los módulos (sección 2), que el
protocolo necesita fijar antes de que Fase 1.6-B pueda escribirse sin ambigüedad. Se presentan aquí
para decisión explícita, no se seleccionan.

### D-048 — Alcance del reporte final: por corrida o consolidado por estrategia

**Hallazgo que origina la pregunta**: `ReporteEscenariosGenerador.Generar()` (Fase 1.5-B, ya
implementado y probado) recibe una `IdentidadExperimento` (una sola combinación estrategia×
timeframe) y produce un reporte para *esa* corrida — no para el conjunto de timeframes de una
estrategia. Si una estrategia se evalúa en 6 timeframes (como Tres Mosqueteros/MHI Mayoría hoy),
el protocolo debe decidir si `REPORTE_EXPERIMENTAL_ESTRATEGIA_V1.md` (Fase 1.6-B) es:

- **Opción A — Un reporte por corrida** (6 documentos si hay 6 timeframes, cada uno con su propia
  sección de escenarios de mercado). Ventaja: reutiliza `ReporteEscenariosGenerador` sin cambios.
  Riesgo: no hay un único documento que responda "¿cómo se comporta esta estrategia en general?" —
  el usuario tendría que abrir 6 archivos.
- **Opción B — Un reporte consolidado por estrategia**, con la sección "Multi-timeframe" (bloque 4
  de la plantilla propuesta) mostrando la tabla ya existente de `ComparadorMultiTimeframe`, y la
  sección "Escenarios de mercado" (bloque 5) repitiendo la tabla de `ReporteEscenariosGenerador`
  **por cada timeframe** dentro del mismo documento. Ventaja: un solo archivo por estrategia,
  consistente con el objetivo de Fase 1.6 ("evaluar con el mismo estándar sin reinterpretar
  manualmente"). Riesgo: el documento puede ser largo (6 timeframes × 2 vistas de régimen cada uno).
- **Opción C — Reporte consolidado con resumen + reportes por corrida como anexo separado**: el
  documento principal solo trae el resumen operacional y multi-timeframe (bloques 1-4), y la
  sección de escenarios de mercado (bloque 5) enlaza a los reportes individuales de
  `ReporteEscenariosGenerador` por timeframe, generados aparte. Ventaja: documento principal corto.
  Riesgo: dos formatos de salida en vez de uno, más superficie para inconsistencia.

Ninguna opción requiere modificar código ya congelado — las tres son formas de *ensamblar* módulos
existentes, no de cambiarlos.

### D-049 — Qué es "Hash de evidencia" (sección 3, entrada obligatoria)

**Hallazgo**: no existe hoy ningún campo que identifique de forma única "esta corrida completa" —
`IdentidadExperimento` tiene `SourceSha256` (hash del dataset origen) y `TimeframeSha256` (hash del
derivado de ese timeframe), pero ningún hash que combine estrategia + parámetros + dataset +
versión del clasificador de régimen en un solo valor verificable. Sin definir esto, el criterio de
cierre de Fase 1.6 ("dos personas distintas puedan repetir el experimento y obtener la misma
salida") no tiene un mecanismo concreto de verificación — sería una afirmación sin evidencia
comprobable.

- **Opción A — No calcular un hash combinado nuevo**: la reproducibilidad se verifica manualmente
  comparando los campos ya existentes de `IdentidadExperimento` uno por uno. Sin costo de
  implementación, pero no hay un único valor que comparar.
- **Opción B — Calcular un hash combinado** (ej. SHA256 de la concatenación de
  `SourceSha256 + TimeframeSha256 + nombre estrategia + parámetros + versión del clasificador`),
  expuesto como un campo nuevo. Requiere una función nueva (no existe hoy), pero dos personas
  podrían comparar un solo valor para confirmar "misma corrida exacta".

**No se selecciona ninguna opción de D-048 ni D-049 en este documento.**

---

## Fuera de alcance (respetado)

No se implementa código en este documento. No se modifica ningún módulo de Fases 1.0-1.5-B. No se
define la plantilla de `REPORTE_EXPERIMENTAL_ESTRATEGIA_V1.md` (Fase 1.6-B). No se construye ningún
pipeline (Fase 1.6-C). No se evalúa ninguna estrategia nueva (Fase 1.6-D). No se introduce
optimización de parámetros ni selección de "mejor" timeframe/estrategia.

---

## Criterio de cierre de Fase 1.6-A (diseño)

- ✓ Objetivo definido, con la pregunta que responde y la que explícitamente no responde (sección 1).
- ✓ Estado real de cada módulo verificado contra el código, no asumido (sección 2) — identificado
  que cada módulo tiene su propio punto de entrada, sin unificar todavía (correspondiente a Fase
  1.6-C, no a este documento).
- ✓ Entrada obligatoria definida, con origen de cada campo señalado — distinguiendo lo que ya
  existe de lo que falta (sección 3).
- ✓ Proceso obligatorio documentado como el orden ya impuesto por la arquitectura de capas
  existente, no un orden nuevo inventado (sección 4).
- ✓ Criterio de determinismo heredado sin ambigüedad, mismo estándar que Fase 1.0/1.4-B (sección 5).
- ✓ Alcance explícitamente delimitado frente a 1.6-B/C/D y frente a Caso 2 (sección 6).
- ⏳ D-048 (alcance del reporte: por corrida, consolidado, o híbrido) y D-049 (si existe un hash de
  evidencia combinado) presentadas, no resueltas — pendientes de decisión explícita antes de
  escribir Fase 1.6-B (sección 7).
- ⏳ Auditoría aprueba la especificación y resuelve D-048/D-049 — pendiente de confirmación
  explícita antes de iniciar Fase 1.6-B.
