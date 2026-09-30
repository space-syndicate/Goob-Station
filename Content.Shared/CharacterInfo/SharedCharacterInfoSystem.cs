// SPDX-License-Identifier: MIT

using Content.Shared.Objectives;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.CharacterInfo;

[Serializable, NetSerializable]
public sealed class RequestCharacterInfoEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;

    public RequestCharacterInfoEvent(NetEntity netEntity)
    {
        NetEntity = netEntity;
    }
}

[Serializable, NetSerializable]
public sealed class CharacterInfoEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;
    public readonly ProtoId<JobPrototype>? JobProto; // CorvaxGoob
    public readonly Dictionary<string, List<ObjectiveInfo>> Objectives;
    public readonly string? Briefing;

    public CharacterInfoEvent(NetEntity netEntity, ProtoId<JobPrototype>? jobProto, Dictionary<string, List<ObjectiveInfo>> objectives, string? briefing) // CorvaxGoob
    {
        NetEntity = netEntity;
        JobProto = jobProto; // CorvaxGoob
        Objectives = objectives;
        Briefing = briefing;
    }
}
