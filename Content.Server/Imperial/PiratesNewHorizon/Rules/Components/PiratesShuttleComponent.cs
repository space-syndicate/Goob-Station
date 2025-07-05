using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking;

namespace Content.Server.Imperial.PiratesNewHorizon.Rules.Components;

[RegisterComponent]
public sealed partial class PiratesShuttleComponent : Component
{
    [DataField]
    public EntityUid AssociatedRule;
}
