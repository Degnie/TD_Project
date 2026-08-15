using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Ingestion;

// spec: RN-15 — timestamps duplicados/desordenados, valores nulos, o precios High<Low o <=0
// rechazan la ingestion atomicamente.
public sealed record ResultadoValidacionDataset(bool EsValido, string? Motivo);

public static class ValidadorDataset
{
    public static ResultadoValidacionDataset Validar(IReadOnlyList<Candle> velas)
    {
        for (var i = 0; i < velas.Count; i++)
        {
            var vela = velas[i];

            if (vela.Open <= 0m || vela.High <= 0m || vela.Low <= 0m || vela.Close <= 0m)
                return new ResultadoValidacionDataset(false, $"Vela en indice {i}: precios deben ser positivos.");

            if (vela.High < vela.Low)
                return new ResultadoValidacionDataset(false, $"Vela en indice {i}: High ({vela.High}) menor que Low ({vela.Low}).");

            if (vela.Open > vela.High || vela.Open < vela.Low || vela.Close > vela.High || vela.Close < vela.Low)
                return new ResultadoValidacionDataset(false, $"Vela en indice {i}: Open/Close fuera del rango [Low, High].");

            if (i > 0 && vela.Timestamp <= velas[i - 1].Timestamp)
                return new ResultadoValidacionDataset(false, $"Vela en indice {i}: Timestamp no estrictamente creciente respecto a la vela anterior.");
        }

        return new ResultadoValidacionDataset(true, null);
    }
}
