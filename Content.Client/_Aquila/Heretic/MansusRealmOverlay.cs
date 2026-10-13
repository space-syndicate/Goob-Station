using Content.Shared._Aquila.Heretic;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Aquila.Heretic;

public sealed class MansusRealmOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> MadnessShader = "MansusMadness";
    private static readonly TimeSpan RampDuration = TimeSpan.FromSeconds(4);

    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public override bool RequestScreenTexture => true;

    public MansusRealmOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _proto.Index(MadnessShader).InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _entity.TryGetComponent(_player.LocalEntity, out EyeComponent? eye)
               && args.Viewport.Eye == eye.Eye;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        var intensity = 1f;
        if (_entity.TryGetComponent(_player.LocalEntity, out MansusVictimComponent? victim))
        {
            var t = Math.Clamp((float) ((_timing.CurTime - victim.ArrivalTime) / RampDuration), 0f, 1f);
            intensity = t * t * (3f - 2f * t);
        }

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("intensity", intensity);
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
