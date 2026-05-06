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

    
    [HttpPatch("{id:guid}")]
    public ActionResult<ApiResponse<bool>> Update(Guid id, [FromBody] UpdateMenuRequestDto request)
        => Ok(id == Guid.Empty
            ? ApiResponse<bool>.Fail("Menu not found.")
            : ApiResponse<bool>.Ok(true, "Menu updated."));

    [HttpGet("by-client/{clientId:guid}")]
    public ActionResult<ApiResponse<IReadOnlyList<MenuResponseDto>>> ByClient(Guid clientId)
        => Ok(new ApiResponse<IReadOnlyList<MenuResponseDto>> { Success = true, Data = Array.Empty<MenuResponseDto>() });

    [HttpPatch("categories/{id:guid}")]
    public ActionResult<ApiResponse<bool>> UpdateCategory(Guid id, [FromBody] UpdateMenuCategoryRequestDto request)
        => Ok(id == Guid.Empty
            ? ApiResponse<bool>.Fail("Menu category not found.")
            : ApiResponse<bool>.Ok(true, "Menu category updated."));

    [HttpPost("categories")]
    public ActionResult<ApiResponse<Guid>> AddCategory([FromBody] CreateMenuCategoryRequestDto request)
        => Ok(new ApiResponse<Guid> { Success = true, Data = Guid.NewGuid() });

    [HttpPatch("items/{id:guid}")]
    public ActionResult<ApiResponse<bool>> UpdateItem(Guid id, [FromBody] UpdateMenuItemRequestDto request)
        => Ok(id == Guid.Empty
            ? ApiResponse<bool>.Fail("Menu item not found.")
            : ApiResponse<bool>.Ok(true, "Menu item updated."));

    [HttpPost("items")]
    public ActionResult<ApiResponse<Guid>> AddItem([FromBody] CreateMenuItemRequestDto request)
        => Ok(new ApiResponse<Guid> { Success = true, Data = Guid.NewGuid() });

    [HttpGet("{menuId:guid}/items")]
    public ActionResult<ApiResponse<IReadOnlyList<MenuItemResponseDto>>> Items(Guid menuId)
        => Ok(new ApiResponse<IReadOnlyList<MenuItemResponseDto>> { Success = true, Data = Array.Empty<MenuItemResponseDto>() });
}
