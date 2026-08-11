# Decisión — Clasificador de Régimen Oficial V1

Estado: **PROPUESTA — PENDIENTE DE APROBACIÓN** (Fase 1.4-B). Este documento no cierra Fase 1.4.
Contiene una recomendación técnica explícita (D-027), no una decisión congelada — el candidato
propuesto y sus parámetros requieren aprobación explícita antes de crear `ClasificadorRegimenV1`
como versión oficial.

Fuente de evidencia: `RESULTADO_EVALUACION_CLASIFICADORES_REGIMEN_V1.md` (Fase 1.4-A, Paso 4) y
`ANALISIS_RESULTADOS_CLASIFICADORES_REGIMEN_V1.md` (análisis de auditoría), ambos ya aprobados.

**D-028 — Definición de estados de régimen** (auditoría, 2026-08-11): ✅ **Aprobada — Opción B
(cuatro estados)**. Modelo oficial: **Alcista, Bajista, Lateral, Ambiguo**. Ver
`DEFINICION_ESTADOS_REGIMEN_V1.md` para el análisis completo de las 3 opciones consideradas.
Justificación de la decisión: (1) mantiene más información experimental — forzar todo a Lateral
mezcla "mercado estable sin dirección" con "mercado sin estructura clara", dos condiciones que
pueden afectar una estrategia de forma distinta; (2) más honesto para un usuario no técnico — "la
estrategia fue evaluada en periodos sin señal clara" es más transparente que forzar esos periodos
dentro de "lateral"; (3) evita sobreinterpretación — Ambiguo evita afirmar "el mercado estaba
lateral" cuando en realidad solo se sabe "no hubo evidencia suficiente de tendencia".

**Consecuencia sobre el Candidato B (ADX+DI, sección 4)**: la aprobación de D-028 **no selecciona
oficialmente a ADX+DI** — sigue siendo propuesta. Lo que establece es una condición: si ADX+DI
continúa como candidato, **debe ser capaz de producir los cuatro estados** (Alcista, Bajista,
Lateral, Ambiguo) o justificar formalmente una representación alternativa antes de congelarse. La
implementación exploratoria usada en Fase 1.4-A (`ClasificadorAdxExperimental.cs`) solo produce
tres estados — no distingue Lateral de Ambiguo (Riesgo 2, sección 3) — por lo que necesita
extenderse antes del congelamiento. Esa extensión se resuelve en
`PARAMETRIZACION_CLASIFICADOR_REGIMEN_V1.md` (Paso 2), no en este documento.

---

## 1. Resumen de candidatos

| Candidato | Enfoque | Estado tras análisis |
|---|---|---|
| A — EMA | Pendiente de una media móvil exponencial | ⚠️ Evidencia fuerte de limitación experimental (D-025) — 99.62%-100% Lateral en 1m-1h con la configuración exploratoria usada |
| **B — ADX + DI** | Fuerza direccional (ADX) + dirección (+DI/-DI), Wilder | 🟢 **Propuesto** — mejor evidencia consolidada |
| C — Retorno + Volatilidad | `PendienteNormalizada` + `RangoRelativo` sobre ventana | 🟡 Mantener en evaluación — métricas de duración no normalizadas entre timeframes (D-026) |

---

## 2. Evidencia observada

### 2.1 Objetivo conceptual

El objetivo de Fase 1.4 es separar tendencia alcista, tendencia bajista y ausencia de tendencia
(lateralidad) — ver `ESPECIFICACION_ANALISIS_ESCENARIOS_MERCADO_V1.md §1`. ADX+DI es, de los tres
candidatos, el único diseñado específicamente para esa separación: el ADX mide fuerza direccional
(hay tendencia / no hay tendencia) de forma independiente del signo, y +DI/-DI aportan la dirección.
EMA y Retorno+Volatilidad infieren tendencia indirectamente a partir de la pendiente de precio, sin
un mecanismo dedicado a distinguir "fuerza" de "dirección".

### 2.2 Evidencia cuantitativa (ya registrada en Fase 1.4-A)

| Dimensión | B — ADX+DI |
|---|---|
| Cambios de régimen (estabilidad) | 4.55% – 5.81% (amplitud 1.26pp entre timeframes — la menor de los 3 candidatos) |
| Cobertura | 100.00% en los 6 timeframes evaluados |
| Distribución | Lateral 42%-59%, Alcista/Bajista repartidos de forma razonablemente simétrica en todos los timeframes — el único candidato con presencia real de las 3 categorías en TODOS los timeframes evaluados (A solo la tiene en 4h/1D) |
| Determinismo | Confirmado en las 6 combinaciones (ejecución doble, comparación campo a campo) |

