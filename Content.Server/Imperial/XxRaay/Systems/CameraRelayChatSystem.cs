using Content.Server.Chat.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Server.Speech;
using Content.Shared.Chat;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Speech;
using Content.Shared.StationAi;
using Robust.Shared.Map;

namespace Content.Server.Imperial.XxRaay.Systems;

public sealed class CameraRelayChatSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CameraRelayChatComponent, TransformSpeakerNameEvent>(OnTransformSpeakerName);
        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech, after: new[] { typeof(AccentSystem) });
        SubscribeLocalEvent<CameraRelayChatComponent, EntitySpokeEvent>(OnEntitySpoke, before: new[] { typeof(HeadsetSystem) });
    }

    private void OnTransformSpeakerName(EntityUid uid, CameraRelayChatComponent component, ref TransformSpeakerNameEvent args)
    {
        if (!component.Enabled || args.Sender != uid)
            return;

        if (!TryGetClosestVision(uid, component, out var closest))
            return;

        args.VoiceName = Name(closest);
    }

    private void OnEntitySpoke(EntityUid uid, CameraRelayChatComponent component, EntitySpokeEvent args)
    {
        if (!component.Enabled)
            return;

        if (args.Channel != null && args.ObfuscatedMessage != null)
        {
            args.Channel = null;
        }
    }

    private void OnTransformSpeech(TransformSpeechEvent args)
    {
        if (!TryComp(args.Sender, out CameraRelayChatComponent? relay) || !relay.Enabled)
            return;

        if (args.Message.StartsWith('.') || args.Message.StartsWith(';') || args.Message.StartsWith(':'))
            return;

        if (!TryGetClosestVision(args.Sender, relay, out var camera))
            return;

        EnsureComp<SpeechComponent>(camera);
        _chat.TrySendInGameICMessage(camera, args.Message, InGameICChatType.Speak, ChatTransmitRange.Normal, checkRadioPrefix: false, ignoreActionBlocker: true);

        args.Message = string.Empty;
    }

    private bool TryGetClosestVision(EntityUid source, CameraRelayChatComponent relay, out EntityUid closest)
    {
        closest = default;

        var xformQuery = GetEntityQuery<TransformComponent>();
        if (!xformQuery.TryGetComponent(source, out var sourceXform))
            return false;

        var sourcePos = _xforms.GetWorldPosition(sourceXform, xformQuery);
        var maxRangeSq = relay.MaxRange > 0 ? relay.MaxRange * relay.MaxRange : float.PositiveInfinity;

        var closestDist = float.PositiveInfinity;

        var cameraQuery = EntityQueryEnumerator<StationAiVisionComponent, TransformComponent>();
        while (cameraQuery.MoveNext(out var visionUid, out var vision, out var xform))
        {
            if (!vision.Enabled)
                continue;

            if (xform.MapID != sourceXform.MapID || xform.MapID == MapId.Nullspace)
                continue;

            var distance = (_xforms.GetWorldPosition(xform, xformQuery) - sourcePos).LengthSquared();
            if (distance > maxRangeSq || distance >= closestDist)
                continue;

            closest = visionUid;
            closestDist = distance;
        }

        return closest != default;
    }
}
