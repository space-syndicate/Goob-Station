// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared.Paper;

public sealed partial class PaperComponent
{
    /// <summary>
    /// Client-to-server request for the per-user insert helper payload.
    /// The message intentionally contains no client-supplied parameters: the server derives every
    /// value from the actor that sent the BUI message, so a modified client cannot ask for another
    /// crew member's station, identity, job, or manifest access.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class PaperInsertDataRequestMessage : BoundUserInterfaceMessage
    {
    }

    /// <summary>
    /// Server-to-client snapshot used by the paper insert helper.
    /// This is sent with ServerSendUiMessage to one actor only: station, identity, and job are
    /// actor-specific, while manifest entries are additionally gated by the actor's PDA.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class PaperInsertDataMessage : BoundUserInterfaceMessage
    {
        public readonly string? StationName;
        public readonly string? OwnName;
        public readonly string? OwnJob;
        public readonly PaperInsertManifestEntry[] ManifestEntries;

        /// <summary>
        /// Shift duration at the same server tick as <see cref="GameTime"/>.
        /// The client advances this by game time, not by the local OS clock.
        /// </summary>
        public readonly TimeSpan ShiftTime;

        /// <summary>
        /// Server game time when this snapshot was created.
        /// </summary>
        public readonly TimeSpan GameTime;

        /// <summary>
        /// Server-local calendar date at snapshot time. The client adds 1000 to the year only
        /// when formatting the paper text, matching the requested in-universe date display.
        /// </summary>
        public readonly int ServerDay;
        public readonly int ServerMonth;
        public readonly int ServerYear;

        public PaperInsertDataMessage(
            string? stationName,
            string? ownName,
            string? ownJob,
            PaperInsertManifestEntry[] manifestEntries,
            TimeSpan shiftTime,
            TimeSpan gameTime,
            int serverDay,
            int serverMonth,
            int serverYear)
        {
            StationName = stationName;
            OwnName = ownName;
            OwnJob = ownJob;
            ManifestEntries = manifestEntries;
            ShiftTime = shiftTime;
            GameTime = gameTime;
            ServerDay = serverDay;
            ServerMonth = serverMonth;
            ServerYear = serverYear;
        }
    }

    /// <summary>
    /// Single manifest row for the insert helper dropdown.
    /// The dropdown label is purely client-side; name and job are kept separate so insertion
    /// cannot accidentally copy the combined display string.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class PaperInsertManifestEntry
    {
        public readonly string Name;
        public readonly string JobTitle;

        public PaperInsertManifestEntry(string name, string jobTitle)
        {
            Name = name;
            JobTitle = jobTitle;
        }
    }
}
