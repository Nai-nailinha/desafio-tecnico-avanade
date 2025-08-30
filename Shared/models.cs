namespace Shared;

public record ProductDto(int Id, string Name, string Description, decimal Price, int Quantity);
public record OrderItemDto(int ProductId, int Quantity);
public record CreateOrderDto(List<OrderItemDto> Items);
// Mensagem de evento publicada pelo Sales e consumida pelo Inventory:
public record OrderConfirmedEvent(int OrderId, List<OrderItemDto> Items);
