namespace OrderService.Application.DTOs;

public class OrderResponseDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}