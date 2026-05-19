using Content.Shared.Imperial.Abilities.Urs.Systems;
using System.Numerics;
using Content.Shared.Damage;

namespace Content.Shared.Imperial.Abilities.Urs.Components
{
    [RegisterComponent, AutoGenerateComponentState]
    [Access(typeof(UrsDashSystem))]
    public sealed partial class UrsDashComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsDashing = false;
        [DataField]
        public float Accumulator = 0f;
        [DataField]
        public float UpdateInterval = 0.65f;
        [DataField]
        public float UpdateIntervalToDash = 0.4f;
        [DataField]
        public float PushStrength = 13f;
        [DataField]
        public float ReversePushStrength = 120f; // 60 = 1 tile
        [DataField]
        public EntityUid Target;
        [DataField(required: true), AutoNetworkedField]
        public DamageSpecifier Damage = default!;
    }
}
