using System.Text;
using TD_Project.AnalisisEscenariosMercado;
using TD_Project.EvaluacionMultiTf;

namespace TD_Project.ReporteEscenariosMercado;

// Fase 1.5-B (ESPECIFICACION_REPORTE_ESCENARIOS_MERCADO_V2.md): capa de presentacion pura sobre
// ReporteMetricasPorEscenario (Paso 3, ya calculado). No recalcula ninguna metrica (D-015). No
// genera conclusiones comparativas (D-047) ni ranking (D-014/D-009). Estructura fija de 4 bloques
// (V2 §3): resumen general, vista por regimen de entrada, vista por regimen de resolucion, nota
// metodologica obligatoria de correlacion != causalidad (D-037).
public static class ReporteEscenariosGenerador
{
    public static string Generar(
        IdentidadExperimento identidad, int operacionesCompletadasTotal, ReporteMetricasPorEscenario metricas)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Reporte de Escenarios de Mercado — {identidad.Estrategia} / {identidad.Timeframe}");
        sb.AppendLine();

        sb.AppendLine("1. Resumen general");
        sb.AppendLine($"   Operaciones completadas de la corrida: {operacionesCompletadasTotal}");
        sb.AppendLine();

        sb.AppendLine("2. Vista por régimen de entrada");
        sb.AppendLine("   ¿Bajo qué contexto de mercado decidió actuar la estrategia?");
        EscribirTabla(sb, metricas.PorRegimenEntrada);
        sb.AppendLine();

        sb.AppendLine("3. Vista por régimen de resolución");
        sb.AppendLine("   ¿En qué contexto de mercado terminó cada operación?");
        EscribirTabla(sb, metricas.PorRegimenResolucion);
        sb.AppendLine();

        sb.AppendLine("4. Nota metodológica obligatoria (D-037)");
        sb.AppendLine("   La clasificación de régimen describe una coincidencia temporal observada entre");
        sb.AppendLine("   comportamiento de mercado y resultados de estrategia. No demuestra que el régimen");
        sb.AppendLine("   sea la causa del resultado. El dataset corresponde a un único periodo histórico;");
        sb.AppendLine("   los regímenes no están distribuidos de forma experimentalmente controlada.");

        return sb.ToString();
    }

    private static void EscribirTabla(StringBuilder sb, VistaPorEscenario vista)
    {
        sb.AppendLine($"   {"Régimen",-12}{"Operaciones",12}{"Eficiencia%",12}{"Inicial",8}{"M1",6}{"M2",6}{"Agotó",7}{"%Marting",9}");
        foreach (var fila in vista.Filas)
        {
            var nombre = EtiquetaRegimen(fila.Regimen);
            sb.AppendLine($"   {nombre,-12}{fila.OperacionesCompletadas,12}{fila.EficienciaOperacionalPct,11:F2}%{fila.GanoInicial,8}{fila.GanoM1,6}{fila.GanoM2,6}{fila.PerdioAgotando,7}{fila.PctResueltasPorMartingala,8:F1}%");
        }
        sb.AppendLine($"   Total: {vista.TotalOperaciones} operaciones (partición exhaustiva — debe igualar el resumen general del bloque 1)");
    }

    // Ambiguo (V2 §5): regimen calculado por el clasificador, evidencia insuficiente.
    // Sin regimen (V2 §5): ausencia de dato, vela fuera de la ventana evaluable del clasificador.
    private static string EtiquetaRegimen(Escenario? regimen) => regimen switch
    {
        Escenario.Ambiguo => "Ambiguo*",
        null => "Sin régimen",
        _ => regimen.ToString()!,
    };
}
