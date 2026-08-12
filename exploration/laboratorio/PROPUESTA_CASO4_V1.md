# Propuesta — Caso 4: Evolución Financiera

Estado: **documento de apertura — previo a cualquier decisión o implementación**. Define la
pregunta que responde el Caso 4, sus límites y las decisiones que deben resolverse antes de tocar
código, siguiendo el mismo ciclo que Caso 1, Caso 2 y Caso 3A: especificación → decisión →
implementación → pruebas → auditoría → congelamiento. No abre implementación. No resuelve deuda
técnica salvo la que quede explícitamente dentro del alcance declarado en la sección 4.

**Punto de partida**: `INDICE_DECISIONES_GLOBAL_V1.md` (D-001 a D-090) — ninguna deuda de Caso
1/Caso 2/Caso 3A bloquea el uso de los tres como referencia estable para esta fase, salvo D-084 y
D-085, que son precisamente el objeto de este Caso.

---

## 1. Objetivo del Caso 4

**Pregunta principal**: ¿puede el motor evolucionar desde un modelo financiero descriptivo hacia
uno económicamente coherente — resolviendo D-084 y D-085 — sin romper la reproducibilidad de los
experimentos ya congelados (Caso 1, Caso 2, Caso 3A)?

**No busca**:
- Optimizar rentabilidad ni recomendar tamaño de posición real.
- Simular capital real, ejecución live ni integración con exchange.
- Resolver Masaniello ni ningún modelo de gestión de riesgo probabilístico (ya evaluado y
  descartado en `EVALUACION_MODELOS_GESTION_RIESGO_V1.md`).

El Caso 4 evalúa la **coherencia interna del modelo económico**, no la rentabilidad de ninguna
estrategia — mismo principio que D-054/D-076 aplicaron: ninguna corrida de esta fase debe
interpretarse como recomendación financiera.

---

## 2. Punto de partida congelado — evidencia verificada en código

**D-084 — `GestorCapital` no distingue apertura/cierre.** Verificado en
`src/Domain/Portfolio/GestorCapital.cs:21`:

```csharp
return requests.Select(r => r with { Cantidad = cantidadCalculada }).ToList();
```

`Ajustar` sobrescribe `Cantidad` en **toda** `OrderRequest` recibida, sin distinguir si la orden
abre, aumenta, reduce o cierra una posición — esa distinción no existe en el tipo `OrderRequest`
(`src/Domain/Shared/OrderRequest.cs:4-9`: solo `Side`, `Type`, `Cantidad`, sin campo de intención).
La intención real (abrir/cerrar) se infiere implícitamente más abajo en el pipeline, en
`ConsumidorFifo`/`ResolutorCrossZero`, comparando el signo de `Cantidad` contra `LotesVivos` — no
en el punto donde `GestorCapital` decide el tamaño. Esto produce el hallazgo ya documentado: una
orden de cierre recibe un `Cantidad` recalculado que casi nunca coincide con el lote abierto,
generando residuos que se acumulan sin límite en corridas largas con reaperturas.

**D-085 — Cantidad histórica sin relación dimensional con `CapitalInicial`.** Verificado: las 5
estrategias existentes (Tres Mosqueteros, MHI Mayoría, EMA Cross, Z-Score Reversal, Neutral) fijan
`Cantidad=1` como constante de diseño heredada de Caso 1, sin relación con
`ConfiguracionExperimento.CapitalInicial=1000` — la desproporción (`Margin ≈ Cantidad × Precio ×
TasaMargen`, del orden de miles para BTCUSDT, contra un capital de 1000) solo se hizo visible al
calcular `MetricasFinancieras` en el baseline de Caso 2, nunca antes.

**`ValidadorCapacidad` es puramente observacional.** Verificado en
`src/Domain/Broker/ValidadorCapacidad.cs:14`: `Validar` retorna `bool` y no altera ningún estado —
la orden se ejecuta exista o no capacidad, quedando solo un `RegistroIncapacidad` (D-059/D-060,
decisión ya congelada, no se reabre aquí). `CalculadoraReservaPreventiva.Calcular`
(`src/Domain/Broker/CalculadoraReservaPreventiva.cs:19`) usa `request.Cantidad` — es decir, la
reserva evaluada ya es la `Cantidad` post-`GestorCapital`, acoplando D-084 y D-085 en el mismo
cálculo.

Ambos son **infraestructura estable pero con deuda conocida** — el Caso 4 los audita y
eventualmente los modifica, a diferencia de Caso 3A que los consumió sin tocarlos.

---

## 3. Hipótesis de Caso 4

