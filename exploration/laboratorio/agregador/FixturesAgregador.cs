using TD_Project.DatosReales;

namespace TD_Project.Agregador;

// Fixtures de DISENO_FASE2B.md Punto 6, corridos ANTES de agregar el dataset real de 527040
// velas. Cada caso demuestra una propiedad especifica del agregador, no solo que "corre sin
// error".
public static class FixturesAgregador
{
    public sealed record Resultado(string Nombre, bool Paso, string Detalle);

    private const long UnMinutoMs = TimeframeExtensiones.UnMinutoMs;

    public static IReadOnlyList<Resultado> EjecutarTodos() => new[]
    {
        CasoSimple(),
        CasoBordeAlineado(),
        CasoBordeNoAlineado(),
        CasoAsociatividad(),
        CasoConservacionVolumen(),
        CasoHuecoHeredado(),
    };

    // 5 velas 1m con valores conocidos -> 1 vela 5m calculada a mano.
    private static Resultado CasoSimple()
    {
        var inicio = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var velas = new List<VelaCruda>
        {
            new(inicio + 0 * UnMinutoMs, 100m, 101m, 99m, 100.5m, 10m),
            new(inicio + 1 * UnMinutoMs, 100.5m, 102m, 100m, 101m, 12m),
            new(inicio + 2 * UnMinutoMs, 101m, 103m, 100.8m, 102m, 8m),
            new(inicio + 3 * UnMinutoMs, 102m, 102.5m, 101m, 101.5m, 5m),
            new(inicio + 4 * UnMinutoMs, 101.5m, 104m, 101m, 103m, 9m),
        };

        var resultado = AgregadorMultiTimeframe.Agregar(velas, Timeframe.M5);
        var esperado = (Open: 100m, High: 104m, Low: 99m, Close: 103m, Volume: 44m);

        var v = resultado.Count == 1 ? resultado[0] : null;
        var paso = v is not null && v.Open == esperado.Open && v.High == esperado.High
            && v.Low == esperado.Low && v.Close == esperado.Close && v.Volume == esperado.Volume
            && !v.EsParcial;

        return new Resultado("CasoSimple", paso,
            v is null ? "No se genero exactamente 1 vela" :
            $"Open={v.Open} High={v.High} Low={v.Low} Close={v.Close} Volume={v.Volume} EsParcial={v.EsParcial} (esperado: O=100 H=104 L=99 C=103 V=44, completa)");
    }

    // Rango que empieza exactamente en un borde de calendario (00:00:00Z) -> cero velas parciales.
    private static Resultado CasoBordeAlineado()
    {
        var inicio = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var velas = Enumerable.Range(0, 10)
            .Select(i => new VelaCruda(inicio + i * UnMinutoMs, 100m, 101m, 99m, 100m, 1m))
            .ToList();

        var resultado = AgregadorMultiTimeframe.Agregar(velas, Timeframe.M5);
        var paso = resultado.Count == 2 && resultado.All(v => !v.EsParcial && v.MinutosRecibidos == 5);

        return new Resultado("CasoBordeAlineado", paso,
            $"Velas generadas={resultado.Count}, todas completas={resultado.All(v => !v.EsParcial)} (esperado: 2 velas 5m completas)");
    }

    // Dataset arrancando a mitad de una vela 5m (00:03Z) -> la primera vela sale parcial.
    private static Resultado CasoBordeNoAlineado()
    {
        var baseDia = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var inicioReal = baseDia + 3 * UnMinutoMs; // 00:03Z: a mitad del intervalo 00:00-00:04
        var velas = Enumerable.Range(0, 4)
            .Select(i => new VelaCruda(inicioReal + i * UnMinutoMs, 100m, 101m, 99m, 100m, 1m))
            .ToList();

        var resultado = AgregadorMultiTimeframe.Agregar(velas, Timeframe.M5);
        // primera vela: minutos 00:03,00:04 dentro de [00:00,00:05) = 2 minutos recibidos de 5 esperados.
        var primera = resultado.Count > 0 ? resultado[0] : null;
        var paso = primera is not null && primera.EsParcial && primera.MinutosRecibidos == 2 && primera.MinutosEsperados == 5;

        return new Resultado("CasoBordeNoAlineado", paso,
            primera is null ? "Sin resultado" :
            $"Primera vela: EsParcial={primera.EsParcial} MinutosRecibidos={primera.MinutosRecibidos}/{primera.MinutosEsperados} (esperado: parcial, 2/5)");
    }

