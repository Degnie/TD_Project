# Especificación de Implementación — Exploración de Disponibilidad (Caso 5C, D-122 Opción B)

Estado: **especificación previa a implementación — cubre exclusivamente la fase exploratoria**.
Traduce D-122 (`DECISIONES_RANGO_ALTERNATIVO_CASO5C_V1.md`, Opción B) a diseño de código concreto.
**Ningún código se modifica en este documento. Ningún dataset se congela. Ninguna comparación se
ejecuta.** No decide todavía el rango definitivo — eso depende del resultado real de correr esta
exploración.

---

## 1. Ubicación y alcance

**Nuevo archivo**: `exploration/laboratorio/datos_reales/ExploradorDisponibilidad.cs` — mismo
proyecto (`DatosReales.csproj`), mismo namespace `TD_Project.DatosReales`. No un módulo separado:
la exploración reutiliza `BinanceClient`/`ValidadorIntegridadDatos` ya existentes en este proyecto,
sin ninguna dependencia nueva.

**`BinanceClient.cs`/`ValidadorIntegridadDatos.cs`/`DescargadorVelas.cs`: sin modificación.** La
exploración es un consumidor nuevo de los 2 primeros — no reescribe la paginación de
`DescargadorVelas.DescargarAsync`, la reimplementa en memoria (§2) porque esa función escribe a CSV
por diseño (`DescargadorVelas.cs:37-57`), y la exploración explícitamente no debe producir ningún
archivo en rutas de datos (§5).

---

## 2. Mecanismo — cómo se divide el año en bloques cortos

**Bloque = 1 mes calendario**, 12 bloques por año candidato — mismo tamaño que ya propuso D-122.

```csharp
namespace TD_Project.DatosReales;

// spec: Caso 5C D-122 (DECISIONES_RANGO_ALTERNATIVO_CASO5C_V1.md, Opcion B) — explora continuidad
// de un rango candidato en bloques mensuales, ANTES de comprometerse a una descarga completa de 1
// anio. No escribe ningun archivo en datasets/reales/ ni en datos_reales/raw/ (separacion
// exploracion/congelacion exigida por el auditor). Reutiliza BinanceClient/ValidadorIntegridadDatos
// sin modificarlos.
public static class ExploradorDisponibilidad
{
    public sealed record ResultadoBloque(DateTimeOffset InicioUtc, DateTimeOffset FinUtc, bool Continuo, int Huecos, int MinutosFaltantes);
    public sealed record ResultadoExploracion(string Symbol, string Interval, IReadOnlyList<ResultadoBloque> Bloques)
    {
        public bool TodosContinuos => Bloques.All(b => b.Continuo);
    }

    public static async Task<ResultadoExploracion> ExplorarAsync(
        BinanceClient cliente, string symbol, string interval, DateTimeOffset inicioAnio, DateTimeOffset finAnio)
    {
        var bloques = new List<ResultadoBloque>();
        var cursorMes = inicioAnio;

        while (cursorMes < finAnio)
        {
            var finMes = cursorMes.AddMonths(1) > finAnio ? finAnio : cursorMes.AddMonths(1);
            var velasDelMes = await DescargarEnMemoriaAsync(cliente, symbol, interval, cursorMes.ToUnixTimeMilliseconds(), finMes.ToUnixTimeMilliseconds());
            var veredicto = ValidadorIntegridadDatos.Verificar(velasDelMes);

            var minutosFaltantes = veredicto.Huecos.Sum(h => h.MinutosFaltantes);
            bloques.Add(new ResultadoBloque(cursorMes, finMes, veredicto.Huecos.Count == 0 && veredicto.Errores.Count == 0, veredicto.Huecos.Count, minutosFaltantes));

            cursorMes = finMes;
        }

        return new ResultadoExploracion(symbol, interval, bloques);
    }

    // Mismo patron de paginacion que DescargadorVelas.DescargarAsync (DescargadorVelas.cs:20-61),
    // pero acumula en memoria en vez de escribir a un StreamWriter — la exploracion nunca toca
    // disco en rutas de datos (spec §5).
    private static async Task<IReadOnlyList<VelaCruda>> DescargarEnMemoriaAsync(
        BinanceClient cliente, string symbol, string interval, long inicioUtcMs, long finUtcMs)
    {
        var velas = new List<VelaCruda>();
        var cursor = inicioUtcMs;

        while (cursor < finUtcMs)
        {
            var lote = await cliente.ObtenerKlinesAsync(symbol, interval, cursor, limit: 1000);
            if (lote.Count == 0) break;

            foreach (var vela in lote)
            {
                if (vela.TimestampUtcMs >= finUtcMs) break;
                velas.Add(vela);
            }

            cursor = lote[^1].TimestampUtcMs + ValidadorIntegridadDatos.UnMinutoMs;
            await Task.Delay(150); // misma pausa entre requests que DescargadorVelas.cs:14
        }

        return velas;
    }
}
```

