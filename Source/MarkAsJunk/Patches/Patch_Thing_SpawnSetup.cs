using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MarkAsJunk
{
    /// <summary>Items arriving inside a storage flagged "treat contents as
    /// junk" are marked automatically. SpawnSetup is the single choke point
    /// every item passes through when it appears at a cell (hauling drop-off,
    /// player placement, caravan unload, corpse creation), for stockpile
    /// zones, storage buildings, storage groups and modded slot group parents
    /// alike - vanilla resolves the slot group here itself to raise
    /// Notify_ReceivedThing.
    ///
    /// Skipped while loading saves: stored items already carry their marks,
    /// and the junkify flag is restored by ExposeData afterwards.</summary>
    [HarmonyPatch(typeof(Thing), "SpawnSetup")]
    internal static class Patch_Thing_SpawnSetup
    {
        static void Postfix(Thing __instance, Map map, bool respawningAfterLoad)
        {
            try
            {
                if (respawningAfterLoad || !MarkAsJunkMod.Active || __instance.def.category != ThingCategory.Item)
                {
                    return;
                }
                SlotGroup slotGroup = map.haulDestinationManager.SlotGroupAt(__instance.Position);
                if (slotGroup == null || !JunkStorageUtility.IsJunkify(slotGroup.Settings))
                {
                    return;
                }
                CompJunkMark compJunkMark = JunkMarkUtility.Comp(__instance);
                if (compJunkMark != null && !compJunkMark.MarkedJunk)
                {
                    JunkMarkUtility.SetJunk(__instance, value: true);
                    if (DebugLog.VerboseEnabled)
                    {
                        DebugLog.Verbose("auto-marked " + __instance.LabelShort + " on arrival in " + JunkStorageUtility.DescribeOwner(slotGroup.Settings) + ".");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("[MarkAsJunk] SpawnSetup patch failed: " + ex);
            }
        }
    }
}
