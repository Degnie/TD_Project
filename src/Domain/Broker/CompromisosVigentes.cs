namespace TD_Project.Domain.Broker;

// spec: RN-12 — ciclo vital de la reserva preventiva: Fill/Cancelled/OCO Cancelled la liberan integramente
public sealed class CompromisosVigentes
{
    private readonly Dictionary<long, decimal> _reservasPorSecuencia = new();

    public decimal Total => _reservasPorSecuencia.Values.Sum();

    public void Reservar(long secuenciaCausal, decimal monto) =>
        _reservasPorSecuencia[secuenciaCausal] = monto;

    public void Liberar(long secuenciaCausal) =>
        _reservasPorSecuencia.Remove(secuenciaCausal);
}
