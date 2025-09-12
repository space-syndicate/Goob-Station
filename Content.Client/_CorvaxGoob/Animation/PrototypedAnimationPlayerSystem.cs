using Content.Shared._CorvaxGoob.Animation;
using Content.Shared._CorvaxGoob.Animation.API;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._CorvaxGoob.Animation;

public sealed class PrototypedAnimationPlayerSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _component = default!;
    [Dependency] private readonly AnimationPlayerSystem _anim = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<PlayAnimationMessage>(OnPlayAnimationMessage);
    }

    private void OnPlayAnimationMessage(PlayAnimationMessage ev)
    {
        if (_prototype.TryIndex<AnimationPrototype>(ev.AnimationID, out var animation))
            PlayAnimation(GetEntity(ev.AnimatedEntity), animation);
    }

    public void PlayAnimation(EntityUid entityUid, AnimationPrototype animationPrototype)
    {
        if (!entityUid.Valid)
            return;

        if (_anim.HasRunningAnimation(entityUid, animationPrototype.ID))
            return;

        var animation = new Robust.Client.Animations.Animation();

        foreach (var track in animationPrototype.Tracks)
        {
            var trackData = new AnimationTrackComponentProperty();

            _component.TryGetRegistration(track.ComponentType, out var registration, true);
            if (registration is null)
                return;

            var comp = _component.GetComponent(registration.Idx);

            trackData.ComponentType = comp.GetType();
            if (trackData.ComponentType is null)
                continue;

            trackData.Property = track.Property;
            trackData.InterpolationMode = track.InterpolationMode;

            foreach (var keyFrame in track.KeyFrames)
            {
                object result = keyFrame.Type.ToLower() switch
                {
                    "int" => int.Parse(keyFrame.Value),
                    "float" => float.Parse(keyFrame.Value),
                    "vector2" => YamlHelpers.AsVector2(keyFrame.Value),
                    "angle" => Angle.FromDegrees(float.Parse(keyFrame.Value)),
                    _ => 0
                };

                trackData.KeyFrames.Add(new AnimationTrackProperty.KeyFrame(result, keyFrame.Keyframe));
            }

            animation.AnimationTracks.Add(trackData);
        }
        animation.Length = TimeSpan.FromSeconds(animationPrototype.Length);

        _anim.Play(entityUid, animation, animationPrototype.ID);
    }
}
