using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using System.Linq;
using UnityEngine;
using static Minimap;
using static MeleeWeaponTrail;
using static ZoneSystem;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections;

namespace LocateEverything
{
    [BepInPlugin("ValheimLocateMapMod", "Locate Everything", "1.0.2")]
    [BepInProcess("valheim.exe")]
    public class ValheimMapMod : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony("ValheimLocateMapMod");
        public static List<ZoneLocation> test;
        public static ZoneSystem zs;
        public static Minimap mm;
        public static Dictionary<Vector2i, LocationInstance> dict;
        void Awake()
        {
            harmony.PatchAll();
        }


    [HarmonyPatch(typeof(ZoneSystem), MethodType.Constructor)]
    private class MyZone
        {
            private static void Postfix(ZoneSystem __instance)
            {
                //if (test.Count < 1) 
                //{ 
                test = __instance.m_locations;
                zs = __instance;
                dict = __instance.m_locationInstances;
                //}
                //foreach (ZoneLocation loc in __instance.m_locations)
                //{
                //    test.Add(loc);
                //}


            }
        }

        [HarmonyPatch(typeof(Minimap), MethodType.Constructor)]
        private class myMap
        {
            private static void Postfix(Minimap __instance)
            {
                mm = __instance;

            }
        }

        [HarmonyPatch(typeof(Terminal), "InputText")]
        public class Console_input
        {
            private const Minimap.PinType pinType0 = Minimap.PinType.None;

            [HarmonyPostfix]
            public static void Postfix(Terminal __instance)
            {


                String inputText = __instance.m_input.text.ToUpper();
                if (inputText.Contains("HELP"))
                {

                    Console.instance.Print("LocateEverything - Locate Closest *location* ");
                    Console.instance.Print("LocateEverything - Locate Closest *location* ");
                    Console.instance.Print("LocateEverything - Locate All *location*");
                    Console.instance.Print("LocateEverything - Remove Everything Pins");
                    Console.instance.Print("LocateEverything - LOCATE EVERYTHING");
                }
                else if (inputText.Equals("LIST LOCATIONS"))
                {
                    LocationList zone = Game.instance.GetComponent<LocationList>();
                    //Console.print(test.Count);

                    int prefabCount = 0;
                    String consoleText = "";
                    foreach (ZoneLocation zloc in test)
                    {
                        prefabCount++;
                        consoleText = consoleText + ", " + zloc.m_prefabName;
                        if (prefabCount > 6)
                        {
                            prefabCount = 0;
                            Console.instance.Print(consoleText.Trim(','));
                            consoleText = "";

                        }
                    }
                    return;
                }
                else if (inputText.Equals("LOCATE EVERYTHING"))
                {

                    if(dict.Count() == 0)
                    {
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Please close and re-open this World to initialize Locations");
                        Console.instance.Print("Please close and re-open this World to initialize Locations");
                        return;
                    }

                    Console.instance.Print("Locating all of these: " + dict.Count());
                    List<Vector3> ExistingPinPositions = new List<Vector3>();
                            
                    List<Minimap.PinData> MiniMapPinData = (List<Minimap.PinData>)Traverse.Create((object)Minimap.instance).Field("m_pins").GetValue(); //No idea how Traverse works but it does the job
                    foreach (PinData pin in MiniMapPinData)
                    {
                        ExistingPinPositions.Add(pin.m_pos);
                    }


                    foreach (Vector2i LI in dict.Keys)
                    {
                        Vector3 LocationPos = dict[LI].m_position;
                        //Console.instance.Print(LI.ToString() + ", " + dict[LI] + ", " + LocationPos + ", " + dict[LI].m_location.m_prefabName);

                        if (!ExistingPinPositions.Contains(LocationPos))
                        {
                            mm.AddPin(LocationPos, PinType.None, dict[LI].m_location.m_prefabName, true, false);
                            Console.instance.Print(" Pin Added at, " + LocationPos);
                        }
                        else
                        {
                            //Console.instance.Print(" Pin Already Exists");
                        }

                    }

                    return;
                }
                else if (inputText.Equals("REMOVE EVERYTHING PINS"))
                {
                    Console.instance.Print("Start Removing");
                    List<Minimap.PinData> MiniMapPinData = (List<Minimap.PinData>)Traverse.Create((object)Minimap.instance).Field("m_pins").GetValue(); //No idea how Traverse works but it does the job
                    List<PinData> RemovablePins = new List<PinData>();
                    foreach (PinData pin in MiniMapPinData)
                    {
                        if (pin.m_type == PinType.None)
                        {
                            RemovablePins.Add(pin);
                        }
                    }
                    foreach (PinData pin in RemovablePins)
                    {
                        mm.RemovePin(pin);
                    }
                    Console.instance.Print("Finished Removing");
                }

               // also add for th ehelp command

                    //either Locate ALL Mines 
                    // or    Locate Closest Mines
                    String[] SplitText = inputText.Split(' ');
                String PrefabName = SplitText[SplitText.Length - 1];
                List<String> PrefabNamesListUpper = new List<String>();
                List<String> OriginalPrefabNamesList = new List<String>();
                foreach (ZoneLocation zloc in test)
                {
                    PrefabNamesListUpper.Add(zloc.m_prefabName.ToUpper());
                    OriginalPrefabNamesList.Add(zloc.m_prefabName);
                }

                bool all = inputText.StartsWith("LOCATE ALL");
                bool closest = inputText.StartsWith("LOCATE CLOSEST");
                if ((closest || all) & PrefabNamesListUpper.Contains(PrefabName))
                {

                    Console.instance.Print("Locating...");
                    String OriginalPrefabName = OriginalPrefabNamesList[PrefabNamesListUpper.IndexOf(PrefabName)];
                    //String[] SplitText = inputText.Split(' ');
                    //String PrefabName = SplitText[1];


                    if (closest)
                    {
                        Game.instance.DiscoverClosestLocation(OriginalPrefabName, Player.m_localPlayer.transform.position, OriginalPrefabName, 8);
                        Console.instance.Print(" Finished Discovering Closest.");
                    }
                    else if(all)
                    {
                        if (dict.Count() == 0)
                        {
                            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Please close and re-open this World to initialize Locations");
                            Console.instance.Print("Please close and re-open this World to initialize Locations");
                            return;
                        }
                        List<Vector3> ExistingPinPositions = new List<Vector3>();
                        List<Minimap.PinData> MiniMapPinData = (List<Minimap.PinData>)Traverse.Create((object)Minimap.instance).Field("m_pins").GetValue();

                        foreach (PinData pin in MiniMapPinData)
                        {
                            ExistingPinPositions.Add(pin.m_pos);
                        }

                        foreach (Vector2i LI in dict.Keys)
                        {
                            Vector3 LocationPos = dict[LI].m_position;
                            //Console.instance.Print(LI.ToString() + ", " + dict[LI] + ", " + LocationPos + ", " + dict[LI].m_location.m_prefabName);
                            if (!ExistingPinPositions.Contains(LocationPos) & dict[LI].m_location.m_prefabName.Equals(OriginalPrefabName))
                            {
                                mm.AddPin(LocationPos, PinType.None, dict[LI].m_location.m_prefabName, true, false);
                                //Console.instance.Print(" Pin Added");
                            }
                            else
                            {
                                //Console.instance.Print(" Pin Already Exists");
                            }

                        }
                        Console.instance.Print("Finished Locating All.");
                    }

                }


            }
        }

    }
}
