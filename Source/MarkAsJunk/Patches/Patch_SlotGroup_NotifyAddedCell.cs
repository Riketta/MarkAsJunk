using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MarkAsJunk
{
    /// <summary>Covers items already lying inside a cell when it joins a
    /// "treat contents as junk" storage: drawing a stockpile over loose items
    /// or expanding an existing junk stockpile. Newly designated zones start
    /// with the flag off, so this only ever fires on an actively junk-flagged
    /// zone growing. Toggle-time contents are handled by MarkContentsAsJunk;
    /// this handles later growth.</summary>
    [HarmonyPatch(typeof(SlotGroup), "Notify_AddedCell")]
    internal static class Patch_SlotGroup_NotifyAddedCell
    {
        static void Postfix(SlotGroup __instance, IntVec3 c)
        {
            try
            {
                if (!MarkAsJunkMod.Active || !JunkStorageUtility.IsJunkify(__instance.Settings))
                {
                    return;
                }
                Map map = __instance.parent.Map;
                if (map == null || map.Disposed)
                {
                    return;
                }
                int marked = JunkStorageUtility.MarkCellContentsAsJunk(map, c);
                if (marked > 0)
                {
                    DebugLog.Verbose("junk stockpile " + JunkStorageUtility.DescribeOwner(__instance.Settings)
                        + " grew over cell " + c + " - marked " + marked + " item(s) already lying there.");
                }
            }
            catch (Exception ex)
            {
                Log.Error("[MarkAsJunk] Notify_AddedCell patch failed: " + ex);
            }
        }
    }
}