**Por qué 1 mes y no otro tamaño**: ~43.200 minutos por mes (~44 requests paginados de 1000 velas)
frente a ~525.600 minutos/~526 requests de un año completo — reduce el costo de una exploración
completa de 12 meses a aproximadamente el mismo número de requests que **una sola** descarga
mensual habría costado dentro del año completo ya rechazado, sin necesidad de comprometerse a los
otros 11 meses si el primero ya revela un hueco.

**Corte temprano**: la exploración puede detenerse en el primer bloque no continuo si el objetivo es
solo descartar el candidato lo antes posible — la especificación deja `ExplorarAsync` recorriendo
los 12 meses completos (para que el reporte final muestre el panorama completo del año, útil si
ninguno de los meses tiene problema), pero el programa que la invoque (§4) puede optar por cortar
en el primer hueco si se prioriza velocidad sobre panorama completo — decisión de uso, no de
contrato del método.

**Alcance de la exploración — por qué solo `1m`, precisión incorporada tras revisión del auditor**:
verificado contra código (`datos_reales/Program.cs:35`), el pipeline descarga **únicamente `1m`**
desde Binance — `interval` es una constante fija, no varía por timeframe. Los demás timeframes
(`5m`/`15m`/`1h`/`1D`/etc.) no se descargan por separado desde la API: se derivan localmente por
agregación desde el `1m` ya congelado (`agregador/AgregadorMultiTimeframe.Agregar`), sin volver a
consultar Binance.

```
Binance
   |
   v
1m  <- unico origen externo, validado por ValidadorIntegridadDatos
   |
   v
Agregacion local (agregador/)
   |
   v
5m / 15m / 1h / 1D / ...  <- derivados, no datasets independientes
```

**Consecuencia directa**: la disponibilidad de los timeframes derivados queda condicionada por
completo a la continuidad del dataset fuente `1m` — agregar velas continuas no puede introducir un
hueco nuevo que no existiera ya en el origen. Por tanto, `ExplorarAsync`/`ExplorarDisponibilidadAsync`
verifican exclusivamente `interval="1m"` — explorar cada timeframe derivado por separado
introduciría una falsa separación de fuentes que no existe en el pipeline real, y sería una
duplicación de trabajo sin valor: no hay una segunda descarga que pudiera fallar de forma
independiente para `1h` o `1D`.

**No se agrega ninguna verificación post-agregación en esta especificación** — `agregador/
Program.cs` ya tiene su propia verificación manual (comparación de las primeras 60 velas 1m contra
la primera vela 1h agregada directamente, `Program.cs:98-126`, bloqueante si no coincide) y
`FixturesAgregador.cs` ya cubre casos de continuidad/huecos heredados a nivel de agregación (ver
`CasoHuecoHeredado` en los fixtures del agregador). Añadir una capa de verificación adicional aquí
sería redundante con garantías ya existentes, y ampliaría el alcance de D-122 más allá de lo que esa
decisión resolvió (exploración de disponibilidad de adquisición, no revalidación del agregador).

**Nota para auditoría futura, no aplicable hoy**: si en algún momento el proyecto incorpora un
origen de datos donde un timeframe distinto de `1m` se obtenga directamente de un proveedor externo
(en vez de derivarse por agregación local), esta hipótesis — "explorar solo `1m` basta" — dejaría de
sostenerse, y la estrategia de exploración debería revisarse para cubrir ese origen adicional de
forma independiente.

---

## 3. Qué endpoint/dato se consulta

**Mismo endpoint ya usado por el pipeline existente**: `GET /api/v3/klines` de Binance, vía
`BinanceClient.ObtenerKlinesAsync` sin modificación — mismos parámetros (`symbol`, `interval`,
`startTime`, `limit=1000`), misma estructura de respuesta ya parseada a `VelaCruda`. La exploración
no introduce ningún endpoint nuevo ni ninguna llamada distinta a la API.

---

## 4. Qué condición determina "candidato viable"

**`ResultadoExploracion.TodosContinuos == true`** — los 12 bloques mensuales del año candidato
deben tener `Continuo: true` (sin huecos, sin errores estructurales) para que el candidato pase a
intentar la descarga completa (§2 Opción B del diagrama de `DECISIONES_RANGO_ALTERNATIVO_
CASO5C_V1.md`).

