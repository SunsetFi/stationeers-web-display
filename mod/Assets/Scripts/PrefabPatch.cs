using System;
using System.Collections.ObjectModel;
using Assets.Scripts.Objects;
using HarmonyLib;
using StationeersMods.Interface;
using UnityEngine;
using UnityEngine.Rendering;

namespace StationeersWebDisplay
{
    [HarmonyPatch]
    public class PrefabPatch
    {
        public static ReadOnlyCollection<GameObject> prefabs { get; set; }


        [HarmonyPatch(typeof(Prefab), "LoadAll")]
        public static void Prefix()
        {
            try
            {
                Debug.Log("Prefab Patch started");
                Material color_white = StationeersModsUtility.GetMaterial("color_white");
                foreach (var gameObject in prefabs)
                {
                    Thing thing = gameObject.GetComponent<Thing>();


                    if (thing is StructureWebDisplay)
                    {
                        Debug.Log("patch WebDisplay: " + thing.name);
                        var webDisplay = gameObject.GetComponent<StructureWebDisplay>();

                        webDisplay.BuildStates[0].Tool.ToolExit = StationeersModsUtility.FindTool(StationeersTool.DRILL);
                        webDisplay.Blueprint.GetComponent<MeshRenderer>().materials = StationeersModsUtility.GetBlueprintMaterials(2);
                    }

                    if (thing is MultiConstructor)
                    {
                        Debug.Log("patch WebDisplayKit");
                        thing.Blueprint.GetComponent<MeshRenderer>().materials = StationeersModsUtility.GetBlueprintMaterials(2);
                    }

                    if (thing != null)
                    {
                        Debug.Log(gameObject.name + " added to WorldManager");
                        WorldManager.Instance.SourcePrefabs.Add(thing);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                Debug.LogException(ex);
            }
        }
    }
}