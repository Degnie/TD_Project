using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;

namespace TD_Project.Laboratorio.Fixtures;

// Estrategia minima para validar el MOTOR contra datasets de market/: abre una posicion en
// N=0 y la mantiene hasta el final del dataset, sin logica propia que pueda introducir ruido en
// la interpretacion del resultado (a diferencia de Tres Mosqueteros/MHI, que tienen su propia
// logica de cuadrante/martingala). Sirve para aislar "el motor calcula bien" de "la estrategia
// decide bien".
public sealed class EstrategiaSimpleLaboratorio : IStrategy
{
    private readonly Side _lado;

    public EstrategiaSimpleLaboratorio(Side lado) => _lado = lado;

    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice) =>
        dataSlice.N == 0
            ? new[] { new OrderRequest(_lado, OrderType.Market, 1m) }
            : Array.Empty<OrderRequest>();
}
