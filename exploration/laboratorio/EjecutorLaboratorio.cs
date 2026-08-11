using TD_Project.Application;
using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;
using TD_Project.Laboratorio.Validadores;

namespace TD_Project.Laboratorio;

public static class EjecutorLaboratorio
{
    public sealed record ResultadoCorrida(
        HipotesisDataset Hipotesis,
        bool HipotesisPaso,
        string HipotesisDetalle,
        bool ReconciliacionCoherente,
        IReadOnlyList<string> ErroresReconciliacion);

    public static ResultadoCorrida Ejecutar(HipotesisDataset hipotesis, IReadOnlyList<Candle> velas, IStrategy estrategia, decimal capitalInicial = 1000m)
    {
        var config = new ConfiguracionExperimento(CapitalInicial: capitalInicial, Velas: velas);
        var resultado = BacktestRunner.Ejecutar(config, estrategia);

        var (paso, detalle) = hipotesis.Verificacion(resultado);
        var reconciliacion = ValidadorReconciliacionFinanciera.Verificar(resultado);

        return new ResultadoCorrida(hipotesis, paso, detalle, reconciliacion.Coherente, reconciliacion.Errores);
    }

    public static void ReportarCorrida(ResultadoCorrida corrida)
    {
        var h = corrida.Hipotesis;
        Console.WriteLine($"\n=== [{h.Categoria}] {h.Nombre} ({h.ArchivoCsv}) ===");
        Console.WriteLine($"Descripcion: {h.Descripcion}");
        Console.WriteLine($"Que valida: {h.QueValida}");
        Console.WriteLine($"Hipotesis: {(corrida.HipotesisPaso ? "PASA" : "FALLA")} — {corrida.HipotesisDetalle}");
        Console.WriteLine($"Reconciliacion financiera: {(corrida.ReconciliacionCoherente ? "OK" : "FALLA")}");
        if (!corrida.ReconciliacionCoherente)
            foreach (var error in corrida.ErroresReconciliacion)
                Console.WriteLine($"  - {error}");
    }
}
