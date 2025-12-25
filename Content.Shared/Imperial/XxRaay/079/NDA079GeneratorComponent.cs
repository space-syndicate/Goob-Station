using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.XxRaay.Nda079;

/// <summary>
/// Маркерный компонент для генераторов нда 079
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NDA079GeneratorComponent : Component
{
    /// <summary>
    /// Бонус к пассивной регенерации энергии NDA079 за каждый подключенный генератор
    /// </summary>
    [DataField]
    public float RegenBonusPerGenerator = 0.4f;
}
