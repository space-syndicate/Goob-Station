using Content.Shared.Imperial.ColorHelper;
using Content.Shared.Imperial.Medieval.Magic.Shaders;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.Imperial.Medieval.Magic.Shaders;


public sealed partial class MedievalParticlesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private static string _shaderId = "MedievalParticles";

    private Dictionary<EntityUid, ShaderInstance> _shaders = new();


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalParticlesComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MedievalParticlesComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<MedievalParticlesComponent, BeforePostShaderRenderEvent>(OnShaderRender);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _shaders.Clear();
    }

    private void OnStartup(EntityUid uid, MedievalParticlesComponent component, ComponentStartup args)
    {
        component.SpawnTime = _timing.CurTime;
        component.Seed = _random.NextFloat();

        _shaders.Add(uid, _prototypeManager.Index<ShaderPrototype>(_shaderId).InstanceUnique());
        SetShader(uid, true);
    }

    private void OnShutdown(EntityUid uid, MedievalParticlesComponent component, ComponentShutdown args)
    {
        _shaders.Remove(uid);

        SetShader(uid, false);
    }

    private void OnShaderRender(EntityUid uid, MedievalParticlesComponent component, BeforePostShaderRenderEvent args)
    {
        _shaders[uid].SetParameter("iTime", (float)(_timing.CurTime - component.SpawnTime).TotalSeconds);
        _shaders[uid].SetParameter("particles", (float)component.ParticlesCount);
        _shaders[uid].SetParameter("startSeed", component.Seed);
        _shaders[uid].SetParameter("speed", component.Speed);
        _shaders[uid].SetParameter("gravity", component.Gravity);

        _shaders[uid].SetParameter("inverted", component.Inverted);
        _shaders[uid].SetParameter("collapseOnMinDistance", component.CollapseOnMinDistance);
        _shaders[uid].SetParameter("disappearOnMinDistance", component.DisappearOnMinDistance);

        _shaders[uid].SetParameter("color", ColorHelper.ToVector3(component.Color));
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
