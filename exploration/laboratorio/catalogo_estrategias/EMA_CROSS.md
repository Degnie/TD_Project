# Ficha de Estrategia Experimental — EMA Cross

Plantilla: Estrategia Experimental v1.0 (Fase 1.1, decisión de auditoría 2026-08-11). Estrategia
de validación de generalidad del laboratorio (D-054, Fase 1.6-D) — su propósito es distinto al de
las demás fichas: no busca describir una estrategia candidata para uso futuro, busca demostrar que
el protocolo/pipeline/catálogo de métricas del laboratorio no dependen de los supuestos
estructurales compartidos por Tres Mosqueteros y MHI Mayoría.

---

# Identificación

- **Nombre**: EMA Cross
- **Versión**: v1.0 (única, sin variantes registradas)
- **Estado**: Validada (evaluada en Fase 1.6-D sobre dataset real BTC/USDT, 6 timeframes, vía pipeline `EjecutorProtocolo`)
- **Tipo**: Tendencia (a diferencia de Tres Mosqueteros/MHI Mayoría, clasificadas Patrón en D-003) — genera su señal a partir de un indicador que resume el comportamiento acumulado de varias velas (EMA), no de una regla determinística sobre una posición fija en el dataset.

---

# Definición lógica

**Descripción funcional**: entra en la dirección de un cruce de medias móviles exponenciales (EMA corta/larga) y cierra en el cruce contrario — sin cuadrantes fijos (`N % 5`), sin martingala, sin límite de velas por posición.

**Entrada**:
- Condición: `EMA_corta` cruza por encima de `EMA_larga` → Buy. `EMA_corta` cruza por debajo → Sell.
- Datos requeridos: `Close` de cada vela, acumulado incrementalmente (EMA de periodo 12 y 26, suavizado exponencial estándar `k=2/(n+1)`, semilla = promedio simple de las primeras `periodo` velas).
- Momento exacto: igual que las estrategias existentes, la señal calculada con `DataSlice` hasta N se ejecuta contra `Velas[N+1]` (RN-13), sin excepción.

**Salida**:
- Condición de cierre: cruce contrario al que abrió la posición (Opción aprobada por auditoría, `ESPECIFICACION_EMA_CROSS_V1.md §2.3`) — sin número máximo de velas, a diferencia de Tres Mosqueteros/MHI Mayoría (que resuelven en como máximo `2+maxMartingalas` ciclos).
- Resultado: gana si el precio de cierre en la vela de resolución es favorable a la dirección de la posición respecto al precio de entrada (Buy gana si `Close_resolución >= Close_entrada`, Sell si `<=`) — comparación contra el precio de entrada real, no contra el color de la última vela (relevante porque una posición puede durar muchas velas).

**Gestión de intentos**: ninguna. Sin martingala, sin reintentos — `MartingalasUsadas = 0` en el 100% de las operaciones, por diseño (D-055).

---

# Supuestos experimentales

- Velas cerradas: la EMA solo usa `Close` de velas ya cerradas.
- UTC: mismo dataset congelado, misma convención horaria que las demás estrategias.
- Sin costes reales, sin ejecución financiera real — mismos supuestos heredados del modelo de posición experimental (Fase 2D).
- Sin lógica específica de BTC/USDT: el cálculo de EMA es genérico sobre cualquier serie de `Close`.
- Parámetros (`PeriodoEmaCorta=12`, `PeriodoEmaLarga=26`) son convención externa de literatura técnica, no calibrados sobre el dataset — mismo criterio que D-030 para el clasificador de régimen.
- **Hallazgo verificado (D-055)**: el catálogo de métricas heredado (`GanoInicial`/`GanoM1`/`GanoM2`/`PctResueltasPorMartingala`) asume martingala. Para esta estrategia, `GanoInicial` captura el 100% de las victorias y `PctResueltasPorMartingala` es siempre 0% — no porque la estrategia "nunca necesitó escalar" (interpretación válida para las estrategias con martingala) sino porque el concepto no aplica. Confirmado en los 6 anexos generados (columna `%Marting` = 0.0% en todas las filas de todas las corridas). D-055: esta es una limitación de generalidad del catálogo de métricas, no un defecto de esta estrategia ni del pipeline.

**Dataset**: `BTCUSDT_2024-01-02_2025-01-02` (real, mismo dataset congelado de Fase 1.0/2C). Hash de origen: `f1a9dcbe72bd...` (idéntico al usado por Tres Mosqueteros/MHI Mayoría).

