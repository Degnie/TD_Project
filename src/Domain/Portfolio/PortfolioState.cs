namespace TD_Project.Domain.Portfolio;

// spec: glosario "Portfolio" — contenedor maestro de Cash, Margin y Positions
public sealed class PortfolioState
{
    public decimal Cash { get; set; }
    public decimal Margin { get; set; }
}
