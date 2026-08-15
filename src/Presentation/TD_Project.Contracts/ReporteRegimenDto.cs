namespace TD_Project.Contracts;

// spec: RN-19, CU-24 — espejo de Application.FilaFaseRegimen/ReporteRegimenResultado, sin logica.
public sealed record FaseRegimenDto(string Regimen, int TotalTrades, decimal PnLTotal, decimal WinRate);

public sealed record ReporteRegimenDto(IReadOnlyList<FaseRegimenDto> Fases, string? RegimenOptimo);
