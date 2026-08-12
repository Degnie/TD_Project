# Especificación de Implementación — Gestores de Riesgo Intercambiables (Caso 5A)

Estado: **especificación previa a implementación**. Traduce D-108 a D-111
(`DECISIONES_CASO5_V1.md`) a diseño de código concreto — nombres, firmas, ubicación de archivos.
**Ningún código se modifica en este documento.**

---

## 1. Hallazgo estructural adicional (detectado en esta especificación, no en la propuesta)

`GestorCapital.Ajustar` (`src/Domain/Portfolio/GestorCapital.cs:30`) tiene la firma:

```csharp
Ajustar(IReadOnlyList<OrderRequest> requests, PortfolioState portfolio,
        ConfiguracionSizing? sizing, decimal precioReferencia, decimal tasaMargen)
```

Recibe `precioReferencia` como **escalar** (el Close de la vela siguiente, D-094) — no recibe
historial de velas ni ninguna ventana de precios.

**Consecuencia para Volatility Sizing (C)**: una medida de volatilidad reciente requiere una
ventana de precios (ej. desviación estándar de N Closes, mismo patrón que
`EstrategiaZScoreReversion._ventanaClose`, citado como precedente conceptual en
`PROPUESTA_CASO5_V1.md` §4). `GestorCapital.Ajustar` no tiene ese dato hoy — ni tampoco
`BacktestRunner.cs:57`, que solo pasa `config.Velas[n + 1].Close`.

**Esto no es una decisión nueva** (D-108 a D-111 no la cubrieron porque no era visible hasta bajar
al nivel de firma exacta) — es una consecuencia directa de D-108 (el gestor calcula cantidad) que
debe resolverse aquí, como parte de la traducción a código, sin reabrir ninguna D-N:

**Resolución de especificación**: `IGestorRiesgo.CalcularCantidad` recibe la ventana de velas
necesaria vía `DataSlice.VelasHastaN` (ya disponible en `BacktestRunner` en el mismo punto donde
se invoca `strategy.Observar`, `BacktestRunner.cs:46-47`) en vez de un escalar. Esto cambia la
firma de `GestorCapital.Ajustar` para aceptar `DataSlice` además de `precioReferencia` — es una
ampliación de dato disponible, no un cambio de responsabilidad: `GestorCapital` sigue sin conocer
lógica de estrategia, `DataSlice` ya es un tipo público sin acoplamiento a ninguna estrategia
concreta (mismo tipo que reciben las 6 estrategias). Fixed Fractional y Fixed Risk ignoran la
ventana (no la necesitan); solo Volatility Sizing la consume.

---

## 2. `IGestorRiesgo` — contrato

**Ubicación**: `src/Domain/Portfolio/IGestorRiesgo.cs`

```csharp
namespace TD_Project.Domain.Portfolio;

// spec: Caso 5A D-108 — unico metodo, calcula cantidad SOLO para Apertura/Aumento. No participa
// en clasificacion de intencion ni normalizacion de Cross-Zero (GestorCapital conserva esa
// responsabilidad, D-092/D-095, sin cambios).
public interface IGestorRiesgo
{
    decimal CalcularCantidad(PortfolioState portfolio, DataSlice dataSlice, decimal precioReferencia, decimal tasaMargen);
}
```

### 2.1 `IIdentidadGestorRiesgo` — contrato separado (precisión derivada de D-109)

**Ubicación**: `src/Domain/Portfolio/IIdentidadGestorRiesgo.cs`

```csharp
namespace TD_Project.Domain.Portfolio;

// spec: Caso 5A, precision derivada de D-109 (DECISIONES_CASO5_V1.md) — la identidad del gestor
// activo pertenece a la identidad experimental economica (D-082), no al contrato funcional de
// IGestorRiesgo (D-108: responsabilidad unica, calcular cantidad). Capacidad separada e
// implementada aparte para no mezclar calculo con identificacion.
public interface IIdentidadGestorRiesgo
{
    // Determinista, estable, basado en configuracion declarada — nunca en resultado de ejecucion
    // (sin retorno/drawdown/metricas). Formato de convencion: "<nombre-gestor>:v1:param=valor:...".
    string ObtenerIdentidadConfiguracion();
}
```

