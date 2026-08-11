using TD_Project.AnalisisEscenariosMercado;
using TD_Project.Exploration;

namespace TD_Project.ReporteEscenariosMercado;

// Fase 1.5-A, Paso 2 (ESPECIFICACION_ASIGNACION_OPERACION_REGIMEN_V1.md): capa de solo lectura que
// vincula una InfoOperacionResuelta ya resuelta (Paso 1) con el regimen de mercado ya congelado
// (ClasificadorRegimenV1, Fase 1.4-B). No calcula metricas (D-039) — ese es el Paso 3. No modifica
// InfoOperacionResuelta ni ClasificadorRegimenV1.cs.

// Escenario? nullable (seccion 3 de la especificacion): distingue "Ambiguo" (regimen calculado)
// de "sin regimen" (dato no disponible — calentamiento del clasificador o clasificador vacio).
public sealed record OperacionConRegimen(
    int OperacionId,
    bool Gano,
    int MartingalasUsadas,
    long TimestampEntrada,
    long TimestampResolucion,
    Escenario? RegimenEntrada,
    Escenario? RegimenResolucion);

public static class AsignadorOperacionRegimen
{
    // D-042: coincidencia exacta de InicioUtcMs, sin tolerancia ni vecino mas cercano.
    public static IReadOnlyList<OperacionConRegimen> Asignar(
        IReadOnlyList<InfoOperacionResuelta> operaciones, IReadOnlyList<VentanaClasificada> clasificacion)
    {
        var porTimestamp = clasificacion.ToDictionary(v => v.InicioUtcMs, v => v.Escenario);

        var resultado = new List<OperacionConRegimen>(operaciones.Count);
        foreach (var op in operaciones)
        {
            var regimenEntrada = porTimestamp.TryGetValue(op.TimestampEntrada, out var eEntrada) ? eEntrada : (Escenario?)null;
            var regimenResolucion = porTimestamp.TryGetValue(op.TimestampResolucion, out var eResolucion) ? eResolucion : (Escenario?)null;

            resultado.Add(new OperacionConRegimen(
                OperacionId: op.OperacionId,
                Gano: op.Gano,
                MartingalasUsadas: op.MartingalasUsadas,
                TimestampEntrada: op.TimestampEntrada,
                TimestampResolucion: op.TimestampResolucion,
                RegimenEntrada: regimenEntrada,
                RegimenResolucion: regimenResolucion));
        }
        return resultado;
    }
}
