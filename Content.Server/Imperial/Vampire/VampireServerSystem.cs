using Content.Server.Store.Systems;
using Content.Shared.Imperial.Vampire;
using Content.Shared.Store;

namespace Content.Server.Imperial.Vampire
{
    public sealed class VampireSystem : EntitySystem
    {
        [Dependency] private readonly StoreSystem _storeSystem = default!;
        [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<VampireGrimoireEvent>(OnGrimoireRequest);
        }

        private void OnGrimoireRequest(VampireGrimoireEvent args)
        {
            var performer = args.Performer;

            if (!TryComp<VampireComponent>(performer, out var comp) || comp.GrimoreActionEntity == null)
                return;

            var storeEntity = comp.GrimoreActionEntity.Value;

            _storeSystem.DisableRefund(performer);
            _storeSystem.UpdateUserInterface(performer, storeEntity);

            _uiSystem.TryToggleUi(storeEntity, StoreUiKey.Key, performer);
        }
    }
}
