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

    // spec: RNF-16 — actualizacion de prueba por reemplazo de flujo UI aprobado (caso16): el
    // dashboard ya no gira en torno a un unico boton contra /api/backtest/run (demo fijo); ahora
    // sirve el flujo de dataset -> estrategia -> ejecucion -> profundizacion. El endpoint demo
    // permanece intacto en el backend (se verifica sin cambios), pero ya no es lo que el
    // dashboard invoca por defecto — se verifica que /app.js consume los endpoints reales de
    // SPEC 7.0 en su lugar.
    [Fact]
    public async Task DashboardSirveElFlujoDeAnalisisHistorico()
    {
        var client = _factory.CreateClient();

        var respuestaIndex = await client.GetAsync("/index.html");
        var respuestaAppJs = await client.GetAsync("/app.js");
        var respuestaBacktestRunIntacto = await client.PostAsync("/api/backtest/run", null);

        Assert.Equal(HttpStatusCode.OK, respuestaIndex.StatusCode);
        Assert.Equal(HttpStatusCode.OK, respuestaAppJs.StatusCode);
        // spec: RNF-16 — /api/backtest/run no se modifica (caso16 DECISIONES S1): sigue
        // respondiendo, aunque el dashboard ya no lo invoque desde ningun boton.
        Assert.Equal(HttpStatusCode.OK, respuestaBacktestRunIntacto.StatusCode);

        var js = await respuestaAppJs.Content.ReadAsStringAsync();
        Assert.Contains("/api/datasets", js);
        Assert.Contains("/api/strategies/dsl/run", js);
        Assert.Contains("/api/capital-managers/recommend", js);
    }

    // spec: RNF-16 — actualizacion de prueba por reemplazo de flujo UI aprobado (caso16): app.js
    // referencia unicamente los nombres de campo reales del contrato (camelCase, forma de
    // serializacion por defecto de Minimal API) del nuevo flujo, sin inventar campos. Sustituye
    // la verificacion anterior (campos del flujo /api/backtest/run) por los campos de Nivel 1/2
    // que caso16 exige mostrar: Explicacion, Incapacidades, Exposicion, ReporteRegimen.
    [Fact]
    public async Task AppJsReferenciaLosCamposRealesDelContratoDelNuevoFlujo()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.GetAsync("/app.js");
        var js = await respuesta.Content.ReadAsStringAsync();

        Assert.Contains("dto.explicacion", js);
        Assert.Contains("dto.reporteRegimen", js);
        Assert.Contains("dto.exposicion", js);
        Assert.Contains("dto.incapacidades", js);
        Assert.Contains("datasetHash", js);
        Assert.Contains("estrategiaDslJson", js);
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
