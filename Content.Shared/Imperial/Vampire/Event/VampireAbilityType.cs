using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Vampire;

[Serializable, NetSerializable]
public enum VampireAbilityType : byte
{
    Base,
    Hemomancer,
    Umbrae,
    Gargantua
}
