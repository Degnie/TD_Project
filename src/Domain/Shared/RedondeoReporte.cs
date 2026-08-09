namespace TD_Project.Domain.Shared;

// spec: RNF-05 — redondeo exclusivo al final, Half-to-Even a 2 decimales.
// Equity_rep es la suma estricta de sus componentes ya redondeados, sin ajuste arbitrario.
public static class RedondeoReporte
{
    public static decimal ADosDecimales(decimal valorInterno) =>
        Math.Round(valorInterno, 2, MidpointRounding.ToEven);

    public static decimal EquityReportado(decimal cashInterno, decimal marginInterno, decimal unrealizedPnLInterno) =>
        ADosDecimales(cashInterno) + ADosDecimales(marginInterno) + ADosDecimales(unrealizedPnLInterno);
}
