using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Broker;

// spec: RN-14 — toda la bolsa de Requests del ciclo N para un activo se evalua junta;
// si contiene Buy y Sell, TODA la bolsa se rechaza atomicamente.
// spec: RN-08 — la direccion pertenece exclusivamente a Side; Cantidad es magnitud
// estrictamente positiva. Una Cantidad <= 0 se rechaza aqui mismo (misma frontera de
// validacion), antes de que el motor pueda interpretarla de forma ambigua.
public static class ValidadorBolsaRequests
{
    public static ResultadoEvaluacionBolsa Evaluar(IReadOnlyList<OrderRequest> bolsa)
    {
        var tieneContradiccion = bolsa.Any(r => r.Side == Side.Buy) && bolsa.Any(r => r.Side == Side.Sell);
        var tieneCantidadInvalida = bolsa.Any(r => r.Cantidad <= 0m);

        return tieneContradiccion || tieneCantidadInvalida
            ? new ResultadoEvaluacionBolsa(Aprobada: false, Rechazadas: bolsa)
            : new ResultadoEvaluacionBolsa(Aprobada: true, Rechazadas: Array.Empty<OrderRequest>());
    }
}
