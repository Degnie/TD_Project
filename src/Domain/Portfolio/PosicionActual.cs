namespace TD_Project.Domain.Portfolio;

// spec: glosario "Position" — inventario acumulado vivo, escalar con signo
public static class PosicionActual
{
    public static decimal De(PortfolioState portfolio) => portfolio.LotesVivos.Sum(l => l.Cantidad);
}
