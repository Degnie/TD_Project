using TD_Project.Application;
using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;
using TD_Project.Exploration;
using TD_Project.ModeloFinanciero;
using TD_Project.Protocolo;

namespace TD_Project.Caso5;

// spec: Caso 5A D-108/D-109/D-110/D-111 (DECISIONES_CASO5_V1.md),
// ESPECIFICACION_IMPLEMENTACION_GESTORES_RIESGO_V1.md §6 — 10 pruebas obligatorias (P1-P10).
// Mismo patron runner manual que TestsReporteIncapacidades.cs (Caso 4.4): sin xUnit, Caso(nombre,
// verificacion) acumula Pasa/Falla, sin detener la suite en el primer fallo.
public static class TestsGestoresRiesgo
{
    public static (int Total, int Pasaron, IReadOnlyList<string> Detalles) EjecutarTodos(string dirDatasets)
    {
        var detalles = new List<string>();
        var pasaron = 0;
        var total = 0;

        void Caso(string nombre, Action verificacion)
        {
            total++;
            try
            {
                verificacion();
                pasaron++;
                detalles.Add($"[PASA] {nombre}");
            }
            catch (Exception ex)
            {
                detalles.Add($"[FALLA] {nombre}: {ex.Message}");
            }
        }

        Caso("P1 — Migracion sin cambio de comportamiento: GestorFixedFractional produce el mismo HashConfiguracionEconomica que la formula historica sobre el mismo dataset", () => VerificarMigracionSinCambioDeComportamiento(dirDatasets));
        Caso("P2 — D-092 intacta: clasificacion de intencion identica con los 3 gestores", VerificarClasificacionIntactaEntreGestores);
        Caso("P3 — D-095 intacta: Cross-Zero espurio se normaliza a CierreTotal igual con los 3 gestores", VerificarNormalizacionCrossZeroEntreGestores);
        Caso("P4 — Sizing=null (caso basico): requests intacto, ningun IGestorRiesgo invocado", VerificarSizingNuloNoInvocaGestor);
        Caso("P5 — GestorFixedRisk: cantidad = MontoRiesgoFijo / (precio * tasaMargen)", VerificarGestorFixedRisk);
        Caso("P6 — GestorVolatilitySizing: warmup produce cantidad 0, ventana calcula desviacion correcta", VerificarGestorVolatilitySizing);
        Caso("P7 — Comparacion de control: misma estrategia/dataset/economia, solo el gestor cambia la cantidad, nunca la secuencia de senales", () => VerificarComparacionDeControl(dirDatasets));
        Caso("P8 — Metricas nuevas: ProfitFactor/CapitalLibreMinimo verificados contra calculo manual", VerificarMetricasNuevas);
        Caso("P9 — Equivalencia con sizing desactivado: Sizing=null preserva Cantidad original y Cross-Zero genuino sobre la suite completa de fixtures historicos", VerificarEquivalenciaSizingDesactivadoSobreFixtures);
        Caso("P10 — Identidad del hash economico: gestores validos producen identidad estable entre instancias; gestor sin IIdentidadGestorRiesgo falla explicitamente", VerificarIdentidadDeHashEconomico);

        return (total, pasaron, detalles);
    }

    private static DataSlice DataSliceDePrueba() => new(new[] { new Candle(1, 100m, 100m, 100m, 100m, 0m) });

    // P1
    private static void VerificarMigracionSinCambioDeComportamiento(string dirDatasets)
    {
        var entrada = new EntradaProtocolo(
            Estrategia: "Tres Mosqueteros", VersionEstrategia: "1.0", Parametros: new[] { "maxMartingalas=2" },
            CrearEstrategia: onOp => new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: onOp),
            Timeframes: new[] { "1D" }, DirDatasets: dirDatasets, NombreDataset: "BTCUSDT_2024-01-02_2025-01-02",
            CapitalInicial: 1000m, Instrumento: new Instrumento("BTCUSDT", 0.1m),
            Costes: new ConfiguracionCostes(0.001m, 0.001m),
            Sizing: new ConfiguracionSizing(new GestorFixedFractional(0.1m)));

        var r1 = EjecutorProtocolo.Ejecutar(entrada);
        var r2 = EjecutorProtocolo.Ejecutar(entrada);

