using Microsoft.EntityFrameworkCore;
using OrderQueryService.Entities;

namespace OrderQueryService.Data;

public class OrderQueryDbContext : DbContext
{
    public OrderQueryDbContext(DbContextOptions<OrderQueryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>().ToTable("Orders");
        modelBuilder.Entity<Order>().HasKey(o => o.OrderId);
    }
}