using TD_Project.AnalisisEscenariosMercado;
using TD_Project.AnalisisMultiTimeframe;
using TD_Project.Application;
using TD_Project.Domain.Broker;
using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;
using TD_Project.EvaluacionMultiTf;
using TD_Project.Exploration;
using TD_Project.ModeloFinanciero;
using TD_Project.ReporteEscenariosMercado;

namespace TD_Project.Protocolo;

// Fase 1.6-C (ESPECIFICACION_PIPELINE_EXPERIMENTAL_V1.md): orquestador que encadena los modulos ya
// congelados de Fases 1.0-1.5-B, en el orden fijo de PROTOCOLO_EVALUACION_ESTRATEGIA_V1.md §4. No
// recalcula ninguna metrica (D-015), no modifica ningun modulo existente, no introduce
// paralelismo ni reintentos automaticos. Capa nueva — no reemplaza los Program.cs de cada modulo,
// que siguen siendo validos para ejecutar un analisis aislado.

// Estado explicito por timeframe (auditoria de Fase 1.6-C: "si una etapa falla, NO generar
// reporte final como Success" — no ocultar errores parciales).
public enum EstadoCorridaTimeframe { Success, Failed, Incomplete }

// spec-lab: ESPECIFICACION_PIPELINE_EXPERIMENTAL_V1.md §6 — un fallo en un timeframe no detiene
// la evaluacion de los demas. Cada timeframe produce su propio resultado, con su propio estado.
// spec: Caso 2 D-072/D-073/D-075/D-077/D-078 — MetricasFinancieras opcional, poblada solo en la
// rama Success (mismo criterio D-061/D-069: no rompe call sites existentes de TestsEjecutorProtocolo).
// spec: Caso 4 D-096/D-097 — Incapacidades expone RegistroIncapacidad (ya calculado por el motor
// desde Caso 2, D-059, pero descartado hasta ahora) sin modificar src/ ni ValidadorCapacidad.
// Restriccion economica observable, no error (D-097) — mismo patron de campo opcional poblado
// solo en Success que MetricasFinancieras.
public sealed record ResultadoCorridaTimeframe(
    string Timeframe,
    EstadoCorridaTimeframe Estado,
    string? MotivoFallo,
    PerfilMultiTf? Perfil,
    string? Anexo,
    MetricasFinancieras? MetricasFinancieras = null,
    IReadOnlyList<RegistroIncapacidad>? Incapacidades = null);

// spec: Caso 3 D-090 — Caracteristicas es propiedad de la Estrategia, no de una corrida por
// timeframe (una sola declaracion vale para todos los timeframes de ResultadoProtocolo).
public sealed record ResultadoProtocolo(
    string Estrategia,
    IdentidadExperimentoCompleta Identidad,
    IReadOnlyList<ResultadoCorridaTimeframe> Corridas,
    PerfilMultiTimeframe? MultiTimeframe, // null si ningun timeframe completo Success (no hay nada que comparar)
    CaracteristicasEstrategia? Caracteristicas = null);

// spec: Caso 2 D-079 — Instrumento/Costes/Sizing opcionales, default null = comportamiento
// historico (mismo criterio D-061). Permite que una corrida del protocolo use el modelo
// economico/costes/gestion de capital de Caso 2 en lugar de los defaults de Caso 1.
// spec: Caso 3 D-090 — CaracteristicasEstrategia opcional, metadata declarativa externa a
// IStrategy (D-088). null = no declarado (no se asume UsaMartingala=true ni false por defecto —
// distinto de D-061/D-079, donde el default representa un comportamiento economico neutro;
// aqui "no declarado" es un tercer estado honesto, ver EjecutorProtocolo.EjecutarUnTimeframe).
public sealed record EntradaProtocolo(
    string Estrategia,
    string VersionEstrategia,
    IReadOnlyList<string> Parametros,
    Func<Action<InfoOperacionResuelta>, IStrategy> CrearEstrategia,
    IReadOnlyList<string> Timeframes,
    string DirDatasets,
    string NombreDataset,
    decimal CapitalInicial,
    Instrumento? Instrumento = null,
    ConfiguracionCostes? Costes = null,
    ConfiguracionSizing? Sizing = null,
    CaracteristicasEstrategia? Caracteristicas = null);

// spec: Caso 3 D-088/D-090 — metadata declarativa de capacidades de la estrategia, externa a
// IStrategy (no contamina el contrato de ejecucion). Deliberadamente minima: solo el campo que
// D-088 necesita resolver ahora. No incluye UsaSizingPropio/UsaEstadoInternoPersistente u otros
// campos sin consumidor concreto (mismo principio que evito extender IStrategy sin necesidad
// demostrada, ver ESPECIFICACION_IMPLEMENTACION_ZSCORE_REVERSAL_V1.md §3).
public sealed record CaracteristicasEstrategia(bool UsaMartingala);

public static class EjecutorProtocolo
{
    public const string VersionProtocolo = "V1";

    public static ResultadoProtocolo Ejecutar(EntradaProtocolo entrada)
    {
        var corridas = new List<ResultadoCorridaTimeframe>();
        var perfilesExitosos = new List<PerfilMultiTf>();
        string? datasetSourceSha256 = null;

        foreach (var tf in entrada.Timeframes)
        {
            var resultado = EjecutarUnTimeframe(entrada, tf);
            corridas.Add(resultado);

            if (resultado.Estado == EstadoCorridaTimeframe.Success && resultado.Perfil is not null)
            {
                perfilesExitosos.Add(resultado.Perfil);
                datasetSourceSha256 ??= resultado.Perfil.Identidad.SourceSha256;
            }
        }

        var multiTimeframe = perfilesExitosos.Count > 0
            ? ComparadorMultiTimeframe.Comparar(entrada.Estrategia, perfilesExitosos)
            : null;

        var identidad = IdentidadExperimentoCompleta.Calcular(
            entrada.Estrategia, entrada.VersionEstrategia, entrada.Parametros,
            datasetSourceSha256 ?? "(sin corridas exitosas — sin hash de dataset disponible)",
            ClasificadorRegimenV1.Version, VersionProtocolo,
            entrada.Instrumento, entrada.Costes, entrada.Sizing);

        return new ResultadoProtocolo(entrada.Estrategia, identidad, corridas, multiTimeframe, entrada.Caracteristicas);
    }

