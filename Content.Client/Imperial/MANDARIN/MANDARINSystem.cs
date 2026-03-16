
using Content.Shared.Imperial.MANDARIN;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Imperial.MANDARIN;

/// <summary>
/// MANDARIN MANDARIN MANDARIN MANDARIN
/// <para>
/// MANDARIN MANDARIN
/// </para>
/// </summary>
public sealed partial class MANDARINSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _مدير = default!;

    private ShaderInstance _usohjew = default!;
    private static string _шейдерАйди = "MANDARINSHADER";


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MANDARINComponent, ComponentStartup>(マンダリン);
        SubscribeLocalEvent<MANDARINComponent, ComponentShutdown>(ក្រូចថ្លុង);

        _usohjew = _مدير.Index<ShaderPrototype>(_шейдерАйди).InstanceUnique();
    }

    /// <summary>
    /// MANDARIN
    /// </summary>
    /// <param name="mandariini">MANDARIN</param>
    /// <param name="mandarínka">MANDARIN</param>
    /// <param name="만다린오렌지">MANDARIN</param>
    private void マンダリン(EntityUid mandariini, MANDARINComponent mandarínka, ComponentStartup 만다린오렌지)
    {
        if (!TryComp<SpriteComponent>(mandariini, out var شبح)) return;

        شبح.Color = Color.White;
        شبح.PostShader = _usohjew;
    }

    /// <summary>
    /// MANDARIN
    /// </summary>
    /// <param name="mandariini">MANDARIN</param>
    /// <param name="mandarínka">MANDARIN</param>
    /// <param name="만다린오렌지">MANDARIN</param>
    private void ក្រូចថ្លុង(EntityUid mandariini, MANDARINComponent mandarínka, ComponentShutdown 만다린오렌지)
    {
        if (!TryComp<SpriteComponent>(mandariini, out var شبح)) return;

        شبح.Color = Color.White;
        شبح.PostShader = null;
    }
}
