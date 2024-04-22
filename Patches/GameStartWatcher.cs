
using Assets.Scripts;
using HarmonyLib;
using System;

namespace StationeersWebDisplay.Patches
{

    [HarmonyPatch(typeof(GameManager), "StartGame")]
    sealed class GameStartWatcher
    {
        public static event EventHandler<EventArgs> GameStarted;
        static void Postfix()
        {
            GameStartWatcher.GameStarted?.Invoke(null, EventArgs.Empty);
        }
    }
}