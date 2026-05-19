using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Content.Shared.Inventory;
using Content.Server.Imperial.ExplosiveProjectile;

/// <summary>
/// Подрывает/разрывает сущность в случае, если на ней в момент попадания не было элемента одежды из "targetInvSlot", обладающего компонентом "PressureProtection".
/// </summary>
namespace Content.Server.Imperial.ExplosiveProjectile.Components
{
    [RegisterComponent]
    [Access(typeof(ExplosiveProjectileSystem))]
    public sealed partial class ExplosiveProjectileComponent : Component
    {
        /// <summary>
        /// Слот, от наличия компонента давления в котором будет определяться действия по отношению к target.
        /// </summary>
        [DataField("targetInvSlot")]
        public string TargetInvSlot = "outerClothing";

        /// <summary>
        /// Звук активации.
        /// </summary>
        [DataField]
        public SoundSpecifier? SoundActivate = new SoundPathSpecifier("/Audio/Imperial/SpecialUnits/RCU/detonation.ogg", AudioParams.Default.WithVolume(5).WithLoop(false).WithMaxDistance(15f));

        // Следущие три филда были спи... взяты у StunOnCollide

        /// <summary>
        /// Рефреш стана при попадании.
        /// </summary>
        [DataField]
        public bool Refresh = true;

        /// <summary>
        /// Следует ли сущности пытаться встать автоматически?
        /// </summary>
        [DataField]
        public bool AutoStand = false;

        /// <summary>
        /// Будет ли сущность терять предмет из рук во время падения?
        /// </summary>
        [DataField]
        public bool Drop = true;

        /// <summary>
        /// Сколько времени будет лежать цель в оглушении, если условия для взрыва будут провалены.
        /// </summary>
        [DataField("knockdownTime")]
        public TimeSpan KnockdownTime = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Время стана.
        /// </summary>
        [DataField("stunParam")]
        public TimeSpan StunParam = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Замедление.
        /// </summary>
        [DataField("slowdownParam")]
        public TimeSpan SlowdownParam = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Скорость ходьбы при оглушении.
        /// </summary>
        [DataField("walkSpeedParam")]
        public float WalkSpeedParam = 1f;

        /// <summary>
        /// Скорость бега при оглушении.
        /// </summary>
        [DataField("runSpeedParam")]
        public float RunSpeedParam = 1f;

        /// <summary>
        /// Фиксчурсы. НЕ ТРОГАТЬ.
        /// </summary>
        [DataField("fixture")] public string CheckedFixtureID = "projectile";
    }
}

