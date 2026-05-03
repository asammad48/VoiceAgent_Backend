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
    public ActionResult<ApiResponse<CreateKnowledgeDocumentRequestDto>> CreateDocument([FromBody] CreateKnowledgeDocumentRequestDto request)
        => Ok(new ApiResponse<CreateKnowledgeDocumentRequestDto> { Success = true, Data = request });

    [HttpPost("knowledge/search")]
    public ActionResult<ApiResponse<IReadOnlyList<SearchKnowledgeResponseDto>>> Search([FromBody] SearchKnowledgeRequestDto request)
        => Ok(new ApiResponse<IReadOnlyList<SearchKnowledgeResponseDto>> { Success = true, Data = Array.Empty<SearchKnowledgeResponseDto>() });

    [HttpPost("knowledge/reindex")]
    public ActionResult<ApiResponse<object>> Reindex([FromBody] CreateKnowledgeBaseRequestDto request)
        => Ok(new ApiResponse<object> { Success = true, Data = new { accepted = true, request.CampaignId } });
}
