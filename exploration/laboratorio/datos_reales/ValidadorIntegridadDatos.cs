namespace TD_Project.DatosReales;

// Validador de integridad del CSV crudo, previo a congelar un dataset como "oficial" en
// datasets/reales/ (ver PLAN_FASE2A.md, seccion 5). Un dato malo puede parecer un bug del motor
// si no se descarta primero en la frontera de entrada.
public static class ValidadorIntegridadDatos
{
    public const long UnMinutoMs = 60_000;

    public sealed record Hueco(long DesdeMs, long HastaMs, int MinutosFaltantes);

    public sealed record Veredicto(bool AptoParaCongelar, IReadOnlyList<string> Errores, IReadOnlyList<Hueco> Huecos);

    public static Veredicto Verificar(IReadOnlyList<VelaCruda> velas)
    {
        var errores = new List<string>();
        var huecos = new List<Hueco>();

        if (velas.Count == 0)
        {
            errores.Add("Dataset vacio: no hay velas para validar.");
            return new Veredicto(false, errores, huecos);
        }

        var vistos = new HashSet<long>();
        for (var i = 0; i < velas.Count; i++)
        {
            var v = velas[i];

            // Orden y duplicados: bloqueantes sin excepcion (defecto de descarga, no del mercado).
            if (!vistos.Add(v.TimestampUtcMs))
            {
                errores.Add($"Timestamp duplicado: {v.TimestampUtcMs}");
                continue;
            }
            if (i > 0 && v.TimestampUtcMs <= velas[i - 1].TimestampUtcMs)
            {
                errores.Add($"Orden temporal roto: vela[{i}]={v.TimestampUtcMs} no es mayor que vela[{i - 1}]={velas[i - 1].TimestampUtcMs}");
                continue;
            }

            // OHLC/volumen invalido: bloqueante sin excepcion.
            if (v.High < v.Open) errores.Add($"Timestamp={v.TimestampUtcMs}: High({v.High}) < Open({v.Open})");
            if (v.High < v.Close) errores.Add($"Timestamp={v.TimestampUtcMs}: High({v.High}) < Close({v.Close})");
            if (v.Low > v.Open) errores.Add($"Timestamp={v.TimestampUtcMs}: Low({v.Low}) > Open({v.Open})");
            if (v.Low > v.Close) errores.Add($"Timestamp={v.TimestampUtcMs}: Low({v.Low}) > Close({v.Close})");
            if (v.High < v.Low) errores.Add($"Timestamp={v.TimestampUtcMs}: High({v.High}) < Low({v.Low})");
            if (v.Volume < 0) errores.Add($"Timestamp={v.TimestampUtcMs}: Volume negativo ({v.Volume})");

            // Continuidad: hueco es dato del mundo real, no error bloqueante (politica: rechaza
            // el dataset oficial pero se documenta aparte, ver PLAN_FASE2A.md seccion 5).
            if (i > 0)
            {
                var esperado = velas[i - 1].TimestampUtcMs + UnMinutoMs;
                if (v.TimestampUtcMs > esperado)
                {
                    var faltantes = (int)((v.TimestampUtcMs - esperado) / UnMinutoMs);
                    huecos.Add(new Hueco(velas[i - 1].TimestampUtcMs, v.TimestampUtcMs, faltantes));
                }
            }
        }

        var apto = errores.Count == 0 && huecos.Count == 0;
        return new Veredicto(apto, errores, huecos);
    }
}