Cada implementación concreta de gestor implementa **ambas** interfaces
(`GestorFixedFractional : IGestorRiesgo, IIdentidadGestorRiesgo`) — son capacidades distintas, no
una jerarquía; `IGestorRiesgo` no depende de `IIdentidadGestorRiesgo` ni viceversa.

**Por qué recibe `PortfolioState` completo y no solo `Cash`/`Margin`**: Fixed Fractional necesita
`Cash - Margin` (capital disponible); un futuro gestor podría necesitar `LotesVivos` (ej. para
ajustar por posición ya abierta). Mismo nivel de acceso que ya tiene `GestorCapital.Ajustar` hoy —
no es una ampliación de superficie, es preservar la que ya existe.

**Por qué no recibe `ConfiguracionSizing`**: el gestor ya está parametrizado en su propia
instancia (D-109) — no necesita leer su configuración desde afuera, la tiene inyectada en el
constructor de cada implementación concreta.

---

## 3. `ConfiguracionSizing` — forma extendida

**Ubicación**: `src/Domain/Shared/ConfiguracionSizing.cs` (modificado)

```csharp
namespace TD_Project.Domain.Shared;

// spec: Caso 5A D-109 — describe una eleccion (que gestor esta activo), no contiene logica de
// calculo (eso vive en la implementacion de IGestorRiesgo). Default = null preservado sin cambios
// (D-061/D-069).
public sealed record ConfiguracionSizing(IGestorRiesgo GestorActivo)
{
    public static ConfiguracionSizing? Default => null;
}
```

**Ruptura de compatibilidad con `PorcentajeRiesgo` como campo directo**: el campo
`PorcentajeRiesgo` desaparece del record — pasa a ser un parámetro del constructor de
`GestorFixedFractional` (§4). Todo código que construye `ConfiguracionSizing(porcentaje)`
directamente debe migrar a `ConfiguracionSizing(new GestorFixedFractional(porcentaje))`.

**Puntos de código a migrar — búsqueda exhaustiva confirmada** (`Grep` sin restricción de
carpeta sobre todo el repositorio, no solo `src/` como la primera pasada de esta especificación
asumió incorrectamente):

1. `tests/Application.Tests/GestorCapitalTests.cs` — 12 llamadas a `new
   ConfiguracionSizing(PorcentajeRiesgo: 0.1m)` (y variantes `0.05m`). Suite de producción
   protegida (126/126) — migrar cada una a `new ConfiguracionSizing(new
   GestorFixedFractional(0.1m))`, sin cambiar ninguna aserción de resultado (el comportamiento de
   Fixed Fractional no cambia).
2. `exploration/laboratorio/validacion_integral/TestsValidacionIntegral.cs:153,218` — 2 llamadas
   posicionales `new ConfiguracionSizing(0.1m)`, misma migración.
3. `exploration/laboratorio/protocolo/IdentidadExperimentoCompleta.cs:61` — **no construye
   `ConfiguracionSizing`, pero lee `sizing.PorcentajeRiesgo` para el hash económico**. Este no es
   un caso de "migrar una llamada al constructor" — es el hallazgo que motivó la precisión
   derivada de D-109 (`DECISIONES_CASO5_V1.md`, sección D-109), resuelto en el §2.1/§7 de este
   documento mediante `IIdentidadGestorRiesgo`, no mediante una migración mecánica de firma.
4. `exploration/laboratorio/protocolo/EjecutorProtocolo.cs:68`,
   `exploration/laboratorio/modelo_financiero/baseline_financiero/ProgramBaselineFinanciero.cs:24`:
   solo declaran/propagan `ConfiguracionSizing?` como parámetro — no construyen ni leen campos
   internos, no requieren cambio.

Ningún otro archivo de `src/` construye `ConfiguracionSizing` directamente fuera de su propia
definición (confirmado).

---

## 4. Implementaciones concretas

**Ubicación**: `src/Domain/Portfolio/GestoresRiesgo/` (carpeta nueva, agrupa las 3
implementaciones — mismo criterio de organización por carpeta que `Domain/Strategy/` agrupa
estrategias... **no existe tal carpeta hoy**; verificar en implementación si se sigue el patrón
plano de `Domain/Portfolio/` en su lugar, evitando anticipar una convención no establecida).

### 4.1 `GestorFixedFractional` — migración del actual, sin cambio de comportamiento

