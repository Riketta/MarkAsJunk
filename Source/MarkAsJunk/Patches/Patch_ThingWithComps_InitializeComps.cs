using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace MarkAsJunk
{
    /// <summary>Injects <see cref="CompJunkMark"/> into every haulable thing
    /// (items, corpses, minified buildings, modded haulables - anything with
    /// def.EverHaulable) at the moment vanilla builds its comp list. Runs both
    /// on PostMake and on load, so marks survive save/load and stack splits
    /// regardless of when things were created.</summary>
    [HarmonyPatch(typeof(ThingWithComps), "InitializeComps")]
    internal static class Patch_ThingWithComps_InitializeComps
    {
        private static readonly FieldInfo compsField = AccessTools.Field(typeof(ThingWithComps), "comps");

        private static readonly FieldInfo compsByTypeField = AccessTools.Field(typeof(ThingWithComps), "compsByType");

        /// <summary>Stateless configuration, shared by every instance - the
        /// same way vanilla shares one CompProperties across all things of a
        /// def. Keeps per-thing allocation to a single comp object.</summary>
        private static readonly CompProperties_JunkMark SharedProps = new CompProperties_JunkMark();

        // Injection is behavior-neutral (the flag defaults to false), so it
        // runs even while the mod is disabled - that way marks persist
        // through a temporarily disabled mod instead of being silently lost.
        static void Postfix(ThingWithComps __instance)
        {
            try
            {
                if (__instance.def == null || !__instance.def.EverHaulable || __instance is Pawn)
                {
                    return;
                }
                List<ThingComp> list = (List<ThingComp>)compsField.GetValue(__instance);
                if (list == null)
                {
                    list = new List<ThingComp>();
                    compsField.SetValue(__instance, list);
                }
                else
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] is CompJunkMark)
                        {
                            return;
                        }
                    }
                }
                CompJunkMark compJunkMark = new CompJunkMark();
                compJunkMark.parent = __instance;
                compJunkMark.Initialize(SharedProps);
                list.Add(compJunkMark);
                // Keep the by-type lookup coherent (GetComp<T> fast path) by
                // appending our entry instead of rebuilding the whole dict.
                Dictionary<Type, ThingComp[]> dictionary = (Dictionary<Type, ThingComp[]>)compsByTypeField.GetValue(__instance);
                ThingComp[] array = new ThingComp[1] { compJunkMark };
                if (dictionary == null)
                {
                    dictionary = new Dictionary<Type, ThingComp[]> { { typeof(CompJunkMark), array } };
                    compsByTypeField.SetValue(__instance, dictionary);
                }
                else
                {
                    dictionary[typeof(CompJunkMark)] = array;
                }
            }
            catch (Exception ex)
            {
                Log.Error("[MarkAsJunk] Failed to attach CompJunkMark to " + __instance?.def?.defName + ": " + ex);
            }
        }
    }
}
