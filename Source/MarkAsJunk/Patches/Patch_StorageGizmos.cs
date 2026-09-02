using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MarkAsJunk
{
    /// <summary>Appends the "junk dump" and "treat contents as junk" toggles
    /// to stockpile zone gizmos (next to copy/paste storage settings). The
    /// toggles read and write the zone's StorageSettings, so they behave
    /// identically to the building versions and stay data-driven.</summary>
    [HarmonyPatch(typeof(Zone_Stockpile), "GetGizmos")]
    internal static class Patch_ZoneStockpile_Gizmos
    {
        static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Zone_Stockpile __instance)
        {
            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }
            if (MarkAsJunkMod.Active && __instance.Map != null)
            {
                yield return JunkStorageUtility.CreateJunkStorageGizmo(__instance.settings, __instance.Map);
                yield return JunkStorageUtility.CreateJunkifyGizmo(__instance.settings);
            }
        }
    }

    /// <summary>Same toggles for storage buildings (shelves etc.). Player
    /// faction only, matching how vanilla gates editable storage. Grouped
    /// buildings toggle their shared group settings - the whole group becomes
    /// the dump / junk source, mirroring how their filter already works.</summary>
    [HarmonyPatch(typeof(Building_Storage), "GetGizmos")]
    internal static class Patch_BuildingStorage_Gizmos
    {
        static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Building_Storage __instance)
        {
            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }
            if (MarkAsJunkMod.Active && __instance.Faction == Faction.OfPlayer && __instance.Spawned)
            {
                yield return JunkStorageUtility.CreateJunkStorageGizmo(__instance.GetStoreSettings(), __instance.Map);
                yield return JunkStorageUtility.CreateJunkifyGizmo(__instance.GetStoreSettings());
            }
        }
    }
}
