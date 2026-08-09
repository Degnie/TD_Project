namespace TD_Project.Domain.Portfolio;

// spec: glosario "Portfolio" — contenedor maestro de Cash, Margin y Positions.
// spec: glosario "Position" — inventario acumulado vivo de un activo, escalar con signo.
public sealed class PortfolioState
{
    public decimal Cash { get; set; }
    public decimal Margin { get; set; }
    public List<Lote> LotesVivos { get; } = new();

    // spec: RN-11 — cada trayectoria candidata resuelve sobre su propia copia del Portfolio, sin contaminar a la otra
    public PortfolioState Clonar()
    {
        var copia = new PortfolioState { Cash = Cash, Margin = Margin };
        copia.LotesVivos.AddRange(LotesVivos);
        return copia;
    }
}