```csharp
public sealed class GestorFixedFractional : IGestorRiesgo, IIdentidadGestorRiesgo
{
    private readonly decimal _porcentajeRiesgo;
    public GestorFixedFractional(decimal porcentajeRiesgo) => _porcentajeRiesgo = porcentajeRiesgo;

    public decimal CalcularCantidad(PortfolioState portfolio, DataSlice dataSlice, decimal precioReferencia, decimal tasaMargen)
    {
        var capitalDisponible = portfolio.Cash - portfolio.Margin;
        var margenObjetivo = capitalDisponible * _porcentajeRiesgo;
        return margenObjetivo / (precioReferencia * tasaMargen);
    }

    public string ObtenerIdentidadConfiguracion() => $"fixed-fractional:v1:riesgo={_porcentajeRiesgo}";
}
```

Exactamente las líneas 37-39 actuales de `GestorCapital.Ajustar`, sin ninguna modificación de
fórmula — **verificación obligatoria**: correr los 5 baselines congelados con
`ConfiguracionSizing(new GestorFixedFractional(mismoPorcentaje))` y confirmar
`HashCompuesto`/`HashConfiguracionEconomica` idénticos a los valores ya registrados (mismo criterio
usado en cada fase desde Caso 2).

### 4.2 `GestorFixedRisk`

Riesgo monetario fijo por operación (no proporcional a capital). Fórmula:
`cantidad = MontoRiesgoFijo / (precioReferencia * tasaMargen)`, donde `MontoRiesgoFijo` es un
parámetro del constructor (unidad monetaria, ej. "100"), fijado por convención declarada — nunca
calibrado (D-030).

```csharp
public sealed class GestorFixedRisk : IGestorRiesgo, IIdentidadGestorRiesgo
{
    private readonly decimal _montoRiesgoFijo;
    public GestorFixedRisk(decimal montoRiesgoFijo) => _montoRiesgoFijo = montoRiesgoFijo;

    public decimal CalcularCantidad(PortfolioState portfolio, DataSlice dataSlice, decimal precioReferencia, decimal tasaMargen)
        => _montoRiesgoFijo / (precioReferencia * tasaMargen);

    public string ObtenerIdentidadConfiguracion() => $"fixed-risk:v1:monto={_montoRiesgoFijo}";
}
```

**Pregunta abierta para pruebas, no para diseño**: `MontoRiesgoFijo` no valida contra
`CapitalDisponible` en este cálculo — puede exceder el capital real. Esto ya lo cubre
`ValidadorCapacidad`/`CalculadoraReservaPreventiva` aguas abajo (`BacktestRunner.cs:70-73`,
registra incapacidad sin bloquear, D-059/D-060) — **mismo comportamiento que Fixed Fractional ya
tiene hoy si `PorcentajeRiesgo` es alto**, no es un caso nuevo a cubrir, solo a confirmar con una
prueba explícita (§6).

### 4.3 `GestorVolatilitySizing`

Exposición inversamente proporcional a volatilidad reciente (mayor volatilidad → menor cantidad).
Requiere ventana de Closes vía `DataSlice.VelasHastaN` (§1). Fórmula de referencia (a fijar por
convención, no calibrar):

```
desviacion = desviacion estandar de los ultimos N Closes (misma formula O(1) que
             EstrategiaZScoreReversion, ventana + suma + suma de cuadrados)
cantidad   = (CapitalDisponible * PorcentajeRiesgoBase) / (precioReferencia * tasaMargen * FactorVolatilidad)
FactorVolatilidad = max(1, desviacion / DesviacionReferencia)
```

`DesviacionReferencia` es un parámetro de convención (ej. desviación "normal" esperada del
instrumento) — **su origen debe declararse explícitamente en la implementación, no inventarse
ad-hoc**: mismo principio D-030/D-016 ya aplicado a todo parámetro no derivable del propio dataset
bajo evaluación.

**Warmup**: si `dataSlice.VelasHastaN.Count < N`, el gestor no tiene ventana suficiente — debe
devolver `0m` (ninguna orden de Apertura/Aumento se ajusta a cantidad positiva) en vez de lanzar
excepción, mismo patrón de warmup ya usado por `EstrategiaZScoreReversion`/`EstrategiaEmaCross`.
Esto es una decisión de especificación, no una D-N: es la única forma consistente con el manejo de
warmup ya establecido en el proyecto.

