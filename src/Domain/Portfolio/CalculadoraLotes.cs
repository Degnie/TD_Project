namespace TD_Project.Domain.Portfolio;

// spec: RN-08 — Margin por lote k = Q_k * PrecioFill_k * TasaMargen, fijo hasta que el lote se consume.
// Margin es colateral retenido (glosario): nunca negativo, independientemente del signo de
// Cantidad (positiva en Long, negativa en Short). Bug real detectado durante auditoria de uso
// cotidiano (2026-08-09): antes de esta correccion, Margin se calculaba con el signo de
// Cantidad, saliendo negativo en aperturas Short puras y propagando ese signo incorrecto hasta
// invertir RealizedPnL/MarginLiberado al reducir la posicion via ConsumidorFifo. Documentado
// como bug preexistente sin corregir en docs/PENDIENTES.md hasta que este ciclo lo manifesto.
public static class CalculadoraLotes
{
    public static Lote AbrirLote(decimal cantidad, decimal precioFill, decimal tasaMargen) =>
        new(cantidad, precioFill, Margin: Math.Abs(cantidad) * precioFill * tasaMargen);
}
