# Especificación de Implementación — Comparador de Gestores de Riesgo (Caso 5B)

Estado: **especificación previa a implementación**. Traduce D-112 a D-115
(`DECISIONES_CASO5B_V1.md`) a diseño de código concreto — nombres, firmas, ubicación de archivos.
**Ningún código se modifica en este documento.**

---

## 1. Ubicación exacta

**Archivo**: `exploration/laboratorio/caso5/ComparadorGestores.cs` — mismo módulo satélite que ya
aloja `IGestorRiesgo`/gestores/pruebas de Caso 5A (`caso5/Caso5.csproj`), no una carpeta nueva.
Justificación: Caso 5B es continuación directa de Caso 5A sobre la misma infraestructura, a
diferencia de Caso 3B (que sí ameritó módulo propio por ser una familia de estrategia distinta) —
mismo criterio de "sin abstracción/separación no solicitada" ya aplicado en el proyecto.

**Namespace**: `TD_Project.Caso5` — mismo namespace que `TestsGestoresRiesgo.cs` en este módulo.

**Relación con `ComparadorMultiTimeframe`**: **no hay dependencia de código entre ambos** —
`ComparadorGestores` no referencia `ComparadorMultiTimeframe`/`PerfilMultiTimeframe.cs`, solo
reutiliza su *patrón de diseño* (D-112): orden de entrada = orden de presentación, sin
recalcular métricas, separación cálculo/agregación. Los tipos son estructuralmente análogos pero
independientes — `ComparadorMultiTimeframe` compara timeframes vía `ReporteOperacional`,
`ComparadorGestores` compara gestores vía `MetricasFinancieras` (D-114), fuentes distintas por
diseño.

**`Caso5.csproj`**: no requiere ningún `<Compile Include>` nuevo — todos los tipos que
`ComparadorGestores` consume (`EntradaProtocolo`, `EjecutorProtocolo`, `ConfiguracionSizing`,
`IGestorRiesgo`, `IIdentidadGestorRiesgo`, `MetricasFinancieras`, `ResultadoCorridaTimeframe`) ya
están enlazados en el proyecto desde Caso 5A.

---

## 2. Contratos

```csharp
namespace TD_Project.Caso5;

// spec: Caso 5B D-114 (DECISIONES_CASO5B_V1.md) — una fila por gestor comparado. Metricas es la
// unica fuente de datos (MetricasFinancieras, D-072/D-077) — nunca ReporteOperacional (excluido
// explicitamente por su acoplamiento a martingala, D-055). Estado permite que una corrida
// individual falle sin invalidar la comparacion completa (mismo principio que EjecutorProtocolo
// ya aplica a timeframes).
public sealed record FilaComparacionGestor(
    string IdentidadGestor,
    EstadoCorridaTimeframe Estado,
    MetricasFinancieras? Metricas);

// spec: Caso 5B D-114 — Filas conserva el orden de la lista de gestores recibida (D-112: orden de
// entrada = orden de presentacion, nunca reordenado por valor de metrica).
public sealed record ResultadoComparativoGestores(
    string Estrategia,
    string Timeframe,
    string NombreDataset,
    IReadOnlyList<FilaComparacionGestor> Filas);

public static class ComparadorGestores
{
    // spec: Caso 5B D-113 — entradaBase NO debe traer Sizing configurado (garantiza que el unico
    // eje que varia entre corridas es el gestor, ver S3). gestores no puede ser vacio ni contener
    // null (ver S4, P1).
    public static ResultadoComparativoGestores Comparar(EntradaProtocolo entradaBase, IReadOnlyList<IGestorRiesgo> gestores);
}
```

**Por qué `Timeframe`/`NombreDataset` son `string` simples en el resultado, no una lista**: D-113
fija la unidad de comparación a un único timeframe/dataset — `EntradaProtocolo.Timeframes` es una
lista (soporta multi-timeframe en el protocolo general), pero `ComparadorGestores` opera sobre un
único timeframe declarado. Ver §3 para cómo se resuelve esa discrepancia de forma explícita, no
silenciosa.

---

## 3. Flujo interno

