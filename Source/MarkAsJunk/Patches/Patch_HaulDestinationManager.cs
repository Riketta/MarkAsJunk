using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MarkAsJunk
{
    /// <summary>Destination registration changed - invalidate the cached
    /// "any junk dump on map" answer. A freshly added destination cannot be
    /// junk-flagged yet (flags only move through SetJunk/CopyFrom/load, all
    /// of which invalidate on their own), so a cache bump is sufficient.</summary>
    [HarmonyPatch(typeof(HaulDestinationManager), "AddHaulDestination")]
    internal static class Patch_HaulDestinationManager_Add
    {
        static void Postfix()
        {
            JunkStorageUtility.NotifyDestinationsChanged();
        }
    }

    /// <summary>A destination was removed - possibly the last junk dump. The full
    /// haulables recomputation runs only when the cached "any junk dump on map"
    /// answer actually flipped to false, so deleting a normal stockpile stays
    /// as cheap as in vanilla. Wrapped like every other patch: this fires
    /// inside vanilla DeSpawn/zone deregistration (including map teardown),
    /// where an exception would propagate into game code.</summary>
    [HarmonyPatch(typeof(HaulDestinationManager), "RemoveHaulDestination")]
    internal static class Patch_HaulDestinationManager_Remove
    {
        static void Postfix(Map ___map)
        {
            try
            {
                // Routing rules only change when the last dump disappears. If no
                // cached "any junk dump" answer exists or it was false, the rules
                // were not in effect and the removal cannot change acceptance.
                bool hadCachedDump = JunkStorageUtility.TryGetCachedAnyJunkDestination(___map, out bool wasJunk) && wasJunk;
                JunkStorageUtility.NotifyDestinationsChanged();
                if (hadCachedDump && !JunkStorageUtility.AnyJunkDestination(___map))
                {
                    JunkStorageUtility.RecalcAllSlotGroups(___map);
                    DebugLog.Message("the last junk dump on map " + ___map.uniqueID + " was removed - storage routing recomputed.");
                }
            }
            catch (Exception ex)
            {
                Log.Error("[MarkAsJunk] RemoveHaulDestination patch failed: " + ex);
            }
        }
    }
}