**Hipótesis principal**: la causa raíz de D-084 no es un defecto de `GestorCapital` en sí, sino la
ausencia de una distinción estructural de intención (apertura/aumento vs. reducción/cierre) en
`OrderRequest` — el punto donde D-071 ya estableció que `GestorCapital` "transforma, nunca crea ni
elimina órdenes" asumió implícitamente que transformar `Cantidad` es seguro para cualquier orden,
sin verificar esa asunción contra órdenes de cierre.

**Evidencia parcial ya existente**: `ConsumidorFifo`/`ResolutorCrossZero` ya calculan esta
distinción internamente (comparan signo de la orden contra `LotesVivos` para decidir si consumen
FIFO, cruzan cero, o abren/aumentan) — la información existe en el pipeline, solo no está
disponible en el punto donde `GestorCapital` la necesitaría.

**Lo que Caso 3A no probó** (y Caso 4 debe): que el modelo financiero es coherente bajo sizing
activo con reaperturas — todas las familias de Caso 3A, igual que el baseline de Caso 2, corrieron
con `Sizing=null` (D-084) precisamente para evitar este problema, no para demostrarlo resuelto.

---

## 4. Deudas técnicas que Caso 4 activa por definición

A diferencia de Caso 3A (que declaraba activación condicional), Caso 4 **existe para resolver**
D-084 y D-085 — son su objeto, no una activación posible.

**D-084 — `GestorCapital` no distingue apertura/cierre.** Alcance: resolver la semántica de la
orden (ver sección 6, sub-fase 4.2) antes de modificar `GestorCapital` — corregir el síntoma sin
resolver la causa reproduciría el mismo hallazgo con otro nombre.

**D-085 — relación dimensional `Cantidad`/`CapitalInicial`.** Alcance: definir qué representa
`Cantidad` (¿unidades del activo? ¿fracción de capital? ¿lotes normalizados?) y cómo se relaciona
con `CapitalInicial`, sin recalibrar retroactivamente el valor ya congelado en Caso 1/Caso 2 (ver
restricción en sección 7).

**No incluidas por defecto**: D-055 (catálogo de métricas de martingala — pertenece a una posible
fase de generalización del laboratorio, no a coherencia financiera) y D-044 (entrada × resolución
— pertenece a interacción estrategia/régimen). Ninguna de las dos se activa por abrir Caso 4.

---

## 5. Criterios de éxito

- **D-084 resuelta con evidencia, no con parche**: `GestorCapital` (o su reemplazo) distingue
  correctamente apertura/aumento de reducción/cierre, verificado por una corrida larga (equivalente
  al baseline financiero, ~82k operaciones) con `Sizing` activo que no produce residuos de lotes ni
  degradación de rendimiento.
- **D-085 resuelta con definición explícita**: qué representa `Cantidad` y cómo se relaciona con
  `CapitalInicial` queda documentado y verificable en código — sin necesidad de recalibrar
  `CapitalInicial=1000` ya congelado (mismo principio que motivó no tocarlo en D-085 original).
- **No regresión sobre Caso 1/Caso 2/Caso 3A**: los 3 hashes/baselines congelados
  (`baseline_final/`, `baseline_financiero_final/`, evidencia de `caso3a-v1-experimental`)
  permanecen bit-a-bit idénticos — cualquier corrección debe ser aditiva o activarse solo bajo
  configuración explícita nueva (mismo patrón D-061: parámetro opcional con default histórico).
- **Separación estrategia/economía preservada (P-002)**: ninguna `IStrategy` existente conoce
  `Cash`/`Margin`/`Equity` como resultado de esta fase.
- **Cambios incompatibles versionados, no parcheados in-place**: si D-084/D-085 requieren un
  cambio de contrato (ej. `OrderRequest` con campo de intención), se declara explícitamente como
  ruptura y se decide su alcance en la sección de decisiones — no se fuerza compatibilidad
  artificial que oculte el cambio real.

---

## 6. Alcance sugerido — sub-fases

No se selecciona ninguna todavía — se presentan como sub-fases posibles, a decidir en el documento
de decisiones que sigue a esta propuesta. Orden sugerido, cada una cerrable independientemente
antes de continuar (mismo ritmo incremental de Caso 1/Caso 2/Caso 3A):

- **4.0 — Auditoría de arquitectura financiera**: sin cambio de código. Documentar exhaustivamente
  qué representa `Cantidad`, cómo se relaciona con exposición/capacidad, y el diagrama de flujo
  real `Strategy → GestorCapital → ValidadorCapacidad → Fill` con sus puntos de acoplamiento
  (D-084↔D-085 vía `CalculadoraReservaPreventiva`). Este documento de propuesta ya adelanta parte
  de esa auditoría (sección 2); 4.0 la formaliza y la completa.
