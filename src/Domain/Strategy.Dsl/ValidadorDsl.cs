using System.Text.Json;

namespace TD_Project.Domain.Strategy.Dsl;

// spec: RN-16 — evaluacion puramente declarativa; el DSL prohibe explicitamente la ejecucion de
// codigo arbitrario, llamadas al sistema o referencias a datos futuros (N+k).
public sealed record ResultadoValidacionDsl(bool EsValido, string? Motivo);

public static class ValidadorDsl
{
    private static readonly string[] IndicadoresPermitidos = { "SMA" };
    private static readonly string[] CamposPermitidos = { "Close", "Open", "High", "Low" };
    private static readonly string[] OperadoresPermitidos = { ">", "<", ">=", "<=" };

    public static ResultadoValidacionDsl Validar(string json)
    {
        JsonDocument documento;
        try
        {
            documento = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return new ResultadoValidacionDsl(false, "JSON malformado.");
        }

        using (documento)
        {
            var raiz = documento.RootElement;

            if (!raiz.TryGetProperty("condicion", out var condicion) || !raiz.TryGetProperty("accion", out var accion))
                return new ResultadoValidacionDsl(false, "El documento debe declarar 'condicion' y 'accion'.");

            // spec: RN-16 — prohibicion explicita de comandos de ejecucion externa
            if (condicion.TryGetProperty("comando", out _))
                return new ResultadoValidacionDsl(false, "El campo 'comando' no esta permitido: ejecucion de codigo externo prohibida.");

            // spec: RN-16 — prohibicion explicita de referencias look-ahead (N+k)
            if (condicion.TryGetProperty("offset", out _))
                return new ResultadoValidacionDsl(false, "El campo 'offset' no esta permitido: referencia look-ahead prohibida.");

            if (!condicion.TryGetProperty("indicador", out var indicadorEl) || !IndicadoresPermitidos.Contains(indicadorEl.GetString()))
                return new ResultadoValidacionDsl(false, "Indicador no soportado o ausente.");

            if (!condicion.TryGetProperty("periodo", out var periodoEl) || periodoEl.GetInt32() <= 0)
                return new ResultadoValidacionDsl(false, "Periodo debe ser un entero positivo.");

            if (!condicion.TryGetProperty("operador", out var operadorEl) || !OperadoresPermitidos.Contains(operadorEl.GetString()))
                return new ResultadoValidacionDsl(false, "Operador no soportado o ausente.");

            if (!condicion.TryGetProperty("campo", out var campoEl) || !CamposPermitidos.Contains(campoEl.GetString()))
                return new ResultadoValidacionDsl(false, "Campo no soportado o ausente.");

            if (!accion.TryGetProperty("side", out var sideEl) || (sideEl.GetString() != "Buy" && sideEl.GetString() != "Sell"))
                return new ResultadoValidacionDsl(false, "Side de la accion invalido o ausente.");

            if (!accion.TryGetProperty("type", out var typeEl) || typeEl.GetString() != "Market")
                return new ResultadoValidacionDsl(false, "Type de la accion invalido o ausente (solo 'Market' soportado en V1).");

            return new ResultadoValidacionDsl(true, null);
        }
    }
}
