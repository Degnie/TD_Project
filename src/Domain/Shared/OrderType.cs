namespace TD_Project.Domain.Shared;

// spec: RN-12 (formulas de Reserva Preventiva por tipo), RN-06 (ciclo Stop-Limit)
public enum OrderType
{
    Market,
    Limit,
    Stop,
    StopLimit
}
