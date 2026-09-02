using System.Text;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MarkAsJunk
{
    /// <summary>On-demand overview for debugging: one block per loaded map
    /// listing junk dumps and marked items. Reachable from the mod settings
    /// window ("Log junk overview") so it does not depend on dev-mode tooling
    /// that keeps changing between game versions.</summary>
    internal static class JunkDebug
    {
        public static void LogOverview()
        {
            if (Current.ProgramState != ProgramState.Playing || Find.Maps.NullOrEmpty())
            {
                DebugLog.Message("debug overview: no game / no maps loaded.");
                return;
            }
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("junk overview (enabled=" + MarkAsJunkMod.Active.ToString().ToLowerInvariant() + "):");
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map map = Find.Maps[i];
                stringBuilder.AppendLine("== map " + map.uniqueID + " (" + map.Biome.label + ") ==");
                AppendMapOverview(stringBuilder, map);
            }
            Verse.Log.Message("[MarkAsJunk] " + stringBuilder.ToString().TrimEnd());
        }

        private static void AppendMapOverview(StringBuilder sb, Map map)
        {
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            List<IHaulDestination> allHaulDestinationsListForReading = map.haulDestinationManager.AllHaulDestinationsListForReading;
            for (int i = 0; i < allHaulDestinationsListForReading.Count; i++)
            {
                StorageSettings storeSettings = allHaulDestinationsListForReading[i].GetStoreSettings();
                if (JunkStorageUtility.IsJunk(storeSettings))
                {
                    num++;
                    sb.AppendLine("  junk dump: " + JunkStorageUtility.DescribeOwner(storeSettings)
                        + " (priority " + storeSettings.Priority.Label() + ")");
                }
                if (JunkStorageUtility.IsJunkify(storeSettings))
                {
                    num3++;
                    sb.AppendLine("  treat-contents-as-junk: " + JunkStorageUtility.DescribeOwner(storeSettings)
                        + " (priority " + storeSettings.Priority.Label() + ")");
                }
            }
            List<Thing> list = map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver);
            for (int j = 0; j < list.Count; j++)
            {
                if (JunkMarkUtility.IsMarkedJunk(list[j]))
                {
                    num2++;
                }
            }
            sb.AppendLine("  junk dumps: " + num + ", junkify storages: " + num3 + ", junk-marked things: " + num2);
            if (num2 > 0 && num == 0)
            {
                sb.AppendLine("  note: marked items exist but no dump - with default settings junk still flows into normal storage.");
            }
        }
    }
}
