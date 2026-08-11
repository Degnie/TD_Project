# Clasificador de Régimen V1 — Congelado

Estado: **CONGELADO PARA VERSIÓN V1 — Fase 1.4-B CERRADA** (auditoría, 2026-08-11). Este documento
describe la versión oficial del laboratorio, separada de `ClasificadorAdxExperimental.cs`
(exploración, Fase 1.4-A). Implementación: `ClasificadorRegimenV1.cs`. Pruebas:
`TestsClasificadorRegimenV1.cs` (5/5 pasando).

**D-034 — Clasificador de régimen oficial V1 congelado**: ✅ Aprobado. `ClasificadorRegimenV1`,
versión v1, con los 4 estados (Alcista/Bajista/Lateral/Ambiguo) y parámetros congelados según
D-030/D-031/D-032/D-033.

**D-035 — Los clasificadores oficiales no sustituyen experimentales**: ✅ Aprobado. Regla
permanente: los clasificadores exploratorios (`ClasificadorAdxExperimental.cs`,
`ClasificadorEmaExperimental.cs`, `ClasificadorRetornoVolatilidadExperimental.cs`) permanecen
disponibles para investigación futura — no se eliminan al congelar una versión oficial.

---

## Separación experimental vs. oficial

| | Experimental | Oficial V1 |
|---|---|---|
| Archivo | `ClasificadorAdxExperimental.cs` | `ClasificadorRegimenV1.cs` |
| Estados | 3 (Alcista, Bajista, Lateral) | 4 (Alcista, Bajista, Lateral, **Ambiguo**) |
| Propósito | Comparar contra A (EMA) y C (Retorno+Volatilidad), Fase 1.4-A | Contrato oficial del laboratorio, Fase 1.4-B en adelante |
| Parámetros | Marcados `CONFIGURACION EXPLORATORIA` | Congelados como constantes públicas |

`ClasificadorAdxExperimental.cs` permanece sin modificar — sigue siendo el registro histórico de la
evaluación comparativa de Fase 1.4-A. `ClasificadorRegimenV1` es un archivo nuevo, no un reemplazo
in-place, consistente con D-017 (versionado: un cambio de criterio crea una versión nueva, nunca se
edita una ya usada).

---

## Definición matemática

Dado `ADX`, `DI+`, `DI-` calculados por suavizado de Wilder (periodo `PeriodoAdx`, fórmula idéntica
a la ya usada en `ClasificadorAdxExperimental.cs` y formalizada en
`PARAMETRIZACION_CLASIFICADOR_REGIMEN_V1.md §2.2`):

```
SesgoDI = |DI+ - DI-| / (DI+ + DI-)

Si ADX >= UmbralAdxTendencia:
    Si DI+ == DI-           → Ambiguo   (tendencia confirmada, dirección indeterminada — borde)
    Si DI+ > DI-             → Alcista
    Si DI- > DI+             → Bajista

Si ADX < UmbralAdxTendencia:
    Si DI+ + DI- == 0        → Lateral  (ausencia total de actividad direccional)
    Si SesgoDI < UmbralSesgoDI → Lateral
    Si SesgoDI >= UmbralSesgoDI → Ambiguo
```

---

## Estados oficiales (D-028)

1. **Alcista** — `ADX ≥ UmbralAdxTendencia` y `DI+ > DI-`.
2. **Bajista** — `ADX ≥ UmbralAdxTendencia` y `DI- > DI+`.
3. **Lateral** — `ADX < UmbralAdxTendencia` y `DI` balanceado (`SesgoDI < UmbralSesgoDI`, o
   `DI+ + DI- = 0`): ausencia de señal + ausencia de ruido.
4. **Ambiguo** — `ADX < UmbralAdxTendencia` y `DI` en disputa (`SesgoDI ≥ UmbralSesgoDI`), o
   `ADX ≥ UmbralAdxTendencia` con `DI+ = DI-` exacto: ausencia de señal + presencia de
   ruido/contradicción, o dirección indeterminada pese a tendencia confirmada.

