using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MarkAsJunk
{
    /// <summary>Cascade junk from corpses: when a junk-marked corpse is
    /// destroyed - the "smelt/destroy junk" bills being the main case - the
    /// worn apparel and the equipped weapon it still holds are dropped at the
    /// corpse's position as junk, instead of being silently destroyed with it.
    /// Vanilla's Corpse.Destroy -> PostCorpseDestroy destroys everything the
    /// pawn still holds; the drops here make those wipes no-ops. The inventory
    /// ("pockets") is dropped too but stays unmarked - normal loot for normal
    /// storage - because with gear-keeping mods (e.g. KeepYourGear) corpses
    /// can hold real loot that must not vanish. Unspawned corpses (graves,
    /// containers, world pawns) keep vanilla behavior, and corpse stripping
    /// already drops gear on its own.
    /// Implemented as a prefix because the vanilla body runs after it and
    /// wipes the pawn's trackers - post-drop they are empty.</summary>
    [HarmonyPatch(typeof(Corpse), "Destroy")]
    internal static class Patch_Corpse_Destroy
    {
        static void Prefix(Corpse __instance)
        {
            try
            {
                if (!__instance.Spawned || __instance.Bugged
                    || !MarkAsJunkMod.Active || !MarkAsJunkMod.CascadeJunkFromCorpses
                    || !JunkMarkUtility.IsMarkedJunk(__instance))
                {
                    return;
                }
                Pawn pawn = __instance.InnerPawn;
                if (pawn == null)
                {
                    return;
                }
                int marked = 0;
                int droppedInventory = 0;
                JunkMarkUtility.BulkMarking = true;
                try
                {
                    if (pawn.apparel != null && pawn.apparel.WornApparelCount > 0)
                    {
                        List<Apparel> wornApparel = pawn.apparel.WornApparel;
                        for (int i = wornApparel.Count - 1; i >= 0; i--)
                        {
                            JunkMarkUtility.SetJunk(wornApparel[i], value: true);
                            marked++;
                        }
                        // dropLocked: the pawn is being discarded, so locked
                        // apparel would otherwise just be destroyed with it.
                        pawn.apparel.DropAll(__instance.PositionHeld, forbid: false, dropLocked: true);
                    }
                    if (pawn.equipment != null && pawn.equipment.AllEquipmentListForReading.Count > 0)
                    {
                        List<ThingWithComps> equipment = pawn.equipment.AllEquipmentListForReading;
                        for (int i = equipment.Count - 1; i >= 0; i--)
                        {
                            JunkMarkUtility.SetJunk(equipment[i], value: true);
                            marked++;
                        }
                        pawn.equipment.DropAllEquipment(__instance.PositionHeld, forbid: false);
                    }
                    if (pawn.inventory != null && pawn.inventory.innerContainer.TotalStackCount > 0)
                    {
                        // Pockets stay unmarked loot: haulers bring them to
                        // normal storage and the player decides per item.
                        droppedInventory = pawn.inventory.innerContainer.TotalStackCount;
                        pawn.inventory.DropAllNearPawn(__instance.PositionHeld, forbid: false);
                    }
                }
                finally
                {
                    JunkMarkUtility.BulkMarking = false;
                }
                if ((marked > 0 || droppedInventory > 0) && DebugLog.MessageEnabled)
                {
                    DebugLog.Message("cascade-marked " + marked + " dropped items as junk from corpse of "
                        + pawn.LabelShort + " (plus " + droppedInventory + " unmarked inventory items).");
                }
            }
            catch (Exception ex)
            {
                Log.Error("[MarkAsJunk] Corpse.Destroy patch failed: " + ex);
            }
        }
    }
}
