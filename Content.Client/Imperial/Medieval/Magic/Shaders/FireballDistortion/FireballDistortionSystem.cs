using Content.Shared.Imperial.ColorHelper;
using Content.Shared.Imperial.Medieval.Magic.Shaders;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Imperial.Medieval.Magic.Shaders;


public sealed partial class FireballDistortionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private static string _shaderId = "FireballDistortion";

    private Dictionary<EntityUid, ShaderInstance> _shaders = new();


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FireballDistortionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<FireballDistortionComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<FireballDistortionComponent, BeforePostShaderRenderEvent>(OnShaderRender);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _shaders.Clear();
    }

    private void OnStartup(EntityUid uid, FireballDistortionComponent component, ComponentStartup args)
    {
        _shaders.Add(
            uid,
            _prototypeManager.Index<ShaderPrototype>(_shaderId).InstanceUnique()
        );

        SetShader(uid, true);
    }

    private void OnShutdown(EntityUid uid, FireballDistortionComponent component, ComponentShutdown args)
    {
        _shaders.Remove(uid);

        SetShader(uid, false);
    }

    private void OnShaderRender(EntityUid uid, FireballDistortionComponent component, BeforePostShaderRenderEvent args)
    {
        _shaders[uid].SetParameter("fire_color", ColorHelper.ToVector3(component.FireColor));
        _shaders[uid].SetParameter("fire_edge_color", ColorHelper.ToVector3(component.FireEdgeColor));

        _shaders[uid].SetParameter("start_point", component.StartPoint);
        _shaders[uid].SetParameter("fireball_size", component.FireballScale);
        _shaders[uid].SetParameter("fire_intensity", component.FireIntensity);
        _shaders[uid].SetParameter("fire_power_factor", component.FirePowerFactor);

        _shaders[uid].SetParameter("rotation", (float)Transform(uid).LocalRotation.Theta);
    }

    #region Helpers

    private void SetShader(EntityUid uid, bool enabled, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite, false))
            return;

        sprite.Color = Color.White;
        sprite.PostShader = enabled ? _shaders[uid] : null;
        sprite.RaiseShaderEvent = enabled;
    }

    #endregion
}
