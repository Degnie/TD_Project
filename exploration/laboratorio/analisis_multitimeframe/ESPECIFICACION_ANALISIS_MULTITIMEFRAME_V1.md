# Especificación del Análisis Multi-Timeframe V1

Estado: **especificación — Fase 1.3 del Caso 1**. Documento de diseño, no implementación. No se
modifica `BacktestRunner`, `IStrategy`, DTOs, estrategias, ni `AnalizadorOperacional.cs` (Fase 1.2,
D-008: el analizador operacional permanece separado del motor y no se altera para dar soporte a
esta fase — esta capa se construye *sobre* sus salidas, no *dentro* de él).

---

## 1. Objetivo

Transformar un conjunto de ejecuciones individuales (una `PerfilMultiTf`/`ReporteOperacional` por
combinación estrategia×timeframe) en un **perfil comparativo por estrategia**: cómo cambia su
comportamiento operacional según la temporalidad evaluada.

```
Estrategia + Timeframe (N ejecuciones individuales)
        ↓
Estrategia completa
        ↓
Comportamiento según temporalidad
```

Pregunta que responde: **"¿Cómo varía el comportamiento operacional de esta estrategia a través de
los timeframes evaluados?"**

Pregunta que NO responde: "¿En qué timeframe esta estrategia genera más dinero?" — eso es ranking
financiero, prohibido por D-009 y por la frontera de Caso 1 ya establecida en Fase 2D.

---

## 2. Qué significa "comparar timeframes"

Comparar no significa ordenar de mejor a peor. Significa exponer, para una misma estrategia, cómo
varían sus métricas operacionales (ya definidas y congeladas en Fase 1.2) a través de las
temporalidades evaluadas, y separar tres preguntas que **no son la misma pregunta** aunque suelen
confundirse:

1. **¿Cuál combinación tuvo el mejor resultado observado?** — un dato puntual (ej. mayor eficiencia
   operacional), sin ajuste por tamaño de muestra ni consistencia.
2. **¿Cuál combinación es más consistente?** — qué tan estable es el comportamiento de la estrategia
   entre timeframes, no cuál es la más alta.
3. **¿Cuál combinación tiene mayor evidencia?** — cuántas operaciones sustentan el resultado, sin lo
   cual "mejor resultado observado" puede ser ruido estadístico, no señal.

Estas tres preguntas se muestran **por separado**, nunca combinadas en un único "ranking" o
"puntaje total" — combinarlas sería reintroducir por la puerta trasera la clasificación cualitativa
que D-005 ya rechazó para una sola métrica, ahora aplicada a la estrategia completa.

---

## 3. Métricas heredadas del analizador operacional (Fase 1.2)

Ninguna métrica nueva se calcula en esta fase. El análisis multi-timeframe **agrupa y presenta**
las salidas ya existentes de `AnalizadorOperacional.Analizar()` (`ReporteOperacional` por cada
combinación estrategia×timeframe), sin tocar sus fórmulas:

| Métrica heredada | Fuente (Fase 1.2) |
|---|---|
| Eficiencia operacional | `ResultadoGeneral.EficienciaOperacionalPct` |
| Intentos completados / incompletos | `ResultadoGeneral.IntentosCompletados`, `IntentoIncompleto` |
| Resolución de intentos (Victoria inicial / M1 / M2 / Pérdida agotando) | `ResolucionDeIntentos.*` |
| `PctResueltasPorMartingala` (solo porcentaje, D-005 sigue vigente aquí) | `ResolucionDeIntentos.PctResueltasPorMartingala` |
| Mayor racha negativa, tope de martingala alcanzado, exposición máxima | `PeoresEscenarios.*` |
| Datos derivados no financieros (Equity, Retorno%) | `DatosDerivadosModeloActual.*` — se muestran agrupados, nunca entran a la comparación operacional (sección 5) |

**Dato nuevo que sí se agrega en esta fase** (no es una métrica del motor, es metadata de la
comparación): el **tamaño de muestra por combinación** (`ResultadoGeneral.IntentosCompletados`), ya
disponible desde Fase 1.2, ahora usado explícitamente como eje de comparación (sección 6).

---

## 4. Cómo mostrar consistencia

"Consistencia" en este documento significa: **¿la eficiencia operacional se mantiene dentro de un
rango similar a través de los timeframes evaluados, o varía ampliamente?** — no significa "es buena
en todos lados".

