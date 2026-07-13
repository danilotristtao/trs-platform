using Microsoft.EntityFrameworkCore;
using Trs.Identity;
using Trs.Sales;
using Trs.Tenancy;

namespace Trs.Infrastructure.Postgres;

// ADR-0011 — implementação real de persistência para o motor
// PostgreSQL. Nenhum Aggregate (tenancy/identity/sales) referencia este
// tipo diretamente; só as interfaces de Repository.
public sealed class TrsDbContext : DbContext
{
    public TrsDbContext(DbContextOptions<TrsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureTenant(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigureCustomer(modelBuilder);
        ConfigureSalesOrder(modelBuilder);
    }

    private static void ConfigureTenant(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(builder =>
        {
            builder.ToTable("tenants");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).HasColumnName("id");
            builder.Property(t => t.Name).HasColumnName("name");
            builder.Property(t => t.CreatedAt).HasColumnName("created_at");

            builder.Property(t => t.Status)
                .HasColumnName("status")
                .HasConversion(v => TenantStatusToString(v), v => TenantStatusFromString(v));

            builder.UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id).HasColumnName("id");
            builder.Property(u => u.TenantId).HasColumnName("tenant_id");
            builder.Property(u => u.ExternalIdentityReference).HasColumnName("external_identity_reference");
            builder.Property(u => u.Name).HasColumnName("name");
            builder.Property(u => u.CreatedAt).HasColumnName("created_at");
            builder.Property(u => u.HumanStatement).HasColumnName("human_statement");
            builder.Property(u => u.Author).HasColumnName("author");

            builder.Property(u => u.Status)
                .HasColumnName("status")
                .HasConversion(v => UserStatusToString(v), v => UserStatusFromString(v));

            builder.Property(u => u.Role)
                .HasColumnName("role")
                .HasConversion(v => UserRoleToString(v), v => UserRoleFromString(v));

            builder.Property(u => u.ReasonCode)
                .HasColumnName("reason_code")
                .HasConversion(v => IdentityReasonCodeToString(v), v => IdentityReasonCodeFromString(v));

            builder.OwnsOne(u => u.Email, email =>
            {
                email.Property(e => e.Value).HasColumnName("email");
            });

            builder.UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }

    private static void ConfigureCustomer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(builder =>
        {
            builder.ToTable("customers");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).HasColumnName("id");
            builder.Property(c => c.TenantId).HasColumnName("tenant_id");
            builder.Property(c => c.Name).HasColumnName("name");
            builder.Property(c => c.CreatedAt).HasColumnName("created_at");
            builder.Property(c => c.HumanStatement).HasColumnName("human_statement");
            builder.Property(c => c.Author).HasColumnName("author");

            builder.Property(c => c.Status)
                .HasColumnName("status")
                .HasConversion(v => CustomerStatusToString(v), v => CustomerStatusFromString(v));

            builder.Property(c => c.ReasonCode)
                .HasColumnName("reason_code")
                .HasConversion(v => SalesReasonCodeToString(v), v => SalesReasonCodeFromString(v));

            builder.OwnsOne(c => c.TaxId, taxId =>
            {
                taxId.Property(t => t.Value).HasColumnName("tax_id");
            });

            builder.UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }

    private static void ConfigureSalesOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SalesOrder>(builder =>
        {
            builder.ToTable("sales_orders");
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Id).HasColumnName("id");
            builder.Property(o => o.TenantId).HasColumnName("tenant_id");
            builder.Property(o => o.CustomerId).HasColumnName("customer_id");
            builder.Property(o => o.CreatedAt).HasColumnName("created_at");
            builder.Property(o => o.HumanStatement).HasColumnName("human_statement");
            builder.Property(o => o.SourceReference).HasColumnName("source_reference");
            builder.Property(o => o.Author).HasColumnName("author");
            builder.Property(o => o.Validity).HasColumnName("validity");
            builder.Property(o => o.ConfidentialityLevel).HasColumnName("confidentiality_level");

            builder.Property(o => o.Status)
                .HasColumnName("status")
                .HasConversion(v => SalesOrderStatusToString(v), v => SalesOrderStatusFromString(v));

            builder.Property(o => o.ReasonCode)
                .HasColumnName("reason_code")
                .HasConversion(v => SalesReasonCodeToString(v), v => SalesReasonCodeFromString(v));

            builder.OwnsOne(o => o.Total, total =>
            {
                total.Property(m => m.Amount).HasColumnName("total_amount");
                total.Property(m => m.Currency).HasColumnName("total_currency");
            });

            // Coleção com backing field privado (_lines) — SalesOrderLine
            // não tem Repository próprio (ADR-0006, Entity interna).
            builder.HasMany(o => o.Lines)
                .WithOne()
                .HasForeignKey("sales_order_id")
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(o => o.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<SalesOrderLine>(builder =>
        {
            builder.ToTable("sales_order_lines");
            builder.HasKey(l => l.Id);
            builder.Property(l => l.Id).HasColumnName("id");
            builder.Property(l => l.TenantId).HasColumnName("tenant_id");
            builder.Property(l => l.LineNumber).HasColumnName("line_number");
            builder.Property(l => l.Description).HasColumnName("description");
            builder.Property(l => l.Quantity).HasColumnName("quantity");

            builder.OwnsOne(l => l.UnitPrice, price =>
            {
                price.Property(m => m.Amount).HasColumnName("unit_price_amount");
                price.Property(m => m.Currency).HasColumnName("unit_price_currency");
            });

            // Calculada em memória (Quantity * UnitPrice) — não é coluna.
            builder.Ignore(l => l.LineTotal);

            builder.UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }

    // As conversões abaixo replicam, byte a byte, os valores aceitos
    // pelos CHECK constraints de migrations/0001_init.sql. Usar o
    // `.HasConversion<string>()` default do EF geraria "Active"/"TenantAdmin"
    // (nome do enum em C#) em vez de "active"/"tenant_admin" — violaria o
    // CHECK constraint em runtime.
    private static string TenantStatusToString(TenantStatus value) => value switch
    {
        TenantStatus.Active => "active",
        TenantStatus.Suspended => "suspended",
        _ => throw new InvalidOperationException($"TenantStatus desconhecido: '{value}'.")
    };

    private static TenantStatus TenantStatusFromString(string value) => value switch
    {
        "active" => TenantStatus.Active,
        "suspended" => TenantStatus.Suspended,
        _ => throw new InvalidOperationException($"Valor de status (tenants) desconhecido: '{value}'.")
    };

    private static string UserStatusToString(UserStatus value) => value switch
    {
        UserStatus.Active => "active",
        UserStatus.Deactivated => "deactivated",
        _ => throw new InvalidOperationException($"UserStatus desconhecido: '{value}'.")
    };

    private static UserStatus UserStatusFromString(string value) => value switch
    {
        "active" => UserStatus.Active,
        "deactivated" => UserStatus.Deactivated,
        _ => throw new InvalidOperationException($"Valor de status (users) desconhecido: '{value}'.")
    };

    private static string UserRoleToString(UserRole value) => value switch
    {
        UserRole.TenantAdmin => "tenant_admin",
        UserRole.Member => "member",
        _ => throw new InvalidOperationException($"UserRole desconhecido: '{value}'.")
    };

    private static UserRole UserRoleFromString(string value) => value switch
    {
        "tenant_admin" => UserRole.TenantAdmin,
        "member" => UserRole.Member,
        _ => throw new InvalidOperationException($"Valor de role desconhecido: '{value}'.")
    };

    private static string CustomerStatusToString(CustomerStatus value) => value switch
    {
        CustomerStatus.Active => "active",
        CustomerStatus.Inactive => "inactive",
        _ => throw new InvalidOperationException($"CustomerStatus desconhecido: '{value}'.")
    };

    private static CustomerStatus CustomerStatusFromString(string value) => value switch
    {
        "active" => CustomerStatus.Active,
        "inactive" => CustomerStatus.Inactive,
        _ => throw new InvalidOperationException($"Valor de status (customers) desconhecido: '{value}'.")
    };

    private static string SalesOrderStatusToString(SalesOrderStatus value) => value switch
    {
        SalesOrderStatus.Draft => "draft",
        SalesOrderStatus.Active => "active",
        _ => throw new InvalidOperationException($"SalesOrderStatus desconhecido: '{value}'.")
    };

    private static SalesOrderStatus SalesOrderStatusFromString(string value) => value switch
    {
        "draft" => SalesOrderStatus.Draft,
        "active" => SalesOrderStatus.Active,
        _ => throw new InvalidOperationException($"Valor de status (sales_orders) desconhecido: '{value}'.")
    };

    private static string IdentityReasonCodeToString(Trs.Identity.ReasonCode value) => value switch
    {
        Trs.Identity.ReasonCode.RoutineCreation => "routine_creation",
        Trs.Identity.ReasonCode.ManualOverride => "manual_override",
        Trs.Identity.ReasonCode.ExceptionApproval => "exception_approval",
        Trs.Identity.ReasonCode.Correction => "correction",
        _ => throw new InvalidOperationException($"ReasonCode (identity) desconhecido: '{value}'.")
    };

    private static Trs.Identity.ReasonCode IdentityReasonCodeFromString(string value) => value switch
    {
        "routine_creation" => Trs.Identity.ReasonCode.RoutineCreation,
        "manual_override" => Trs.Identity.ReasonCode.ManualOverride,
        "exception_approval" => Trs.Identity.ReasonCode.ExceptionApproval,
        "correction" => Trs.Identity.ReasonCode.Correction,
        _ => throw new InvalidOperationException($"Valor de reason_code (users) desconhecido: '{value}'.")
    };

    private static string SalesReasonCodeToString(Trs.Sales.ReasonCode value) => value switch
    {
        Trs.Sales.ReasonCode.RoutineCreation => "routine_creation",
        Trs.Sales.ReasonCode.ManualOverride => "manual_override",
        Trs.Sales.ReasonCode.ExceptionApproval => "exception_approval",
        Trs.Sales.ReasonCode.Correction => "correction",
        _ => throw new InvalidOperationException($"ReasonCode (sales) desconhecido: '{value}'.")
    };

    private static Trs.Sales.ReasonCode SalesReasonCodeFromString(string value) => value switch
    {
        "routine_creation" => Trs.Sales.ReasonCode.RoutineCreation,
        "manual_override" => Trs.Sales.ReasonCode.ManualOverride,
        "exception_approval" => Trs.Sales.ReasonCode.ExceptionApproval,
        "correction" => Trs.Sales.ReasonCode.Correction,
        _ => throw new InvalidOperationException($"Valor de reason_code (sales) desconhecido: '{value}'.")
    };
}
