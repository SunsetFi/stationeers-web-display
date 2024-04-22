using CefSharp.OffScreen;
using CefSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace StationeersWebDisplay
{
    static class CefHost
    {
        public static void Initialize()
        {
            Logging.LogTrace($"Initializing CEF ");
            
            Logging.LogTrace($"Starting CEF demo process");
            Dispatcher.RunOnMainThread(async () =>
            {
                try
                {
                    var settings = new CefSettings()
                    {
                        //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                        // CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
                    };

                    //Perform dependency check to make sure all relevant resources are in our output directory.
                    var success = await Cef.InitializeAsync(settings, performDependencyCheck: true, browserProcessHandler: null);

                    if (!success)
                    {
                        throw new Exception("Unable to initialize CEF, check the log file.");
                    }

                    // Create the CefSharp.OffScreen.ChromiumWebBrowser instance
                    using (var browser = new ChromiumWebBrowser("https://www.google.com"))
                    {
                        var initialLoadResponse = await browser.WaitForInitialLoadAsync();

                        if (!initialLoadResponse.Success)
                        {
                            throw new Exception(string.Format("Page load failed with ErrorCode:{0}, HttpStatusCode:{1}", initialLoadResponse.ErrorCode, initialLoadResponse.HttpStatusCode));
                        }

                        _ = await browser.EvaluateScriptAsync("document.querySelector('[name=q]').value = 'CefSharp Was Here!'");

                        //Give the browser a little time to render
                        await Task.Delay(500);
                        // Wait for the screenshot to be taken.
                        var bitmapAsByteArray = await browser.CaptureScreenshotAsync();

                        // File path to save our screenshot e.g. C:\Users\{username}\Desktop\CefSharp screenshot.png
                        var screenshotPath = Path.Combine(StationeersWebDisplayPlugin.AssemblyDirectory, "CefSharp screenshot.png");

                        Logging.LogTrace("Screenshot ready. Saving to {0}", screenshotPath);

                        File.WriteAllBytes(screenshotPath, bitmapAsByteArray);

                        Logging.LogTrace("Screenshot saved. Launching your default image viewer...");

                        Logging.LogTrace("CEF initialized");
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogError($"Failed to initialize CEF: {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}");
                }
            });
        }
    }
}