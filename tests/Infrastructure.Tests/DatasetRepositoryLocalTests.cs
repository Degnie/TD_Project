using TD_Project.Domain.Shared;
using TD_Project.Infrastructure;
using Xunit;

namespace TD_Project.Infrastructure.Tests;

// spec: RN-15, CU-21 — adaptador de catalogo local en disco (Opcion A del ADR-001, sin base de
// datos en esta fase). Cada test usa una carpeta temporal aislada, eliminada al finalizar.
public class DatasetRepositoryLocalTests : IDisposable
{
    private readonly string _carpetaTemporal = Path.Combine(Path.GetTempPath(), "td-project-tests-" + Guid.NewGuid());

    private static readonly Candle[] VelasValidas =
    {
        new(1, 100m, 105m, 95m, 102m, 500m),
        new(2, 102m, 106m, 100m, 104m, 500m)
    };

    public void Dispose()
    {
        if (Directory.Exists(_carpetaTemporal))
            Directory.Delete(_carpetaTemporal, recursive: true);
    }

    // spec: CU-21 — subida de archivo de velas 1m -> validado y guardado en catalogo local ->
    // retorna DatasetHash e ID de seleccion
    [Fact]
    public void GuardarUnDatasetValidoRetornaUnDatasetHash()
    {
        var repositorio = new DatasetRepositoryLocal(_carpetaTemporal);

        var hash = repositorio.Guardar("btc-1m-demo", VelasValidas);

        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    // spec: RN-15, RNF-13 — round-trip: lo guardado se recupera identico por su hash
    [Fact]
    public void ObtenerPorHashRecuperaLasMismasVelasGuardadas()
    {
        var repositorio = new DatasetRepositoryLocal(_carpetaTemporal);
        var hash = repositorio.Guardar("btc-1m-demo", VelasValidas);

        var velasRecuperadas = repositorio.Obtener(hash);

        Assert.Equal(VelasValidas, velasRecuperadas);
    }

    // spec: RN-15 — un dataset invalido (High < Low) se rechaza atomicamente, 0 velas guardadas
    [Fact]
    public void GuardarUnDatasetInvalidoNoPersisteNada()
    {
        var repositorio = new DatasetRepositoryLocal(_carpetaTemporal);
        var velasInvalidas = new[] { new Candle(1, 100m, 95m, 100m, 97m, 500m) };

        Assert.Throws<InvalidOperationException>(() => repositorio.Guardar("dataset-invalido", velasInvalidas));
        Assert.Empty(repositorio.ListarCatalogo());
    }

    // spec: CU-21 — el catalogo local lista los datasets ya validados y guardados
    [Fact]
    public void ListarCatalogoIncluyeLosDatasetsGuardados()
    {
        var repositorio = new DatasetRepositoryLocal(_carpetaTemporal);
        repositorio.Guardar("btc-1m-demo", VelasValidas);

        var catalogo = repositorio.ListarCatalogo();

        Assert.Single(catalogo);
    }
}
