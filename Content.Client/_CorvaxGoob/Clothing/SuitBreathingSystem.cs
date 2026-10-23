using Content.Shared._CorvaxGoob.Clothing;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Inventory;
using Robust.Client.Player;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._CorvaxGoob.Clothing;

public sealed class SuitBreathingSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedInternalsSystem _internals = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityUid? _activeStream;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var localPlayer = _player.LocalEntity;
        if (localPlayer == null || !Exists(localPlayer.Value))
        {
            _activeStream = _audio.Stop(_activeStream);
            return;
        }

        if (!_inventory.TryGetSlotEntity(localPlayer.Value, "head", out var headUid) ||
            !TryComp<SuitBreathingComponent>(headUid, out var breathingComp))
        {
            _activeStream = _audio.Stop(_activeStream);
            return;
        }

        if (!TryComp<BreathToolComponent>(headUid, out var breathTool) ||
            !breathTool.IsFunctional ||
            !_internals.AreInternalsWorking(localPlayer.Value))
        {
            _activeStream = _audio.Stop(_activeStream);
            breathingComp.Timer = 0f;
            return;
        }

        if (_activeStream != null &&
            TryComp<AudioComponent>(_activeStream, out var audioComp) &&
            audioComp.Playing)
        {
            return;
        }

        breathingComp.Timer -= frameTime;
        if (breathingComp.Timer <= 0f)
        {
            breathingComp.Timer = _random.NextFloat(breathingComp.MinInterval, breathingComp.MaxInterval);

            if (breathingComp.BreathingSound != null)
            {
                _activeStream = _audio.PlayLocal(
                    breathingComp.BreathingSound,
                    localPlayer.Value,
                    localPlayer.Value,
                    breathingComp.BreathingSound.Params)?.Entity;
            }
        }
    }
}
