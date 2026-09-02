using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MarkAsJunk
{
    /// <summary>Persists both storage flags inside the storage's own XML node
    /// without touching vanilla save format beyond two extra bools. Runs in
    /// both save and load directions; on load the flags land in the weak
    /// table keyed by the freshly created StorageSettings instance.</summary>
    [HarmonyPatch(typeof(StorageSettings), "ExposeData")]
    internal static class Patch_StorageSettings_ExposeData
    {
        static void Postfix(StorageSettings __instance)
        {
            try
            {
                // Persist regardless of the enabled switch so marks survive a
                // temporarily disabled mod (same policy as the item comp).
                bool junk = JunkStorageUtility.IsJunk(__instance);
                bool junkify = JunkStorageUtility.IsJunkify(__instance);
                if (Scribe.mode == LoadSaveMode.Saving && !junk && !junkify)
                {
                    // Unflagged storage stays byte-identical to vanilla saves;
                    // loading a missing node simply yields the false default.
                    return;
                }
                Scribe_Values.Look(ref junk, "markedAsJunkDump", defaultValue: false);
                Scribe_Values.Look(ref junkify, "markedAsJunkify", defaultValue: false);
                JunkStorageUtility.SetStateSilent(__instance, junk, junkify);
            }
            catch (Exception ex)
            {
                Log.Error("[MarkAsJunk] Failed to persist storage flags: " + ex);
            }
        }
    }
}
