# Definición de Estados de Régimen V1

Estado: **especificación de subfase — D-028, bloqueante para el congelamiento de
`ClasificadorRegimenV1`**. Este documento no selecciona una opción — responde las 4 preguntas que
auditoría fijó para poder decidir, y dejar explícitamente esa decisión como acción posterior tuya,
no inferida aquí. No se modifica `DECISION_CLASIFICADOR_REGIMEN_V1.md` (Candidato B sigue
propuesto, sección 4 de ese documento) — este documento resuelve el diseño de **qué estados**
producirá el clasificador final, no **cuál familia** de clasificador se usa (eso ya está decidido
como propuesta, Fase 1.4-B).

---

## 1. ¿El laboratorio necesita "Ambiguo"?

**Evidencia ya generada, no opinión nueva**: el candidato C (Retorno+Volatilidad) implementó
"Ambiguo" en Fase 1.4-A y produjo entre 12.33% y 19.00% de ventanas en esa categoría, según el
timeframe (`RESULTADO_EVALUACION_CLASIFICADORES_REGIMEN_V1.md §1`). Es decir: cuando existe un
mecanismo explícito para "no sé", entre 1 de cada 8 y 1 de cada 5 ventanas del dataset real de
BTC/USDT cae ahí — no es una categoría vacía en la práctica.

El candidato B (ADX+DI, propuesto) **no tiene ese mecanismo** en su implementación exploratoria —
cualquier ventana con ADX bajo el umbral se clasificó como "Lateral", sin distinguir si el precio
estaba genuinamente en rango o si la señal era simplemente insuficiente para decidir.

**Esto no responde por sí solo la pregunta** (eso depende de la opción elegida en la sección 5),
pero sí establece que "Ambiguo" no es un caso marginal hipotético — es una porción medible y no
trivial del dataset real cuando se mide explícitamente.

---

## 2. ¿Qué diferencia matemática existe entre Lateral y Ambiguo?

Formalizando la distinción conceptual que ya trazó la auditoría (Caso 1 / Caso 2):

| | Lateral | Ambiguo |
|---|---|---|
| **Definición conceptual** | Baja fuerza direccional, movimiento contenido, equilibrio relativo — el mercado "decide" no tener tendencia | Señales contradictorias o evidencia insuficiente para decidir — el clasificador "no sabe", no es que el mercado haya decidido algo |
| **Ejemplo** | Precio oscilando dentro de un rango estable | Transición entre regímenes; cambio de tendencia en curso |
| **Traducción al candidato C (ya implementado)** | `PendienteNormalizada` baja **y** `RangoRelativo` bajo (sección 4 de `ESPECIFICACION_ANALISIS_ESCENARIOS_MERCADO_V1.md`: sin tendencia, tranquilo) | `PendienteNormalizada` baja **pero** `RangoRelativo` alto (sin tendencia, con dispersión — la señal es contradictoria, no ausente) |
| **Traducción posible a ADX+DI (no implementada, solo conceptual)** | ADX bajo, DI+ y DI- cercanos entre sí (sin fuerza y sin sesgo direccional) | ADX bajo pero con cruces frecuentes de DI+/DI- dentro de la ventana de cálculo (sin fuerza, pero con dirección inestable/oscilante) |

**Diferencia matemática resumida**: Lateral = ausencia de señal + ausencia de ruido. Ambiguo =
ausencia de señal + presencia de ruido o contradicción. No es la misma condición, aunque ambas
compartan "ADX bajo" o "pendiente baja" como primer filtro.

---

## 3. ¿Qué hace el sistema cuando no existe evidencia suficiente?

**Regla ya establecida y que este documento no cambia** (`ESPECIFICACION_ANALISIS_ESCENARIOS_MERCADO_V1.md
§5`): ninguna ventana se descarta silenciosamente. Toda vela pertenece a exactamente una categoría.

Lo que sí depende de la opción elegida (sección 5) es **cuál** es esa categoría cuando la evidencia
es insuficiente:

- Si se adopta un estado "Ambiguo" (Opción B) o una "zona de exclusión" (Opción C): la ventana se
  marca explícitamente como no-clasificada-con-confianza, y **queda excluida de cualquier
  comparación posterior entre estrategia y régimen** — mismo principio que las velas parciales en
  Fase 2B (incluir y marcar, pero excluir de backtest cuando corresponde) y que las operaciones
  incompletas en Fase 1.2 (categoría separada, nunca mezclada con ganada/perdida).
- Si se adopta el modelo de 3 estados fijos (Opción A): la ventana se fuerza a "Lateral" — el
  sistema no tiene forma de distinguir, en el reporte final, entre "el mercado estaba realmente en
  rango" y "el clasificador no tuvo evidencia suficiente". Esta es la limitación explícita que
  motivó D-028.

