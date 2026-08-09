using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using TD_Project.Contracts;
using Xunit;

namespace TD_Project.Api.Tests;

public class BacktestRunEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BacktestRunEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    // spec: RNF-08 — el endpoint responde 200 con un ResultDto deserializable
    [Fact]
    public async Task PostRunDevuelveOkConResultDto()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsync("/api/backtest/run", null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var dto = await respuesta.Content.ReadFromJsonAsync<ResultDto>();
        Assert.NotNull(dto);
    }

    // spec: RNF-09 — el pipeline completo (Domain -> Application -> Contracts -> HTTP) corre de
    // verdad: Estado Success y al menos un punto de EquityCurve (evidencia minima de ejecucion,
    // independiente de si la estrategia demo produjo Trades)
    [Fact]
    public async Task PostRunEjecutaElBacktestDemoYDevuelveEstadoSuccess()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsync("/api/backtest/run", null);
        var dto = await respuesta.Content.ReadFromJsonAsync<ResultDto>();

        Assert.Equal("Success", dto!.Estado);
        Assert.True(dto.EquityCurve.Count > 0);
    }

    // spec: RNF-08 — el endpoint no tiene contrato de entrada todavia; cualquier body enviado
    // se ignora sin producir error
    [Fact]
    public async Task PostRunIgnoraCualquierBodyEnviado()
    {
        var client = _factory.CreateClient();
        var contenido = new StringContent("{\"foo\":\"bar\"}", Encoding.UTF8, "application/json");

        var respuesta = await client.PostAsync("/api/backtest/run", contenido);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    // spec: RNF-06 — determinismo: sin estado compartido entre requests, dos ejecuciones
    // sucesivas del mismo dataset/estrategia demo producen el mismo resultado
    [Fact]
    public async Task DosLlamadasSucesivasDevuelvenElMismoResultado()
    {
        var client = _factory.CreateClient();

        var primeraRespuesta = await client.PostAsync("/api/backtest/run", null);
        var primerDto = await primeraRespuesta.Content.ReadFromJsonAsync<ResultDto>();
        var segundaRespuesta = await client.PostAsync("/api/backtest/run", null);
        var segundoDto = await segundaRespuesta.Content.ReadFromJsonAsync<ResultDto>();

        Assert.Equal(primerDto!.Metrics.EquityFinal, segundoDto!.Metrics.EquityFinal);
        Assert.Equal(primerDto.Trades.Count, segundoDto.Trades.Count);
    }
}
