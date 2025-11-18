using Content.Shared.Actions;

namespace Content.Shared.Imperial.Vampire
{
    public sealed partial class VampireRushBloodEvent : InstantActionEvent
    {
        [DataField]
        public float BoostSpeed = 2.0f;
    }
}
