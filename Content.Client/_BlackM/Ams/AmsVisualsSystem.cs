using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Content.Shared._BlackM.Ams;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._BlackM.Ams;

public sealed class AmsVisualsSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const int Layer = 0;
    private const float SyncIntroDuration = 1.6f;
    private const string StateBase = "base";
    private const string StateSync = "sync";
    private const string StateSyncStatic = "sync-2";
    private const string StateOn = "on";

    private readonly Dictionary<EntityUid, string> _currentState = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AmsComponent, ComponentShutdown>((uid, _, _) =>
        {
            _currentState.Remove(uid);
            _tinted.Remove(uid);
        });
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<AmsComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var ams, out var sprite))
        {
            UpdateFlicker(uid, ams, sprite);

            var state = GetDesiredState(ams);

            if (_currentState.TryGetValue(uid, out var current) && current == state)
                continue;

            _currentState[uid] = state;
            _sprite.LayerSetRsiState((uid, sprite), Layer, state);
        }
    }

    private static readonly Color FlickerGreen = Color.FromHex("#9dff8a");

    private readonly HashSet<EntityUid> _tinted = new();

    private void UpdateFlicker(EntityUid uid, AmsComponent ams, SpriteComponent sprite)
    {
        if (ams.Status != AmsStatus.Detonating)
        {
            if (_tinted.Remove(uid))
                _sprite.SetColor((uid, sprite), Color.White);
            return;
        }

        _tinted.Add(uid);

        var t = (float) _timing.RealTime.TotalSeconds;
        var progress = AmsHelpers.GetDetonationProgress(ams, _timing.CurTime);
        var flicker = 0.5f + 0.5f * MathF.Sin(t * (8f + 22f * progress)) * MathF.Sin(t * 3.1f);
        _sprite.SetColor((uid, sprite), Color.InterpolateBetween(Color.White, FlickerGreen, 0.35f + 0.5f * flicker * progress));
    }

    private string GetDesiredState(AmsComponent ams)
    {
        switch (ams.Status)
        {
            case AmsStatus.Synchronizing:
                if (ams.SyncStartTime is { } start && (_timing.CurTime - start).TotalSeconds < SyncIntroDuration)
                    return StateSync;
                return StateSyncStatic;

            case AmsStatus.Ready:
                return StateSyncStatic;

            case AmsStatus.Active:
            case AmsStatus.Detonating:
                return StateOn;

            default: // Inactive, Destroyed
                return StateBase;
        }
    }
}
