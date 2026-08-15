namespace TD_Project.Domain.Strategy.Dsl;

// spec: RN-16 — esquema JSON declarativo minimo: condicion sobre un indicador parametrico
// (unico soportado en V1: SMA sobre Close) comparado contra un campo de la vela actual, y una
// accion que emite un OrderRequest. Sin campo "comando"/"offset" — su sola presencia ya viola
// RN-16 (prohibicion de ejecucion de codigo externo y de referencias look-ahead).
public sealed record CondicionDsl(string? Indicador, int? Periodo, string? Operador, string? Campo);

public sealed record AccionDsl(string Side, string Type);

public sealed record DocumentoDsl(CondicionDsl Condicion, AccionDsl Accion);
