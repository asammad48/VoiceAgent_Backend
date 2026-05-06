using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Dtos.Reports;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Pagination;

namespace VoiceAgent.Application.Services;
public class ReportingService(IAppDbContext db) : IReportingService
{
    public async Task<PagedResponseDto<BillingCostReportItemDto>> GetBillingCostsAsync(PagedRequestDto request, CancellationToken ct = default)
    { var q = db.CallCostLogs.AsQueryable(); var total = await q.CountAsync(ct); var items = await q.OrderByDescending(x=>x.CreatedOn).Skip((request.PageNumber-1)*request.PageSize).Take(request.PageSize).Select(x=>new BillingCostReportItemDto{TenantId=x.TenantId,ClientId=x.ClientId,CampaignId=x.CampaignId,EstimatedCost=x.EstimatedCost,CreatedOn=x.CreatedOn}).ToListAsync(ct); return new PagedResponseDto<BillingCostReportItemDto>{Items=items,PageNumber=request.PageNumber,PageSize=request.PageSize,TotalCount=total,TotalPages=(int)Math.Ceiling(total/(double)request.PageSize)}; }
    public async Task<PagedResponseDto<CallUsageReportItemDto>> GetCallUsageAsync(PagedRequestDto request, CancellationToken ct = default)
    { var q = db.CallCostLogs.AsQueryable(); var total = await q.CountAsync(ct); var items = await q.OrderByDescending(x=>x.CreatedOn).Skip((request.PageNumber-1)*request.PageSize).Take(request.PageSize).Select(x=>new CallUsageReportItemDto{CallSessionId=x.CallSessionId,LlmInputTokens=x.LlmInputTokens,LlmOutputTokens=x.LlmOutputTokens,TtsCharacters=x.TtsCharacters,SttAudioSeconds=x.SttAudioSeconds,CallDurationSeconds=x.CallDurationSeconds,CreatedOn=x.CreatedOn}).ToListAsync(ct); return new PagedResponseDto<CallUsageReportItemDto>{Items=items,PageNumber=request.PageNumber,PageSize=request.PageSize,TotalCount=total,TotalPages=(int)Math.Ceiling(total/(double)request.PageSize)}; }
    public async Task<PagedResponseDto<OutboundPerformanceReportItemDto>> GetOutboundPerformanceAsync(PagedRequestDto request, CancellationToken ct = default)
    { var q = db.OutboundLeads.AsQueryable(); var total = await q.CountAsync(ct); var items = await q.OrderByDescending(x=>x.CreatedOn).Skip((request.PageNumber-1)*request.PageSize).Take(request.PageSize).Select(x=>new OutboundPerformanceReportItemDto{LeadId=x.Id,CampaignId=x.CampaignId,Status=x.Status,OptedOut=x.OptedOut,CreatedOn=x.CreatedOn}).ToListAsync(ct); return new PagedResponseDto<OutboundPerformanceReportItemDto>{Items=items,PageNumber=request.PageNumber,PageSize=request.PageSize,TotalCount=total,TotalPages=(int)Math.Ceiling(total/(double)request.PageSize)}; }
}
