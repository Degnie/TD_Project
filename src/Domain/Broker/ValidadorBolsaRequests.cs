using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Broker;

// spec: RN-14 — toda la bolsa de Requests del ciclo N para un activo se evalua junta;
// si contiene Buy y Sell, TODA la bolsa se rechaza atomicamente.
public static class ValidadorBolsaRequests
{
    public static ResultadoEvaluacionBolsa Evaluar(IReadOnlyList<OrderRequest> bolsa)
    {
        var tieneContradiccion = bolsa.Any(r => r.Side == Side.Buy) && bolsa.Any(r => r.Side == Side.Sell);

        return tieneContradiccion
            ? new ResultadoEvaluacionBolsa(Aprobada: false, Rechazadas: bolsa)
            : new ResultadoEvaluacionBolsa(Aprobada: true, Rechazadas: Array.Empty<OrderRequest>());
    }
}
