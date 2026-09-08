using Content.Shared.Decals;

namespace Content.Server.Decals; // чтобы не прописывать using

/// <summary>
///     Вызывается при добавлении декали
///     <see cref="DecalSystem.TryAddDecal"/>.
/// </summary>
[ByRefEvent]
public readonly record struct DecalAddedEvent(EntityUid Grid, uint DecalId, Decal Decal);

/// <summary>
///     Вызывается при удалении декали
///     <see cref="DecalSystem.OnDecalRemoved"/>.
/// </summary>
[ByRefEvent]
public readonly record struct DecalRemovedEvent(EntityUid Grid, uint DecalId, Vector2i ChunkIndices);