**Un solo bloque con `Continuo: false` descarta el candidato** — mismo criterio estricto que
`ValidadorIntegridadDatos` ya aplica al año completo (sin excepciones, sin "casi apto").

**Aclaración explícita, para evitar ambigüedad**: que la exploración mensual no detecte huecos no
garantiza al 100% que la descarga completa tampoco los tenga (la exploración corre en requests
separados, mes a mes; la descarga completa corre en una sola pasada continua) — por eso D-122 ya
estableció que `ValidadorIntegridadDatos` sobre la descarga completa sigue siendo obligatorio y
autoritativo. La exploración reduce drásticamente la probabilidad de sorpresa, no la elimina.

---

## 5. Qué salida produce y cómo se evita escribir en `datasets/reales/`

**Salida**: únicamente a consola (`Console.WriteLine`), mismo patrón que `datos_reales/Program.cs`
ya usa para sus reportes — un resumen por mes (`Continuo`/`Huecos`/`MinutosFaltantes`) y un
veredicto final (`TodosContinuos`).

**Ningún archivo se escribe durante la exploración**: `ExploradorDisponibilidad.ExplorarAsync`/
`DescargarEnMemoriaAsync` no reciben ninguna ruta de archivo como parámetro — estructuralmente no
pueden escribir a disco (a diferencia de `DescargadorVelas.DescargarAsync`, que sí recibe
`rutaCsv`). Esto satisface la restricción del auditor por construcción, no por disciplina de uso:
no hay ningún parámetro de ruta que alguien pudiera pasar por error apuntando a
`datasets/reales/`.

**Documentación de rangos rechazados**: no se persiste en ningún archivo nuevo — se documenta
manualmente en el resultado de la exploración (consola) y, si el auditor lo requiere tras ver el
resultado real, en un documento de hallazgo equivalente a
`HALLAZGO_RECHAZO_DATASET_2023_CASO5C_V1.md` (mismo patrón ya usado, no una funcionalidad de código
nueva).

---

## 6. Mecanismo de invocación

**Nuevo ejecutable pequeño, no una extensión de `datos_reales/Program.cs`**:
`datos_reales/ProgramExploracion.cs` — pero esto introduciría un segundo `Program.cs` en el mismo
`.csproj` con `OutputType=Exe`, el mismo conflicto ya visto al implementar `campana_corpus/`
(§1 de `ESPECIFICACION_IMPLEMENTACION_CAMPANA_CORPUS_CASO5C_V1.md`). **Se opta por integrar la
exploración como una tercera etapa opt-in dentro del `datos_reales/Program.cs` ya existente**, tras
las etapas 1 (fixtures) y 2 (descarga real), controlada por una variable de entorno separada:

```csharp
// Etapa 3 (exploracion, red real, opt-in separado de DESCARGAR_BINANCE): antes de una descarga
// completa de anio, permite verificar continuidad por mes de un candidato.
var explorarAnio = Environment.GetEnvironmentVariable("EXPLORAR_DISPONIBILIDAD_ANIO");
if (explorarAnio is not null && int.TryParse(explorarAnio, out var anio))
{
    var inicioAnio = new DateTimeOffset(anio, 1, 1, 0, 0, 0, TimeSpan.Zero);
    var finAnioExploracion = inicioAnio.AddYears(1);
    var resultado = await ExploradorDisponibilidad.ExplorarAsync(new BinanceClient(), symbol, interval, inicioAnio, finAnioExploracion);

    Console.WriteLine($"\n=== Exploracion de disponibilidad: {symbol} {interval} {anio} ===");
    foreach (var b in resultado.Bloques)
        Console.WriteLine($"  {b.InicioUtc:yyyy-MM} .. {b.FinUtc:yyyy-MM}: {(b.Continuo ? "OK" : $"HUECO ({b.Huecos} huecos, {b.MinutosFaltantes} min)")}");
    Console.WriteLine($"\nCandidato {anio}: {(resultado.TodosContinuos ? "VIABLE — continuidad OK en los 12 meses" : "DESCARTADO — al menos 1 mes con discontinuidad")}");
    return;
}
```

**Por qué una variable separada (`EXPLORAR_DISPONIBILIDAD_ANIO`) y no reutilizar
`DESCARGAR_BINANCE`**: mantiene la exploración claramente distinguible de una descarga real en la
invocación misma — nunca se podría disparar por accidente una exploración creyendo que se está
haciendo una descarga completa, o viceversa. `return` inmediato tras la exploración evita que el
programa continúe hacia la etapa de descarga real en la misma invocación.

