using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

        private static readonly ConditionalWeakTable<Map, HashSet<Thing>> overlayThings =
            new ConditionalWeakTable<Map, HashSet<Thing>>();

        /// <summary>Keeps the per-map overlay registry in sync. Called from the
        /// mark setter and from SpawnSetup. Items are DrawerType.MapMeshOnly -
        /// baked into the static map mesh - so they never get a per-frame comp
        /// PostDraw like animated buildings do; that is exactly how vanilla
        /// forbidden icons work too (CompForbiddable registers with the map's
        /// overlay system instead of drawing). The draw pass prunes despawned
        /// leftovers. ConditionalWeakTable keys the registry by map so map
        /// instances are never kept alive by it.</summary>
        public static void OverlayNotifySpawnState(Thing t, Map map, bool marked)
        {
            if (map == null)
            {
                return;
            }
            HashSet<Thing> set = overlayThings.GetOrCreateValue(map);
            if (marked)
            {
                set.Add(t);
            }
            else
            {
                set.Remove(t);
            }
        }

        private static Material junkOverlayMatInt;

        /// <summary>The comp's junk mark as a small icon drawn over the item in
        /// the world, the way forbidden items show one. Vanilla's OverlayDrawer
        /// only supports a closed set of hardcoded overlay types, so this draws
        /// its own quad (same MetaOverlay shader and mesh size as the forbidden
        /// icon), centered on the item. The material is built once from the
        /// shared gizmo icon (with its fallback) and reused.</summary>
        public static void DrawJunkOverlay(Thing t)
        {
            if (junkOverlayMatInt == null)
            {
                junkOverlayMatInt = MaterialPool.MatFrom(GizmoIcon, ShaderDatabase.MetaOverlay, Color.white);
            }
            Vector3 drawPos = t.DrawPos;
            drawPos.z += 0.05f;
            drawPos.y = AltitudeLayer.MetaOverlays.AltitudeFor() + 0.15f;
            Graphics.DrawMesh(MeshPool.plane05, drawPos, Quaternion.identity, junkOverlayMatInt, 0);
        }

        /// <summary>Per-frame draw for one map, invoked from the
        /// DynamicDrawManager patch. Prunes things that despawned since the
        /// last frame (carried away, merged, destroyed) instead of hooking
        /// every despawn path.</summary>
        public static void DrawJunkOverlays(Map map)
        {
            if (!MarkAsJunkMod.Active || !MarkAsJunkMod.ShowJunkIcon
                || !overlayThings.TryGetValue(map, out HashSet<Thing> set))
            {
                return;
            }
            List<Thing> stale = null;
            foreach (Thing t in set)
            {
                if (!t.Spawned || t.Map != map)
                {
                    (stale ??= new List<Thing>()).Add(t);
                    continue;
                }
                if (!t.Fogged())
                {
                    DrawJunkOverlay(t);
                }
            }
            if (stale != null)
            {
                foreach (Thing t in stale)
                {
                    set.Remove(t);
                }
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
    }
}
