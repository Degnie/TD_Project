using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Regimen;

// spec: RN-19 — clasificacion determinista por pendiente de regresion lineal sobre ventana movil
// W=20 del precio de Close. Umbral epsilon documentado como version V1 (decision inicial, sin
// calibrar): aislado en una constante propia para no mezclar la calibracion futura con esta
// primera implementacion (restriccion explicita del auditor sobre RN-19).
public static class ClasificadorRegimen
{
    public const string Version = "V1";
    public const int Ventana = 20;
    public const decimal Epsilon = 0.01m;

    public static Regimen Clasificar(IReadOnlyList<Candle> velas, int indiceVela)
    {
        if (indiceVela < Ventana - 1)
            return Regimen.Horizontal;

        var inicio = indiceVela - Ventana + 1;
        var slope = PendienteRegresionLineal(velas, inicio, Ventana);

        if (slope > Epsilon)
            return Regimen.Alcista;
        if (slope < -Epsilon)
            return Regimen.Bajista;
        return Regimen.Horizontal;
    }

    // spec: RN-19 — regresion lineal por minimos cuadrados de Close sobre el indice de vela dentro
    // de la ventana (x = 0..W-1), slope = Cov(x,y) / Var(x).
    private static decimal PendienteRegresionLineal(IReadOnlyList<Candle> velas, int inicio, int longitud)
    {
        decimal sumaX = 0m, sumaY = 0m, sumaXY = 0m, sumaXX = 0m;
        for (var x = 0; x < longitud; x++)
        {
            var y = velas[inicio + x].Close;
            sumaX += x;
            sumaY += y;
            sumaXY += x * y;
            sumaXX += (decimal)x * x;
        }

        var n = longitud;
        var denominador = n * sumaXX - sumaX * sumaX;
        if (denominador == 0m)
            return 0m;

        return (n * sumaXY - sumaX * sumaY) / denominador;
    }
}
