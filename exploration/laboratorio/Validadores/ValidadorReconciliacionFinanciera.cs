using TD_Project.Application;

namespace TD_Project.Laboratorio.Validadores;

// Validacion OBLIGATORIA del laboratorio (aplicada a TODOS los datasets, no solo a los que
// prueban una hipotesis financiera especifica): reconciliacion de Equity y coherencia del PnL
// completo. Un dataset puede pasar su hipotesis especifica (ej. "hay divergencia RN-11") y aun
// asi esconder una inconsistencia financiera que ningun validador especifico detecta.
public static class ValidadorReconciliacionFinanciera
{
    public sealed record Resultado(bool Coherente, IReadOnlyList<string> Errores);

    // spec: RN-08, glosario "Equity" — Equity = Cash + Margin + UnrealizedPnL, punto a punto en
    // toda la EquityCurve, no solo al cierre.
    public static Resultado Verificar(ResultadoBacktest resultado)
    {
        var errores = new List<string>();

        foreach (var punto in resultado.EquityCurve)
        {
            var esperado = punto.Cash + punto.Margin + punto.UnrealizedPnL;
            if (esperado != punto.Equity)
                errores.Add($"Timestamp={punto.Timestamp}: Equity={punto.Equity} != Cash+Margin+UnrealizedPnL={esperado}");
        }

        // PnL completo = suma de RealizedPnL de Trades cerrados + UnrealizedPnL de la posicion
        // viva al cierre (si la hay). Debe coincidir con la variacion neta de Equity respecto al
        // capital inicial, SOLO si no hubo Cross-Zero con perdida de trazabilidad de apertura
        // (validado aparte por el escenario dedicado). Aqui se verifica la relacion mas basica:
        // el ultimo punto de EquityCurve es coherente con CashFinal + Margin/UnrealizedPnL
        // reportados por el propio backtest.
        if (resultado.EquityCurve.Count > 0)
        {
            var ultimo = resultado.EquityCurve[^1];
            if (ultimo.Cash != resultado.CashFinal)
                errores.Add($"CashFinal reportado ({resultado.CashFinal}) difiere del Cash del ultimo EquityPoint ({ultimo.Cash})");
        }

        return new Resultado(errores.Count == 0, errores);
    }
}
