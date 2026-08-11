namespace TD_Project.Exploration;

// Herramienta de analisis de resultados (no forma parte del motor ni de sus contratos).
// Consume InfoOperacionResuelta (una entrada por OPERACION LOGICA completa, no por Trade
// individual del motor) para calcular metricas de riesgo bajo martingala.
public sealed class AnalisisRiesgo
{
    private readonly List<InfoOperacionResuelta> _operaciones = new();

    public void Registrar(InfoOperacionResuelta info) => _operaciones.Add(info);

    public void Reportar(string nombreEscenario)
    {
        var total = _operaciones.Count;
        var ganadas = _operaciones.Count(o => o.Gano);
        var perdidas = total - ganadas;

        Console.WriteLine($"\n=== Analisis de riesgo: {nombreEscenario} ===");
        Console.WriteLine($"Operaciones completas: {total} (Ganadas: {ganadas} · Perdidas: {perdidas})");

        // Rachas negativas de OPERACIONES completas (no de Trades individuales).
        var rachas = new List<int>();
        var rachaActual = 0;
        foreach (var op in _operaciones)
        {
            if (!op.Gano) { rachaActual++; }
            else { if (rachaActual > 0) rachas.Add(rachaActual); rachaActual = 0; }
        }
        if (rachaActual > 0) rachas.Add(rachaActual); // racha abierta al final del dataset

        var rachaMaxima = rachas.Count > 0 ? rachas.Max() : 0;
        Console.WriteLine($"Racha negativa maxima (operaciones completas): {rachaMaxima}");

        Console.WriteLine("Distribucion de rachas negativas:");
        Console.WriteLine($"  2 perdidas consecutivas: {rachas.Count(r => r == 2)}");
        Console.WriteLine($"  3 perdidas consecutivas: {rachas.Count(r => r == 3)}");
        Console.WriteLine($"  4 perdidas consecutivas: {rachas.Count(r => r == 4)}");
        Console.WriteLine($"  5+ perdidas consecutivas: {rachas.Count(r => r >= 5)}");
        Console.WriteLine($"  Maxima observada: {rachaMaxima}");

        // Distribucion segun donde se resolvio cada operacion GANADA, y perdidas por agotamiento.
        var ganoInicial = _operaciones.Count(o => o.Gano && o.MartingalasUsadas == 0);
        var ganoM1 = _operaciones.Count(o => o.Gano && o.MartingalasUsadas == 1);
        var ganoM2 = _operaciones.Count(o => o.Gano && o.MartingalasUsadas == 2);
        var perdioAgotoMartingalas = _operaciones.Count(o => !o.Gano);

        Console.WriteLine("Distribucion del resultado segun uso de martingala:");
        Console.WriteLine($"  Gano en entrada inicial: {ganoInicial}");
        Console.WriteLine($"  Gano despues de Martingala 1: {ganoM1}");
        Console.WriteLine($"  Gano despues de Martingala 2: {ganoM2}");
        Console.WriteLine($"  Perdio despues de agotar martingalas: {perdioAgotoMartingalas}");

        // Peor secuencia interna: la operacion (perdida) con mas intentos fallidos consecutivos
        // DENTRO de ella misma (intento inicial + martingalas, todos fallidos = MartingalasUsadas+1).
        var peorInterna = _operaciones.Where(o => !o.Gano).OrderByDescending(o => o.MartingalasUsadas + 1).FirstOrDefault();
        if (peorInterna is not null)
        {
            Console.WriteLine($"Peor secuencia interna: {peorInterna.MartingalasUsadas + 1} fallos consecutivos, en la operacion #{peorInterna.OperacionId}");
        }
        else
        {
            Console.WriteLine("Peor secuencia interna: no hubo operaciones perdidas.");
        }
    }
}
