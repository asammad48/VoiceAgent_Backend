namespace VoiceAgent.Common.Pagination;

public sealed class SortRuleDto
{
    public string Field { get; set; } = string.Empty;
    public bool Descending { get; set; }
}
