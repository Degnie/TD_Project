namespace TD_Project.Domain.Shared;

// spec: glosario "OCO (One-Cancels-Other)"
public sealed record OcoGroup(IReadOnlyList<Order> Ramas);
