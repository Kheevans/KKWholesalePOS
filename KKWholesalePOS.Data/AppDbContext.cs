using KKWholesalePOS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KKWholesalePOS.Data;

public class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }
    public AppDbContext(DbContextOptions<AppDbContext> options)
    : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<RegularSale> RegularSales => Set<RegularSale>();
    public DbSet<RegularSaleLine> RegularSaleLines => Set<RegularSaleLine>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<CreditSale> CreditSales => Set<CreditSale>();
    public DbSet<CreditSaleLine> CreditSaleLines => Set<CreditSaleLine>();
    public DbSet<CreditPayment> CreditPayments => Set<CreditPayment>();

    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "KKWholesalePOS");

            Directory.CreateDirectory(folder);

            string dbPath = Path.Combine(
                folder, "kkwholesale.db");

            optionsBuilder.UseSqlite(
                $"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        modelBuilder.Entity<Permission>()
            .HasIndex(p => p.Name)
            .IsUnique();

        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId);

        modelBuilder.Entity<AuditLogEntry>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<RegularSale>()
            .HasMany(s => s.Lines)
            .WithOne()
            .HasForeignKey(l => l.RegularSaleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RegularSaleLine>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockMovement>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockMovement>()
        .HasOne<RegularSale>()
        .WithMany()
        .HasForeignKey(m => m.RegularSaleId)
        .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<StockMovement>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);
      
        // Credit sale relationships
        modelBuilder.Entity<CreditSale>()
            .HasOne<Customer>()
            .WithMany()
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CreditSale>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CreditSale>()
            .HasMany(s => s.Lines)
            .WithOne()
            .HasForeignKey(l => l.CreditSaleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Credit sale line relationships
        modelBuilder.Entity<CreditSaleLine>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Credit payment relationships
        modelBuilder.Entity<CreditPayment>()
            .HasOne<CreditSale>()
            .WithMany()
            .HasForeignKey(p => p.CreditSaleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CreditPayment>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Credit sale configuration
        modelBuilder.Entity<CreditSale>()
            .HasIndex(s => s.ReferenceNumber)
            .IsUnique();

        // TotalAmount is calculated from PurchaseAmount + CreditFee.
        modelBuilder.Entity<CreditSale>()
            .Ignore(s => s.TotalAmount);

    }
}