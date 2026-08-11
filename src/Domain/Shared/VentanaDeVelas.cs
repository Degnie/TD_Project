namespace TD_Project.Domain.Shared;

// spec: RN-13, glosario "DataSlice" — vista O(1) sobre los primeros 'Longitud' elementos de una
// lista subyacente, sin copiar. Preserva el bloqueo fisico contra lecturas mas alla de N: el
// indexador lanza IndexOutOfRangeException fuera de [0, Longitud), igual que una lista truncada
// real, pero sin el costo O(n) por vela de reconstruir una sublista en cada iteracion del
// backtest (ver BacktestRunner.cs).
public sealed class VentanaDeVelas : IReadOnlyList<Candle>
{
    private readonly IReadOnlyList<Candle> _origen;

    public VentanaDeVelas(IReadOnlyList<Candle> origen, int longitud)
    {
        _origen = origen;
        Count = longitud;
    }

    public int Count { get; }

    public Candle this[int index] =>
        index < 0 || index >= Count
            ? throw new ArgumentOutOfRangeException(nameof(index))
            : _origen[index];

    public IEnumerator<Candle> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
            yield return _origen[i];
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
