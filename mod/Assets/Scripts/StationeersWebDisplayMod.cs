using System;
using HarmonyLib;
using StationeersMods.Interface;
using StationeersWebDisplay;
using StationeersWebDisplay.Cef;

public class StationeersWebDisplayMod : ModBehaviour
{
    public override void OnLoaded(ContentHandler contentHandler)
    {
        UnityEngine.Debug.Log("StationeersWebDisplay Loading...");
        CefHost.Initialize();
        Harmony harmony = new Harmony("dev.sunsetfi.stationeers.webdisplay");
        PrefabPatch.prefabs = contentHandler.prefabs;
        harmony.PatchAll();
        UnityEngine.Debug.Log("StationeersWebDisplay Loaded with " + contentHandler.prefabs.Count + " prefab(s)");
    }
}