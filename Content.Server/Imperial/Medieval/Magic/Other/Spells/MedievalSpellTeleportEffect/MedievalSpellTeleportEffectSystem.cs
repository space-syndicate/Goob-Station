using Content.Server.Examine;
using Content.Shared.Imperial.Medieval.Magic;
using Content.Shared.Imperial.Medieval.Magic.MedievalSpellTeleportEffect;

namespace Content.Server.Imperial.Medieval.Magic.MedievalSpellTeleportEffect;


/// <summary>
/// Adds phase space effects to the caster.
/// </summary>
public sealed partial class MedievalSpellTeleportEffectSystem : SharedMedievalSpellTeleportEffectSystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void OnBeforeCast(EntityUid uid, MedievalSpellTeleportEffectComponent component, ref MedievalBeforeCastSpellEvent args)
    {
        base.OnBeforeCast(uid, component, ref args);
    }

    protected override void OnAfterCast(EntityUid uid, MedievalSpellTeleportEffectComponent component, ref MedievalAfterCastSpellEvent args)
    {
        base.OnAfterCast(uid, component, ref args);
    }
}
