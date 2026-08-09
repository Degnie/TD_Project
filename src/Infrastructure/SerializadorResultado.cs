using System.Text.Json;
using TD_Project.Application;

namespace TD_Project.Infrastructure;

// spec: RNF-13 — Deserializar(Serializar(Result)) == Result. Formato: JSON (System.Text.Json, stdlib de .NET).
public static class SerializadorResultado
{
    public static string Serializar(ResultadoBacktest resultado) =>
        JsonSerializer.Serialize(resultado);

    public static ResultadoBacktest Deserializar(string serializado) =>
        JsonSerializer.Deserialize<ResultadoBacktest>(serializado)
            ?? throw new InvalidOperationException("No se pudo deserializar el ResultadoBacktest.");
}
