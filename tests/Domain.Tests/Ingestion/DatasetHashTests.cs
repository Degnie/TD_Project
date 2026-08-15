using TD_Project.Domain.Ingestion;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Domain.Tests.Ingestion;

public class DatasetHashTests
{
    private static readonly Candle[] VelasDeEjemplo =
    {
        new(1, 100m, 105m, 95m, 102m, 500m),
        new(2, 102m, 106m, 100m, 104m, 500m)
    };

    // spec: RN-15 — el hash se calcula al persistir en el catalogo local, es inmutable para el
    // mismo contenido (mismo criterio de determinismo que RNF-06 aplicado a Ingestion)
    [Fact]
    public void ElMismoDatasetProduceElMismoHash()
    {
        var hash1 = DatasetHash.Calcular(VelasDeEjemplo);
        var hash2 = DatasetHash.Calcular(VelasDeEjemplo);

        Assert.Equal(hash1, hash2);
    }

    // spec: RN-15 — datasets con contenido distinto producen hashes distintos
    [Fact]
    public void DatasetsConContenidoDistintoProducenHashesDistintos()
    {
        var otrasVelas = new[] { new Candle(1, 100m, 105m, 95m, 103m, 500m) };

        var hash1 = DatasetHash.Calcular(VelasDeEjemplo);
        var hash2 = DatasetHash.Calcular(otrasVelas);

        Assert.NotEqual(hash1, hash2);
    }
}
