using System.ComponentModel.DataAnnotations;
namespace OrderService.Application.DTOs;
public class OrderDto
{
    [Required]
    public string ProductName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}