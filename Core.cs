using MelonLoader;
using HarmonyLib;
using Il2CppFishNet.Object;
using Il2CppScheduleOne.Growing;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using SceneManager = UnityEngine.SceneManagement.SceneManager;
using Scene = UnityEngine.SceneManagement.Scene;
using Il2CppScheduleOne;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Quests;
using Il2CppScheduleOne.Persistence.Datas;

[assembly: MelonInfo(typeof(QualityPlants.QualityPlants), QualityPlants.BuildInfo.Name, QualityPlants.BuildInfo.Version, QualityPlants.BuildInfo.Author, QualityPlants.BuildInfo.DownloadLink)]
[assembly: MelonColor()]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace QualityPlants
{
    public static class BuildInfo
    {
        public const string Name = "Quality Plants";
        public const string Description = "Ugh, who knew \"gardening\" was so hard?";
        public const string Author = "SoulDaSythe";
        public const string Company = null;
        public const string Version = "1.0";
        public const string DownloadLink = null;

    }
    public class QualityPlants : MelonMod
    {
        internal static bool loaded = false;
        public override void OnInitializeMelon()
        {


            Melon<QualityPlants>.Logger.Msg("Initialized!");

            SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)OnSceneLoaded);
            base.OnInitializeMelon();
        }
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {

            if (scene.name.Equals("Main")) MelonCoroutines.Start(Load()); else loaded = false;
            //Melon<QualityPlants>.Logger.Msg($"Scene {scene.name} loaded!");
            //Melon<QualityPlants>.Logger.Msg($"loaded = " + loaded.ToString());
        }

        private static IEnumerator Load()
        {
            for (int i = 0; i < 40; i++)
            {
                yield return new WaitForEndOfFrame();  // Wait until the end of the frame
            }
            Melon<QualityPlants>.Logger.Msg($"Finally! I can start!");
            loaded = true;
            Il2CppScheduleOne.Levelling.Unlockable unlockable;

        }
        
        [HarmonyPatch(typeof(Plant), "Initialize")]
        public static class PlantQualityPatch
        {

            public static void Postfix(Plant __instance, NetworkObject pot, float growthProgress, float yieldLevel, float qualityLevel)
            {
                Array plants = Array.Empty<Plant>();

                if (__instance.Pot != null && loaded)
                {
                    //Melon<QualityMod>.Logger.Msg("Pot was not Null");
                    string potName = __instance.Pot.Name.ToString();
                    if (potName.Equals("Grow Tent")) __instance.QualityLevel = 0.1f;
                    if (potName.Equals("Plastic Pot")) __instance.QualityLevel = 0.26f;
                    if (potName.Equals("Moisture-Preserving Pot")) __instance.QualityLevel = 0.5f;
                    if (potName.Equals("Air Pot")) __instance.QualityLevel = 0.8f;
                    //Melon<QualityMod>.Logger.Msg("Quality level is " + __instance.QualityLevel);
                }
                //Melon<QualityMod>.Logger.Msg("Plant was initialized");
            }

        }
        [HarmonyPatch(typeof(Registry))]
        internal class ConfigLoad
        {
            [HarmonyPatch("Awake")]
            [HarmonyPostfix]
            private static void PostfixPlayerLoaded()
            {
                StorableItemDefinition dryingDef = null;
                StorableItemDefinition plasticPotDef = null;

                foreach (var item in Singleton<Registry>.instance.ItemRegistry)
                {
                    var definition = item.Definition.TryCast<StorableItemDefinition>();
                    if ((Il2CppSystem.Object)definition == null)
                        continue;

                    if (definition.ID == "dryingrack")
                    {
                        dryingDef = definition;
                        MelonLogger.Msg($"Required Rank for Drying Rack is {dryingDef.RequiredRank}");
                    }
                    else if (definition.ID == "plasticpot")
                    {
                        plasticPotDef = definition;
                        MelonLogger.Msg($"Required Rank for Plastic Pot is {plasticPotDef.RequiredRank}");
                    }
                }

                if ((Il2CppSystem.Object)dryingDef != null && (Il2CppSystem.Object)plasticPotDef != null)
                {
                    dryingDef.RequiredRank = plasticPotDef.RequiredRank;
                    MelonLogger.Msg("Changed Drying Rack Required Rank to match Plastic Pot's.");
                }
                //i know this is cursed but i really dont want to figure out how to construct a RequiredRank type.
                //plus i want the drying rack to be unlocked at the same time as the plastic pot
                //this ensures no matter what rank the plastic pot requires the drying rack will unlock too.
                //lets just call this "mod compatibility" and leave it at that
            }
        }
    }
}