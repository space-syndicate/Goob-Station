// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers.ItemSlots;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.CardPdaPainter;

[RegisterComponent]
public sealed partial class CardPdaPainterComponent : Component
{
    public const string TargetSlotId = "CardPdaPainter-target";

    /// <summary>
    /// Holds the ID card or PDA being recolored. Only the item in this slot is affected.
    /// </summary>
    [DataField]
    public ItemSlot TargetSlot = new();

    /// <summary>
    /// Played after a valid target receives a new visual style.
    /// </summary>
    [DataField]
    public SoundSpecifier PaintSound = new SoundPathSpecifier("/Audio/Effects/spray2.ogg");
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CardPdaVisualOverrideComponent : Component
{
    /// <summary>
    /// Entity prototype used as the visual template for this card or PDA.
    /// This does not change ID data, access, job title, or the ID card stored inside a PDA.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? VisualPrototype;
}

[Serializable, NetSerializable]
public enum CardPdaPainterUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum CardPdaPainterTargetType : byte
{
    None,
    IdCard,
    Pda
}

[Serializable, NetSerializable]
public readonly record struct CardPdaPainterJobEntry(
    ProtoId<JobPrototype> JobId,
    EntProtoId VisualPrototype);

/// <summary>
/// Data sent from the server to the painter window.
/// The client only displays this data; the server re-checks everything again when repainting.
/// </summary>
[Serializable, NetSerializable]
public sealed class CardPdaPainterBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly bool IsTargetPresent;
    public readonly string TargetName;
    public readonly CardPdaPainterTargetType TargetType;
    public readonly List<CardPdaPainterJobEntry> Jobs;

    public CardPdaPainterBoundUserInterfaceState(
        bool isTargetPresent,
        string targetName,
        CardPdaPainterTargetType targetType,
        List<CardPdaPainterJobEntry> jobs)
    {
        IsTargetPresent = isTargetPresent;
        TargetName = targetName;
        TargetType = targetType;
        Jobs = jobs;
    }
}

/// <summary>
/// Sent when the player presses the repaint button for a selected job style.
/// Only the job ID is sent; the server chooses the actual ID-card or PDA visual prototype.
/// </summary>
[Serializable, NetSerializable]
public sealed class CardPdaPainterRepaintMessage : BoundUserInterfaceMessage
{
    public readonly ProtoId<JobPrototype> JobId;

    public CardPdaPainterRepaintMessage(ProtoId<JobPrototype> jobId)
    {
        JobId = jobId;
    }
}
