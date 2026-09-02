using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MarkAsJunk
{
    /// <summary>When a storage building joins a storage group whose shared
    /// settings treat contents as junk, items already stored in the joining
    /// building must be marked too. This extension method is the single
    /// funnel every vanilla link path uses (the link/unlink gizmos and
    /// gravship storage group moves); re-marking the whole group is a no-op
    /// for members that were already marked.</summary>
    [HarmonyPatch(typeof(StorageGroupUtility), "SetStorageGroup")]
    internal static class Patch_StorageGroupUtility_SetStorageGroup
    {
        static void Postfix(IStorageGroupMember member, StorageGroup newGroup)
        {
            try
            {
                if (newGroup == null || !MarkAsJunkMod.Active || !JunkStorageUtility.IsJunkify(newGroup.GetStoreSettings()))
                {
                    return;
                }
                JunkStorageUtility.MarkContentsAsJunk(newGroup.GetStoreSettings());
            }
            catch (Exception ex)
            {
                Log.Error("[MarkAsJunk] SetStorageGroup patch failed: " + ex);
            }
        }
    }
}
