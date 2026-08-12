# Auditoría de Cierre — Caso 5A: Evaluación Comparativa de Gestores de Riesgo

Estado: **documento de cierre de sub-fase — Caso 5A completo**. Consolida evidencia verificada del
ciclo propuesta → decisión → especificación → implementación → pruebas → auditoría para D-108 a
D-111. Mismo patrón que las auditorías de cierre de Caso 3B (`AUDITORIA_CASO3B_V1.md`) y Caso 4.

---

## 1. Alcance auditado

Documentos de origen: `../PROPUESTA_CASO5_V1.md`, `DECISIONES_CASO5_V1.md` (D-108 a D-111, más la
precisión derivada de D-109), `ESPECIFICACION_IMPLEMENTACION_GESTORES_RIESGO_V1.md`.
Implementación: `src/Domain/Portfolio/IGestorRiesgo.cs`,
`src/Domain/Portfolio/IIdentidadGestorRiesgo.cs`, `src/Domain/Portfolio/GestorFixedFractional.cs`,
`src/Domain/Portfolio/GestorFixedRisk.cs`, `src/Domain/Portfolio/GestorVolatilitySizing.cs`,
`src/Domain/Portfolio/GestorCapital.cs` (modificado), `src/Domain/Shared/ConfiguracionSizing.cs`
(modificado), `src/Application/BacktestRunner.cs` (modificado),
`exploration/laboratorio/protocolo/IdentidadExperimentoCompleta.cs` (modificado),
`exploration/laboratorio/modelo_financiero/MetricasFinancieras.cs`/
`CalculadoraMetricasFinancieras.cs` (modificados), `caso5/TestsGestoresRiesgo.cs`.

**Alcance confirmado explícitamente por el auditor** (dos veces en este ciclo): framework de
gestores de riesgo intercambiables — Fixed Fractional, Fixed Risk, Volatility Sizing —, **no** la
gestión de exposición/drawdown/límites planteada inicialmente como "Caso 5" antes de esta
propuesta (diferida, ver §8).

---

## 2. Origen y punto de partida verificado

`MAPA_EVOLUCION_V2.md` §0 (post-validación integral) reformuló la pregunta guía de "¿funciona
correctamente el laboratorio?" a "¿qué capacidad nueva aporta mayor valor experimental?". Sobre
esa base, el auditor propuso dos framings distintos para "Caso 5" en mensajes consecutivos —
gestión de exposición/drawdown vs. framework de gestores intercambiables (mapa de evolución V3,
Caso 5A) — y resolvió explícitamente por el segundo vía pregunta de clarificación.

**Hallazgo central que ancló el diseño** (`PROPUESTA_CASO5_V1.md` §2, verificado contra código
antes de proponer, no reconstruido de memoria): `GestorCapital.Ajustar` mezclaba dos
responsabilidades — cálculo de cantidad específico de Fixed Fractional (líneas 37-39 del código
previo a esta fase) y clasificación de intención + normalización de Cross-Zero (D-092/D-095,
líneas 41-71, independiente de qué gestor calcula la cantidad). Esta distinción se confirmó
correcta durante la implementación: la parte compartida no referencia ningún parámetro de Fixed
Fractional.

---

## 3. Decisiones D-108 a D-111 — resumen

| Decisión | Resolución |
|---|---|
| D-108 | Aislamiento de responsabilidades: interfaz `IGestorRiesgo` (único método, calcula cantidad) + `GestorCapital` pasa a orquestar, conservando íntegra la clasificación/normalización (D-092/D-095) sin duplicarla por gestor |
| D-109 | `ConfiguracionSizing` describe una elección (`GestorActivo: IGestorRiesgo` ya parametrizado), rechazando un enum de tipos con campos por gestor (anticiparía gestores inexistentes, permitiría estados inválidos) |
| D-110 | Alcance inicial: A (Fixed Fractional, control obligatorio) → B (Fixed Risk) → C (Volatility Sizing), en ese orden de prioridad conceptual; Kelly/Masaniello diferidos por el bloqueo de probabilidad-de-acierto ya identificado en Caso 2.3 |
| D-111 | Métricas de comparación por categoría (retorno/riesgo/consistencia/exposición/supervivencia); alcance de implementación reducido durante la especificación (ver §6) |

