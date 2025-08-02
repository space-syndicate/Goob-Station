using Robust.Shared.GameObjects;
using Robust.Shared.Audio;

namespace Content.Server.Imperial.Power.Components
{
    [RegisterComponent]
    public sealed partial class SupermatterTouchComponent : Component
    {
        // Маркер
        // Цвет вспышки при контакте
        [DataField]
        public Color FlashColor = new(1f, 0f, 0f, 0.8f);
        // Звук гиба при уничтожении моба
        [DataField]
        public SoundSpecifier GibSound = new SoundCollectionSpecifier("gib");
        // Имя прототипа сущности пепла
        [DataField]
        public string AshPrototype = "Ash";
    }
}
