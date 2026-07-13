using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Trs.Infrastructure.Postgres;

// ADR-0007/ADR-0010 — RLS depende de `app.tenant_id` estar definido em
// toda conexão antes de qualquer query rodar. Centralizado aqui para
// que nenhum Repository individual precise (ou possa esquecer de)
// executar esse SET por conta própria.
public sealed class TenantContextInterceptor : DbConnectionInterceptor
{
    private readonly ICurrentTenantProvider _currentTenantProvider;

    public TenantContextInterceptor(ICurrentTenantProvider currentTenantProvider)
    {
        _currentTenantProvider = currentTenantProvider;
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetTenantContextAsync((NpgsqlConnection)connection, CancellationToken.None)
            .GetAwaiter().GetResult();
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await SetTenantContextAsync((NpgsqlConnection)connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private async Task SetTenantContextAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        // Falha fechada (ADR-0007/ADR-0011): sem tenant autenticado no
        // contexto atual, usamos Guid.Empty — valor que nenhum Aggregate
        // aceita como tenant_id real (Tenant/User/Customer/SalesOrder
        // rejeitam tenantId == Guid.Empty na criação) — então a política
        // de RLS nunca casa com nenhuma linha real, sem lançar erro de
        // cast (o que aconteceria se a sessão ficasse sem o valor
        // definido e a policy tentasse converter NULL/'' para UUID).
        var tenantId = _currentTenantProvider.TenantId ?? Guid.Empty;

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.tenant_id', $1, false)";
        command.Parameters.Add(new NpgsqlParameter { Value = tenantId.ToString() });
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
