
using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using StationeersWebDisplay.Cef;

namespace StationeersWebDisplay
{
    [BepInPlugin("dev.sunsetfi.stationeers.webdisplay", "Chromium Web Renderer API for Stationeers", "1.0.0.0")]
    public class StationeersWebDisplayPlugin : BaseUnityPlugin
    {
        public static StationeersWebDisplayPlugin Instance { get; private set; }

        private static HashSet<Uri> AllowedUrlsList { get; } = new();

        public static string AssemblyDirectory
        {
            get
            {
                var assemblyLocation = typeof(StationeersWebDisplayPlugin).Assembly.Location;
                var assemblyDir = Path.GetDirectoryName(assemblyLocation);
                return assemblyDir;
            }
        }

        public static void AddAllowedUri(Uri url)
        {
            AllowedUrlsList.Add(url);
        }



        void Awake()
        {
            Instance = this;

            // Test code for diagnosing assembly load failures.
            // AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
            // {
            //     Logging.LogTrace($"Assembly resolve for {e.Name} from {e.RequestingAssembly.FullName}");
            //     throw new Exception("Last ditch assembly resolve failed.");
            // };

            StationeersCefHost.Initialize();
        }
    }
}