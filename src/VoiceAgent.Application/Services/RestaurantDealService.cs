using Microsoft.EntityFrameworkCore;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Dtos.Deals;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Common.Pagination;
using VoiceAgent.Domain.Entities;

namespace VoiceAgent.Application.Services;

public class RestaurantDealService(IAppDbContext db) : IRestaurantDealService
{
    public async Task<Guid> CreateDealAsync(CreateDealRequestDto request, CancellationToken ct = default)
    {
        var e = new RestaurantDeal { Id = Guid.NewGuid(), TenantId = request.TenantId, ClientId = request.ClientId, BranchId = request.BranchId, Name = request.Name, DealPrice = request.DealPrice, Currency = request.Currency, Description = string.Empty, AvailabilityScheduleJson = "{}", MetadataJson = "{}", IsAvailable = true };
        db.RestaurantDeals.Add(e);
        await db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<PagedResponseDto<DealResponseDto>> GetDealsByClientAsync(Guid clientId, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var q = db.RestaurantDeals.Where(x => x.ClientId == clientId && x.IsActive);
        var total = await q.CountAsync(ct);
        var items = await q.Skip((pageNumber - 1) * pageSize).Take(pageSize).Select(x => new DealResponseDto { Id = x.Id, Name = x.Name, DealPrice = x.DealPrice, Currency = x.Currency, IsAvailable = x.IsAvailable }).ToListAsync(ct);
        return new PagedResponseDto<DealResponseDto> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize) };
    }

    public async Task<bool> UpdateDealAsync(Guid dealId, UpdateDealRequestDto request, CancellationToken ct = default)
    {
        var deal = await db.RestaurantDeals.FirstOrDefaultAsync(x => x.Id == dealId, ct); if (deal is null) return false;
        deal.Name = request.Name; deal.Description = request.Description; deal.DealPrice = request.DealPrice; deal.Currency = request.Currency; deal.IsAvailable = request.IsAvailable;
        await db.SaveChangesAsync(ct); return true;
    }
    public async Task<bool> DeleteDealAsync(Guid dealId, bool softDelete, CancellationToken ct = default)
    {
        var deal = await db.RestaurantDeals.FirstOrDefaultAsync(x => x.Id == dealId, ct); if (deal is null) return false;
        if (softDelete) deal.IsActive = false; else db.RestaurantDeals.Remove(deal);
        await db.SaveChangesAsync(ct); return true;
    }

    public async Task<Guid> AddDealItemAsync(Guid dealId, UpsertDealItemRequestDto request, CancellationToken ct = default) { var e = new RestaurantDealItem { Id = Guid.NewGuid(), DealId = dealId, TenantId = request.TenantId, ClientId = request.ClientId, MenuItemId = request.MenuItemId, MenuItemVariantId = request.MenuItemVariantId, Quantity = request.Quantity, IsRequired = request.IsRequired }; db.RestaurantDealItems.Add(e); await db.SaveChangesAsync(ct); return e.Id; }
    public async Task<bool> UpdateDealItemAsync(Guid dealId, Guid itemId, UpsertDealItemRequestDto request, CancellationToken ct = default) { var e = await db.RestaurantDealItems.FirstOrDefaultAsync(x => x.DealId == dealId && x.Id == itemId, ct); if (e is null) return false; e.MenuItemId = request.MenuItemId; e.MenuItemVariantId = request.MenuItemVariantId; e.Quantity = request.Quantity; e.IsRequired = request.IsRequired; await db.SaveChangesAsync(ct); return true; }
    public async Task<bool> DeleteDealItemAsync(Guid dealId, Guid itemId, CancellationToken ct = default) { var e = await db.RestaurantDealItems.FirstOrDefaultAsync(x => x.DealId == dealId && x.Id == itemId, ct); if (e is null) return false; db.RestaurantDealItems.Remove(e); await db.SaveChangesAsync(ct); return true; }
    public async Task<Guid> AddDealAddonAsync(Guid dealId, UpsertDealAddonRequestDto request, CancellationToken ct = default) { var e = new RestaurantDealAddon { Id = Guid.NewGuid(), DealId = dealId, TenantId = request.TenantId, ClientId = request.ClientId, MenuItemAddonId = request.MenuItemAddonId, Quantity = request.Quantity, IsIncluded = request.IsIncluded, ExtraPrice = request.ExtraPrice }; db.RestaurantDealAddons.Add(e); await db.SaveChangesAsync(ct); return e.Id; }
    public async Task<bool> UpdateDealAddonAsync(Guid dealId, Guid addonId, UpsertDealAddonRequestDto request, CancellationToken ct = default) { var e = await db.RestaurantDealAddons.FirstOrDefaultAsync(x => x.DealId == dealId && x.Id == addonId, ct); if (e is null) return false; e.MenuItemAddonId = request.MenuItemAddonId; e.Quantity = request.Quantity; e.IsIncluded = request.IsIncluded; e.ExtraPrice = request.ExtraPrice; await db.SaveChangesAsync(ct); return true; }
    public async Task<bool> DeleteDealAddonAsync(Guid dealId, Guid addonId, CancellationToken ct = default) { var e = await db.RestaurantDealAddons.FirstOrDefaultAsync(x => x.DealId == dealId && x.Id == addonId, ct); if (e is null) return false; db.RestaurantDealAddons.Remove(e); await db.SaveChangesAsync(ct); return true; }
    public async Task<Guid> AddChoiceGroupAsync(Guid dealId, UpsertDealChoiceGroupRequestDto request, CancellationToken ct = default) { var e = new RestaurantDealChoiceGroup { Id = Guid.NewGuid(), DealId = dealId, TenantId = request.TenantId, ClientId = request.ClientId, Name = request.Name, MinSelections = request.MinSelections, MaxSelections = request.MaxSelections, SortOrder = request.SortOrder, OptionsJson = request.OptionsJson }; db.RestaurantDealChoiceGroups.Add(e); await db.SaveChangesAsync(ct); return e.Id; }
    public async Task<bool> UpdateChoiceGroupAsync(Guid dealId, Guid groupId, UpsertDealChoiceGroupRequestDto request, CancellationToken ct = default) { var e = await db.RestaurantDealChoiceGroups.FirstOrDefaultAsync(x => x.DealId == dealId && x.Id == groupId, ct); if (e is null) return false; e.Name = request.Name; e.MinSelections = request.MinSelections; e.MaxSelections = request.MaxSelections; e.SortOrder = request.SortOrder; e.OptionsJson = request.OptionsJson; await db.SaveChangesAsync(ct); return true; }
    public async Task<bool> DeleteChoiceGroupAsync(Guid dealId, Guid groupId, CancellationToken ct = default) { var e = await db.RestaurantDealChoiceGroups.FirstOrDefaultAsync(x => x.DealId == dealId && x.Id == groupId, ct); if (e is null) return false; db.RestaurantDealChoiceGroups.Remove(e); await db.SaveChangesAsync(ct); return true; }
}
