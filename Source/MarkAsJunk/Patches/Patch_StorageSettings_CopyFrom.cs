using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MarkAsJunk
{
    /// <summary>Keeps copy/paste of storage settings intuitive: both flags
    /// (junk dump, treat contents as junk) travel with the copied filter and
    /// priority. CopyFrom is what the vanilla copy/paste storage gizmos use;
    /// the clipboard itself has a null owner, so pasting onto it (the copy
    /// direction) cannot trigger side effects.</summary>
    [HarmonyPatch(typeof(StorageSettings), "CopyFrom")]
    internal static class Patch_StorageSettings_CopyFrom
    {
        static void Postfix(StorageSettings __instance, StorageSettings other)
        {
            try
            {
                Map map = JunkStorageUtility.MapOf(__instance);
                bool junk = JunkStorageUtility.IsJunk(other);
                if (junk != JunkStorageUtility.IsJunk(__instance))
                {
                    JunkStorageUtility.SetJunk(__instance, junk, map);
                }
                bool junkify = JunkStorageUtility.IsJunkify(other);
                if (junkify != JunkStorageUtility.IsJunkify(__instance))
                {
                    JunkStorageUtility.SetJunkify(__instance, junkify);
                }
            }
            catch (Exception ex)
            {
                Log.Error("[MarkAsJunk] CopyFrom patch failed: " + ex);
            }
        }
    }
}
