using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Broker;

// spec: RN-12 Fase 1 — CashDisponiblePrevio = CashTotal - compromisos vigentes.
// Se aprueba si CashDisponiblePrevio >= reserva de la orden evaluada.
public static class ValidadorCapacidad
{
    public static bool Validar(PortfolioState portfolio, OrderRequest request, decimal closeActual, decimal tasaMargen, decimal compromisosVigentes)
    {
        var reserva = CalculadoraReservaPreventiva.Calcular(request, closeActual, tasaMargen);
        var cashDisponiblePrevio = portfolio.Cash - compromisosVigentes;
        return cashDisponiblePrevio >= reserva;
    }
}
