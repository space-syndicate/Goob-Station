using Robust.Shared.GameStates;
using Content.Shared.Damage;
using Robust.Shared.Utility;
namespace Content.Shared.Imperial.PiratesNewHorizon.Reagent.Components
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class ResistModifierMetabolismComponent : Component
    {
        [AutoNetworkedField, ViewVariables]
        public DamageModifierSet Modifiers { get; set; }
        /// <summary>
        /// When the current modifier is expected to end.
        /// </summary>
        [AutoNetworkedField, ViewVariables]
        public TimeSpan ModifierTimer { get; set; } = TimeSpan.Zero;
    }
}

