using Robust.Shared.Audio;

namespace Content.Server.Imperial.JanitorsResponseTeam;

[RegisterComponent]
public sealed partial class CallJRTViaCentcomFaxComponent : Component
{
    /// <summary>
    /// How much do need TrashSmall to add to MinAcceptedTrash in the counter for this to be true?
    /// </summary>
    [DataField]
    public int AmountTrashSmall = 1;

    /// <summary>
    /// How much do need TrashMedium to add to MinAcceptedTrash in the counter for this to be true?
    /// </summary>
    [DataField]
    public int AmountTrashMedium = 2;

    /// <summary>
    /// How much do need TrashLarge to add to MinAcceptedTrash in the counter for this to be true?
    /// </summary>
    [DataField]
    public int AmountTrashLarge = 3;


    /// <summary>
    /// The minimum amount of garbage that lies on the grid of the station
    /// </summary>
    [DataField]
    public int MinAmountTrash = 200;

    /// <summary>
    /// Sound override for the announcement.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Announcements/attention.ogg");
}