---

## Parámetros congelados

| Parámetro | Valor | Origen | Decisión |
|---|---|---|---|
| `PeriodoAdx` | 14 | Estándar Wilder (*New Concepts in Technical Trading Systems*, 1978) | D-030 |
| `UmbralAdxTendencia` | 25 | Convención de literatura técnica | D-030 |
| Método SesgoDI | Relativo, `\|DI+-DI-\|/(DI+ + DI-)` | D-031 | D-031 |
| `UmbralSesgoDI` | **0.153467** | Mediana de medianas por timeframe, calculada sobre `BTCUSDT_2024-01-02_2025-01-02` en 6 timeframes (`RESULTADO_CALIBRACION_UMBRAL_SESGO_DI_V1.md`) | D-032/D-033 |

Ninguno de estos valores fue elegido mirando resultado de ninguna estrategia (D-016). `UmbralSesgoDI`
es el único derivado de datos del propio dataset, por un procedimiento fijado *antes* de calcularlo
(mediana, sin excepciones) — no ajustado después de ver el resultado (D-032).

---

## Tratamiento de bordes (heredado de `PARAMETRIZACION_CLASIFICADOR_REGIMEN_V1.md §3`)

- `ADX` exactamente igual a `UmbralAdxTendencia` → incluido en la rama "hay tendencia" (`≥`, no `>`).
- `DI+ = DI-` exacto con `ADX ≥ UmbralAdxTendencia` → **Ambiguo**, no se fuerza un lado.
- `TR_suavizado = 0` (vela sin rango verdadero) → ventana excluida del cálculo de esa iteración.
- Ventana de calentamiento (`2 × PeriodoAdx` primeras velas) → sin clasificación, no forzada a
  ningún estado.

---

## Limitaciones

- **Dependencia del timeframe no eliminada, solo documentada**: el mismo `PeriodoAdx = 14`
  representa 14 minutos en 1m y 14 días en 1D (`PARAMETRIZACION_CLASIFICADOR_REGIMEN_V1.md §4`). La
  evidencia de Fase 1.4-A mostró la menor amplitud de variación entre escalas de los 3 candidatos
  comparados (1.26pp bajo el modelo de 3 estados) — no hay evidencia equivalente todavía bajo el
  modelo de 4 estados.
- **`UmbralSesgoDI` es específico de este dataset**: a diferencia de `PeriodoAdx`/`UmbralAdxTendencia`
  (convención externa, aplicable a cualquier mercado), `UmbralSesgoDI = 0.153467` se calibró sobre
  BTC/USDT. Aplicar `ClasificadorRegimenV1` a otro instrumento sin recalibrar este valor no está
  validado.
- **Sensibilidad no evaluada**: O-005 (¿un cambio pequeño de ADX/umbral produce una clasificación
  muy distinta?) sigue sin resolverse.
- **Sin validación contra estrategias**: por diseño (D-016/D-021), esta versión no fue evaluada
  contra ninguna estrategia — esa validación es la etapa siguiente a este congelamiento, no parte de
  él.

---

## Versión

**V1** — 2026-08-11. Cambios futuros a cualquier parámetro o regla de esta definición requieren una
nueva versión (`ClasificadorRegimenV2`), nunca edición in-place de este archivo (D-017).

---

## Pruebas requeridas — resultado

`TestsClasificadorRegimenV1.cs`, ejecutadas sobre el dataset real congelado:

| Prueba | Resultado |
|---|---|
| Determinismo (dos corridas idénticas) | ✅ PASA |
| Reproducción (mismos parámetros, explícitos vs. por defecto) | ✅ PASA |
| Cuatro estados generados en el dataset real | ✅ PASA |
| No dependencia de estrategia | ✅ PASA |
| Compatibilidad con `EvaluadorClasificadores.cs` (Fase 1.4-A) | ✅ PASA |

**5/5 pruebas pasan.**
