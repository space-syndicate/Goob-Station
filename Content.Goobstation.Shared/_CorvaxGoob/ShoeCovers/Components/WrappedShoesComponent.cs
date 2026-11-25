using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared._CorvaxGoob.ShoeCovers.Components;

// Состояние покрытия на самих ботинках. Клиенту передаётся путь к спрайту, счётчик нужен серверу.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class WrappedShoesComponent : Component
{
    // Distance накапливает путь, а MaxDistance задаёт предел, после которого бахилы рвутся.
    [DataField]
    public float Distance;

    [DataField(required: true)]
    public float MaxDistance;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? UnwrapSound;

    // Идентификатор прототипа мусора. Пустое значение оставляет снятие без создания предмета.
    [DataField, AutoNetworkedField]
    public EntProtoId? UnwrapTrash;

    // Путь к RSI. AutoNetworkedField передаёт его клиенту, когда на обуви появляется покрытие.
    [DataField(required: true), AutoNetworkedField]
    public string OverlaySprite = string.Empty;

    // Запоминаем, добавили ли мы запись выдачи и создали ли сам компонент выдачи. Это нужно при снятии бахил.
    [DataField]
    public bool AddedWearerGrant;

    // Если компонент выдачи уже был у обуви, сохраняем его даже после удаления записи бахил.
    [DataField]
    public bool CreatedGrantComponent;
}