```
EntradaProtocolo base (Sizing = null obligatorio, ver P1)
        |
        v
Validar: gestores.Count > 0, ningun elemento null, entradaBase.Timeframes.Count == 1 (ver nota abajo)
        |
        v
Para cada gestor en gestores (en orden):
        |
        +-- entradaVariante = entradaBase with { Sizing = new ConfiguracionSizing(gestor) }
        |        (unico campo que cambia: Sizing. Estrategia/Dataset/Timeframes/Instrumento/
        |         Costes/CapitalInicial permanecen identicos por construccion — record `with`
        |         copia todo lo demas sin posibilidad de divergencia manual)
        |
        +-- resultado = EjecutorProtocolo.Ejecutar(entradaVariante)
        |
        +-- corridaDelTimeframe = resultado.Corridas.Single(c => c.Timeframe == entradaBase.Timeframes[0])
        |
        +-- identidadGestor = ((IIdentidadGestorRiesgo)gestor).ObtenerIdentidadConfiguracion()
        |        (si gestor no implementa IIdentidadGestorRiesgo: excepcion explicita, mismo
        |         principio que IdentidadExperimentoCompleta.CalcularHashConfiguracionEconomica
        |         ya aplica — nunca una fila con identidad inventada)
        |
        +-- fila = new FilaComparacionGestor(identidadGestor, corridaDelTimeframe.Estado, corridaDelTimeframe.MetricasFinancieras)
        |
        v
ResultadoComparativoGestores { Estrategia, Timeframe, NombreDataset, Filas }
```

**Nota sobre `Timeframes`**: `EntradaProtocolo.Timeframes` es `IReadOnlyList<string>` (soporta
multi-timeframe en el protocolo general, D-113 de Caso 5B no lo prohíbe a nivel de tipo). Para
mantener la unidad de comparación de D-113 exacta (un timeframe fijo), `ComparadorGestores.Comparar`
exige `entradaBase.Timeframes.Count == 1` — **falla explícitamente** (`ArgumentException`) si
`entradaBase` declara más de un timeframe, en vez de comparar solo el primero silenciosamente o
iterar todos sin que D-113 lo haya autorizado. Extender a comparación multi-timeframe queda como
alcance futuro explícito, no una interpretación implícita de esta especificación.

**Por qué `Sizing = null` es obligatorio en `entradaBase`** (verificación P1, §4): si `entradaBase`
ya trae un `Sizing` configurado, `with { Sizing = new ConfiguracionSizing(gestor) }` lo sobrescribe
igual — pero permitir un `Sizing` previo en la entrada base sugeriría (engañosamente) que ese valor
importa, cuando en realidad siempre se descarta. Exigir `null` hace explícito que la única fuente
del gestor activo es la lista `gestores`, sin una configuración "por defecto" ambigua.

---

## 4. Protección experimental — pruebas obligatorias

Ubicación: `caso5/TestsComparadorGestores.cs`, mismo patrón runner manual (`Caso(nombre,
verificacion)`) que `TestsGestoresRiesgo.cs`, agregado a `caso5/Program.cs`.

1. **P1 — Validación de entrada**: `gestores` vacío → excepción explícita. `gestores` con un
   elemento `null` → excepción explícita. `entradaBase.Sizing` no nulo → excepción explícita.
   `entradaBase.Timeframes.Count != 1` → excepción explícita. Ninguno de estos casos debe producir
   un `ResultadoComparativoGestores` parcial o silenciosamente incorrecto.
2. **P2 — Mismos parámetros salvo gestor**: para cada fila del resultado, reconstruir
   manualmente la `EntradaProtocolo` que debería haberse ejecutado (`entradaBase with { Sizing =
   ... }`) y confirmar que produce el mismo `MetricasFinancieras` que `EjecutorProtocolo.Ejecutar`
   invocado directamente con esa misma entrada — confirma que `ComparadorGestores` no introduce
   ninguna divergencia respecto a ejecutar cada gestor por separado.
3. **P3 — Mismo hash de configuración base, distinta identidad de gestor**: las N corridas
   internas deben producir `HashCompuesto` idéntico entre sí (mismo dataset/estrategia/parámetros/
   versión — D-082 no depende del gestor) y `HashConfiguracionEconomica` **distinto** entre sí
   siempre que los gestores tengan identidades distintas (D-109) — confirma que el único cambio
   real entre corridas es el gestor, verificable a través del propio mecanismo de identidad ya
   congelado, no solo por inspección de código.
