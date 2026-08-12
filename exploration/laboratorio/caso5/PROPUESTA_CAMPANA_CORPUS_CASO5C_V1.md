# Propuesta — Campaña de Generación de Corpus Comparativo (Caso 5C, previo a evaluar Capa 2)

Estado: **documento de apertura — previo a cualquier ejecución**. Define el alcance exacto de una
campaña deliberada de comparaciones, para que `AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V1.md` tenga
evidencia real que auditar, en vez de solo el corpus técnico dejado por
`TestsPersistidorComparaciones.cs`. No es una fase nueva del ciclo D-N — no introduce decisiones,
componentes, ni contratos nuevos. Es una **generación de evidencia usando exactamente lo ya
congelado** (`caso5b-v1-experimental`, `caso5c-capa1-v1-experimental`).

**Principio rector, explícito**: esto es generación de evidencia, no optimización. Ninguna corrida
de la campaña se elige, repite, ni descarta en función de su resultado — el conjunto de
combinaciones se fija por completo antes de ejecutar la primera.

---

## 1. Ejes de variación disponibles (verificado contra código, no asumido)

**Estrategias**: `Caso5.csproj` solo linkea 2 (`caso5/Caso5.csproj:9-10`) — ampliar la lista
requeriría tocar el `.csproj`, fuera de esta campaña:
- `EstrategiaTresMosqueteros` (con martingala, `maxMartingalas=2`).
- `EstrategiaEmaCross` (sin martingala) — ya usada en Caso 5A/P7 para confirmar aislamiento
  estrategia/gestor.

**Gestores de riesgo**: los 3 congelados en Caso 5A (D-110), sin parámetros nuevos:
- `GestorFixedFractional(0.1m)` — control/referencia obligatorio en toda comparación (D-110).
- `GestorFixedRisk(50m)`.
- `GestorVolatilitySizing(ventana: 20, porcentajeRiesgoBase: 0.1m, desviacionReferencia: 2m)` —
  mismos valores usados en pruebas ya congeladas de Caso 5A/5B, no nuevos.

**Dataset**: único disponible, `BTCUSDT_2024-01-02_2025-01-02` (`datasets/reales/BTCUSDT/`).
Variar dataset en esta campaña significa variar **timeframe** (D-113 fija 1 timeframe por
comparación) — no existe un segundo instrumento ni un segundo rango temporal ya presente en el
repositorio.

**Timeframes disponibles** (archivos verificados en `datasets/reales/BTCUSDT/`): `1m`, `2m`, `5m`,
`10m`, `15m`, `30m`, `1h`, `2h`, `4h`, `8h`, `12h`, `1D`, `1W`. La campaña no necesita cubrir los 13
— cubre un subconjunto que aporte diversidad temporal real sin convertirse en una corrida masiva no
supervisable.

---

## 2. Diseño de la campaña — matriz fija, declarada antes de ejecutar

```
Estrategia A (Tres Mosqueteros)          Estrategia B (EMA Cross)
    |                                        |
    +-- Timeframe 15m                        +-- Timeframe 15m
    |     +-- FixedFractional                |     +-- FixedFractional
    |     +-- FixedRisk                      |     +-- FixedRisk
    |     +-- VolatilitySizing               |     +-- VolatilitySizing
    |                                        |
    +-- Timeframe 1h                         +-- Timeframe 1h
    |     (mismos 3 gestores)                |     (mismos 3 gestores)
    |                                        |
    +-- Timeframe 1D                         +-- Timeframe 1D
          (mismos 3 gestores)                      (mismos 3 gestores)
```

**6 comparaciones totales** (2 estrategias × 3 timeframes), cada una con los mismos 3 gestores —
equivalente a 18 corridas individuales de `EjecutorProtocolo` ejecutadas internamente por
`ComparadorGestores.Comparar` (D-113, sin cambio).

**Por qué 3 timeframes, no 13**: `15m`/`1h`/`1D` cubren corto/medio/largo plazo con separación
suficiente para que D-119 (diversidad temporal) tenga algo real que evaluar, sin producir un
volumen que ninguna auditoría posterior pueda revisar con cuidado. Ampliar la cobertura de
timeframes es un ajuste de alcance de una campaña futura, no de esta.

