using System.Text;
using TD_Project.AnalisisMultiTimeframe;

namespace TD_Project.Protocolo;

// Fase 1.6-B (ESPECIFICACION_REPORTE_CONSOLIDADO_V1.md): resumen + referencias a anexos. D-050:
// no incluye ninguna metrica dependiente de timeframe (eficiencia operacional, winrate, escenarios)
// directamente — remite a la seccion 4 (multi-timeframe, agregado) o a los anexos (detalle por
// timeframe). D-051: los anexos son la fuente primaria de detalle, el resumen no los replica.
public static class ReporteConsolidadoGenerador
{
    public static string Generar(ResultadoProtocolo resultado)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Reporte Experimental Consolidado — {resultado.Estrategia}");
        sb.AppendLine();

        sb.AppendLine("1. Identificación");
        sb.AppendLine($"   Estrategia: {resultado.Estrategia}");
        sb.AppendLine($"   Versión: {resultado.Identidad.VersionEstrategia}");
        sb.AppendLine($"   Parámetros: {string.Join(", ", resultado.Identidad.Parametros)}");
        sb.AppendLine($"   Versión de protocolo: {resultado.Identidad.VersionProtocolo}");
        sb.AppendLine($"   Versión de clasificador de régimen: {resultado.Identidad.ClasificadorRegimenVersion}");
        sb.AppendLine($"   Hash de identidad experimental: {resultado.Identidad.HashCompuesto}");
        sb.AppendLine();

        sb.AppendLine("2. Validación experimental");
        sb.AppendLine($"   {"Timeframe",-10}{"Estado",-14}Motivo (si aplica)");
        foreach (var c in resultado.Corridas)
            sb.AppendLine($"   {c.Timeframe,-10}{c.Estado,-14}{c.MotivoFallo ?? "-"}");
        sb.AppendLine();

        sb.AppendLine("3. Resultado operacional");
        sb.AppendLine("   Ver sección 4 — no existe una única cifra global cuando la métrica depende del timeframe (D-050).");
        sb.AppendLine();

        sb.AppendLine("4. Multi-timeframe");
        if (resultado.MultiTimeframe is null)
        {
            sb.AppendLine("   Sin datos — ningún timeframe completó la corrida con Estado=Success.");
        }
        else
        {
            var mt = resultado.MultiTimeframe;
            sb.AppendLine($"   {"Timeframe",-10}{"Completadas",12}{"Eficiencia%",12}{"%Marting",9}{"RachaMax",9}");
            foreach (var f in mt.Filas)
                sb.AppendLine($"   {f.Timeframe,-10}{f.IntentosCompletados,12}{f.EficienciaOperacionalPct,11:F2}%{f.PctResueltasPorMartingala,8:F1}%{f.MayorRachaNegativa,9}");
            sb.AppendLine($"   Consistencia: {mt.ConsistenciaEficienciaOperacional.TimeframeMinimo}={mt.ConsistenciaEficienciaOperacional.ValorMinimo:F2}% .. {mt.ConsistenciaEficienciaOperacional.TimeframeMaximo}={mt.ConsistenciaEficienciaOperacional.ValorMaximo:F2}% (amplitud {mt.ConsistenciaEficienciaOperacional.AmplitudPuntosPorcentuales:F2}pp)");
            sb.AppendLine($"   Mejor resultado observado: {mt.MejorResultadoObservado.Timeframe} ({mt.MejorResultadoObservado.EficienciaOperacionalPct:F2}%, muestra={mt.MejorResultadoObservado.IntentosCompletados})");
            sb.AppendLine($"   Mayor evidencia: {mt.MayorEvidencia.Timeframe} (muestra={mt.MayorEvidencia.IntentosCompletados})");
            sb.AppendLine("   Nota: ambas dimensiones pueden apuntar a timeframes distintos — separación intencional, no se combinan en un ranking único (D-014).");
        }
        sb.AppendLine();

        sb.AppendLine("5. Escenarios de mercado");
        sb.AppendLine($"   Evaluado en {resultado.Corridas.Count(c => c.Estado == EstadoCorridaTimeframe.Success)} timeframe(s) con Estado=Success.");
        sb.AppendLine("   Ver anexo correspondiente para el detalle completo (vista por régimen de entrada, vista por");
        sb.AppendLine("   régimen de resolución, nota de correlación≠causalidad D-037):");
        foreach (var c in resultado.Corridas)
        {
            var referencia = c.Estado == EstadoCorridaTimeframe.Success
                ? $"REPORTE_EXPERIMENTAL_ESTRATEGIA_V1_ANEXO_{c.Timeframe}.md"
                : "sin anexo — ver estado en sección 2";
            sb.AppendLine($"   - {c.Timeframe,-6} → {referencia}");
        }
        sb.AppendLine();

        sb.AppendLine("6. Limitaciones");
        sb.AppendLine("   Modelo económico incompleto (sin costes reales, sin margen completo). Sin interpretación");
        sb.AppendLine("   financiera — este reporte no responde \"¿debo invertir dinero en esta estrategia?\".");
        sb.AppendLine("   Datos derivados (equity, retorno%) no son comparables financieramente entre estrategias.");

        return sb.ToString();
    }
}