4. **P4 — Orden de entrada preservado**: `Filas` en el mismo orden que la lista `gestores` recibida,
   para 3 permutaciones distintas del mismo conjunto de gestores — confirma que no hay reordenamiento
   por valor de ninguna métrica (D-114/D-112).
5. **P5 — Ausencia de ranking**: verificación estructural, no de comportamiento — `
   ResultadoComparativoGestores`/`FilaComparacionGestor` no exponen ningún campo de posición,
   puntuación, ni booleano de tipo "EsMejor"/"Recomendado" (inspección de la definición del tipo,
   falla si algún campo de ese tipo se agrega en el futuro sin pasar por una decisión D-N nueva).
6. **P6 — Ausencia de recomendación**: `ComparadorGestores` no expone ningún método que reciba
   `ResultadoComparativoGestores` y devuelva un único gestor o una preferencia — solo `Comparar`
   existe en la clase (inspección de la superficie pública del tipo).
7. **P7 — Corrida individual fallida no invalida la comparación**: con un gestor que produce
   `Failed`/`Incomplete` en un dataset diseñado para eso (ej. dataset ausente en disco para ese
   timeframe) mezclado con gestores que sí producen `Success`, `ResultadoComparativoGestores`
   contiene las 3 filas — la fallida con `Metricas = null` y su `Estado` correcto, las demás
   completas.
8. **P8 — Extracción de identidad falla explícitamente sin `IIdentidadGestorRiesgo`**: un
   `IGestorRiesgo` de prueba que no implementa `IIdentidadGestorRiesgo` provoca que `Comparar`
   lance excepción, no que produzca una fila con identidad inventada — mismo criterio ya verificado
   en Caso 5A P10, aplicado ahora en este componente.

Suite de producción (`dotnet test -c Release`) debe permanecer en 126/126 — ningún archivo de
`src/`/`tests/` se modifica en esta fase.

---

## 5. Salida — separación objeto comparativo / render de tabla

**`ResultadoComparativoGestores`/`FilaComparacionGestor`** (§2): el objeto comparativo, única
fuente de verdad. No tiene ningún método de formateo — es un record de datos puros.

**Render de tabla**: función separada, `RenderizadorComparacionGestores.Generar(ResultadoComparativoGestores)
: string` — mismo patrón de separación cálculo/reporte que `CalculadoraMetricasFinancieras`/
`ReporteFinancieroGenerador` ya establece en el proyecto (D-072/D-077 aplicado a esta capa de
presentación). Vive en el mismo archivo `ComparadorGestores.cs` o en uno separado
(`RenderizadorComparacionGestores.cs`) — decisión de organización menor, no arquitectónica, a
resolver al escribir el código.

Formato mínimo de la tabla (texto plano, mismo estilo que el resto de reportes del laboratorio):
columnas = gestores (por `IdentidadGestor`), filas = métricas (`PnLTotal`, `DrawdownMaximoPct`,
`ProfitFactor`, `ExposicionMaxima`, `CashFinal`, `EquityFinal`) — **sin ninguna columna ni fila
adicional que sugiera "mejor"/"peor"/posición relativa** (D-115). Una corrida con `Estado != Success`
se presenta como tal explícitamente (ej. `"(Failed)"` en vez de valores numéricos), nunca omitida
silenciosamente de la tabla.

---

## 6. Fuera de alcance de esta especificación

No se implementó código. **No incluye**: sistema recomendador, optimización de parámetros de
ningún gestor, selección automática de gestor por ninguna estrategia o corrida, Kelly fraccionado,
Masaniello, calibración de ningún valor observando resultados (D-030). No se extiende a
comparación multi-timeframe ni multi-estrategia (§3, nota sobre `Timeframes.Count == 1`) — alcance
futuro explícito, no resuelto aquí. No se decide si `RenderizadorComparacionGestores` vive en
archivo propio o junto a `ComparadorGestores` (organización menor, se resuelve al implementar).

---

## Próximo paso

Autorización explícita del auditor para implementar: `FilaComparacionGestor`,
`ResultadoComparativoGestores`, `ComparadorGestores.Comparar`, `RenderizadorComparacionGestores.
Generar`, y la suite de pruebas (§4, 8 pruebas) — todo en `exploration/laboratorio/caso5/`, sin
tocar ningún archivo de `src/`/`tests/`.
