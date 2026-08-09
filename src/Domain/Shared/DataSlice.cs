namespace TD_Project.Domain.Shared;

// spec: glosario "DataSlice", RN-13 (bloqueada contra lecturas mas alla de N)
public sealed class DataSlice
{
    private readonly IReadOnlyList<Candle> _velasHastaN;

    public DataSlice(IReadOnlyList<Candle> velasHastaN)
    {
        _velasHastaN = velasHastaN;
    }

    public int N => _velasHastaN.Count - 1;

    public Candle VelaActual => _velasHastaN[N];

    public IReadOnlyList<Candle> VelasHastaN => _velasHastaN;
}
