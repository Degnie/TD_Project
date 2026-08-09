using TD_Project.Domain.Shared;

namespace TD_Project.Domain.VelaResolution;

// spec: RN-11
public sealed record ResultadoResolucionVela(
    Trayectoria TrayectoriaOficial,
    decimal EquityFinal,
    decimal EquityDescartada,
    IReadOnlyList<Fill> Fills,
    IReadOnlyList<Order> OrdenesCanceladas);
