using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MarkAsJunk
{
    /// <summary>Optional auto-marking of gear pawns discard. Every case of worn
    /// apparel hitting the ground funnels through this one TryDrop overload:
    /// vanilla's filter-fail removal job, wardrobe swaps, strips and DropAll
    /// alike. The pawn's own apparel policy is what separates junk from
    /// upgrades - vanilla only ever auto-removes apparel that fails that
    /// filter (JobGiver_OptimizeApparel: tattered, wrong quality), while
    /// apparel taken off to make room for a better piece still passes it. So
    /// instead of sniffing call sites we simply consult the filter, which
    /// stays correct for any policy, DLC or modded filter. Pawns without a
    /// restrictive policy (the default "Anything" policy allows everything,
    /// which covers all non-player pawns) never drop anything that fails, so
    /// they are never affected. Inventory moves and caravan handling do not
    /// pass through here, and death does not drop apparel either.
    /// TargetMethod instead of an attribute argument list because the out
    /// parameter type (typeof(Apparel).MakeByRefType()) is not a compile-time
    /// constant.</summary>
    [HarmonyPatch]
    internal static class Patch_ApparelTracker_TryDrop
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.TryDrop),
                new[] { typeof(Apparel), typeof(Apparel).MakeByRefType(), typeof(IntVec3), typeof(bool) });
        }

        static void Postfix(Pawn_ApparelTracker __instance, Apparel ap, Apparel resultingAp, bool __result)
        {
            try
            {
                if (!__result || !MarkAsJunkMod.Active || !MarkAsJunkMod.AutoMarkDroppedGear)
                {
                    return;
                }
                ApparelPolicy policy = __instance.pawn?.outfits?.CurrentApparelPolicy;
                if (policy?.filter == null)
                {
                    return;
                }
                Thing thing = resultingAp ?? ap;
                // Still allowed by the policy: the pawn is swapping a good item
                // for a better one - the mark stays off.
                if (thing == null || thing.Destroyed || policy.filter.Allows(thing))
                {
                    return;
                }
                JunkMarkUtility.SetJunk(thing, value: true);
                if (DebugLog.MessageEnabled)
                {
                    DebugLog.Message("auto-marked " + thing.LabelShort + " as junk: " + __instance.pawn.LabelShort
                        + " dropped apparel that fails their policy '" + policy.label + "'.");
                }
            }
            catch (Exception ex)
            {
                Log.Error("[MarkAsJunk] ApparelTracker.TryDrop patch failed: " + ex);
            }
        }
    }
}
