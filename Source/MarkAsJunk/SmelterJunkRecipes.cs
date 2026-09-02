using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MarkAsJunk
{
    /// <summary>Runtime visibility control for the smelter junk recipes.
    /// Recipes attach to the smelter through recipeUsers (the recipe lists
    /// its users; ThingDef.AllRecipes scans for that), so visibility is a
    /// matter of keeping ElectricSmelter in the list and clearing the
    /// smelter's cached AllRecipes afterwards. Existing bills keep working
    /// either way - they reference the recipe directly.</summary>
    [StaticConstructorOnStartup]
    internal static class SmelterJunkRecipes
    {
        private static readonly FieldInfo allRecipesCachedField = AccessTools.Field(typeof(ThingDef), "allRecipesCached");

        static SmelterJunkRecipes()
        {
            // Runs after all defs are loaded and resolved; applies the saved
            // setting once. Skipped gracefully when the defs are missing
            // (e.g. a mod removed the ElectricSmelter def entirely).
            ApplyVisibility();
        }

        public static void ApplyVisibility()
        {
            bool visible = MarkAsJunkMod.Settings?.smelterRecipes ?? true;
            SetRecipeVisible(MarkAsJunkDefOf.MarkAsJunk_SmeltOrDestroyJunk, visible);
            SetRecipeVisible(MarkAsJunkDefOf.MarkAsJunk_DestroyJunk, visible);
        }

        private static void SetRecipeVisible(RecipeDef recipe, bool visible)
        {
            try
            {
                if (recipe == null || MarkAsJunkDefOf.ElectricSmelter == null)
                {
                    return;
                }
                recipe.recipeUsers ??= new List<ThingDef>();
                bool flag = recipe.recipeUsers.Contains(MarkAsJunkDefOf.ElectricSmelter);
                if (visible)
                {
                    if (!flag)
                    {
                        recipe.recipeUsers.Add(MarkAsJunkDefOf.ElectricSmelter);
                    }
                }
                else if (flag)
                {
                    recipe.recipeUsers.Remove(MarkAsJunkDefOf.ElectricSmelter);
                }
                // AllRecipes caches the attached recipe list per building def.
                allRecipesCachedField?.SetValue(MarkAsJunkDefOf.ElectricSmelter, null);
                DebugLog.Message("smelter recipe " + recipe.defName + (visible ? " shown." : " hidden."));
            }
            catch (System.Exception ex)
            {
                Log.Error("[MarkAsJunk] Failed to set smelter recipe visibility: " + ex);
            }
        }
    }
}
