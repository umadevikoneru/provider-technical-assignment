using Candidate.API.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Candidate.API.Data;

/// <summary>
/// EF Core mapping onto the schema defined in Database/Scripts. The scripts own the schema;
/// this context never creates or migrates tables.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Provider> Providers => Set<Provider>();

    public DbSet<License> Licenses => Set<License>();

    public DbSet<ProviderAudit> ProviderAudit => Set<ProviderAudit>();

    public DbSet<LicenseView> LicenseViews => Set<LicenseView>();

    public DbSet<ActiveProviderActiveLicense> ActiveProvidersActiveLicenses => Set<ActiveProviderActiveLicense>();

    public DbSet<ActiveProviderExpiredLicense> ActiveProvidersWithExpiredLicenses => Set<ActiveProviderExpiredLicense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Provider>(entity =>
        {
            // Declaring the triggers stops EF Core 8 from using RETURNING, which SQLite
            // does not support on tables that have triggers.
            entity.ToTable("Provider", table =>
            {
                table.HasTrigger("TR_Provider_PreventHardDelete");
                table.HasTrigger("TR_Provider_PreventEditWhileDeleted");
                table.HasTrigger("TR_Provider_AuditInsert");
                table.HasTrigger("TR_Provider_AuditUpdate");
            });
            entity.HasKey(p => p.ProviderId);
            entity.Property(p => p.ProviderName).HasMaxLength(200).IsRequired();
            entity.Property(p => p.County).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Status).HasConversion<string>();
            entity.Property(p => p.Version).IsConcurrencyToken();

            // Soft delete, application layer: every query against Providers excludes deleted rows
            // unless it explicitly calls IgnoreQueryFilters() (audit and restore paths only).
            entity.HasQueryFilter(p => !p.IsDeleted);

            entity.HasMany(p => p.Licenses)
                .WithOne(l => l.Provider)
                .HasForeignKey(l => l.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<License>(entity =>
        {
            entity.ToTable("License", table =>
            {
                table.HasTrigger("TR_License_PreventHardDelete");
                table.HasTrigger("TR_License_PreventInsertForDeletedProvider");
            });
            entity.HasKey(l => l.LicenseId);
            entity.Property(l => l.LicenseNumber).HasMaxLength(50).IsRequired();
            entity.Property(l => l.LicenseStatus).HasConversion<string>();

            // Licenses of a soft-deleted provider disappear with it.
            entity.HasQueryFilter(l => !l.Provider.IsDeleted);
        });

        modelBuilder.Entity<ProviderAudit>(entity =>
        {
            entity.ToTable("ProviderAudit", table =>
            {
                table.HasTrigger("TR_ProviderAudit_PreventUpdate");
                table.HasTrigger("TR_ProviderAudit_PreventDelete");
            });
            entity.HasKey(a => a.AuditId);
            entity.Property(a => a.Status).HasConversion<string>();
        });

        modelBuilder.Entity<LicenseView>(entity =>
        {
            entity.HasNoKey().ToView("vw_LicenseEffectiveStatus");
            entity.Property(v => v.ProviderStatus).HasConversion<string>();
            entity.Property(v => v.RecordedStatus).HasConversion<string>();
            entity.Property(v => v.EffectiveStatus).HasConversion<string>();
        });

        modelBuilder.Entity<ActiveProviderActiveLicense>(entity =>
        {
            entity.HasNoKey().ToView("vw_ActiveProvidersActiveLicenses");
        });

        modelBuilder.Entity<ActiveProviderExpiredLicense>(entity =>
        {
            entity.HasNoKey().ToView("vw_ActiveProvidersWithExpiredLicenses");
            entity.Property(v => v.RecordedStatus).HasConversion<string>();
        });

        // SQLite has no date-time type; values are stored as UTC text. Mark them UTC when read
        // so the API serializes them with a "Z" suffix.
        var utc = new ValueConverter<DateTime, DateTime>(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        var nullableUtc = new ValueConverter<DateTime?, DateTime?>(
            v => v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetProperties()))
        {
            if (property.ClrType == typeof(DateTime))
            {
                property.SetValueConverter(utc);
            }
            else if (property.ClrType == typeof(DateTime?))
            {
                property.SetValueConverter(nullableUtc);
            }
        }
    }
}
