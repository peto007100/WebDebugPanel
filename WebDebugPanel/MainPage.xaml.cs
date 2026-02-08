using System.Collections.ObjectModel;
using System.Text.Json;
using WebDebugPanel.Models;

namespace WebDebugPanel;

public partial class MainPage : ContentPage
{
    public ObservableCollection<DevLogItem> Logs { get; } = new();

    bool _panelCollapsed = false;
    double _panelExpandedHeight = 260;

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;

        SizeChanged += (_, __) => EnsurePanelHeight();
        EnsurePanelHeight();

        Browser.Navigated += async (_, __) =>
        {
            // tenta injetar automaticamente (pode falhar em páginas com CSP rígida)
            await TryInjectConsoleHookAsync();
        };

        Browser.MessageReceived += (_, raw) =>
        {
            try
            {
                var msg = JsonSerializer.Deserialize<BridgeMessage>(raw);
                if (msg is null) return;

                var text = msg.message ?? "";
                var type = msg.type ?? "log";

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Logs.Add(new DevLogItem
                    {
                        Time = DateTimeOffset.Now,
                        Type = type,
                        Message = text
                    });

                    if (Logs.Count > 0)
                        LogsView.ScrollTo(Logs[^1], position: ScrollToPosition.End, animate: false);
                });
            }
            catch
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Logs.Add(new DevLogItem
                    {
                        Time = DateTimeOffset.Now,
                        Type = "raw",
                        Message = raw
                    });
                });
            }
        };

        Browser.Source = UrlEntry.Text;
    }

    void EnsurePanelHeight()
    {
        BottomPanel.HeightRequest = _panelCollapsed ? 48 : _panelExpandedHeight;
    }

    async void OnLoadClicked(object sender, EventArgs e)
    {
        var url = (UrlEntry.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(url)) return;

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
            UrlEntry.Text = url;
        }

        Logs.Add(new DevLogItem { Time = DateTimeOffset.Now, Type = "info", Message = $"Navegando para: {url}" });
        Browser.Source = url;

        await Task.Delay(150);
        await TryInjectConsoleHookAsync();
    }

    void OnClearClicked(object sender, EventArgs e) => Logs.Clear();

    void OnTogglePanelClicked(object sender, EventArgs e)
    {
        _panelCollapsed = !_panelCollapsed;
        EnsurePanelHeight();
    }

    async void OnInjectClicked(object sender, EventArgs e)
    {
        await TryInjectConsoleHookAsync(force: true);
    }

    async Task TryInjectConsoleHookAsync(bool force = false)
    {
        var js = BuildHookScript(force);

        try
        {
            await Browser.EvaluateJavaScriptAsync(js);
            MainThread.BeginInvokeOnMainThread(() =>
                Logs.Add(new DevLogItem { Time = DateTimeOffset.Now, Type = "info", Message = "Hook de console injetado (se a página permitir)." })
            );
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
                Logs.Add(new DevLogItem { Time = DateTimeOffset.Now, Type = "warn", Message = $"Falha ao injetar hook (CSP/JS?): {ex.Message}" })
            );
        }
    }

    static string BuildHookScript(bool force)
    {
        var forceFlag = force ? "true" : "false";

        return $@"
(function() {{
  try {{
    if (!window.mauiBridge || !window.mauiBridge.postMessage) return;

    if (window.__mauiHookInstalled && !{forceFlag}) return;
    window.__mauiHookInstalled = true;

    function safeToString(v) {{
      try {{
        if (typeof v === 'string') return v;
        if (v instanceof Error) return (v.stack || v.message || String(v));
        return JSON.stringify(v);
      }} catch(e) {{
        try {{ return String(v); }} catch(_) {{ return '[unprintable]'; }}
      }}
    }}

    function emit(type, args) {{
      try {{
        var text = '';
        for (var i=0;i<args.length;i++) {{
          text += (i ? ' ' : '') + safeToString(args[i]);
        }}
        window.mauiBridge.postMessage(JSON.stringify({{ type: type, message: text }}));
      }} catch(e) {{}}
    }}

    var original = {{
      log: console.log,
      warn: console.warn,
      error: console.error,
      info: console.info
    }};

    console.log = function() {{ emit('log', arguments); original.log.apply(console, arguments); }};
    console.warn = function() {{ emit('warn', arguments); original.warn.apply(console, arguments); }};
    console.error = function() {{ emit('error', arguments); original.error.apply(console, arguments); }};
    console.info = function() {{ emit('info', arguments); original.info.apply(console, arguments); }};

    window.addEventListener('error', function(ev) {{
      try {{
        emit('unhandled', [ev.message, ev.filename + ':' + ev.lineno + ':' + ev.colno]);
      }} catch(e) {{}}
    }});

    if (window.fetch && !window.__mauiFetchHook) {{
      window.__mauiFetchHook = true;
      var _fetch = window.fetch;
      window.fetch = function() {{
        var url = arguments[0];
        var start = Date.now();
        return _fetch.apply(this, arguments).then(function(resp) {{
          var ms = Date.now() - start;
          emit('net', ['fetch', resp.status, url, ms + 'ms']);
          return resp;
        }}).catch(function(err) {{
          var ms = Date.now() - start;
          emit('net', ['fetch', 'ERR', url, ms + 'ms', err]);
          throw err;
        }});
      }}
    }}

    emit('info', ['MAUI hook ativo']);
  }} catch(e) {{}}
}})();
";
    }

    private sealed class BridgeMessage
    {
        public string? type { get; set; }
        public string? message { get; set; }
    }
}
