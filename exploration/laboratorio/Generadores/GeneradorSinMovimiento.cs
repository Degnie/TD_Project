using TD_Project.Domain.Shared;

namespace TD_Project.Laboratorio.Generadores;

// Velas planas (Open == High == Low == Close) durante todo el dataset: caso degenerado de
// "mercado sin movimiento". Valida que el motor no crashea ni produce division por cero /
// NaN cuando no hay ningun rango que cruzar, y que RN-11 degenera correctamente a
// EquityA == EquityB (sin ambiguedad posible sin rango).
public static class GeneradorSinMovimiento
{
    public static IReadOnlyList<Candle> Generar(int velas, decimal precioFijo)
    {
        var resultado = new List<Candle>(velas);
        for (var i = 0; i < velas; i++)
            resultado.Add(new Candle(i + 1, precioFijo, precioFijo, precioFijo, precioFijo, 500m));
        return resultado;
    }
}
