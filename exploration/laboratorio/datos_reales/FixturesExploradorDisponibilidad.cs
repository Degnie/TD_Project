namespace TD_Project.DatosReales;

// spec: ESPECIFICACION_IMPLEMENTACION_EXPLORACION_DISPONIBILIDAD_CASO5C_V1.md §8 — 4 pruebas
// obligatorias (P1-P4), sin red (mismo criterio que FixturesValidador.cs: puerta de entrada antes
// de cualquier llamada real a Binance).
public static class FixturesExploradorDisponibilidad
{
    public static (int Total, int Pasaron, IReadOnlyList<string> Detalles) EjecutarTodos()
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
                detalles.Add($"OK: {nombre}");
            }
            catch (Exception ex)
            {
                detalles.Add($"FALLA: {nombre} — {ex.Message}");
            }
        }

        Caso("P1 — Bloque continuo detectado correctamente", VerificarBloqueContinuo);
        Caso("P2 — Bloque con hueco detectado correctamente", VerificarBloqueConHueco);
        Caso("P3 — TodosContinuos agrega correctamente (11 OK + 1 con hueco -> false)", VerificarTodosContinuosAgrega);
        Caso("P4 — Ningun metodo publico de ExploradorDisponibilidad recibe una ruta de archivo", VerificarSinParametroDeRuta);

        return (total, pasaron, detalles);
    }

    private const long T0 = 1_700_000_000_000;

    // P1
    private static void VerificarBloqueContinuo()
    {
        var velas = new[]
        {
            new VelaCruda(T0, 100m, 101m, 99m, 100.5m, 10m),
            new VelaCruda(T0 + 60_000, 100.5m, 102m, 100m, 101m, 12m),
            new VelaCruda(T0 + 120_000, 101m, 103m, 100.8m, 102m, 8m),
        };
        var veredicto = ValidadorIntegridadDatos.Verificar(velas);
        var continuo = veredicto.Huecos.Count == 0 && veredicto.Errores.Count == 0;
        if (!continuo)
            throw new Exception($"Se esperaba bloque continuo, Huecos={veredicto.Huecos.Count} Errores={veredicto.Errores.Count}");
    }

    // P2
    private static void VerificarBloqueConHueco()
    {
        var velas = new[]
        {
            new VelaCruda(T0, 100m, 101m, 99m, 100m, 10m),
            new VelaCruda(T0 + 60_000, 100m, 101m, 99m, 100m, 10m),
            new VelaCruda(T0 + 180_000, 100m, 101m, 99m, 100m, 10m), // falta T0+120000
        };
        var veredicto = ValidadorIntegridadDatos.Verificar(velas);
        if (veredicto.Huecos.Count != 1)
            throw new Exception($"Se esperaba 1 hueco, se detectaron {veredicto.Huecos.Count}.");
    }

    // P3
    private static void VerificarTodosContinuosAgrega()
    {
        var bloques = new List<ExploradorDisponibilidad.ResultadoBloque>();
        for (var i = 0; i < 11; i++)
            bloques.Add(new ExploradorDisponibilidad.ResultadoBloque(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, Continuo: true, Huecos: 0, MinutosFaltantes: 0));
        bloques.Add(new ExploradorDisponibilidad.ResultadoBloque(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, Continuo: false, Huecos: 1, MinutosFaltantes: 80));

        var resultado = new ExploradorDisponibilidad.ResultadoExploracion("BTCUSDT", "1m", bloques);
        if (resultado.TodosContinuos)
            throw new Exception("TodosContinuos deberia ser false con 1 bloque no continuo entre 12.");
    }

    // P4
    private static void VerificarSinParametroDeRuta()
    {
        var metodos = typeof(ExploradorDisponibilidad).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Where(m => m.DeclaringType == typeof(ExploradorDisponibilidad));

        foreach (var metodo in metodos)
        {
            foreach (var parametro in metodo.GetParameters())
            {
                var nombre = parametro.Name ?? string.Empty;
                if (nombre.Contains("ruta", StringComparison.OrdinalIgnoreCase) || parametro.ParameterType == typeof(System.IO.Stream))
                    throw new Exception($"{metodo.Name}({parametro.Name}) sugiere una ruta de archivo — la exploracion no deberia poder escribir a disco.");
            }
        }
    }
}