**Por qué no una tercera estrategia**: ninguna otra está linkeada en `Caso5.csproj` hoy — añadir una
requeriría modificar ese archivo, que es una decisión de infraestructura fuera del alcance de
"generar evidencia con lo ya congelado".

---

## 3. Qué debe usarse (sin flujo paralelo)

Exclusivamente los 2 componentes ya congelados, sin ninguna modificación:
- `ComparadorGestores.Comparar(entradaBase, gestores)` (Caso 5B) — produce cada
  `ResultadoComparativoGestores` en memoria.
- `PersistidorComparaciones.Persistir(dirResultados, resultado)` (Caso 5C Capa 1) — escribe cada
  resultado a `caso5/resultados/`.

**Mecanismo de ejecución**: un `Program.cs` de campaña (o una extensión temporal del existente,
a decidir en la especificación) que itera la matriz de §2, invocando ambos componentes en
secuencia por cada combinación — sin lógica nueva más allá de la iteración.

---

## 4. Qué no debe hacerse

- No seleccionar un "ganador" de ninguna comparación generada.
- No calcular ninguna recomendación ni criterio de orden.
- No ajustar ningún parámetro de gestor observando resultados intermedios de la propia campaña
  (D-030 — los 3 gestores usan exactamente los valores ya congelados en Caso 5A/5B/pruebas).
- No modificar `EstrategiaTresMosqueteros`/`EstrategiaEmaCross`.
- No modificar `ComparadorGestores.cs`/`PersistidorComparaciones.cs`/`RenderizadorComparacionGestores.cs`
  (Caso 5B/5C Capa 1, congelados).
- No repetir ni descartar una combinación de la matriz en función de su resultado — las 6
  comparaciones se ejecutan todas, tal como quedaron fijadas en §2.
- No tocar `src/`/`tests/`.

---

## 5. Objetivo mínimo del corpus generado

No se define aquí ningún umbral de suficiencia para recomendación (D-119 ya estableció que eso se
difiere). Esta campaña busca únicamente que el corpus permita evaluar, en la auditoría posterior:
- **Repetición**: ¿el mismo gestor se comporta de forma consistente entre timeframes de la misma
  estrategia?
- **Estabilidad**: ¿las diferencias entre gestores son consistentes entre las 2 estrategias, o
  dependen de la estrategia?
- **Diferencias observadas**: ¿hay variación real entre gestores, o resultados prácticamente
  idénticos que no aportarían nada a una futura Capa 2?
- **Cobertura experimental**: 2 estrategias × 3 timeframes × 3 gestores — suficiente para que la
  auditoría hable de "diversidad" con datos concretos, sin ser todavía una afirmación de que esto
  basta para recomendar.

---

## 6. Restricciones heredadas (sin relajar)

- `ComparadorGestores`/`PersistidorComparaciones`/`RenderizadorComparacionGestores` (Caso
  5B/5C Capa 1, congelados): sin modificación.
- `IStrategy`, las 2 estrategias usadas, `AplicadorFill`, `ResolutorCrossZero`, `GestorCapital`,
  `IGestorRiesgo`, `EjecutorProtocolo`, `EntradaProtocolo`: sin modificación.
- Ningún baseline congelado se toca (`caso1` a `caso5c-capa1-v1-experimental`).
- `src/`/`tests/` intactos.
- `caso5/resultados/` sigue excluido de git — el corpus generado es evidencia regenerable, no se
  commitea.

---

## 7. Siguiente documento

Una vez aprobado el alcance de esta propuesta, el siguiente paso es una especificación de
implementación mínima (mecanismo de iteración de la matriz — probablemente un método nuevo o un
`Program.cs` de campaña separado del runner de pruebas, a decidir), seguida de la ejecución real.
Solo después de tener el corpus de 6 comparaciones persistidas se redacta
`AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V1.md`, con las preguntas ya fijadas por el auditor (cuántas
comparaciones, qué diversidad, repetidas o independientes, variabilidad suficiente para observar
patrones, qué limitaciones impiden recomendar todavía) — nunca qué gestor gana ni qué recomendar.

---

## Fuera de alcance de este documento

No se implementó código. No se ejecutó ninguna comparación. No se decide el mecanismo exacto de
iteración (método nuevo vs. `Program.cs` separado) — queda para la especificación siguiente. No se
audita ningún corpus todavía.
