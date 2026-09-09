using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MarkAsJunk
{
    /// <summary>Debug verbosity levels. Stored as int in settings so enum
    /// reordering can never corrupt saves.</summary>
    public enum DebugLogLevel
    {
        Off = 0,
        Basic = 1,
        Verbose = 2
    }

    public class MarkAsJunkSettings : ModSettings
    {
        public bool enabled = true;

        public int debugLevel = (int)DebugLogLevel.Off;

        /// <summary>When true (default), junk-marked items keep flowing into
        /// normal storage while no junk dump exists on their map. When false,
        /// junk items are rejected by normal storage unconditionally - they
        /// then simply stay where they are until a dump is marked.</summary>
        public bool routeOnlyWithJunkStorage = true;

        /// <summary>Adds the "smelt/destroy junk" and "destroy junk" bills
        /// to electric smelters (default true).</summary>
        public bool smelterRecipes = true;

        /// <summary>Auto-mark apparel pawns take off because it no longer
        /// passes their apparel policy (e.g. tattered). Apparel swapped out
        /// for a better piece still passes the policy and is never marked
        /// (default true).</summary>
        public bool autoMarkDroppedGear = true;

        /// <summary>Draws a small icon over junk-marked items in the world,
        /// mirroring vanilla's forbidden overlay (default true).</summary>
        public bool showJunkIcon = true;

        /// <summary>When a junk-marked corpse is destroyed (smelting/destroy
        /// bills, decay), the worn apparel and equipped weapon it still holds
        /// are dropped as junk instead of being silently destroyed with the
        /// corpse. Inventory keeps vanilla behavior (default true).</summary>
        public bool cascadeJunkFromCorpses = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enabled, "enabled", true);
            Scribe_Values.Look(ref debugLevel, "debugLevel", (int)DebugLogLevel.Off);
            Scribe_Values.Look(ref routeOnlyWithJunkStorage, "routeOnlyWithJunkStorage", true);
            Scribe_Values.Look(ref smelterRecipes, "smelterRecipes", true);
            Scribe_Values.Look(ref autoMarkDroppedGear, "autoMarkDroppedGear", true);
            Scribe_Values.Look(ref showJunkIcon, "showJunkIcon", true);
            Scribe_Values.Look(ref cascadeJunkFromCorpses, "cascadeJunkFromCorpses", true);
        }
    }

    public class MarkAsJunkMod : Mod
    {
        public const string PackageId = "Riketta.MarkAsJunk";

        public static MarkAsJunkSettings Settings;

        /// <summary>Master switch, read by every patch on each call. Null-safe:
        /// without settings the patches stay active rather than silently
        /// disabling the mod.</summary>
        public static bool Active => Settings?.enabled ?? true;

        /// <summary>Convenience for the "fallback routing" setting.</summary>
        public static bool RejectJunkInNormalStorageOnlyWithDump =>
            Settings?.routeOnlyWithJunkStorage ?? true;

        /// <summary>Convenience for the "auto-mark discarded gear" setting.</summary>
        public static bool AutoMarkDroppedGear => Settings?.autoMarkDroppedGear ?? true;

        /// <summary>Convenience for the "junk overlay icon" setting.</summary>
        public static bool ShowJunkIcon => Settings?.showJunkIcon ?? true;

        /// <summary>Convenience for the "cascade junk from corpses" setting.</summary>
        public static bool CascadeJunkFromCorpses => Settings?.cascadeJunkFromCorpses ?? true;

        public MarkAsJunkMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<MarkAsJunkSettings>();
            // Patch each class separately: a game update that renames one target must
            // degrade to "that vanilla behavior stays", never break the other patch.
            Harmony harmony = new Harmony(PackageId);
            PatchSafe(harmony, typeof(Patch_ThingWithComps_InitializeComps));
            PatchSafe(harmony, typeof(Patch_StorageSettings_AllowedToAccept));
            PatchSafe(harmony, typeof(Patch_StorageSettings_ExposeData));
            PatchSafe(harmony, typeof(Patch_StorageSettings_CopyFrom));
            PatchSafe(harmony, typeof(Patch_ZoneStockpile_Gizmos));
            PatchSafe(harmony, typeof(Patch_BuildingStorage_Gizmos));
            PatchSafe(harmony, typeof(Patch_HaulDestinationManager_Add));
            PatchSafe(harmony, typeof(Patch_HaulDestinationManager_Remove));
            PatchSafe(harmony, typeof(Patch_Thing_SpawnSetup));
            PatchSafe(harmony, typeof(Patch_SlotGroup_NotifyAddedCell));
            PatchSafe(harmony, typeof(Patch_StorageGroupUtility_SetStorageGroup));
            PatchSafe(harmony, typeof(Patch_ApparelTracker_TryDrop));
            PatchSafe(harmony, typeof(Patch_Pawn_EquipmentTracker_TryDropEquipment));
            PatchSafe(harmony, typeof(Patch_DynamicDrawManager_DrawDynamicThings));
            PatchSafe(harmony, typeof(Patch_Corpse_Destroy));
            PatchSafe(harmony, typeof(Patch_RecipeDef_WorkAmountTotal));
            DebugLog.Message("loaded (enabled=" + (Settings.enabled ? "true" : "false")
                + ", debugLevel=" + (DebugLogLevel)Settings.debugLevel + ").");
        }

        private static void PatchSafe(Harmony harmony, Type patchClass)
        {
            try
            {
                harmony.CreateClassProcessor(patchClass).Patch();
                DebugLog.Message("applied " + patchClass.Name + ".");
            }
            catch (Exception e)
            {
                Log.Error("[MarkAsJunk] Patch " + patchClass.Name + " could not be applied (game update?). " + e.Message);
            }
        }

        public override string SettingsCategory()
        {
            return "MarkAsJunk.SettingsCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);
            list.CheckboxLabeled("MarkAsJunk.Enabled".Translate(), ref Settings.enabled, "MarkAsJunk.Enabled.Tip".Translate());
            list.Gap(6f);

            Rect debugRect = list.GetRect(30f);
            string levelName = ((DebugLogLevel)Settings.debugLevel).ToString();
            if (Widgets.ButtonText(debugRect, "MarkAsJunk.DebugLevel".Translate(levelName)))
            {
                Settings.debugLevel = (Settings.debugLevel + 1) % 3;
            }
            TooltipHandler.TipRegion(debugRect, "MarkAsJunk.DebugLevel.Tip".Translate());
            list.Gap(6f);

            list.CheckboxLabeled("MarkAsJunk.RequireJunkStorage".Translate(), ref Settings.routeOnlyWithJunkStorage, "MarkAsJunk.RequireJunkStorage.Tip".Translate());
            list.Gap(6f);

            list.CheckboxLabeled("MarkAsJunk.AutoMarkDroppedGear".Translate(), ref Settings.autoMarkDroppedGear, "MarkAsJunk.AutoMarkDroppedGear.Tip".Translate());
            list.Gap(6f);

            list.CheckboxLabeled("MarkAsJunk.CascadeJunkFromCorpses".Translate(), ref Settings.cascadeJunkFromCorpses, "MarkAsJunk.CascadeJunkFromCorpses.Tip".Translate());
            list.Gap(6f);

            bool smelterRecipes = Settings.smelterRecipes;
            list.CheckboxLabeled("MarkAsJunk.SmelterRecipes".Translate(), ref smelterRecipes, "MarkAsJunk.SmelterRecipes.Tip".Translate());
            if (smelterRecipes != Settings.smelterRecipes)
            {
                Settings.smelterRecipes = smelterRecipes;
                SmelterJunkRecipes.ApplyVisibility();
            }
            list.Gap(6f);

            list.CheckboxLabeled("MarkAsJunk.ShowJunkIcon".Translate(), ref Settings.showJunkIcon, "MarkAsJunk.ShowJunkIcon.Tip".Translate());
            list.Gap(12f);

            Rect overviewRect = list.GetRect(30f);
            if (Widgets.ButtonText(overviewRect, "MarkAsJunk.LogOverview".Translate()))
            {
                JunkDebug.LogOverview();
            }
            TooltipHandler.TipRegion(overviewRect, "MarkAsJunk.LogOverview.Tip".Translate());
            list.Gap(12f);

            GUI.color = ColoredText.SubtleGrayColor;
            list.Label("MarkAsJunk.BehaviorNote".Translate());
            GUI.color = Color.white;
            list.End();
        }
    }
}
