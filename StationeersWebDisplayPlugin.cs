
using System;
using System.IO;
using System.Linq;
using Assets.Scripts.Objects;
using BepInEx;
using HarmonyLib;
using StationeersWebDisplay.Cef;
using StationeersWebDisplay.Patches;
using UnityEngine;

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
            // Test code for diagnosing assembly load failures.
            // AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
            // {
            //     Logging.LogTrace($"Assembly resolve for {e.Name} from {e.RequestingAssembly.FullName}");
            //     throw new Exception("Last ditch assembly resolve failed.");
            // };

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

            foreach (var frame in Thing.AllThings.OfType<PictureFrame>())
            {
                HyjackFrame(frame);
            }

            // For testing.  We never unsubscribe from this so starting multiple games will break stuff.
            PictureFrameAwakeWatcher.PictureFrameAwake += (_, e) => HyjackFrame(e.PictureFrame);
        }

        void HyjackFrame(PictureFrame frame)
        {
            try
            {
                var material = Reflection.GetPrivateField<Material>(frame, "PictureImage");
                var display = frame.GetOrAddComponent<WebDisplayBehavior>();
                // Hack to make the picture frames work as a screen due to their non-rendering bezel.
                // When we have our own game object, this won't be necessary as we can use our own properly sized collider.
                display.Bezel = new(0.19f, 0.25f);
                display.RenderMaterial = material;

                display.Url = "https://codepen.io/SunsetFi/pen/oNOJEje"; // "https://www.youtube.com/embed/pE_RXUWw9ys?autoplay=1&mute=1";
            }
            catch(Exception ex)
            {
                Logging.LogError($"Error hyjacking frame: {ex.GetType().FullName} {ex.Message} {ex.StackTrace}");
            }
        }
    }
}