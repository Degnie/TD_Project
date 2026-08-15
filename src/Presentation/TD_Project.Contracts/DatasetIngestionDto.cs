namespace TD_Project.Contracts;

// spec: RN-15, CU-21 — contrato de entrada/salida del endpoint de ingestion de datasets.
public sealed record CandleDto(long Timestamp, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);

public sealed record IngestarDatasetRequestDto(string Nombre, IReadOnlyList<CandleDto> Velas);

public sealed record IngestarDatasetResponseDto(string DatasetHash);

public sealed record CatalogoDatasetEntradaDto(string Nombre, string Hash);
