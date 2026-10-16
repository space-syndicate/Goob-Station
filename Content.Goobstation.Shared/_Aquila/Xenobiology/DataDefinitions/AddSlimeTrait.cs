using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared._Aquila.Xenobiology.DataDefinitions;

public sealed partial class AddSlimeTrait : EntityEffectBase<AddSlimeTrait>
{
    /// <summary>
    /// Добавляемая черта. Если не указана, добавляется случайная подходящая.
    /// </summary>
    [DataField]
    public ProtoId<SlimeTraitPrototype>? Trait;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}
