using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Matching;

// spec: RN-01, RN-06
public static class OrdenTransiciones
{
    public static void Ejecutar(Order orden)
    {
        RequierePending(orden);
        orden.Status = OrderStatus.Executed;
    }

    public static void Cancelar(Order orden)
    {
        RequierePending(orden);
        orden.Status = OrderStatus.Cancelled;
    }

    public static void Rechazar(Order orden)
    {
        RequierePending(orden);
        orden.Status = OrderStatus.Rejected;
    }

    // spec: RN-06 — Stop-Limit muta a Limit sin abandonar Pending
    public static void Disparar(Order orden, decimal precioLimite)
    {
        RequierePending(orden);
        if (orden.Type != OrderType.StopLimit)
            throw new InvalidOperationException("Solo una orden Stop-Limit puede dispararse.");

        orden.Type = OrderType.Limit;
        orden.PrecioLimite = precioLimite;
        orden.PrecioStop = null;
    }

    private static void RequierePending(Order orden)
    {
        if (orden.Status != OrderStatus.Pending)
            throw new InvalidOperationException($"No se puede transicionar una orden en estado {orden.Status}.");
    }
}
