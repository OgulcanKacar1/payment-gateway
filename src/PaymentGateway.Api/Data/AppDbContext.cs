using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PaymentGateway.Api.Models.Entities;

namespace PaymentGateway.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>(payment =>
        {
            payment.Property(p => p.Amount).HasPrecision(18, 2); // 18 basamaklı, 2 ondalık
            payment.Property(p => p.Status).HasConversion<string>();
            payment.Property(p => p.Currency).HasMaxLength(3);
            payment.Property(p => p.CardLast4).HasMaxLength(4);
            
        });
        
        modelBuilder.Entity<Merchant>(merchant =>
        {
            merchant.HasIndex(m => m.ApiKey).IsUnique();
        });
        
        modelBuilder.Entity<Merchant>().HasData(new Merchant
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Test Merchant",
            ApiKey = "sk_test_123456789",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        
        modelBuilder.Entity<IdempotencyRecord>()
            .HasIndex(r => new { r.MerchantId, r.Key })
            .IsUnique();

        modelBuilder.Entity<WebhookEvent>(webhook =>
        {
            webhook.Property(w => w.Status).HasConversion<string>();
            webhook.Property(w => w.EventType).HasMaxLength(50);
        });

        modelBuilder.Entity<LedgerEntry>(entry =>
        {
            entry.Property(e => e.Amount).HasPrecision(18, 2); // 18 basamaklı, 2 ondalık
            entry.Property(e => e.Account).HasConversion<string>(); // Enum to string
            entry.Property(e => e.Currency).HasMaxLength(3); // ISO 4217
            entry.Property(e => e.Description).HasMaxLength(200);
            
            entry.HasIndex(e => new {e.MerchantId, e.Account}); // bakiye sorgusu: bir merchantın bir hesabındaki tüm entryler
            entry.HasIndex(e => e.TransactionId); // transaction bazlı sorgu: bir olayın tüm entryleri
        });

    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }
    
    public override int SaveChanges()
    {
        ApplyAuditFields();
        return base.SaveChanges();
    }




    private void ApplyAuditFields()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            
            
        }
    }
}