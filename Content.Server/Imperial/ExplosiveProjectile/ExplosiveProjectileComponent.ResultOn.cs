using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Content.Server.Imperial.ExplosiveProjectile;
using Content.Server.Imperial.ExplosiveProjectile.Components;

/// <summary>
/// НЕ добавляйте его сущностям. Это технический компонент для работы ExplosiveProjectileSystem.
/// </summary>
namespace Content.Server.Imperial.ExplosiveProjectile.Components
{
    //    [AutoGenerateComponentPause]
    [RegisterComponent]
    [Access(typeof(ExplosiveProjectileResultOnSystem))]
    public sealed partial class ExplosiveProjectileResultOnComponent : Component
    {
        // Время до взрыва в случае провала проверки.
        [DataField]
        public TimeSpan DetonationTime = TimeSpan.FromSeconds(1);
    }
}

