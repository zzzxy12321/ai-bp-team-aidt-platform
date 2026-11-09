using AIDT.Application.Contracts;

namespace AIDT.Mcp;

public sealed class McpAmbientIdentity
{
    private readonly AsyncLocal<McpTokenIdentity?> _current = new();

    public McpTokenIdentity? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
