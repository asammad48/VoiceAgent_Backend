using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Dtos.Outbound;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Pagination;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Services;
public class OutboundService(IAppDbContext db) : IOutboundService
{
    public async Task<Guid> CreateRunAsync(CreateOutboundRunRequestDto request, CancellationToken ct = default)
    { var run = new OutboundCampaignRun { Id = Guid.NewGuid(), TenantId = request.TenantId, ClientId = request.ClientId, CampaignId = request.CampaignId, Name = request.Name, Status = "created", StartedAt = DateTime.UtcNow }; db.OutboundCampaignRuns.Add(run); await db.SaveChangesAsync(ct); return run.Id; }
    public async Task<PagedResponseDto<OutboundRunDto>> SearchRunsAsync(PagedRequestDto request, CancellationToken ct = default)
    { var q = db.OutboundCampaignRuns.AsQueryable(); var total = await q.CountAsync(ct); var items = await q.OrderByDescending(x=>x.StartedAt).Skip((request.PageNumber-1)*request.PageSize).Take(request.PageSize).Select(x=>new OutboundRunDto{Id=x.Id,CampaignId=x.CampaignId,Name=x.Name,Status=x.Status,StartedAt=x.StartedAt}).ToListAsync(ct); return new PagedResponseDto<OutboundRunDto>{Items=items,PageNumber=request.PageNumber,PageSize=request.PageSize,TotalCount=total,TotalPages=(int)Math.Ceiling(total/(double)request.PageSize)}; }
    public async Task<Guid> UpsertLeadAsync(Guid runId, UpsertOutboundLeadRequestDto request, CancellationToken ct = default)
    { var lead = await db.OutboundLeads.FirstOrDefaultAsync(x=>x.CampaignId==request.CampaignId && x.Phone==request.Phone, ct); if(lead is null){ lead = new OutboundLead{Id=Guid.NewGuid(),TenantId=request.TenantId,ClientId=request.ClientId,CampaignId=request.CampaignId,Name=request.Name,Phone=request.Phone,Email=request.Email,DataJson=request.DataJson,Status=request.Status,OptedOut=request.OptedOut}; db.OutboundLeads.Add(lead);} else { lead.Name=request.Name; lead.Email=request.Email; lead.DataJson=request.DataJson; lead.Status=request.Status; lead.OptedOut=request.OptedOut;} await db.SaveChangesAsync(ct); return lead.Id; }
    public async Task<PagedResponseDto<OutboundLeadDto>> SearchLeadsAsync(Guid runId, PagedRequestDto request, CancellationToken ct = default)
    { var run = await db.OutboundCampaignRuns.FirstOrDefaultAsync(x=>x.Id==runId, ct); if(run is null) return new PagedResponseDto<OutboundLeadDto>{PageNumber=request.PageNumber,PageSize=request.PageSize,TotalCount=0,TotalPages=0}; var q=db.OutboundLeads.Where(x=>x.CampaignId==run.CampaignId); var total=await q.CountAsync(ct); var items=await q.Skip((request.PageNumber-1)*request.PageSize).Take(request.PageSize).Select(x=>new OutboundLeadDto{Id=x.Id,CampaignId=x.CampaignId,Name=x.Name,Phone=x.Phone,Status=x.Status,OptedOut=x.OptedOut}).ToListAsync(ct); return new PagedResponseDto<OutboundLeadDto>{Items=items,PageNumber=request.PageNumber,PageSize=request.PageSize,TotalCount=total,TotalPages=(int)Math.Ceiling(total/(double)request.PageSize)}; }
}
