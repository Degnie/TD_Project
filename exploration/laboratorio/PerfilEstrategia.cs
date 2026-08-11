using TD_Project.Application;
using TD_Project.Exploration;
using TD_Project.Laboratorio.Validadores;

namespace TD_Project.Laboratorio;

// Perfil de comportamiento de UNA estrategia sobre UN dataset (Fase 1.5). No mide si la
// estrategia "gana" — mide como se comporta: retorno, riesgo por operacion logica completa
// (via InfoOperacionResuelta, no Trade individual — ver AnalisisRiesgo.cs) e integridad del
// motor (reconciliacion financiera). La separacion de estas 3 dimensiones es intencional: un
// mal retorno no es un hallazgo del motor, una reconciliacion rota si lo es.
public sealed class PerfilEstrategia
{
    public required string Escenario { get; init; }
    public required string Estrategia { get; init; }
    public required EstadoBacktest EstadoMotor { get; init; }
    public required decimal EquityInicial { get; init; }
    public required decimal EquityFinal { get; init; }
    public decimal RetornoPct => EquityInicial == 0 ? 0 : (EquityFinal - EquityInicial) / EquityInicial * 100m;
    public required int TotalOperaciones { get; init; }
    public required int OperacionesGanadas { get; init; }
    public required int OperacionesPerdidas { get; init; }
    public required int RachaNegativaMaxima { get; init; }
    public required int GanoInicial { get; init; }
    public required int GanoM1 { get; init; }
    public required int GanoM2 { get; init; }
    public required int PerdioAgotandoMartingalas { get; init; }
    public required decimal MaxExposicion { get; init; }
    public required bool ReconciliacionCoherente { get; init; }
    public required IReadOnlyList<string> ErroresReconciliacion { get; init; }
    public decimal PctOperacionesResueltasPorMartingala =>
        TotalOperaciones == 0 ? 0 : (GanoM1 + GanoM2) * 100m / TotalOperaciones;

    public static PerfilEstrategia Medir(string escenario, string nombreEstrategia, ResultadoBacktest resultado, IReadOnlyList<InfoOperacionResuelta> operaciones)
    {
        var equityInicial = resultado.EquityCurve.Count > 0 ? resultado.EquityCurve[0].Equity : 0m;
        var equityFinal = resultado.EquityCurve.Count > 0 ? resultado.EquityCurve[^1].Equity : 0m;
        var reconciliacion = ValidadorReconciliacionFinanciera.Verificar(resultado);

        var rachas = new List<int>();
        var rachaActual = 0;
        foreach (var op in operaciones)
        {
            if (!op.Gano) { rachaActual++; }
            else { if (rachaActual > 0) rachas.Add(rachaActual); rachaActual = 0; }
        }
        if (rachaActual > 0) rachas.Add(rachaActual);

        var maxExposicion = resultado.PortfolioSnapshots.Count == 0
            ? 0m
            : resultado.PortfolioSnapshots.Max(s => s.LotesVivos.Sum(l => Math.Abs(l.Cantidad)));

        return new PerfilEstrategia
        {
            Escenario = escenario,
            Estrategia = nombreEstrategia,
            EstadoMotor = resultado.Estado,
            EquityInicial = equityInicial,
            EquityFinal = equityFinal,
            TotalOperaciones = operaciones.Count,
            OperacionesGanadas = operaciones.Count(o => o.Gano),
            OperacionesPerdidas = operaciones.Count(o => !o.Gano),
            RachaNegativaMaxima = rachas.Count > 0 ? rachas.Max() : 0,
            GanoInicial = operaciones.Count(o => o.Gano && o.MartingalasUsadas == 0),
            GanoM1 = operaciones.Count(o => o.Gano && o.MartingalasUsadas == 1),
            GanoM2 = operaciones.Count(o => o.Gano && o.MartingalasUsadas == 2),
            PerdioAgotandoMartingalas = operaciones.Count(o => !o.Gano),
            MaxExposicion = maxExposicion,
            ReconciliacionCoherente = reconciliacion.Coherente,
            ErroresReconciliacion = reconciliacion.Errores,
        };
    }
}
