using Content.Shared._CorvaxGoob.TintedWindow;
using Robust.Client.Player;

namespace Content.Client._CorvaxGoob.TintedWindow;

public sealed class TintedWindowSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly OccluderSystem _occluder = default!;

    public override void FrameUpdate(float frameTime) // _BlackM: moved from Update, visuals once per frame
    {
        base.FrameUpdate(frameTime);

        if (_player.LocalEntity is null)
            return;

        if (TryComp<EyeComponent>(_player.LocalEntity, out var eye) && !eye.DrawFov)
            return;

        var playerPos = _xform.GetWorldPosition(_player.LocalEntity.Value); // _BlackM: hoisted out of the loop
        var query = EntityQueryEnumerator<TintedWindowComponent, OccluderComponent>();

        while (query.MoveNext(out var uid, out var window, out var occluder))
        {
            var angle = Angle.FromWorldVec(playerPos - _xform.GetWorldPosition(uid)); // _BlackM: cached player position
            var angleDelta = (_xform.GetWorldRotation(uid) - angle).Reduced().FlipPositive();
            var showOccluder = angleDelta < window.Arc / 2 || Math.Tau - angleDelta < window.Arc / 2;
            _occluder.SetEnabled(uid, showOccluder, occluder);
        }
    }
}
