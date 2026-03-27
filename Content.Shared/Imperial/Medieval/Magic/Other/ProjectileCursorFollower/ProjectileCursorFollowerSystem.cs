using System.Numerics;
using Content.Shared.Imperial.ColorHelper;
using Content.Shared.Imperial.MouseInput;
using Content.Shared.Imperial.MouseInput.Events;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

namespace Content.Shared.Imperial.Medieval.Magic.ProjectileCursorFollower;



public sealed partial class ProjectileCursorFollowerSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedMouseInputSystem _mouseInputSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileCursorFollowerComponent, ComponentStartup>(OnStartup);

        SubscribeLocalEvent<ProjectileCursorFollowerComponent, MousePositionRefreshEvent>(OnMousePositionChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<ProjectileCursorFollowerComponent, ProjectileComponent>();

        while (enumerator.MoveNext(out var uid, out var component, out var projectileComponent))
        {
            if (projectileComponent.Shooter == null) continue;
            if (!HasComp<ActorComponent>(projectileComponent.Shooter)) continue;

            _mouseInputSystem.CreateMousePositionRequest(projectileComponent.Shooter.Value, uid);
        }
    }

    private void OnStartup(EntityUid uid, ProjectileCursorFollowerComponent component, ComponentStartup args)
    {
    }

    private void OnMousePositionChanged(EntityUid uid, ProjectileCursorFollowerComponent component, MousePositionRefreshEvent args)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physicsComponent)) return;

        var grid = _transformSystem.GetGrid(uid);
        var gridRotation = grid.HasValue ? _transformSystem.GetWorldRotation(grid.Value) : Angle.Zero;

        var projectilePosition = _transformSystem.GetWorldPosition(uid);
        var cursorPosition = _transformSystem.ToMapCoordinates(args.MousePosition).Position;

        var direction = cursorPosition - projectilePosition;
        var impulse = direction * component.LinearVelocityIntensy;
        var rotation = direction.ToAngle() - component.RelativeAngle - gridRotation;

        var resultVelocity = physicsComponent.LinearVelocity + impulse * physicsComponent.InvMass;

        resultVelocity = resultVelocity.Length() > component.MaxLinearVelocityIntensy
            ? direction.Normalized() * new Vector2(component.MaxLinearVelocityIntensy)
            : resultVelocity;

        _transformSystem.SetLocalRotation(uid, rotation);
        _physicsSystem.SetLinearVelocity(uid, resultVelocity, body: physicsComponent);
    }
}
