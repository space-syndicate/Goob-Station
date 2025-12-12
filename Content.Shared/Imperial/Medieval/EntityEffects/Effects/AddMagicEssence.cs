using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Medieval.EntityEffects;


/// <summary>
///     Explodes the body
/// </summary>
public sealed partial class AddMagicEssence : EntityEffectBase<AddMagicEssence>
{
    /// <summary>
    /// Added reagents to entity
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> AddedEssences = new();


    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => "";
}

