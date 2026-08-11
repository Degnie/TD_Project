using System.Security.Cryptography;
using System.Text;
using TD_Project.EvaluacionMultiTf;

namespace TD_Project.AnalisisEscenariosMercado;

// Fase 1.4-B, Paso 3-B: pruebas requeridas antes de cerrar el congelamiento de
// ClasificadorRegimenV1 (determinismo, reproduccion, 4 estados, no dependencia de estrategia,
// compatibilidad con el evaluador ya existente de Fase 1.4-A). Ejecuta sobre el dataset real
// congelado BTCUSDT_2024-01-02_2025-01-02 — no usa datos sinteticos ni resultados de estrategia.
public static class TestsClasificadorRegimenV1
{
    public static (int Total, int Pasaron, IReadOnlyList<string> Detalles) EjecutarTodos(string dirDatasets, string nombreDataset)
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

        var rutaCsv1m = Path.Combine(dirDatasets, "1m", $"{nombreDataset}_1m.csv");
        var rutaCsv1D = Path.Combine(dirDatasets, "1D", $"{nombreDataset}_1D.csv");

        Caso("Determinismo — dos corridas sobre 1m producen exactamente la misma clasificación",
            () => VerificarDeterminismo(rutaCsv1m));
        Caso("Reproducción — usar Clasificar(velas) sin parámetros produce el mismo resultado que pasar los 3 valores congelados explícitamente",
            () => VerificarReproduccionConParametrosExplicitos(rutaCsv1m));
        Caso("Cuatro estados — el clasificador produce Alcista, Bajista, Lateral y Ambiguo en el dataset real (1m)",
            () => VerificarCuatroEstados(rutaCsv1m));
        Caso("No dependencia de estrategia — el clasificador no requiere ninguna estrategia para ejecutar sobre datasets de distinto tamaño",
            () => VerificarNoDependenciaDeEstrategia(rutaCsv1m, rutaCsv1D));
        Caso("Compatibilidad con EvaluadorClasificadores — la salida de ClasificadorRegimenV1 puede medirse con la misma infraestructura de Fase 1.4-A",
            () => VerificarCompatibilidadConEvaluador(rutaCsv1m));
        Caso("Metadata de version (D-052) — ClasificadorRegimenV1.Version existe y tiene el valor esperado",
            VerificarMetadataDeVersion);
        Caso("Hash de clasificacion sin cambios (D-052) — agregar Version no altera ningun resultado de clasificacion en los 6 timeframes",
            () => VerificarHashClasificacionSinCambios(dirDatasets, nombreDataset));

