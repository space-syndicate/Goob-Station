// BlackM edit
// Moved to Content.Shared/_BlackM/Weapons/BlackMGunSpreadMath.cs.
// using Content.Shared.Weapons.Ranged.Components;
// 
// namespace Content.Shared.Weapons.Ranged;
// 
// /// <summary>Spread recovery is based on actual shots, never the next-fire cooldown.</summary>
// public static class GunSpreadMath
// {
//     public static double GetNextShotTheta(GunComponent gun, TimeSpan now)
//     {
//         var min = Math.Min(gun.MinAngleModified.Theta, gun.MaxAngleModified.Theta);
//         var max = Math.Max(gun.MinAngleModified.Theta, gun.MaxAngleModified.Theta);
//         var elapsed = Math.Max(0d, (now - gun.LastFire).TotalSeconds);
//         // Clamp recovered spread BEFORE the shot increment. A long idle period
//         // must not swallow the entire first shot's recoil impulse.
//         var recovered = Math.Clamp(gun.CurrentAngle.Theta - gun.AngleDecayModified.Theta * elapsed, min, max);
//         return Math.Clamp(recovered + gun.AngleIncreaseModified.Theta, min, max);
//     }
// }
// BlackM edit end
