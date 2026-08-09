using TD_Project.Domain.Shared;

namespace TD_Project.Application;

// spec: RN-11 — evidencia historica de ambas trayectorias evaluadas por vela: cual gano y por que.
public sealed record BranchResolutionInfo(
    long Timestamp,
    TrayectoriaResolucion TrayectoriaOficial,
    decimal EquityA,
    decimal EquityB,
    IReadOnlyList<Fill> FillsA,
    IReadOnlyList<Fill> FillsB);
