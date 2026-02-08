#if ANDROID
using Android.Webkit;
using Java.Interop;
using Microsoft.Maui.Handlers;

namespace WebDebugPanel.Platforms.Android;

/// <summary>
/// Handler Android que adiciona uma JS interface "mauiBridge" ao WebView.
/// Também habilita WebContents debugging (para usar chrome://inspect no PC).
/// </summary>
public class DebugWebViewHandler : WebViewHandler
{
    JsBridge? _bridge;

    protected override void ConnectHandler(WebView platformView)
    {
        base.ConnectHandler(platformView);

        platformView.Settings.JavaScriptEnabled = true;
        platformView.Settings.DomStorageEnabled = true;

#if DEBUG
        WebView.SetWebContentsDebuggingEnabled(true);
#endif

        _bridge = new JsBridge(this);
        platformView.AddJavascriptInterface(_bridge, "mauiBridge");
    }

    protected override void DisconnectHandler(WebView platformView)
    {
        try
        {
            if (_bridge is not null)
                platformView.RemoveJavascriptInterface("mauiBridge");
        }
        catch { /* ignore */ }

        _bridge = null;
        base.DisconnectHandler(platformView);
    }

    internal void OnJsMessage(string message)
    {
        if (VirtualView is DebugWebView dv)
            dv.RaiseMessageReceived(message);
    }

    private sealed class JsBridge : Java.Lang.Object
    {
        readonly DebugWebViewHandler _handler;

        public JsBridge(DebugWebViewHandler handler) => _handler = handler;

        [JavascriptInterface]
        [Export("postMessage")]
        public void PostMessage(string message) => _handler.OnJsMessage(message);
    }
}
#endif