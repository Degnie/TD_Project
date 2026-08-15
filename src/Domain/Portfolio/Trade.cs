namespace TD_Project.Domain.Portfolio;

// spec: glosario "Trade" — ciclo vital de exposicion desde apertura hasta cierre total
// spec: RN-19 — TimestampApertura/TimestampCierre son metadata de ejecucion (no alteran RN-07/
// 08/09/10): permiten asociar el Trade a la vela en que se abrio, sin depender de orden de listas
// ni de inferencia sobre Fills/EquityCurve (decision explicita del cliente, ver caso de RN-19).
// Opcionales con default 0 para no romper la construccion posicional de tests preexistentes ajenos
// a este delta (mismo patron ya usado en ConfiguracionExperimento).
public sealed record Trade(
    decimal CantidadInicial,
    decimal PrecioApertura,
    decimal? PrecioCierre,
    decimal RealizedPnL,
    long TimestampApertura = 0,
    long TimestampCierre = 0);
