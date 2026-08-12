# Especificación de Implementación — Reversión a la Media por Z-Score (Caso 3A)

Estado: **documento de diseño implementable — previo a implementación**. Traduce
`ESPECIFICACION_FAMILIA_ESTRATEGIA_CASO3_V1.md` §2 a un diseño concreto, fijando los parámetros
experimentales que quedaron deliberadamente abiertos en esa especificación. No modifica código en
este documento.

---

## 1. Parámetros congelados

Misma convención D-030 (referencia externa reconocible, no calibrada sobre el dataset — igual
criterio que EMA 12/26 en `EstrategiaEmaCross.cs`):

- **`Ventana = 20`**: tamaño de la ventana deslizante para media y desviación estándar. Valor
  convencional en literatura técnica (Bandas de Bollinger estándar usan 20 periodos) — no
  calibrado sobre `BTCUSDT_2024-01-02_2025-01-02`.
- **`UmbralEntrada = 2.0`**: z-score absoluto que dispara la señal (`|z| > 2` ≈ fuera del ~95% de
  una distribución normal — convención estadística estándar, no ajustada al dataset).
- **`UmbralSalida = 0.5`**: z-score absoluto de cierre por reversión — se considera "revertido a la
  media" cuando `|z|` cae por debajo de este umbral, sin exigir llegar exactamente a `z=0`
  (evita cierres tardíos por ruido alrededor de la media exacta).

**Criterio de salida por falla de reversión**: **no incluido en V1**. Si el precio nunca revierte
(el z-score se mantiene persistentemente extremo o sigue alejándose), la posición permanece abierta
indefinidamente — mismo comportamiento que EMA Cross acepta para el caso simétrico (una tendencia
que nunca cruza de vuelta mantiene la posición abierta sin límite). Introducir un stop por
horizonte máximo o por z-score que se dispara aún más lejos sería un segundo parámetro de diseño
sin evidencia que lo requiera todavía — si el resultado de la primera corrida revela que esto
produce posiciones abiertas patológicamente largas, se registra como hallazgo (mismo patrón D-062/
D-083: la disciplina de este proyecto descubre estos casos con datos reales, no por anticipación).

**Posiciones simultáneas**: **no permitidas**. Mismo patrón que las 3 estrategias existentes
(Tres Mosqueteros/MHI: una apuesta en curso a la vez; EMA Cross: `_posicionAbierta` como único
estado, nunca dos direcciones simultáneas) — una única variable de estado
(`_posicionAbierta: Side?`) representa la posición actual; mientras haya una posición abierta, no
se evalúan nuevas señales de entrada (solo se evalúa la condición de cierre), exactamente como
`EstrategiaEmaCross.Observar` estructura su lógica (`if (_posicionAbierta is null)` antes de
evaluar apertura).

**Representación de neutralidad**: `|z| <= UmbralEntrada` sin posición abierta → `Observar` retorna
`Array.Empty<OrderRequest>()` — mismo patrón que EMA Cross retorna vacío durante el warmup y que
Tres Mosqueteros retorna vacío cuando `N%5 != 2`. No hay un tercer estado "neutral" explícito más
allá de "ninguna orden generada en este ciclo".

---

## 2. Punto de integración con `IStrategy`

```csharp
public sealed class EstrategiaZScoreReversion : IStrategy
{
    private readonly int _ventana;
    private readonly decimal _umbralEntrada;
    private readonly decimal _umbralSalida;
    // ... instrumentacion InfoOperacionResuelta, igual patron que EstrategiaEmaCross ...

    private Side? _posicionAbierta;
    private decimal _precioEntradaActual;

    // Estado incremental de ventana deslizante (suma y suma de cuadrados sobre las ultimas
    // `_ventana` velas) — evita el mismo bug O(n^2) ya detectado y corregido en EMA Cross
    // (recalcular el historial completo en cada Observar sobre datasets de ~500k velas en 1m).
    private readonly Queue<decimal> _ventanaClose = new();
    private decimal _sumaVentana;
    private decimal _sumaCuadradosVentana;

    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice) { /* ... */ }
}
```

