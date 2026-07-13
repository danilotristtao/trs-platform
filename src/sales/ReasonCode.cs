namespace Trs.Sales;

// Vocabulário controlado (ADR-0009) — duplicado intencionalmente do
// mesmo enum em Trs.Identity: `sales` e `identity` são Modules de
// Bounded Contexts diferentes (ADR-0006) e não compartilham código de
// domínio entre si.
public enum ReasonCode
{
    RoutineCreation,
    ManualOverride,
    ExceptionApproval,
    Correction
}