**Ventaja frente a A**: A queda descalificado por D-025 en la práctica — no discrimina régimen en
4 de los 6 timeframes evaluados (99.62%-100% Lateral en 1m/5m/15m/1h). B sí distingue las 3
categorías de forma consistente en las 6 escalas.

**Ventaja frente a C**: B tiene menor amplitud de variación entre timeframes en su métrica de
estabilidad (1.26pp vs. 8.74pp), y no depende de normalizar "duración media" por tamaño de ventana
de muestreo — problema abierto y no resuelto para C (D-026).

---

## 3. Riesgos aceptados por esta propuesta

**Riesgo 1 — Parámetros exploratorios, no oficiales**: los valores usados en la evaluación
(`PeriodoAdxExploratorio = 14`, `UmbralAdxTendenciaExploratorio = 25m`) son configuración
exploratoria (D-022) — sirvieron para comparar candidatos, no fueron elegidos ni validados como
definitivos. El valor 25 corresponde a una convención de literatura técnica, no a un ajuste sobre
BTC/USDT, pero **no ha pasado por una fase de congelamiento** y no se declara oficial en este
documento.

**Riesgo 2 — Definición formal pendiente, ahora con requisito explícito (D-028)**: antes de
congelar `ClasificadorRegimenV1` deben definirse: periodo de ADX, ventana de DI (en esta
implementación exploratoria comparten el mismo periodo — a confirmar si deben separarse), umbral de
tendencia, y **el criterio matemático para distinguir Lateral de Ambiguo**, ya que D-028 aprobó el
modelo de 4 estados como oficial. La implementación exploratoria de B (`ClasificadorAdxExperimental.cs`)
solo produce 3 estados — este riesgo pasa de "punto a resolver" a **requisito bloqueante**: ADX+DI
no puede congelarse sin extender su lógica para producir el cuarto estado.

**Riesgo 3 — Sin validación de impacto sobre estrategias**: esta propuesta se basa exclusivamente
en propiedades del clasificador como instrumento (D-016) — ninguna estrategia fue evaluada dentro
de los segmentos que produciría B. Esa validación ocurre, por diseño, únicamente después del
congelamiento (`ESPECIFICACION_ANALISIS_ESCENARIOS_MERCADO_V1.md §6`, paso 4-5) — nunca antes, para
no introducir selección retrospectiva.

**Riesgo adicional no cubierto por la evaluación de Fase 1.4-A**: O-005 (sensibilidad del
clasificador ante cambios pequeños de parámetro, ej. ADX 24 vs. 26) sigue sin evaluarse — queda
como mejora futura, no bloqueante para esta propuesta pero relevante antes del congelamiento final.

---

## 4. Candidato seleccionado

**Estado: PROPUESTA — PENDIENTE DE APROBACIÓN**

**Propuesta: Candidato B — ADX + DI**

Basada únicamente en criterios del clasificador (objetivo conceptual, evidencia observada,
riesgos aceptados — secciones 1-3). No basada en resultado de ninguna estrategia.

---

## 5. Parámetros oficiales

**PENDIENTE DE DEFINICIÓN.**

No se fija `ADX = 25` ni ningún otro valor como oficial en este documento. El valor 25 fue usado
como configuración exploratoria (convención técnica, no ajustada a BTC/USDT), pero requiere pasar
formalmente por la fase de congelamiento antes de convertirse en parámetro oficial de
`ClasificadorRegimenV1`.

---

## 6. Limitaciones

- La propuesta no resuelve D-018 (umbral numérico) ni D-019 (tamaño de ventana) — quedan abiertas,
  ahora en el contexto específico de B (periodo ADX, umbral de tendencia).
- No resuelve el tratamiento de zonas Lateral/Ambiguo dentro de B (ver Riesgo 2).
- No incluye evaluación de sensibilidad (O-005).
- No valida el clasificador contra ninguna estrategia — por diseño, corresponde a una etapa
  posterior al congelamiento.

---

## 7. Versión inicial congelada

**No aplica todavía.** `ClasificadorRegimenV1` no se crea en este documento. Su creación requiere:
(1) auditoría de esta propuesta, (2) aprobación explícita del candidato B, (3) definición de
parámetros oficiales (sección 5), en ese orden — ninguno de los tres pasos está completo.