**Ejecución esperada**: `EXPLORAR_DISPONIBILIDAD_ANIO=2022 dotnet run -c Release` desde
`datos_reales/` — sin necesidad de establecer `DESCARGAR_BINANCE`.

---

## 7. Qué no debe incluir esta implementación

- No modificar `BinanceClient.cs`, `ValidadorIntegridadDatos.cs`, `DescargadorVelas.cs`.
- No escribir ningún archivo en `datasets/reales/`, `datos_reales/raw/`, ni `datos_reales/
  metadata/` durante la exploración.
- No ejecutar ninguna campaña ni comparación (`ComparadorGestores`/`PersistidorComparaciones`) —
  la exploración es exclusivamente sobre disponibilidad de datos crudos.
- No congelar ningún dataset — un resultado `TodosContinuos: true` habilita intentar la descarga
  completa (§2 Opción B), no reemplaza esa descarga ni su validación.
- No decidir en este documento qué año(s) se exploran primero — eso ocurre al ejecutar, no antes.
- No retomar todavía los cambios pendientes en `agregador/Program.cs`/`datos_reales/Program.cs`
  del rango 2023 (`HALLAZGO_RECHAZO_DATASET_2023_CASO5C_V1.md`) más allá de agregar esta etapa 3 —
  el cambio de rango en la etapa 2 (descarga completa) se ajusta solo cuando haya un candidato
  viable confirmado por la exploración.

---

## 8. Pruebas

**Sin pruebas de red obligatorias en esta especificación** — mismo criterio que
`FixturesValidador.EjecutarTodos()` ya usa para `ValidadorIntegridadDatos` (fixtures sintéticos,
sin Binance real, como puerta de entrada antes de cualquier llamada real). Se añade un fixture
equivalente para `ExploradorDisponibilidad`, sin red:

1. **P1 — Bloque continuo detectado correctamente**: velas sintéticas de 1 mes sin huecos →
   `ResultadoBloque.Continuo == true`.
2. **P2 — Bloque con hueco detectado correctamente**: velas sintéticas de 1 mes con 1 hueco de 2
   minutos → `ResultadoBloque.Continuo == false`, `Huecos == 1`.
3. **P3 — `TodosContinuos` agrega correctamente**: `ResultadoExploracion` con 12 bloques, 11
   continuos y 1 no → `TodosContinuos == false`.
4. **P4 — Ningún método de `ExploradorDisponibilidad` recibe una ruta de archivo como parámetro**
   (verificación por reflexión sobre la firma de `ExplorarAsync`/`DescargarEnMemoriaAsync`, mismo
   patrón que P6/P7 ya usados en `TestsComparadorGestores.cs`/`TestsPersistidorComparaciones.cs`) —
   confirma por construcción que la exploración no puede escribir a disco.

Ubicación: `datos_reales/FixturesExploradorDisponibilidad.cs`, mismo patrón runner manual que
`FixturesValidador.cs`, invocado como Etapa 1 (antes de cualquier red) en `Program.cs`.

Suite de producción (`dotnet test -c Release`) debe permanecer en 126/126 — ningún archivo de
`src/`/`tests/` se toca en esta especificación. `caso5/Caso5.csproj` no compila `datos_reales/`, sin
impacto.

---

## 9. Fuera de alcance de esta especificación

No se implementó código. No se ejecutó ninguna exploración real. No se decide el rango definitivo.
No se retoma la descarga completa del año candidato — eso depende del resultado real de correr
`EXPLORAR_DISPONIBILIDAD_ANIO`. No se especifica la campaña sobre el dataset temporal ampliado
(`Sub-campaña D`, ya descrita en `ESPECIFICACION_IMPLEMENTACION_DIVERSIDAD_TEMPORAL_CASO5C_V1.md`
§5) — sigue condicionada a tener primero un dataset congelado.

---

## Próximo paso

Autorización explícita del auditor para implementar: `ExploradorDisponibilidad.cs`,
`FixturesExploradorDisponibilidad.cs` (4 pruebas, §8), y la Etapa 3 opt-in en
`datos_reales/Program.cs`. Tras esto, ejecutar `EXPLORAR_DISPONIBILIDAD_ANIO={año}` contra 1 o más
candidatos reales — el primer resultado `TodosContinuos: true` determina el rango que retoma
`ESPECIFICACION_IMPLEMENTACION_DIVERSIDAD_TEMPORAL_CASO5C_V1.md` (ajustando `finUtc` en la Etapa 2
de descarga completa al año confirmado viable).
