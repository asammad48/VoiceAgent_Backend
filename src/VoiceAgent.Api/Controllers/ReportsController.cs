using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Dtos.Reports;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Pagination;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController(IReportingService service) : ControllerBase
{
    [HttpPost("billing/costs")]
    public async Task<ActionResult<ApiResponse<PagedResponseDto<BillingCostReportItemDto>>>> BillingCosts([FromBody] PagedRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<PagedResponseDto<BillingCostReportItemDto>> { Success = true, Data = await service.GetBillingCostsAsync(request, ct) });

    [HttpPost("calls/usage")]
    public async Task<ActionResult<ApiResponse<PagedResponseDto<CallUsageReportItemDto>>>> CallUsage([FromBody] PagedRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<PagedResponseDto<CallUsageReportItemDto>> { Success = true, Data = await service.GetCallUsageAsync(request, ct) });

    [HttpPost("outbound/performance")]
    public async Task<ActionResult<ApiResponse<PagedResponseDto<OutboundPerformanceReportItemDto>>>> OutboundPerformance([FromBody] PagedRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<PagedResponseDto<OutboundPerformanceReportItemDto>> { Success = true, Data = await service.GetOutboundPerformanceAsync(request, ct) });
}
