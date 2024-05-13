
using System.IO;
using BepInEx;
using HarmonyLib;
using StationeersWebDisplay.Cef;

namespace StationeersWebDisplay
{
    [BepInPlugin("dev.sunsetfi.stationeers.webdisplay", "Chromium Web Renderer API for Stationeers", "1.0.0.0")]
    public class StationeersWebDisplayPlugin : BaseUnityPlugin
    {
        public static StationeersWebDisplayPlugin Instance;

        public static string AssemblyDirectory
        {
            get
            {
                var assemblyLocation = typeof(StationeersWebDisplayPlugin).Assembly.Location;
                var assemblyDir = Path.GetDirectoryName(assemblyLocation);
                return assemblyDir;
            }
        }

        void Awake()
        {
            // Test code for diagnosing assembly load failures.
            // AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
            // {
            //     Logging.LogTrace($"Assembly resolve for {e.Name} from {e.RequestingAssembly.FullName}");
            //     throw new Exception("Last ditch assembly resolve failed.");
            // };

            StationeersWebDisplayPlugin.Instance = this;
            CefHost.Initialize();
        }
    }
}