**Precisión derivada de D-109 (no reabre la decisión, la completa)**: durante la especificación de
implementación se detectó que `IdentidadExperimentoCompleta.CalcularHashConfiguracionEconomica`
serializaba `ConfiguracionSizing.PorcentajeRiesgo` directamente — campo que D-109 elimina. Resuelto
con un contrato separado, `IIdentidadGestorRiesgo` (`ObtenerIdentidadConfiguracion(): string`),
manteniendo `IGestorRiesgo` con responsabilidad única (D-108 intacta) — la identidad experimental
no es parte del contrato funcional del gestor, es una capacidad aparte que cada gestor concreto
también implementa. Mismo criterio que D-062/D-083/D-084/D-095/D-107 en fases anteriores: una
precisión que completa una decisión existente no es, por sí sola, una decisión estructural nueva —
no se abrió D-112.

---

## 4. Hallazgos de implementación (verificados y corregidos, ninguno oculto)

**Hallazgo 1 — Firma de `GestorCapital.Ajustar` insuficiente para Volatility Sizing**: la firma
original recibía `precioReferencia` como escalar — sin historial de velas. `GestorVolatilitySizing`
requiere una ventana de Closes. Resuelto extendiendo la firma para aceptar el `DataSlice` que
`BacktestRunner` ya construye para la Strategy (mismo dato, sin cálculo adicional) — no traslada
lógica de mercado al orquestador, `GestorCapital` sigue sin interpretar velas, solo las entrega a
quien las necesita.

**Hallazgo 2 — Consumidores de `ConfiguracionSizing`/`PorcentajeRiesgo` no capturados por la
búsqueda textual inicial**: la primera pasada de la especificación (búsqueda de
`ConfiguracionSizing(` en `src/`) concluyó "ninguno detectado fuera de la propia definición". Una
búsqueda exhaustiva sin restricción de carpeta, y posteriormente la compilación real de cada
`.csproj` satélite uno por uno, encontraron **5 consumidores reales**:
`tests/Application.Tests/GestorCapitalTests.cs` (12 usos, suite protegida), `exploration/
laboratorio/validacion_integral/TestsValidacionIntegral.cs` (2 usos), `IdentidadExperimentoCompleta.cs`
(el hallazgo que originó la precisión de D-109, §3), y dos detectados solo al compilar
(`ReporteFinancieroGenerador.cs`, `ProgramBaselineFinanciero.cs` — ambos leían `.PorcentajeRiesgo`
para texto de reporte/JSON, no vía constructor, por lo que el grep original no los alcanzó). Los 5
se migraron: los 2 de test a `new ConfiguracionSizing(new GestorFixedFractional(...))` sin cambiar
ninguna aserción de resultado; los 3 restantes al patrón `IIdentidadGestorRiesgo` ya resuelto en la
precisión de D-109.

**Hallazgo 3 — `RachaPositivaMaxima` (D-111) requiere una capa fuera del alcance autorizado**:
vive naturalmente en `exploration/laboratorio/evaluacion_multi_tf/PerfilMultiTf.cs`, capa de
evaluación agregada multi-timeframe distinta de los 4 archivos que Caso 5A autorizó modificar.
Escalado antes de tocar el archivo — el auditor resolvió diferirla explícitamente, no como deuda
bloqueante (ver §6).

**Hallazgo 4 — `LaboratorioSintetico.csproj` falla al compilar, preexistente**: no excluye
`caso4/**`/`validacion_integral/**` de su compilación automática por carpeta, generando conflictos
de `AssemblyInfo` duplicado. Verificado con `git log` que el archivo no fue tocado en esta sesión
ni en las de Caso 4/validación integral — la falla existe desde que esas carpetas se crearon, sin
que nadie actualizara sus exclusiones. No corregido dentro de Caso 5A (fuera del alcance de esta
fase, es un archivo de configuración de build compartido).

