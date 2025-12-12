using Content.Shared.Chemistry.Reaction;
using Content.Shared.EntityEffects;
using Robust.Shared.Audio;

namespace Content.Server.Imperial.Medieval.Magic.MedievalSpellOnInteract;


/// <summary>
///
/// </summary>
[RegisterComponent]
public sealed partial class MedievalSpellOnInteractComponent : Component
{
    /// <summary>
    /// List of effects that should be applied.
    /// </summary>
    [DataField]
    public EntityEffect[] Effects = [];

    [DataField]
    public EntityEffect[] SelfUseEffects = [];

    [DataField]
    public SoundSpecifier? SoundOnInteract;

    [DataField]
    public int RemainingUses = 1;


    [ViewVariables]
    public int UseCount = 0;
}
