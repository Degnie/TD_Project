using TD_Project.Application;
using TD_Project.Domain.Portfolio;
using TD_Project.Exploration;
using TD_Project.Laboratorio.Validadores;

namespace TD_Project.EvaluacionMultiTf;

// Extension de PerfilEstrategia (Fase 1.5) para Fase 2C: agrega distribucion de rachas por
// longitud, operaciones abiertas al cierre (categoria separada, precision C del usuario) y
// completitud del dataset usado (precision A). No modifica PerfilEstrategia — es un tipo nuevo
// que reutiliza el mismo calculo base sobre InfoOperacionResuelta/ResultadoBacktest.
public sealed class PerfilMultiTf
{
    public required IdentidadExperimento Identidad { get; init; }
    public required EstadoBacktest EstadoMotor { get; init; }
    public required decimal EquityInicial { get; init; }
    public required decimal EquityFinal { get; init; }
    public decimal RetornoPct => EquityInicial == 0 ? 0 : (EquityFinal - EquityInicial) / EquityInicial * 100m;

    // Operaciones COMPLETADAS (via InfoOperacionResuelta) vs. abierta al cierre del dataset —
    // categorias separadas, nunca mezcladas con ganadas/perdidas (precision C).
    public required int OperacionesCompletadas { get; init; }
    public required int OperacionesGanadas { get; init; }
    public required int OperacionesPerdidas { get; init; }
    public required bool OperacionAbiertaAlCierre { get; init; }
    public required decimal CapitalComprometidoAlCierre { get; init; }

    public required int RachaNegativaMaxima { get; init; }
    public required int Racha2 { get; init; }
    public required int Racha3 { get; init; }
    public required int Racha4 { get; init; }
    public required int Racha5Mas { get; init; }

    public required int GanoInicial { get; init; }
    public required int GanoM1 { get; init; }
    public required int GanoM2 { get; init; }
    public required int PerdioAgotandoMartingalas { get; init; }
    public decimal PctResueltasPorMartingala =>
        OperacionesCompletadas == 0 ? 0 : (GanoM1 + GanoM2) * 100m / OperacionesCompletadas;

    public required decimal MaxExposicion { get; init; }
    public required bool ReconciliacionCoherente { get; init; }
    public required IReadOnlyList<string> ErroresReconciliacion { get; init; }

    // Completitud del dataset usado en ESTA corrida (precision A): cuantas velas del timeframe
    // existian, cuantas se usaron (EsParcial=false) y cuantas se excluyeron.
    public required int VelasDisponibles { get; init; }
    public required int VelasUtilizadas { get; init; }
    public int VelasExcluidas => VelasDisponibles - VelasUtilizadas;
    public decimal PctUtilizado => VelasDisponibles == 0 ? 0 : VelasUtilizadas * 100m / VelasDisponibles;

    public static PerfilMultiTf Medir(
        IdentidadExperimento identidad, ResultadoBacktest resultado,
        IReadOnlyList<InfoOperacionResuelta> operaciones, int velasDisponibles, int velasUtilizadas)
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

        var ultimoSnapshot = resultado.PortfolioSnapshots.Count > 0 ? resultado.PortfolioSnapshots[^1] : null;
        var lotesVivosAlCierre = ultimoSnapshot?.LotesVivos ?? Array.Empty<Lote>();
        var abiertaAlCierre = lotesVivosAlCierre.Count > 0;
        var capitalComprometido = lotesVivosAlCierre.Sum(l => l.Margin);

        return new PerfilMultiTf
        {
            Identidad = identidad,
            EstadoMotor = resultado.Estado,
            EquityInicial = equityInicial,
            EquityFinal = equityFinal,
            OperacionesCompletadas = operaciones.Count,
            OperacionesGanadas = operaciones.Count(o => o.Gano),
            OperacionesPerdidas = operaciones.Count(o => !o.Gano),
            OperacionAbiertaAlCierre = abiertaAlCierre,
            CapitalComprometidoAlCierre = capitalComprometido,
            RachaNegativaMaxima = rachas.Count > 0 ? rachas.Max() : 0,
            Racha2 = rachas.Count(r => r == 2),
            Racha3 = rachas.Count(r => r == 3),
            Racha4 = rachas.Count(r => r == 4),
            Racha5Mas = rachas.Count(r => r >= 5),
            GanoInicial = operaciones.Count(o => o.Gano && o.MartingalasUsadas == 0),
            GanoM1 = operaciones.Count(o => o.Gano && o.MartingalasUsadas == 1),
            GanoM2 = operaciones.Count(o => o.Gano && o.MartingalasUsadas == 2),
            PerdioAgotandoMartingalas = operaciones.Count(o => !o.Gano),
            MaxExposicion = maxExposicion,
            ReconciliacionCoherente = reconciliacion.Coherente,
            ErroresReconciliacion = reconciliacion.Errores,
            VelasDisponibles = velasDisponibles,
            VelasUtilizadas = velasUtilizadas,
        };
    }
}
