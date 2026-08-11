namespace TD_Project.EvaluacionMultiTf;

// Precision D del usuario: cada corrida debe quedar trazable para poder reproducirse meses
// despues. Registra exactamente que dataset, que version de agregacion y que parametros
// produjeron un resultado — sin esto, "el resultado cambio" no se puede diagnosticar como
// motor/estrategia vs. dataset vs. parametros.
public sealed record IdentidadExperimento(
    string Dataset,
    string Timeframe,
    string Estrategia,
    decimal CapitalInicial,
    string AggregationVersion,
    string SourceSha256,
    string TimeframeSha256,
    DateTime FechaEjecucionUtc);
