using TD_Project.Domain.Portfolio;

namespace TD_Project.Application;

// spec: glosario "Trade" — Domain emite el evento puntual por Fill (RealizedPnL de ese Fill;
// el Trade que devuelve solo referencia el Fill de cierre, con la cantidad/precio de apertura
// del ULTIMO lote FIFO, no del ciclo completo). Application observa la Position antes/despues de
// cada Fill para fijar la apertura real del ciclo (el primer Fill que rompe cero) y acumula el
// RealizedPnL de las reducciones parciales previas hasta consolidar el cierre.
internal sealed class AcumuladorTrade
{
    private decimal _realizedPnLAcumulado;
    private decimal _cantidadInicialCiclo;
    private decimal _precioAperturaCiclo;
    private long _timestampAperturaCiclo;

    // spec: RN-19 — timestamp de apertura como metadata de ejecucion, mismo criterio que
    // _precioAperturaCiclo (se fija en el Fill que rompe cero, incluido el caso Cross-Zero).
    public void AntesDeAplicar(decimal posicionActual, decimal precioFill, long timestampFill)
    {
        if (posicionActual == 0m)
        {
            _cantidadInicialCiclo = 0m;
            _precioAperturaCiclo = precioFill;
            _timestampAperturaCiclo = timestampFill;
        }
        else if (_cantidadInicialCiclo == 0m)
        {
            _cantidadInicialCiclo = Math.Abs(posicionActual);
        }
    }

    // spec: RN-10 — un Cross-Zero cierra el ciclo viejo y abre uno nuevo en el MISMO Fill, sin
    // pasar por Position == 0 en ningun punto observable antes del Fill. AntesDeAplicar no puede
    // detectar esta apertura (solo ve la posicion previa, nunca cero). DespuesDeAplicar la
    // detecta con la posicion resultante: si el Fill cerro un Trade (TradeCerrado) y la posicion
    // ya no es cero, ese mismo Fill es la apertura real del ciclo siguiente.
    public void DespuesDeAplicar(ResultadoAplicacionFill resultado, decimal posicionResultante, decimal precioFill, long timestampFill)
    {
        if (resultado.TradeCerrado is not null && posicionResultante != 0m)
        {
            _cantidadInicialCiclo = Math.Abs(posicionResultante);
            _precioAperturaCiclo = precioFill;
            _timestampAperturaCiclo = timestampFill;
        }
    }

    public void Registrar(ResultadoAplicacionFill resultado) =>
        _realizedPnLAcumulado += resultado.RealizedPnLReconocido;

    public Trade CerrarYExtraer(Trade tradeDelFillDeCierre, long timestampFillDeCierre)
    {
        var tradeConsolidado = tradeDelFillDeCierre with
        {
            CantidadInicial = _cantidadInicialCiclo,
            PrecioApertura = _precioAperturaCiclo,
            RealizedPnL = _realizedPnLAcumulado,
            TimestampApertura = _timestampAperturaCiclo,
            TimestampCierre = timestampFillDeCierre
        };
        _realizedPnLAcumulado = 0m;
        _cantidadInicialCiclo = 0m;
        _timestampAperturaCiclo = 0;
        return tradeConsolidado;
    }
}
