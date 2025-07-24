using Microsoft.EntityFrameworkCore;
using Order.Core.Entities.Models;

namespace Order.Application.Database;

public interface IOrderDbContext
{
    DbSet<CustomerOrder> CustomerOrders { get; set; }
    DbSet<OrderItem> OrderItems { get; set; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}