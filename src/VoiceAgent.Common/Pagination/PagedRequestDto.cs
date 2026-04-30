namespace VoiceAgent.Common.Pagination;

public sealed class PagedRequestDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public List<FilterRuleDto> Filters { get; set; } = new();
    public List<SortRuleDto> Sorts { get; set; } = new();
}