Ejemplo con datos ya publicados (Tres Mosqueteros, `catalogo_estrategias/TRES_MOSQUETEROS.md`):

| TF | Eficiencia operacional |
|----|--------------------------|
| 1m | 87.08% |
| 5m | 87.89% |
| 15m | 87.67% |
| 1h | 86.52% |
| 4h | 86.29% |
| 1D | 88.52% |

Rango observado: 86.29% – 88.52% (amplitud 2.23 puntos porcentuales) — la eficiencia operacional es
consistente a través de los 6 timeframes evaluados. Esto se muestra como **el rango y la amplitud**,
no como un score único de "consistencia: alta/media/baja" (evitar repetir el error que D-005 ya
corrigió: no clasificar cualitativamente sin una regla aprobada).

**Formato propuesto para el reporte** (diseño, no implementación):
```
Eficiencia operacional por timeframe:
  Mínimo:    86.29% (4h)
  Máximo:    88.52% (1D)
  Amplitud:   2.23 puntos porcentuales
```

Esta misma estructura (mínimo/máximo/amplitud, sin clasificación) se aplica a cualquier métrica
heredada que se quiera comparar entre timeframes (ej. `PctResueltasPorMartingala`).

---

## 5. Cómo evitar ranking financiero

Regla directa, heredada de D-009: **ninguna combinación de `EquityFinal`/`RetornoPct` entre
timeframes se ordena, compara ni presenta como "mejor" o "peor"**.

Los datos derivados del modelo actual (sección 3, última fila) se muestran en una sección propia
del reporte comparativo — igual que en Fase 1.2, nunca mezclados con las métricas operacionales — y
se listan **en el mismo orden que los timeframes** (ej. 1m→1D), no ordenados de mayor a menor
Retorno%. Ordenar por Retorno% es, en sí mismo, una forma de ranking financiero implícito, aunque no
se le llame "ranking".

Ejemplo de lo que el reporte **no debe hacer**:
```
Mejor timeframe por retorno:  1m (9896.54%)     [PROHIBIDO — ranking financiero]
```

Ejemplo de lo que sí puede mostrar:
```
Datos derivados del modelo actual (no comparables financieramente, orden 1m→1D):
  1m:  Retorno% = 9896.54%
  5m:  Retorno% = 3947.09%
  ...
```

---

## 6. Cómo tratar timeframes con diferente cantidad de muestras

Dato real ya publicado (Tres Mosqueteros): 1m tiene 82,475 operaciones completadas; 1D tiene 61 —
una diferencia de ~1350x entre el timeframe más corto y el más largo evaluados en Fase 2C.

