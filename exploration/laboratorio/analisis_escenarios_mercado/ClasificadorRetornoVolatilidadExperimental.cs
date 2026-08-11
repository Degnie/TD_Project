using TD_Project.EvaluacionMultiTf;

namespace TD_Project.AnalisisEscenariosMercado;

// Fase 1.4-A, Candidato C (Retorno + Volatilidad) — CONFIGURACION EXPLORATORIA, no oficial (D-022).
// Formaliza PendienteNormalizada/RangoRelativo ya esbozados en
// ESPECIFICACION_ANALISIS_ESCENARIOS_MERCADO_V1.md §4. Umbrales y tamaño de ventana son valores de
// referencia para poder ejecutar la comparacion (D-018/D-019 siguen pendientes, no se resuelven
// aqui). No conoce ninguna estrategia ni resultado de backtest (D-016).
public static class ClasificadorRetornoVolatilidadExperimental
{
    // CONFIGURACION EXPLORATORIA — no oficial. Valores de referencia solo para poder comparar
    // propiedades del clasificador (estabilidad/cobertura/consistencia), no para congelar D-018/D-019.
    public const decimal UmbralPendienteExploratorio = 0.01m; // 1% por ventana
    public const decimal UmbralRangoAmbiguoExploratorio = 0.03m; // 3% por ventana

    public static IReadOnlyList<VentanaClasificada> Clasificar(IReadOnlyList<VelaDerivadaCruda> velas, int tamanoVentana)
    {
        if (tamanoVentana <= 0)
            throw new ArgumentOutOfRangeException(nameof(tamanoVentana));

        var resultado = new List<VentanaClasificada>();
        for (var inicio = 0; inicio + tamanoVentana <= velas.Count; inicio += tamanoVentana)
        {
            var ventana = velas.Skip(inicio).Take(tamanoVentana).ToList();
            var closeInicial = ventana[0].Open;
            var closeFinal = ventana[^1].Close;
            var maxHigh = ventana.Max(v => v.High);
            var minLow = ventana.Min(v => v.Low);

            if (closeInicial == 0m)
                continue; // guarda defensiva, no deberia ocurrir con datos validados en Fase 2A

            var pendienteNormalizada = (closeFinal - closeInicial) / closeInicial;
            var rangoRelativo = (maxHigh - minLow) / closeInicial;

            var escenario = ClasificarVentana(pendienteNormalizada, rangoRelativo);
            resultado.Add(new VentanaClasificada(ventana[0].InicioUtcMs, ventana[^1].InicioUtcMs, escenario));
        }

        return resultado;
    }

    private static Escenario ClasificarVentana(decimal pendienteNormalizada, decimal rangoRelativo)
    {
        if (Math.Abs(pendienteNormalizada) <= UmbralPendienteExploratorio && rangoRelativo > UmbralRangoAmbiguoExploratorio)
            return Escenario.Ambiguo; // sin tendencia clara pero con dispersion alta: no es "tranquilo"

        if (pendienteNormalizada > UmbralPendienteExploratorio)
            return Escenario.Alcista;

        if (pendienteNormalizada < -UmbralPendienteExploratorio)
            return Escenario.Bajista;

        return Escenario.Lateral;
    }
}
