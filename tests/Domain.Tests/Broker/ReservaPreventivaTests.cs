using TD_Project.Domain.Broker;
using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Domain.Tests.Broker;

public class ReservaPreventivaTests
{
    // spec: RN-12 — Market: Close(N) * Cantidad * TasaMargen
    [Fact]
    public void ElCalculoDeReservaParaMarketUsaElCloseActual()
    {
        var request = new OrderRequest(Side.Buy, OrderType.Market, Cantidad: 10m);
        var closeActual = 100m;
        var tasaMargen = 0.1m;

        var reserva = CalculadoraReservaPreventiva.Calcular(request, closeActual, tasaMargen);

        Assert.Equal(100m * 10m * 0.1m, reserva);
    }

    // spec: RN-12 Fase 1 — se aprueba si CashDisponiblePrevio >= reserva de la orden i
    [Fact]
    public void SeApruebaLaOrdenSiElCashDisponibleCubreLaReserva()
    {
        var portfolio = new PortfolioState { Cash = 1000m };
        var request = new OrderRequest(Side.Buy, OrderType.Market, Cantidad: 5m);
        var closeActual = 100m;
        var tasaMargen = 0.1m;

        var aprobada = ValidadorCapacidad.Validar(portfolio, request, closeActual, tasaMargen, compromisosVigentes: 0m);

        Assert.True(aprobada);
    }

    // spec: CU-15 — falla la validacion preventiva -> OrderRequestRejected
    [Fact]
    public void SeRechazaLaOrdenSiElCashDisponibleNoCubreLaReserva()
    {
        var portfolio = new PortfolioState { Cash = 10m };
        var request = new OrderRequest(Side.Buy, OrderType.Market, Cantidad: 5m);
        var closeActual = 100m;
        var tasaMargen = 0.1m;

        var aprobada = ValidadorCapacidad.Validar(portfolio, request, closeActual, tasaMargen, compromisosVigentes: 0m);

        Assert.False(aprobada);
    }

    // spec: CU-16 — cancelacion en N -> orden Cancelled, reserva liberada integramente
    [Fact]
    public void CancelarUnaOrdenPendingLiberaSuReservaIntegramente()
    {
        var compromisos = new CompromisosVigentes();
        var orden = new Order { SecuenciaCausal = 1, Side = Side.Buy, Type = OrderType.Market, Cantidad = 5m };
        compromisos.Reservar(orden.SecuenciaCausal, monto: 50m);

        compromisos.Liberar(orden.SecuenciaCausal);

        Assert.Equal(0m, compromisos.Total);
    }
}
