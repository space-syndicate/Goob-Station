using Content.Shared.Imperial.XenoGenetics;
using Robust.Shared.Random;
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

public class ServerXenoGeneticsSystem : SharedXenoGeneticsSystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<XenoGeneComponent, ComponentStartup>(OnXenoGeneStartup);
        
        SubscribeLocalEvent<GeneSplicerComponent, GeneInsertingDoAfterEvent>(OnGeneInsert);
        SubscribeLocalEvent<GeneSplicerComponent, GeneWithdrawDoAfterEvent>(OnGeneWithdraw);
        SubscribeLocalEvent<GeneSplicerComponent, UseInHandEvent>(OnSplicerUse);
    }

    private void OnXenoGeneStartup(EntityUid uid, XenoGeneComponent component, ComponentStartup args)
    {
        if(component.randomizeGeneQuality)
        {
            float multiplier;
            int quality = _rand.Next(0, 10);
            switch(quality)
            {
                case <= 2:
                    multiplier = _rand.Next(1, 200) / 1000f;
                    break;
                case > 2 and <= 7:
                    multiplier = _rand.Next(200, 600) / 1000f;
                    break;
                case > 7 and <= 9:
                    multiplier = _rand.Next(600, 900) / 1000f;
                    break;
                case > 9 and <= 10:
                    multiplier = _rand.Next(900, 1200) / 1000f;
                    break;
                default:
                    multiplier = 0.1f;
                    break;
            }   

            component.geneMultiplier = multiplier;
        }
        
    }

    private void OnSplicerUse(EntityUid uid, GeneSplicerComponent component, UseInHandEvent args)
    {
        switch (component.InsertMode)
        {
            case GeneSplicerMode.Insert:
            component.InsertMode = GeneSplicerMode.Withdraw;
            _popup.PopupEntity("Текущий режим: извлечение", args.User, PopupType.Small);
            break;

            case GeneSplicerMode.Withdraw:
            component.InsertMode = GeneSplicerMode.Insert;
            _popup.PopupEntity("Текущий режим: ввод", args.User, PopupType.Small);
            break;

            default:
            break;
        }
        args.Handled = true;
    }
    protected virtual void OnGeneInsert(EntityUid uid, GeneSplicerComponent comp, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;
        
        if(!TryComp<ContainerManagerComponent>(args.Target, out var slots))
        {
            _popup.PopupEntity("Ошибка ввода!", args.User, PopupType.Small);
            args.Handled = true;
            return;
        }

        var targetContainer = _containerSystem.EnsureContainer<Container>(args.Target.Value, comp.entityGeneContainerID);
        var geneContainer = _containerSystem.GetContainer(uid, comp.geneContainerID);

        var genesInstalled = new List<EntityUid>(targetContainer.ContainedEntities);
        if(genesInstalled.Any())
        {
            _popup.PopupEntity("У цели уже имеются генные модификации.", args.User, PopupType.Small);
            args.Handled = true;
            return;
        }

        var toInsert = new List<EntityUid>(geneContainer.ContainedEntities);
        if(!toInsert.Any())
        {
            _popup.PopupEntity("Отсутствует образец для ввода.", args.User, PopupType.Small);
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
    protected virtual void OnGeneWithdraw(EntityUid uid, GeneSplicerComponent comp, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if(!TryComp<ContainerManagerComponent>(args.Target, out var slots))
        {
            _popup.PopupEntity("Ошибка извлечения!", args.User, PopupType.Small);
            args.Handled = true;
            return;
        }

        var targetContainer = _containerSystem.EnsureContainer<Container>(args.Target.Value, comp.entityGeneContainerID);
        var geneContainer = _containerSystem.GetContainer(uid, comp.geneContainerID);

        var genesInstalled = new List<EntityUid>(targetContainer.ContainedEntities);
        if(!genesInstalled.Any())
        {
            _popup.PopupEntity("У цели нет генных модификаций.", args.User, PopupType.Small);
            args.Handled = true;
            return;
        }

        var toInsert = new List<EntityUid>(geneContainer.ContainedEntities);
        if(toInsert.Any())
        {
            _popup.PopupEntity("Освободите слот для генного материала спайщика.", args.User, PopupType.Small);
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