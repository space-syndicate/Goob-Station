using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Server.Imperial.ExplosiveProjectile;
/// <summary>
/// НЕ добавляйте его сущностям. Это технический компонент для работы ExplosiveProjectileSystem.
/// </summary>
namespace Content.Server.Imperial.ExplosiveProjectile.Components
{
    //    [AutoGenerateComponentPause]
    [RegisterComponent]
    [Access(typeof(ExplosiveProjectileResultOffSystem))]
    public sealed partial class ExplosiveProjectileResultOffComponent : Component
    {
        // Время до взрыва в случае провала проверки.
        [DataField]
        public TimeSpan CancelTime = TimeSpan.FromSeconds(1);

        // Саунд деактивации.
        [DataField]
        public SoundSpecifier? SoundDeactivate = new SoundPathSpecifier("/Audio/Imperial/SpecialUnits/RCU/canceldetonation.ogg", AudioParams.Default.WithVolume(5).WithLoop(false).WithMaxDistance(15f));
    }
}

