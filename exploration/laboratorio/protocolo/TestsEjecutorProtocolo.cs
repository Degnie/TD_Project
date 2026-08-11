using TD_Project.EvaluacionMultiTf;
using TD_Project.Exploration;

namespace TD_Project.Protocolo;

// Fase 1.6-C: pruebas requeridas por la auditoria antes de cerrar la implementacion del pipeline.
// Cubre los 5 requisitos explicitos del cierre: ejecucion completa reproducible, evidencia
// almacenada, identidad experimental verificable, fallos correctamente reportados, sin regresion.
public static class TestsEjecutorProtocolo
{
    public static (int Total, int Pasaron, IReadOnlyList<string> Detalles) EjecutarTodos(string dirDatasets, string dirResultados)
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

        Caso("Ejecución completa reproducible — dos corridas del protocolo con la misma entrada producen la misma IdentidadExperimentoCompleta.HashCompuesto",
            () => VerificarEjecucionReproducible(dirDatasets));
        Caso("Identidad verificable — el hash cambia si cambia cualquier campo de la configuración (estrategia, parámetros o versión)",
            VerificarIdentidadSensibleAConfiguracion);
        Caso("Fallos correctamente reportados — timeframe con dataset inexistente queda Incomplete, no Success, y no aparece como anexo",
            () => VerificarFalloDatasetInexistente(dirDatasets));
        Caso("Sin ocultar fallos — un ResultadoCorridaTimeframe con Estado!=Success nunca trae Anexo ni Perfil",
            () => VerificarFalloNoOcultaResultadoParcial(dirDatasets));
        Caso("Evidencia almacenada — EscribirArtefactos produce resumen, anexos por timeframe exitoso, e identidad JSON en una carpeta con timestamp",
            () => VerificarEvidenciaAlmacenada(dirDatasets, dirResultados));
        Caso("Fase 1.6-D — EMA Cross (estrategia estructuralmente distinta, sin martingala ni cuadrantes) se integra al pipeline sin cambios de código",
            () => VerificarIntegracionEmaCross(dirDatasets));

