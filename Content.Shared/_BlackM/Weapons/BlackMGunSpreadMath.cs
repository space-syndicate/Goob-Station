using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared._BlackM.Weapons;

public static class BlackMGunSpreadMath
{
    public static float GetGripMultiplier(bool oneHanded) => oneHanded ? 2f : 1f;

    public static double GetNextShotTheta(GunComponent gun, TimeSpan now)
    {
        var min = Math.Min(gun.MinAngleModified.Theta, gun.MaxAngleModified.Theta);
        var max = Math.Max(gun.MinAngleModified.Theta, gun.MaxAngleModified.Theta);
        var elapsed = Math.Max(0d, (now - gun.LastFire).TotalSeconds);
        var recovered = Math.Clamp(gun.CurrentAngle.Theta - gun.AngleDecayModified.Theta * elapsed, min, max);
        return Math.Clamp(recovered + gun.AngleIncreaseModified.Theta, min, max);
    }
}
