namespace VoiceAgent.Application.Dtos.Campaigns;

public sealed class UpdateCampaignRequestDto
{
    public string? Name { get; set; }
    public string? CampaignType { get; set; }
    public string? Direction { get; set; }
    public bool? IsActive { get; set; }
}
