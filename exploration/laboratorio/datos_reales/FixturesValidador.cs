namespace TD_Project.DatosReales;

// Los 6 fixtures obligatorios de PLAN_FASE2A.md seccion 7. Corren ANTES de tocar Binance:
// demuestran que el validador detecta cada clase de defecto, no solo que "parece funcionar"
// porque los primeros datos reales vienen limpios.
public static class FixturesValidador
{
    public sealed record CasoPrueba(string Nombre, IReadOnlyList<VelaCruda> Velas, bool EsperaApto, bool EsperaHuecos, bool EsperaErrores);

    private const long T0 = 1_700_000_000_000; // ancla arbitraria, ms epoch

    public static IReadOnlyList<CasoPrueba> Casos() => new[]
    {
        new CasoPrueba(
            "ContinuidadPerfecta",
            new[]
            {
                new VelaCruda(T0, 100m, 101m, 99m, 100.5m, 10m),
                new VelaCruda(T0 + 60_000, 100.5m, 102m, 100m, 101m, 12m),
                new VelaCruda(T0 + 120_000, 101m, 103m, 100.8m, 102m, 8m),
            },
            EsperaApto: true, EsperaHuecos: false, EsperaErrores: false),

        new CasoPrueba(
            "HuecoDeDosMinutos",
            new[]
            {
                new VelaCruda(T0, 100m, 101m, 99m, 100m, 10m),
                new VelaCruda(T0 + 60_000, 100m, 101m, 99m, 100m, 10m),
                new VelaCruda(T0 + 240_000, 100m, 101m, 99m, 100m, 10m), // faltan T0+120000 y T0+180000
            },
            EsperaApto: false, EsperaHuecos: true, EsperaErrores: false),

        new CasoPrueba(
            "TimestampDuplicado",
            new[]
            {
                new VelaCruda(T0, 100m, 101m, 99m, 100m, 10m),
                new VelaCruda(T0, 100m, 101m, 99m, 100m, 10m),
            },
            EsperaApto: false, EsperaHuecos: false, EsperaErrores: true),

        new CasoPrueba(
            "OrdenInvertido",
            new[]
            {
                new VelaCruda(T0 + 60_000, 100m, 101m, 99m, 100m, 10m),
                new VelaCruda(T0, 100m, 101m, 99m, 100m, 10m),
            },
            EsperaApto: false, EsperaHuecos: false, EsperaErrores: true),

        new CasoPrueba(
            "OhlcImposible",
            new[]
            {
                new VelaCruda(T0, 100m, 95m, 99m, 100m, 10m), // High(95) < Open(100)
            },
            EsperaApto: false, EsperaHuecos: false, EsperaErrores: true),

        new CasoPrueba(
            "VolumenNegativo",
            new[]
            {
                new VelaCruda(T0, 100m, 101m, 99m, 100m, -5m),
            },
            EsperaApto: false, EsperaHuecos: false, EsperaErrores: true),
    };

    // Ejecuta los 6 casos y devuelve (nombreCaso, paso). No usa un framework de test: es el
    // mismo patron de harness ejecutable que exploration/Program.cs, sin fixtures/asserts extra
    // que esta etapa no necesita.
    public static (int Total, int Pasaron, IReadOnlyList<string> Detalles) EjecutarTodos()
    {
        var detalles = new List<string>();
        var pasaron = 0;

        foreach (var caso in Casos())
        {
            var veredicto = ValidadorIntegridadDatos.Verificar(caso.Velas);
            var tieneHuecos = veredicto.Huecos.Count > 0;
            var tieneErrores = veredicto.Errores.Count > 0;

            var paso = veredicto.AptoParaCongelar == caso.EsperaApto
                && tieneHuecos == caso.EsperaHuecos
                && tieneErrores == caso.EsperaErrores;

            if (paso) pasaron++;
            detalles.Add($"{(paso ? "OK" : "FALLA")}: {caso.Nombre} — AptoParaCongelar={veredicto.AptoParaCongelar} Huecos={veredicto.Huecos.Count} Errores={veredicto.Errores.Count}");
        }

        return (Casos().Count, pasaron, detalles);
    }
}
