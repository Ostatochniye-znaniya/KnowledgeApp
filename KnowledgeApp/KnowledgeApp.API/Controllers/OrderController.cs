using KnowledgeApp.Application.DTOs.Orders;
using KnowledgeApp.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KnowledgeApp.API.Controllers;

public class OrderController : DocumentControllerBase
{
    private readonly OrderService _orderService;

    public OrderController(OrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost("/createOrder")]
    public Task<IResult> CreateOrder(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CreateOrderDto? request,
        CancellationToken cancellationToken) =>
        Execute(async () =>
        {
            var id = await _orderService.Create(request, cancellationToken);
            return Results.Json(new { id }, statusCode: StatusCodes.Status201Created);
        });

    [HttpGet("/getOrdersEssentials")]
    public Task<IResult> GetOrdersEssentials([FromQuery] string? status) =>
        Execute(async () => Results.Json(await _orderService.GetEssentials(status)));

    [HttpGet("/getOrderDetails/{id:int}")]
    public Task<IResult> GetOrderDetails(int id, CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(await _orderService.GetDetails(id, cancellationToken)));

    [HttpPost("/signOrder/{id:int}")]
    public Task<IResult> SignOrder(int id, CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(await _orderService.Sign(id, cancellationToken)));

    [HttpGet("/getOrder/{id}")]
    public Task<IResult> GetOrder(int id, CancellationToken cancellationToken) =>
        Execute(async () =>
        {
            var file = await _orderService.OpenFile(id, cancellationToken);
            HttpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
            HttpContext.Response.Headers.ContentDisposition = $"inline; filename=\"{file.FileName}\"";
            return Results.File(file.Content, file.ContentType);
        });
}
