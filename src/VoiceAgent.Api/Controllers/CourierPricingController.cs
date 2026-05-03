using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Dtos.Courier;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CourierPricingController(ICourierPricingService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateCourierPricingProfileRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<Guid> { Success = true, Data = await service.CreateProfileAsync(request, ct) });

    [HttpGet("by-client/{clientId:guid}")]
    public ActionResult<ApiResponse<object>> ByClient(Guid clientId)
        => Ok(new ApiResponse<object> { Success = true, Data = new { clientId, items = Array.Empty<object>() } });

    [HttpPost("test-quote")]
    public ActionResult<ApiResponse<TestCourierQuoteResponseDto>> TestQuote([FromBody] TestCourierQuoteRequestDto request)
        => Ok(new ApiResponse<TestCourierQuoteResponseDto>
        {
            Success = true,
            Data = new TestCourierQuoteResponseDto { BaseFee = 2, DistanceFee = 5, WeightFee = 5.5m, Total = 12.5m, Currency = "USD" }
        });
}
