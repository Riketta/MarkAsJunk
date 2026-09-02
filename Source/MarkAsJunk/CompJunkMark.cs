using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MarkAsJunk
{
    public class CompProperties_JunkMark : CompProperties
    {
        public CompProperties_JunkMark()
        {
            compClass = typeof(CompJunkMark);
        }
    }

    /// <summary>Attachable "mark as junk" flag for haulable items - the junk
    /// analogue of <see cref="CompForbiddable"/>. Injected at runtime into
    /// every haulable thing by <c>Patch_ThingWithComps_InitializeComps</c>,
    /// so no def, DLC or mod item list is ever hardcoded.</summary>
    public class CompJunkMark : ThingComp
    {
        private bool markedJunkInt;

        public bool MarkedJunk
        {
            get
            {
                return markedJunkInt;
            }
            set
            {
                if (value == markedJunkInt)
                {
                    return;
                }
                markedJunkInt = value;
                // Acceptance and stack-mergeability of this item just changed
                // everywhere; make the listers re-evaluate it so pawns pick the
                // job up (or drop it) immediately instead of waiting for the
                // slow tick.
                if (parent.Spawned)
                {
                    parent.Map.listerHaulables.RecalcAllInCell(parent.Position);
                    parent.Map.listerMergeables.RecalcAllInCell(parent.Position);
                }
                if (JunkMarkUtility.BulkMarking)
                {
                    // Bulk operations summarize at Basic; keep the detail on Verbose.
                    if (DebugLog.VerboseEnabled)
                    {
                        DebugLog.Verbose((value ? "marked " : "unmarked ") + parent.LabelShort + " (ID " + parent.thingIDNumber + ") as junk. (bulk).");
                    }
                }
                else if (DebugLog.MessageEnabled)
                {
                    DebugLog.Message((value ? "marked " : "unmarked ") + parent.LabelShort + " (ID " + parent.thingIDNumber + ") as junk.");
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref markedJunkInt, "markedJunk", defaultValue: false);
        }

        /// <summary>Split-off pieces keep the mark, mirroring how forbidden
        /// state follows stack splits.</summary>
        public override void PostSplitOff(Thing piece)
        {
            base.PostSplitOff(piece);
            JunkMarkUtility.SetJunk(piece, markedJunkInt);
        }

        /// <summary>Junk and non-junk stacks of the same def must not merge,
        /// or marks would silently disappear (and a merged stack would carry a
        /// contradictory routing flag for storage acceptance).</summary>
        public override bool AllowStackWith(Thing other)
        {
            return base.AllowStackWith(other) && JunkMarkUtility.IsMarkedJunk(other) == markedJunkInt;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!MarkAsJunkMod.Active)
            {
                yield break;
            }
            Command_Toggle command_Toggle = new Command_Toggle();
            command_Toggle.hotKey = JunkMarkUtility.HotKey;
            command_Toggle.icon = JunkMarkUtility.GizmoIcon;
            command_Toggle.isActive = () => MarkedJunk;
            command_Toggle.activateIfAmbiguous = false;
            command_Toggle.defaultLabel = "MarkAsJunk.MarkJunk".Translate();
            command_Toggle.defaultDesc = markedJunkInt
                ? "MarkAsJunk.MarkJunk.DescOn".Translate()
                : "MarkAsJunk.MarkJunk.DescOff".Translate();
            command_Toggle.toggleAction = delegate
            {
                MarkedJunk = !markedJunkInt;
            };
            yield return command_Toggle;
        }

        public override string CompInspectStringExtra()
        {
            if (markedJunkInt && MarkAsJunkMod.Active)
            {
                return "MarkAsJunk.MarkedInspect".Translate();
            }
            return null;
        }
    }
}
