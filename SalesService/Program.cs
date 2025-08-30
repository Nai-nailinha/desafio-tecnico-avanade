using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SalesService;
using Shared;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SalesDb>(o =>
    o.UseInMemoryDatabase("SalesDb"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "super_secret_dev_key_please_change");
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

builder.Services.AddHttpClient("inventory", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["InventoryBaseUrl"]!);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => "sales ok");

app.MapPost("/orders", async (CreateOrderDto dto, SalesDb db, IHttpClientFactory httpFactory) =>
{
    var inv = httpFactory.CreateClient("inventory");
    var resp = await inv.PostAsJsonAsync("/validate-stock", dto);
    if (!resp.IsSuccessStatusCode)
        return Results.BadRequest(await resp.Content.ReadAsStringAsync());

    var order = new Order
    {
        Status = "Confirmed",
        Items = dto.Items.Select(i => new OrderItem { ProductId = i.ProductId, Quantity = i.Quantity }).ToList()
    };
    db.Add(order);
    await db.SaveChangesAsync();

    return Results.Created($"/orders/{order.Id}", new { order.Id, order.Status });
});

app.MapGet("/orders/{id:int}", async (int id, SalesDb db) =>
{
    var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

using (var scope = app.Services.CreateScope())
{
    var sdb = scope.ServiceProvider.GetRequiredService<SalesDb>();
    sdb.Database.EnsureCreated();
}

app.Run();
