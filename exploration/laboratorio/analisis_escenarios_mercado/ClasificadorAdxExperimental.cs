using TD_Project.EvaluacionMultiTf;

namespace TD_Project.AnalisisEscenariosMercado;

// Fase 1.4-A, Candidato B (ADX + DI, Wilder) — CONFIGURACION EXPLORATORIA, no oficial (D-022).
// Separa explicitamente "hay tendencia" (ADX alto) de "no hay tendencia" (ADX bajo -> Lateral),
// y usa +DI/-DI para el signo (Alcista/Bajista). Umbral ADX=25 es convencional en literatura
// tecnica (no elegido mirando BTC/USDT), pero sigue siendo exploratorio hasta una decision D-018
// posterior — no se congela aqui.
public static class ClasificadorAdxExperimental
{
    // CONFIGURACION EXPLORATORIA — no oficial. Umbral convencional de literatura, no ajustado a BTC/USDT.
    public const int PeriodoAdxExploratorio = 14;
    public const decimal UmbralAdxTendenciaExploratorio = 25m;

    public static IReadOnlyList<VentanaClasificada> Clasificar(IReadOnlyList<VelaDerivadaCruda> velas, int periodo)
    {
        if (velas.Count <= periodo * 2)
            return Array.Empty<VentanaClasificada>();

        var trs = new decimal[velas.Count];
        var dmMas = new decimal[velas.Count];
        var dmMenos = new decimal[velas.Count];

        for (var i = 1; i < velas.Count; i++)
        {
            var altoActual = velas[i].High;
            var bajoActual = velas[i].Low;
            var altoPrevio = velas[i - 1].High;
            var bajoPrevio = velas[i - 1].Low;
            var cierrePrevio = velas[i - 1].Close;

            trs[i] = Math.Max(altoActual - bajoActual, Math.Max(Math.Abs(altoActual - cierrePrevio), Math.Abs(bajoActual - cierrePrevio)));

            var subidaDireccional = altoActual - altoPrevio;
            var bajadaDireccional = bajoPrevio - bajoActual;

            dmMas[i] = subidaDireccional > bajadaDireccional && subidaDireccional > 0 ? subidaDireccional : 0m;
            dmMenos[i] = bajadaDireccional > subidaDireccional && bajadaDireccional > 0 ? bajadaDireccional : 0m;
        }

        var trSuavizado = SuavizadoWilder(trs, periodo);
        var dmMasSuavizado = SuavizadoWilder(dmMas, periodo);
        var dmMenosSuavizado = SuavizadoWilder(dmMenos, periodo);

        var resultado = new List<VentanaClasificada>();
        decimal? adxPrevio = null;
        var dxAcumulados = new List<decimal>();

        for (var i = periodo; i < velas.Count; i++)
        {
            if (trSuavizado[i] == 0m)
                continue;

            var diMas = 100m * dmMasSuavizado[i] / trSuavizado[i];
            var diMenos = 100m * dmMenosSuavizado[i] / trSuavizado[i];
            var sumaDi = diMas + diMenos;
            if (sumaDi == 0m)
                continue;

            var dx = 100m * Math.Abs(diMas - diMenos) / sumaDi;
            dxAcumulados.Add(dx);

            if (dxAcumulados.Count < periodo)
                continue; // ventana de calentamiento del ADX (promedio de DX sobre "periodo" valores)

            var adx = adxPrevio is null
                ? dxAcumulados.TakeLast(periodo).Average()
                : (adxPrevio.Value * (periodo - 1) + dx) / periodo;
            adxPrevio = adx;

            var escenario = adx < UmbralAdxTendenciaExploratorio
                ? Escenario.Lateral
                : diMas > diMenos ? Escenario.Alcista : Escenario.Bajista;

            resultado.Add(new VentanaClasificada(velas[i - 1].InicioUtcMs, velas[i].InicioUtcMs, escenario));
        }

        return resultado;
    }

    private static decimal[] SuavizadoWilder(decimal[] valores, int periodo)
    {
        var resultado = new decimal[valores.Length];
        decimal acumulado = 0m;

        for (var i = 1; i <= periodo && i < valores.Length; i++)
            acumulado += valores[i];

        if (periodo < valores.Length)
            resultado[periodo] = acumulado;

        for (var i = periodo + 1; i < valores.Length; i++)
            resultado[i] = resultado[i - 1] - resultado[i - 1] / periodo + valores[i];

        return resultado;
    }
}
