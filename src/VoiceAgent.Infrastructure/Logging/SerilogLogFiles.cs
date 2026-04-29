namespace VoiceAgent.Infrastructure.Logging;

public static class SerilogLogFiles
{
    public const string Api = "logs/api-.log";
    public const string Worker = "logs/worker-.log";
    public const string FreeSwitch = "logs/freeswitch-.log";
    public const string ProviderErrors = "logs/provider-errors-.log";
    public const string BusinessLogsRule = "Business logs go to PostgreSQL.";
}
