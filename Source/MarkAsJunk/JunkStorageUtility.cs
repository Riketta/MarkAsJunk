using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;

namespace MarkAsJunk
{
    /// <summary>Storage-side state and routing rules.
    ///
    /// Both flags (junk dump, treat-contents-as-junk) live on
    /// <see cref="StorageSettings"/> (not on zones or buildings) so stockpile
    /// zones, storage buildings and entire storage groups all behave
    /// identically, and any modded IHaulDestination that funnels through
    /// StorageSettings.AllowedToAccept is covered too.
    ///
    /// Flags are held in a ConditionalWeakTable (no leaks from deleted
    /// zones/buildings) and persisted by Patch_StorageSettings_ExposeData.</summary>
    internal static class JunkStorageUtility
    {
        private class JunkState
        {
            public bool Junk;

            public bool Junkify;
        }

        private class MapCacheEntry
        {
            public int Version = -1;

            public bool Result;
        }

        private static readonly ConditionalWeakTable<StorageSettings, JunkState> States = new ConditionalWeakTable<StorageSettings, JunkState>();

        private static readonly ConditionalWeakTable<Map, MapCacheEntry> MapCache = new ConditionalWeakTable<Map, MapCacheEntry>();

        /// <summary>Bumped whenever anything could change the set of registered
        /// junk destinations; per-map results are recomputed lazily.</summary>
        private static int version;

        public static bool IsJunk(StorageSettings settings)
        {
            return settings != null && States.TryGetValue(settings, out JunkState state) && state.Junk;
        }

        public static bool IsJunkify(StorageSettings settings)
        {
            return settings != null && States.TryGetValue(settings, out JunkState state) && state.Junkify;
        }

        /// <summary>Player-facing setter for the junk dump flag: flags the
        /// storage, invalidates caches and forces a full haulables
        /// re-evaluation so pawns react at once.</summary>
        public static void SetJunk(StorageSettings settings, bool value, Map map)
        {
            if (settings == null || IsJunk(settings) == value)
            {
                return;
            }
            SetStateSilent(settings, value, IsJunkify(settings));
            RecalcAllSlotGroups(map);
            DebugLog.Message("junk dump flag " + (value ? "set on " : "cleared from ") + DescribeOwner(settings) + ".");
        }

        /// <summary>Player-facing setter for "treat contents as junk": while
        /// on, everything stored here is marked as junk automatically (on
        /// arrival and, when the flag is set, retroactively for the current
        /// contents). Marks are kept when the flag is turned off again -
        /// unmarking is always a per-item decision, never a side effect.</summary>
        public static void SetJunkify(StorageSettings settings, bool value)
        {
            if (settings == null || IsJunkify(settings) == value)
            {
                return;
            }
            SetStateSilent(settings, IsJunk(settings), value);
            if (value)
            {
                MarkContentsAsJunk(settings);
            }
            DebugLog.Message("treat-contents-as-junk flag " + (value ? "set on " : "cleared from ") + DescribeOwner(settings) + ".");
        }

        /// <summary>Sets both flags without side effects - used while loading
        /// saves (stored items already carry their marks, and the listerHaulables
        /// rebuild happens after load anyway).</summary>
        public static void SetStateSilent(StorageSettings settings, bool junk, bool junkify)
        {
            JunkState junkState = States.GetOrCreateValue(settings);
            junkState.Junk = junk;
            junkState.Junkify = junkify;
            version++;
        }

        /// <summary>True when at least one registered haul destination on this
        /// map is flagged as a junk dump. Cached per map; the cache version is
        /// invalidated by flag changes, copy/paste and destination
        /// registration changes.</summary>
        public static bool AnyJunkDestination(Map map)
        {
            if (map == null)
            {
                return false;
            }
            MapCacheEntry mapCacheEntry = MapCache.GetOrCreateValue(map);
            if (mapCacheEntry.Version != version)
            {
                mapCacheEntry.Result = ComputeAnyJunkDestination(map);
                mapCacheEntry.Version = version;
                DebugLog.Verbose("recomputed 'any junk dump on map' for map " + map.uniqueID + ": " + mapCacheEntry.Result + ".");
            }
            return mapCacheEntry.Result;
        }

