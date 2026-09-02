using Verse;

namespace MarkAsJunk
{
    /// <summary>Optional layered logging, off by default (see mod settings).
    /// Basic: toggles, patch application, cache recomputes.
    /// Verbose: additionally every storage-acceptance override decision.
    /// Errors are always logged regardless of level.</summary>
    internal static class DebugLog
    {
        private const string Prefix = "[MarkAsJunk] ";

        private static bool AtLeast(DebugLogLevel level)
        {
            return (MarkAsJunkMod.Settings?.debugLevel ?? (int)DebugLogLevel.Off) >= (int)level;
        }

        /// <summary>Hot-path guards: check these BEFORE building log strings
        /// so disabled logging costs nothing but a property read. Verbose
        /// implies Message.</summary>
        public static bool MessageEnabled => AtLeast(DebugLogLevel.Basic);

        public static bool VerboseEnabled => AtLeast(DebugLogLevel.Verbose);

        /// <summary>Basic-level message (player-visible state changes).</summary>
        public static void Message(string message)
        {
            if (AtLeast(DebugLogLevel.Basic))
            {
                Verse.Log.Message(Prefix + message);
            }
        }

        /// <summary>Verbose-level message (hot-path decisions, cache internals).</summary>
        public static void Verbose(string message)
        {
            if (AtLeast(DebugLogLevel.Verbose))
            {
                Verse.Log.Message(Prefix + message);
            }
        }

        /// <summary>Unconditional warning (degraded behavior, not a crash).</summary>
        public static void Warning(string message)
        {
            Verse.Log.Warning(Prefix + message);
        }
    }
}
