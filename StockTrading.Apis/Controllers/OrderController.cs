using Microsoft.AspNetCore.Mvc;
using StockTrading.Common.DTOs;
using StockTrading.IServices;

namespace StockTrading.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        try
        {
            var orders = await _orderService.GetOrdersAsync(HttpContext.RequestAborted);
            return Ok(new { orders });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to retrieve orders", error = ex.Message });
        }
    }

    [HttpGet("{brokerOrderId}")]
    public async Task<IActionResult> GetOrder(string brokerOrderId)
    {
        try
        {
            var order = await _orderService.GetOrderAsync(brokerOrderId, HttpContext.RequestAborted);
            return order == null
                ? NotFound(new { message = "Order not found." })
                : Ok(order);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to retrieve order", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(PlaceOrderRequest request)
    {
        try
        {
            var result = await _orderService.PlaceOrderAsync(request, HttpContext.RequestAborted);
            return result.IsSuccess
                ? Ok(result)
                : BadRequest(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to place order", error = ex.Message });
        }
    }

    [HttpDelete("{brokerOrderId}")]
    public async Task<IActionResult> CancelOrder(string brokerOrderId)
    {
        try
        {
            var result = await _orderService.CancelOrderAsync(brokerOrderId, HttpContext.RequestAborted);
            return result.IsSuccess
                ? Ok(result)
                : BadRequest(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to cancel order", error = ex.Message });
        }
    }

    [HttpGet("{brokerOrderId}/history")]
    public async Task<IActionResult> GetHistory(string brokerOrderId)
    {
        try
        {
            var history = await _orderService.GetHistoryAsync(brokerOrderId, HttpContext.RequestAborted);
            return Ok(new { history });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to retrieve order history", error = ex.Message });
        }
    }
}
