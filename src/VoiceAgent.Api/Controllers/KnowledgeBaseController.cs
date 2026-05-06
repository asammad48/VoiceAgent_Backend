using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Dtos.Knowledge;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api")]
public class KnowledgeBaseController(IKnowledgeBaseService service) : ControllerBase
{
    [HttpPost("knowledge-bases")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateBase([FromBody] CreateKnowledgeBaseRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<Guid> { Success = true, Data = await service.CreateBaseAsync(request, ct) });

    [HttpPost("knowledge-documents")]
    public ActionResult<ApiResponse<Guid>> CreateDocument([FromBody] CreateKnowledgeDocumentRequestDto request)
        => Ok(new ApiResponse<Guid> { Success = true, Data = Guid.NewGuid() });

    [HttpPost("knowledge/search")]
    public ActionResult<ApiResponse<IReadOnlyList<SearchKnowledgeResponseDto>>> Search([FromBody] SearchKnowledgeRequestDto request)
        => Ok(new ApiResponse<IReadOnlyList<SearchKnowledgeResponseDto>> { Success = true, Data = Array.Empty<SearchKnowledgeResponseDto>() });

    [HttpPost("knowledge/reindex")]
    public ActionResult<ApiResponse<ReindexKnowledgeResponseDto>> Reindex([FromBody] ReindexKnowledgeRequestDto request)
        => Ok(new ApiResponse<ReindexKnowledgeResponseDto>
        {
            Success = true,
            Data = new ReindexKnowledgeResponseDto
            {
                KnowledgeBaseId = request.KnowledgeBaseId,
                Status = "Queued",
                DocumentsQueued = 0,
                Message = "Reindex job queued successfully"
            }
        });
}
