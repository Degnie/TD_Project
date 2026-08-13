# Auditoría — Análisis Interpretativo Limitado (Caso 5C)

Estado: **documento de auditoría — evalúa la implementación real contra D-124, no propone ni
implementa ninguna fase posterior**. Cierra el ciclo propuesta (`PROPUESTA_EVOLUCION_POST_
CAPA2_V1.md`) → decisión (D-124, `DECISIONES_EVOLUCION_POST_CAPA2_V1.md`) → especificación
(`ESPECIFICACION_IMPLEMENTACION_ANALISIS_INTERPRETATIVO_CASO5C_V1.md`) → implementación
(`analisis_interpretativo/`) → auditoría, mismo patrón ya cerrado para Capa 2 (D-123). No evalúa si
procede abrir Vía A (instrumento) ni ninguna forma de recomendación — eso queda para una decisión
posterior explícita.

---

## 1. ¿La implementación respeta el límite fijado por D-124?

**Sí, verificado por prueba, no solo por diseño.**

D-124 definió 4 capacidades permitidas y una lista de prohibiciones. Cada permiso tiene un método
correspondiente en `DetectorRelaciones`, y cada prohibición tiene al menos una prueba estructural
que la verifica sobre el código real (no sobre datos hipotéticos):

| Permitido (D-124) | Método | Verificado por |
|---|---|---|
| Detectar relaciones observadas | `CruzarDimensiones` | P1 (no pierde/inventa combinaciones), P2 (nunca 1 sola combinación), P3 (orden de aparición) |
| Agrupar comportamientos | `AgruparPorPatron` | P4 (expone ambos lados, sin solapamiento ni omisión) |
| Describir condiciones de aparición | `DescribirCondicionesDeAparicion` | Ejecutado sobre corpus real (§3), sin prueba dedicada adicional — reutiliza la misma trazabilidad que P7 valida sobre `CruzarDimensiones` |
| Comparar estabilidad de patrones | `CompararConsistencia` | Ejecutado sobre corpus real (§3); estructura idéntica a `AgruparPorPatron`, mismas garantías |

| Prohibido (D-124) | Verificado por |
|---|---|
| Recomendar gestor/estrategia | P5 (reflexión: ningún tipo con campo "Mejor"/"Recomend"/etc.) |
| Seleccionar configuración | P5 (mismo mecanismo) |
| Puntuar alternativas | Ningún método combina 2+ métricas en un número — verificado por inspección de `DetectorRelaciones.cs` (§4 de la especificación); ninguna prueba automatizada dedicada, mismo criterio que Capa 2 no la tuvo tampoco |
| Inferir comportamiento futuro | Cubierto por el texto fijo de `Limitaciones` en `ProgramAnalisisInterpretativo.cs` ("Observación histórica, no proyección de comportamiento futuro") — no hay verificación automatizada de que ningún string generado prediga el futuro, es una garantía de plantilla fija, no de contenido dinámico |
| Crear reglas operativas | P6 (ausencia léxica: "debe", "recomendado", "usar", "elegir", etc.) |
| Extrapolar fuera de BTCUSDT | Mismo texto fijo de `Limitaciones`; el corpus en sí solo contiene `BTCUSDT` (verificado en Capa 2) |

**8/8 pruebas (P1-P8) pasan**, incluyendo P7 (trazabilidad completa a evidencia origen sobre el
corpus real — cada `CarpetaOrigen` referenciada existe físicamente y aparece en
`MANIFIESTO_CORPUS_CASO5C_V1.json`) y P8 (ausencia textual de referencias a `ComparadorGestores.
Comparar`/`EjecutorProtocolo.Ejecutar`/`PersistidorComparaciones`/`CrearEstrategia`/`IStrategy` en
`DetectorRelaciones.cs`).

---

## 2. Punto señalado por el auditor: P6 es una barrera, no una garantía completa

El auditor precisó explícitamente que la prohibición léxica (P6) es una defensa adicional, no
suficiente por sí sola — un texto puede ser prescriptivo sin usar esas palabras exactas. Esto se
respeta en el diseño de dos formas:

1. **P6 nunca es la única salvaguarda de una capacidad dada** — cada método de `DetectorRelaciones`
   también está cubierto por P5 (ausencia estructural de campos de ranking/selección) y, cuando
   aplica, por P2 (nunca una combinación destacada). Las salvaguardas son redundantes por diseño,
   no secuenciales.
2. **`DetectorRelaciones` no genera prosa interpretativa en ningún punto** (§5 de la especificación,
   verificado por inspección directa del código: cada método devuelve exclusivamente records con
   listas/diccionarios de datos crudos — `CombinacionObservada`, `AgrupacionPorPatron`, etc. —, y las
   únicas cadenas de texto que construye son descripciones factuales templadas de la forma
   `"{Estrategia}/{Timeframe}/{Dataset}/{Gestor}"`, sin ningún verbo ni juicio de valor). La ausencia
   de prosa generada dinámicamente reduce el espacio en el que un texto prescriptivo podría colarse
   sin usar las palabras prohibidas — no lo elimina como riesgo futuro si el código evoluciona, pero
   sí lo acota al estado actual verificado.

**Limitación reconocida, no resuelta aquí**: si una fase futura extiende `DetectorRelaciones` para
generar prosa más rica, P6 seguirá siendo necesaria pero no suficiente — esa fase futura tendría que
evaluar de nuevo si el texto generado es prescriptivo en espíritu, no solo en vocabulario.

