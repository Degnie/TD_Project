using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Broker;

// spec: glosario "Secuencia Causal" — entero, inmutable, estrictamente monotono
// creciente y unico dentro del Experiment, asignado en el instante de registro.
public sealed class RegistradorOrdenes
{
    private long _siguienteSecuencia = 1;

    public Order Registrar(OrderRequest request)
    {
        var orden = new Order
        {
            SecuenciaCausal = _siguienteSecuencia,
            Side = request.Side,
            Type = request.Type,
            Cantidad = request.Cantidad,
            PrecioLimite = request.PrecioLimite,
            PrecioStop = request.PrecioStop
        };
        _siguienteSecuencia++;
        return orden;
    }
}
