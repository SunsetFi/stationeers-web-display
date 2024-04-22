
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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

        private readonly Size browserSize = new Size(1024, 768);
        private readonly Texture2D browserTexture = new Texture2D(1024, 768, TextureFormat.BGRA32, false);
        private OffscreenCefClient browserClient;
        private List<PictureFrame> hyjackedPictureFrames = new();

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
            PictureFrameAwakeWatcher.PictureFrameAwake += (_, e) =>
            {
                Logging.LogTrace("Found picture frame to hyjack");
                this.hyjackedPictureFrames.Add(e.PictureFrame);
            };
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
            // "https://cdn.svgator.com/images/2024/02/animated-geometric-background.svg"
            var url = "https://www.youtube.com/embed/pE_RXUWw9ys?autoplay=1&mute=1";
            this.browserClient = CefHost.CreateClient(url, this.browserSize);
        }

        void Update()
        {
            try
            {
                this.browserClient.CopyToTexture(this.browserTexture);
                foreach (var frame in this.hyjackedPictureFrames)
                {
                    var material = Reflection.GetPrivateField<Material>(frame, "PictureImage");
                    material.mainTexture = this.browserTexture;
                }
            }
            catch(Exception ex)
            {
                Logging.LogError($"Failed to set picture frame textures: {ex.GetType().FullName} {ex.Message} {ex.StackTrace}");
            }
        }
    }
}