# Decisiones — Selección de Rango Temporal Alternativo (Caso 5C)

Estado: **D-122 resuelta**. Misma estructura usada en D-001 a D-121 (decisión, opciones, criterio,
evidencia, resolución). Ningún código se modifica en este documento — la resolución aquí registrada
habilita una especificación de implementación posterior, no la reemplaza. No se descarga ningún
dato en este documento.

Contexto completo en `HALLAZGO_RECHAZO_DATASET_2023_CASO5C_V1.md` (rechazo de
`BTCUSDT 2023-01-02–2024-01-02` por hueco real de 80 minutos) y
`DECISIONES_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md` (D-121, Vía B — tiempo primero). Verificación contra
código existente, no reconstruida de memoria (mismo criterio que abrió toda fase anterior, D-057).

---

## D-122 — Cómo seleccionar el rango temporal alternativo tras el rechazo del rango 2023

**Estado**: 🟢 Aprobada. **Selección: B — búsqueda previa de disponibilidad por rangos cortos,
separada de la congelación.**

**Decisión**: `BTCUSDT 2023-01-02–2024-01-02` fue rechazado por `ValidadorIntegridadDatos` (hueco
real de 80 minutos). D-121 ya fijó que la Vía B (tiempo) se ejecuta antes que la Vía A
(instrumento) — esta decisión no reabre D-121, solo resuelve cómo elegir el próximo rango a
intentar sin repetir el costo de una descarga completa de 1 año que termine rechazada de nuevo.

### Opciones

- **A — Otro año completo directamente**: elegir un año distinto (ej. `2022-01-02`–`2023-01-02`) y
  descargarlo completo de una vez, igual que se hizo con 2023.
  - Riesgo confirmado por el propio hallazgo que originó esta decisión: una descarga de 1 año
    completo son ~526 requests paginados (`DescargadorVelas.cs:19`, ya verificado en la ejecución
    real de 2023) — si el nuevo año también tuviera un hueco, el costo completo de la descarga se
    gastaría de nuevo antes de descubrirlo, igual que ocurrió con 2023.
- **B — Búsqueda previa de disponibilidad por rangos cortos**: antes de comprometerse a un año
  completo, verificar continuidad en ventanas más pequeñas (ej. por mes) del candidato, y solo
  descargar el año completo si esa verificación previa no encuentra huecos.
  - Ventaja: reduce el costo de descubrir un hueco — una verificación por mes cuesta una fracción
    de las ~526 requests de un año completo (paginación proporcional al rango).
  - Precisión del auditor, incorporada aquí como parte de la resolución, no solo como nota: **la
    búsqueda previa debe quedar separada de la congelación** — explorar disponibilidad puede
    descartar candidatos, pero solo un dataset que pase por el camino completo ya existente
    (descarga → `ValidadorIntegridadDatos` → congelación manual, `PLAN_FASE2A.md` §6) entra al
    corpus. La exploración no es una vía alternativa de validación, es un filtro previo que reduce
    qué candidatos llegan a intentar el camino real.
- **C — Otra estrategia**: no se identificó ninguna alternativa adicional con valor distinto de A o
  B durante esta ronda — ver "Evidencia" para el análisis de por qué no se desarrolla como opción
  separada.

### Criterio decisivo — coste de adquisición, trazabilidad, reproducibilidad, riesgo de repetición

- **Coste de adquisición**: B reduce el coste esperado de encontrar un rango válido — verificar
  continuidad por mes (12 verificaciones cortas como máximo, cada una una fracción de una descarga
  anual) es más barato que descargar años completos hasta acertar uno sin huecos.
- **Trazabilidad**: ambas opciones son igual de trazables una vez que un dataset pasa a
  `datasets/reales/` — la diferencia está en qué queda registrado *antes* de llegar ahí. B produce
  un registro adicional (qué rangos se descartaron y por qué) que A no genera, porque A solo
  intentaría rangos completos uno a la vez sin dejar evidencia de candidatos previos explorados.
- **Reproducibilidad**: sin diferencia entre A y B — ambas terminan usando el mismo pipeline de
  descarga/validación/congelación ya existente para el rango finalmente elegido.
- **Riesgo de repetir el mismo problema**: A no reduce el riesgo de que el próximo año completo
  elegido también tenga un hueco — sería descubrirlo de nuevo solo después de gastar el costo
  completo de la descarga. B mitiga directamente este riesgo, que es precisamente el que ya se
  materializó con 2023.

### Resolución adoptada

**Selección: B.** Antes de descargar un año completo candidato, se verifica su continuidad por
ventanas mensuales usando el mismo `BinanceClient`/`ValidadorIntegridadDatos` ya existentes — sin
construir un mecanismo de validación nuevo, solo aplicándolo a rangos más cortos antes de
comprometerse al año completo.

**Separación exploración/congelación, tal como exige la precisión del auditor**:

```
Exploracion de disponibilidad (rangos cortos, ej. por mes)
        |
        v
   ¿Continuidad OK en cada mes explorado?
        |
   NO ---+--- SI
   |          |
   v          v
Candidato   Candidato pasa a descarga completa del anio
descartado         |
                   v
        Descarga completa (mismo pipeline ya existente,
        datos_reales/Program.cs)
                   |
                   v
        ValidadorIntegridadDatos sobre el anio completo
        (verificacion real, no solo la exploracion previa)
                   |
              NO apto --- Apto
              |             |
              v             v
        Rechazado,      Congelacion manual
        no promovido    (PLAN_FASE2A.md §6)
                              |
                              v
                        datasets/reales/BTCUSDT/*_{anio}/
```

