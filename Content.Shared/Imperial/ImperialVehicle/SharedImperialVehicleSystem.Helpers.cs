using Content.Shared.Imperial.ImperialVehicle.Components;
using Content.Shared.Imperial.ImperialVehicle.Enums;
using Content.Shared.Projectiles;

namespace Content.Shared.Imperial.ImperialVehicle;

public partial class SharedImperialVehicleSystem : EntitySystem
{
    private EntityUid? GetProjectileAttacker(EntityUid entity)
    {
        if (TryComp<ProjectileComponent>(entity, out var projectile))
        {
            return projectile.Shooter;
        }

        return null;
    }

    /// <summary>
    /// Set the draw depth for the sprite.
    /// </summary>
    private void UpdateDrawDepth(EntityUid uid, int drawDepth)
    {
        Appearance.SetData(uid, VehicleVisuals.DrawDepth, drawDepth);
    }

    /// <summary>
    /// Set whether the vehicle's base layer is animating or not.
    /// </summary>
    private void UpdateAutoAnimate(EntityUid uid, bool autoAnimate)
    {
        Appearance.SetData(uid, VehicleVisuals.AutoAnimate, autoAnimate);
    }

    /// <summary>
    /// Depending on which direction the vehicle is facing,
    /// change its draw depth.
    /// </summary>
    private int GetDrawDepth(TransformComponent xform, ImperialVehicleComponent component)
    {
        var vehicleDirection = xform.LocalRotation.GetDir();

        return vehicleDirection switch
        {
            Direction.North => component.NorthOver
                ? (int)DrawDepth.DrawDepth.Doors
                : (int)DrawDepth.DrawDepth.WallMountedItems,
            Direction.South => component.SouthOver
                ? (int)DrawDepth.DrawDepth.Doors
                : (int)DrawDepth.DrawDepth.WallMountedItems,
            Direction.West => component.WestOver
                ? (int)DrawDepth.DrawDepth.Doors
                : (int)DrawDepth.DrawDepth.WallMountedItems,
            Direction.East => component.EastOver
                ? (int)DrawDepth.DrawDepth.Doors
                : (int)DrawDepth.DrawDepth.WallMountedItems,
            _ => (int)DrawDepth.DrawDepth.WallMountedItems
        };
    }
}
