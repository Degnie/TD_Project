using TD_Project.Domain.Portfolio;

namespace TD_Project.Domain.Shared;

// spec: Caso 2 D-066/D-067/D-070 — parametros del GestorCapital, capa externa a IStrategy.
// spec: Caso 5A D-109 (DECISIONES_CASO5_V1.md) — describe una eleccion (que gestor esta activo),
// no contiene logica de calculo (eso vive en la implementacion de IGestorRiesgo). Reemplaza el
// campo PorcentajeRiesgo directo (acoplado 1:1 a Fixed Fractional) por el gestor ya parametrizado.
public sealed record ConfiguracionSizing(IGestorRiesgo GestorActivo)
{
    // spec: Caso 2 D-061/D-069 — sin configuracion (null) = sizing inactivo, Cantidad de la
    // Strategy pasa intacta. No es un ConfiguracionSizing con un gestor de riesgo=0 (eso produciria
    // Cantidad=0 en cada orden, un comportamiento distinto de "sin cambios").
    public static ConfiguracionSizing? Default => null;
}