**Ninguno de los 4 hallazgos abrió una decisión D-112 ni cambió D-108/D-109/D-110/D-111**: los
hallazgos 1 y 2 son correcciones de precisión entre especificación e implementación real
(consistente con el patrón ya visto en Caso 3B); el hallazgo 3 es una decisión de alcance explícita
del auditor, documentada; el hallazgo 4 es una falla ajena a esta fase, solo señalada.

---

## 5. Evidencia de pruebas

**10/10 pruebas de Caso 5A** (`caso5/TestsGestoresRiesgo.cs`): P1 (migración sin cambio de
comportamiento — `HashCompuesto`/`HashConfiguracionEconomica` reproducibles con
`GestorFixedFractional`), P2 (D-092 intacta — clasificación de intención idéntica entre los 3
gestores), P3 (D-095 intacta — Cross-Zero espurio normalizado igual entre los 3 gestores), P4
(`Sizing=null` no invoca ningún gestor), P5 (`GestorFixedRisk`, fórmula y caso de exceso de
capital), P6 (`GestorVolatilitySizing`, warmup y ventana con desviación conocida), P7 (comparación
de control — misma secuencia de señales de la estrategia con los 3 gestores, solo cambia la
cantidad), P8 (`ProfitFactor`/`CapitalLibreMinimo` contra cálculo manual, incluyendo el caso `null`
sin pérdidas), P9 (equivalencia con sizing desactivado sobre 4 fixtures — Cross-Zero genuino,
CierreTotal, ReducciónParcial, bolsa completa), P10 (identidad estable entre instancias
equivalentes de los 3 gestores + fallo explícito de `IdentidadExperimentoCompleta` ante un gestor
sin `IIdentidadGestorRiesgo`).

**126/126 tests de producción**: sin regresión, incluyendo `GestorCapitalTests.cs` migrado (mismas
aserciones, mismos valores esperados).

**14/15 `.csproj` satélite de `exploration/laboratorio/` compilan limpio** tras la migración
(§4, hallazgo 2) — el único que no compila es una falla preexistente ajena a esta fase (§4,
hallazgo 4).

**Pipeline de Caso 1**: `HashCompuesto = A48CCC57DA1919F533F4D532FDC0F945705681DCDA813B385BBFE7F44
F40998E` idéntico al congelado en `caso1-v1-experimental` — verificado reproducible tras todos los
cambios de esta fase.

---

## 6. Métricas D-111 — implementadas y diferidas

**Implementadas** (`MetricasFinancieras`/`CalculadoraMetricasFinancieras`, fuente única, sin
recalcular nada que el motor ya calcule, D-072/D-077):
- `ProfitFactor: decimal?` — suma de ganancias / suma de pérdidas absolutas sobre `Trades`, `null`
  sin pérdidas (evita división por cero, mismo criterio `decimal?` que `DrawdownMaximoPct`, D-078).
- `CapitalLibreMinimo: decimal` — mínimo de `Cash - Margin` sobre `PortfolioSnapshots` más el
  estado inicial.
- `MargenMaximoUtilizado` — **no** se agregó como campo nuevo: es exactamente el mismo cálculo que
  `ExposicionMaxima` ya tenía (`PortfolioSnapshots.Max(s => s.Margin)`) — equivalencia documentada,
  aprobada explícitamente por el auditor en vez de duplicar fuente.

**Diferidas, no como deuda bloqueante** (documentado en `DECISIONES_CASO5_V1.md`, precisión de
D-111): `RachaPositivaMaxima` (requiere `PerfilMultiTf.cs`, fuera del alcance autorizado — §4,
hallazgo 3), duración de drawdown, riesgo de ruina (ninguna fuente de dato tan directa como las
demás, requieren definición explícita no fijada en esta fase). Las métricas ya implementadas
(retorno, profit factor, drawdown existente, margen máximo, capital libre, exposición) son
suficientes para la comparación inicial de gestores que Caso 5A busca habilitar.

---

## 7. Confirmación de no regresión

