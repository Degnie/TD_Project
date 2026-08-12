using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;
using TD_Project.ModeloFinanciero;
using TD_Project.Protocolo;

namespace TD_Project.Caso5;

// spec: Caso 5B D-114 (DECISIONES_CASO5B_V1.md) — una fila por gestor comparado. Metricas es la
// unica fuente de datos (MetricasFinancieras, D-072/D-077) — nunca ReporteOperacional (excluido
// explicitamente por su acoplamiento a martingala, D-055). Estado permite que una corrida
// individual falle sin invalidar la comparacion completa (mismo principio que EjecutorProtocolo
// ya aplica a timeframes).
public sealed record FilaComparacionGestor(
    string IdentidadGestor,
    EstadoCorridaTimeframe Estado,
    MetricasFinancieras? Metricas);

// spec: Caso 5B D-114 — Filas conserva el orden de la lista de gestores recibida (D-112: orden de
// entrada = orden de presentacion, nunca reordenado por valor de metrica).
public sealed record ResultadoComparativoGestores(
    string Estrategia,
    string Timeframe,
    string NombreDataset,
    IReadOnlyList<FilaComparacionGestor> Filas);

// spec: Caso 5B D-112 (DECISIONES_CASO5B_V1.md) — componente nuevo de laboratorio, mismo patron
// que ComparadorMultiTimeframe (patron conceptual compartido, sin dependencia de codigo). No toca
// EjecutorProtocolo/EntradaProtocolo/src/.
public static class ComparadorGestores
{
    // spec: D-113 — entradaBase fija estrategia/dataset/timeframe/instrumento/costes; el unico eje
    // que varia es el gestor. entradaBase.Sizing debe ser null (evita una configuracion "por
    // defecto" ambigua que de todos modos se descartaria). entradaBase.Timeframes debe tener
    // exactamente 1 elemento (D-113 fija la unidad de comparacion a un timeframe; comparacion
    // multi-timeframe es alcance futuro no autorizado aqui).
    public static ResultadoComparativoGestores Comparar(EntradaProtocolo entradaBase, IReadOnlyList<IGestorRiesgo> gestores)
    {
        if (entradaBase.Sizing is not null)
            throw new ArgumentException("entradaBase.Sizing debe ser null — el gestor activo se determina exclusivamente por la lista 'gestores'.", nameof(entradaBase));
        if (entradaBase.Timeframes.Count != 1)
            throw new ArgumentException("entradaBase.Timeframes debe declarar exactamente 1 timeframe — D-113 fija la unidad de comparacion a un timeframe unico.", nameof(entradaBase));
        if (gestores.Count == 0)
            throw new ArgumentException("Se requiere al menos un gestor para comparar.", nameof(gestores));
        if (gestores.Any(g => g is null))
            throw new ArgumentException("Ningun elemento de 'gestores' puede ser null.", nameof(gestores));

        var timeframe = entradaBase.Timeframes[0];
        var filas = new List<FilaComparacionGestor>(gestores.Count);

        foreach (var gestor in gestores)
        {
            if (gestor is not IIdentidadGestorRiesgo identidad)
                throw new InvalidOperationException(
                    $"GestorRiesgo de tipo '{gestor.GetType().Name}' no implementa IIdentidadGestorRiesgo — " +
                    "no puede participar en una comparacion sin identidad reproducible.");

            // record `with`: construye una copia nueva de entradaBase, nunca muta el objeto
            // original — entradaBase permanece identica en cada iteracion del bucle (inmutabilidad
            // estructural de C# records, no solo disciplina de codigo).
            var entradaVariante = entradaBase with { Sizing = new ConfiguracionSizing(gestor) };
            var resultado = EjecutorProtocolo.Ejecutar(entradaVariante);
            var corridaDelTimeframe = resultado.Corridas.Single(c => c.Timeframe == timeframe);

            filas.Add(new FilaComparacionGestor(identidad.ObtenerIdentidadConfiguracion(), corridaDelTimeframe.Estado, corridaDelTimeframe.MetricasFinancieras));
        }

        return new ResultadoComparativoGestores(entradaBase.Estrategia, timeframe, entradaBase.NombreDataset, filas);
    }
}

// spec: Caso 5B D-115 — render de tabla separado del objeto comparativo (mismo principio D-072/
// D-077 aplicado a la capa de presentacion: CalculadoraMetricasFinancieras/ReporteFinancieroGenerador
// ya establecen esta separacion). Sin ninguna columna/fila que sugiera "mejor"/"peor"/ranking.
public static class RenderizadorComparacionGestores
{
    public static string Generar(ResultadoComparativoGestores resultado)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== Comparación de gestores de riesgo — {resultado.Estrategia} / {resultado.Timeframe} / {resultado.NombreDataset} ===");
        sb.AppendLine();

        foreach (var fila in resultado.Filas)
        {
            sb.AppendLine($"Gestor: {fila.IdentidadGestor}");
            sb.AppendLine($"  Estado: {fila.Estado}");
            if (fila.Metricas is { } m)
            {
                sb.AppendLine($"  PnLTotal: {m.PnLTotal}");
                sb.AppendLine($"  DrawdownMaximoPct: {(m.DrawdownMaximoPct is { } d ? d.ToString() : "null")}");
                sb.AppendLine($"  ProfitFactor: {(m.ProfitFactor is { } pf ? pf.ToString() : "null")}");
                sb.AppendLine($"  ExposicionMaxima: {m.ExposicionMaxima}");
                sb.AppendLine($"  CashFinal: {m.CashFinal}");
                sb.AppendLine($"  EquityFinal: {m.EquityFinal}");
            }
            else
            {
                sb.AppendLine("  Métricas: (no disponibles — corrida no exitosa)");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
