using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Trs.Infrastructure.SqlServer;

// ADR-0007/ADR-0011 — equivalente ao TenantContextInterceptor do
// Postgres, usando SESSION_CONTEXT em vez de SET app.tenant_id.
//
// Diferença relevante: se nunca chamarmos sp_set_session_context nesta
// sessão, SESSION_CONTEXT(N'tenant_id') retorna NULL, e a Security
// Policy (tenant_id = NULL) já falha fechada por construção — não
// precisamos do truque de Guid.Empty que usamos no Postgres para evitar
// erro de cast (current_setting sem valor lança erro; SESSION_CONTEXT
// não lança). Mesmo assim, definimos explicitamente por simetria e
// previsibilidade com o comportamento do Postgres.
public sealed class TenantContextInterceptor : DbConnectionInterceptor
{
    private readonly ICurrentTenantProvider _currentTenantProvider;

    public TenantContextInterceptor(ICurrentTenantProvider currentTenantProvider)
    {
        _currentTenantProvider = currentTenantProvider;
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetTenantContextAsync((SqlConnection)connection, CancellationToken.None)
            .GetAwaiter().GetResult();
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await SetTenantContextAsync((SqlConnection)connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private async Task SetTenantContextAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantProvider.TenantId ?? Guid.Empty;

        await using var command = connection.CreateCommand();
        command.CommandText = "EXEC sp_set_session_context @key = N'tenant_id', @value = @tenant_id";
        command.Parameters.Add(new SqlParameter("@tenant_id", tenantId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
