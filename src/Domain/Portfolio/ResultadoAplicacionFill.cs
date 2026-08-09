namespace TD_Project.Domain.Portfolio;

// spec: RN-07 — resultado puntual de aplicar un Fill: RealizedPnL reconocido en ese Fill,
// Margin liberado, y el Trade que se cierra si la posicion llega a cero (null en otro caso).
public sealed record ResultadoAplicacionFill(Trade? TradeCerrado, decimal RealizedPnLReconocido, decimal MarginLiberado);
