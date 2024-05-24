using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using UnityEngine;
using Xilium.CefGlue;

namespace StationeersWebDisplay.Cef
{
    public static class StationeersCefHost
    {
        private static bool initialized = false;
        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;

            try
            {
                Logging.LogTrace("Loading CEF assemblies");
                CefRuntime.Load(StationeersWebDisplayPlugin.AssemblyDirectory);

                var cefArgs = new CefMainArgs(new string[] { "mute-audio" });

                var cefApp = new StationeersCefApp();

                // This is where the code path diverges for child processes.
                if (CefRuntime.ExecuteProcess(cefArgs, cefApp, IntPtr.Zero) != -1)
                {
                    Logging.LogError("Could not start the CEF worker process.");
                    return;
                }

                var cefSettings = new CefSettings
                {
                    BrowserSubprocessPath = Path.Combine(StationeersWebDisplayPlugin.AssemblyDirectory, "CefGlueBrowserProcess/Xilium.CefGlue.BrowserProcess.exe"),
                    MultiThreadedMessageLoop = false,
                    LogSeverity = CefLogSeverity.Verbose,
                    LogFile = "cef.log",
                    WindowlessRenderingEnabled = true,
                    NoSandbox = true,
                };

                Logging.LogTrace("Initializing CEF runtime");
                CefRuntime.Initialize(cefArgs, cefSettings, cefApp, IntPtr.Zero);

                Logging.LogTrace("Starting CEF message pump");
                var pump = new GameObject("CefMessagePump");
                pump.transform.parent = StationeersWebDisplayPlugin.Instance.gameObject.transform;
                pump.AddComponent<CefMessagePump>();
            }
            catch (Exception ex)
            {
                Logging.LogError($"Failed to initialize CEF: {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static StationeersCefClient CreateClient(string url, Size windowSize, ICollection<Uri> allowedUris)
        {
            if (!initialized)
            {
                throw new Exception("StationeersCefHost is not initialized.");
            }

            var cefWindowInfo = CefWindowInfo.Create();
            cefWindowInfo.SetAsWindowless(IntPtr.Zero, false);

            var cefBrowserSettings = new CefBrowserSettings()
            {
                BackgroundColor = new CefColor(0, 0, 0, 255),
                JavaScript = CefState.Enabled,
                JavaScriptAccessClipboard = CefState.Disabled,
                JavaScriptCloseWindows = CefState.Disabled,
                JavaScriptDomPaste = CefState.Disabled,
                Databases = CefState.Disabled,
                LocalStorage = CefState.Disabled
            };

            var cefClient = new StationeersCefClient(windowSize, allowedUris);
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
                    Logging.LogError($"CEF failed to DoMessageLoopWork: {ex.GetType().FullName} {ex.Message} {ex.StackTrace}");
                }
            }
        }
    }
}