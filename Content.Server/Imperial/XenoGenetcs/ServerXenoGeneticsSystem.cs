using Content.Shared.Imperial.XenoGenetics;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Imperial.XenoGenetics.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Content.Shared.Containers;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Content.Shared.Verbs;
using Microsoft.VisualBasic;
using System.Collections;
using System.Collections.Generic;

namespace Content.Server.Imperial.XenoGenetics;

public sealed class ServerXenoGeneticsSystem : SharedXenoGeneticsSystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GeneSplicerComponent, GeneInsertingDoAfterEvent>(OnGeneInsert);
        SubscribeLocalEvent<GeneSplicerComponent, GeneWithdrawDoAfterEvent>(OnGeneWithdraw);
        SubscribeLocalEvent<GeneSplicerComponent, UseInHandEvent>(OnSplicerUse);

        SubscribeLocalEvent<GeneCombinerComponent, GetVerbsEvent<AlternativeVerb>>(AddAltVerb);
        SubscribeLocalEvent<GeneCombinerComponent, InteractUsingEvent>(OnCombinerInteractUsing);
    }
    private void OnSplicerUse(EntityUid uid, GeneSplicerComponent component, UseInHandEvent args)
    {
        switch (component.InsertMode)
        {
            case GeneSplicerMode.Insert:
                component.InsertMode = GeneSplicerMode.Withdraw;
                _popup.PopupEntity(Loc.GetString("gene-splicer-mode-withdraw"), args.User, PopupType.Small);
                break;

            case GeneSplicerMode.Withdraw:
                component.InsertMode = GeneSplicerMode.Insert;
                _popup.PopupEntity(Loc.GetString("gene-splicer-mode-insert"), args.User, PopupType.Small);
                break;

            default:
                break;
        }
        args.Handled = true;
    }
    private void OnGeneInsert(EntityUid uid, GeneSplicerComponent comp, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (!TryComp<ContainerManagerComponent>(args.Target, out var slots))
        {
            _popup.PopupEntity(Loc.GetString("gene-splicer-error"), args.User, PopupType.Small);
            args.Handled = true;
            return;
        }

        var targetContainer = _containerSystem.EnsureContainer<Container>(args.Target.Value, comp.EntityGeneContainerID);
        var geneContainer = _containerSystem.GetContainer(uid, comp.GeneContainerID);

        var genesInstalled = new List<EntityUid>(targetContainer.ContainedEntities);
        if (genesInstalled.Any())
        {
            _popup.PopupEntity(Loc.GetString("gene-splicer-already-modified"), args.User, PopupType.Small);
            args.Handled = true;
            return;
        }

        var toInsert = new List<EntityUid>(geneContainer.ContainedEntities);
        if (!toInsert.Any())
        {
            _popup.PopupEntity(Loc.GetString("gene-splicer-gene-not-inserted"), args.User, PopupType.Small);
            args.Handled = true;
            return;
        }
        var insertedEv = new GeneInsertedEvent(toInsert[0], args.Target.Value);
        RaiseLocalEvent(toInsert[0], insertedEv);
        foreach (var geneUid in toInsert)
        {
            _containerSystem.Insert(geneUid, targetContainer);
        }
        args.Handled = true;
    }
    private void OnGeneWithdraw(EntityUid uid, GeneSplicerComponent comp, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (!TryComp<ContainerManagerComponent>(args.Target, out var slots))
        {
            _popup.PopupEntity(Loc.GetString("gene-splicer-error"), args.User, PopupType.Small);
            args.Handled = true;
            return;
        }

        var targetContainer = _containerSystem.EnsureContainer<Container>(args.Target.Value, comp.EntityGeneContainerID);
        var geneContainer = _containerSystem.GetContainer(uid, comp.GeneContainerID);

        var genesInstalled = new List<EntityUid>(targetContainer.ContainedEntities);
        if (!genesInstalled.Any())
        {
            _popup.PopupEntity(Loc.GetString("gene-splicer-has-no-gene"), args.User, PopupType.Small);
            args.Handled = true;
            return;
        }

        var toInsert = new List<EntityUid>(geneContainer.ContainedEntities);
        if (toInsert.Any())
        {
            _popup.PopupEntity(Loc.GetString("gene-splicer-slot-filled"), args.User, PopupType.Small);
            args.Handled = true;
            return;
        }
        var withdrawEv = new GeneWithdrawnEvent(genesInstalled[0], args.Target.Value);
        RaiseLocalEvent(genesInstalled[0], withdrawEv);
        foreach (var geneUid in genesInstalled)
        {
            _containerSystem.Insert(geneUid, geneContainer);
        }

        args.Handled = true;
    }
    private void OnCombinerInteractUsing(EntityUid uid, GeneCombinerComponent comp, ref InteractUsingEvent args)
    {
        var entToInsert = args.Used;

        var geneContainer = _containerSystem.GetContainer(uid, comp.GeneContainerID);
        var geneContainerEnt = new List<EntityUid>(geneContainer.ContainedEntities);

        if (!HasComp<XenoGeneComponent>(entToInsert) || geneContainerEnt.Count() >= comp.MaxGenes)
            return;

        _containerSystem.Insert(entToInsert, geneContainer);

        args.Handled = true;
    }
    private void AddAltVerb(EntityUid uid, GeneCombinerComponent comp, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var geneContainer = _containerSystem.GetContainer(uid, comp.GeneContainerID);
        var geneContainerEnt = new List<EntityUid>(geneContainer.ContainedEntities);

        if (geneContainerEnt.Count < 2)
            return;

        if (!args.CanAccess || !args.CanInteract || args.Hands == null || !geneContainerEnt.Any())
            return;

        var user = args.User;
        var verb = new AlternativeVerb
        {
            Text = Loc.GetString("gene-combiner-start"),
            Priority = 1,
            Act = () => OnCombinerInteract(uid, comp, user)
        };
        args.Verbs.Add(verb);
    }
    private void OnCombinerInteract(EntityUid uid, GeneCombinerComponent comp, EntityUid user)
    {
        var geneContainer = _containerSystem.GetContainer(uid, comp.GeneContainerID);
        var geneContainerEnt = new List<EntityUid>(geneContainer.ContainedEntities);

        if (geneContainerEnt.Count < 2)
            return;

        var geneProtoID1 = MetaData(geneContainerEnt[0]).EntityPrototype;
        var geneProtoID2 = MetaData(geneContainerEnt[1]).EntityPrototype;

        if (geneProtoID1 == null || geneProtoID2 == null)
            return;

        if (!geneContainerEnt.Any() || geneContainerEnt.Count() > comp.MaxGenes || geneProtoID1 != geneProtoID2)
        {
            _containerSystem.EmptyContainer(geneContainer);
            _audioSystem.PlayPvs(comp.DeclineSound, uid);
            return;
        }

        var geneComp1 = EnsureComp<XenoGeneComponent>(geneContainerEnt[0]);
        var geneComp2 = EnsureComp<XenoGeneComponent>(geneContainerEnt[1]);

        var newGeneFloat = geneComp1.GeneMultiplier + geneComp2.GeneMultiplier;
        if (newGeneFloat > 1f)
            newGeneFloat = 1f;

        var geneProto = MetaData(geneContainerEnt[0]).EntityPrototype;
        if (geneProto == null)
            return;

        var newGeneEnt = Spawn(geneProto.ID, Transform(uid).Coordinates);
        AddComp<GeneWithdrawnComponent>(newGeneEnt);
        var geneComp = EnsureComp<XenoGeneComponent>(newGeneEnt);
        geneComp.GeneMultiplier = newGeneFloat;

        foreach (var e in geneContainerEnt)
        {
            QueueDel(e);
        }

        _audioSystem.PlayPvs(comp.CompleteSound, uid);
        var outputContainer = _containerSystem.GetContainer(uid, comp.GeneContainerIDOutput);
        _containerSystem.Insert(newGeneEnt, outputContainer);
    }
}
