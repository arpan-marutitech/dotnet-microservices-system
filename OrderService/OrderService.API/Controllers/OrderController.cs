using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Common;
using OrderService.Application.Services.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;
using OrderService.Application.DTOs;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _service;

    public OrderController(IOrderService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<OrderResponseDto>>>> Get()
    {
        var userId = GetUserId();
        System.Console.WriteLine($"Fetching orders for user ID: {userId}");

        var orders = await _service.GetOrders(userId);

        return Ok(ApiResponse<List<OrderResponseDto>>
            .SuccessResponse(orders, "Orders fetched successfully"));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderResponseDto>>> Create([FromBody] OrderDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<string>.FailResponse("Invalid input"));

        var userId = GetUserId();

        var result = await _service.Create(dto, userId);

        return Ok(ApiResponse<OrderResponseDto>
            .SuccessResponse(result, "Order created"));
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("User not authorized");

        return int.Parse(userIdClaim);
    }
}