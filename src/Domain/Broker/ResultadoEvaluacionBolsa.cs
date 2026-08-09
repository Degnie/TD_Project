using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Broker;

// spec: RN-14
public sealed record ResultadoEvaluacionBolsa(bool Aprobada, IReadOnlyList<OrderRequest> Rechazadas);
