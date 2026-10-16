using System.Diagnostics.CodeAnalysis;
using Content.Goobstation.Server.Xenobiology;
using Content.Goobstation.Shared._Aquila.Xenobiology.Components;
using Content.Goobstation.Shared.Xenobiology;
using Content.Goobstation.Shared.Xenobiology.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared._Aquila.Xenobiology.Systems;

public sealed class SlimeCrossbreedingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SlimeLatchSystem _latch = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SlimeComponent, SlimeCrossbreedDoAfterEvent>(OnSlimeCrossbreedDoAfter);
        SubscribeLocalEvent<SlimeComponent, SlimeScannerInfoEvent>(OnSlimeScannerInfo);
    }

    private void OnSlimeScannerInfo(Entity<SlimeComponent> ent, ref SlimeScannerInfoEvent args)
    {
        if (!TryGetCategory(ent.Comp.Breed, out var category))
            return;

        args.Text.AppendLine(Loc.GetString("slime-scanner-crossbreed-category",
            ("color", category.Color.ToHex()),
            ("category", Loc.GetString(category.Name)),
            ("required", category.Required)));

        if (TryComp<SlimeCrossbreedingComponent>(ent, out var crossbreeding) && crossbreeding.Extract is { } extract)
        {
            args.Text.AppendLine(Loc.GetString("slime-scanner-crossbreed-progress",
                ("fed", crossbreeding.Fed),
                ("required", category.Required),
                ("extract", _proto.Index(extract).Name)));
        }
    }

    private void OnInteractUsing(Entity<SlimeComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled
            || !HasComp<SlimeExtractComponent>(args.Used)
            || _mobState.IsDead(ent))
            return;

        args.Handled = true;

        if (!CanFeed(ent, args.Used, args.User, out var category, out _))
            return;

        var doAfter = new DoAfterArgs(EntityManager, args.User, category.FeedDelay, new SlimeCrossbreedDoAfterEvent(), ent, ent, args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnSlimeCrossbreedDoAfter(Entity<SlimeComponent> ent, ref SlimeCrossbreedDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used is not { } extract)
            return;

        args.Handled = true;

        if (_mobState.IsDead(ent)
            || !CanFeed(ent, extract, args.User, out var category, out var extractProto))
            return;

        var crossbreeding = EnsureComp<SlimeCrossbreedingComponent>(ent);
        crossbreeding.Extract = extractProto;
        crossbreeding.Fed++;
        Dirty(ent, crossbreeding);

        PredictedQueueDel(extract);

        if (crossbreeding.Fed < category.Required)
        {
            _popup.PopupClient(Loc.GetString("slime-crossbreed-fed", ("fed", crossbreeding.Fed), ("required", category.Required)), ent, args.User);
            return;
        }

        _popup.PopupPredicted(Loc.GetString("slime-crossbreed-complete", ("slime", ent)), ent, args.User, PopupType.Medium);

        if (_net.IsClient)
            return;

        _audio.PlayPvs(category.Sound, ent);
        SpawnNextToOrDrop(category.Results[extractProto], ent);

        _latch.Unlatch(ent);
        _container.EmptyContainer(ent.Comp.Stomach);
        QueueDel(ent);
    }

    private bool CanFeed(
        Entity<SlimeComponent> slime,
        EntityUid extract,
        EntityUid user,
        [NotNullWhen(true)] out SlimeCrossbreedCategoryPrototype? category,
        out EntProtoId extractProto)
    {
        extractProto = default;

        if (!TryGetCategory(slime.Comp.Breed, out category))
        {
            _popup.PopupClient(Loc.GetString("slime-crossbreed-not-interested", ("slime", slime)), slime, user);
            return false;
        }

        if (TryComp<MobGrowthComponent>(slime, out var growth) && growth.IsFirstStage)
        {
            _popup.PopupClient(Loc.GetString("slime-crossbreed-too-young", ("slime", slime)), slime, user);
            return false;
        }

        if (!HasComp<ReactiveComponent>(extract) || MetaData(extract).EntityPrototype is not { } proto)
        {
            _popup.PopupClient(Loc.GetString("slime-crossbreed-spent"), slime, user);
            return false;
        }

        extractProto = proto.ID;

        if (!category.Results.ContainsKey(extractProto))
        {
            _popup.PopupClient(Loc.GetString("slime-crossbreed-rejects", ("slime", slime)), slime, user);
            return false;
        }

        if (TryComp<SlimeCrossbreedingComponent>(slime, out var crossbreeding)
            && crossbreeding.Extract is { } current
            && current != extractProto)
        {
            _popup.PopupClient(Loc.GetString("slime-crossbreed-saturated", ("slime", slime), ("extract", _proto.Index(current).Name)), slime, user);
            return false;
        }

        return true;
    }

    private bool TryGetCategory(ProtoId<BreedPrototype> breed, [NotNullWhen(true)] out SlimeCrossbreedCategoryPrototype? category)
    {
        foreach (var candidate in _proto.EnumeratePrototypes<SlimeCrossbreedCategoryPrototype>())
        {
            if (candidate.Breed != breed)
                continue;

            category = candidate;
            return true;
        }

        category = null;
        return false;
    }
}
