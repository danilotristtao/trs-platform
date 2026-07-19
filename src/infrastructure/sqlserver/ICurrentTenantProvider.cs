namespace Trs.Infrastructure.SqlServer;

// Abstração mínima para o interceptor descobrir o tenant da requisição
// atual. A implementação real (lida de claims/JWT do usuário
// autenticado) vive na camada de host/API — que ainda não existe nesta
// fase de scaffolding. Duplicado do equivalente em
// Trs.Infrastructure.Postgres de propósito: cada infraestrutura de
// motor é auto-contida (ADR-0011), sem depender uma da outra.
public interface ICurrentTenantProvider
{
    Guid? TenantId { get; }
}
