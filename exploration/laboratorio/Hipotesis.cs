using TD_Project.Application;

namespace TD_Project.Laboratorio;

public enum CategoriaHipotesis { Market, Scenario }

// Un registro por dataset del laboratorio. La Verificacion es un chequeo automatico (assert),
// no una descripcion para inspeccionar a ojo — convierte cada dataset en una prueba ejecutable.
public sealed record HipotesisDataset(
    string Nombre,
    CategoriaHipotesis Categoria,
    string ArchivoCsv,
    string Descripcion,
    string QueValida,
    Func<ResultadoBacktest, (bool Paso, string Detalle)> Verificacion);