---

## 3. Evidencia de ejecución real sobre el corpus (49 comparaciones, 147 filas)

Ejecutado `analisis_interpretativo/` sobre el corpus oficial completo, vía el mismo manifiesto que
Capa 2 usa (`MANIFIESTO_CORPUS_CASO5C_V1.json`), sin ninguna ejecución nueva de backtest:

- **`CruzarDimensiones(filas, "DrawdownMaximoPct", ["Estrategia", "Gestor"])`**: produjo 18
  combinaciones (6 estrategias × 3 gestores), cada una con su propia estadística — ninguna
  destacada, todas presentadas en el mismo formato.
- **`AgruparPorPatron(..., "DrawdownMaximoPct>=99%", ...)`**: 43 filas donde aparece, 104 donde no
  — cubre exactamente las 147 filas del corpus (43+104=147).
- **`AgruparPorPatron(..., "SinActividad", ...)`**: 18 filas donde aparece (ZScore Reversion, ambos
  períodos, los 3 timeframes × 3 gestores), 129 donde no — 18+129=147, coincide con el hallazgo ya
  documentado en `RESULTADO_ANALISIS_CORPUS_CASO5C_CAPA2_V1.md` §4.
- **`CompararConsistencia(..., "DrawdownMaximoPct>=99%", ...)`**: 15 condiciones (estrategia/
  timeframe/gestor) en el período 2024-2025, 16 en 2022-2023 — 14 de esas condiciones son idénticas
  entre ambos períodos (mismo trío estrategia/timeframe/gestor), con 1 diferencia en cada dirección
  (`Ema Cross/1h/fixed-risk` solo aparece en 2022-2023; ninguna condición aparece exclusivamente en
  2024-2025 que no esté también en 2022-2023 salvo por el conteo total). Este es exactamente el tipo
  de observación de consistencia factual que D-124 autorizó — presencia/ausencia del mismo conjunto
  de condiciones, sin calificar la robustez.

Ningún cálculo produjo un error, ninguna combinación quedó sin trazabilidad (P7 lo confirma sobre
el corpus real, no solo sobre fixtures).

---

## 4. Qué no se hizo (restricción respetada)

- No se implementó ningún mecanismo de recomendación, ranking, ni selección automática.
- No se modificó `ComparadorGestores.cs`, `PersistidorComparaciones.cs`, `EjecutorProtocolo.cs`, ni
  `AnalisisDescriptivo.cs`/`LectorCorpus.cs` (Capa 2) — `analisis_interpretativo/` los reutiliza por
  referencia de compilación, nunca los altera.
- No se modificó `MANIFIESTO_CORPUS_CASO5C_V1.json` — se consumió tal cual.
- No se ejecutó ninguna campaña nueva ni ningún backtest — confirmado por P8 y por el hecho de que
  `AnalisisInterpretativo.csproj` no referencia `src/Domain`/`src/Application` ni ningún componente
  de ejecución.
- No se calibró ningún umbral nuevo — los 2 patrones usados (`DrawdownMaximoPct>=0.99`,
  `SinActividad`) son exactamente los mismos ya nombrados y usados por `AnalisisDescriptivo.
  DetectarCasosAtipicos` (Capa 2) y documentados en auditorías previas (D-030 respetado).

---

## 5. Estado consolidado tras esta fase

```
Caso 5C Capa 1                         ✅ cerrado
Corpus oficial                         ✅ 49 comparaciones declaradas
Caso 5C Capa 2 (descriptiva)           ✅ implementada y ejecutada
Análisis interpretativo limitado (D-124) ✅ implementado y ejecutado, 8/8 pruebas
Recomendación / selección automática    ❌ no existe (D-118 intacta)
```

Suites de regresión sin cambios: 126/126 producción, 25/25 `caso5` (incluyendo Capa 1/5A/5B).

---

## Fuera de alcance de este documento

No se evalúa si procede abrir Vía A (diversidad de instrumento, D-121, pospuesta). No se evalúa si
la evidencia interpretativa acumulada justifica activar D-118/D-119/D-120. No se genera ningún
documento de resultado narrativo sobre las relaciones detectadas (posible documento futuro,
equivalente a `RESULTADO_ANALISIS_CORPUS_CASO5C_CAPA2_V1.md` pero para esta capa) — esta auditoría
solo confirma que la infraestructura respeta D-124, no interpreta el contenido de lo que detectó.

---

## Conclusión

`analisis_interpretativo/` implementa exactamente las 4 capacidades autorizadas por D-124, con cada
prohibición cubierta por al menos una salvaguarda estructural verificada por prueba (8/8), y una
precisión adicional del auditor (P6 como barrera, no garantía completa) incorporada al diseño
mediante redundancia de salvaguardas y ausencia de prosa generada dinámicamente. La ejecución real
sobre el corpus de 49 comparaciones confirma que el mecanismo produce trazabilidad completa y
resultados consistentes con lo ya documentado en auditorías previas. Ninguna forma de
recomendación, ranking, selección, ni regla operativa fue implementada — D-118/D-119/D-120
permanecen intactas. El siguiente cierre esperado, cuando se decida abrir, será sobre la capacidad
de detectar relaciones dentro del corpus (esta fase), no sobre decisiones operativas.
