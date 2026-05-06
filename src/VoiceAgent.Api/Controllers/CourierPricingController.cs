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

    
    [HttpPatch("{id:guid}")]
    public ActionResult<ApiResponse<bool>> Update(Guid id, [FromBody] UpdateCourierPricingProfileRequestDto request)
        => Ok(id == Guid.Empty
            ? ApiResponse<bool>.Fail("Courier pricing profile not found.")
            : ApiResponse<bool>.Ok(true, "Courier pricing profile updated."));

    [HttpGet("by-client/{clientId:guid}")]
    public ActionResult<ApiResponse<IReadOnlyList<CourierPricingProfileResponseDto>>> ByClient(Guid clientId)
        => Ok(new ApiResponse<IReadOnlyList<CourierPricingProfileResponseDto>> { Success = true, Data = Array.Empty<CourierPricingProfileResponseDto>() });

    [HttpPost("test-quote")]
    public ActionResult<ApiResponse<TestCourierQuoteResponseDto>> TestQuote([FromBody] TestCourierQuoteRequestDto request)
        => Ok(new ApiResponse<TestCourierQuoteResponseDto>
        {
            Success = true,
            Data = new TestCourierQuoteResponseDto { BaseFee = 2, DistanceFee = 5, WeightFee = 5.5m, Total = 12.5m, Currency = "USD" }
        });
}
