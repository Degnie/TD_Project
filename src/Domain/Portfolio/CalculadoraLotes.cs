namespace TD_Project.Domain.Portfolio;

// spec: RN-08 — Margin por lote k = Q_k * PrecioFill_k * TasaMargen, fijo hasta que el lote se consume
public static class CalculadoraLotes
{
    public static Lote AbrirLote(decimal cantidad, decimal precioFill, decimal tasaMargen) =>
        new(cantidad, precioFill, Margin: cantidad * precioFill * tasaMargen);
}