- **4.1 — Semántica de la orden (D-084)**: definir cómo se distingue apertura/cierre en el punto
  donde `GestorCapital` decide el tamaño — sin decidir aquí si es un campo nuevo en `OrderRequest`,
  un parámetro adicional a `Ajustar`, o una consulta a `PortfolioState`/`LotesVivos` antes de
  transformar.
- **4.2 — Corrección de `GestorCapital`**: implementar la solución de 4.1, verificada contra la
  corrida larga que originalmente colgó (D-084).
- **4.3 — Unidades y exposición (D-085)**: definir la relación `Cantidad`/`CapitalInicial`/
  `TasaMargen`, sin recalibrar el baseline congelado.
- **4.4 — `ValidadorCapacidad`**: revisar si debe seguir siendo puramente observacional (D-059/
  D-060 ya congeladas) una vez que D-084/D-085 estén resueltas, o si la nueva coherencia dimensional
  cambia qué significa "capacidad" — sin decidir aquí si se reabre D-059/D-060.

---

## 7. Exclusiones

Fuera de alcance de Caso 4 (mismo criterio de exclusión que Caso 1 §D-002, Caso 2 §5 de
`DEUDA_TECNICA_CASO2_V1.md`, Caso 3A §7 de `PROPUESTA_CASO3_V1.md`):

- Recalibración retroactiva de `CapitalInicial=1000` o de cualquier valor ya congelado en Caso
  1/Caso 2/Caso 3A — D-085 se resuelve hacia adelante, no reinterpretando el pasado.
- Masaniello ni ningún modelo de gestión de riesgo probabilístico (`EVALUACION_MODELOS_GESTION_
  RIESGO_V1.md`, ya descartado).
- Spread, funding, capital real, ejecución live, integración con exchange.
- Optimización automática de parámetros económicos (`TasaMargen`/`Costes`/`PorcentajeRiesgo`
  siguen siendo input explícito del experimento, no calculado por el sistema).
- Ranking financiero entre estrategias (extiende D-014/D-047/D-076).
- D-055 (catálogo de métricas de martingala) y D-044 (entrada × resolución) — no se activan por
  esta fase, permanecen con su condición de activación ya declarada en
  `INDICE_DECISIONES_GLOBAL_V1.md`.

---

## 8. Decisiones nuevas

Numeración reservada desde **D-091**. Ninguna decisión se resuelve dentro de esta propuesta — el
siguiente documento (`DECISIONES_CASO4_V1.md`, o equivalente) resuelve cada punto abierto de las
secciones 4 y 6 con la misma disciplina de fases anteriores: opciones, evidencia, criterio,
selección explícita del auditor. Primera decisión esperada: si Caso 4 debe tocar `src/` (D-084/
D-085 viven en `src/Domain/Portfolio/`/`src/Domain/Broker/`, a diferencia de Caso 3A que se
mantuvo enteramente en `exploration/`) o si es posible resolverlos manteniendo el motor congelado
mediante una capa adicional en `exploration/` — esta pregunta condiciona todas las demás.

---

## 9. Criterios de cierre del Caso 4

El cierre debe responder:

- ¿D-084 está resuelta con causa raíz corregida, no con un parche sintomático? (evaluado con la
  misma corrida larga que originalmente expuso el problema)
- ¿D-085 tiene una definición explícita y verificable de qué representa `Cantidad`? (sin haber
  recalibrado el pasado)
- ¿Los 3 baselines congelados (Caso 1, Caso 2, Caso 3A) siguen siendo bit-a-bit idénticos?
- ¿Qué partes del modelo económico siguen siendo deuda técnica después de esta fase? (documentado,
  no corregido silenciosamente — mismo principio D-055/D-062/D-084)
- ¿Caso 4 tocó `src/`? Si sí, ¿bajo qué decisión explícita y con qué evidencia de no regresión?

---

## Fuera de alcance de este documento

No se implementó código. No se modifica ningún módulo de Caso 1, Caso 2 ni Caso 3A. No se resuelve
D-084 ni D-085 — solo se declara su alcance. No se decide si Caso 4 toca `src/` — queda para el
documento de decisiones siguiente.

---

## Próximo documento

`DECISIONES_CASO4_V1.md` (numeración D-091 en adelante), resolviendo: si Caso 4 toca `src/` o
permanece en `exploration/`, alcance exacto de las sub-fases 4.0-4.4, y si D-084/D-085 requieren
un cambio de contrato versionado (nuevo campo en `OrderRequest`, o equivalente).
