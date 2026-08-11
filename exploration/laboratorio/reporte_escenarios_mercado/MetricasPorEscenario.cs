using TD_Project.AnalisisEscenariosMercado;

namespace TD_Project.ReporteEscenariosMercado;

// Fase 1.5-A, Paso 3 (ESPECIFICACION_METRICAS_ESCENARIOS_MERCADO_V1.md): agrupa OperacionConRegimen
// (Paso 2) reutilizando el catalogo ya congelado de Fase 1.2 (AnalizadorOperacional.cs, secciones
// 4.1-4.3), sin definir formulas nuevas (D-015). D-044 (aprobado con restriccion): dos vistas
// independientes por RegimenEntrada y por RegimenResolucion, nunca una matriz de transicion
// Entrada x Resolucion. D-045 (aprobado): "Peores escenarios" (racha negativa, exposicion) queda
// excluido de este catalogo segmentado — no respeta limites de un regimen, se mantiene solo en su
// forma global (Fase 1.2, sin segmentar).

// Fila por regimen — mismo catalogo de Fase 1.2 §4.1/§4.2/§4.3, agrupado en vez de global.
// Regimen=null representa la categoria "Sin regimen" (Paso 2: dato no disponible), distinta de
// Escenario.Ambiguo (regimen calculado).
public sealed record FilaEscenario(
    Escenario? Regimen,
    int OperacionesCompletadas,
    int Ganadas,
    int Perdidas,
    decimal EficienciaOperacionalPct,
    int GanoInicial,
    int GanoM1,
    int GanoM2,
    int PerdioAgotando,
    decimal PctResueltasPorMartingala);

// Vista = una agrupacion completa (particion exhaustiva) segun una sola dimension de regimen.
public sealed record VistaPorEscenario(IReadOnlyList<FilaEscenario> Filas)
{
    public int TotalOperaciones => Filas.Sum(f => f.OperacionesCompletadas);
}

public sealed record ReporteMetricasPorEscenario(
    VistaPorEscenario PorRegimenEntrada,
    VistaPorEscenario PorRegimenResolucion);

public static class MetricasPorEscenario
{
    public static ReporteMetricasPorEscenario Calcular(IReadOnlyList<OperacionConRegimen> operaciones) =>
        new(
            PorRegimenEntrada: CalcularVista(operaciones, o => o.RegimenEntrada),
            PorRegimenResolucion: CalcularVista(operaciones, o => o.RegimenResolucion));

    private static VistaPorEscenario CalcularVista(
        IReadOnlyList<OperacionConRegimen> operaciones, Func<OperacionConRegimen, Escenario?> seleccionarRegimen)
    {
        var filas = operaciones
            .GroupBy(seleccionarRegimen)
            .Select(grupo => CalcularFila(grupo.Key, grupo.ToList()))
            .OrderBy(f => f.Regimen.HasValue ? (int)f.Regimen.Value : int.MaxValue) // "Sin regimen" (null) al final, consistente entre corridas
            .ToList();

        return new VistaPorEscenario(filas);
    }

    private static FilaEscenario CalcularFila(Escenario? regimen, IReadOnlyList<OperacionConRegimen> operaciones)
    {
        var completadas = operaciones.Count;
        var ganadas = operaciones.Count(o => o.Gano);

        return new FilaEscenario(
            Regimen: regimen,
            OperacionesCompletadas: completadas,
            Ganadas: ganadas,
            Perdidas: operaciones.Count(o => !o.Gano),
            EficienciaOperacionalPct: PctSeguro(ganadas, completadas),
            GanoInicial: operaciones.Count(o => o.Gano && o.MartingalasUsadas == 0),
            GanoM1: operaciones.Count(o => o.Gano && o.MartingalasUsadas == 1),
            GanoM2: operaciones.Count(o => o.Gano && o.MartingalasUsadas == 2),
            PerdioAgotando: operaciones.Count(o => !o.Gano),
            PctResueltasPorMartingala: PctSeguro(operaciones.Count(o => o.Gano && o.MartingalasUsadas > 0), completadas));
    }

    private static decimal PctSeguro(int parte, int total) =>
        total == 0 ? 0 : parte * 100m / total;
}
