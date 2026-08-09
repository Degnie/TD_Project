namespace TD_Project.Domain.Portfolio;

// spec: RN-10 — un Fill que invierte la posicion cierra el Trade activo (libera todo el
// Margin de la posicion vieja) y abre uno nuevo por la cantidad excedente (retiene Margin nuevo).
public static class ResolutorCrossZero
{
    public static ResultadoCrossZero Resolver(Lote posicionVieja, decimal cantidadFillInversion, decimal precioFillInversion, decimal tasaMargen)
    {
        var cantidadPosicionNueva = cantidadFillInversion - posicionVieja.Cantidad;
        var marginRetenidoPosicionNueva = cantidadPosicionNueva * precioFillInversion * tasaMargen;

        return new ResultadoCrossZero(
            MarginLiberadoPosicionVieja: posicionVieja.Margin,
            CantidadPosicionNueva: cantidadPosicionNueva,
            MarginRetenidoPosicionNueva: marginRetenidoPosicionNueva);
    }
}