`ObtenerIdentidadConfiguracion()` para este gestor: `"volatility-sizing:v1:ventana={N}:base=
{PorcentajeRiesgoBase}:desviacionReferencia={DesviacionReferencia}"` — todos los parámetros
declarados por convención, ninguno calculado en tiempo de ejecución.

### 4.4 `GestorCapital.Ajustar` — orquestación resultante

```csharp
public static class GestorCapital
{
    public static IReadOnlyList<OrderRequest> Ajustar(IReadOnlyList<OrderRequest> requests, PortfolioState portfolio, ConfiguracionSizing? sizing, DataSlice dataSlice, decimal precioReferencia, decimal tasaMargen)
    {
        if (sizing is null)
            return requests;

        var cantidadCalculada = sizing.GestorActivo.CalcularCantidad(portfolio, dataSlice, precioReferencia, tasaMargen);

        // Lineas 41-71 actuales: SIN NINGUN CAMBIO. Clasificacion de intencion (D-092) y
        // normalizacion de Cross-Zero (D-095) permanecen identicas caracter por caracter.
        ...
    }
}
```

`BacktestRunner.cs:57` pasa a invocar con el `dataSlice` ya construido en la línea 46 (mismo valor,
ningún cálculo adicional):
```csharp
requests = GestorCapital.Ajustar(requests, portfolio, config.Sizing, dataSlice, config.Velas[n + 1].Close, instrumento.TasaMargen);
```

### 4.5 `IdentidadExperimentoCompleta.CalcularHashConfiguracionEconomica` — ajuste (precisión
derivada de D-109)

**Ubicación**: `exploration/laboratorio/protocolo/IdentidadExperimentoCompleta.cs:61` (modificado)

```csharp
var textoSizing = sizing switch
{
    null => "sin-sizing",
    { GestorActivo: IIdentidadGestorRiesgo identidad } => identidad.ObtenerIdentidadConfiguracion(),
    _ => throw new InvalidOperationException(
        $"GestorActivo de tipo '{sizing.GestorActivo.GetType().Name}' no implementa " +
        $"IIdentidadGestorRiesgo — configuracion no reproducible, HashConfiguracionEconomica " +
        $"no puede calcularse.")
};
```

**Por qué falla en vez de usar un valor por defecto**: si un gestor activo no declara su
identidad, el hash económico no puede garantizar que dos corridas con el mismo gestor produzcan
el mismo `HashConfiguracionEconomica` — inventar un texto (ej. `GetType().Name` solo) ocultaría
silenciosamente una configuración no reproducible, mismo principio D-055/D-062/D-095 de no
esconder un supuesto no satisfecho. Esta clase no conoce `GestorFixedFractional`,
`GestorFixedRisk` ni `GestorVolatilitySizing` por nombre — solo el contrato
`IIdentidadGestorRiesgo` (rechaza explícitamente el pattern-matching-por-tipo concreto).

**Consecuencia práctica**: los 3 gestores de D-110 deben implementar `IIdentidadGestorRiesgo`
desde el primer commit — no es opcional ni diferible, es requisito de compilación funcional del
hash económico para cualquier gestor que se active en una corrida real.

---

## 5. Métricas nuevas (D-111) — ubicación exacta

- **`MetricasFinancieras`** (`exploration/laboratorio/modelo_financiero/MetricasFinancieras.cs`,
  extender el record): `ProfitFactor: decimal?` (null si no hay pérdidas, evita división por
  cero — mismo criterio `decimal?` que `DrawdownMaximoPct`, D-078), `MargenMaximoUtilizado:
  decimal` (ya calculable de `PortfolioSnapshots.Max(s => s.Margin)` — **verificar si es
  literalmente el mismo cálculo que `ExposicionMaxima` ya tiene, en cuyo caso no se duplica campo,
  se documenta la equivalencia**), `CapitalLibreMinimo: decimal` (`min(Cash - Margin)` sobre
  `PortfolioSnapshots`).
- **`CalculadoraMetricasFinancieras.Calcular`**: única fuente de estos cálculos nuevos (D-072/
  D-077, sin excepción) — ningún `IGestorRiesgo` calcula sus propias métricas, confirmando la
  precisión del auditor: los gestores solo calculan cantidad, nunca observabilidad.
