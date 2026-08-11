using TD_Project.EvaluacionMultiTf;

namespace TD_Project.AnalisisEscenariosMercado;

// D-032 (auditoria, 2026-08-11): calcula la serie SesgoDI = |DI+-DI-|/(DI+ + DI-) sobre la zona
// ADX < UmbralAdxTendencia del dataset congelado, y obtiene la mediana como candidato de
// UmbralSesgoDI. Formula identica a ClasificadorAdxExperimental.cs (mismo SuavizadoWilder de
// Wilder), pero expone DI+/DI-/ADX por ventana en vez de devolver solo el Escenario final — no
// modifica ClasificadorAdxExperimental.cs, es un calculo paralelo de solo lectura sobre el mismo
// dataset OHLC. No referencia InfoOperacionResuelta, PerfilMultiTf ni ninguna estrategia (D-016).

public readonly record struct MuestraAdxDi(long InicioUtcMs, decimal Adx, decimal DiMas, decimal DiMenos, decimal SesgoDiRelativo);

public static class CalibradorUmbralSesgoDI
{
    public static IReadOnlyList<MuestraAdxDi> CalcularSerie(IReadOnlyList<VelaDerivadaCruda> velas, int periodo)
    {
        if (velas.Count <= periodo * 2)
            return Array.Empty<MuestraAdxDi>();

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

        var resultado = new List<MuestraAdxDi>();
        decimal? adxPrevio = null;
        var dxAcumulados = new List<decimal>();

        for (var i = periodo; i < velas.Count; i++)
        {
            if (trSuavizado[i] == 0m)
                continue; // division por cero: vela sin rango verdadero, ventana excluida (misma regla que ClasificadorAdxExperimental)

            var diMas = 100m * dmMasSuavizado[i] / trSuavizado[i];
            var diMenos = 100m * dmMenosSuavizado[i] / trSuavizado[i];
            var sumaDi = diMas + diMenos;
            if (sumaDi == 0m)
                continue; // division por cero: DI+ y DI- ambos nulos, ventana excluida (mismo criterio que ADX)

            var dx = 100m * Math.Abs(diMas - diMenos) / sumaDi;
            dxAcumulados.Add(dx);

            if (dxAcumulados.Count < periodo)
                continue; // ventana de calentamiento del ADX, sin valor ADX valido todavia

            var adx = adxPrevio is null
                ? dxAcumulados.TakeLast(periodo).Average()
                : (adxPrevio.Value * (periodo - 1) + dx) / periodo;
            adxPrevio = adx;

            var sesgoDiRelativo = Math.Abs(diMas - diMenos) / sumaDi; // identico a dx/100, expuesto sin escalar a 0-100

            resultado.Add(new MuestraAdxDi(velas[i].InicioUtcMs, adx, diMas, diMenos, sesgoDiRelativo));
        }

        return resultado;
    }

    // Mediana de SesgoDiRelativo restringida a la zona ADX < umbralTendencia (D-032, paso 2:
    // "obtener distribucion sobre dataset congelado" se calcula solo dentro de la zona de interes,
    // no sobre toda la serie — fuera de esa zona el candidato ya se clasifica Alcista/Bajista).
    public static decimal? CalcularMedianaEnZonaSinTendencia(IReadOnlyList<MuestraAdxDi> serie, decimal umbralTendencia)
    {
        var muestrasSinTendencia = serie.Where(m => m.Adx < umbralTendencia).Select(m => m.SesgoDiRelativo).OrderBy(v => v).ToList();
        if (muestrasSinTendencia.Count == 0)
            return null;

        var mitad = muestrasSinTendencia.Count / 2;
        return muestrasSinTendencia.Count % 2 == 1
            ? muestrasSinTendencia[mitad]
            : (muestrasSinTendencia[mitad - 1] + muestrasSinTendencia[mitad]) / 2m;
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
