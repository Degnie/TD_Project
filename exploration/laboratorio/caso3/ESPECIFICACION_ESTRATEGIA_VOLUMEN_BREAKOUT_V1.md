# Especificación de Implementación — `EstrategiaVolumenBreakout` (Caso 3B, D-106)

Estado: **implementado y verificado — documento actualizado post-implementación con 2 hallazgos
corregidos** (ver §4 y §7.1). Traduce D-099 a D-107 (resueltas en `DECISIONES_CASO3B_V1.md`) en una
clase concreta, su ubicación, algoritmo exacto y batería de pruebas. La estrategia es
**bidireccional** (Long y Short) — D-105 se amplió con ruptura simétrica a la baja, y D-107 fijó
cierre por señal contraria (reversión), no por pérdida de volumen. Mismo patrón documental que
`ESPECIFICACION_CLASIFICADOR_INTENCION_ORDEN_V1.md` (Caso 4) y
`ESPECIFICACION_IMPLEMENTACION_ESTRATEGIA_NEUTRAL_CASO3_V1.md` (Caso 3A).

**Nota de fidelidad**: este documento fue escrito antes de implementar y contenía 2 imprecisiones
frente al código real, detectadas durante la implementación y corregidas aquí — no en un documento
aparte — para que el texto describa lo que efectivamente existe. Ver `AUDITORIA_CASO3B_V1.md` para
el registro completo de ambos hallazgos y su resolución.

---

## 1. Ubicación y nombre

`exploration/EstrategiaVolumenBreakout.cs` — mismo directorio que las 5 estrategias existentes
(`EstrategiaTresMosqueteros.cs`, `EstrategiaMhiMayoria.cs`, `EstrategiaEmaCross.cs`,
`EstrategiaZScoreReversion.cs`, `EstrategiaNeutral.cs`), no en `exploration/laboratorio/caso3/`
(que queda reservado para el módulo satélite de pruebas/programa, mismo patrón ya usado por
Z-Score/Neutral: la estrategia vive en `exploration/`, las pruebas en `caso3/`).

Namespace: `TD_Project.Exploration` (mismo que las 5 estrategias existentes).

Clase: `EstrategiaVolumenBreakout`, `sealed`, implementa `IStrategy` (D-104).

---

## 2. Objetos internos de condición (D-100)

```csharp
namespace TD_Project.Exploration;

internal sealed class CondicionVolumen
{
    private readonly int _ventana;
    private readonly decimal _multiplo;
    private readonly Queue<decimal> _ventanaVolumen = new();
    private decimal _sumaVentana;

    public CondicionVolumen(int ventana, decimal multiplo) { ... }

    // Actualiza la ventana con el volumen de la vela actual y evalúa la condición.
    // Debe llamarse exactamente una vez por vela, en orden, antes de EvaluarBreakout.
    public bool Evaluar(decimal volumenActual) { ... }
}

internal sealed class CondicionBreakout
{
    private readonly int _ventana;
    private readonly Queue<decimal> _ventanaMaximos = new();
    private readonly Queue<decimal> _ventanaMinimos = new();

    public CondicionBreakout(int ventana) { ... }

    // Compara High/Low contra el maximo/minimo de la ventana ANTES de incorporar la vela actual
    // (D-105: ambos extremos excluyen la vela actual). Actualiza ambas ventanas despues de
    // comparar. RupturaAlcista y RupturaBajista nunca son true simultaneamente (Close no puede
    // superar el maximo y perforar el minimo de la misma ventana en la misma vela).
    public (bool RupturaAlcista, bool RupturaBajista) Evaluar(decimal closeActual, decimal highActual, decimal lowActual) { ... }
}
```

**Estado propio encapsulado por condición** (D-104): `CondicionVolumen` mantiene su ventana
deslizante de volumen (suma O(1), mismo patrón que `EstrategiaZScoreReversion`/
`CalculadoraLotes`); `CondicionBreakout` mantiene sus dos ventanas deslizantes (máximos y mínimos)
— la extensión bidireccional de D-105 comparte la misma ventana temporal para ambos extremos, no
introduce una tercera estructura de estado. Ninguna condición comparte estado con la otra ni con la
clase contenedora — cada una es una unidad evaluable de forma aislada (criterio de éxito de
`PROPUESTA_CASO3B_V1.md` §6).

