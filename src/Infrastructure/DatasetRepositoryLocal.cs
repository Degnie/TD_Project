using System.Text.Json;
using TD_Project.Domain.Ingestion;
using TD_Project.Domain.Shared;

namespace TD_Project.Infrastructure;

// spec: RN-15, CU-21 — Opcion A del ADR-001: almacenamiento local en disco, sin base de datos.
// Cada dataset se persiste como un archivo <hash>.json bajo la carpeta raiz; el catalogo (indice
// nombre->hash) se persiste en un unico archivo catalogo.json en la misma carpeta.
public sealed class DatasetRepositoryLocal : IDatasetRepository
{
    private readonly string _carpetaRaiz;
    private readonly string _rutaCatalogo;

    public DatasetRepositoryLocal(string carpetaRaiz)
    {
        _carpetaRaiz = carpetaRaiz;
        _rutaCatalogo = Path.Combine(_carpetaRaiz, "catalogo.json");
        Directory.CreateDirectory(_carpetaRaiz);
    }

    // spec: RN-15 — rechazo atomico ante dataset invalido, 0 velas persistidas
    public string Guardar(string nombre, IReadOnlyList<Candle> velas)
    {
        var validacion = ValidadorDataset.Validar(velas);
        if (!validacion.EsValido)
            throw new InvalidOperationException($"Dataset invalido: {validacion.Motivo}");

        var hash = DatasetHash.Calcular(velas);
        var rutaDataset = Path.Combine(_carpetaRaiz, $"{hash}.json");
        File.WriteAllText(rutaDataset, JsonSerializer.Serialize(velas));

        var catalogo = LeerCatalogo();
        if (!catalogo.Any(e => e.Hash == hash))
        {
            catalogo.Add(new EntradaCatalogo(nombre, hash));
            File.WriteAllText(_rutaCatalogo, JsonSerializer.Serialize(catalogo));
        }

        return hash;
    }

    public IReadOnlyList<Candle>? Obtener(string hash)
    {
        var rutaDataset = Path.Combine(_carpetaRaiz, $"{hash}.json");
        if (!File.Exists(rutaDataset))
            return null;

        return JsonSerializer.Deserialize<List<Candle>>(File.ReadAllText(rutaDataset));
    }

    public IReadOnlyList<EntradaCatalogo> ListarCatalogo() => LeerCatalogo();

    private List<EntradaCatalogo> LeerCatalogo()
    {
        if (!File.Exists(_rutaCatalogo))
            return new List<EntradaCatalogo>();

        return JsonSerializer.Deserialize<List<EntradaCatalogo>>(File.ReadAllText(_rutaCatalogo)) ?? new List<EntradaCatalogo>();
    }
}
