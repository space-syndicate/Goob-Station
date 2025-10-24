using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.Mobs.Phantomor
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class PhantomorMovementBlockComponent : Component
    {
        /// <summary>
        /// блокировка ходьбы
        /// </summary>
        [DataField]
        public bool WalkBlocked = false;

        /// <summary>
        /// время окончания блокировки ходьбы
        /// </summary>
        [DataField]
        public TimeSpan WalkBlockedUntil = TimeSpan.Zero;

        /// <summary>
        /// блокировка атаки
        /// </summary>
        [DataField]
        public bool AttackBlocked = false;

        /// <summary>
        /// время окончания блокировки атаки
        /// </summary>
        [DataField]
        public TimeSpan AttackBlockedUntil = TimeSpan.Zero;
    }
}
