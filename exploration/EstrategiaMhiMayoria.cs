using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;

namespace TD_Project.Exploration;

// Estrategia "MHI Mayoría" (fuente: usuario, patrón encontrado en internet).
// Mercado dividido en cuadrantes FIJOS de 5 velas, anclados a la posición absoluta en el
// dataset (N%5), NO ventana deslizante. Al cerrar la vela 5 del cuadrante (N%5==4, 0-indexed)
// se toman las velas 3, 4 y 5 de ESE cuadrante (N-2, N-1, N) y se cuenta el color mayoritario;
// la operación se abre al inicio del cuadrante SIGUIENTE (N+1, la vela 1 del próximo cuadrante),
// vía el desfase RN-13. Una sola evaluación por cuadrante — nunca se recalcula la mayoría en
// cada vela disponible.
//
// Traducción a órdenes reales (fixture de auditoría, no regla de dominio): abrir con el mismo
// lado que la mayoría apostada y cerrar con el lado OPUESTO al de apertura — nunca "Sell fijo".
//
// Desfase N/N+1 (RN-13): Observar es invocado con dataSlice hasta N; cualquier orden devuelta
// se ejecuta contra Velas[N+1]. La señal se calcula en N%5==4 (vela 5 recién cerrada) y ejecuta
// en N+1 (vela 1 del cuadrante siguiente), exactamente lo que exige la regla.
public sealed class EstrategiaMhiMayoria : IStrategy
{
    private readonly int _maxMartingalas;

    // Instrumentacion OPCIONAL exclusiva de analisis (ver EstrategiaTresMosqueteros.cs para el
    // razonamiento completo de por que esto no puede reconstruirse desde fuera con certeza).
    private readonly Action<InfoOperacionResuelta>? _onOperacionResuelta;
    private int _siguienteOperacionId = 1;
    private int _operacionIdActual;
    private long _timestampEntradaActual;

    private enum Fase { Ninguna, EsperandoCierre, EsperandoReapertura }

    private Fase _fase = Fase.Ninguna;
    private Side? _colorApostado;
    private int _martingalasUsadas;

    public EstrategiaMhiMayoria(int maxMartingalas, Action<InfoOperacionResuelta>? onOperacionResuelta = null)
    {
        if (maxMartingalas is < 0 or > 2)
            throw new ArgumentOutOfRangeException(nameof(maxMartingalas), "Casos definidos: 0, 1 o 2 martingalas.");
        _maxMartingalas = maxMartingalas;
        _onOperacionResuelta = onOperacionResuelta;
    }

    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice)
    {
        var velaActual = dataSlice.VelaActual;

        if (_fase == Fase.EsperandoCierre)
        {
            var acerto = ColorDe(velaActual) == _colorApostado;
            var ladoCierre = Opuesto(_colorApostado!.Value);

            if (acerto || _martingalasUsadas >= _maxMartingalas)
            {
                _fase = Fase.Ninguna;
                _onOperacionResuelta?.Invoke(new InfoOperacionResuelta(_operacionIdActual, _martingalasUsadas, acerto, _timestampEntradaActual, velaActual.Timestamp));
                return new[] { new OrderRequest(ladoCierre, OrderType.Market, 1m) };
            }

            _martingalasUsadas++;
            _fase = Fase.EsperandoReapertura;
            return new[] { new OrderRequest(ladoCierre, OrderType.Market, 1m) };
        }

        if (_fase == Fase.EsperandoReapertura)
        {
            _fase = Fase.EsperandoCierre;
            return new[] { new OrderRequest(_colorApostado!.Value, OrderType.Market, 1m) };
        }

        // Sin apuesta en curso: evaluar si ESTA vela (por su posición absoluta N) es la vela 5
        // del cuadrante (cierre del cuadrante, dispara la unica evaluacion de mayoria).
        if (dataSlice.N % 5 != 4)
            return Array.Empty<OrderRequest>();

        if (dataSlice.N < 2)
            return Array.Empty<OrderRequest>(); // no deberia ocurrir con N%5==4, guarda defensiva

        var velasCuadrante = dataSlice.VelasHastaN;
        var tresYCincoDelCuadrante = new[]
        {
            velasCuadrante[dataSlice.N - 2], // vela 3
            velasCuadrante[dataSlice.N - 1], // vela 4
            velasCuadrante[dataSlice.N],     // vela 5
        };
        var colores = tresYCincoDelCuadrante.Select(ColorDe).Where(c => c is not null).Select(c => c!.Value).ToList();

        if (colores.Count < 3)
            return Array.Empty<OrderRequest>(); // alguna doji entre las 3: sin mayoria clara

        var verdes = colores.Count(c => c == Side.Buy);
        var rojas = colores.Count(c => c == Side.Sell);

        if (verdes == rojas)
            return Array.Empty<OrderRequest>(); // defensivo, no deberia ocurrir con 3 velas sin doji

        var colorMayoritario = verdes > rojas ? Side.Buy : Side.Sell;

        _colorApostado = colorMayoritario;
        _martingalasUsadas = 0;
        _fase = Fase.EsperandoCierre;
        _operacionIdActual = _siguienteOperacionId++;
        _timestampEntradaActual = velaActual.Timestamp;
        return new[] { new OrderRequest(colorMayoritario, OrderType.Market, 1m) };
    }

    private static Side? ColorDe(Candle vela) =>
        vela.Close > vela.Open ? Side.Buy
        : vela.Close < vela.Open ? Side.Sell
        : null;

    private static Side Opuesto(Side lado) => lado == Side.Buy ? Side.Sell : Side.Buy;
}
