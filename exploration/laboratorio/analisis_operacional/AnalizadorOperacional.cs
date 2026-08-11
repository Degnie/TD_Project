using TD_Project.EvaluacionMultiTf;

namespace TD_Project.AnalisisOperacional;

// Fase 1.2 (ESPECIFICACION_ANALIZADOR_OPERACIONAL_V1.md): capa de lectura pura sobre PerfilMultiTf,
// ya calculado y verificado por el motor. No recalcula nada que el motor no calcule; no modifica
// BacktestRunner, IStrategy, DTOs ni estrategias. Todo campo aqui es Dato observado o Metrica
// calculada (secciones 4.1, 4.2, 4.6 de la especificacion) — no incluye Dependencia de martingala
// clasificada (D-005, pendiente) ni Escenarios de mercado (D-006, pendiente).

// spec-lab: ESPECIFICACION_ANALIZADOR_OPERACIONAL_V1.md §4.1 — Resultado general de estrategia.
public sealed record ResultadoGeneral(
    int IntentosCompletados,
    bool IntentoIncompleto,
    int Victorias,
    int Derrotas,
    decimal EficienciaOperacionalPct);

// spec-lab: ESPECIFICACION_ANALIZADOR_OPERACIONAL_V1.md §4.2 — Resolucion de intentos.
// Los 4 porcentajes suman 100% de OperacionesCompletadas por construccion (particion exhaustiva).
public sealed record ResolucionDeIntentos(
    decimal VictoriaInicialPct,
    decimal RecuperacionM1Pct,
    decimal RecuperacionM2Pct,
    decimal PerdidaAgotandoPct,
    decimal PctResueltasPorMartingala); // D-005: solo porcentaje, sin clasificacion baja/media/alta

// spec-lab: ESPECIFICACION_ANALIZADOR_OPERACIONAL_V1.md §4.6 — Peores escenarios observados.
public sealed record PeoresEscenarios(
    int MayorRachaNegativa,
    bool AlcanzoTopeMartingala,
    decimal MayorExposicionExperimental);

// spec-lab: ESPECIFICACION_ANALIZADOR_OPERACIONAL_V1.md §5 — Datos derivados del modelo actual,
// explicitamente separados de las metricas oficiales. Nunca se mezclan con ResultadoGeneral.
public sealed record DatosDerivadosModeloActual(
    decimal EquityInicial,
    decimal EquityFinal,
    decimal RetornoPct);

public sealed record ReporteOperacional(
    IdentidadExperimento Identidad,
    ResultadoGeneral ResultadoGeneral,
    ResolucionDeIntentos ResolucionDeIntentos,
    PeoresEscenarios PeoresEscenarios,
    DatosDerivadosModeloActual DatosDerivadosModeloActual,
    bool ReconciliacionCoherente);

public static class AnalizadorOperacional
{
    public static ReporteOperacional Analizar(PerfilMultiTf perfil)
    {
        var completadas = perfil.OperacionesCompletadas;

        var resultadoGeneral = new ResultadoGeneral(
            IntentosCompletados: completadas,
            IntentoIncompleto: perfil.OperacionAbiertaAlCierre,
            Victorias: perfil.OperacionesGanadas,
            Derrotas: perfil.OperacionesPerdidas,
            EficienciaOperacionalPct: PctSeguro(perfil.OperacionesGanadas, completadas));

        var resolucionDeIntentos = new ResolucionDeIntentos(
            VictoriaInicialPct: PctSeguro(perfil.GanoInicial, completadas),
            RecuperacionM1Pct: PctSeguro(perfil.GanoM1, completadas),
            RecuperacionM2Pct: PctSeguro(perfil.GanoM2, completadas),
            PerdidaAgotandoPct: PctSeguro(perfil.PerdioAgotandoMartingalas, completadas),
            PctResueltasPorMartingala: perfil.PctResueltasPorMartingala);

        var peoresEscenarios = new PeoresEscenarios(
            MayorRachaNegativa: perfil.RachaNegativaMaxima,
            AlcanzoTopeMartingala: perfil.GanoM2 > 0 || perfil.PerdioAgotandoMartingalas > 0,
            MayorExposicionExperimental: perfil.MaxExposicion);

        var datosDerivados = new DatosDerivadosModeloActual(
            EquityInicial: perfil.EquityInicial,
            EquityFinal: perfil.EquityFinal,
            RetornoPct: perfil.RetornoPct);

        return new ReporteOperacional(
            Identidad: perfil.Identidad,
            ResultadoGeneral: resultadoGeneral,
            ResolucionDeIntentos: resolucionDeIntentos,
            PeoresEscenarios: peoresEscenarios,
            DatosDerivadosModeloActual: datosDerivados,
            ReconciliacionCoherente: perfil.ReconciliacionCoherente);
    }

    private static decimal PctSeguro(int parte, int total) =>
        total == 0 ? 0 : parte * 100m / total;
}
