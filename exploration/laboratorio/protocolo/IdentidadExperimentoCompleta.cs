using System.Security.Cryptography;
using System.Text;

namespace TD_Project.Protocolo;

// Fase 1.6-C (ESPECIFICACION_PIPELINE_EXPERIMENTAL_V1.md §4, D-049): identifica de forma unica la
// CONFIGURACION de una corrida (que se evaluo, con que parametros, sobre que dataset, con que
// version de clasificador y protocolo) — no identifica el resultado. No modifica
// IdentidadExperimento (Fase 1.2) ni ningun contrato existente; es un tipo adicional del pipeline.
public sealed record IdentidadExperimentoCompleta(
    string Estrategia,
    string VersionEstrategia,
    IReadOnlyList<string> Parametros,
    string DatasetSourceSha256,
    string ClasificadorRegimenVersion,
    string VersionProtocolo,
    string HashCompuesto)
{
    // Orden fijo y documentado (no configurable) — el mismo orden en cada calculo es lo que
    // garantiza que dos ejecuciones con los mismos datos produzcan el mismo hash. Delimitador " "
    // no puede aparecer dentro de un hash hexadecimal ni (por convencion del proyecto) dentro de
    // un nombre de estrategia o de un parametro serializado ("clave=valor").
    public static IdentidadExperimentoCompleta Calcular(
        string estrategia, string versionEstrategia, IReadOnlyList<string> parametros,
        string datasetSourceSha256, string clasificadorRegimenVersion, string versionProtocolo)
    {
        var textoParaHash = string.Join(" ",
            estrategia, versionEstrategia, string.Join(",", parametros),
            datasetSourceSha256, clasificadorRegimenVersion, versionProtocolo);

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(textoParaHash)));

        return new IdentidadExperimentoCompleta(
            estrategia, versionEstrategia, parametros, datasetSourceSha256,
            clasificadorRegimenVersion, versionProtocolo, hash);
    }
}
