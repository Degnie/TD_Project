using TD_Project.Domain.Matching;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Domain.Tests.Matching;

public class TransicionesOrdenTests
{
    private static Order NuevaOrdenLimit() => new()
    {
        SecuenciaCausal = 1,
        Side = Side.Buy,
        Type = OrderType.Limit,
        Cantidad = 1m,
        PrecioLimite = 100m
    };

    // spec: RN-01
    [Fact]
    public void UnaOrdenPendingPuedeMutarAExecuted()
    {
        var orden = NuevaOrdenLimit();

        OrdenTransiciones.Ejecutar(orden);

        Assert.Equal(OrderStatus.Executed, orden.Status);
    }

    // spec: RN-01
    [Fact]
    public void UnaOrdenPendingPuedeMutarACancelled()
    {
        var orden = NuevaOrdenLimit();

        OrdenTransiciones.Cancelar(orden);

        Assert.Equal(OrderStatus.Cancelled, orden.Status);
    }

    // spec: RN-01
    [Fact]
    public void UnaOrdenPendingPuedeMutarARejected()
    {
        var orden = NuevaOrdenLimit();

        OrdenTransiciones.Rechazar(orden);

        Assert.Equal(OrderStatus.Rejected, orden.Status);
    }

    // spec: RN-01
    [Theory]
    [InlineData(OrderStatus.Executed)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Rejected)]
    public void UnEstadoTerminalNuncaMutaAOtroEstado(OrderStatus estadoTerminal)
    {
        var orden = NuevaOrdenLimit();
        orden.Status = estadoTerminal;

        Assert.Throws<InvalidOperationException>(() => OrdenTransiciones.Ejecutar(orden));
        Assert.Throws<InvalidOperationException>(() => OrdenTransiciones.Cancelar(orden));
        Assert.Throws<InvalidOperationException>(() => OrdenTransiciones.Rechazar(orden));
        Assert.Equal(estadoTerminal, orden.Status);
    }

    // spec: RN-01, RN-06
    [Fact]
    public void UnaOrdenCondicionalPuedeModificarParametrosSinAbandonarPending()
    {
        var orden = new Order
        {
            SecuenciaCausal = 1,
            Side = Side.Buy,
            Type = OrderType.StopLimit,
            Cantidad = 1m,
            PrecioStop = 100m,
            PrecioLimite = 102m
        };

        OrdenTransiciones.Disparar(orden, precioLimite: 102m);

        Assert.Equal(OrderStatus.Pending, orden.Status);
        Assert.Equal(OrderType.Limit, orden.Type);
    }
}
