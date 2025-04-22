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
using Il2CppScheduleOne.Levelling;
using Il2CppScheduleOne.Interaction;
using System.Reflection;

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
            RankPatcher.GenerateConfig();
            MelonPreferences.Save();
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
            Melon<QualityPlants>.Logger.Msg("[QualityPlants] Coroutine started. Waiting for scene to initialize...");

            for (int i = 0; i < 40; i++)
                yield return new WaitForEndOfFrame();

            Melon<QualityPlants>.Logger.Msg("[QualityPlants] Running ApplyRanksWithFallback after delay...");
            RankPatcher.ApplyRanksWithFallback();

            loaded = true;
            Melon<QualityPlants>.Logger.Msg("[QualityPlants] Load complete!");
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


        public static class RankPatcher
        {
            private static MelonPreferences_Category category;

            public static readonly Dictionary<string, (MelonPreferences_Entry<string> Rank, MelonPreferences_Entry<int> Tier)> ConfigEntries
                = new Dictionary<string, (MelonPreferences_Entry<string>, MelonPreferences_Entry<int>)>();


            // Hardcoded fallback (default) ranks
            private static readonly Dictionary<string, FullRank> DefaultRanks = new Dictionary<string, FullRank>
    {
        { "moisturepreservingpot",  new FullRank(ERank.Hoodlum, 5) },
        { "ledgrowlight",           new FullRank(ERank.Hoodlum, 3) },
        { "plasticpot",             new FullRank(ERank.Street_Rat, 5) },
        { "halogengrowlight",       new FullRank(ERank.Street_Rat, 5) },
        { "airpot",                 new FullRank(ERank.Hustler, 5) },
        { "fullspectrumgrowlight",  new FullRank(ERank.Hoodlum, 5) },
        { "dryingrack",  new FullRank(ERank.Street_Rat, 5) }
    };
            public static void GenerateConfig()
            {
                category = MelonPreferences.CreateCategory("BuildableRankOverrides", "Buildable Rank Overrides");

                foreach (var kvp in DefaultRanks)
                {
                    string id = kvp.Key;
                    FullRank fallback = kvp.Value;

                    var rankEntry = category.CreateEntry($"{id}_Rank", fallback.Rank.ToString(), $"Rank for {id}");
                    var tierEntry = category.CreateEntry($"{id}_Tier", fallback.Tier, $"Tier for {id}");

                    ConfigEntries[id] = (rankEntry, tierEntry);
                }

                Melon<QualityPlants>.Logger.Msg("[RankConfig] Config entries generated.");
            }
            public static Dictionary<string, (string Rank, int Tier)> LoadRankOverrides()
            {
                var result = new Dictionary<string, (string, int)>();
                foreach (var kvp in ConfigEntries)
                {
                    string id = kvp.Key;
                    string rankStr = kvp.Value.Rank.Value;
                    int tier = kvp.Value.Tier.Value;
                    result[id] = (rankStr, tier);
                }
                return result;
            }



            public static void ApplyRanksWithFallback()
            {
                Melon<QualityPlants>.Logger.Msg("[RankPatcher] Starting ApplyRanksWithFallback via Registry.ItemRegistry...");

                var configOverrides = LoadRankOverrides();
                Melon<QualityPlants>.Logger.Msg($"[RankPatcher] Loaded {configOverrides.Count} config overrides.");
                Melon<QualityPlants>.Logger.Msg($"[RankPatcher] DefaultRanks has {DefaultRanks.Count} entries.");

                var registry = Singleton<Registry>.instance;
                if (registry == null || registry.ItemRegistry == null)
                {
                    Melon<QualityPlants>.Logger.Warning("[RankPatcher] Registry or ItemRegistry is null!");
                    return;
                }

                int patched = 0;

                foreach (var item in registry.ItemRegistry)
                {
                    if (item?.Definition == null) continue;

                    var def = item.Definition.TryCast<BuildableItemDefinition>();
                    if ((Il2CppSystem.Object)def == null || def.ID == null) continue;

                    string id = def.ID.ToString().ToLowerInvariant();

                    if (!DefaultRanks.ContainsKey(id)) continue;

                    FullRank finalRank;

                    if (configOverrides.TryGetValue(id, out var overrideData) &&
                        TryParseRank(overrideData.Rank, out var parsedRank))
                    {
                        finalRank = new FullRank(parsedRank, overrideData.Tier);
                        Melon<QualityPlants>.Logger.Msg($"[RankPatcher] Overridden rank for {id}: {parsedRank} (Tier {overrideData.Tier})");
                    }
                    else
                    {
                        finalRank = DefaultRanks[id];
                        Melon<QualityPlants>.Logger.Msg($"[RankPatcher] Fallback rank for {id}: {finalRank.Rank} (Tier {finalRank.Tier})");
                    }

                    def.RequiredRank = finalRank;
                    patched++;
                }

                Melon<QualityPlants>.Logger.Msg($"[RankPatcher] Finished. Patched {patched} BuildableItemDefinitions.");
            }




            private static bool TryParseRank(string input, out ERank result)
            {
                return Enum.TryParse(input, ignoreCase: true, out result);
            }
        }




    }
}