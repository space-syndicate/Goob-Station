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
using  Content.Shared.Containers;
using Content.Shared.Containers.ItemSlots;

namespace Content.Server.Imperial.XenoGenetics;

public sealed class ServerXenoGeneticsSystem : SharedXenoGeneticsSystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<GeneSplicerComponent, GeneInsertingDoAfterEvent>(OnGeneInsert);
        SubscribeLocalEvent<GeneSplicerComponent, GeneWithdrawDoAfterEvent>(OnGeneWithdraw);
        SubscribeLocalEvent<GeneSplicerComponent, UseInHandEvent>(OnSplicerUse);
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
        
        if(!TryComp<ContainerManagerComponent>(args.Target, out var slots))
        {
            _popup.PopupEntity(Loc.GetString("gene-splicer-error"), args.User, PopupType.Small);
            args.Handled = true;
            return;
        }

        var targetContainer = _containerSystem.EnsureContainer<Container>(args.Target.Value, comp.EntityGeneContainerID);
        var geneContainer = _containerSystem.GetContainer(uid, comp.GeneContainerID);

        var genesInstalled = new List<EntityUid>(targetContainer.ContainedEntities);
        if(genesInstalled.Any())
        {
            _popup.PopupEntity(Loc.GetString("gene-splicer-already-modified"), args.User, PopupType.Small);
            args.Handled = true;
            return;
        }

        var toInsert = new List<EntityUid>(geneContainer.ContainedEntities);
        if(!toInsert.Any())
        {
            _popup.PopupEntity(Loc.GetString("gene-splicer-gene-not-inserted"), args.User, PopupType.Small);
            args.Handled = true;
            return;
        }
        var insertedEv = new GeneInsertedEvent(toInsert[0], args.Target.Value);
        RaiseLocalEvent(toInsert[0], ref insertedEv);
        foreach(EntityUid geneUid in toInsert)
        {
            _containerSystem.Insert(geneUid, targetContainer);
        }
        
        args.Handled = true;
    }
    private void OnGeneWithdraw(EntityUid uid, GeneSplicerComponent comp, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if(!TryComp<ContainerManagerComponent>(args.Target, out var slots))
        {
            _popup.PopupEntity(Loc.GetString("gene-splicer-error"), args.User, PopupType.Small);
            args.Handled = true;
            return;
        }

        var targetContainer = _containerSystem.EnsureContainer<Container>(args.Target.Value, comp.EntityGeneContainerID);
        var geneContainer = _containerSystem.GetContainer(uid, comp.GeneContainerID);

        var genesInstalled = new List<EntityUid>(targetContainer.ContainedEntities);
        if(!genesInstalled.Any())
        {
            _popup.PopupEntity(Loc.GetString("gene-splicer-has-no-gene"), args.User, PopupType.Small);
            args.Handled = true;
            return;
        }

        var toInsert = new List<EntityUid>(geneContainer.ContainedEntities);
        if(toInsert.Any())
        {
            _popup.PopupEntity(Loc.GetString("gene-splicer-slot-filled"), args.User, PopupType.Small);
            args.Handled = true;
            return;
        }
        var withdrawEv = new GeneWithdrawnEvent(genesInstalled[0], args.Target.Value);
        RaiseLocalEvent(genesInstalled[0], ref withdrawEv);
        foreach(EntityUid geneUid in genesInstalled)
        {
            _containerSystem.Insert(geneUid, geneContainer);
        }
        
        args.Handled = true;
    }
}