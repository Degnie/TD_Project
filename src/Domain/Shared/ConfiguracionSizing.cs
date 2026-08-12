namespace TD_Project.Domain.Shared;

// spec: Caso 2 D-066/D-067/D-070 — parametros del GestorCapital, capa externa a IStrategy.
public sealed record ConfiguracionSizing(decimal PorcentajeRiesgo)
{
    // spec: Caso 2 D-061/D-069 — sin configuracion (null) = sizing inactivo, Cantidad de la
    // Strategy pasa intacta. No es un ConfiguracionSizing con PorcentajeRiesgo=0 (eso produciria
    // Cantidad=0 en cada orden, un comportamiento distinto de "sin cambios").
    public static ConfiguracionSizing? Default => null;
}
