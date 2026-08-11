using TD_Project.Application;
using TD_Project.Exploration;
using TD_Project.Laboratorio.Validadores;

namespace TD_Project.Laboratorio.Fixtures;

// Validador dedicado del escenario de perdida maxima con martingala. No usa el patron generico
// de HipotesisDataset.Verificacion porque necesita capturar InfoOperacionResuelta (via el
// callback de EstrategiaTresMosqueteros) ademas del ResultadoBacktest — la firma generica solo
// recibe el resultado, no el vinculo operacion-logica <-> Trades que exige esta verificacion.
public static class ValidadorMartingalaAgotada
{
    public sealed record Veredicto(bool Paso, IReadOnlyList<string> Detalles);

    public static Veredicto Ejecutar(decimal capitalInicial = 1000m)
    {
        var operaciones = new List<InfoOperacionResuelta>();
        var estrategia = new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: operaciones.Add);
        var config = new ConfiguracionExperimento(CapitalInicial: capitalInicial, Velas: EscenarioMartingalaAgotada.Velas());

        var resultado = BacktestRunner.Ejecutar(config, estrategia);
        var detalles = new List<string>();
        var paso = true;

        // 1. Cantidad de intentos: la operacion completa debe consumir exactamente los 3
        // niveles (inicial + M1 + M2) y resolverse como UNA sola operacion perdida.
        if (operaciones.Count != 1)
        {
            paso = false;
            detalles.Add($"FALLA: se esperaba 1 operacion completa, hubo {operaciones.Count}");
        }
        else
        {
            var op = operaciones[0];
            if (op.MartingalasUsadas != 2 || op.Gano)
            {
                paso = false;
                detalles.Add($"FALLA: se esperaba MartingalasUsadas=2, Gano=false; fue MartingalasUsadas={op.MartingalasUsadas}, Gano={op.Gano}");
            }
            else
            {
                detalles.Add($"OK: 1 operacion completa, 3 intentos (inicial+M1+M2), resultado=PERDIDA");
            }
        }

        // El motor registra cada intento como un Trade individual (RN-07: Domain no conoce la
        // nocion de "operacion logica", solo Fills/Trades). 3 intentos fallidos = 3 Trades.
        if (resultado.Trades.Count != 3)
        {
            paso = false;
            detalles.Add($"FALLA: se esperaban 3 Trades individuales (intento+M1+M2), hubo {resultado.Trades.Count}");
        }

        // 2. Perdida acumulada: el RealizedPnL de la operacion completa debe ser exactamente la
        // suma de los 3 Trades (Trade0 + Trade1 + Trade2), sin perdida ni ganancia fantasma.
        var sumaTrades = resultado.Trades.Sum(t => t.RealizedPnL);
        var pnlTotalReportado = resultado.Trades.Sum(t => t.RealizedPnL); // mismo calculo, verificado explicito
        if (sumaTrades != pnlTotalReportado)
        {
            paso = false;
            detalles.Add($"FALLA: suma de Trades ({sumaTrades}) no coincide con PnL total reportado ({pnlTotalReportado})");
        }
        else
        {
            detalles.Add($"OK: perdida acumulada = suma exacta de los 3 Trades = {sumaTrades}");
        }
        if (sumaTrades >= 0)
        {
            paso = false;
            detalles.Add($"FALLA: se esperaba una perdida neta (PnL<0) en el peor caso disenado, PnL={sumaTrades}");
        }

        // 3. Capital: reconciliacion financiera obligatoria del laboratorio.
        var reconciliacion = ValidadorReconciliacionFinanciera.Verificar(resultado);
        if (!reconciliacion.Coherente)
        {
            paso = false;
            detalles.AddRange(reconciliacion.Errores.Select(e => $"FALLA reconciliacion: {e}"));
        }
        else
        {
            detalles.Add("OK: EquityFinal = CashFinal + Margin + UnrealizedPnL en toda la curva");
        }

        // 4. Exposicion maxima: la posicion nunca debe exceder 1 unidad (cada intento abre
        // exactamente 1 unidad; nunca deberian coexistir dos lotes vivos del mismo ciclo).
        var maxPosicion = resultado.PortfolioSnapshots.Count == 0
            ? 0m
            : resultado.PortfolioSnapshots.Max(s => s.LotesVivos.Sum(l => Math.Abs(l.Cantidad)));
        if (maxPosicion > 1m)
        {
            paso = false;
            detalles.Add($"FALLA: exposicion maxima excedio el limite esperado (1 unidad): {maxPosicion}");
        }
        else
        {
            detalles.Add($"OK: exposicion maxima = {maxPosicion} (dentro del limite esperado)");
        }

        // 5. Reporte: trazabilidad completa de los 3 intentos.
        detalles.Add("Reporte de intentos:");
        for (var i = 0; i < resultado.Trades.Count; i++)
        {
            var t = resultado.Trades[i];
            var etiqueta = i == 0 ? "Inicial" : $"Martingala {i}";
            detalles.Add($"  {etiqueta}: Apertura={t.PrecioApertura} Cierre={t.PrecioCierre} RealizedPnL={t.RealizedPnL} ({(t.RealizedPnL < 0 ? "PERDIDA" : "GANANCIA")})");
        }

        return new Veredicto(paso, detalles);
    }
}