---

## 4. ¿Cómo afecta esto al reporte para usuario?

Bajo cualquier opción con estado explícito de incertidumbre (B o C), el reporte multi-timeframe /
por escenario (heredero de D-010, Fase 1.3) debe mostrar una fila adicional junto a
Alcista/Bajista/Lateral:

```
Régimen        Operaciones   Eficiencia   Muestra
Alcista        ...           ...          ...
Bajista        ...           ...          ...
Lateral        ...           ...          ...
Ambiguo/       ...           ...          ...     ← excluido de comparación régimen-estrategia,
No clasificado                                       mostrado solo como transparencia de cobertura
```

Bajo la Opción A (3 estados fijos), esta fila no existe — toda operación que hubiera caído en
"Ambiguo" bajo otra opción se reporta silenciosamente como parte de "Lateral", sin forma de que el
usuario distinga ambos casos en el reporte final. Esto es exactamente el riesgo que D-028 señaló:
un reporte que no puede comunicar "no lo sé" tiende a comunicar certeza donde no la hay.

---

## 5. Las 3 opciones (registradas por auditoría, sin selección en este documento)

| Opción | Estados | Ventaja | Riesgo |
|---|---|---|---|
| **A — Tres estados fijos** | Alcista, Bajista, Lateral | Simple, fácil de explicar | Obliga a clasificar toda ausencia de tendencia como Lateral, incluso cuando la evidencia es insuficiente (no solo cuando el mercado está genuinamente en rango) |
| **B — Cuatro estados** | Alcista, Bajista, Lateral, Ambiguo | Representa mejor la incertidumbre; evita forzar clasificación | Mayor complejidad; requiere definir con precisión matemática cuándo aparece Ambiguo (sección 2 de este documento da el marco conceptual, no un umbral) |
| **C — Tres estados + zona de exclusión** | Alcista, Bajista, Lateral; periodos insuficientemente claros quedan sin clasificar (no es un cuarto "estado de mercado", es ausencia de clasificación) | Evita inventar información — no le asigna al mercado una etiqueta que el mercado no "tiene" | Similar complejidad de implementación que B; la diferencia con B es principalmente conceptual (¿"Ambiguo" es un régimen de mercado real, o es una limitación del instrumento de medición?) |

**Diferencia entre B y C, para que la decisión sea informada**: en la Opción B, "Ambiguo" es una
categoría de régimen igual de válida que las otras tres — se interpreta como una propiedad del
mercado ("el mercado está en transición"). En la Opción C, la "zona de exclusión" no es un régimen
de mercado — es una admisión de que el clasificador, con la evidencia disponible, no puede opinar;
conceptualmente más cercano a "dato faltante" que a "una cuarta condición de mercado". Ambas
producen el mismo comportamiento práctico en el reporte (sección 4), pero difieren en cómo se
comunica el significado al usuario.

---

## Impacto sobre el Candidato B (ADX+DI) propuesto

Este hallazgo **no invalida la propuesta** de `DECISION_CLASIFICADOR_REGIMEN_V1.md` — sigue siendo
un riesgo de diseño pendiente, no una debilidad definitiva del candidato (tal como clasificó
auditoría). La implementación exploratoria de ADX+DI usada en Fase 1.4-A tendría que extenderse con
un mecanismo de "Ambiguo"/"zona de exclusión" si se aprueba la Opción B o C — actualmente no lo
tiene. Esa extensión no se implementa en este documento; corresponde a la fase de definición de
parámetros oficiales, después de que se apruebe una opción aquí.

---

## Fuera de alcance (respetado)

No se selecciona ninguna de las 3 opciones. No se modifica `ClasificadorAdxExperimental.cs` ni
ningún código de Fase 1.4-A. No se fija ningún umbral matemático para distinguir Lateral de Ambiguo
bajo ADX+DI — eso depende de qué opción se apruebe primero.

---

## Criterio de cierre de D-028

- ✓ Respondido si el laboratorio necesita "Ambiguo", con evidencia cuantitativa ya generada (12-19%
  del dataset bajo el candidato C).
- ✓ Formalizada la diferencia matemática entre Lateral y Ambiguo (sección 2).
- ✓ Definido qué hace el sistema ante evidencia insuficiente, bajo cada opción (sección 3).
- ✓ Definido el impacto sobre el reporte de usuario (sección 4).
- ✓ Las 3 opciones (A/B/C) documentadas con ventajas/riesgos, sin selección impuesta.
- ⏳ Selección de opción — pendiente de decisión explícita antes de continuar con parámetros
  oficiales de `ClasificadorRegimenV1`.
