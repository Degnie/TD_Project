using TD_Project.AnalisisEscenariosMercado;

namespace TD_Project.ReporteEscenariosMercado;

// Fase 1.5-A, Paso 3: pruebas requeridas por la auditoria antes de cerrar MetricasPorEscenario.
// Datos sinteticos deterministas (OperacionConRegimen construidas a mano) — aisla el
// comportamiento de la agrupacion, no revalida AsignadorOperacionRegimen (Paso 2, ya cerrado) ni
// ClasificadorRegimenV1 (Fase 1.4-B, ya cerrado).
public static class TestsMetricasPorEscenario
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
                detalles.Add($"[PASA] {nombre}");
            }
            catch (Exception ex)
            {
                detalles.Add($"[FALLA] {nombre}: {ex.Message}");
            }
        }

        Caso("Particion correcta — cada operacion aparece en exactamente una fila, por cada vista",
            VerificarParticionCorrecta);
        Caso("Tratamiento de null — las operaciones sin regimen (Paso 2) se agrupan en su propia fila Regimen=null, no se pierden ni se mezclan con Ambiguo",
            VerificarTratamientoDeNull);
        Caso("Determinismo — dos calculos sobre los mismos datos producen exactamente las mismas filas",
            VerificarDeterminismo);
        Caso("Igualdad segmentado vs. total — la suma de OperacionesCompletadas de todas las filas de una vista es igual al total de operaciones de entrada",
            VerificarIgualdadSegmentadoVsTotal);
        Caso("Sin matriz de transicion — el reporte solo expone dos vistas independientes, ninguna estructura Entrada x Resolucion",
            VerificarSinMatrizDeTransicion);

        return (total, pasaron, detalles);
    }

    private static OperacionConRegimen Op(int id, bool gano, int martingalas, Escenario? entrada, Escenario? resolucion) =>
        new(id, gano, martingalas, TimestampEntrada: id * 10, TimestampResolucion: id * 10 + 1, entrada, resolucion);

    private static void VerificarParticionCorrecta()
    {
        var operaciones = new[]
        {
            Op(1, gano: true, martingalas: 0, entrada: Escenario.Alcista, resolucion: Escenario.Alcista),
            Op(2, gano: false, martingalas: 2, entrada: Escenario.Alcista, resolucion: Escenario.Bajista),
            Op(3, gano: true, martingalas: 1, entrada: Escenario.Lateral, resolucion: Escenario.Ambiguo),
        };

        var reporte = MetricasPorEscenario.Calcular(operaciones);

        Assert(reporte.PorRegimenEntrada.Filas.Sum(f => f.OperacionesCompletadas) == 3, "Vista por entrada debe cubrir las 3 operaciones");
        Assert(reporte.PorRegimenResolucion.Filas.Sum(f => f.OperacionesCompletadas) == 3, "Vista por resolucion debe cubrir las 3 operaciones");

        var filaAlcistaEntrada = reporte.PorRegimenEntrada.Filas.Single(f => f.Regimen == Escenario.Alcista);
        Assert(filaAlcistaEntrada.OperacionesCompletadas == 2, $"2 operaciones entraron en Alcista, obtuvo {filaAlcistaEntrada.OperacionesCompletadas}");
    }

    private static void VerificarTratamientoDeNull()
    {
        var operaciones = new[]
        {
            Op(1, gano: true, martingalas: 0, entrada: null, resolucion: Escenario.Alcista),
            Op(2, gano: false, martingalas: 0, entrada: Escenario.Ambiguo, resolucion: null),
            Op(3, gano: true, martingalas: 0, entrada: null, resolucion: null),
        };

        var reporte = MetricasPorEscenario.Calcular(operaciones);

        var sinRegimenEntrada = reporte.PorRegimenEntrada.Filas.Single(f => f.Regimen is null);
        Assert(sinRegimenEntrada.OperacionesCompletadas == 2, $"2 operaciones sin regimen de entrada, obtuvo {sinRegimenEntrada.OperacionesCompletadas}");

        var ambiguoEntrada = reporte.PorRegimenEntrada.Filas.SingleOrDefault(f => f.Regimen == Escenario.Ambiguo);
        Assert(ambiguoEntrada is not null && ambiguoEntrada.OperacionesCompletadas == 1,
            "La fila Ambiguo debe existir por separado de la fila Regimen=null (no se mezclan)");
    }

    private static void VerificarDeterminismo()
    {
        var operaciones = new[]
        {
            Op(1, gano: true, martingalas: 0, entrada: Escenario.Alcista, resolucion: Escenario.Alcista),
            Op(2, gano: false, martingalas: 2, entrada: Escenario.Lateral, resolucion: null),
        };

        var r1 = MetricasPorEscenario.Calcular(operaciones);
        var r2 = MetricasPorEscenario.Calcular(operaciones);

        Assert(r1.PorRegimenEntrada.Filas.Count == r2.PorRegimenEntrada.Filas.Count, "Mismo numero de filas en ambas corridas (vista entrada)");
        for (var i = 0; i < r1.PorRegimenEntrada.Filas.Count; i++)
        {
            Assert(r1.PorRegimenEntrada.Filas[i].Regimen == r2.PorRegimenEntrada.Filas[i].Regimen, $"Mismo regimen en la fila {i}");
            Assert(r1.PorRegimenEntrada.Filas[i].EficienciaOperacionalPct == r2.PorRegimenEntrada.Filas[i].EficienciaOperacionalPct, $"Misma eficiencia en la fila {i}");
        }
    }

    private static void VerificarIgualdadSegmentadoVsTotal()
    {
        var operaciones = new[]
        {
            Op(1, gano: true, martingalas: 0, entrada: Escenario.Alcista, resolucion: Escenario.Alcista),
            Op(2, gano: false, martingalas: 2, entrada: Escenario.Bajista, resolucion: Escenario.Lateral),
            Op(3, gano: true, martingalas: 1, entrada: null, resolucion: Escenario.Ambiguo),
            Op(4, gano: false, martingalas: 0, entrada: Escenario.Lateral, resolucion: null),
        };

        var reporte = MetricasPorEscenario.Calcular(operaciones);

        Assert(reporte.PorRegimenEntrada.TotalOperaciones == operaciones.Length,
            $"Total vista entrada ({reporte.PorRegimenEntrada.TotalOperaciones}) debe igualar total de operaciones ({operaciones.Length})");
        Assert(reporte.PorRegimenResolucion.TotalOperaciones == operaciones.Length,
            $"Total vista resolucion ({reporte.PorRegimenResolucion.TotalOperaciones}) debe igualar total de operaciones ({operaciones.Length})");
    }

    private static void VerificarSinMatrizDeTransicion()
    {
        // Verificacion estructural, no de comportamiento: ReporteMetricasPorEscenario solo expone
        // dos VistaPorEscenario (una dimension cada una) — ningun tipo de este archivo combina
        // RegimenEntrada y RegimenResolucion en una misma fila/celda (D-044, alcance restringido).
        var tipo = typeof(ReporteMetricasPorEscenario);
        var propiedades = tipo.GetProperties().Select(p => p.Name).ToList();

        Assert(propiedades.Count == 2, $"ReporteMetricasPorEscenario debe exponer exactamente 2 vistas, tiene {propiedades.Count}: {string.Join(",", propiedades)}");
        Assert(propiedades.Contains("PorRegimenEntrada") && propiedades.Contains("PorRegimenResolucion"),
            "Debe exponer PorRegimenEntrada y PorRegimenResolucion, nada mas");

        var propiedadesFila = typeof(FilaEscenario).GetProperties().Select(p => p.Name).ToList();
        Assert(propiedadesFila.Count(p => p.Contains("Regimen")) == 1,
            "FilaEscenario debe tener un unico campo de regimen (no Entrada+Resolucion combinados en la misma fila)");
    }

    private static void Assert(bool condicion, string mensaje)
    {
        if (!condicion) throw new Exception(mensaje);
    }
}
