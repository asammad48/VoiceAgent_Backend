using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DemoController(IDemoConversationService demoService, ICallQueryService callQueryService) : ControllerBase
{
    [HttpGet("campaigns")] public ActionResult<ApiResponse<object>> Campaigns() => Ok(new ApiResponse<object> { Success = true, Data = new[] { "RestaurantOrder", "CourierService", "CabBooking", "DoctorAppointment", "Sales" } });
    [HttpPost("start")] public async Task<ActionResult<ApiResponse<object>>> Start([FromBody] object request, CancellationToken ct) => Ok(new ApiResponse<object> { Success = true, Data = await demoService.StartAsync(request, ct) });
    [HttpPost("message")] public async Task<ActionResult<ApiResponse<object>>> Message([FromBody] object request, CancellationToken ct) => Ok(new ApiResponse<object> { Success = true, Data = await demoService.SendAsync(request, ct) });
    [HttpPost("end")] public ActionResult<ApiResponse<object>> End([FromBody] object request) => Ok(new ApiResponse<object> { Success = true, Data = request });
    [HttpGet("{callSessionId:guid}")] public async Task<ActionResult<ApiResponse<object?>>> Get(Guid callSessionId, CancellationToken ct) => Ok(new ApiResponse<object?> { Success = true, Data = await callQueryService.GetSessionAsync(callSessionId, ct) });
}
