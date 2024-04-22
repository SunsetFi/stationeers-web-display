using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Xilium.CefGlue;

namespace StationeersWebDisplay.Cef
{
    static class CefHost
    {
        private static bool initialized = false;
        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;

            Logging.LogTrace($"Initializing CEF ");

            Logging.LogTrace($"Starting CEF demo process");
            Dispatcher.RunOnMainThread(async () =>
            {
                try
                {
                    Logging.LogTrace("Loading cef");
                    CefRuntime.Load(StationeersWebDisplayPlugin.AssemblyDirectory);
                    Logging.LogTrace("Cef loaded");

                    var cefArgs = new CefMainArgs(new string[] { "mute-audio" });
                    Logging.LogTrace("Main args created");

                    var cefApp = new OffscreenCefApp();
                    Logging.LogTrace("App created");

                    // This is where the code path diverges for child processes.
                    if (CefRuntime.ExecuteProcess(cefArgs, cefApp, IntPtr.Zero) != -1)
                        Logging.LogError("Could not start the secondary process.");

                    Logging.LogTrace("Executed process");

                    var cefSettings = new CefSettings
                    {
                        BrowserSubprocessPath = Path.Combine(StationeersWebDisplayPlugin.AssemblyDirectory, "CefGlueBrowserProcess/Xilium.CefGlue.BrowserProcess.exe"),
                        MultiThreadedMessageLoop = false,
                        LogSeverity = CefLogSeverity.Verbose,
                        LogFile = "cef.log",
                        WindowlessRenderingEnabled = true,
                        NoSandbox = true,
                    };

                    CefRuntime.Initialize(cefArgs, cefSettings, cefApp, IntPtr.Zero);
                    Logging.LogTrace("CEF Initialized");

                    var cefWindowInfo = CefWindowInfo.Create();
                    cefWindowInfo.SetAsWindowless(IntPtr.Zero, false);
                    Logging.LogTrace("CEF windowless set");

                    CefBrowserSettings cefBrowserSettings = new CefBrowserSettings()
                    {
                        BackgroundColor = new CefColor(0, 0, 0, 255),
                        JavaScript = CefState.Enabled,
                        JavaScriptAccessClipboard = CefState.Disabled,
                        JavaScriptCloseWindows = CefState.Disabled,
                        JavaScriptDomPaste = CefState.Disabled,
                        FileAccessFromFileUrls = CefState.Disabled,
                        Databases = CefState.Disabled,
                        LocalStorage = CefState.Disabled
                    };

                    var windowSize = new Size(1024, 768);

                    var cefClient = new OffscreenCefClient(windowSize);
                    Logging.LogTrace("Creating browser");
                    CefBrowserHost.CreateBrowser(cefWindowInfo, cefClient, cefBrowserSettings, "https://www.google.com");
                    Logging.LogTrace("Browser created");

                    var pump = new GameObject("CefMessagePump");
                    pump.transform.parent = StationeersWebDisplayPlugin.Instance.gameObject.transform;
                    pump.AddComponent<CefMessagePump>();
                    Logging.LogTrace("Cef Message pump started");

                    await Task.Delay(3000);

                    Logging.LogTrace("Starting texture copy");

                    var renderTexture = new Texture2D(windowSize.Width, windowSize.Height, TextureFormat.BGRA32, false);
                    cefClient.CopyToTexture(renderTexture);
                    Logging.LogTrace("Copied to texture");

                    var textureBytes = renderTexture.EncodeToPNG();
                    File.WriteAllBytes(Path.Combine(StationeersWebDisplayPlugin.AssemblyDirectory, "test.png"), textureBytes);

                    Logging.LogTrace("Written to file");
                }
                catch (Exception ex)
                {
                    Logging.LogError($"Failed to initialize CEF: {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}");
                }
            });
        }

        private class CefMessagePump : MonoBehaviour
        {
            void Update()
            {
                try
                {
                    CefRuntime.DoMessageLoopWork();
                }
                catch(Exception ex)
                {
                    Logging.LogError($"Failed to DoMessageLoopWork: {ex.GetType().FullName} {ex.Message} {ex.StackTrace}");
                }
            }
        }
    }
}