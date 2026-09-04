using System;
using HarmonyLib;
using Verse;

namespace MarkAsJunk
{
    /// <summary>Draws the junk overlay icons once per frame per map, right
    /// after vanilla draws its dynamic things and before its own overlay pass.
    /// This cannot live in a ThingComp.PostDraw: items are
    /// DrawerType.MapMeshOnly (baked into the static map mesh) and never see a
    /// per-frame draw call - the same reason vanilla's forbidden overlay is
    /// driven from the map's draw loop instead of CompForbiddable.PostDraw.
    /// Errors self-disable the drawing rather than spamming the log every
    /// frame.</summary>
    [HarmonyPatch(typeof(DynamicDrawManager), nameof(DynamicDrawManager.DrawDynamicThings))]
    internal static class Patch_DynamicDrawManager_DrawDynamicThings
    {
        private static bool disabledByError;

        static void Postfix(Map ___map)
        {
            if (disabledByError || ___map == null || ___map != Find.CurrentMap)
            {
                return;
            }
            try
            {
                JunkMarkUtility.DrawJunkOverlays(___map);
            }
            catch (Exception ex)
            {
                disabledByError = true;
                Log.Error("[MarkAsJunk] junk overlay drawing failed, disabling overlay icons: " + ex);
            }
        }
    }
}
