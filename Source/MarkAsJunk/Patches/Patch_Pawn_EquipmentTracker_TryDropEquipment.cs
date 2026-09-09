using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MarkAsJunk
{
    /// <summary>Equipment-side twin of the corpse cascade: when gear drops
    /// from a DEAD pawn whose corpse is junk-marked, it is marked as junk.
    /// This is what makes the strip the game itself performs when hauling a
    /// corpse to a bill (RecipeDef.autoStripCorpses) or a strip order produce
    /// junk-marked gear instead of unmarked drops. The same funnel covers
    /// death drops and our own Corpse.Destroy cascade (marking is idempotent).
    /// Pawns that are merely downed - e.g. gear kept on them by gear-keeping
    /// mods - are never touched.</summary>
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.TryDropEquipment))]
    internal static class Patch_Pawn_EquipmentTracker_TryDropEquipment
    {
        static void Postfix(Pawn_EquipmentTracker __instance, ThingWithComps resultingEq, bool __result)
        {
            try
            {
                if (!__result || resultingEq == null || !MarkAsJunkMod.Active
                    || !MarkAsJunkMod.CascadeJunkFromCorpses)
                {
                    return;
                }
                Pawn pawn = __instance.pawn;
                if (pawn == null || !pawn.Dead || !JunkMarkUtility.IsMarkedJunk(pawn.Corpse))
                {
                    return;
                }
                JunkMarkUtility.SetJunk(resultingEq, value: true);
                if (DebugLog.MessageEnabled)
                {
                    DebugLog.Message("cascade-marked " + resultingEq.LabelShort + " as junk from corpse of "
                        + pawn.LabelShort + ".");
                }
            }
            catch (Exception ex)
            {
                Log.Error("[MarkAsJunk] EquipmentTracker.TryDropEquipment patch failed: " + ex);
            }
        }
    }
}