**Mecanismo de ventana deslizante O(1) por vela**: al agregar una vela nueva, sumar su `Close` a
`_sumaVentana`/`_sumaCuadradosVentana`; si `_ventanaClose.Count > _ventana`, restar el valor más
antiguo (`_ventanaClose.Dequeue()`) de ambas sumas antes de calcular media/desviación — evita
recorrer las últimas `_ventana` velas en cada ciclo, mismo principio que la actualización
incremental de EMA. `Media = _sumaVentana / _ventana`; `Varianza = _sumaCuadradosVentana/_ventana -
Media²`; `DesviaciónEstándar = √Varianza` (guarda: `Varianza < 0` por error de redondeo de punto
flotante acumulado → tratar como `0`, mismo tipo de guarda que D-073/`CalcularDrawdownMaximo` usó
para `pico == 0m`).

**Warmup**: sin señal hasta acumular `_ventana` velas — mismo patrón que EMA Cross
(`_emaCortaActual is null || ...` → `Array.Empty<OrderRequest>()`).

**Desfase N/N+1 (RN-13)**: igual que las 3 estrategias existentes — la señal calculada con
`DataSlice` hasta N se ejecuta contra `Velas[N+1]`, sin excepción, sin necesidad de código adicional
(el motor ya garantiza esto para toda `IStrategy`).

**Ganancia**: comparación contra el precio de entrada real (`_precioEntradaActual`), igual criterio
que EMA Cross — `Buy` gana si `Close_resolución >= Close_entrada`, `Sell` si `<=`.

**Instrumentación**: `InfoOperacionResuelta(OperacionId, MartingalasUsadas: 0, Gano, TimestampEntrada,
TimestampResolucion)` — mismo tipo reutilizado sin modificar, `MartingalasUsadas` siempre `0`
(D-055, hallazgo documentado, no oculto — igual que EMA Cross).

---

## 3. Metadata `CaracteristicasEstrategia` (D-090)

```csharp
public sealed record CaracteristicasEstrategia(bool UsaMartingala);
```

**Ubicación**: junto a `EntradaProtocolo` en `protocolo/EjecutorProtocolo.cs`, o en un archivo
nuevo del mismo namespace (`TD_Project.Protocolo`) — a decidir en la implementación según el punto
exacto de consumo (sección 3, `ESPECIFICACION_FAMILIA_ESTRATEGIA_CASO3_V1.md`). No vive en
`src/`, mismo criterio que toda la infraestructura de laboratorio (D-015).

**Valor para esta estrategia**: `CaracteristicasEstrategia(UsaMartingala: false)`.

**Valores para las estrategias existentes** (necesario para que el reporte no trate "no declarado"
como "no aplica" por accidente — Tres Mosqueteros/MHI deben declarar explícitamente
`UsaMartingala: true`, EMA Cross `UsaMartingala: false`): actualizar los 3 puntos de construcción
existentes de `EntradaProtocolo` (`protocolo/Program.cs` y los usos de prueba en
`TestsEjecutorProtocolo.cs`) para incluir el campo nuevo. Si `CaracteristicasEstrategia` se agrega
como opcional con default `null`/`UsaMartingala: true` (mismo criterio D-061: default =
comportamiento histórico), los call sites existentes de Tres Mosqueteros/MHI no requieren cambio
explícito, solo la estrategia nueva y EMA Cross.

---

## 4. Tratamiento de métricas no aplicables

`ReporteConsolidadoGenerador`/`ReporteEscenariosGenerador` (Caso 1, **congelados**, no se
modifican — mismo criterio D-080 aplicó a Caso 2) no cambian. El tratamiento de "no aplica" se
implementa en un punto de presentación **nuevo**, no en los generadores ya congelados — a definir
exactamente cuál en la fase de implementación (candidatos: un nuevo campo opcional en
`ResultadoOperacional`/`AnalizadorOperacional`, o una capa de formato en el reporte de Caso 3 que
lea `CaracteristicasEstrategia` antes de mostrar `ResolucionDeIntentos`).

**Regla de presentación**: si `CaracteristicasEstrategia.UsaMartingala == false`, todo campo de
`ResolucionDeIntentos` se presenta como `"no aplica"` en el reporte, nunca como `"0.0%"` — mismo
principio D-078 (`null` ≠ `0`) aplicado aquí a nivel de texto de reporte en vez de tipo `decimal?`,
porque `ResolucionDeIntentos` en sí no cambia de tipo (sigue siendo `decimal`, las fórmulas no se
tocan) — solo la capa que lo traduce a texto legible decide cómo mostrarlo.

