using RimWorld;
using Verse;

namespace MarkAsJunk
{
    /// <summary>Def references bound automatically by defName. Includes the
    /// vanilla smelter def so the recipes attach to it without hardcoding a
    /// lookup at every call site.</summary>
    [DefOf]
    public static class MarkAsJunkDefOf
    {
        public static ThingDef ElectricSmelter;

        public static RecipeDef MarkAsJunk_SmeltOrDestroyJunk;

        public static RecipeDef MarkAsJunk_DestroyJunk;
    }
}
