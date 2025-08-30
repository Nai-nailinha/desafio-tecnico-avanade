using InventoryService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;


var builder = WebApplication.CreateBuilder(args);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// >>> Troca para SQL Server (lendo appsettings.json)
builder.Services.AddDbContext<InventoryDb>(o =>
    o.UseInMemoryDatabase("InventoryDb"));

// JWT simples (deixamos configurado; não vamos exigir ainda)
var key = Encoding.UTF8.GetBytes("super_secret_dev_key_please_change");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new()
        {
            ValidateIssuer = false, ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => "inventory ok");

// Endpoints
app.MapPost("/products", async (Product p, InventoryDb db) =>
{
    db.Add(p); await db.SaveChangesAsync();
    return Results.Created($"/products/{p.Id}", p);
});

app.MapGet("/products", async (InventoryDb db) =>
    await db.Products.Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.Quantity)).ToListAsync());

app.MapGet("/products/{id:int}", async (int id, InventoryDb db) =>
    await db.Products.FindAsync(id) is { } p ? Results.Ok(p) : Results.NotFound());

// valida estoque para uma lista de itens (usado pelo SalesService)
app.MapPost("/validate-stock", async (CreateOrderDto order, InventoryDb db) =>
{
    foreach (var item in order.Items)
    {
        var prod = await db.Products.FindAsync(item.ProductId);
        if (prod is null || prod.Quantity < item.Quantity)
            return Results.BadRequest($"Sem estoque para produto {item.ProductId}");
    }
    return Results.Ok(new { ok = true });
});

// Consumer do RabbitMQ: OrderConfirmedEvent -> debita estoque
Task.Run(() =>
{
    RabbitMQ.Client.ConnectionFactory factory = new RabbitMQ.Client.ConnectionFactory()
    {
        HostName = "localhost"
    };

    using RabbitMQ.Client.IConnection conn = factory.CreateConnection();
    using RabbitMQ.Client.IModel ch = conn.CreateModel();

    ch.QueueDeclare(queue: "order_confirmed", durable: false, exclusive: false, autoDelete: false);

    var consumer = new RabbitMQ.Client.Events.EventingBasicConsumer(ch);
    consumer.Received += async (_, ea) =>
    {
        string json = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray());
        var msg = System.Text.Json.JsonSerializer.Deserialize<OrderConfirmedEvent>(json);
        if (msg is null) return;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDb>();

        foreach (var it in msg.Items)
        {
            var prod = await db.Products.FindAsync(it.ProductId);
            if (prod is not null)
            {
                prod.Quantity -= it.Quantity;
            }
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"[Inventory] Estoque atualizado para pedido {msg.OrderId}");
    };

    ch.BasicConsume(queue: "order_confirmed", autoAck: true, consumer: consumer);
    System.Console.WriteLine("[Inventory] Consumer ligado.");
    System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
});


// Cria DB e semente
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDb>();
    db.Database.EnsureDeleted();
    db.Database.EnsureCreated();
    if (!db.Products.Any())
    {
        db.Products.Add(new Product { Name = "Teclado", Description = "ABNT2", Price = 150m, Quantity = 5 });
        db.SaveChanges();
    }
}



app.Run();