    // 1m -> 5m -> 1h debe dar el mismo resultado que 1m -> 1h directo.
    private static Resultado CasoAsociatividad()
    {
        var inicio = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var rnd = new Random(20240102); // seed fijo: determinista
        var velas = new List<VelaCruda>();
        var precio = 100m;
        for (var i = 0; i < 60; i++) // 1 hora completa de velas 1m
        {
            var open = precio;
            var close = precio + (decimal)(rnd.NextDouble() - 0.5);
            var high = Math.Max(open, close) + (decimal)rnd.NextDouble();
            var low = Math.Min(open, close) - (decimal)rnd.NextDouble();
            velas.Add(new VelaCruda(inicio + i * UnMinutoMs, open, high, low, close, (decimal)(i + 1)));
            precio = close;
        }

        var directo = AgregadorMultiTimeframe.Agregar(velas, Timeframe.H1);
        var intermedio5m = AgregadorMultiTimeframe.Agregar(velas, Timeframe.M5);
        var recursivo = AgregadorMultiTimeframe.Agregar(intermedio5m, Timeframe.H1);

        var paso = directo.Count == 1 && recursivo.Count == 1
            && directo[0].Open == recursivo[0].Open
            && directo[0].High == recursivo[0].High
            && directo[0].Low == recursivo[0].Low
            && directo[0].Close == recursivo[0].Close
            && directo[0].Volume == recursivo[0].Volume
            && directo[0].MinutosRecibidos == recursivo[0].MinutosRecibidos;

        return new Resultado("CasoAsociatividad", paso,
            $"Directo(1m->1h): O={directo.FirstOrDefault()?.Open} H={directo.FirstOrDefault()?.High} L={directo.FirstOrDefault()?.Low} C={directo.FirstOrDefault()?.Close} V={directo.FirstOrDefault()?.Volume} | " +
            $"Recursivo(1m->5m->1h): O={recursivo.FirstOrDefault()?.Open} H={recursivo.FirstOrDefault()?.High} L={recursivo.FirstOrDefault()?.Low} C={recursivo.FirstOrDefault()?.Close} V={recursivo.FirstOrDefault()?.Volume}");
    }

    // Volume(timeframe superior) == suma(Volume 1m) para CADA vela generada, no solo el total.
    private static Resultado CasoConservacionVolumen()
    {
        var inicio = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var velas = Enumerable.Range(0, 15)
            .Select(i => new VelaCruda(inicio + i * UnMinutoMs, 100m, 101m, 99m, 100m, (i + 1) * 1.5m))
            .ToList();

        var resultado = AgregadorMultiTimeframe.Agregar(velas, Timeframe.M5);
        var volumenTotalOrigen = velas.Sum(v => v.Volume);
        var volumenTotalDerivado = resultado.Sum(v => v.Volume);

        // ademas: cada vela individual debe conservar exactamente la suma de SUS 5 velas 1m.
        var cadaVelaCorrecta = true;
        for (var k = 0; k < resultado.Count; k++)
        {
            var sumaEsperada = velas.Skip(k * 5).Take(5).Sum(v => v.Volume);
            if (resultado[k].Volume != sumaEsperada) cadaVelaCorrecta = false;
        }

        var paso = volumenTotalOrigen == volumenTotalDerivado && cadaVelaCorrecta;

        return new Resultado("CasoConservacionVolumen", paso,
            $"VolumenOrigen={volumenTotalOrigen} VolumenDerivado={volumenTotalDerivado} CadaVelaCorrecta={cadaVelaCorrecta}");
    }

    // Un hueco en el 1m NO debe generar una vela superior "completa" tapando el hueco: debe
    // reflejarse como MinutosRecibidos menor, nunca inventar datos para llegar a MinutosEsperados.
    private static Resultado CasoHuecoHeredado()
    {
        var inicio = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        // faltan los minutos 2 y 3 del intervalo 5m [00:00,00:05) -> solo 3 de 5 minutos.
        var velas = new List<VelaCruda>
        {
            new(inicio + 0 * UnMinutoMs, 100m, 101m, 99m, 100m, 1m),
            new(inicio + 1 * UnMinutoMs, 100m, 101m, 99m, 100m, 1m),
            new(inicio + 4 * UnMinutoMs, 100m, 101m, 99m, 100m, 1m),
        };

        var resultado = AgregadorMultiTimeframe.Agregar(velas, Timeframe.M5);
        var v = resultado.Count == 1 ? resultado[0] : null;
        var paso = v is not null && v.EsParcial && v.MinutosRecibidos == 3 && v.MinutosEsperados == 5;

        return new Resultado("CasoHuecoHeredado", paso,
            v is null ? "Sin resultado" :
            $"MinutosRecibidos={v?.MinutosRecibidos}/{v?.MinutosEsperados} EsParcial={v?.EsParcial} (esperado: 3/5, parcial — no se debe fabricar una vela completa)");
    }
}
