using TD_Project.Domain.Portfolio;

namespace TD_Project.Application;

// spec: glosario "Trade" — Domain emite el evento puntual por Fill (RealizedPnL de ese Fill;
// el Trade que devuelve solo referencia el Fill de cierre, con la cantidad/precio de apertura
// del ULTIMO lote FIFO, no del ciclo completo). Application observa la Position antes de cada
// Fill para fijar la apertura real del ciclo (el primer Fill que rompe cero) y acumula el
// RealizedPnL de las reducciones parciales previas hasta consolidar el cierre.
internal sealed class AcumuladorTrade
{
    private decimal _realizedPnLAcumulado;
    private decimal _cantidadInicialCiclo;
    private decimal _precioAperturaCiclo;

    public void AntesDeAplicar(decimal posicionActual, decimal precioFill)
    {
        if (posicionActual == 0m)
        {
            _cantidadInicialCiclo = 0m;
            _precioAperturaCiclo = precioFill;
        }
        else if (_cantidadInicialCiclo == 0m)
        {
            _cantidadInicialCiclo = Math.Abs(posicionActual);
        }
    }

    public void Registrar(ResultadoAplicacionFill resultado) =>
        _realizedPnLAcumulado += resultado.RealizedPnLReconocido;

    public Trade CerrarYExtraer(Trade tradeDelFillDeCierre)
    {
        var tradeConsolidado = tradeDelFillDeCierre with
        {
            CantidadInicial = _cantidadInicialCiclo,
            PrecioApertura = _precioAperturaCiclo,
            RealizedPnL = _realizedPnLAcumulado
        };
        _realizedPnLAcumulado = 0m;
        _cantidadInicialCiclo = 0m;
        return tradeConsolidado;
    }
}
