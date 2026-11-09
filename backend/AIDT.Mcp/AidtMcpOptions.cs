namespace AIDT.Mcp;

public sealed class AidtMcpOptions
{
    public const string SectionName = "AIDT:Mcp";

    public string PublicBaseUrl { get; set; } = "http://127.0.0.1:8088";

    public Guid SystemAuditUserId { get; set; } =
        Guid.Parse("10000000-0000-0000-0000-000000000024");

    public int MaxRequestsPerMinute { get; set; } = 120;

    public int MaxResponseBytes { get; set; } = 1_048_576;

    public long MaxRequestBytes { get; set; } = 10 * 1024 * 1024;

    public string[] AllowedOrigins { get; set; } = [];
}
