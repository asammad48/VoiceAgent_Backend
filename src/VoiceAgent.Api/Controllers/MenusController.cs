using Microsoft.AspNetCore.Mvc;
using VoiceAgent.Application.Dtos.Menus;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Responses;

namespace VoiceAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenusController(IRestaurantMenuService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateMenuRequestDto request, CancellationToken ct)
        => Ok(new ApiResponse<Guid> { Success = true, Data = await service.CreateMenuAsync(request, ct) });

    [HttpGet("by-client/{clientId:guid}")]
    public ActionResult<ApiResponse<object>> ByClient(Guid clientId)
        => Ok(new ApiResponse<object> { Success = true, Data = new { clientId, items = Array.Empty<object>() } });

    [HttpPost("categories")]
    public ActionResult<ApiResponse<CreateMenuCategoryRequestDto>> AddCategory([FromBody] CreateMenuCategoryRequestDto request)
        => Ok(new ApiResponse<CreateMenuCategoryRequestDto> { Success = true, Data = request });

    [HttpPost("items")]
    public ActionResult<ApiResponse<CreateMenuItemRequestDto>> AddItem([FromBody] CreateMenuItemRequestDto request)
        => Ok(new ApiResponse<CreateMenuItemRequestDto> { Success = true, Data = request });

    [HttpGet("{menuId:guid}/items")]
    public ActionResult<ApiResponse<object>> Items(Guid menuId)
        => Ok(new ApiResponse<object> { Success = true, Data = new { menuId, items = Array.Empty<object>() } });
}
