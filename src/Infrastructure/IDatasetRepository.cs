using TD_Project.Domain.Shared;

namespace TD_Project.Infrastructure;

// spec: RN-15, CU-21 — repositorio de catalogo local de datasets validados.
public interface IDatasetRepository
{
    string Guardar(string nombre, IReadOnlyList<Candle> velas);
    IReadOnlyList<Candle>? Obtener(string hash);
    IReadOnlyList<EntradaCatalogo> ListarCatalogo();
}

public sealed record EntradaCatalogo(string Nombre, string Hash);
