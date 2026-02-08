namespace WebDebugPanel.Models;

public sealed class DevLogItem
{
    public DateTimeOffset Time { get; init; } = DateTimeOffset.Now;
    public string Type { get; init; } = "log";  // log|warn|error|info|net|unhandled|raw
    public string Message { get; init; } = "";
}
