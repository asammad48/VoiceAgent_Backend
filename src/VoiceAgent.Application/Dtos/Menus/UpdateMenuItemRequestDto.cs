namespace VoiceAgent.Application.Dtos.Menus;

public sealed class UpdateMenuItemRequestDto
{
    public string? Name { get; set; }
    public decimal? BasePrice { get; set; }
    public string? Currency { get; set; }
}
