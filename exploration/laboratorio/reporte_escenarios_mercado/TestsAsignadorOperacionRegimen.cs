using TD_Project.AnalisisEscenariosMercado;
using TD_Project.Exploration;

namespace TD_Project.ReporteEscenariosMercado;

// Fase 1.5-A, Paso 2: pruebas requeridas por la auditoria antes de cerrar AsignadorOperacionRegimen.
// Usa datos sinteticos deterministas (VentanaClasificada construidas a mano) — no depende del
// clasificador real ejecutando sobre el dataset, porque el objetivo es aislar el comportamiento de
// la asignacion misma (coincidencia exacta), no revalidar ClasificadorRegimenV1 (ya congelado y
// probado en Fase 1.4-B).
public static class TestsAsignadorOperacionRegimen
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

        Caso("Coincidencia exacta — una operacion cuyo timestamp coincide con una VentanaClasificada recibe ese Escenario",
            VerificarCoincidenciaExacta);
        Caso("Sin coincidencia — Escenario=null cuando el timestamp no aparece en la clasificacion (calentamiento)",
            VerificarSinCoincidencia);
        Caso("Clasificacion vacia — todas las operaciones quedan con RegimenEntrada/RegimenResolucion=null, sin fallar",
            VerificarClasificacionVacia);
        Caso("Conservacion — el numero de OperacionConRegimen es igual al numero de operaciones de entrada",
            VerificarConservacionDeOperaciones);
        Caso("Determinismo — dos ejecuciones sobre los mismos datos producen exactamente la misma asignacion",
            VerificarDeterminismo);

        return (total, pasaron, detalles);
    }

    private static InfoOperacionResuelta Op(int id, long entrada, long resolucion, bool gano = true, int martingalas = 0) =>
        new(id, martingalas, gano, entrada, resolucion);

    private static VentanaClasificada Ventana(long timestamp, Escenario escenario) =>
        new(timestamp, timestamp, escenario);

    private static void VerificarCoincidenciaExacta()
    {
        var operaciones = new[] { Op(1, entrada: 100, resolucion: 200) };
        var clasificacion = new[] { Ventana(100, Escenario.Alcista), Ventana(200, Escenario.Bajista) };

        var resultado = AsignadorOperacionRegimen.Asignar(operaciones, clasificacion);

        Assert(resultado.Count == 1, "Debe producir una fila por operacion");
        Assert(resultado[0].RegimenEntrada == Escenario.Alcista, $"RegimenEntrada esperado Alcista, obtuvo {resultado[0].RegimenEntrada}");
        Assert(resultado[0].RegimenResolucion == Escenario.Bajista, $"RegimenResolucion esperado Bajista, obtuvo {resultado[0].RegimenResolucion}");
    }

    private static void VerificarSinCoincidencia()
    {
        // Timestamp 50 no existe en la clasificacion (simula ventana de calentamiento, seccion 4).
        var operaciones = new[] { Op(1, entrada: 50, resolucion: 200) };
        var clasificacion = new[] { Ventana(200, Escenario.Lateral) };

        var resultado = AsignadorOperacionRegimen.Asignar(operaciones, clasificacion);

        Assert(resultado[0].RegimenEntrada is null, $"RegimenEntrada debe ser null cuando no hay coincidencia, obtuvo {resultado[0].RegimenEntrada}");
        Assert(resultado[0].RegimenResolucion == Escenario.Lateral, "RegimenResolucion si debe resolverse cuando el timestamp coincide");
    }

    private static void VerificarClasificacionVacia()
    {
        var operaciones = new[] { Op(1, entrada: 10, resolucion: 20), Op(2, entrada: 30, resolucion: 40) };
        var clasificacion = Array.Empty<VentanaClasificada>();

        var resultado = AsignadorOperacionRegimen.Asignar(operaciones, clasificacion);

        Assert(resultado.Count == 2, "Debe conservar todas las operaciones aun sin clasificacion disponible");
        Assert(resultado.All(r => r.RegimenEntrada is null && r.RegimenResolucion is null),
            "Con clasificacion vacia, todas las filas deben quedar sin regimen (null), sin lanzar excepcion");
    }

    private static void VerificarConservacionDeOperaciones()
    {
        var operaciones = new[]
        {
            Op(1, entrada: 10, resolucion: 20),
            Op(2, entrada: 20, resolucion: 30, gano: false, martingalas: 2),
            Op(3, entrada: 40, resolucion: 40), // entrada == resolucion, caso valido (sin martingala, aunque RN-13 lo hace infrecuente en datos reales)
        };
        var clasificacion = new[] { Ventana(10, Escenario.Alcista), Ventana(20, Escenario.Bajista), Ventana(30, Escenario.Lateral), Ventana(40, Escenario.Ambiguo) };

        var resultado = AsignadorOperacionRegimen.Asignar(operaciones, clasificacion);

        Assert(resultado.Count == operaciones.Length, $"Conservacion esperada {operaciones.Length}, obtuvo {resultado.Count}");
        Assert(resultado.Select(r => r.OperacionId).ToHashSet().SetEquals(operaciones.Select(o => o.OperacionId)),
            "Todos los OperacionId de entrada deben aparecer exactamente una vez en la salida");
    }

    private static void VerificarDeterminismo()
    {
        var operaciones = new[]
        {
            Op(1, entrada: 10, resolucion: 20),
            Op(2, entrada: 20, resolucion: 30, gano: false, martingalas: 1),
        };
        var clasificacion = new[] { Ventana(10, Escenario.Alcista), Ventana(20, Escenario.Bajista), Ventana(30, Escenario.Ambiguo) };

        var r1 = AsignadorOperacionRegimen.Asignar(operaciones, clasificacion);
        var r2 = AsignadorOperacionRegimen.Asignar(operaciones, clasificacion);

        Assert(r1.Count == r2.Count, "Ambas ejecuciones deben producir el mismo numero de filas");
        for (var i = 0; i < r1.Count; i++)
        {
            Assert(r1[i].RegimenEntrada == r2[i].RegimenEntrada, $"RegimenEntrada debe coincidir en la fila {i}");
            Assert(r1[i].RegimenResolucion == r2[i].RegimenResolucion, $"RegimenResolucion debe coincidir en la fila {i}");
        }
    }

    private static void Assert(bool condicion, string mensaje)
    {
        if (!condicion) throw new Exception(mensaje);
    }
}
