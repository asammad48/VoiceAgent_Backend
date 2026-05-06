using VoiceAgent.Application.Dtos.Deals;
using VoiceAgent.Common.Pagination;

namespace VoiceAgent.Application.Interfaces;

public interface IRestaurantDealService
{
    Task<Guid> CreateDealAsync(CreateDealRequestDto request, CancellationToken ct = default);
    Task<PagedResponseDto<DealResponseDto>> GetDealsByClientAsync(Guid clientId, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<bool> UpdateDealAsync(Guid dealId, UpdateDealRequestDto request, CancellationToken ct = default);
    Task<bool> DeleteDealAsync(Guid dealId, bool softDelete, CancellationToken ct = default);
    Task<Guid> AddDealItemAsync(Guid dealId, UpsertDealItemRequestDto request, CancellationToken ct = default);
    Task<bool> UpdateDealItemAsync(Guid dealId, Guid itemId, UpsertDealItemRequestDto request, CancellationToken ct = default);
    Task<bool> DeleteDealItemAsync(Guid dealId, Guid itemId, CancellationToken ct = default);
    Task<Guid> AddDealAddonAsync(Guid dealId, UpsertDealAddonRequestDto request, CancellationToken ct = default);
    Task<bool> UpdateDealAddonAsync(Guid dealId, Guid addonId, UpsertDealAddonRequestDto request, CancellationToken ct = default);
    Task<bool> DeleteDealAddonAsync(Guid dealId, Guid addonId, CancellationToken ct = default);
    Task<Guid> AddChoiceGroupAsync(Guid dealId, UpsertDealChoiceGroupRequestDto request, CancellationToken ct = default);
    Task<bool> UpdateChoiceGroupAsync(Guid dealId, Guid groupId, UpsertDealChoiceGroupRequestDto request, CancellationToken ct = default);
    Task<bool> DeleteChoiceGroupAsync(Guid dealId, Guid groupId, CancellationToken ct = default);
}
