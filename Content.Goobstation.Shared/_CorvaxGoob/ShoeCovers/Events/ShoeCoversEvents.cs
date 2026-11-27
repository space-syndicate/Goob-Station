using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared._CorvaxGoob.ShoeCovers.Events;

// Событие завершения надевания бахил. Его получает предмет, которым покрывают обувь.
// DoAfter возвращает в этом событии пользователя, цель и признак отмены. Система проверяет их перед применением.
[Serializable, NetSerializable]
public sealed partial class WrappedShoesDoAfterEvent : SimpleDoAfterEvent;
