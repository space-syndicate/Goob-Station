using Robust.Shared.Audio;

namespace Content.Server.Imperial.EnergyCore.Components
{
    /// <summary>
    /// Не касайтесь термоядерных ядер. Это очень вредно для вашего здоровья.
    /// </summary>
    [RegisterComponent]
    public sealed partial class CoreCollisionComponent : Component
    {
        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public int EntitiesDeleted = 0;

        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public bool Deletions = false;

        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public SoundSpecifier DelitionSound = new SoundPathSpecifier("/Audio/Effects/gib3.ogg");
    }
}