        /// <summary>Peeks the cached answer without recomputing; false when no
        /// cached answer exists yet. Used to detect flips cheaply.</summary>
        public static bool TryGetCachedAnyJunkDestination(Map map, out bool result)
        {
            result = false;
            if (map != null && MapCache.TryGetValue(map, out MapCacheEntry mapCacheEntry) && mapCacheEntry.Version == version)
            {
                result = mapCacheEntry.Result;
                return true;
            }
            return false;
        }

        private static bool ComputeAnyJunkDestination(Map map)
        {
            List<IHaulDestination> allHaulDestinationsListForReading = map.haulDestinationManager.AllHaulDestinationsListForReading;
            for (int i = 0; i < allHaulDestinationsListForReading.Count; i++)
            {
                if (IsJunk(allHaulDestinationsListForReading[i].GetStoreSettings()))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Registration changed (zone/building added or removed):
        /// stale "any junk dump" answers must not be served.</summary>
        public static void NotifyDestinationsChanged()
        {
            version++;
        }

        /// <summary>Re-runs the haulable check for everything stored anywhere
        /// on the map. Vanilla does exactly this for the one group whose filter
        /// changed; junk routing is map-global, so all groups are refreshed.
        /// Rare (player clicked a toggle / a dump was deleted), so the cost is
        /// acceptable.</summary>
        public static void RecalcAllSlotGroups(Map map)
        {
            if (map == null || map.Disposed || map.listerHaulables == null)
            {
                return;
            }
            List<SlotGroup> allGroupsListInPriorityOrder = map.haulDestinationManager.AllGroupsListInPriorityOrder;
            for (int i = 0; i < allGroupsListInPriorityOrder.Count; i++)
            {
                map.listerHaulables.Notify_SlotGroupChanged(allGroupsListInPriorityOrder[i]);
            }
        }

        /// <summary>Toggle gizmo shared by stockpile zones and storage
        /// buildings. Works on whatever settings object the selection uses,
        /// which makes it apply storage-group-wide for grouped shelves.</summary>
        public static Gizmo CreateJunkStorageGizmo(StorageSettings settings, Map map)
        {
            // Capture the target state at creation (gizmos are rebuilt every
            // frame, so this is always fresh). When several selected buildings
            // share one settings object (a storage group), GizmoGridDrawer
            // activates every merged gizmo whose isActive matches; computing
            // !IsJunk live inside the action would flip the shared flag once
            // per gizmo and cancel itself out. SetJunk no-ops on repeats.
            bool flag = !IsJunk(settings);
            Command_Toggle command_Toggle = new Command_Toggle();
            command_Toggle.hotKey = JunkMarkUtility.HotKey;
            command_Toggle.icon = JunkMarkUtility.GizmoIcon;
            command_Toggle.isActive = () => IsJunk(settings);
            command_Toggle.activateIfAmbiguous = false;
            command_Toggle.defaultLabel = "MarkAsJunk.JunkStorage".Translate();
            command_Toggle.defaultDesc = IsJunk(settings)
                ? "MarkAsJunk.JunkStorage.DescOn".Translate()
                : "MarkAsJunk.JunkStorage.DescOff".Translate();
            command_Toggle.toggleAction = delegate
            {
                SetJunk(settings, flag, map);
            };
            return command_Toggle;
        }

        /// <summary>"Treat contents as junk" toggle, next to the junk dump
        /// toggle on stockpile zones and storage buildings. Same
        /// capture-state-at-creation pattern as the dump gizmo above (grouped
        /// shelves share one settings object via GetStoreSettings, so this
        /// applies storage-group-wide too). Deliberately no hotkey: it shares
        /// the grid with the item and dump toggles, which already bind
        /// MarkAsJunkToggle, and one key press must not flip two gizmos.</summary>
        public static Gizmo CreateJunkifyGizmo(StorageSettings settings)
        {
            bool flag = !IsJunkify(settings);
            Command_Toggle command_Toggle = new Command_Toggle();
            command_Toggle.icon = JunkMarkUtility.JunkifyIcon;
            command_Toggle.isActive = () => IsJunkify(settings);
            command_Toggle.activateIfAmbiguous = false;
            command_Toggle.defaultLabel = "MarkAsJunk.Junkify".Translate();
            command_Toggle.defaultDesc = IsJunkify(settings)
                ? "MarkAsJunk.Junkify.DescOn".Translate()
                : "MarkAsJunk.Junkify.DescOff".Translate();
            command_Toggle.toggleAction = delegate
            {
                SetJunkify(settings, flag);
            };
            return command_Toggle;
        }

        /// <summary>Marks everything currently stored under these settings as
        /// junk. Slot groups are resolved by settings identity, which covers
        /// zones, single buildings and every member of a storage group (their
        /// slot groups all report the shared group settings) with one loop.</summary>
        public static void MarkContentsAsJunk(StorageSettings settings)
        {
            Map map = MapOf(settings);
            if (map == null || map.Disposed || map.listerHaulables == null)
            {
                return;
            }
            int marked = 0;
            List<SlotGroup> allGroupsListInPriorityOrder = map.haulDestinationManager.AllGroupsListInPriorityOrder;
            JunkMarkUtility.BulkMarking = true;
            try
            {
                for (int i = 0; i < allGroupsListInPriorityOrder.Count; i++)
                {
                    if (allGroupsListInPriorityOrder[i].Settings != settings)
                    {
                        continue;
                    }
                    foreach (Thing heldThing in allGroupsListInPriorityOrder[i].HeldThings)
                    {
                        CompJunkMark compJunkMark = JunkMarkUtility.Comp(heldThing);
                        if (compJunkMark == null || compJunkMark.MarkedJunk)
                        {
                            continue;
                        }
                        JunkMarkUtility.SetJunk(heldThing, value: true);
                        marked++;
                    }
                }
            }
            finally
            {
                JunkMarkUtility.BulkMarking = false;
            }
            DebugLog.Message("marked " + marked + " stored thing(s) inside " + DescribeOwner(settings) + " as junk.");
        }

        /// <summary>Marks the haulable things currently lying in one cell
        /// (comp-less items are skipped, matching the item toggle). Returns
        /// the number of newly marked things so the caller can summarize.</summary>
        public static int MarkCellContentsAsJunk(Map map, IntVec3 cell)
        {
            List<Thing> thingList = map.thingGrid.ThingsListAt(cell);
            int marked = 0;
            JunkMarkUtility.BulkMarking = true;
            try
            {
                for (int i = 0; i < thingList.Count; i++)
                {
                    CompJunkMark compJunkMark = JunkMarkUtility.Comp(thingList[i]);
                    if (compJunkMark != null && !compJunkMark.MarkedJunk)
                    {
                        JunkMarkUtility.SetJunk(thingList[i], value: true);
                        marked++;
                    }
                }
            }
            finally
            {
                JunkMarkUtility.BulkMarking = false;
            }
            return marked;
        }

        /// <summary>Best-effort map for a settings owner: zones and storage
        /// buildings are IHaulDestination; storage groups are not, but expose
        /// their own Map. Null when unknown.</summary>
        public static Map MapOf(StorageSettings settings)
        {
            if (settings?.owner == null)
            {
                return null;
            }
            if (settings.owner is IHaulDestination haulDestination)
            {
                return haulDestination.Map;
            }
            if (settings.owner is StorageGroup storageGroup)
            {
                return storageGroup.Map;
            }
            return null;
        }

        public static string DescribeOwner(StorageSettings settings)
        {
            if (settings?.owner == null)
            {
                return "orphaned settings";
            }
            if (settings.owner is Zone zone)
            {
                return "zone '" + zone.label + "'";
            }
            if (settings.owner is StorageGroup storageGroup)
            {
                return "storage group '" + storageGroup.RenamableLabel + "'";
            }
            if (settings.owner is Thing thing)
            {
                return thing.LabelShort;
            }
            return settings.owner.ToString();
        }
    }
}
