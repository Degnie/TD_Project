# Propuesta — Expansión de Corpus Comparativo V2 (Caso 5C, previo a reevaluar Capa 2)

Estado: **documento de apertura — previo a cualquier ejecución**. Continúa directamente
`AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V1.md` §5 (conclusión: "evidencia todavía insuficiente") y
`PROPUESTA_CAMPANA_CORPUS_CASO5C_V1.md` (campaña V1, ya ejecutada y congelada en commit `c83c7a6`).
No es una fase nueva del ciclo D-N — no introduce decisiones, componentes, ni contratos nuevos.
Mismo principio rector que V1: **generación de evidencia, no optimización**.

**Objetivo de esta expansión**: cerrar, en la medida de lo posible con lo ya disponible en el
repositorio, las 4 limitaciones concretas que la auditoría de V1 identificó (§4 de
`AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V1.md`):
1. Sin repetición de ninguna combinación.
2. Sin diversidad de dataset/instrumento.
3. Solo 2 de 6 estrategias congeladas representadas.
4. Sin casos de corrida fallida representados.

---

## 1. Qué limitaciones pueden cerrarse con lo ya disponible (verificado contra código)

**Estrategias adicionales disponibles, no linkeadas todavía en ningún `.csproj` de Caso 5**
(verificado en `exploration/`, constructores leídos directamente):
- `EstrategiaZScoreReversion(ventana: int, umbralEntrada: decimal, umbralSalida: decimal)` — valores
  ya congelados en `caso3/TestsCaso3.cs`: `ventana=5, umbralEntrada=2.0m, umbralSalida=0.5m` (hay
  también un caso con `ventana=20`, mismo `umbralEntrada`/`umbralSalida`).
- `EstrategiaNeutral(ciclo: int)` — valor ya congelado en `caso3/TestsEstrategiaNeutral.cs`:
  `ciclo=10`.
- `EstrategiaVolumenBreakout()` — sin parámetros obligatorios (constructor sin argumentos ya usado
  en `caso3/TestsEstrategiaVolumenBreakout.cs`).
- `EstrategiaMhiMayoria(maxMartingalas: int)` — valor ya usado en `Fase15.cs`/`evaluacion_multi_tf/
  Program.cs`: `maxMartingalas=2`, mismo patrón que `EstrategiaTresMosqueteros`.

**Con esto, las 6 estrategias congeladas del laboratorio quedan disponibles para la campaña** —
cierra la limitación 3 por completo, usando en todos los casos parámetros ya congelados en otras
fases, sin inventar ninguno nuevo (D-030).

**Dataset/instrumento**: sigue existiendo un único dataset real en el repositorio (`BTCUSDT`,
`2024-01-02`–`2025-01-02`). **La limitación 2 no puede cerrarse por completo con lo ya
disponible** — no hay un segundo instrumento ni un segundo rango temporal descargado. Lo que sí
puede ampliarse dentro del mismo dataset es la **diversidad temporal vía timeframe** (ya presente
parcialmente en V1: 3 de 13 disponibles) — cubrir más de los 13 timeframes existentes no es
diversidad de instrumento, pero sí diversidad temporal adicional dentro de lo que D-119 evalúa.

**Repetición (limitación 1)**: cerrable por diseño — repetir exactamente las mismas combinaciones
de V1 (mismas estrategias, mismos timeframes, mismos gestores) una segunda vez es información
válida por sí sola (confirma reproducibilidad del mecanismo, D-116/D-117, ya verificada a nivel de
componente por P4 de Caso 5C Capa 1, pero nunca ejecutada como repetición real de campaña).

**Corridas fallidas (limitación 4)**: cerrable usando el mismo mecanismo que
`TestsComparadorGestores.cs`/P7 ya usa — un `NombreDataset` inexistente fuerza `Estado: Failed` en
las 3 filas de una comparación, sin necesitar ningún dato nuevo ni ninguna estrategia que falle por
diseño.

---

## 2. Diseño de la expansión — 3 sub-campañas independientes, cada una cierra una limitación

**Sub-campaña A — Cobertura de estrategias** (cierra limitación 3): las 4 estrategias no cubiertas
en V1 (`ZScoreReversion`, `Neutral`, `VolumenBreakout`, `MhiMayoria`), cada una con los 3 timeframes
ya usados en V1 (`15m`/`1h`/`1D`) y los mismos 3 gestores — mismo patrón de matriz que V1.

```
ZScoreReversion (ventana=5, umbralEntrada=2.0, umbralSalida=0.5)
Neutral (ciclo=10)
VolumenBreakout ()
MhiMayoria (maxMartingalas=2)
    |
    +-- 15m / 1h / 1D
          +-- FixedFractional / FixedRisk / VolatilitySizing
```

4 estrategias × 3 timeframes = 12 comparaciones nuevas, 36 corridas internas.

**Sub-campaña B — Repetición** (cierra limitación 1): re-ejecutar exactamente la matriz completa de
V1 (2 estrategias × 3 timeframes ya cubiertas) una segunda vez, sin cambiar ningún parámetro.