**Regla obligatoria**: toda comparación entre timeframes debe mostrar el tamaño de muestra
(`IntentosCompletados`) junto a cualquier métrica comparada, nunca la métrica sola. Esto es una
extensión directa de la observación O-registrada-como-mejora-futura en Fase 1.2 ("Nivel de confianza
de la métrica", sección 4.7 de `ESPECIFICACION_ANALIZADOR_OPERACIONAL_V1.md`), aplicada aquí como
regla obligatoria porque en la comparación multi-timeframe el riesgo de sobreinterpretar una muestra
chica es más alto que en una ficha individual.

Formato obligatorio (no opcional, a diferencia del "nivel de confianza" de Fase 1.2 que quedó como
mejora futura):
```
TF    Eficiencia   Muestra
1m    87.08%       82475 operaciones
5m    87.89%       16829 operaciones
15m   87.67%        5605 operaciones
1h    86.52%        1380 operaciones
4h    86.29%         350 operaciones
1D    88.52%          61 operaciones   ← muestra reducida, interpretar con cautela
```

**No se define en este documento** ningún umbral numérico de "muestra reducida" (ej. "< 100
operaciones es poco confiable") — eso repetiría el mismo error que D-005 corrigió: definir un
umbral cualitativo sin validación externa. Se marca solo como advertencia textual relativa (la
muestra más pequeña del conjunto comparado), no como clasificación absoluta.

---

## 7. Cómo diferenciar mejor resultado / mayor consistencia / mayor evidencia

Las tres preguntas de la sección 2 se calculan de forma independiente y se muestran en secciones
separadas del reporte — nunca combinadas en un solo indicador:

| Pregunta | Cómo se calcula | Ejemplo (Tres Mosqueteros) |
|---|---|---|
| Mejor resultado observado | El timeframe con mayor valor puntual de la métrica elegida (ej. Eficiencia operacional) | 1D: 88.52% |
| Mayor consistencia | El conjunto completo mostrado como rango/amplitud (sección 4) — no es "un timeframe", es una propiedad de la estrategia completa | Amplitud 2.23pp entre 4h y 1D — consistente en todo el rango |
| Mayor evidencia | El timeframe con mayor `IntentosCompletados` | 1m: 82,475 operaciones |

Nótese que en el ejemplo real, **el "mejor resultado observado" (1D) es también el que tiene la
menor evidencia (61 operaciones)** — exactamente el caso que esta separación existe para prevenir:
sin las tres preguntas separadas, se podría concluir erróneamente "1D es el mejor timeframe" cuando
en realidad es el que menos sustento estadístico tiene.

**Interpretación prohibida derivada de esto** (extiende el catálogo de Fase 1.2, sección 6):
```
1D es el mejor timeframe para esta estrategia.              [PROHIBIDO — mezcla "mejor resultado
                                                               observado" con "mayor evidencia" sin
                                                               declarar la diferencia]
```

**Interpretación permitida**:
```
1D tiene la mayor eficiencia operacional observada (88.52%), pero también la menor evidencia
(61 operaciones); 1m tiene la mayor evidencia (82,475 operaciones) con eficiencia comparable
(87.08%).
```

---

## 8. Formato futuro del reporte comparativo

Estructura de salida esperada (diseño, no implementación):

```
Reporte Multi-Timeframe — Estrategia

1. Identidad
   (Estrategia, dataset, hashes — heredado de IdentidadExperimento por cada combinación)

2. Cobertura de timeframes evaluados
   (D-007: distinguir "timeframes derivados disponibles" vs. "timeframes evaluados por backtest",
   igual que en Fase 1.2 — no repetir aquí la ambigüedad que D-007 ya resolvió)

3. Métricas operacionales por timeframe
   (Tabla heredada de Fase 1.2: Eficiencia, Resolución de intentos, Peores escenarios — una fila
   por timeframe evaluado, con columna de tamaño de muestra obligatoria, sección 6)

4. Consistencia
   (Rango/mínimo/máximo/amplitud por métrica, sección 4 — sin clasificación cualitativa)

5. Mejor resultado / Mayor consistencia / Mayor evidencia
   (Tres respuestas separadas, sección 7 — nunca combinadas en un ranking único)

6. Datos derivados del modelo actual (no financieros)
   (En orden de timeframe, nunca ordenados por valor — sección 5)

7. Limitaciones
   (Heredadas de Fase 1.0/1.1/1.2 + limitación propia de esta fase: solo compara timeframes
   realmente evaluados por backtest, D-007)
```

---

## Decisiones registradas por auditoría (2026-08-11)

**D-010 — Tamaño de muestra obligatorio en toda comparación** (sección 6): ✅ Aprobada. A
diferencia de "Nivel de confianza de la métrica" en Fase 1.2 (mejora futura, opcional, afecta
interpretación avanzada), en Fase 1.3 mostrar el tamaño de muestra es **parte del contrato de
presentación** porque evita una mala interpretación inmediata. Toda comparación multi-timeframe
debe incluir: métrica mostrada, cantidad de operaciones, periodo analizado.

**D-011 — Métrica principal de comparación**: ⏳ Pendiente. Correctamente no definida. Eficiencia
operacional es hoy la métrica oficial del Caso 1, pero no debe asumirse como el único eje posible
de "mejor resultado observado" (sección 7). Futuros candidatos registrados: estabilidad,
dependencia de escalado, cantidad de muestras, peor escenario. No resolver todavía.

**D-012 — Umbral de muestra reducida**: ⏳ Pendiente. Correcto no establecer un umbral fijo (ej.
"<100 operaciones = insuficiente") sin evidencia — el umbral depende de estrategia, frecuencia y
distribución de eventos. Mantenerlo abierto, mismo criterio que D-005.

**D-013 — Timeframes disponibles sin backtest**: ⏳ Pendiente. Debe mantenerse la separación
Dataset existe ≠ Estrategia evaluada (extiende D-007 a esta fase). Un reporte futuro debe
distinguir explícitamente "Disponible" (existe información temporal) de "Evaluado" (existe
ejecución reproducible) — no resolver la extensión de cobertura en esta fase.

**Observación — Comparabilidad estadística entre timeframes** (mejora futura, no bloquea Fase 1.3):
dos resultados como "80% eficiencia con 50 operaciones" y "75% eficiencia con 50.000 operaciones" no
deberían tratarse como comparables en el mismo plano — mostrar ambos números lado a lado (D-010) es
necesario pero no suficiente; una comparación estadísticamente rigurosa (ej. intervalos de
confianza, significancia) queda registrada como posible evolución futura, fuera del alcance de Fase
1.3.

---

## Fuera de alcance (respetado)

No se realizan cambios en `BacktestRunner`, `IStrategy`, DTOs, estrategias, `AnalizadorOperacional.cs`
ni ningún archivo de Fase 1.2. No se implementa clasificador de escenarios de mercado (D-006, sigue
pendiente e independiente de esta fase). No se calcula ranking financiero entre timeframes ni entre
estrategias. Este documento es la única salida de esta fase.

---

## Criterio de cierre de Fase 1.3

- ✓ Especificación formal del análisis multi-timeframe creada.
- ✓ Definido qué significa "comparar timeframes" (sección 2) y separadas las tres preguntas que
  suelen confundirse (mejor resultado / consistencia / evidencia).
- ✓ Métricas heredadas del analizador operacional identificadas, sin cálculos nuevos del motor
  (sección 3).
- ✓ Formato de consistencia definido sin clasificación cualitativa no aprobada (sección 4).
- ✓ Regla explícita para evitar ranking financiero (sección 5).
- ✓ Tratamiento obligatorio del tamaño de muestra dispar entre timeframes (sección 6, con el caso
  real ya documentado de 61 vs. 82,475 operaciones).
- ✓ Separación mejor resultado / mayor consistencia / mayor evidencia con ejemplo real que muestra
  por qué la separación es necesaria (sección 7).
- ✓ Formato futuro del reporte diseñado (sección 8).
- ✅ Auditoría aprueba la especificación — **Diseño aprobado** (2026-08-11), D-010 incorporada como
  decisión de diseño. D-011, D-012, D-013 quedan pendientes para una fase posterior sin bloquear el
  cierre. Implementación autorizada bajo el orden Paso 1 (módulo nuevo en
  `exploration/laboratorio/analisis_multitimeframe/`, sin tocar motor/estrategias/contratos) →
  Paso 2 (`PerfilMultiTimeframe` construido sobre `PerfilMultiTf` + `AnalizadorOperacional`, con
  tamaño de muestra obligatorio) → Paso 3 (pruebas con Tres Mosqueteros — validar 1m alta muestra,
  1D baja muestra, separación correcta — y MHI Mayoría — validar estructura completa y comparación
  consistente).
- ✅ **Fase 1.3 cerrada por auditoría (2026-08-11)** — implementación completada en
  `exploration/laboratorio/analisis_multitimeframe/` (`PerfilMultiTimeframe.cs`, `Tests.cs`,
  `Program.cs`), 6/6 pruebas pasan reproduciendo cifras ya publicadas en el catálogo, 0 cambios en
  `src/`/`tests/`. La prueba clave (`TresMosqueterosSeparacionMejorResultadoVsEvidencia`) confirma
  que "mejor resultado observado" (1D, 88.52%, 61 operaciones) y "mayor evidencia" (1m, 87.08%,
  82,475 operaciones) son timeframes distintos, sin fusionarse en un ranking. Decisiones
  registradas en el cierre:
  - **D-014 — Multi-timeframe sin ranking implícito**: aprobado. El laboratorio no genera un
    "mejor timeframe" automático; presenta dimensiones separadas (mejor resultado / consistencia /
    mayor evidencia), y el orden de entrada se preserva siempre (nunca se reordena por valor).
  - **D-015 — Analizadores compuestos por capas**: aprobado. Arquitectura confirmada:
    `Backtest → PerfilMultiTf → AnalizadorOperacional → ComparadorMultiTimeframe → Reporte`. Cada
    analizador consume los resultados del anterior sin duplicar cálculos; regla permanente para
    futuros analizadores del laboratorio.
  - **O-003 — Persistencia formal del perfil multi-timeframe** (JSON/Markdown/reporte visual):
    observación no bloqueante, mejora futura.
  - **O-004 — Visualización** (matriz estrategia × timeframe, distribución de muestras, evolución
    de eficiencia): observación no bloqueante, mejora futura.
