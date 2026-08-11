using TD_Project.EvaluacionMultiTf;

namespace TD_Project.AnalisisEscenariosMercado;

// Fase 1.4-A, Candidato A (Medias moviles / EMA) — CONFIGURACION EXPLORATORIA, no oficial (D-022).
// Riesgo ya señalado por auditoria: fuerte dependencia del timeframe (un periodo de N velas
// representa una ventana temporal muy distinta en 1m que en 1D) — evaluado explicitamente en
// EVALUACION_CLASIFICADORES_REGIMEN_V1.md §3.3 (Consistencia multi-timeframe).
public static class ClasificadorEmaExperimental
{
    // CONFIGURACION EXPLORATORIA — no oficial. Mismo criterio que D-022.
    public const int PeriodoEmaExploratorio = 20;
    public const decimal UmbralPendienteExploratorio = 0.005m; // 0.5% entre EMA actual y EMA previa

    public static IReadOnlyList<VentanaClasificada> Clasificar(IReadOnlyList<VelaDerivadaCruda> velas, int periodoEma)
    {
        if (periodoEma <= 1 || velas.Count <= periodoEma)
            return Array.Empty<VentanaClasificada>();

        var alfa = 2m / (periodoEma + 1);
        var ema = velas[0].Close;
        var emaPrevia = ema;
        var resultado = new List<VentanaClasificada>();

        for (var i = 1; i < velas.Count; i++)
        {
            emaPrevia = ema;
            ema = velas[i].Close * alfa + ema * (1 - alfa);

            if (i < periodoEma || emaPrevia == 0m)
                continue; // ventana de calentamiento de la EMA, sin señal fiable todavia

            var pendienteRelativa = (ema - emaPrevia) / emaPrevia;
            var escenario = pendienteRelativa > UmbralPendienteExploratorio ? Escenario.Alcista
                : pendienteRelativa < -UmbralPendienteExploratorio ? Escenario.Bajista
                : Escenario.Lateral; // el candidato EMA no distingue Ambiguo de Lateral (limitacion del enfoque, ver informe)

            resultado.Add(new VentanaClasificada(velas[i - 1].InicioUtcMs, velas[i].InicioUtcMs, escenario));
        }

        return resultado;
    }
}
