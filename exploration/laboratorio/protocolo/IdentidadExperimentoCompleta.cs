using System.Security.Cryptography;
using System.Text;
using TD_Project.Domain.Shared;

namespace TD_Project.Protocolo;

// Fase 1.6-C (ESPECIFICACION_PIPELINE_EXPERIMENTAL_V1.md §4, D-049): identifica de forma unica la
// CONFIGURACION de una corrida (que se evaluo, con que parametros, sobre que dataset, con que
// version de clasificador y protocolo) — no identifica el resultado. No modifica
// IdentidadExperimento (Fase 1.2) ni ningun contrato existente; es un tipo adicional del pipeline.
// spec: Caso 2 D-082 — HashCompuesto mantiene su semantica y texto de calculo ORIGINALES sin
// tocar (preserva baseline_final/ de Caso 1 bit-a-bit: A48CCC57DA1919F533F4D532FDC0F945705681D
// CDA813B385BBFE7F44F40998E). La configuracion economica (Instrumento/Costes/Sizing efectivos) se
// identifica con un segundo hash independiente, HashConfiguracionEconomica — un unico hash no
// puede incluir campos nuevos sin cambiar para toda entrada, incluida la historica (SHA256 no
// admite "campos nuevos con el mismo valor por defecto producen el mismo hash"). Intento previo
// de fusionar ambos en HashCompuesto fue revertido tras verificar que rompia el hash congelado de
// Caso 1 (E013F02E... en vez de A48CCC57...) — hallazgo registrado en D-082.
public sealed record IdentidadExperimentoCompleta(
    string Estrategia,
    string VersionEstrategia,
    IReadOnlyList<string> Parametros,
    string DatasetSourceSha256,
    string ClasificadorRegimenVersion,
    string VersionProtocolo,
    string HashCompuesto,
    string HashConfiguracionEconomica)
{
    // Orden fijo y documentado (no configurable) — el mismo orden en cada calculo es lo que
    // garantiza que dos ejecuciones con los mismos datos produzcan el mismo hash. Delimitador " "
    // no puede aparecer dentro de un hash hexadecimal ni (por convencion del proyecto) dentro de
    // un nombre de estrategia o de un parametro serializado ("clave=valor").
    public static IdentidadExperimentoCompleta Calcular(
        string estrategia, string versionEstrategia, IReadOnlyList<string> parametros,
        string datasetSourceSha256, string clasificadorRegimenVersion, string versionProtocolo,
        Instrumento? instrumento = null, ConfiguracionCostes? costes = null,
        ConfiguracionSizing? sizing = null)
    {
        var textoParaHash = string.Join(" ",
            estrategia, versionEstrategia, string.Join(",", parametros),
            datasetSourceSha256, clasificadorRegimenVersion, versionProtocolo);

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(textoParaHash)));
        var hashEconomico = CalcularHashConfiguracionEconomica(instrumento, costes, sizing);

        return new IdentidadExperimentoCompleta(
            estrategia, versionEstrategia, parametros, datasetSourceSha256,
            clasificadorRegimenVersion, versionProtocolo, hash, hashEconomico);
    }

    // spec: D-082 — instrumento/costes/sizing EFECTIVOS (null -> Default) antes de entrar al
    // texto, para que "no configurar nada" y "configurar explicitamente el default" produzcan el
    // mismo HashConfiguracionEconomica (misma corrida logica economica). Sizing inactivo
    // (Default=null en si mismo, ver ConfiguracionSizing.cs) usa el literal "sin-sizing", nunca un
    // ConfiguracionSizing con PorcentajeRiesgo=0 (serian dos configuraciones distintas).
    private static string CalcularHashConfiguracionEconomica(
        Instrumento? instrumento, ConfiguracionCostes? costes, ConfiguracionSizing? sizing)
    {
        var instrumentoEfectivo = instrumento ?? Instrumento.Default;
        var costesEfectivos = costes ?? ConfiguracionCostes.Default;
        var textoSizing = sizing is null ? "sin-sizing" : $"sizing:{sizing.PorcentajeRiesgo}";

        var texto = string.Join(" ",
            $"instrumento:{instrumentoEfectivo.Simbolo}:{instrumentoEfectivo.TasaMargen}",
            $"costes:{costesEfectivos.TasaComision}:{costesEfectivos.TasaSlippage}",
            textoSizing);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto)));
    }
}