- **5 baselines congelados** (`caso1-v1-experimental`, `caso2-v1-experimental`,
  `caso3a-v1-experimental`, `caso3b-v1-experimental`, `caso4-v1-experimental`): sin alterar en
  ningún punto del ciclo de Caso 5A.
- **`IStrategy`**: sin modificación.
- **Las 6 estrategias existentes** (Tres Mosqueteros, MHI Mayoría, EMA Cross, Z-Score Reversal,
  Estrategia Neutral, Volumen Breakout): sin ningún cambio de código — confirmado además por P7
  (misma secuencia de señales con los 3 gestores).
- **`AplicadorFill`/`ResolutorCrossZero`/`ConsumidorFifo`**: sin ninguna modificación — el cambio
  de firma de `GestorCapital.Ajustar` (§4, hallazgo 1) no altera el motor de matching.
- **`ClasificadorIntencionOrden`**: sin modificación — D-092 verificada intacta por P2.
- **Kelly, Masaniello**: no implementados — D-110 los difiere explícitamente por el bloqueo de
  probabilidad de acierto de Caso 2.3, no resuelto en esta fase.
- **Límites de exposición/drawdown/circuit breakers**: no implementados — framing original de
  "Caso 5" explícitamente diferido a una fase posterior distinta (`PROPUESTA_CASO5_V1.md` §7).
- **Sistema recomendador de gestores**: no implementado — reservado como Caso 5B, condicionado a
  que Caso 5A produzca evidencia comparativa primero.

---

## 8. Estado final — Decisiones de Caso 5A

| Decisión | Estado |
|---|---|
| D-108 | ✅ Aislamiento cálculo/clasificación (`IGestorRiesgo` + orquestación) |
| D-109 | ✅ Configuración por elección (`ConfiguracionSizing.GestorActivo`) + precisión de identidad (`IIdentidadGestorRiesgo`) |
| D-110 | ✅ Alcance inicial: Fixed Fractional, Fixed Risk, Volatility Sizing; Kelly/Masaniello diferidos |
| D-111 | ✅ Métricas: ProfitFactor/CapitalLibreMinimo/MargenMaximoUtilizado(=ExposicionMaxima) implementadas; RachaPositivaMaxima/DuraciónDrawdown/RiesgoDeRuina diferidas, no bloqueantes |

**Ninguna deuda técnica bloqueante queda abierta dentro del alcance de Caso 5A.** Las 3 métricas
diferidas y la falla preexistente de `LaboratorioSintetico.csproj` quedan documentadas como
pendientes explícitos, no como bloqueos de cierre.

---

## Fuera de alcance de este documento

No se decide si Caso 5A se congela como versión experimental independiente ni si requiere una
sub-fase adicional antes del congelamiento. No se recalibra ningún parámetro de ningún gestor. No
se abre Caso 5B (sistema recomendador). No se corrige `LaboratorioSintetico.csproj`. No se
introducen Kelly/Masaniello ni límites de exposición/drawdown.

---

## Criterio de cierre de esta sub-fase

- ✓ D-108 a D-111: cada una con opciones evaluadas, evidencia y selección explícita del auditor.
- ✓ Precisión derivada de D-109 (identidad separada del contrato funcional) escalada y resuelta
  sin asumir respuesta, sin abrir decisión D-112 innecesaria.
- ✓ 4 hallazgos de implementación detectados, corregidos o diferidos explícitamente y
  documentados — ninguno oculto, ninguno cambió hipótesis, alcance ni arquitectura sin pasar por
  el auditor.
- ✓ 10/10 pruebas Caso 5A + 126/126 producción + 14/15 satélites (1 falla preexistente ajena) +
  hash Caso 1 intacto + 5 baselines congelados sin alterar.
- ✓ Ninguna restricción de alcance relajada: `IStrategy`, estrategias existentes, motor de
  matching, Kelly/Masaniello, límites de exposición, recomendador de gestores — todos fuera,
  como estaba autorizado.
- ⏳ Pendiente de tu decisión: congelar Caso 5A como versión experimental o abrir una sub-fase
  adicional antes del congelamiento.
