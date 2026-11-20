using System.Linq;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._BlackM.Lobby.Terminal;

public static class TerminalStyler
{
    private const string Marker = "BlackMTerminalStyled";

    public static void ApplyTree(Control root, bool aggressive = false)
    {
        ApplyOne(root, aggressive);
        Apply(root, aggressive);
    }

    public static void Apply(Control root, bool aggressive = false)
    {
        foreach (var child in root.Children)
        {
            ApplyOne(child, aggressive);
            Apply(child, aggressive);
        }
    }

    private static void ApplyOne(Control control, bool aggressive)
    {
        if (aggressive && control is Button)
        {
            Enforce(control, StyleNano.StyleClassButtonTerminalBlackM);
            return;
        }

        if (control.HasStyleClass(Marker))
            return;

        var type = control.GetType();
        var plain = !HasForeign(control, null);
        control.AddStyleClass(Marker);

        if (control is Button)
        {
            if (plain)
                control.AddStyleClass(StyleNano.StyleClassButtonTerminalBlackM);
        }
        else if (type == typeof(Label))
        {
            if (!plain && !aggressive)
                return;
            if (!plain)
                Strip(control, null);
            control.AddStyleClass(StyleNano.StyleClassLabelTerminalBlackM);
        }
        else if (type == typeof(OptionButton))
        {
            if (!plain && !aggressive)
                return;
            if (!plain)
                Strip(control, null);
            control.AddStyleClass(StyleNano.StyleClassJobButtonTerminalBlackM);
        }
        else if (type == typeof(Slider))
        {
            control.AddStyleClass(StyleNano.StyleClassSliderTerminalBlackM);
        }
        else if (type == typeof(LineEdit))
        {
            control.AddStyleClass(StyleNano.StyleClassLineEditTerminalBlackM);
        }
    }

    private static void Enforce(Control control, string terminalClass)
    {
        if (HasForeign(control, terminalClass))
            Strip(control, terminalClass);

        if (!control.HasStyleClass(terminalClass))
            control.AddStyleClass(terminalClass);
    }

    private static bool IsAllowed(string cls, string? keep)
    {
        return cls == ContainerButton.StyleClassButton
               || cls == Marker
               || cls == keep
               || cls == StyleBase.ButtonOpenLeft
               || cls == StyleBase.ButtonOpenRight
               || cls == StyleBase.ButtonOpenBoth;
    }

    private static bool HasForeign(Control control, string? keep)
    {
        foreach (var cls in control.StyleClasses)
        {
            if (!IsAllowed(cls, keep))
                return true;
        }

        return false;
    }

    private static void Strip(Control control, string? keep)
    {
        foreach (var cls in control.StyleClasses.ToArray())
        {
            // The structural Open* classes are removed as well: they glue buttons together in Nano.
            var structural = cls == StyleBase.ButtonOpenLeft
                             || cls == StyleBase.ButtonOpenRight
                             || cls == StyleBase.ButtonOpenBoth;

            if (structural || !IsAllowed(cls, keep))
                control.RemoveStyleClass(cls);
        }
    }
}
