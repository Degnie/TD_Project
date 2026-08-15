using System.Text.Json;
using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;

namespace TD_Project.Domain.Strategy.Dsl;

// spec: RN-16 — evaluacion del DSL puramente declarativa, aislada y determinista, solo accede a
// la porcion de mercado visible en DataSlice(N). SMA(periodo) se calcula sobre las 'periodo'
// velas ANTERIORES a N (nunca incluye Close(N)), para no comparar N contra si mismo.
public sealed class InterpreteDsl : IStrategy
{
    private readonly DocumentoDsl _documento;

    private InterpreteDsl(DocumentoDsl documento) => _documento = documento;

    public static InterpreteDsl CargarDesdeJson(string json)
    {
        var validacion = ValidadorDsl.Validar(json);
        if (!validacion.EsValido)
            throw new InvalidOperationException($"DSL invalido: {validacion.Motivo}");

        var documento = JsonSerializer.Deserialize<DocumentoDsl>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("No se pudo interpretar el documento DSL.");
        return new InterpreteDsl(documento);
    }

    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice)
    {
        var periodo = _documento.Condicion.Periodo!.Value;
        if (dataSlice.N < periodo)
            return Array.Empty<OrderRequest>();

        var sma = CalcularSma(dataSlice, periodo);
        var valorCampo = ObtenerCampo(dataSlice.VelaActual, _documento.Condicion.Campo!);

        if (!EvaluarOperador(valorCampo, sma, _documento.Condicion.Operador!))
            return Array.Empty<OrderRequest>();

        var side = _documento.Accion.Side == "Buy" ? Side.Buy : Side.Sell;
        return new[] { new OrderRequest(side, OrderType.Market, 1m) };
    }

    private static decimal CalcularSma(DataSlice dataSlice, int periodo)
    {
        decimal suma = 0m;
        for (var i = dataSlice.N - periodo; i < dataSlice.N; i++)
            suma += dataSlice.VelasHastaN[i].Close;
        return suma / periodo;
    }

    private static decimal ObtenerCampo(Candle vela, string campo) => campo switch
    {
        "Close" => vela.Close,
        "Open" => vela.Open,
        "High" => vela.High,
        "Low" => vela.Low,
        _ => throw new InvalidOperationException($"Campo no soportado: {campo}")
    };

    private static bool EvaluarOperador(decimal izquierda, decimal derecha, string operador) => operador switch
    {
        ">" => izquierda > derecha,
        "<" => izquierda < derecha,
        ">=" => izquierda >= derecha,
        "<=" => izquierda <= derecha,
        _ => throw new InvalidOperationException($"Operador no soportado: {operador}")
    };
}
