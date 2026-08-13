namespace TD_Project.DatosReales;

// spec: Caso 5C D-122 (DECISIONES_RANGO_ALTERNATIVO_CASO5C_V1.md, Opcion B) — explora continuidad
// de un rango candidato en bloques mensuales, ANTES de comprometerse a una descarga completa de 1
// anio. No escribe ningun archivo en datasets/reales/ ni en datos_reales/raw/ (separacion
// exploracion/congelacion exigida por el auditor: ningun metodo recibe una ruta de archivo).
// Reutiliza BinanceClient/ValidadorIntegridadDatos sin modificarlos.
//
// Alcance: opera exclusivamente sobre interval="1m" — es el unico intervalo que este proyecto
// descarga de Binance (datos_reales/Program.cs:35); los demas timeframes (5m/15m/1h/1D/...) se
// derivan localmente por agregacion desde el 1m ya congelado (agregador/AgregadorMultiTimeframe),
// sin volver a consultar la API. La continuidad de 1m determina por construccion la continuidad de
// todos los derivados (ver ESPECIFICACION_IMPLEMENTACION_EXPLORACION_DISPONIBILIDAD_CASO5C_V1.md
// §2). Si en el futuro un timeframe distinto de 1m se obtuviera de un origen externo independiente,
// esta hipotesis dejaria de sostenerse y la estrategia de exploracion requeriria revision.
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
    // disco en rutas de datos. Ningun parametro de ruta de archivo existe en esta firma: no puede
    // escribir un dataset experimental por construccion, no por disciplina de uso.
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