**Complejidad real de `CondicionBreakout.Evaluar`** (hallazgo 2, corregido tras revisión de
auditoría — ver `AUDITORIA_CASO3B_V1.md`): la implementación real usa `Queue<decimal>.Max()`/
`.Min()` sobre cada ventana, que es **O(N) por evaluación, no O(1)** — a diferencia de
`CondicionVolumen`, que sí mantiene una suma acumulada O(1). Con `N=20` fijo (D-105) esto no
representa degradación observable (P12 confirma rendimiento sobre 100k velas), pero la afirmación
original de esta especificación ("cálculo O(1) por vela") era incorrecta. Descripción correcta:
**ventana deslizante con coste lineal sobre una ventana fija de tamaño 20**, no un algoritmo O(1)
como el de `CondicionVolumen`/`EstrategiaZScoreReversion`.

**Orden de actualización dentro de `CondicionBreakout.Evaluar`** (crítico para D-105 "ambos
extremos excluyen la vela actual"): comparar `closeActual` contra el máximo/mínimo acumulados en
las ventanas **antes** de agregar `highActual`/`lowActual` de la vela actual a esas ventanas. Si el
orden se invirtiera (actualizar primero, comparar después), la vela actual podría participar en su
propio extremo de referencia, reproduciendo la circularidad que D-105 explícitamente descartó.

**Ambas ventanas (máximos y mínimos) son `Queue<decimal>` de tamaño fijo `N=20`** (D-105) —
comparten ventana temporal por construcción (ambas leen las mismas 20 velas previas), pero siguen
siendo independientes de la ventana de `CondicionVolumen` (coincidencia de tamaño declarada por
simplicidad, no por relación obligatoria, ya registrado en D-105).

---

## 3. Clase contenedora

```csharp
public sealed class EstrategiaVolumenBreakout : IStrategy
{
    private readonly CondicionVolumen _condicionVolumen;
    private readonly CondicionBreakout _condicionBreakout;
    private readonly Action<ResultadoEvaluacionCondiciones>? _onEvaluacion;
    private readonly Action<InfoOperacionResuelta>? _onOperacionResuelta;

    private Side? _posicionAbierta;
    private int _siguienteOperacionId = 1;
    private int _operacionIdActual;
    private long _timestampEntradaActual;

    public EstrategiaVolumenBreakout(
        int ventanaVolumen = 20, decimal multiploVolumen = 1.5m, int ventanaBreakout = 20,
        Action<ResultadoEvaluacionCondiciones>? onEvaluacion = null,
        Action<InfoOperacionResuelta>? onOperacionResuelta = null)
    { ... }

    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice) { ... }
}
```

**Parámetros con default = valores de D-105**: permite instanciar `new
EstrategiaVolumenBreakout()` sin argumentos para el caso convencional, mismo patrón que
`EstrategiaNeutral(ciclo: 10)` no exige pero admite override — los tests de warmup/ventana
distinta (sección 6) pueden pasar valores explícitos sin violar D-105 (los valores de D-105 son la
convención de la estrategia congelada, no una restricción sobre cómo se prueban sus componentes
internos de forma aislada).

**Constructores por defecto de `int`/`decimal` para `ventanaVolumen=20`/`multiploVolumen=1.5m`/
`ventanaBreakout=20`**: son los valores resueltos en D-105, no una elección nueva de esta
especificación.

---

## 4. Algoritmo de `Observar` (bidireccional, D-105 ampliada + D-107) — actualizado con el
mecanismo real de reversión (hallazgo 1, ver `AUDITORIA_CASO3B_V1.md`)

```
1. Actualizar y evaluar CondicionVolumen con Volume de la vela actual.
2. Si CondicionVolumen.Evaluar == false:
     - Reportar ResultadoEvaluacionCondiciones(Primaria: false, Secundaria: null, Accion: Ninguna)
     - Actualizar CondicionBreakout de todas formas (ambas ventanas, maximos y minimos, deben
       avanzar independientemente de si la condicion primaria se cumplio, para no perder
       continuidad cuando la condicion de volumen empiece a cumplirse mas adelante)
     - return Array.Empty<OrderRequest>()
3. Si CondicionVolumen.Evaluar == true:
     - Evaluar CondicionBreakout con Close/High/Low de la vela actual, obteniendo
       (RupturaAlcista, RupturaBajista) — internamente actualiza ambas ventanas tras comparar
       (ver seccion 2)
     - Si RupturaAlcista == false Y RupturaBajista == false:
         - Reportar ResultadoEvaluacionCondiciones(Primaria: true, Secundaria: false, Accion: Ninguna)
         - return Array.Empty<OrderRequest>()
     - DireccionSenal = RupturaAlcista ? Buy : Sell
     - Si _posicionAbierta == null:
         - Emitir 1 OrderRequest(DireccionSenal, Market, 1m) — apertura simple.
         - Registrar _posicionAbierta = DireccionSenal. Accion = OrdenEmitida.
     - Si _posicionAbierta == DireccionSenal (misma direccion):
         - No emitir ninguna orden (D-104, una posicion maxima, sin escalado).
         - Accion = OrdenOmitidaPorPosicionAbierta.
     - Si _posicionAbierta != DireccionSenal (senal contraria, D-107):
         - Emitir 2 OrderRequest: (a) cierre — Side opuesto a _posicionAbierta, Cantidad=1m;
           (b) apertura — DireccionSenal, Cantidad=1m. AMBAS en la misma llamada a Observar,
           mismo patron ya usado por EstrategiaNeutral en su punto de reversion — NO una unica
           orden de magnitud mayor resuelta por ResolutorCrossZero (ver nota debajo).
         - Registrar _posicionAbierta = DireccionSenal. Accion = OrdenEmitida.
```

**Nota sobre el mecanismo de reversión (hallazgo 1 corregido)**: el diseño original de este
documento asumía que una única `OrderRequest` de magnitud mayor a la posición existente activaría
`ResolutorCrossZero` (mecanismo de motor ya usado por `EstrategiaZScoreReversion`/
`EstrategiaCrossZeroControlada`). Verificado contra el comportamiento real de `AplicadorFill`: con
`Cantidad=1m` fija tanto en la posición existente como en la nueva orden, una sola orden nunca
cruza cero (`magnitudFill == magnitudPosicion`, no `>`) — produce un `CierreTotal`, no un
`CrossZero`. `EstrategiaNeutral` ya resolvía esto emitiendo explícitamente 2 `OrderRequest` en su
punto de reversión; `EstrategiaVolumenBreakout` adopta el mismo patrón exacto. El motor sigue sin
requerir ningún cambio — ambas órdenes se procesan con la lógica de `AplicadorFill` ya existente,
la primera como cierre (`CierreTotal`), la segunda como apertura nueva.

**Cierre de posición (D-107, resuelta)**: no existe un cierre "puro" separado de la apertura
contraria — la posición se cierra exclusivamente por reversión (`Long → Short` o `Short → Long`)
cuando aparece la señal jerárquica completa (volumen válido + breakout) en sentido opuesto al de
la posición abierta. Sin stop por pérdida de volumen (Opción A rechazada), sin salida basada
únicamente en precio sin la condición de volumen (Opción B unilateral rechazada) — la señal de
cierre es exactamente la misma regla jerárquica que abre una posición, evaluada en la dirección
contraria a la posición actual.

**Consecuencia de diseño**: la estrategia nunca emite una orden de cierre "pura" desacoplada de
una apertura — toda salida es, desde la perspectiva de `Observar`, una señal de entrada en sentido
contrario que dispara 2 `OrderRequest` (cierre + apertura, ver §4). La estrategia no necesita
calcular cantidades de cobertura ni conocer la posición real (P-002 preservado); el motor procesa
ambas órdenes con `AplicadorFill` ya existente sin ningún cambio — la primera actúa como
`CierreTotal` (magnitud igual a la posición existente), la segunda como apertura nueva.

---

## 5. Observabilidad (D-101)

```csharp
public readonly record struct ResultadoEvaluacionCondiciones(
    bool Primaria,
    bool? Secundaria,        // null si Primaria == false (D-099: secundaria no se evalua)
    Side? DireccionSenal,    // Buy/Sell si Secundaria == true, null en otro caso
    AccionEvaluacion Accion);

public enum AccionEvaluacion
{
    Ninguna,
    OrdenEmitida,
    OrdenOmitidaPorPosicionAbierta   // senal en la misma direccion de una posicion ya abierta
}
```

**`DireccionSenal`**: agregado respecto al diseño original (unidireccional) para distinguir, en el
resultado reportado, si la ruptura fue alcista o bajista — necesario tras la ampliación
bidireccional de D-105/D-107, ya que `Secundaria == true` por sí solo no indica el sentido de la
señal.

**Mecanismo**: callback opcional `Action<ResultadoEvaluacionCondiciones>? onEvaluacion`, invocado
exactamente una vez por cada llamada a `Observar` — mismo patrón ya establecido por
`Action<InfoOperacionResuelta>? onOperacionResuelta` en las 5 estrategias existentes. Sin campo
público mutable, sin nuevo sistema de eventos, sin metadata en `IStrategy` (D-101, confirmado).

**`Secundaria: bool?` en vez de `bool`**: representa explícitamente el caso "no evaluado" (D-099:
la secundaria solo se evalúa si la primaria se cumplió) — `null` no es un valor por defecto
implícito, es la representación fiel de "esta condición no llegó a ejecutarse", distinta de
`false` ("se ejecutó y no se cumplió").

---

## 6. Tratamiento de datos insuficientes al inicio (warmup)

Mientras `CondicionVolumen`/`CondicionBreakout` no acumulen `N=20` velas en su ventana respectiva,
ninguna condición es evaluable con sentido — mismo patrón de warmup ya usado por
`EstrategiaZScoreReversion` (sin señal hasta acumular `Ventana` velas).

**Regla**: durante warmup, `Observar` devuelve `Array.Empty<OrderRequest>()` y reporta
`ResultadoEvaluacionCondiciones(Primaria: false, Secundaria: null, Accion: Ninguna)` — tratado
como "condición primaria no cumplida" a efectos de observabilidad (no como un cuarto estado
distinto), porque conceptualmente no hay diferencia observable entre "no hay suficiente historia
para evaluar" y "la condición no se cumple" desde la perspectiva de quien consume el resultado.

**Ambas ventanas deben alcanzar `N=20` independientemente antes de que la primera evaluación real
sea posible** — si tienen tamaños distintos en una prueba (sección 7), el warmup dura hasta que la
mayor de las dos se llene.

---

## 7. Pruebas obligatorias

Mismo patrón P1-P8 de `TestsEstrategiaNeutral.cs`/`TestsCaso3.cs`, adaptado a la lógica jerárquica
de esta familia. Archivo: `exploration/laboratorio/caso3/TestsEstrategiaVolumenBreakout.cs`,
namespace `TD_Project.Caso3` (mismo módulo satélite, reutilizado — D-104 confirmó no crear uno
nuevo).

- **P1 — Jerarquía respetada**: con volumen insuficiente (`Primaria=false`), `Secundaria` debe ser
  `null` en el resultado reportado — construir un dataset sintético donde el volumen nunca supere
  el múltiplo y confirmar que `CondicionBreakout.Evaluar` nunca se invoca aun cuando el precio sí
  rompe el rango (verificable con un mock/spy sobre la condición, o verificando que
  `Secundaria == null` en cada evaluación).
- **P2 — Entrada Long**: dataset sintético con volumen elevado (`>1.5×` media) coincidiendo con
  ruptura de máximo de 20 velas → confirma `OrderRequest(Side.Buy, ...)` emitido,
  `Accion == OrdenEmitida`, `DireccionSenal == Side.Buy`.
- **P3 — Entrada Short**: simétrico a P2, ruptura de mínimo de 20 velas con volumen elevado →
  `OrderRequest(Side.Sell, ...)`, `DireccionSenal == Side.Sell`.
- **P4 — Volumen sin breakout**: volumen elevado sin ruptura de precio en ningún sentido →
  `Primaria=true`, `Secundaria=false`, sin orden emitida.
- **P5 — Máximo y mínimo excluyen la vela actual**: construir una vela cuyo `Close` sea exactamente
  igual al máximo de las 19 anteriores más ella misma, pero menor al máximo de las 20 anteriores
  sin incluirla — confirma ausencia de circularidad (D-105); repetir simétricamente para el mínimo.
- **P6 — Comparación estricta**: `Close_actual == Máximo20Anterior` exacto (y simétrico para el
  mínimo) → no debe contar como ruptura en ningún sentido (`>`/`<` estrictos, D-105).
- **P7 — Cierre por señal contraria (reversión Long → Short)**: posición Long abierta; en una vela
  posterior aparece volumen válido + ruptura de mínimo → confirma `OrderRequest(Side.Sell, ...)`
  emitido (D-107), y verificar contra `AplicadorFill`/`ResolutorCrossZero` reales (no solo la
  `OrderRequest` emitida) que la posición neta resultante es `Short`, no un cierre parcial o
  residual — mismo patrón de verificación con evidencia directa ya usado en D-095 (Caso 4).
- **P8 — Cierre por señal contraria (reversión Short → Long)**: simétrico a P7.
- **P9 — Sin posiciones simultáneas en la misma dirección**: con posición Long ya abierta, una
  nueva combinación volumen+ruptura alcista no debe emitir una segunda orden de apertura —
  `Accion == OrdenOmitidaPorPosicionAbierta`.
- **P10 — Determinismo**: dos instancias independientes sobre el mismo dataset producen secuencias
  idénticas de `Side` — mismo patrón que P4/P1 de Neutral/Z-Score respectivamente.
- **P11 — Metadata**: `CaracteristicasEstrategia(UsaMartingala: false)` produce "no aplica" en el
  reporte — mismo patrón que P5 de Neutral (D-104: hereda sin martingala).
- **P12 — Rendimiento**: sobre dataset de 100k velas, cálculo O(1) por vela — mismo umbral y
  patrón que P6 de Neutral.
- **P13 — Integración en el pipeline sin cambios de código**: `EjecutorProtocolo` sobre dataset
  real (`BTCUSDT_2024-01-02_2025-01-02`, 1D) — `Estado == Success`, `MetricasFinancieras` poblado.
- **P14 — Regresión**: `EstrategiaVolumenBreakout` no afecta corridas independientes de otras
  estrategias — mismo patrón que P8 de Neutral (comparación de `HashCompuesto` entre corridas
  idénticas de una estrategia de control).

---

## 8. Restricciones confirmadas (heredadas, sin relajar)

- Sin modificar `src/`, `IStrategy`, ni ninguna de las 5 estrategias existentes.
- Sin activar `GestorCapital`/sizing/`ValidadorCapacidad`/`ClasificadorIntencionOrden` (Caso 4).
- Sin modificar ningún reporte o métrica financiera ya congelados.
- Sin optimización de ningún parámetro — todos los valores numéricos provienen de D-105.
- Sin agregar metadata nueva de capacidades (`UsaMultiplesCondiciones` o similar) — D-101 ya
  confirmó que no hay consumidor concreto que la requiera.

---

## Fuera de alcance de este documento

No se implementó código todavía — este documento lo habilita, no lo reemplaza. D-105 (ampliada) y
D-107 ya están resueltas — no queda ninguna decisión D-N pendiente para esta especificación.

---

## Próximo paso

Aprobación explícita del auditor. Tras eso: implementación de
`exploration/EstrategiaVolumenBreakout.cs` y
`exploration/laboratorio/caso3/TestsEstrategiaVolumenBreakout.cs`, ejecución de P1-P14, verificación
de no regresión (126/126 tests de producción sin cambio — esta fase no toca `src/`/`tests/`) y
confirmación de que los 4 baselines congelados (`caso1-v1-experimental`, `caso2-v1-experimental`,
`caso3a-v1-experimental`, `caso4-v1-experimental`) permanecen intactos.
