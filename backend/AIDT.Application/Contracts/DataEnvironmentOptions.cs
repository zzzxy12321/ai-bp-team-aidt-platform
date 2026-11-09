namespace AIDT.Application.Contracts;

public sealed class DataEnvironmentOptions
{
    public string CurrentEnvironmentCode { get; set; } = "demo";

    public bool IncludeTestDataByDefault { get; set; }
}