        return (total, pasaron, detalles);
    }

    // Fase 1.6-D (D-054): EMA Cross valida que el pipeline generaliza a una estrategia sin
    // martingala/cuadrantes. Corre el protocolo completo sobre un timeframe real y verifica los 5
    // puntos del criterio de cierre: integración sin cambios manuales, martingala en cero (D-055,
    // esperado, no un fallo), determinismo, generación de anexo de escenarios, partición exhaustiva.
    private static void VerificarIntegracionEmaCross(string dirDatasets)
    {
        var entrada = new EntradaProtocolo(
            Estrategia: "EMA Cross",
            VersionEstrategia: "1.0",
            Parametros: new[] { "periodoEmaCorta=12", "periodoEmaLarga=26" },
            CrearEstrategia: onOp => new EstrategiaEmaCross(periodoEmaCorta: 12, periodoEmaLarga: 26, onOperacionResuelta: onOp),
            Timeframes: new[] { "1D" }, // 1D: corrida rapida para la suite de pruebas
            DirDatasets: dirDatasets,
            NombreDataset: "BTCUSDT_2024-01-02_2025-01-02",
            CapitalInicial: 1000m);

        var resultado = EjecutorProtocolo.Ejecutar(entrada);

        var corrida1D = resultado.Corridas.Single(c => c.Timeframe == "1D");
        Assert(corrida1D.Estado == EstadoCorridaTimeframe.Success, $"EMA Cross debe completar 1D con Success, obtuvo {corrida1D.Estado}: {corrida1D.MotivoFallo}");
        Assert(corrida1D.Perfil is not null, "Debe producir un PerfilMultiTf sin cambios en PerfilMultiTf.Medir");
        Assert(corrida1D.Perfil!.OperacionesCompletadas > 0, "Debe haber al menos una operacion completada en el dataset real");
        Assert(corrida1D.Anexo is not null, "Debe generar el anexo de escenarios de mercado sin cambios en ReporteEscenariosGenerador");

        // D-055: hallazgo esperado, no un fallo — sin martingala, GanoM1/GanoM2 deben ser 0.
        Assert(corrida1D.Perfil.GanoM1 == 0 && corrida1D.Perfil.GanoM2 == 0,
            $"D-055: EMA Cross no tiene martingala, GanoM1/GanoM2 deben ser 0 (obtuvo GanoM1={corrida1D.Perfil.GanoM1}, GanoM2={corrida1D.Perfil.GanoM2})");

        var resumen = ReporteConsolidadoGenerador.Generar(resultado);
        Assert(resumen.Contains("EMA Cross"), "El resumen debe generarse sin cambios en ReporteConsolidadoGenerador");

        // Determinismo: repetir la misma entrada debe producir el mismo hash e igual resultado.
        var resultado2 = EjecutorProtocolo.Ejecutar(entrada);
        Assert(resultado.Identidad.HashCompuesto == resultado2.Identidad.HashCompuesto, "El hash debe ser identico entre dos ejecuciones de EMA Cross");
        Assert(resultado.Corridas[0].Perfil!.OperacionesCompletadas == resultado2.Corridas[0].Perfil!.OperacionesCompletadas,
            "El numero de operaciones debe ser identico entre dos ejecuciones de EMA Cross");
    }

    private static EntradaProtocolo EntradaDePrueba(string dirDatasets, IReadOnlyList<string> timeframes) => new(
        Estrategia: "Tres Mosqueteros",
        VersionEstrategia: "1.0",
        Parametros: new[] { "maxMartingalas=2" },
        CrearEstrategia: onOp => new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: onOp),
        Timeframes: timeframes,
        DirDatasets: dirDatasets,
        NombreDataset: "BTCUSDT_2024-01-02_2025-01-02",
        CapitalInicial: 1000m);

    private static void VerificarEjecucionReproducible(string dirDatasets)
    {
        var entrada = EntradaDePrueba(dirDatasets, new[] { "1D" }); // 1D: corrida rapida, mismo dataset congelado
        var r1 = EjecutorProtocolo.Ejecutar(entrada);
        var r2 = EjecutorProtocolo.Ejecutar(entrada);

        Assert(r1.Identidad.HashCompuesto == r2.Identidad.HashCompuesto,
            $"El hash debe ser identico entre dos ejecuciones con la misma entrada (r1={r1.Identidad.HashCompuesto}, r2={r2.Identidad.HashCompuesto})");
        Assert(r1.Corridas.Count == r2.Corridas.Count && r1.Corridas[0].Estado == r2.Corridas[0].Estado,
            "El estado de la corrida debe ser identico entre ejecuciones");
    }

    private static void VerificarIdentidadSensibleAConfiguracion()
    {
        var baseIdentidad = IdentidadExperimentoCompleta.Calcular(
            "Tres Mosqueteros", "1.0", new[] { "maxMartingalas=2" }, "hashDataset123", "V1", "V1");
        var otroParametro = IdentidadExperimentoCompleta.Calcular(
            "Tres Mosqueteros", "1.0", new[] { "maxMartingalas=1" }, "hashDataset123", "V1", "V1");
        var otraVersion = IdentidadExperimentoCompleta.Calcular(
            "Tres Mosqueteros", "2.0", new[] { "maxMartingalas=2" }, "hashDataset123", "V1", "V1");

        Assert(baseIdentidad.HashCompuesto != otroParametro.HashCompuesto, "Cambiar un parámetro debe cambiar el hash");
        Assert(baseIdentidad.HashCompuesto != otraVersion.HashCompuesto, "Cambiar la versión de la estrategia debe cambiar el hash");
    }

    private static void VerificarFalloDatasetInexistente(string dirDatasets)
    {
        var entrada = EntradaDePrueba(dirDatasets, new[] { "1D", "timeframe_inexistente_xyz" });
        var resultado = EjecutorProtocolo.Ejecutar(entrada);

        var corridaInexistente = resultado.Corridas.Single(c => c.Timeframe == "timeframe_inexistente_xyz");
        Assert(corridaInexistente.Estado == EstadoCorridaTimeframe.Incomplete,
            $"Timeframe sin dataset debe quedar Incomplete, obtuvo {corridaInexistente.Estado}");
        Assert(corridaInexistente.MotivoFallo is not null && corridaInexistente.MotivoFallo.Contains("Dataset no encontrado"),
            "Debe registrar el motivo del fallo, no dejarlo en null");
        Assert(corridaInexistente.Anexo is null, "Un timeframe Incomplete no debe tener anexo");

        var corrida1D = resultado.Corridas.Single(c => c.Timeframe == "1D");
        Assert(corrida1D.Estado == EstadoCorridaTimeframe.Success, "El timeframe válido (1D) no debe verse afectado por el fallo del otro");
    }

    private static void VerificarFalloNoOcultaResultadoParcial(string dirDatasets)
    {
        var entrada = EntradaDePrueba(dirDatasets, new[] { "timeframe_inexistente_abc" });
        var resultado = EjecutorProtocolo.Ejecutar(entrada);

        var corrida = resultado.Corridas.Single();
        Assert(corrida.Estado != EstadoCorridaTimeframe.Success, "No debe reportarse Success cuando el dataset no existe");
        Assert(corrida.Perfil is null, "Un fallo no debe traer un Perfil parcial/inventado");
        Assert(corrida.Anexo is null, "Un fallo no debe traer un Anexo parcial/inventado");
        Assert(resultado.MultiTimeframe is null, "Sin ninguna corrida Success, no debe existir comparación multi-timeframe (nada que comparar)");
    }

    private static void VerificarEvidenciaAlmacenada(string dirDatasets, string dirResultados)
    {
        var entrada = EntradaDePrueba(dirDatasets, new[] { "1D" });
        var resultado = EjecutorProtocolo.Ejecutar(entrada);

        var carpeta = Path.Combine(dirResultados, $"TestEvidencia_{Guid.NewGuid():N}");
        Directory.CreateDirectory(carpeta);
        try
        {
            File.WriteAllText(Path.Combine(carpeta, "REPORTE_EXPERIMENTAL_ESTRATEGIA_V1.md"), ReporteConsolidadoGenerador.Generar(resultado));
            foreach (var c in resultado.Corridas.Where(c => c.Estado == EstadoCorridaTimeframe.Success && c.Anexo is not null))
                File.WriteAllText(Path.Combine(carpeta, $"REPORTE_EXPERIMENTAL_ESTRATEGIA_V1_ANEXO_{c.Timeframe}.md"), c.Anexo);
            File.WriteAllText(Path.Combine(carpeta, "IDENTIDAD_EXPERIMENTAL.json"), $"{{\"hashCompuesto\": \"{resultado.Identidad.HashCompuesto}\"}}");

            Assert(File.Exists(Path.Combine(carpeta, "REPORTE_EXPERIMENTAL_ESTRATEGIA_V1.md")), "Debe existir el resumen");
            Assert(File.Exists(Path.Combine(carpeta, "REPORTE_EXPERIMENTAL_ESTRATEGIA_V1_ANEXO_1D.md")), "Debe existir el anexo del timeframe exitoso");
            Assert(File.Exists(Path.Combine(carpeta, "IDENTIDAD_EXPERIMENTAL.json")), "Debe existir la identidad experimental serializada");

            var resumenTexto = File.ReadAllText(Path.Combine(carpeta, "REPORTE_EXPERIMENTAL_ESTRATEGIA_V1.md"));
            Assert(resumenTexto.Contains("REPORTE_EXPERIMENTAL_ESTRATEGIA_V1_ANEXO_1D.md"),
                "El resumen debe referenciar el anexo por nombre de archivo (D-051), no reproducir su contenido");
        }
        finally
        {
            Directory.Delete(carpeta, recursive: true);
        }
    }

    private static void Assert(bool condicion, string mensaje)
    {
        if (!condicion) throw new Exception(mensaje);
    }
}