**D-029 — Compatibilidad entre clasificador y modelo de estados** (auditoría, 2026-08-11): ✅
Aprobado. Regla: ningún clasificador puede convertirse en oficial si no cumple el modelo de estados
aprobado (D-028) o si no existe una justificación formal aprobada para una representación
equivalente. Consecuencia directa: ADX+DI permanece como **candidato propuesto**, no clasificador
oficial, mientras sigan pendientes estados (parcialmente resuelto, ver
`PARAMETRIZACION_CLASIFICADOR_REGIMEN_V1.md` — fórmula de Ambiguo definida, valor de
`UmbralSesgoDI` aún sin fijar), parámetros, comportamiento y validación.

**Paso 2 completado**: `PARAMETRIZACION_CLASIFICADOR_REGIMEN_V1.md` resuelve periodo ADX (14,
estándar Wilder), ventana DI (igual al periodo ADX), umbral de tendencia (25, convención de
literatura), dirección, Lateral y Ambiguo (fórmula matemática completa, con `UmbralSesgoDI`
explícitamente pendiente por ausencia de convención externa), tratamiento de bordes (4 casos) y
dependencia del timeframe (periodo uniforme propuesto, sin asumir equivalencia automática entre
escalas). Ningún valor de esa tabla es oficial todavía — todos marcados "Propuesto" o "Pendiente".

**D-030 — Parámetros convencionales pueden entrar como propuesta oficializable** (auditoría,
2026-08-11): ✅ Aprobado. Regla: un parámetro puede avanzar como "Propuesto" (no "Pendiente") si
tiene referencia externa, no fue seleccionado mirando resultados del laboratorio, y queda pendiente
de validación experimental — formaliza la distinción ya aplicada en la sección 5 de
`PARAMETRIZACION_CLASIFICADOR_REGIMEN_V1.md` (Periodo ADX=14 y Umbral=25 como "Propuesto";
`UmbralSesgoDI` como "Pendiente" por ausencia de convención externa equivalente) como regla general
del laboratorio, aplicable a futuros parámetros de cualquier clasificador o estrategia.

**D-031 — Método UmbralSesgoDI** (auditoría, 2026-08-11): ✅ **Aprobado — Opción B, umbral
relativo**. Fórmula base: `|DI+ - DI-| / (DI+ + DI-)`. Motivo: normaliza el sesgo direccional
respecto a la actividad total del indicador, evitando que una diferencia absoluta pequeña se
interprete igual en contextos de baja actividad y de alta actividad (asimetría identificada en
`DEFINICION_UMBRAL_SESGO_DI_V1.md §2`). **Aclaración de alcance**: D-031 aprueba únicamente la
familia matemática (SesgoDI relativo) — no aprueba todavía el valor del umbral, la clasificación
final de Lateral/Ambiguo con ese valor, ni la implementación. Ver
`DEFINICION_VALOR_UMBRAL_SESGO_DI_V1.md` (segunda parte del Paso 3-A) para la definición del valor.

| Parámetro | Estado |
|---|---|
| Periodo ADX = 14 | Propuesto |
| Umbral ADX = 25 | Propuesto |
| Método SesgoDI = relativo | ✅ Aprobado (D-031) |
| Valor de `UmbralSesgoDI` | ⏳ Pendiente |

---

## Criterio de cierre de Fase 1.4-B (pendiente)

- ✓ Resumen de candidatos con evidencia consolidada.
- ✓ Propuesta técnica explícita (D-027), con justificación basada solo en propiedades del
  clasificador.
- ✓ Riesgos aceptados declarados explícitamente (sección 3).
- ✅ Auditoría de la propuesta — realizada (2026-08-11).
- ✅ D-028 resuelto — modelo de 4 estados (Alcista/Bajista/Lateral/Ambiguo) aprobado como oficial.
- 🟡 Aprobación del candidato — **parcial**: B sigue propuesto, ahora con el requisito explícito de
  producir los 4 estados de D-028 antes de congelarse.
- ⏳ Definición de parámetros oficiales — pendiente. **Siguiente paso**: `PARAMETRIZACION_CLASIFICADOR_REGIMEN_V1.md`
  (periodo ADX, cálculo DI, umbral tendencia, criterio Lateral, criterio Ambiguo — sin asumir
  `ADX > 25` como regla final).
- ⏳ Creación de `ClasificadorRegimenV1` como versión congelada — pendiente, posterior a la
  parametrización oficial. Debe quedar separado de `ClasificadorAdxExperimental.cs`: el
  experimental es exploración, `ClasificadorRegimenV1` es el contrato oficial del laboratorio.
