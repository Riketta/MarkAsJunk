using HarmonyLib;
using Verse;

namespace MarkAsJunk
{
    /// <summary>Vanilla gives RecipeDef.WorkAmountTotal a special case only
    /// for its own SmeltOrDestroyThing recipe: smeltable things cost
    /// smeltingWorkAmount, everything else falls through to the recipe's own
    /// workAmount. Mirror that exact behavior for the junk variant, so
    /// smeltable junk is priced like vanilla smelting (1600) while
    /// non-smeltable junk stays at that recipe's base workAmount (400).
    /// The separate fast "destroy junk" recipe (60) needs no special case -
    /// it is always base workAmount.</summary>
    [HarmonyPatch(typeof(RecipeDef), "WorkAmountTotal", new System.Type[] { typeof(Thing) })]
    internal static class Patch_RecipeDef_WorkAmountTotal
    {
        static void Postfix(RecipeDef __instance, Thing thing, ref float __result)
        {
            if (thing != null && __instance == MarkAsJunkDefOf.MarkAsJunk_SmeltOrDestroyJunk && __instance.smeltingWorkAmount >= 0f && thing.Smeltable)
            {
                __result = __instance.smeltingWorkAmount;
            }
        }
    }
}
