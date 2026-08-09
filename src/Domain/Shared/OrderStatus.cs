namespace TD_Project.Domain.Shared;

// spec: RN-01 (transiciones terminales), RN-06 (Stop-Limit muta internamente sin abandonar Pending)
public enum OrderStatus
{
    Pending,
    Executed,
    Cancelled,
    Rejected
}
