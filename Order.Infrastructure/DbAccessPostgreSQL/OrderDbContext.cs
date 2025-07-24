using Microsoft.EntityFrameworkCore;
using Order.Application.Database;
using Order.Core.Entities.Abstracts;
using Order.Core.Entities.Models;

namespace Order.Infrastructure.DbAccessPostgreSQL;

public class OrderDbContext : DbContext, IOrderDbContext
{
    public DbSet<CustomerOrder> CustomerOrders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) {}

    protected override void OnModelCreating(ModelBuilder  modelBuilder)
    {
        // FluentAPI validation
        modelBuilder.Ignore<DomainEvent>();
        modelBuilder.Ignore<Entity>();
    }
}