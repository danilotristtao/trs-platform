namespace Trs.Identity;

// Vocabulário controlado (ADR-0009) — não expandir informalmente.
// `routine_creation` é o único valor que dispensa `human_statement`.
public enum ReasonCode
{
    RoutineCreation,
    ManualOverride,
    ExceptionApproval,
    Correction
}
