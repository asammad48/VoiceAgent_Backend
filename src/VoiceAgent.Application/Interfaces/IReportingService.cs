using VoiceAgent.Application.Dtos.Reports;
using VoiceAgent.Common.Pagination;

namespace VoiceAgent.Application.Interfaces;
public interface IReportingService
{
    Task<PagedResponseDto<BillingCostReportItemDto>> GetBillingCostsAsync(PagedRequestDto request, CancellationToken ct = default);
    Task<PagedResponseDto<CallUsageReportItemDto>> GetCallUsageAsync(PagedRequestDto request, CancellationToken ct = default);
    Task<PagedResponseDto<OutboundPerformanceReportItemDto>> GetOutboundPerformanceAsync(PagedRequestDto request, CancellationToken ct = default);
}
