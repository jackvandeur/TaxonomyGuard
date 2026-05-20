using EnterpriseGovernance.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseGovernance.Adapters.Database;

public class GovernanceDbContext : DbContext
{
    public GovernanceDbContext(DbContextOptions<GovernanceDbContext> options) : base(options)
    {
    }

    public DbSet<TenantAuditResult> AuditResults => Set<TenantAuditResult>();
    public DbSet<ContentTypeDefinition> ContentTypes => Set<ContentTypeDefinition>();
    public DbSet<FieldDefinition> Fields => Set<FieldDefinition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Id-definities voor de tabellen (TenantAuditResult krijgt een gegenereerde sleutel voor de historie)
        modelBuilder.Entity<TenantAuditResult>().HasKey(r => r.ScanDateTime);
        modelBuilder.Entity<ContentTypeDefinition>().HasKey(c => c.Id);

        // Let op: Omdat veld-IDs binnen SharePoint hergebruikt kunnen worden, 
        // maken we een gecombineerde sleutel van Field Id en ContentType Id 
        // of we laten EF zelf een schaduw-id genereren.
        modelBuilder.Entity<FieldDefinition>().HasKey(f => f.Id);

        // Relatie: Een AuditResult heeft meerdere ContentTypes
        modelBuilder.Entity<TenantAuditResult>()
            .HasMany(r => r.DetectedContentTypes)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        // Relatie: Een AuditResult heeft meerdere Global Fields (losse kolommen)
        modelBuilder.Entity<TenantAuditResult>()
            .HasMany(r => r.DetectedGlobalFields)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        // Relatie: Een ContentType heeft meerdere Fields
        modelBuilder.Entity<ContentTypeDefinition>()
            .HasMany(c => c.Fields)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}