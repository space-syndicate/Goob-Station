using Content.Shared._Shitmed.Autodoc.Components;
using Content.Shared._Shitmed.Medical.Surgery;
using Content.Shared._Shitmed.Medical.Surgery.Conditions;
using Content.Shared._Shitmed.Medical.Surgery.Steps;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;

namespace Content.Shared._Shitmed.Autodoc.Systems;

/// <summary>
/// Завершает проблемные операции автодока и останавливает выполненные повторяемые шаги.
/// </summary>
public sealed class AutodocSurgeryCompletionSystem : EntitySystem
{
    [Dependency] private readonly SharedSurgerySystem _surgery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AutodocComponent, SurgeryStepEvent>(OnSurgeryStep);
        SubscribeLocalEvent<BodyComponent, SurgeryDoAfterEvent>(OnSurgeryDoAfter, after: [typeof(SharedSurgerySystem)]);
    }

    private void OnSurgeryStep(Entity<AutodocComponent> ent, ref SurgeryStepEvent args)
    {
        if (!TryComp<ActiveAutodocComponent>(ent, out var active) ||
            active.CurrentSurgery is not { } current)
        {
            return;
        }

        var (body, part, surgeryId) = current;

        // Учитываем только последний шаг текущей корневой операции автодока
        if (body != args.Body || part != args.Part ||
            _surgery.GetSingleton(surgeryId) != args.Surgery ||
            !TryComp(args.Surgery, out SurgeryComponent? surgery) ||
            surgery.Steps.Count == 0 ||
            _surgery.GetSingleton(surgery.Steps[^1]) != args.Step)
        {
            return;
        }

        // Не вмешиваемся в лечение костей, органов и ран и прочего
        var removesPart = HasComp<SurgeryRemovePartStepComponent>(args.Step);
        var removesOrgan = HasComp<SurgeryRemoveOrganStepComponent>(args.Step);
        var attachesPart = HasComp<SurgeryAffixPartStepComponent>(args.Step) &&
                           HasComp<SurgeryPartRemovedConditionComponent>(args.Surgery);
        var attachesOrgan = HasComp<SurgeryAffixOrganStepComponent>(args.Step) &&
                            TryComp(args.Surgery, out SurgeryOrganConditionComponent? organCondition) &&
                            organCondition.Reattaching;

        if (!removesPart && !removesOrgan && !attachesPart && !attachesOrgan)
        {
            return;
        }

        // После ампутации обычная проверка шага ненадежна, поскольку целевая часть уже отсоединена
        if (removesPart)
        {
            if (TryComp(part, out BodyPartComponent? partComponent) &&
                partComponent.Body == body)
            {
                return;
            }
        }
        else if (!IsStepComplete(body, part, args.Surgery, args.Step))
        {
            return;
        }

        active.Waiting = false;
        active.CurrentSurgery = null;
        active.ProgramStep++;
    }

    private void OnSurgeryDoAfter(Entity<BodyComponent> ent, ref SurgeryDoAfterEvent args)
    {
        // Останавливаем повторение, если шаг активного автодока завершился после применения эффекта
        if (args.Cancelled || !args.Repeat ||
            args.Target is not { } part ||
            !TryComp<ActiveAutodocComponent>(args.User, out var active) ||
            active.CurrentSurgery is not { } current)
        {
            return;
        }

        var (body, currentPart, _) = current;

        if (body != ent.Owner || currentPart != part ||
            _surgery.GetSingleton(args.Surgery) is not { } surgery ||
            _surgery.GetSingleton(args.Step) is not { } step ||
            !IsStepComplete(body, part, surgery, step))
        {
            return;
        }

        args.Repeat = false;
        active.Waiting = false;
    }

    private bool IsStepComplete(EntityUid body, EntityUid part, EntityUid surgery, EntityUid step)
    {
        var check = new SurgeryStepCompleteCheckEvent(body, part, surgery);
        RaiseLocalEvent(step, ref check);
        return !check.Cancelled;
    }
}
