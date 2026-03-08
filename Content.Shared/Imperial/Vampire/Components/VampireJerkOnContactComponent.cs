using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.Vampire;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VampireJerkOnContactComponent : Component
{
    [DataField]
    public TimeSpan Knockdown;

    [DataField]
    public int Damage;

    [DataField]
    public string DamageType = "Blunt";

    /// <summary>
    /// задержка перед удалением компонента, чтобы обработать все столкновения
    /// </summary>
    [DataField("delayDeletion")]
    public TimeSpan DelayDeletion = TimeSpan.FromSeconds(0.5f);

    [AutoNetworkedField]
    public TimeSpan DeletionTime;
}
