using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using System.Linq;
using System.Collections.Generic;
using TPDespair.ZetAspects;
using HarmonyLib;
using Augmentum.Modules.ItemTiers.HighlanderTier;
using Augmentum.Modules.Utils;
using RoR2.ContentManagement;

namespace ZetAspectHighlander
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(ZetAspectsPlugin.ModGuid)]
    [BepInDependency(Augmentum.Augmentum.ModGuid)]
    public class Main : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "prodzpod";
        public const string PluginName = "ZetAspectHighlander";
        public const string PluginVersion = "1.0.1";
        public static ManualLogSource Log;
        public static PluginInfo pluginInfo;
        public static ConfigFile Config;
        public static ConfigEntry<bool> SpawnNaturally;
        public static Harmony Harmony;
        public static List<ItemIndex> Affixes = [];

        public void Awake()
        {
            pluginInfo = Info;
            Log = Logger;
            Config = new ConfigFile(System.IO.Path.Combine(Paths.ConfigPath, PluginGUID + ".cfg"), true);
            SpawnNaturally = Config.Bind("General", "Aspects drop naturally", false, "set this to make it like any other highlander");
            Harmony = new(PluginGUID);
            Harmony.PatchAll(typeof(ya));
            Harmony.PatchAll(typeof(yi));
            if (!SpawnNaturally.Value) Harmony.PatchAll(typeof(ye));
            RoR2Application.onLoad += () =>
            {
                foreach (var def in ContentManager._itemDefs) if (def.name.StartsWith("ZetAspect") && (def.tier == ItemTier.Boss || def.tier == ItemTier.Tier3))
                {
                    Log.LogInfo("Fixing " + def.name);
                    def._itemTierDef = Highlander.instance.itemTierDef;
                    Affixes.Add(def.itemIndex);
                }
            };
        }
    }

    [HarmonyPatch(typeof(ZetAspectsContent), nameof(ZetAspectsContent.FinalizeAsync))]
    public class ya
    {
        public static void Prefix(ZetAspectsContent __instance)
        {
            foreach (var i in ZetAspectsContent.itemDefs) if (i._itemTierDef == Catalog.BossItemTier || i._itemTierDef == Catalog.RedItemTier)
                i._itemTierDef = Highlander.instance.itemTierDef;
        }
    }

    [HarmonyPatch(typeof(ItemHelpers), nameof(ItemHelpers.PickupDefsWithTier))]
    public class ye
    {
        public static void Postfix(ItemTierDef itemTierDef, ref List<PickupDef> __result)
        {
            if (itemTierDef == Highlander.instance.itemTierDef)
                __result = __result.Where(x => !Main.Affixes.Contains(x.itemIndex)).ToList();
        }
    }

    [HarmonyPatch(typeof(Catalog), nameof(Catalog.SetItemState))]
    public class yi
    {
        public static void Postfix(ItemDef itemDef, bool shown)
        {
            if (itemDef._itemTierDef == Catalog.BossItemTier || itemDef._itemTierDef == Catalog.RedItemTier)
                itemDef._itemTierDef = Highlander.instance.itemTierDef;
        }
    }
}
