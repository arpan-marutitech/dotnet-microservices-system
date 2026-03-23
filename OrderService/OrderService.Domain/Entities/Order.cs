namespace OrderService.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int UserId { get; set; }
}