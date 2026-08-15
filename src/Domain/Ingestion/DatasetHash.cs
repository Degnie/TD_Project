using System.Security.Cryptography;
using System.Text;
using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Ingestion;

// spec: RN-15 — hash inmutable calculado al persistir en el catalogo local.
public static class DatasetHash
{
    public static string Calcular(IReadOnlyList<Candle> velas)
    {
        var contenido = new StringBuilder();
        foreach (var vela in velas)
            contenido.Append(vela.Timestamp).Append('|').Append(vela.Open).Append('|').Append(vela.High)
                .Append('|').Append(vela.Low).Append('|').Append(vela.Close).Append('|').Append(vela.Volume).Append(';');

        var bytes = Encoding.UTF8.GetBytes(contenido.ToString());
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
