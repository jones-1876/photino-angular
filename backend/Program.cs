using System.Drawing;
using Photino.NET;
using Photino.NET.Server;

namespace GimxBackend;

internal static class Program
{
#if DEBUG
    // Debug mode: the window points at the Angular dev server (ng serve) so the
    // Angular HMR / live-reload loop works inside the native window.
    private const bool IsDebugMode = true;
#else
    // Release mode: the window points at the in-process static file server that
    // serves the Angular production build out of wwwroot.
    private const bool IsDebugMode = false;
#endif

    /// <summary>Where `ng serve` listens. Must match FrontEnd/angular.json serve.options.port.</summary>
    private const string DevServerUrl = "http://localhost:4200";

    private const string WindowTitle = "Gimx";

    [STAThread]
    private static void Main(string[] args)
    {
        // A published app can be launched from any working directory, and PhotinoServer
        // resolves its web root relative to the content root. Pin it to the exe folder.
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        string appUrl;

        // IsDebugMode is a compile-time constant, so one branch below is always folded away.
#pragma warning disable CS0162 // Unreachable code detected
        if (IsDebugMode)
        {
            appUrl = DevServerUrl;
            Console.WriteLine($"[GimxBackend] DEBUG - loading Angular dev server at {appUrl}");
            Console.WriteLine("[GimxBackend] Run `npm start` in FrontEnd first.");
        }
        else
        {
            PhotinoServer
                .CreateStaticFileServer(
                    args,
                    startPort: 8000,
                    portRange: 100,
                    webRootFolder: "wwwroot",
                    out string baseUrl)
                .RunAsync();

            appUrl = $"{baseUrl}/index.html";
        }
#pragma warning restore CS0162

        PhotinoWindow window = new PhotinoWindow()
            .SetTitle(WindowTitle)
            .SetUseOsDefaultSize(false)
            .SetSize(new Size(1280, 800))
            .Center()
            .SetResizable(true)
            // Dev tools and the browser context menu are only useful while developing.
            .SetDevToolsEnabled(IsDebugMode)
            .SetContextMenuEnabled(IsDebugMode)
            .SetLogVerbosity(IsDebugMode ? 1 : 0)
            .RegisterWebMessageReceivedHandler(OnWebMessageReceived)
            .Load(appUrl);

        window.WaitForClose(); // Starts the native event loop and blocks until the window closes.
    }

    /// <summary>
    /// Angular -> .NET bridge. The UI calls window.external.sendMessage(text) and listens
    /// with window.external.receiveMessage(callback); SendWebMessage is the reply channel.
    /// </summary>
    private static void OnWebMessageReceived(object sender, string message)
    {
        PhotinoWindow window = (PhotinoWindow)sender;
        Console.WriteLine($"[GimxBackend] web message: {message}");
        window.SendWebMessage($"pong: {message}");
    }
}