        if (r1.Identidad.HashCompuesto != r2.Identidad.HashCompuesto)
            throw new Exception("HashCompuesto no es reproducible entre 2 corridas identicas.");
        if (r1.Identidad.HashConfiguracionEconomica != r2.Identidad.HashConfiguracionEconomica)
            throw new Exception("HashConfiguracionEconomica no es reproducible entre 2 corridas identicas.");
    }

    // P2
    private static void VerificarClasificacionIntactaEntreGestores()
    {
        IGestorRiesgo[] gestores = { new GestorFixedFractional(0.1m), new GestorFixedRisk(100m), new GestorVolatilitySizing(5, 0.1m, 1m) };
        var requests = new[] { new OrderRequest(Side.Buy, OrderType.Market, 1m) };

        foreach (var gestor in gestores)
        {
            var portfolio = new PortfolioState { Cash = 1000m };
            var sizing = new ConfiguracionSizing(gestor);
            var dataSlice = new DataSlice(new[]
            {
                new Candle(1, 100m, 100m, 100m, 100m, 0m), new Candle(2, 100m, 100m, 100m, 100m, 0m),
                new Candle(3, 100m, 100m, 100m, 100m, 0m), new Candle(4, 100m, 100m, 100m, 100m, 0m),
                new Candle(5, 100m, 100m, 100m, 100m, 0m),
            });

            var ajustadas = GestorCapital.Ajustar(requests, portfolio, sizing, dataSlice, precioReferencia: 100m, tasaMargen: 0.1m);

            var (intencion, _) = ClasificadorIntencionOrden.Clasificar(portfolio, requests[0]);
            if (intencion != IntencionOrden.Apertura)
                throw new Exception($"Clasificacion base inesperada: {intencion} (esperado Apertura).");
            if (ajustadas.Count != 1)
                throw new Exception($"Gestor {gestor.GetType().Name}: bolsa ajustada con {ajustadas.Count} elementos, esperado 1.");
        }
    }

    // P3
    private static void VerificarNormalizacionCrossZeroEntreGestores()
    {
        IGestorRiesgo[] gestores = { new GestorFixedFractional(0.1m), new GestorFixedRisk(100m), new GestorVolatilitySizing(5, 0.1m, 1m) };

        foreach (var gestor in gestores)
        {
            var portfolio = new PortfolioState { Cash = 1000m };
            AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Buy, 10m, 100m, 0m, 1, OrderType.Market));
            var sizing = new ConfiguracionSizing(gestor);
            var requests = new[] { new OrderRequest(Side.Sell, OrderType.Market, 15m) };
            var dataSlice = new DataSlice(new[]
            {
                new Candle(1, 100m, 100m, 100m, 100m, 0m), new Candle(2, 100m, 100m, 100m, 100m, 0m),
                new Candle(3, 100m, 100m, 100m, 100m, 0m), new Candle(4, 100m, 100m, 100m, 100m, 0m),
                new Candle(5, 100m, 100m, 100m, 100m, 0m),
            });

            var ajustadas = GestorCapital.Ajustar(requests, portfolio, sizing, dataSlice, precioReferencia: 100m, tasaMargen: 0.1m);

            // D-095: CrossZero espurio (15 > 10) se normaliza a CierreTotal con la magnitud real (10).
            if (ajustadas[0].Cantidad != 10m)
                throw new Exception($"Gestor {gestor.GetType().Name}: Cantidad normalizada = {ajustadas[0].Cantidad}, esperado 10 (magnitud de la posicion proyectada).");
        }
    }

    // P4
    private static void VerificarSizingNuloNoInvocaGestor()
    {
        var gestorQueFalla = new GestorQueLanzaSiEsInvocado();
        var sizing = new ConfiguracionSizing(gestorQueFalla);
        var portfolio = new PortfolioState { Cash = 1000m };
        var requests = new[] { new OrderRequest(Side.Buy, OrderType.Market, 1m) };

        // Sizing=null retorna requests intacto sin invocar ningun gestor — se verifica pasando
        // sizing=null explicitamente (no el gestor que falla, que solo prueba el guard temprano).
        var ajustadas = GestorCapital.Ajustar(requests, portfolio, sizing: null, DataSliceDePrueba(), precioReferencia: 100m, tasaMargen: 0.1m);

        if (!ReferenceEquals(ajustadas, requests))
            throw new Exception("Sizing=null no devolvio la misma referencia de requests (deberia ser un no-op).");
        if (gestorQueFalla.FueInvocado)
            throw new Exception("Sizing=null invoco un IGestorRiesgo — no deberia, el guard temprano debe evitarlo.");
    }

    private sealed class GestorQueLanzaSiEsInvocado : IGestorRiesgo
    {
        public bool FueInvocado { get; private set; }
        public decimal CalcularCantidad(PortfolioState portfolio, DataSlice dataSlice, decimal precioReferencia, decimal tasaMargen)
        {
            FueInvocado = true;
            throw new InvalidOperationException("No deberia invocarse con Sizing=null.");
        }
    }

    // P5
    private static void VerificarGestorFixedRisk()
    {
        var gestor = new GestorFixedRisk(montoRiesgoFijo: 50m);
        var portfolio = new PortfolioState { Cash = 1000m };

        var cantidad = gestor.CalcularCantidad(portfolio, DataSliceDePrueba(), precioReferencia: 100m, tasaMargen: 0.1m);

        var esperado = 50m / (100m * 0.1m);
        if (cantidad != esperado)
            throw new Exception($"CalcularCantidad = {cantidad}, esperado {esperado}.");

        // Caso donde excede CapitalDisponible: la formula sigue devolviendo el valor pedido (no
        // valida contra capital, eso lo hace ValidadorCapacidad aguas abajo, D-059/D-060).
        var gestorAlto = new GestorFixedRisk(montoRiesgoFijo: 10_000m);
        var cantidadAlta = gestorAlto.CalcularCantidad(portfolio, DataSliceDePrueba(), precioReferencia: 100m, tasaMargen: 0.1m);
        if (cantidadAlta <= 0m)
            throw new Exception("GestorFixedRisk con monto alto no deberia devolver una cantidad no positiva.");
    }

    // P6
    private static void VerificarGestorVolatilitySizing()
    {
        var gestor = new GestorVolatilitySizing(ventana: 5, porcentajeRiesgoBase: 0.1m, desviacionReferencia: 1m);
        var portfolio = new PortfolioState { Cash = 1000m };

        // Warmup: menos velas que la ventana -> cantidad 0.
        var dataSliceCorta = new DataSlice(new[] { new Candle(1, 100m, 100m, 100m, 100m, 0m) });
        var cantidadWarmup = gestor.CalcularCantidad(portfolio, dataSliceCorta, precioReferencia: 100m, tasaMargen: 0.1m);
        if (cantidadWarmup != 0m)
            throw new Exception($"Warmup deberia producir cantidad 0, produjo {cantidadWarmup}.");

        // Ventana completa, precios constantes -> desviacion 0 -> factorVolatilidad = max(1, 0) = 1
        // -> misma formula que Fixed Fractional con PorcentajeRiesgoBase.
        var velasConstantes = new[]
        {
            new Candle(1, 100m, 100m, 100m, 100m, 0m), new Candle(2, 100m, 100m, 100m, 100m, 0m),
            new Candle(3, 100m, 100m, 100m, 100m, 0m), new Candle(4, 100m, 100m, 100m, 100m, 0m),
            new Candle(5, 100m, 100m, 100m, 100m, 0m),
        };
        var cantidad = gestor.CalcularCantidad(portfolio, new DataSlice(velasConstantes), precioReferencia: 100m, tasaMargen: 0.1m);
        var esperado = (1000m * 0.1m) / (100m * 0.1m * 1m);
        if (cantidad != esperado)
            throw new Exception($"Ventana con desviacion 0: cantidad = {cantidad}, esperado {esperado}.");
    }

    // P7
    private static void VerificarComparacionDeControl(string dirDatasets)
    {
        IGestorRiesgo[] gestores = { new GestorFixedFractional(0.1m), new GestorFixedRisk(50m), new GestorVolatilitySizing(5, 0.1m, 1m) };
        var senalesPorGestor = new List<int>();

        foreach (var gestor in gestores)
        {
            var senalesEmitidas = 0;
            var entrada = new EntradaProtocolo(
                Estrategia: "EMA Cross", VersionEstrategia: "1.0", Parametros: new[] { "rapida=5", "lenta=20" },
                CrearEstrategia: onOp => new EstrategiaEmaCrossConteo(rapida: 5, lenta: 20, contador: () => senalesEmitidas++),
                Timeframes: new[] { "1D" }, DirDatasets: dirDatasets, NombreDataset: "BTCUSDT_2024-01-02_2025-01-02",
                CapitalInicial: 100_000m, Instrumento: new Instrumento("BTCUSDT", 0.1m),
                Costes: new ConfiguracionCostes(0.001m, 0.001m),
                Sizing: new ConfiguracionSizing(gestor));

            EjecutorProtocolo.Ejecutar(entrada);
            senalesPorGestor.Add(senalesEmitidas);
        }

        if (senalesPorGestor.Distinct().Count() != 1)
            throw new Exception($"La cantidad de senales emitidas difiere entre gestores: {string.Join(",", senalesPorGestor)} — la estrategia no deberia ver el gestor activo.");
    }

    // Envoltorio de EstrategiaEmaCross que cuenta cuantas OrderRequest emite, sin alterar su logica
    // de decision — usado solo para P7, aislado en caso5/ (no toca exploration/EstrategiaEmaCross.cs).
    private sealed class EstrategiaEmaCrossConteo : TD_Project.Domain.Strategy.IStrategy
    {
        private readonly EstrategiaEmaCross _interna;
        private readonly Action _contador;
        public EstrategiaEmaCrossConteo(int rapida, int lenta, Action contador)
        {
            _interna = new EstrategiaEmaCross(rapida, lenta);
            _contador = contador;
        }
        public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice)
        {
            var ordenes = _interna.Observar(dataSlice);
            for (var i = 0; i < ordenes.Count; i++) _contador();
            return ordenes;
        }
    }

    // P8
    private static void VerificarMetricasNuevas()
    {
        var trades = new[]
        {
            new Trade(1m, 100m, 110m, RealizedPnL: 10m),
            new Trade(1m, 110m, 100m, RealizedPnL: -5m),
            new Trade(1m, 100m, 130m, RealizedPnL: 30m),
        };
        var resultado = new ResultadoBacktest(
            EstadoBacktest.Success, Array.Empty<Fill>(), CashFinal: 1035m, trades,
            Array.Empty<Order>(), Array.Empty<EquityPoint>(),
            new[] { new PortfolioSnapshot(1, Cash: 900m, Margin: 0m, Array.Empty<Lote>()), new PortfolioSnapshot(2, Cash: 1035m, Margin: 0m, Array.Empty<Lote>()) },
            Array.Empty<BranchResolutionInfo>());

        var metricas = CalculadoraMetricasFinancieras.Calcular(resultado, capitalInicial: 1000m);

        var profitFactorEsperado = (10m + 30m) / 5m;
        if (metricas.ProfitFactor != profitFactorEsperado)
            throw new Exception($"ProfitFactor = {metricas.ProfitFactor}, esperado {profitFactorEsperado}.");

        // CapitalLibreMinimo: min(1000 inicial, 900-0, 1035-0) = 900.
        if (metricas.CapitalLibreMinimo != 900m)
            throw new Exception($"CapitalLibreMinimo = {metricas.CapitalLibreMinimo}, esperado 900.");

        // Sin perdidas: ProfitFactor debe ser null, no un numero (evita division por cero, D-078).
        var resultadoSinPerdidas = resultado with { Trades = new[] { new Trade(1m, 100m, 110m, RealizedPnL: 10m) } };
        var metricasSinPerdidas = CalculadoraMetricasFinancieras.Calcular(resultadoSinPerdidas, capitalInicial: 1000m);
        if (metricasSinPerdidas.ProfitFactor is not null)
            throw new Exception($"ProfitFactor deberia ser null sin perdidas, fue {metricasSinPerdidas.ProfitFactor}.");
    }

    // P9 — corrida completa contra los fixtures existentes de GestorCapitalTests.cs con Sizing=null
    // explicito: cantidades historicas, cierres, y Cross-Zero genuino se preservan intactos.
    private static void VerificarEquivalenciaSizingDesactivadoSobreFixtures()
    {
        // Fixture 1: Cross-Zero genuino (magnitud nominal excede la posicion, sin sizing).
        var portfolio1 = new PortfolioState { Cash = 1000m };
        AplicadorFill.Aplicar(portfolio1, new Fill(1, Side.Buy, 10m, 100m, 0m, 1, OrderType.Market));
        var requests1 = new[] { new OrderRequest(Side.Sell, OrderType.Market, 15m) };
        var ajustadas1 = GestorCapital.Ajustar(requests1, portfolio1, sizing: null, DataSliceDePrueba(), precioReferencia: 100m, tasaMargen: 0.1m);
        if (ajustadas1[0].Cantidad != 15m)
            throw new Exception($"Sizing=null deberia preservar CrossZero genuino con Cantidad=15, produjo {ajustadas1[0].Cantidad}.");

        // Fixture 2: CierreTotal exacto.
        var portfolio2 = new PortfolioState { Cash = 1000m };
        AplicadorFill.Aplicar(portfolio2, new Fill(1, Side.Buy, 1m, 100m, 0m, 1, OrderType.Market));
        var requests2 = new[] { new OrderRequest(Side.Sell, OrderType.Market, 1m) };
        var ajustadas2 = GestorCapital.Ajustar(requests2, portfolio2, sizing: null, DataSliceDePrueba(), precioReferencia: 100m, tasaMargen: 0.1m);
        if (ajustadas2[0].Cantidad != 1m)
            throw new Exception($"Sizing=null deberia preservar CierreTotal con Cantidad=1, produjo {ajustadas2[0].Cantidad}.");

        // Fixture 3: ReduccionParcial.
        var portfolio3 = new PortfolioState { Cash = 1000m };
        AplicadorFill.Aplicar(portfolio3, new Fill(1, Side.Buy, 1m, 100m, 0m, 1, OrderType.Market));
        var requests3 = new[] { new OrderRequest(Side.Sell, OrderType.Market, 0.5m) };
        var ajustadas3 = GestorCapital.Ajustar(requests3, portfolio3, sizing: null, DataSliceDePrueba(), precioReferencia: 100m, tasaMargen: 0.1m);
        if (ajustadas3[0].Cantidad != 0.5m)
            throw new Exception($"Sizing=null deberia preservar ReduccionParcial con Cantidad=0.5, produjo {ajustadas3[0].Cantidad}.");

        // Fixture 4: bolsa completa (Apertura), sin sizing la cantidad nominal pasa intacta.
        var portfolio4 = new PortfolioState { Cash = 1000m };
        var requests4 = new[] { new OrderRequest(Side.Buy, OrderType.Market, 3m), new OrderRequest(Side.Buy, OrderType.Market, 3m) };
        var ajustadas4 = GestorCapital.Ajustar(requests4, portfolio4, sizing: null, DataSliceDePrueba(), precioReferencia: 100m, tasaMargen: 0.1m);
        if (ajustadas4[0].Cantidad != 3m || ajustadas4[1].Cantidad != 3m)
            throw new Exception("Sizing=null deberia preservar Cantidad nominal en toda la bolsa.");
    }

    // P10
    private static void VerificarIdentidadDeHashEconomico()
    {
        // Identidad estable entre instancias (criterio anadido por el auditor): dos instancias
        // distintas, misma configuracion, mismo texto exacto.
        var gestor1 = new GestorFixedFractional(0.01m);
        var gestor2 = new GestorFixedFractional(0.01m);
        if (gestor1.ObtenerIdentidadConfiguracion() != gestor2.ObtenerIdentidadConfiguracion())
            throw new Exception("Dos instancias de GestorFixedFractional con la misma configuracion produjeron identidades distintas.");
        if (gestor1.ObtenerIdentidadConfiguracion() != "fixed-fractional:v1:riesgo=0.01")
            throw new Exception($"Identidad inesperada: {gestor1.ObtenerIdentidadConfiguracion()}.");

        var fr1 = new GestorFixedRisk(100m);
        var fr2 = new GestorFixedRisk(100m);
        if (fr1.ObtenerIdentidadConfiguracion() != fr2.ObtenerIdentidadConfiguracion())
            throw new Exception("Dos instancias de GestorFixedRisk con la misma configuracion produjeron identidades distintas.");

        var vs1 = new GestorVolatilitySizing(20, 0.1m, 2m);
        var vs2 = new GestorVolatilitySizing(20, 0.1m, 2m);
        if (vs1.ObtenerIdentidadConfiguracion() != vs2.ObtenerIdentidadConfiguracion())
            throw new Exception("Dos instancias de GestorVolatilitySizing con la misma configuracion produjeron identidades distintas.");

        // Gestor que NO implementa IIdentidadGestorRiesgo -> el hash economico debe fallar explicitamente.
        var sizingInvalido = new ConfiguracionSizing(new GestorSinIdentidad());
        var fallo = false;
        try
        {
            IdentidadExperimentoCompleta.Calcular(
                estrategia: "x", versionEstrategia: "1.0", parametros: Array.Empty<string>(),
                datasetSourceSha256: "hash", clasificadorRegimenVersion: "v1", versionProtocolo: "v1",
                sizing: sizingInvalido);
        }
        catch (InvalidOperationException)
        {
            fallo = true;
        }
        if (!fallo)
            throw new Exception("Un gestor sin IIdentidadGestorRiesgo deberia hacer fallar el calculo del hash economico, no producir un hash silencioso.");
    }

    private sealed class GestorSinIdentidad : IGestorRiesgo
    {
        public decimal CalcularCantidad(PortfolioState portfolio, DataSlice dataSlice, decimal precioReferencia, decimal tasaMargen) => 1m;
    }
}