**Por qué la exploración no reemplaza la validación completa**: verificar continuidad por mes no
garantiza que el año completo, descargado en una sola pasada continua, produzca exactamente el
mismo resultado (aunque se espera que sí, dado que Binance sirve el mismo histórico) — el paso de
`ValidadorIntegridadDatos` sobre la descarga completa sigue siendo obligatorio y autoritativo,
exactamente como ya lo es hoy. La exploración es un filtro de bajo costo para no gastar la descarga
completa en un candidato ya sabido problemático, no un sustituto de la validación real.

**Por qué no A directamente**: repetiría el mismo patrón que ya costó una descarga completa
rechazada (2023) — sin ninguna mitigación del riesgo que causó el rechazo original.

**Por qué no se desarrolla C como opción separada**: ni la propuesta original
(`PROPUESTA_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md`) ni el hallazgo que originó esta decisión
identificaron una tercera vía con valor distinto de "año completo directo" (A) o "explorar antes de
comprometerse" (B) — ambas ya cubren el espacio de opciones razonables dado el pipeline existente.
Mantenerla en la plantilla de decisión (mismo formato que D-112/D-121) sin forzar una tercera
opción artificial es preferible a inventar una sin justificación.

### Qué candidato de año probar primero

**No se fija aquí** — queda para la especificación de implementación siguiente, que deberá aplicar
la exploración de B sobre 1 o más candidatos (ej. 2022, u otro año) antes de decidir cuál pasa a
descarga completa. Fijar el año exacto en esta decisión sería anticipar un resultado que la propia
exploración todavía no produjo.

### Restricciones que aplican

- Reafirmadas de D-121/`PROPUESTA_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md` §7, sin relajar: ningún
  parámetro económico se recalibra por el dataset nuevo (D-030); congelación manual, no automática;
  no se generan datasets sintéticos; ningún baseline congelado se toca.
- **`ValidadorIntegridadDatos` no se modifica** para esta exploración — se reutiliza tal cual,
  aplicado a rangos más cortos, mismo criterio de rechazo estricto ya vigente (reafirmado tras el
  hallazgo del rango 2023).
- **La exploración de disponibilidad no persiste ningún artefacto en `datasets/reales/`** — solo un
  dataset que complete el camino real de descarga+validación+congelación entra al corpus
  experimental. Cualquier CSV/reporte generado por la exploración vive, si acaso, en un lugar
  temporal o de diagnóstico, no en la ruta de datasets congelados.
- Los cambios de código ya realizados en `datos_reales/Program.cs`/`agregador/Program.cs`
  (pendientes de commit, ver `HALLAZGO_RECHAZO_DATASET_2023_CASO5C_V1.md`) se mantienen sin
  commitear hasta que la especificación de implementación siguiente defina su forma final —
  probablemente requieran una extensión adicional para soportar la exploración por rangos cortos
  antes de la descarga completa, no solo el cambio de rango ya aplicado.

### Evidencia

- `HALLAZGO_RECHAZO_DATASET_2023_CASO5C_V1.md`: origen de esta decisión — rechazo real de
  `BTCUSDT 2023-01-02–2024-01-02` por hueco de 80 minutos, costo ya incurrido de una descarga
  completa (~526 requests) antes de descubrirlo.
- `datos_reales/BinanceClient.cs:19`, `DescargadorVelas.cs:19-61`: mecanismo de descarga ya
  paginado y reanudable, reutilizable sin cambio para ventanas más cortas (rango de fechas es
  parámetro, no constante).
- `ValidadorIntegridadDatos` (referenciado en `datos_reales/Program.cs:62`): mecanismo de
  validación ya existente, reutilizable sin cambio sobre cualquier rango de velas leídas.
- Precisión explícita del auditor en la revisión del hallazgo: separación exploración/congelación
  como restricción central de esta decisión, incorporada aquí como parte de la resolución de B, no
  como nota externa.
- D-113 (`DECISIONES_CASO5B_V1.md`)/D-121: mismo principio de mantener responsabilidades separadas
  por construcción (control experimental, atribución causal) extendido aquí a la separación entre
  "explorar candidatos" y "congelar evidencia".

---

## Fuera de alcance de este documento

No se implementó código. No se descargó ningún dato. No se decide todavía qué año(s) explorar
primero. No se especifica el mecanismo exacto de la exploración por rangos cortos (qué endpoint,
qué tamaño de ventana exacto, dónde vive el código) — queda para la especificación de
implementación siguiente.

---

## Próximo documento

Una especificación de implementación para la exploración de disponibilidad (B): mecanismo concreto
(reutilización de `BinanceClient`/`ValidadorIntegridadDatos` sobre ventanas mensuales), candidato(s)
de año a explorar, y solo tras encontrar un candidato sin huecos, la descarga completa siguiendo
exactamente el mismo camino que ya existe (`datos_reales/Program.cs` con el rango ajustado). Los
cambios de código pendientes de `datos_reales/Program.cs`/`agregador/Program.cs` se retoman y
extienden ahí, no se commitean antes.
