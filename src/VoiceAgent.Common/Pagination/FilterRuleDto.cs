namespace VoiceAgent.Common.Pagination;

public sealed class FilterRuleDto
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = "eq";
    public string? Value { get; set; }
}
