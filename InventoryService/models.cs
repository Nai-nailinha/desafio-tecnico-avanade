using Microsoft.EntityFrameworkCore;

namespace InventoryService;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class InventoryDb : DbContext
{
    public InventoryDb(DbContextOptions<InventoryDb> opt) : base(opt) { }
    public DbSet<Product> Products => Set<Product>();
}