**Timeframes**: 1m, 5m, 15m, 1h, 4h, 1D — las 6 corridas completaron con `Estado=Success` (verificado, `EjecutorProtocolo`, Fase 1.6-C).

**Configuración**:
- Capital inicial: 1000. Tamaño de operación: 1 (fijo) — mismo modelo de posición que las estrategias existentes.
- `Warmup`: 26 velas (`PeriodoEmaLarga`) — primer uso real del campo `ConfiguracionExperimento.Warmup` en el laboratorio (existente desde el inicio, sin usar hasta esta estrategia). **Hallazgo de implementación**: `Warmup` en `BacktestRunner` es solo una guarda de tamaño mínimo del dataset (`CU-03`), no salta velas del loop — la exclusión real de los primeros ciclos la produce una guarda interna de la estrategia (esperar a tener ambas EMA calculadas), no el parámetro `Warmup` en sí.
- Semilla aleatoria: no aplica (estrategia determinista).

---

# Métricas evaluadas

**Operaciones** (vía `EjecutorProtocolo`, Fase 1.6-C, pipeline completo — no ejecución manual):

| TF | OpCompletas | Eficiencia% | RachaNegMax | %Martingala |
|----|-------------|-------------|-------------|-------------|
| 1m | 19332 | 29.16% | 23 | 0.0% |
| 5m | 3886 | 27.82% | 24 | 0.0% |
| 15m | 1285 | 28.09% | 14 | 0.0% |
| 1h | 290 | 30.34% | 12 | 0.0% |
| 4h | 61 | 39.34% | 7 | 0.0% |
| 1D | 9 | 33.33% | 4 | 0.0% |

**Resolución de intentos**: no aplicable — ver "Supuestos experimentales" (D-055). Las 4 categorías
(`GanoInicial`/`GanoM1`/`GanoM2`/`PerdioAgotando`) siguen sumando 100% por construcción matemática
(partición exhaustiva verificada en los 6 anexos), pero `GanoM1`/`GanoM2` son 0 en todas las
corridas — dato vacío, no un hallazgo sobre la estrategia.

**Tamaño de muestra — nota obligatoria (D-010)**: el volumen de operaciones es un orden de magnitud
menor que Tres Mosqueteros/MHI Mayoría en el mismo timeframe (ej. 1D: 9 operaciones vs. 61 de Tres
Mosqueteros) — confirmado, no solo anticipado (`ESPECIFICACION_EMA_CROSS_V1.md §6`). Comparar
"Eficiencia operacional" entre EMA Cross y las demás estrategias sin mostrar el tamaño de muestra
sería exactamente el tipo de comparación sin contexto que D-010 prohíbe.

**Escenarios de mercado**: evaluado en las 6 corridas exitosas, vistas por régimen de entrada y de
resolución generadas sin cambios manuales (`ReporteEscenariosGenerador`, reutilizado sin
modificación). Partición exhaustiva verificada en los 6 anexos (`REPORTE_EXPERIMENTAL_ESTRATEGIA_
V1_ANEXO_{tf}.md`, generados en `protocolo/resultados/EMACross_20260811T174737Z/`).

**Identidad experimental**: `1F27C4C076D1F5E8586A1F31E62909558F44115F8BA1CC7A8132396957FEC6FC`
(D-049, `IdentidadExperimentoCompleta` sobre Estrategia+Versión+Parámetros+Dataset+ClasificadorV1+ProtocoloV1).

---

# Hipótesis experimental

*(Solo hipótesis — no se evalúa rentabilidad, D-054 lo prohíbe explícitamente.)*

**Comportamiento esperado**: menor frecuencia de señales que las estrategias por cuadrante (cruces
de EMA son más raros que una señal evaluada cada 5 velas), posiciones de duración variable (sin
límite de velas, a diferencia del cierre garantizado en `2+maxMartingalas` ciclos).

**Propósito de esta ficha**: validar cinco preguntas de generalidad del laboratorio (criterio de
Fase 1.6-D), no evaluar si la estrategia es "buena":

