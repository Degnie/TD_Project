using TD_Project.AnalisisOperacional;
using TD_Project.Protocolo;

namespace TD_Project.Caso3;

// spec: Caso 3 D-055/D-088 — capa de presentacion nueva, no modifica AnalizadorOperacional.cs ni
// ResolucionDeIntentos (record, tipo, formulas de PctSeguro intactos). Traduce "no aplica" cuando
// CaracteristicasEstrategia.UsaMartingala == false, distinguiendolo de un 0% real — mismo
// principio D-078 (Caso 2: null != 0), aplicado aqui a nivel de texto porque ResolucionDeIntentos
// sigue siendo decimal (el motor de calculo no cambia, D-088).
public static class PresentadorResolucionIntentos
{
    public static string Formatear(ResolucionDeIntentos resolucion, CaracteristicasEstrategia? caracteristicas)
    {
        // caracteristicas is null = "no declarado" (D-090, EntradaProtocolo.Caracteristicas=null
        // por defecto) -> se presenta el valor real, sin asumir aplicabilidad ni inaplicabilidad.
        if (caracteristicas is not null && !caracteristicas.UsaMartingala)
            return "no aplica (estrategia sin martingala, D-055/D-088)";

        return $"VictoriaInicial={resolucion.VictoriaInicialPct:F1}% M1={resolucion.RecuperacionM1Pct:F1}% " +
               $"M2={resolucion.RecuperacionM2Pct:F1}% PerdidaAgotando={resolucion.PerdidaAgotandoPct:F1}% " +
               $"%Marting={resolucion.PctResueltasPorMartingala:F1}%";
    }
}