        return (total, pasaron, detalles);
    }

    // D-052: agregar Version debe ser metadata pura — no logica. Se verifica que exista y tenga
    // el valor esperado, sin inferir el valor desde el nombre del tipo (D-052 exige que la fuente
    // de verdad viva junto al artefacto, no que el pipeline la adivine).
    private static void VerificarMetadataDeVersion()
    {
        Assert(ClasificadorRegimenV1.Version == "V1", $"ClasificadorRegimenV1.Version debe ser \"V1\", obtuvo \"{ClasificadorRegimenV1.Version}\"");
    }

    // D-052, requisito explicito de la auditoria: "prueba de igualdad de resultados antes/despues".
    // El hash se calculo ANTES de agregar el campo Version (678768 ventanas, 6 timeframes,
    // BTCUSDT_2024-01-02_2025-01-02) y se embebe aqui como valor esperado — si el algoritmo de
    // clasificacion cambiara por accidente al tocar el archivo, esta prueba lo detectaria.
    private const string HashClasificacionEsperado = "482A242044303190258DEE3F7C80764D1C2CC1093B5DA0652822D5D85BE34052";

    private static void VerificarHashClasificacionSinCambios(string dirDatasets, string nombreDataset)
    {
        var timeframes = new[] { "1m", "5m", "15m", "1h", "4h", "1D" };
        var sb = new StringBuilder();

        foreach (var tf in timeframes)
        {
            var ruta = Path.Combine(dirDatasets, tf, $"{nombreDataset}_{tf}.csv");
            var velas = LectorDerivado.Leer(ruta);
            var clasificacion = ClasificadorRegimenV1.Clasificar(velas);
            foreach (var v in clasificacion)
                sb.Append(tf).Append('|').Append(v.InicioUtcMs).Append('|').Append(v.Escenario).Append('\n');
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
        Assert(hash == HashClasificacionEsperado,
            $"El hash de clasificacion cambio tras agregar Version — esto violaria D-052 (metadata pura, sin cambio de logica). Esperado={HashClasificacionEsperado}, obtuvo={hash}");
    }

    private static void VerificarDeterminismo(string rutaCsv)
    {
        var velas = LectorDerivado.Leer(rutaCsv);
        var corrida1 = ClasificadorRegimenV1.Clasificar(velas);
        var corrida2 = ClasificadorRegimenV1.Clasificar(velas);

        Assert(corrida1.Count == corrida2.Count, $"Ambas corridas deben tener el mismo conteo de ventanas ({corrida1.Count} vs {corrida2.Count})");
        for (var i = 0; i < corrida1.Count; i++)
        {
            Assert(corrida1[i].InicioUtcMs == corrida2[i].InicioUtcMs, $"InicioUtcMs debe coincidir en la ventana {i}");
            Assert(corrida1[i].Escenario == corrida2[i].Escenario, $"Escenario debe coincidir en la ventana {i} ({corrida1[i].Escenario} vs {corrida2[i].Escenario})");
        }
    }

    private static void VerificarReproduccionConParametrosExplicitos(string rutaCsv)
    {
        var velas = LectorDerivado.Leer(rutaCsv);
        var conDefault = ClasificadorRegimenV1.Clasificar(velas);
        var conExplicito = ClasificadorRegimenV1.Clasificar(velas, ClasificadorRegimenV1.PeriodoAdx, ClasificadorRegimenV1.UmbralAdxTendencia, ClasificadorRegimenV1.UmbralSesgoDI);

        Assert(conDefault.Count == conExplicito.Count, "Ambas formas de invocación deben producir el mismo conteo de ventanas");
        for (var i = 0; i < conDefault.Count; i++)
            Assert(conDefault[i].Escenario == conExplicito[i].Escenario, $"Escenario debe coincidir en la ventana {i} entre Clasificar(velas) y Clasificar(velas, 14, 25, 0.153467)");
    }

    private static void VerificarCuatroEstados(string rutaCsv)
    {
        var velas = LectorDerivado.Leer(rutaCsv);
        var clasificacion = ClasificadorRegimenV1.Clasificar(velas);

        var presentes = clasificacion.Select(v => v.Escenario).Distinct().ToHashSet();
        Assert(presentes.Contains(Escenario.Alcista), "Debe producir Alcista en el dataset real");
        Assert(presentes.Contains(Escenario.Bajista), "Debe producir Bajista en el dataset real");
        Assert(presentes.Contains(Escenario.Lateral), "Debe producir Lateral en el dataset real");
        Assert(presentes.Contains(Escenario.Ambiguo), "Debe producir Ambiguo en el dataset real — este es el estado que la implementación exploratoria de Fase 1.4-A NO producía (motivo de D-028/D-029)");
    }

    private static void VerificarNoDependenciaDeEstrategia(string rutaCsv1m, string rutaCsv1D)
    {
        // No hay forma de "pasar" una estrategia a ClasificadorRegimenV1.Clasificar — su firma solo
        // acepta IReadOnlyList<VelaDerivadaCruda> y parametros numericos (D-016 por diseño de tipos,
        // no solo por convencion). Esta prueba confirma que el clasificador ejecuta identicamente
        // sobre datasets de tamano y timeframe muy distintos sin ningun dato operacional externo.
        var velas1m = LectorDerivado.Leer(rutaCsv1m);
        var velas1D = LectorDerivado.Leer(rutaCsv1D);

        var clasif1m = ClasificadorRegimenV1.Clasificar(velas1m);
        var clasif1D = ClasificadorRegimenV1.Clasificar(velas1D);

        Assert(clasif1m.Count > 0, "Debe producir clasificación sobre 1m sin ningún dato de estrategia");
        Assert(clasif1D.Count > 0, "Debe producir clasificación sobre 1D sin ningún dato de estrategia");
    }

    private static void VerificarCompatibilidadConEvaluador(string rutaCsv)
    {
        var velas = LectorDerivado.Leer(rutaCsv);
        var clasificacion = ClasificadorRegimenV1.Clasificar(velas);

        // Misma infraestructura de medicion ya usada para comparar A/B/C en Fase 1.4-A —
        // confirma que ClasificadorRegimenV1 no requiere un evaluador nuevo (D-015).
        var metricas = EvaluadorClasificadores.Evaluar("ClasificadorRegimenV1", "1m", clasificacion, velas.Count);

        Assert(metricas.VentanasTotales > 0, "El evaluador debe poder medir la salida de ClasificadorRegimenV1");
        Assert(metricas.PctAlcista + metricas.PctBajista + metricas.PctLateral + metricas.PctAmbiguo > 99m, "Las 4 categorías deben sumar ~100% (partición exhaustiva)");
    }

    private static void Assert(bool condicion, string mensaje)
    {
        if (!condicion) throw new Exception(mensaje);
    }
}
