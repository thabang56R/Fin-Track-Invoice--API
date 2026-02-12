using FinTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Users
        b.Entity<AppUser>()
            .HasIndex(x => x.Email)
            .IsUnique();

        // ✅ Optimistic concurrency for Invoice
        b.Entity<Invoice>()
            .Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Invoice money fields
        b.Entity<Invoice>(e =>
        {
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.Property(x => x.VatTotal).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);

            // Relationships
            e.HasMany(x => x.Items)
             .WithOne(x => x.Invoice!)
             .HasForeignKey(x => x.InvoiceId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Payments)
             .WithOne(x => x.Invoice!)
             .HasForeignKey(x => x.InvoiceId)
             .OnDelete(DeleteBehavior.Restrict); // safer than cascade for payments
        });

        // InvoiceItem money fields
        b.Entity<InvoiceItem>(e =>
        {
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.LineTotal).HasPrecision(18, 2);
            e.Property(x => x.VatAmount).HasPrecision(18, 2);

            // VAT rate like 0.1500
            e.Property(x => x.VatRate).HasPrecision(5, 4);
        });

        // Payment money fields
        b.Entity<Payment>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);

            // Optional: index for faster lookups by invoice
            e.HasIndex(x => x.InvoiceId);
        });

        // Optional: indexes for performance
        b.Entity<Invoice>()
            .HasIndex(x => x.CustomerId);

        b.Entity<Invoice>()
            .HasIndex(x => x.InvoiceNumber)
            .IsUnique();
    }
}

