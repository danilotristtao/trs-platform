namespace Trs.Infrastructure.Postgres;

// Abstração mínima para o interceptor descobrir o tenant da requisição
// atual. A implementação real (lida de claims/JWT do usuário
// autenticado) vive na camada de host/API — que ainda não existe nesta
// fase de scaffolding (ver src/README.md). Aqui só o contrato
// necessário para centralizar `SET app.tenant_id` (ADR-0007/ADR-0010).
public interface ICurrentTenantProvider
{
    Guid? TenantId { get; }
}
