using TD_Project.Caso5.AnalisisCorpus;

namespace TD_Project.Caso6.Recomendador;

// spec: ESPECIFICACION_IMPLEMENTACION_RECOMENDADOR_CASO6_V1.md §1 (D-120, extendido por D-128) —
// unidad reportada = fila atomica, con gestor. Nunca agregada/promediada entre gestores.
public sealed record ConfiguracionCandidata(
    string Estrategia,
    string Timeframe,
    string IdentidadGestor,
    string NombreDataset,
    decimal ValorMetrica,
    int CantidadFilas,
    IReadOnlyList<string> CarpetasOrigen);

// spec: §1 (D-120) — nunca un valor unico senalado como salida. Candidatas en orden de aparicion, nunca
// por ValorMetrica.
public sealed record RecomendacionExperimental(
    string Perfil,
    string CriterioUsado,
    int ConfiguracionesAnalizadas,
    int ConfiguracionesConEvidencia,
    IReadOnlyList<ConfiguracionCandidata> Candidatas,
    string Limitaciones);

// spec: §2-§3 (D-128) — mecanismo de umbral por mediana, sin top-N/ranking. Perfiles de criterio
// unico (sin "Balanceado"). GrupoComparacion=(Estrategia,Timeframe,Dataset) SIN gestor calcula la
// mediana; ConfiguracionCandidata=(Estrategia,Timeframe,Dataset,Gestor) CON gestor es la unidad
// evaluada fila por fila contra esa mediana. Fusionar ambas claves anularia el umbral (grupo de 1
// fila -> mediana = ese valor -> filtro siempre verdadero) — ver P9.
public static class MotorRecomendacion
{
    public static RecomendacionExperimental Recomendar(IReadOnlyList<FilaCorpus> filas, string perfil, string? metricaPersonalizada = null)
    {
        var (metrica, direccion) = perfil switch
        {
            "Crecimiento" => ("PnLTotal", "Mayor"),
            "Preservacion" => ("DrawdownMaximoPct", "Menor"),
            "Personalizado" => (metricaPersonalizada ?? throw new ArgumentException("Perfil Personalizado requiere metricaPersonalizada.", nameof(metricaPersonalizada)), "Mayor"),
            _ => throw new ArgumentException($"Perfil no reconocido: '{perfil}'. Valores validos: Crecimiento, Preservacion, Personalizado.", nameof(perfil))
        };

        var conEvidencia = filas.Where(f => f.Estado == "Success" && ExtraerMetrica(f, metrica).HasValue).ToList();

        var gruposComparacionEnOrden = new List<(string Estrategia, string Timeframe, string Dataset)>();
        foreach (var f in conEvidencia)
        {
            var clave = (f.Estrategia, f.Timeframe, f.NombreDataset);
            if (!gruposComparacionEnOrden.Contains(clave)) gruposComparacionEnOrden.Add(clave);
        }

        var candidatas = CalcularCandidatas(conEvidencia, metrica, direccion);

        var criterioUsado = $"{perfil}: {metrica} {(direccion == "Mayor" ? ">=" : "<=")} mediana del grupo (Estrategia, Timeframe, Dataset)";
        var limitaciones =
            $"Recomendacion experimental sobre {filas.Count} filas del corpus persistido " +
            $"({gruposComparacionEnOrden.Count} grupo(s) Estrategia+Timeframe+Dataset con evidencia). " +
            "El umbral es la mediana del grupo de comparacion (sin gestor en su clave) — no representa " +
            "calidad absoluta, no crea ranking, no define un unico resultado preferente entre gestores. " +
            "Es unicamente un criterio de pertenencia al conjunto de candidatas bajo el perfil solicitado. " +
            "Las candidatas se reportan en orden de aparicion en el corpus, nunca por valor de metrica. " +
            "No constituye seleccion ni ejecucion automatica (D-118/D-119/D-120). Observacion historica " +
            "de backtest, no proyeccion futura.";

        return new RecomendacionExperimental(perfil, criterioUsado, gruposComparacionEnOrden.Count, candidatas.Select(c => (c.Estrategia, c.Timeframe, c.NombreDataset)).Distinct().Count(), candidatas, limitaciones);
    }

    // Clave del GRUPO DE COMPARACION (Estrategia, Timeframe, NombreDataset) — SIN gestor: reune las
    // filas de los distintos gestores presentes, calcula la mediana de la metrica sobre esos
    // valores (reutiliza AnalisisDescriptivo, ninguna formula nueva), y evalua el umbral fila por
    // fila. Clave de cada CONFIGURACIONCANDIDATA devuelta (Estrategia, Timeframe, NombreDataset,
    // Gestor) — CON gestor: solo las filas atomicas que cruzaron el umbral de su propio grupo,
    // reportadas individualmente, nunca agregadas ni promediadas entre si.
    private static IReadOnlyList<ConfiguracionCandidata> CalcularCandidatas(IReadOnlyList<FilaCorpus> filas, string metrica, string direccion)
    {
        var candidatas = new List<ConfiguracionCandidata>();

        var gruposEnOrden = new List<(string Estrategia, string Timeframe, string Dataset)>();
        foreach (var f in filas)
        {
            var clave = (f.Estrategia, f.Timeframe, f.NombreDataset);
            if (!gruposEnOrden.Contains(clave)) gruposEnOrden.Add(clave);
        }

        foreach (var grupo in gruposEnOrden)
        {
            var filasGrupo = filas.Where(f => f.Estrategia == grupo.Estrategia && f.Timeframe == grupo.Timeframe && f.NombreDataset == grupo.Dataset).ToList();
            var valores = filasGrupo.Select(f => ExtraerMetrica(f, metrica)!.Value).ToList();
            var mediana = CalcularMediana(valores);

            foreach (var fila in filasGrupo)
            {
                var valor = ExtraerMetrica(fila, metrica)!.Value;
                var cruzaUmbral = direccion == "Mayor" ? valor >= mediana : valor <= mediana;
                if (!cruzaUmbral) continue;

                candidatas.Add(new ConfiguracionCandidata(
                    fila.Estrategia, fila.Timeframe, fila.IdentidadGestor, fila.NombreDataset,
                    valor, 1, new[] { fila.CarpetaOrigen }));
            }
        }

        return candidatas;
    }

    private static decimal CalcularMediana(List<decimal> valores)
    {
        var ordenados = valores.OrderBy(v => v).ToList();
        var n = ordenados.Count;
        return n % 2 == 1 ? ordenados[n / 2] : (ordenados[n / 2 - 1] + ordenados[n / 2]) / 2m;
    }

    private static decimal? ExtraerMetrica(FilaCorpus f, string nombreMetrica) => nombreMetrica switch
    {
        "PnLTotal" => f.PnLTotal,
        "DrawdownMaximoPct" => f.DrawdownMaximoPct,
        "ProfitFactor" => f.ProfitFactor,
        "ExposicionMaxima" => f.ExposicionMaxima,
        "CashFinal" => f.CashFinal,
        "EquityFinal" => f.EquityFinal,
        _ => throw new ArgumentException($"Metrica no reconocida: '{nombreMetrica}'.", nameof(nombreMetrica))
    };
}
