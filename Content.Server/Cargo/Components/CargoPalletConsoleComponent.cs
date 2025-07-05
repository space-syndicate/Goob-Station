using Content.Server.Cargo.Systems;
using Content.Shared.Stacks;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Cargo.Components;

[RegisterComponent]
[Access(typeof(CargoSystem))]
public sealed partial class CargoPalletConsoleComponent : Component
// Imperial Space Pirates: New Horizon; Start
{
    [ViewVariables(VVAccess.ReadWrite), DataField("cashType", customTypeSerializer: typeof(PrototypeIdSerializer<StackPrototype>))]
    public string CashType = "Credit";
    [ViewVariables(VVAccess.ReadWrite), DataField("spawnStack")]
    public bool SpawnStack = false;
}
// Imperial Space Pirates: New Horizon; End