6 comparaciones repetidas, 18 corridas internas — permite comparar, en la auditoría posterior, si
`IDENTIDAD_COMPARACION.json` y el perfil relativo observado en `COMPARACION_GESTORES_V1.md` se
mantienen estables entre la primera y la segunda ejecución.

**Sub-campaña C — Evidencia parcial** (cierra limitación 4): 1 comparación con
`NombreDataset` inexistente (mismo patrón que P7 de `TestsComparadorGestores.cs`), cualquier
estrategia/timeframe ya usado — no necesita variar, solo demostrar que el corpus puede contener y
persistir una comparación con las 3 filas en `Failed`.

1 comparación, 3 corridas internas (todas fallidas por diseño, no por error).

**Total de la expansión**: 12 + 6 + 1 = **19 comparaciones nuevas**, sumadas a las 6 de V1 = 25
comparaciones acumuladas en el corpus tras esta expansión.

**Diversidad de timeframe adicional, fuera de las 3 sub-campañas obligatorias**: opcional, no
incluida en el alcance mínimo de esta propuesta — si el auditor quiere ampliarla, es una
sub-campaña D separada, a decidir en la especificación, no aquí.

---

## 3. Qué debe usarse (sin flujo paralelo, igual que V1)

Exclusivamente `ComparadorGestores.Comparar` (Caso 5B) y `PersistidorComparaciones.Persistir` (Caso
5C Capa 1), sin ninguna modificación — mismo criterio que V1. El mecanismo de campaña
(`campana_corpus/`) se reutiliza, ampliando su matriz declarada, no se reemplaza por uno nuevo.

---

## 4. Qué no debe hacerse (reafirmado de V1, sin relajar)

- No seleccionar un "ganador" de ninguna comparación, nueva o repetida.
- No calcular ninguna recomendación ni criterio de orden.
- No ajustar ningún parámetro de gestor ni de estrategia observando resultados de V1 o de esta
  expansión (D-030 — todos los parámetros de §1 ya estaban congelados en otras fases antes de esta
  propuesta).
- No modificar ninguna de las 6 estrategias, ningún gestor, `ComparadorGestores`,
  `PersistidorComparaciones`, `RenderizadorComparacionGestores`.
- No repetir ni descartar una combinación en función de su resultado.
- No tocar `src/`/`tests/`.
- No interpretar el corpus resultante — eso es tarea de la auditoría posterior, no de la ejecución.

---

## 5. Objetivo mínimo de esta expansión

No se define aquí ningún valor de D-119. El objetivo es que la auditoría posterior pueda responder,
con datos reales en vez de con "no disponible":
- ¿Las 6 estrategias congeladas del laboratorio muestran un perfil relativo entre gestores
  consistente, o depende fuertemente de la estrategia?
- ¿Repetir la misma comparación produce el mismo perfil relativo, o hay algo no determinista que la
  auditoría V1 no pudo detectar?
- ¿El corpus puede contener y distinguir correctamente evidencia parcial (corridas fallidas) sin
  romper el mecanismo de persistencia?
- Diversidad de instrumento/dataset **sigue sin poder cerrarse** — la auditoría posterior deberá
  seguir señalándolo como limitación estructural del repositorio, no de la campaña.

---

## 6. Restricciones heredadas (sin relajar)

- Mismas restricciones que `PROPUESTA_CAMPANA_CORPUS_CASO5C_V1.md` §6.
- `caso5/campana_corpus/` (V1, ya congelado en `c83c7a6`): se amplía su matriz, no se modifica su
  contrato ni su mecanismo de ejecución.
- Ningún baseline congelado se toca (`caso1` a `caso5c-capa1-v1-experimental`).
- `caso5/resultados/` sigue excluido de git — no se commitea el corpus generado.

---

## 7. Siguiente documento

Tras aprobar el alcance de esta propuesta, el siguiente paso es una especificación de
implementación mínima (ampliación de la matriz en `campana_corpus/`, mecanismo para la sub-campaña
C de dataset inexistente), seguida de la ejecución real de las 3 sub-campañas. Solo después se
redacta una segunda auditoría de corpus (`AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V2.md` o extensión de
la V1, a decidir), que evalúa si el corpus ampliado (25 comparaciones) ya permite diseñar Capa 2 —
la limitación de diversidad de dataset/instrumento probablemente seguirá abierta incluso después de
esta expansión, y esa auditoría deberá decir si eso, por sí solo, sigue bloqueando Capa 2 o no.

---

## Fuera de alcance de este documento

No se implementó código. No se ejecutó ninguna comparación. No se decide el mecanismo exacto de la
sub-campaña C (cómo forzar `Failed` de forma más controlada que un dataset inexistente, si el
auditor prefiere otra vía) — queda para la especificación siguiente. No se audita ningún corpus
todavía. No se resuelve la limitación de diversidad de dataset/instrumento — se declara
explícitamente como fuera del alcance de lo que el repositorio permite hoy.
