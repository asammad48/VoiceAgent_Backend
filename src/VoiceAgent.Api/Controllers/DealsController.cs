using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Dtos.Deals;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Pagination;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DealsController(IRestaurantDealService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateDealRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<Guid> { Success = true, Data = await service.CreateDealAsync(request, ct) });

    [HttpGet("by-client/{clientId:guid}")]
    public async Task<ActionResult<ApiResponse<PagedResponseDto<DealResponseDto>>>> ByClient(Guid clientId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(new ApiResponse<PagedResponseDto<DealResponseDto>> { Success = true, Data = await service.GetDealsByClientAsync(clientId, pageNumber, pageSize, ct) });

    [HttpPatch("{dealId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateDeal(Guid dealId, [FromBody] UpdateDealRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<bool> { Success = true, Data = await service.UpdateDealAsync(dealId, request, ct) });

    [HttpDelete("{dealId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteDeal(Guid dealId, [FromQuery] bool softDelete = true, CancellationToken ct = default)
        => Ok(new ApiResponse<bool> { Success = true, Data = await service.DeleteDealAsync(dealId, softDelete, ct) });

    [HttpPost("{dealId:guid}/items")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddDealItem(Guid dealId, [FromBody] UpsertDealItemRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<Guid> { Success = true, Data = await service.AddDealItemAsync(dealId, request, ct) });

    [HttpPatch("{dealId:guid}/items/{itemId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateDealItem(Guid dealId, Guid itemId, [FromBody] UpsertDealItemRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<bool> { Success = true, Data = await service.UpdateDealItemAsync(dealId, itemId, request, ct) });

    [HttpDelete("{dealId:guid}/items/{itemId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteDealItem(Guid dealId, Guid itemId, CancellationToken ct)
        => Ok(new ApiResponse<bool> { Success = true, Data = await service.DeleteDealItemAsync(dealId, itemId, ct) });

    [HttpPost("{dealId:guid}/addons")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddDealAddon(Guid dealId, [FromBody] UpsertDealAddonRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<Guid> { Success = true, Data = await service.AddDealAddonAsync(dealId, request, ct) });

    [HttpPatch("{dealId:guid}/addons/{addonId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateDealAddon(Guid dealId, Guid addonId, [FromBody] UpsertDealAddonRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<bool> { Success = true, Data = await service.UpdateDealAddonAsync(dealId, addonId, request, ct) });

    [HttpDelete("{dealId:guid}/addons/{addonId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteDealAddon(Guid dealId, Guid addonId, CancellationToken ct)
        => Ok(new ApiResponse<bool> { Success = true, Data = await service.DeleteDealAddonAsync(dealId, addonId, ct) });

    [HttpPost("{dealId:guid}/choice-groups")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddChoiceGroup(Guid dealId, [FromBody] UpsertDealChoiceGroupRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<Guid> { Success = true, Data = await service.AddChoiceGroupAsync(dealId, request, ct) });

    [HttpPatch("{dealId:guid}/choice-groups/{groupId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateChoiceGroup(Guid dealId, Guid groupId, [FromBody] UpsertDealChoiceGroupRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<bool> { Success = true, Data = await service.UpdateChoiceGroupAsync(dealId, groupId, request, ct) });

    [HttpDelete("{dealId:guid}/choice-groups/{groupId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteChoiceGroup(Guid dealId, Guid groupId, CancellationToken ct)
        => Ok(new ApiResponse<bool> { Success = true, Data = await service.DeleteChoiceGroupAsync(dealId, groupId, ct) });
}
