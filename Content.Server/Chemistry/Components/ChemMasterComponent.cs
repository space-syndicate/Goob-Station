// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry;
// CorvaxGoob start
using Content.Shared.Chemistry.Reagent;
// CorvaxGoob end
using Robust.Shared.Audio;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// An industrial grade chemical manipulator with pill and bottle production included.
    /// <seealso cref="ChemMasterSystem"/>
    /// </summary>
    [RegisterComponent]
    [Access(typeof(ChemMasterSystem))]
    public sealed partial class ChemMasterComponent : Component
    {
        [DataField("pillType"), ViewVariables(VVAccess.ReadWrite)]
        public uint PillType = 0;

        [DataField("mode"), ViewVariables(VVAccess.ReadWrite)]
        public ChemMasterMode Mode = ChemMasterMode.Transfer;

        [DataField]
        public ChemMasterSortingType SortingType = ChemMasterSortingType.None;

        [DataField("pillDosageLimit", required: true), ViewVariables(VVAccess.ReadWrite)]
        public uint PillDosageLimit;

        [DataField("clickSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        /// <summary>
        /// Which source the chem master should draw from when making pills/bottles.
        /// </summary>
        [DataField]
        public ChemMasterDrawSource DrawSource = ChemMasterDrawSource.Internal;

        // CorvaxGoob start
        /// <summary>
        /// Temperatures stored independently for reagents in the internal buffer.
        /// Reagents in a ChemMaster do not exchange heat until they are dispensed into a real solution.
        /// </summary>
        [ViewVariables]
        public readonly Dictionary<ReagentId, float> BufferReagentTemperatures = new();
        // CorvaxGoob end
    }
}