using TD_Project.EvaluacionMultiTf;

namespace TD_Project.AnalisisEscenariosMercado;

// Fase 1.4-B, Paso 3-B: version OFICIAL congelada del clasificador de regimen. Separado de
// ClasificadorAdxExperimental.cs (que permanece como exploracion/comparacion, Fase 1.4-A) —
// D-029: ningun clasificador es oficial mientras no cumpla el modelo de 4 estados aprobado por
// D-028. Este archivo si lo cumple. Parametros congelados por D-030/D-031/D-032/D-033:
//   - PeriodoAdx = 14 (estandar Wilder)
//   - UmbralAdxTendencia = 25 (convencion de literatura)
//   - Metodo SesgoDI = relativo, |DI+-DI-|/(DI+ + DI-) (D-031)
//   - UmbralSesgoDI = 0.153467 (D-032/D-033, mediana calculada sobre BTCUSDT_2024-01-02_2025-01-02,
//     ver RESULTADO_CALIBRACION_UMBRAL_SESGO_DI_V1.md)
// No conoce ninguna estrategia (D-016) — solo consume OHLC via VelaDerivadaCruda.
public static class ClasificadorRegimenV1
{
    // D-052 (auditoria, Fase 1.6-C): metadata de identificacion pura, no logica. No cambia
    // algoritmo, parametros, estados ni salida — verificado por hash de clasificacion completa
    // antes/despues de este cambio (ver TestsClasificadorRegimenV1.VerificarHashClasificacionSinCambios).
    public const string Version = "V1";

    public const int PeriodoAdx = 14;
    public const decimal UmbralAdxTendencia = 25m;
    public const decimal UmbralSesgoDI = 0.153467m;

    public static IReadOnlyList<VentanaClasificada> Clasificar(IReadOnlyList<VelaDerivadaCruda> velas) =>
        Clasificar(velas, PeriodoAdx, UmbralAdxTendencia, UmbralSesgoDI);

    // Sobrecarga con parametros explicitos: unicamente para pruebas/reproducibilidad — el uso
    // oficial del laboratorio es Clasificar(velas) con los 3 valores congelados arriba.
    public static IReadOnlyList<VentanaClasificada> Clasificar(
        IReadOnlyList<VelaDerivadaCruda> velas, int periodoAdx, decimal umbralAdxTendencia, decimal umbralSesgoDI)
    {
        var serie = CalibradorUmbralSesgoDI.CalcularSerie(velas, periodoAdx);
        var resultado = new List<VentanaClasificada>(serie.Count);

        foreach (var muestra in serie)
        {
            var escenario = Clasificar(muestra.Adx, muestra.DiMas, muestra.DiMenos, umbralAdxTendencia, umbralSesgoDI);
            // FinUtcMsExclusivo: la muestra representa el cierre de la ventana en InicioUtcMs;
            // se reporta como punto (Inicio=Fin) porque CalibradorUmbralSesgoDI.CalcularSerie ya
            // expone una serie por vela, no por rango — consistente con como el candidato B
            // exploratorio reportaba sus VentanaClasificada en Fase 1.4-A (par de timestamps
            // consecutivos delimitando la vela evaluada).
            resultado.Add(new VentanaClasificada(muestra.InicioUtcMs, muestra.InicioUtcMs, escenario));
        }

        return resultado;
    }

    // spec-lab: PARAMETRIZACION_CLASIFICADOR_REGIMEN_V1.md §2.4-2.6 y §3 (tratamiento de bordes).
    private static Escenario Clasificar(decimal adx, decimal diMas, decimal diMenos, decimal umbralAdxTendencia, decimal umbralSesgoDI)
    {
        var sumaDi = diMas + diMenos;

        if (adx >= umbralAdxTendencia)
        {
            if (diMas == diMenos)
                return Escenario.Ambiguo; // §3: empate exacto con tendencia confirmada, direccion indeterminada

            return diMas > diMenos ? Escenario.Alcista : Escenario.Bajista;
        }

        // ADX < umbralAdxTendencia: sin fuerza de tendencia confirmada.
        if (sumaDi == 0m)
            return Escenario.Lateral; // DI+ y DI- ambos nulos: ausencia total de actividad direccional

        var sesgoDi = Math.Abs(diMas - diMenos) / sumaDi;
        return sesgoDi < umbralSesgoDI ? Escenario.Lateral : Escenario.Ambiguo;
    }
}
