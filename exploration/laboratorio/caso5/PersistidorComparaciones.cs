namespace TD_Project.Caso5;

// spec: Caso 5C D-116 (DECISIONES_CASO5C_V1.md) — envuelve ComparadorGestores, no lo modifica.
// Extension directa del patron ya verificado en protocolo/Program.cs:48-72 (carpeta timestamped +
// JSON de identidad + reporte). No falla la comparacion en memoria si la escritura a disco falla
// (D-059/D-096: fallo de capa secundaria no bloquea la capa principal) — el resultado recibido ya
// esta calculado antes de llamar a este metodo, por lo que una excepcion aqui nunca lo invalida.
public static class PersistidorComparaciones
{
    // Devuelve la ruta de la carpeta escrita (mismo patron que protocolo/Program.cs:71).
    public static string Persistir(string dirResultados, ResultadoComparativoGestores resultado)
    {
        var carpeta = Path.Combine(
            dirResultados,
            $"{resultado.Estrategia.Replace(" ", "")}_{resultado.Timeframe}_{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
        Directory.CreateDirectory(carpeta);

        var gestoresJson = string.Join(",\n    ", resultado.Filas.Select(f =>
            $$"""{ "identidad": "{{f.IdentidadGestor}}", "estado": "{{f.Estado}}" }"""));

        var identidadJson = $$"""
            {
              "estrategia": "{{resultado.Estrategia}}",
              "timeframe": "{{resultado.Timeframe}}",
              "nombreDataset": "{{resultado.NombreDataset}}",
              "gestores": [
                {{gestoresJson}}
              ],
              "fechaGeneracionUtc": "{{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}}"
            }
            """;
        File.WriteAllText(Path.Combine(carpeta, "IDENTIDAD_COMPARACION.json"), identidadJson);
        File.WriteAllText(Path.Combine(carpeta, "COMPARACION_GESTORES_V1.md"), RenderizadorComparacionGestores.Generar(resultado));

        return carpeta;
    }
}
