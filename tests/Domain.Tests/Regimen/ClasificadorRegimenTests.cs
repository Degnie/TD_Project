using TD_Project.Domain.Regimen;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Domain.Tests.Regimen;

public class ClasificadorRegimenTests
{
    private static Candle[] VelasConPendiente(int cantidad, decimal closeInicial, decimal incrementoPorVela)
    {
        var velas = new Candle[cantidad];
        for (var i = 0; i < cantidad; i++)
        {
            var close = closeInicial + incrementoPorVela * i;
            velas[i] = new Candle(i, close, close, close, close, 0m);
        }
        return velas;
    }

    // spec: RN-19 — ventana movil W=20, regresion lineal sobre Close; slope > epsilon -> Alza
    [Fact]
    public void UnaSerieConPendientePositivaFuerteSeClasificaComoAlcista()
    {
        var velas = VelasConPendiente(20, closeInicial: 100m, incrementoPorVela: 5m);

        var regimen = ClasificadorRegimen.Clasificar(velas, indiceVela: 19);

        Assert.Equal(Regimen.Alcista, regimen);
    }

    // spec: RN-19 — slope < -epsilon -> Baja
    [Fact]
    public void UnaSerieConPendienteNegativaFuerteSeClasificaComoBajista()
    {
        var velas = VelasConPendiente(20, closeInicial: 500m, incrementoPorVela: -5m);

        var regimen = ClasificadorRegimen.Clasificar(velas, indiceVela: 19);

        Assert.Equal(Regimen.Bajista, regimen);
    }

    // spec: RN-19 — dentro de [-epsilon, epsilon] -> Horizontal (precio constante = slope 0)
    [Fact]
    public void UnaSerieSinPendienteSeClasificaComoHorizontal()
    {
        var velas = VelasConPendiente(20, closeInicial: 100m, incrementoPorVela: 0m);

        var regimen = ClasificadorRegimen.Clasificar(velas, indiceVela: 19);

        Assert.Equal(Regimen.Horizontal, regimen);
    }

    // spec: RN-19 — violacion: si la ventana no supera el umbral de inclinacion, se clasifica por
    // defecto como Horizontal (incluye el caso de ventana incompleta antes de W=20 velas)
    [Fact]
    public void UnaVentanaIncompletaSeClasificaComoHorizontalPorDefecto()
    {
        var velas = VelasConPendiente(5, closeInicial: 100m, incrementoPorVela: 5m);

        var regimen = ClasificadorRegimen.Clasificar(velas, indiceVela: 4);

        Assert.Equal(Regimen.Horizontal, regimen);
    }

    // spec: RN-19 — clasificacion determinista: misma serie produce siempre el mismo regimen
    [Fact]
    public void LaClasificacionEsDeterminista()
    {
        var velas = VelasConPendiente(20, closeInicial: 100m, incrementoPorVela: 3m);

        var regimen1 = ClasificadorRegimen.Clasificar(velas, indiceVela: 19);
        var regimen2 = ClasificadorRegimen.Clasificar(velas, indiceVela: 19);

        Assert.Equal(regimen1, regimen2);
    }
}
