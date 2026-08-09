using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;

namespace TD_Project.Domain.VelaResolution;

// spec: RN-11 — TrayectoriaOficial/EquityFinal/EquityDescartada/Fills/OrdenesCanceladas
// describen la seleccion ya hecha. FillsA/FillsB/EquityA/EquityB conservan evidencia de AMBAS
// trayectorias evaluadas (sin alterar la seleccion). CashFinal/MarginFinal/UnrealizedPnLFinal/
// LotesVivosFinal son el desglose de EquityFinal, pertenecientes UNICAMENTE a la rama oficial.
public sealed record ResultadoResolucionVela(
    Trayectoria TrayectoriaOficial,
    decimal EquityFinal,
    decimal EquityDescartada,
    IReadOnlyList<Fill> Fills,
    IReadOnlyList<Order> OrdenesCanceladas,
    IReadOnlyList<Fill> FillsA,
    IReadOnlyList<Fill> FillsB,
    decimal EquityA,
    decimal EquityB,
    decimal CashFinal,
    decimal MarginFinal,
    decimal UnrealizedPnLFinal,
    IReadOnlyList<Lote> LotesVivosFinal);
