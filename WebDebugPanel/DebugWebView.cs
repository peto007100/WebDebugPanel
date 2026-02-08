namespace WebDebugPanel;

/// <summary>
/// WebView com "ponte" JS->C# (Android). O JS chama: window.mauiBridge.postMessage("...").
/// </summary>
public class DebugWebView : WebView
{
    public event EventHandler<string>? MessageReceived;

    internal void RaiseMessageReceived(string message)
        => MessageReceived?.Invoke(this, message);
}