    private static ResultadoCorridaTimeframe EjecutarUnTimeframe(EntradaProtocolo entrada, string tf)
    {
        var rutaCsv = Path.Combine(entrada.DirDatasets, tf, $"{entrada.NombreDataset}_{tf}.csv");
        var rutaMetadata = Path.Combine(entrada.DirDatasets, tf, "metadata.json");

        // §6: dataset del timeframe no existe — se omite ese timeframe, se registra, nunca se
        // omite silenciosamente de la lista de timeframes declarados en la entrada.
        if (!File.Exists(rutaCsv))
            return new ResultadoCorridaTimeframe(tf, EstadoCorridaTimeframe.Incomplete,
                $"Dataset no encontrado: {rutaCsv}", null, null);

        var crudas = LectorDerivado.Leer(rutaCsv);
        var metadata = LectorDerivado.LeerMetadata(rutaMetadata);
        var (velas, disponibles, utilizadas) = LectorDerivado.FiltrarParaBacktest(crudas);

        // §5 del protocolo: validacion de determinismo — 2 corridas, comparar campo por campo.
        var operaciones1 = new List<InfoOperacionResuelta>();
        var resultado1 = BacktestRunner.Ejecutar(
            new ConfiguracionExperimento(CapitalInicial: entrada.CapitalInicial, Velas: velas,
                Instrumento: entrada.Instrumento, Costes: entrada.Costes, Sizing: entrada.Sizing),
            entrada.CrearEstrategia(operaciones1.Add));

        var operaciones2 = new List<InfoOperacionResuelta>();
        var resultado2 = BacktestRunner.Ejecutar(
            new ConfiguracionExperimento(CapitalInicial: entrada.CapitalInicial, Velas: velas,
                Instrumento: entrada.Instrumento, Costes: entrada.Costes, Sizing: entrada.Sizing),
            entrada.CrearEstrategia(operaciones2.Add));

        var motivoNoDeterminismo = VerificarDeterminismo(operaciones1, operaciones2, resultado1, resultado2);
        if (motivoNoDeterminismo is not null)
            return new ResultadoCorridaTimeframe(tf, EstadoCorridaTimeframe.Failed, motivoNoDeterminismo, null, null);

        if (resultado1.Estado != EstadoBacktest.Success)
            return new ResultadoCorridaTimeframe(tf, EstadoCorridaTimeframe.Failed,
                $"EstadoBacktest={resultado1.Estado} (sin operaciones validas que vincular — no se genera anexo)", null, null);

        var identidad = new IdentidadExperimento(
            Dataset: entrada.NombreDataset, Timeframe: tf, Estrategia: entrada.Estrategia,
            CapitalInicial: entrada.CapitalInicial, AggregationVersion: metadata.AggregationVersion,
            SourceSha256: metadata.SourceSha256, TimeframeSha256: metadata.Sha256, FechaEjecucionUtc: DateTime.UtcNow);

        var perfil = PerfilMultiTf.Medir(identidad, resultado1, operaciones1, disponibles, utilizadas);

        // Clasificacion de regimen sobre las MISMAS velas del backtest (D-016: nunca conoce la estrategia).
        var clasificacion = ClasificadorRegimenV1.Clasificar(crudas);
        var operacionesConRegimen = AsignadorOperacionRegimen.Asignar(operaciones1, clasificacion);
        var metricas = MetricasPorEscenario.Calcular(operacionesConRegimen);
        var anexo = ReporteEscenariosGenerador.Generar(identidad, perfil.OperacionesCompletadas, metricas);

        // spec: Caso 2 D-072/D-077 — resultado1 (ResultadoBacktest) y entrada.CapitalInicial ya
        // estan disponibles en este scope, fuente oficial unica (sin recalculo paralelo).
        var metricasFinancieras = CalculadoraMetricasFinancieras.Calcular(resultado1, entrada.CapitalInicial);

        return new ResultadoCorridaTimeframe(tf, EstadoCorridaTimeframe.Success, null, perfil, anexo, metricasFinancieras, resultado1.IncapacidadesEfectivas);
    }

    private static string? VerificarDeterminismo(
        IReadOnlyList<InfoOperacionResuelta> op1, IReadOnlyList<InfoOperacionResuelta> op2,
        ResultadoBacktest r1, ResultadoBacktest r2)
    {
        if (r1.Estado != r2.Estado)
            return $"EstadoBacktest difiere entre corridas: {r1.Estado} vs {r2.Estado}";
        if (op1.Count != op2.Count)
            return $"Cantidad de operaciones difiere entre corridas: {op1.Count} vs {op2.Count}";

        for (var i = 0; i < op1.Count; i++)
        {
            if (op1[i].Gano != op2[i].Gano || op1[i].MartingalasUsadas != op2[i].MartingalasUsadas ||
                op1[i].TimestampEntrada != op2[i].TimestampEntrada || op1[i].TimestampResolucion != op2[i].TimestampResolucion)
                return $"Operacion {i} difiere entre corridas (Gano/MartingalasUsadas/Timestamps no coinciden)";
        }

        return null;
    }
}