1. ¿La ficha de catálogo puede incorporarse con la misma plantilla? — Sí, esta ficha.
2. ¿El pipeline genera identidad correcta? — Sí, hash compuesto calculado y verificable.
3. ¿Los reportes se producen sin cambios manuales? — Sí, mismo `ReporteConsolidadoGenerador`/`ReporteEscenariosGenerador` sin modificar.
4. ¿Las métricas existentes funcionan para otra estructura? — Sí, pero con un hallazgo real (D-055): parte del catálogo (resolución de intentos) queda vacío de información, no falla, pero tampoco aporta nada interpretable para esta estrategia.
5. ¿Aparecen supuestos ocultos específicos de las estrategias actuales? — Sí, exactamente D-055: el vocabulario de martingala en `InfoOperacionResuelta`/`PerfilMultiTf` es un supuesto oculto que esta estrategia expuso.

---

# Resultados observados

*(Completado tras ejecutar Fase 1.6-D vía `EjecutorProtocolo` — pipeline real, no simulado.)*

Las 6 corridas completaron con `Estado=Success`, determinismo verificado (2 corridas por
timeframe comparadas campo por campo, `EjecutorProtocolo.VerificarDeterminismo`), y partición
exhaustiva íntegra en cada anexo de escenarios de mercado. El pipeline construido en Fase 1.6-C
aceptó esta estrategia sin ningún cambio de código en `EjecutorProtocolo.cs`,
`ReporteConsolidadoGenerador.cs`, `ReporteEscenariosGenerador.cs`, `MetricasPorEscenario.cs`,
`AsignadorOperacionRegimen.cs` ni `ClasificadorRegimenV1.cs` — confirma la generalidad buscada por
D-054 para el pipeline en sí.

**Hallazgo no anticipado durante la implementación**: la primera versión de `EstrategiaEmaCross.cs`
recalculaba el historial completo de EMA en cada llamada a `Observar` (O(n) por vela, O(n²) total)
— sobre el timeframe 1m (~500,000 velas) esto no completaba en un tiempo razonable. Se corrigió a
una actualización incremental de EMA (O(1) por vela, O(n) total), verificada matemáticamente
equivalente contra el resultado ya validado con datos sintéticos antes del cambio. Este hallazgo no
es sobre la estrategia en sí — es una lección de implementación sobre el costo de recalcular
indicadores acumulativos vela por vela en datasets grandes, relevante para cualquier futura
estrategia de tipo "Tendencia" que se agregue al laboratorio.

**Hallazgo confirmado (D-055)**: en las 6 corridas, `%Marting = 0.0%` de forma constante — el
catálogo de métricas heredado de Fase 1.2 no distingue "0% porque no hay reintentos" de "0% porque
la estrategia nunca necesitó escalar" (que sí sería informativo para una estrategia con martingala).
Registrado como limitación de generalidad del catálogo, no de esta estrategia ni del pipeline —
decisión explícita de la auditoría de no modificar `ReporteConsolidadoGenerador.cs` en esta fase
(D-055: "Fase 1.6-D busca validar el pipeline, no rediseñar el modelo de métricas").

---

# Limitaciones

- Falla lógica: ninguna detectada — las 6 corridas completan con `Estado=Success`, determinismo
  verificado.
- Falla de generalidad del catálogo (no de la estrategia): "Resolución de intentos" no aplica —
  ver D-055.
- Sin interpretación financiera: mismo alcance que las demás fichas — esta ficha no evalúa
  rentabilidad, riesgo monetario ni recomendación de uso (D-054, explícito).
- Parámetros no calibrados: EMA 12/26 es convención externa, no ajustada sobre este dataset —
  mismo criterio que D-030.
- Muestra pequeña en timeframes largos (1D: 9 operaciones) — cualquier lectura de "Eficiencia
  operacional" en 1D debe considerarse junto al tamaño de muestra (D-010), no de forma aislada.

---

# Conclusión experimental

EMA Cross cumplió su propósito de validación (D-054): el laboratorio —protocolo, pipeline, catálogo
de métricas heredado, análisis por régimen y generación de reportes— generaliza a una estrategia
estructuralmente distinta (Tendencia vs. Patrón, sin cuadrantes, sin martingala, posiciones de
duración variable) sin requerir ningún cambio en los módulos ya congelados de Fases 1.0-1.6-C. El
único hallazgo real de esta validación (D-055) no es un defecto del pipeline sino una limitación de
generalidad conocida y documentada del catálogo de métricas heredado de Fase 1.2, que queda
registrada para una futura fase de rediseño de métricas — explícitamente no resuelta aquí, conforme
al alcance acotado que la auditoría fijó para Fase 1.6-D.
