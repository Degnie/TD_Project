using System.Text;
using TD_Project.Protocolo;

namespace TD_Project.ModeloFinanciero;

// spec: Caso 2 D-080 — reporte financiero independiente de ReporteConsolidadoGenerador (Caso 1,
// congelado en VERSION_EXPERIMENTAL_CASO1_V1.md, no modificado). Presenta MetricasFinancieras por
// timeframe junto a la configuracion economica activa (Instrumento/Costes/Sizing, D-079). No
// recalcula ninguna metrica (D-077) — solo lee ResultadoCorridaTimeframe.MetricasFinancieras.
public static class ReporteFinancieroGenerador
{
    public static string Generar(ResultadoProtocolo resultado, EntradaProtocolo entrada)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Reporte Financiero Experimental — {resultado.Estrategia}");
        sb.AppendLine();

        sb.AppendLine("1. Identificación");
        sb.AppendLine($"   Estrategia: {resultado.Estrategia}");
        sb.AppendLine($"   Versión: {resultado.Identidad.VersionEstrategia}");
        sb.AppendLine($"   Parámetros: {string.Join(", ", resultado.Identidad.Parametros)}");
        sb.AppendLine($"   Versión de protocolo: {resultado.Identidad.VersionProtocolo}");
        sb.AppendLine($"   Versión de clasificador de régimen: {resultado.Identidad.ClasificadorRegimenVersion}");
        sb.AppendLine($"   Hash de identidad experimental (estrategia/dataset/protocolo): {resultado.Identidad.HashCompuesto}");
        sb.AppendLine($"   Hash de configuración económica (Instrumento/Costes/Sizing, D-082): {resultado.Identidad.HashConfiguracionEconomica}");
        sb.AppendLine();

        sb.AppendLine("2. Configuración económica activa (Caso 2)");
        var instrumento = entrada.Instrumento ?? Domain.Shared.Instrumento.Default;
        sb.AppendLine($"   Instrumento: Simbolo={instrumento.Simbolo}, TasaMargen={instrumento.TasaMargen}");
        var costes = entrada.Costes ?? Domain.Shared.ConfiguracionCostes.Default;
        sb.AppendLine($"   Costes: TasaComision={costes.TasaComision}, TasaSlippage={costes.TasaSlippage}");
        sb.AppendLine(entrada.Sizing is not null
            ? $"   Sizing: PorcentajeRiesgo={entrada.Sizing.PorcentajeRiesgo} (GestorCapital activo)"
            : "   Sizing: inactivo (Cantidad de la estrategia sin ajustar)");
        sb.AppendLine();

        sb.AppendLine("3. Métricas financieras por timeframe (unidades monetarias experimentales, D-058)");
        foreach (var c in resultado.Corridas)
        {
            if (c.Estado != EstadoCorridaTimeframe.Success || c.MetricasFinancieras is null)
            {
                sb.AppendLine($"   {c.Timeframe,-6} Estado={c.Estado} — sin métricas financieras (D-078: no disponible, no 0)");
                continue;
            }

            var m = c.MetricasFinancieras;
            sb.AppendLine($"   {c.Timeframe}");
            sb.AppendLine($"     Capital inicial:    {m.CapitalInicial:F2}");
            sb.AppendLine($"     Cash final:         {m.CashFinal:F2}");
            sb.AppendLine($"     Equity final:       {m.EquityFinal:F2}");
            sb.AppendLine($"     PnL total:          {m.PnLTotal:F2}");
            sb.AppendLine($"     Drawdown máximo:    {(m.DrawdownMaximoPct.HasValue ? $"{m.DrawdownMaximoPct.Value:P2}" : "no disponible")}");
            sb.AppendLine($"     Exposición máxima:  {m.ExposicionMaxima:F2}");
        }
        sb.AppendLine();

        sb.AppendLine("4. Límites (D-076/D-058)");
        sb.AppendLine("   Ninguna cifra de esta sección representa dinero real ni retorno financiero real —");
        sb.AppendLine("   unidad monetaria experimental (D-058). Sin ranking ni comparación de superioridad entre");
        sb.AppendLine("   timeframes o estrategias (D-076). El porcentaje de sizing, si está activo, es un parámetro");
        sb.AppendLine("   experimental fijo, no una optimización.");
        sb.AppendLine();

        sb.AppendLine("5. Advertencia — escala económica histórica (D-085)");
        sb.AppendLine("   Las métricas financieras absolutas representan la ejecución del modelo económico");
        sb.AppendLine("   experimental bajo la configuración histórica de tamaño de posición. No constituyen una");
        sb.AppendLine("   simulación de capital real ni una recomendación de dimensionamiento. La estrategia usa");
        sb.AppendLine("   una cantidad nominal fija (Cantidad=1) sin relación dimensional explícita con el capital");
        sb.AppendLine("   inicial — deuda técnica registrada en D-085, no resuelta en Caso 2 V1.");

        return sb.ToString();
    }
}
