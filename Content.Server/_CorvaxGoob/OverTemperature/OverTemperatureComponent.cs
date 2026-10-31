using Content.Shared._CorvaxGoob.OverTemperature;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using System.Numerics;

namespace Content.Server._CorvaxGoob.OverTemperature;

[RegisterComponent]
public sealed partial class OverTemperatureComponent : Component
{
    /// <summary>
    /// Порог рабочей температуры, при которой устройство может работать (В кельвинах).
    /// </summary>
    [DataField("threshold")]
    public Vector2 OperationalTemperatureThreshold = new Vector2(213.15f, 473.15f);

    [DataField]
    public OverTemperatureStatus Status = OverTemperatureStatus.Okay;

    [DataField]
    public SoundSpecifier OverTemparatureAlertSound = new SoundPathSpecifier("/Audio/_CorvaxGoob/Machines/alarm_beep.ogg", AudioParams.Default.WithVolume(-10));

    [DataField]
    public SoundSpecifier CritTemparatureAlertSound = new SoundPathSpecifier("/Audio/_EinsteinEngines/Voice/IPC/beep_500.ogg", AudioParams.Default.WithVolume(-10));

    [DataField]
    public TimeSpan NextAlertTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan IntervalAlertTime = TimeSpan.FromSeconds(3);

    [DataField]
    public bool TemperatureThresholdEnabled = true;

    [DataField]
    public bool TemperatureDamageEnabled = true;

    /// <summary>
    /// Урон при статусе "Over"
    /// </summary>
    [DataField]
    public DamageSpecifier MinTemperatureDamage = new()
    {
        DamageDict = new()
        {
            { "Heat", 5 },
        },
        ArmorPenetration = 1
    };
}
