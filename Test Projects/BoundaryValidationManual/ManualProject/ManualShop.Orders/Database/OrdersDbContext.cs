using Microsoft.EntityFrameworkCore;

namespace ManualShop.Orders.Database;

public sealed class OrdersDbContext : DbContext
{
	public OrdersDbContext(DbContextOptions<OrdersDbContext> options)
		: base(options)
	{
	}
}