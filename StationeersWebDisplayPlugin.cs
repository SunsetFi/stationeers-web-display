
using System;
using System.IO;
using BepInEx;
using HarmonyLib;
using StationeersWebDisplay.Patches;

namespace StationeersWebDisplay
{
    [BepInPlugin("net.sunsetfidev.stationeers.StationeersWebDisplay", "Web API for Stationeers", "2.1.1.0")]
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
            AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
            {
                Logging.LogTrace($"Assembly resolve for {e.Name} from {e.RequestingAssembly.FullName}");
                throw new Exception("Last ditch assembly resolve failed.");
            };

            StationeersWebDisplayPlugin.Instance = this;
            ApplyPatches();

            GameStartWatcher.GameStarted += (_, _) => this.StartDisplays();
        }

        private void ApplyPatches()
        {
            var harmony = new Harmony("net.robophreddev.stationeers.StationeersWebApi");
            harmony.PatchAll();
            Logging.Log("Patch succeeded");
        }

        void StartDisplays()
        {
            Logging.LogTrace("Starting displays");
            Dispatcher.Initialize();
            CefHost.Initialize();
        }
    }
}