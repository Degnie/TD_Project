using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Portfolio;

// spec: Caso 5A D-108 (DECISIONES_CASO5_V1.md) — unico metodo, calcula cantidad SOLO para
// Apertura/Aumento. No participa en clasificacion de intencion ni normalizacion de Cross-Zero
// (GestorCapital conserva esa responsabilidad, D-092/D-095, sin cambios).
public interface IGestorRiesgo
{
    decimal CalcularCantidad(PortfolioState portfolio, DataSlice dataSlice, decimal precioReferencia, decimal tasaMargen);
}