**No se modifica**: `AnalizadorOperacional.cs:62-67` (las fórmulas de `PctSeguro`), ni el tipo
`ResolucionDeIntentos` en sí — D-088 exige distinguir aplicabilidad en la presentación, no alterar
el cálculo ya congelado.

---

## 5. Pruebas obligatorias antes de cerrar

- **P1 — Señal de entrada correcta**: dataset sintético con un desvío conocido produce `|z| >
  UmbralEntrada` exactamente en la vela esperada, verificado calculando z-score a mano.
- **P2 — Señal de salida correcta**: tras una entrada, una serie sintética que revierte a la media
  produce el cierre en la vela donde `|z| <= UmbralSalida`, verificado a mano.
- **P3 — Sin posición simultánea**: mientras `_posicionAbierta != null`, ninguna nueva señal de
  entrada genera una segunda orden de apertura — solo se evalúa condición de cierre.
- **P4 — Ventana deslizante O(1) equivalente al cálculo directo**: sobre un dataset sintético
  pequeño, el resultado de la actualización incremental (suma/suma de cuadrados) coincide
  exactamente con recalcular media/desviación desde cero sobre las últimas `Ventana` velas en cada
  ciclo — mismo tipo de verificación que EMA Cross aplicó a su actualización incremental.
- **P5 — Rendimiento sobre 1m**: la corrida completa sobre el timeframe 1m (~500,000 velas)
  termina en un tiempo razonable — verificación directa del mismo tipo de bug que EMA Cross tuvo en
  su primera versión (O(n²) por recalcular el historial completo).
- **P6 — Determinismo**: misma entrada produce las mismas operaciones en dos ejecuciones,
  verificado por `EjecutorProtocolo.VerificarDeterminismo` (ya existente, sin cambios).
- **P7 — Metadata correcta**: `CaracteristicasEstrategia.UsaMartingala == false` para esta
  estrategia; el reporte muestra "no aplica" (no "0.0%") en `ResolucionDeIntentos` para su corrida.
- **P8 — Regresión de Caso 1/Caso 2**: agregar `CaracteristicasEstrategia` a `EntradaProtocolo` no
  cambia ningún resultado de las corridas existentes (Tres Mosqueteros/MHI/EMA Cross) ni el
  `HashCompuesto` de `baseline_final/`/`baseline_financiero_final/`.

---

## 6. Qué partes de D-055 toca esta implementación y cuáles quedan fuera

**Toca**: la presentación de `ResolucionDeIntentos` para estrategias sin martingala pasa de
"0.0% sin distinción" a "no aplica, distinguible de un 0% real" — resuelve la ambigüedad que D-055
documentó como hallazgo, para las 2 estrategias que hoy la sufren (EMA Cross y la nueva).

**Queda fuera**: no se rediseña el catálogo de métricas de fondo — `ResolucionDeIntentos` sigue
siendo un record obligatorio en `ReporteOperacional` (no se vuelve opcional), las fórmulas de
`AnalizadorOperacional.cs` no cambian, y no se introduce la Opción C de D-088 ("nuevo catálogo de
métricas universales separado"). D-055 permanece parcialmente activada (D-089), no cerrada — el
rediseño completo, si se justifica, es una decisión futura separada.

---

## Fuera de alcance de este documento

No se implementa código. No se fija el punto exacto de consumo de `CaracteristicasEstrategia`
dentro del pipeline más allá de lo descrito en la sección 3 — se decide durante la implementación,
verificado contra el código real en ese momento (mismo criterio que D-062/D-067 encontraron su
punto de integración exacto solo al escribir la prueba, no antes).

---

## Próximo paso

Autorización de implementación bajo el alcance de este documento — restricciones esperables: no
tocar `ReporteConsolidadoGenerador.cs`/`ReporteEscenariosGenerador.cs` (congelados), no modificar
`AnalizadorOperacional.cs` fórmulas existentes, no tocar `src/`, verificar P1-P8 antes de cerrar,
confirmar hash de ambos baselines sin cambio.
