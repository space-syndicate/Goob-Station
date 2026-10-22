using Content.Goobstation.Common.MartialArts;
using Content.Goobstation.Shared.MartialArts.Components;

namespace Content.Goobstation.Shared._CorvaxGoob.MartialArts.Components;

[RegisterComponent]
public sealed partial class GrantMimejutsuComponent : GrantMartialArtKnowledgeComponent
{
    [DataField]
    public override MartialArtsForms MartialArtsForm { get; set; } = MartialArtsForms.Mimejutsu;

    public override LocId? LearnMessage { get; set; } = "mimejutsu-success-learned";
}
