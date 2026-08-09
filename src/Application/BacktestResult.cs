namespace TD_Project.Application;

// spec: RNF-09 — estados exclusivos de observabilidad
public enum EstadoBacktest
{
    Success,
    StrategyError,
    DataInvalid,
    ConfigInvalid,
    NotEvaluable,
    InternalCrash
}
