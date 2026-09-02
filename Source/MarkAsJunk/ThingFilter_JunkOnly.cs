using Verse;

namespace MarkAsJunk
{
    /// <summary>A ThingFilter that only passes things carrying the junk mark.
    /// Instantiated from XML via the Class attribute on recipe ingredient
    /// filters (&lt;filter Class="MarkAsJunk.ThingFilter_JunkOnly"&gt;), which
    /// is the only reliable way to gate ingredients per-thing: def-level
    /// filter fields (categories etc.) cannot express "marked" state, and
    /// special filters only enforce their disallow side.
    ///
    /// Every ingredient validation path consults this override:
    /// WorkGiver_DoBill.IsUsableIngredient / TryFindBestBillIngredientsInSet,
    /// Bill.IsFixedOrAllowedIngredient and
    /// RecipeDef.PotentiallyMissingIngredients.
    ///
    /// Gated on the mod's Enabled switch so a disabled mod pauses its bills
    /// (they simply find no ingredients) instead of silently consuming
    /// marked items nobody can see.</summary>
    public class ThingFilter_JunkOnly : ThingFilter
    {
        public override bool Allows(Thing t)
        {
            if (!MarkAsJunkMod.Active || t == null || !JunkMarkUtility.IsMarkedJunk(t))
            {
                return false;
            }
            return base.Allows(t);
        }
    }
}
