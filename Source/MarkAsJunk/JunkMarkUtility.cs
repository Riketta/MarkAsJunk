using RimWorld;
using UnityEngine;
using Verse;

namespace MarkAsJunk
{
    /// <summary>Item-side helpers: reading/writing the junk mark and loading
    /// shared gizmo resources.</summary>
    internal static class JunkMarkUtility
    {
        private static Texture2D gizmoIconInt;

        private static Texture2D junkifyIconInt;

        private static KeyBindingDef hotKeyInt;

        private static bool hotKeySearched;

        /// <summary>Set while a bulk operation (storage toggles, stockpile
        /// growth) marks many items at once: the per-item Basic log in
        /// <see cref="CompJunkMark.MarkedJunk"/> drops to Verbose so the
        /// caller's single summary line stays readable. Bulk loops never
        /// nest, so a plain flag with try/finally at the call sites is
        /// enough.</summary>
        public static bool BulkMarking;

        /// <summary>Own texture with graceful fallback to a vanilla icon so a
        /// missing/renamed PNG can never break the UI.</summary>
        public static Texture2D GizmoIcon
        {
            get
            {
                if (gizmoIconInt == null)
                {
                    gizmoIconInt = ContentFinder<Texture2D>.Get("UI/Commands/MarkAsJunk", reportFailure: false)
                        ?? TexCommand.ForbidOff;
                    if (gizmoIconInt == TexCommand.ForbidOff)
                    {
                        DebugLog.Warning("icon Textures/UI/Commands/MarkAsJunk.png not found, using vanilla fallback.");
                    }
                }
                return gizmoIconInt;
            }
        }

        /// <summary>Icon for the storage "treat contents as junk" toggle;
        /// falls back to the item junk icon if its own PNG is missing.</summary>
        public static Texture2D JunkifyIcon
        {
            get
            {
                if (junkifyIconInt == null)
                {
                    junkifyIconInt = ContentFinder<Texture2D>.Get("UI/Commands/MarkAsJunkAll", reportFailure: false) ?? GizmoIcon;
                    if (junkifyIconInt == GizmoIcon)
                    {
                        DebugLog.Warning("icon Textures/UI/Commands/MarkAsJunkAll.png not found, falling back to the junk item icon.");
                    }
                }
                return junkifyIconInt;
            }
        }

        /// <summary>Optional hotkey def (Defs/KeyBindings_MarkAsJunk.xml);
        /// null is fine - vanilla gizmos handle an unbound key. The search
        /// result (including a miss) is cached: gizmos rebuild every frame,
        /// so an uncached miss would re-query DefDatabase per frame.</summary>
        public static KeyBindingDef HotKey
        {
            get
            {
                if (!hotKeySearched)
                {
                    hotKeySearched = true;
                    hotKeyInt = DefDatabase<KeyBindingDef>.GetNamedSilentFail("MarkAsJunkToggle");
                }
                return hotKeyInt;
            }
        }

        public static CompJunkMark Comp(Thing t)
        {
            return (t as ThingWithComps)?.GetComp<CompJunkMark>();
        }

        /// <summary>Fast null-safe check used on the storage hot path.</summary>
        public static bool IsMarkedJunk(Thing t)
        {
            return Comp(t)?.MarkedJunk ?? false;
        }

        public static void SetJunk(Thing t, bool value)
        {
            CompJunkMark compJunkMark = Comp(t);
            if (compJunkMark != null)
            {
                compJunkMark.MarkedJunk = value;
            }
            else
            {
                DebugLog.Verbose("cannot set junk flag on " + t?.LabelShort + " - no CompJunkMark attached.");
            }
        }
    }
}
