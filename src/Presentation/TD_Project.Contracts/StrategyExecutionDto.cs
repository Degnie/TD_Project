namespace TD_Project.Contracts;

// spec: RN-16, CU-22 — contrato de entrada del endpoint de ejecucion de estrategias DSL. El DSL
// en si viaja como texto JSON crudo (EstrategiaDslJson), este DTO solo agrega los datos de
// experimento que el DSL necesita para ejecutarse contra un dataset ya ingerido.
public sealed record EjecutarDslRequestDto(string DatasetHash, decimal CapitalInicial, string EstrategiaDslJson);
