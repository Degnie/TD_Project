using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Broker;

// spec: RN-12 — reserva preventiva por tipo de orden
public static class CalculadoraReservaPreventiva
{
    public static decimal Calcular(OrderRequest request, decimal closeActual, decimal tasaMargen)
    {
        var precioBase = request.Type switch
        {
            OrderType.Market => closeActual,
            OrderType.Limit => request.PrecioLimite!.Value,
            OrderType.Stop => request.PrecioStop!.Value,
            OrderType.StopLimit => Math.Max(request.PrecioStop!.Value, request.PrecioLimite!.Value),
            _ => throw new InvalidOperationException($"Tipo de orden no soportado: {request.Type}")
        };

        return precioBase * request.Cantidad * tasaMargen;
    }
}
