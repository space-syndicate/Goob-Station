using Content.Shared.DoAfter;

namespace Content.Shared.Imperial.Medical
{
    public sealed class DoAfterCustomHypospray : EntitySystem
    {
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        public void OnStart(EntityUid user, EntityUid item, EntityUid target, CustomHyposprayComponent comp)
        {
            var args = new DoAfterArgs(EntityManager, user, comp.InjectionDelayTime,
                new DoAfterCustomHyposprayEvent(), target)
            {
                Used = item,
                BreakOnMove = true,
                BreakOnDamage = true
            };

            _doAfterSystem.TryStartDoAfter(args);
        }
    }
}
