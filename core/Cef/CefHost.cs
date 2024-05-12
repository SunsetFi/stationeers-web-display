using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Xilium.CefGlue;

namespace StationeersWebDisplay.Cef
{
    public static class CefHost
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

            try
            {
                Logging.LogTrace("Loading CEF");
                CefRuntime.Load(StationeersWebDispay.AssemblyDirectory);
                Logging.LogTrace("CEF loaded");

                var cefArgs = new CefMainArgs(new string[] { "mute-audio" });

                var cefApp = new OffscreenCefApp();

                // This is where the code path diverges for child processes.
                if (CefRuntime.ExecuteProcess(cefArgs, cefApp, IntPtr.Zero) != -1)
                    Logging.LogError("Could not start the CEF secondary process.");

                Logging.LogTrace("Executed CEF process");

                var cefSettings = new CefSettings
                {
                    BrowserSubprocessPath = Path.Combine(StationeersWebDispay.AssemblyDirectory, "CefGlueBrowserProcess/Xilium.CefGlue.BrowserProcess.exe"),
                    MultiThreadedMessageLoop = false,
                    LogSeverity = CefLogSeverity.Verbose,
                    LogFile = "cef.log",
                    WindowlessRenderingEnabled = true,
                    NoSandbox = true,
                };

                CefRuntime.Initialize(cefArgs, cefSettings, cefApp, IntPtr.Zero);
                Logging.LogTrace("CEF runtime initialized");

                var pump = new GameObject("CefMessagePump");
                pump.transform.parent = StationeersWebDispay.Instance.gameObject.transform;
                pump.AddComponent<CefMessagePump>();
                Logging.LogTrace("CEF Message pump started");
            }
            catch (Exception ex)
            {
                Logging.LogError($"Failed to initialize CEF: {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static OffscreenCefClient CreateClient(string url, Size windowSize)
        {
            if (!initialized)
            {
                throw new Exception("CefHost is not initialized.");
            }

            var cefWindowInfo = CefWindowInfo.Create();
            cefWindowInfo.SetAsWindowless(IntPtr.Zero, false);

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

            var cefClient = new OffscreenCefClient(windowSize);
            CefBrowserHost.CreateBrowser(cefWindowInfo, cefClient, cefBrowserSettings, url);

            return cefClient;
        }

        private class CefMessagePump : MonoBehaviour
        {
            void Update()
            {
                try
                {
                    CefRuntime.DoMessageLoopWork();
                }
                catch (Exception ex)
                {
                    Logging.LogError($"Failed to DoMessageLoopWork: {ex.GetType().FullName} {ex.Message} {ex.StackTrace}");
                }
            }
        }
    }
}