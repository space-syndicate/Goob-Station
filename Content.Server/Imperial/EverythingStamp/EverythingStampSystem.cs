using Content.Shared.Interaction.Events;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Robust.Shared.Audio.Systems;
using Content.Shared.Popups;

namespace Content.Server.Paper
{
    public sealed class EverythingStampSystem : EntitySystem
    {
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;


        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EverythingStampComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<EverythingStampComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<EverythingStampComponent, UseInHandEvent>(OnInHandActivation);
        }
        private void OnInit(EntityUid uid, EverythingStampComponent everythingStampComp, ComponentInit args)
        {
            everythingStampComp.CollectedStamps.Add(new StampDisplayInfo { StampedName = Loc.GetString("stamp-component-stamped-name-syndicate"), StampedColor = Color.FromHex("#850000") });
        }
        private void OnInHandActivation(Entity<EverythingStampComponent> entity, ref UseInHandEvent args)
        {
            var positionOfPreviousMode = -1;
            var i = 0;
            entity.Comp.CollectedStamps.ForEach(item =>
            {
                positionOfPreviousMode = item.StampedName == entity.Comp.CurrentStampName ? i : positionOfPreviousMode;
                i += 1;
            });
            positionOfPreviousMode = positionOfPreviousMode == entity.Comp.CollectedStamps.Count - 1 ? -1 : positionOfPreviousMode;
            entity.Comp.CurrentStampName = entity.Comp.CollectedStamps[positionOfPreviousMode + 1].StampedName;
            _popup.PopupEntity(Loc.GetString("everything-stamp-chosen-stamp") + " " + Loc.GetString(entity.Comp.CollectedStamps[positionOfPreviousMode + 1].StampedName), entity, args.User);
            if (TryComp(entity, out StampComponent? stamp))
            {
                stamp.StampedName = entity.Comp.CollectedStamps[positionOfPreviousMode + 1].StampedName;
                stamp.StampedColor = entity.Comp.CollectedStamps[positionOfPreviousMode + 1].StampedColor;
            }
        }

        private void OnInteractUsing(EntityUid uid, EverythingStampComponent everythingStampComp, InteractUsingEvent args)
        {
            if (!TryComp<StampComponent>(args.Used, out var stampComp)) return;

            if (!TryCopyStamp(uid, GetStampInfo(stampComp), stampComp.StampState, everythingStampComp))
            {
                _audio.PlayPvs(stampComp.Sound, uid);
                _popup.PopupEntity(Loc.GetString("everything-stamp-new-stamp-added"), uid, args.User);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("everything-stamp-new-stamp-already-added"), uid, args.User);
            }
        }
        private static StampDisplayInfo GetStampInfo(StampComponent stamp)
        {
            return new StampDisplayInfo
            {
                StampedName = stamp.StampedName,
                StampedColor = stamp.StampedColor
            };
        }
        public bool TryCopyStamp(EntityUid uid, StampDisplayInfo stampInfo, string spriteStampState, EverythingStampComponent comp)
        {
            var ifAlreadyInCollected = false;
            comp.CollectedStamps.ForEach(item => ifAlreadyInCollected = ifAlreadyInCollected || item.StampedName == stampInfo.StampedName);
            if (!ifAlreadyInCollected) comp.CollectedStamps.Add(stampInfo);
            return ifAlreadyInCollected;
        }
    }
}
