namespace VoiceAgent.Application.Dtos.Tenants;

public class CreateTenantRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string DefaultTimezone { get; set; } = "UTC";
    public string DefaultCurrency { get; set; } = "USD";
}

public class UpdateTenantRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string DefaultTimezone { get; set; } = "UTC";
    public string DefaultCurrency { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
}

public class TenantResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