- **`AnalizadorOperacional`/`ReporteOperacional`**: `RachaPositivaMaxima: int` (nuevo, simétrico a
  `MayorRachaNegativa` ya existente), `DistribucionRachas` (a definir alcance mínimo en
  implementación — lista de longitudes de racha, no una estructura estadística completa, evitando
  sobre-construir sin necesidad probada, mismo criterio de mínimo necesario del resto del
  proyecto).
- **Duración de drawdown, riesgo de ruina, proximidad a incapacidad**: quedan **fuera de esta
  ronda de implementación** — no tienen fuente de dato tan directa como las anteriores (duración
  requiere identificar inicio/fin de cada drawdown sobre `EquityCurve`, no solo su magnitud; ruina
  requiere definir un umbral que no existe en ningún lado del proyecto hoy). Se documentan como
  pendientes explícitos, no implementados silenciosamente a medias — mismo criterio que
  `PROPUESTA_CASO5_V1.md` §3 ya usó para señalar huecos sin bloquear la apertura de fase.
  **`ValidadorCapacidad`/`CalculadoraReservaPreventiva`/`RegistroIncapacidad` ya existentes cubren
  parcialmente "proximidad a incapacidad"** — la implementación debe verificar si reutilizarlos
  alcanza antes de crear una métrica nueva (D-072: no duplicar fuente).

---

## 6. Pruebas obligatorias

Ubicación: `exploration/laboratorio/caso5/TestsGestoresRiesgo.cs` (mismo patrón satélite que
`caso3/Caso3.csproj`, `ProjectReference` a `Domain`/`Application` únicamente).

1. **Migración sin cambio de comportamiento**: los 5 baselines congelados, ejecutados con
   `GestorFixedFractional` con el mismo `PorcentajeRiesgo` histórico, producen
   `HashCompuesto`/`HashConfiguracionEconomica` idénticos a los valores ya registrados en cada
   `VERSION_EXPERIMENTAL_*.md`.
2. **D-092 intacta**: clasificación de intención (Apertura/Aumento/Reducción/Cierre/CrossZero)
   produce exactamente los mismos resultados con cualquier `IGestorRiesgo` activo — prueba
   parametrizada sobre los 3 gestores con la misma bolsa de `OrderRequest` de entrada, mismo
   resultado de clasificación en los 3 casos (solo cambia `cantidadCalculada`, nunca `intencion`).
3. **D-095 intacta**: Cross-Zero espurio bajo sizing activo se normaliza a `CierreTotal` con la
   magnitud de la posición proyectada, igual con los 3 gestores — mismo test que ya existe para
   Fixed Fractional, parametrizado.
4. **`Sizing=null` (caso básico)**: `requests` intacto, ningún `IGestorRiesgo` invocado
   (verificable con un gestor de prueba que lanza excepción si `CalcularCantidad` se llama — debe
   no dispararse).
5. **`GestorFixedRisk`**: cantidad resultante = `MontoRiesgoFijo / (precioReferencia *
   tasaMargen)` para un caso simple; caso donde excede `CapitalDisponible` registra incapacidad
   (no bloquea) — mismo patrón que test ya existente para Fixed Fractional con porcentaje alto.
6. **`GestorVolatilitySizing`**: warmup (`VelasHastaN.Count < N` → cantidad `0m`), ventana
   correcta (comparar contra cálculo directo no incremental sobre el mismo dataset pequeño, mismo
   patrón usado para verificar `EstrategiaZScoreReversion`).
7. **Comparación de control**: correr la misma estrategia (ej. EMA Cross) + mismo dataset +
   mismo timeframe + misma configuración económica con los 3 gestores, confirmar que solo
   `MetricasFinancieras`/`ReporteOperacional` difieren, no la secuencia de señales emitidas por la
   estrategia (`Observar` no ve el gestor — mismo aislamiento ya confirmado en
   `PROPUESTA_CASO5_V1.md` §2).
8. **Métricas nuevas**: `ProfitFactor`/`CapitalLibreMinimo`/`RachaPositivaMaxima` verificados
   contra cálculo manual sobre un `ResultadoBacktest` fijo y pequeño (mismo patrón que
   `TestsMetricasFinancieras.cs` ya usa).
