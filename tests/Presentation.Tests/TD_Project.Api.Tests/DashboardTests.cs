using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TD_Project.Api.Tests;

public class DashboardTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DashboardTests(WebApplicationFactory<Program> factory) => _factory = factory;

    // spec: RNF-13 — la API sirve el dashboard estatico (wwwroot/index.html) en la raiz
    [Fact]
    public async Task IndexHtmlSeSirveCorrectamente()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var html = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("TD_Project", html);
    }

    // spec: RNF-08 — el dashboard consume el mismo endpoint que ya expone la API
    [Fact]
    public async Task DashboardPuedeConsumirEndpointRun()
    {
        var client = _factory.CreateClient();

        var respuestaIndex = await client.GetAsync("/index.html");
        var respuestaAppJs = await client.GetAsync("/app.js");
        var respuestaRun = await client.PostAsync("/api/backtest/run", null);

        Assert.Equal(HttpStatusCode.OK, respuestaIndex.StatusCode);
        Assert.Equal(HttpStatusCode.OK, respuestaAppJs.StatusCode);
        Assert.Equal(HttpStatusCode.OK, respuestaRun.StatusCode);
    }

    // spec: RNF-08 — app.js referencia unicamente los nombres de campo reales del contrato
    // (camelCase, forma de serializacion por defecto de Minimal API), sin inventar campos
    [Fact]
    public async Task AppJsReferenciaLosCamposRealesDelContrato()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.GetAsync("/app.js");
        var js = await respuesta.Content.ReadAsStringAsync();

        Assert.Contains("dto.estado", js);
        Assert.Contains("dto.metrics.equityFinal", js);
        Assert.Contains("dto.equityCurve", js);
        Assert.Contains("dto.trades", js);
        Assert.Contains("dto.branchResolutions", js);
    }

    // spec: RNF-12 — el dashboard no trae dependencias frontend externas (sin npm/CDN/framework);
    // wwwroot solo contiene HTML/JS/CSS puro
    [Fact]
    public void NoExistenDependenciasFrontendExternas()
    {
        var wwwroot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..",
            "src", "Presentation", "TD_Project.Api", "wwwroot");

        Assert.True(Directory.Exists(wwwroot), $"wwwroot no encontrado en {Path.GetFullPath(wwwroot)}");
        Assert.False(Directory.Exists(Path.Combine(wwwroot, "node_modules")));
        var archivos = Directory.GetFiles(wwwroot, "*", SearchOption.AllDirectories);
        Assert.All(archivos, f => Assert.True(
            f.EndsWith(".html") || f.EndsWith(".js") || f.EndsWith(".css"),
            $"Archivo inesperado en wwwroot: {f}"));
    }
}
