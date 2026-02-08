using Microsoft.Extensions.Logging;

namespace WebDebugPanel;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.ConfigureMauiHandlers(handlers =>
        {
#if ANDROID
            handlers.AddHandler(typeof(DebugWebView), typeof(Platforms.Android.DebugWebViewHandler));
#endif
        });

        return builder.Build();
    }
}