9. **(P9, añadida por el auditor) Equivalencia con sizing desactivado**: `Sizing=null` produce
   exactamente la `Cantidad` original de cada `OrderRequest` (sin redondeo, sin normalización) y
   el mismo comportamiento de Cross-Zero genuino que las fixtures/baselines históricos ya
   verifican (D-061/D-069) — corrida completa contra los fixtures existentes de
   `GestorCapitalTests.cs` con `Sizing=null` explícito, no solo el caso trivial de bolsa vacía
   (distinto del punto 4: aquí se corre la suite completa de fixtures históricos, no un caso
   aislado). Motivo (auditor): la frontera sizing activo/inactivo ya demostró ser crítica en Caso
   4 (D-084/D-095) — no basta con probarla de forma indirecta a través de otra prueba, requiere su
   propio caso.
10. **Identidad del hash económico (precisión derivada de D-109)**: `ObtenerIdentidadConfiguracion()`
    de cada uno de los 3 gestores produce el texto esperado por convención (§4.5); dos instancias
    del mismo gestor con los mismos parámetros producen el mismo `HashConfiguracionEconomica`; un
    `IGestorRiesgo` de prueba que NO implementa `IIdentidadGestorRiesgo` provoca que
    `CalcularHashConfiguracionEconomica` lance excepción explícita (no un hash silencioso).
    **Identidad estable entre instancias (criterio añadido por el auditor)**: `new
    GestorFixedFractional(0.01m).ObtenerIdentidadConfiguracion()` y `new
    GestorFixedFractional(0.01m).ObtenerIdentidadConfiguracion()` — dos instancias distintas, misma
    configuración — deben producir el mismo texto exacto (`"fixed-fractional:v1:riesgo=0.01"`),
    verificado con `Assert.Equal`, no solo `Assert.NotNull`. Repetir para `GestorFixedRisk` y
    `GestorVolatilitySizing`. Protege contra una identidad basada accidentalmente en referencia de
    objeto, estado interno mutable, o orden de creación — no requiere una nueva decisión, es
    criterio de prueba de D-109 (`GetHashCode()`/`ToString()` por defecto de una clase, si se
    usaran por error en vez de una cadena explícita, fallarían exactamente este caso).

Suite de producción (`dotnet test -c Release`) debe permanecer en 126/126 sin cambios — ningún
archivo de `tests/` se modifica en esta fase salvo que la migración de `ConfiguracionSizing` (§3)
lo requiera exclusivamente por firma, nunca por comportamiento.

---

## 7. Fuera de alcance de esta especificación

No se implementó código. `Kelly`/`Masaniello` no se especifican aquí (D-110, diferidos). Duración
de drawdown/riesgo de ruina no se especifican en detalle (§5, pendientes explícitos). No se decide
todavía si `GestoresRiesgo/` es carpeta nueva o archivos planos en `Domain/Portfolio/` — decisión
de implementación menor, no arquitectónica, a resolver al escribir el código.

---

## Historial de revisión

- **v1**: especificación inicial, aprobada por el auditor con condición de resolver §1 (firma de
  `GestorCapital.Ajustar`) — resuelto en la misma versión.
- **v1 (esta revisión)**: incorpora la precisión derivada de D-109 (`IIdentidadGestorRiesgo`,
  §2.1/§4.5), la migración exhaustiva de 3 consumidores reales de `ConfiguracionSizing` detectados
  por búsqueda sin restricción de carpeta (§3) — no cubiertos por la búsqueda original de esta
  especificación —, y P9/P10 en la suite de pruebas (§6), todo por instrucción explícita del
  auditor. Ningún punto de D-108/D-109/D-110/D-111 fue reabierto.

---

## Próximo paso

Autorización explícita del auditor para implementar: `IGestorRiesgo`, `IIdentidadGestorRiesgo`,
`GestorFixedFractional`, `GestorFixedRisk`, `GestorVolatilitySizing`, migración de
`GestorCapital`/`ConfiguracionSizing`/`BacktestRunner`/`IdentidadExperimentoCompleta`, migración de
los 2 archivos de test que construyen `ConfiguracionSizing` directamente (§3), métricas nuevas
(§5, alcance reducido: `ProfitFactor`, `MargenMaximoUtilizado`/equivalencia con
`ExposicionMaxima`, `CapitalLibreMinimo`, `RachaPositivaMaxima`), y suite de pruebas (§6,
10 pruebas) — con el cambio de firma de `GestorCapital.Ajustar` (§1) y el contrato separado de
identidad (§2.1/§4.5) ya incorporados a esta especificación.
