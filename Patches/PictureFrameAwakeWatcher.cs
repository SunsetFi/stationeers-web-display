
using Assets.Scripts.Objects;
using HarmonyLib;
using System;

namespace StationeersWebDisplay.Patches
{

    [HarmonyPatch(typeof(PictureFrame), "Awake")]
    sealed class PictureFrameAwakeWatcher
    {
        public static event EventHandler<PictureFrameAwakeEventArgs> PictureFrameAwake;
        static void Postfix(PictureFrame __instance)
        {
            PictureFrameAwake?.Invoke(null, new PictureFrameAwakeEventArgs(__instance));
        }

        public class PictureFrameAwakeEventArgs
        {
            public PictureFrame PictureFrame { get; private set; }

            public PictureFrameAwakeEventArgs(PictureFrame pictureFrame)
            {
                PictureFrame = pictureFrame;
            }
        }
    }
}