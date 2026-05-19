using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.NinjaHeadset.Components
{
    [RegisterComponent, NetworkedComponent]
    [AutoGenerateComponentState]
    public sealed partial class NinjaHeadsetComponent : Component
    {
        [DataField]
        [AutoNetworkedField]
        public TimeSpan CopyFrequenciesTime = TimeSpan.FromSeconds(5);

        [DataField]
        [AutoNetworkedField]
        public List<string> CopiedFrequencies = new();

        [ViewVariables]
        public EntityUid? HackingTarget;

        [ViewVariables]
        public EntityUid? TargetHeadset;
    }
}
