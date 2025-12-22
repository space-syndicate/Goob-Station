using Content.Client.Vampire.UI;
using Content.Client.Eui;
using JetBrains.Annotations;
using System.Numerics;
using Robust.Client.Graphics;
using Content.Shared.Imperial.Vampire;

namespace Content.Client.Vampire;

[UsedImplicitly]
public sealed class VampireRequestedEui : BaseEui
{
    private SelectingSubgroup? _window;

    public override void Opened()
    {
        _window = new SelectingSubgroup();

        _window.OnAcceptHemomancer += () =>
        {
            SendMessage(new VampireRequestedEuiMessage(1));
            _window?.Close();
        };

        _window.OnAcceptUmbrae += () =>
        {
            SendMessage(new VampireRequestedEuiMessage(2));
            _window?.Close();
        };

        _window.OnAcceptGargantua += () =>
        {
            SendMessage(new VampireRequestedEuiMessage(3));
            _window?.Close();
        };

        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCenteredAt(new Vector2(0.5f, 0.5f));
    }

    public override void Closed()
    {
        _window?.Close();
    }
}
