using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.ShoeCovers.Components;

[RegisterComponent]
public sealed partial class ShoeCoversWrapComponent : Component
{
    [DataField] public TimeSpan WrapDelay = TimeSpan.FromSeconds(1);
    [DataField] public SoundSpecifier? WrapSound;
    [DataField] public EntityWhitelist? Whitelist;
    [DataField] public EntityWhitelist? Blacklist;
    // Этот список загружается из addedComponents в YAML и добавляется к ботинкам после завершения действия.
    [DataField(required: true)] public ComponentRegistry AddedComponents = new();
}

// Состояние покрытия на самих ботинках. Клиенту передаётся путь к спрайту, счётчик нужен серверу.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class WrappedShoesComponent : Component
{
    // Distance накапливает путь, а MaxDistance задаёт предел, после которого бахилы рвутся.
    [DataField] public float Distance;
    [DataField(required: true)] public float MaxDistance;
    [DataField] public SoundSpecifier? UnwrapSound;
    // Идентификатор прототипа мусора. Пустое значение оставляет снятие без создания предмета.
    [DataField] public EntProtoId? UnwrapTrash;

    // Путь к RSI. AutoNetworkedField передаёт его клиенту, когда на обуви появляется покрытие.
    [DataField(required: true), AutoNetworkedField]
    public string OverlaySprite = string.Empty;

    // Запоминаем, добавили ли мы запись выдачи и создали ли сам компонент выдачи. Это нужно при снятии бахил.
    [DataField] public bool AddedWearerGrant;
    // Если компонент выдачи уже был у обуви, сохраняем его даже после удаления записи бахил.
    [DataField] public bool CreatedGrantComponent;
}

// Маркер персонажа в бахилах. По нему обрабатываются движение и попытки оставить след.
[RegisterComponent]
public sealed partial class ShoeCoversWearerComponent : Component;
