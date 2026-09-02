using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MarkAsJunk
{
    /// <summary>The single routing choke point. Both Zone_Stockpile.Accepts
    /// and Building_Storage.Accepts (and nearly every modded storage) funnel
    /// into StorageSettings.AllowedToAccept, so one postfix covers everything:
    ///
    ///  - a storage flagged as junk dump keeps working as a normal storage:
    ///    its own filter whitelist applies as always, and the flag only
    ///    exempts it from the rule below (so junk lands there too, subject
    ///    to the same filter);
    ///  - every OTHER storage rejects junk-marked things while at least one
    ///    junk dump exists on the item's map (fallback controlled by
    ///    setting), which is what makes pawns re-route junk into dumps.
    ///
    /// The owner != null guard is essential: vanilla calls this method
    /// recursively on owner.GetParentStoreSettings() (fixed def filters, the
    /// shared EverStorableFixedSettings), and those parent settings have a
    /// null owner. Without the guard the "normal storage rejects junk" rule
    /// would fire on the parent check and make even junk dumps reject junk.</summary>
    [HarmonyPatch(typeof(StorageSettings), "AllowedToAccept", new Type[] { typeof(Thing) })]
    internal static class Patch_StorageSettings_AllowedToAccept
    {
        static void Postfix(StorageSettings __instance, Thing t, ref bool __result)
        {
            try
            {
                if (!MarkAsJunkMod.Active || !__result || t == null || __instance.owner == null)
                {
                    return;
                }
                if (!JunkMarkUtility.IsMarkedJunk(t) || JunkStorageUtility.IsJunk(__instance))
                {
                    return;
                }
                if (JunkRoutingWantsDump(t))
                {
                    __result = false;
                    if (DebugLog.VerboseEnabled)
                    {
                        DebugLog.Verbose("normal storage " + JunkStorageUtility.DescribeOwner(__instance) + " rejected junk " + t.LabelShort + " (junk dump exists).");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("[MarkAsJunk] Acceptance patch failed: " + ex);
            }
        }

        /// <summary>Junk is routed away from normal storage only when there is
        /// somewhere for it to go (or the player opted out of the fallback).</summary>
        private static bool JunkRoutingWantsDump(Thing t)
        {
            if (MarkAsJunkMod.RejectJunkInNormalStorageOnlyWithDump)
            {
                return JunkStorageUtility.AnyJunkDestination(t.MapHeld);
            }
            return true;
        }
    }
}
