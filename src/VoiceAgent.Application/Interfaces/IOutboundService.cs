using VoiceAgent.Application.Dtos.Outbound;
using VoiceAgent.Common.Pagination;

namespace VoiceAgent.Application.Interfaces;
public interface IOutboundService
{
    Task<Guid> CreateRunAsync(CreateOutboundRunRequestDto request, CancellationToken ct = default);
    Task<PagedResponseDto<OutboundRunDto>> SearchRunsAsync(PagedRequestDto request, CancellationToken ct = default);
    Task<Guid> UpsertLeadAsync(Guid runId, UpsertOutboundLeadRequestDto request, CancellationToken ct = default);
    Task<PagedResponseDto<OutboundLeadDto>> SearchLeadsAsync(Guid runId, PagedRequestDto request, CancellationToken ct = default);
}
