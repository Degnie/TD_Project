namespace TD_Project.Domain.Portfolio;

// spec: Caso 5A, precision derivada de D-109 (DECISIONES_CASO5_V1.md) — la identidad del gestor
// activo pertenece a la identidad experimental economica (D-082), no al contrato funcional de
// IGestorRiesgo (D-108: responsabilidad unica, calcular cantidad). Capacidad separada e
// implementada aparte para no mezclar calculo con identificacion.
public interface IIdentidadGestorRiesgo
{
    // Determinista, estable, basado en configuracion declarada — nunca en resultado de ejecucion
    // (sin retorno/drawdown/metricas). Formato de convencion: "<nombre-gestor>:v1:param=valor:...".
    string ObtenerIdentidadConfiguracion();
}
