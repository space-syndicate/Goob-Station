using Robust.Client.UserInterface;

namespace Content.Client.Imperial.UI;


public sealed class LockRequirementsOverride(HashSet<string>? lockStriepeClasses = null, Control? label = null, Color? lockStripeColor = null)
{
    public Control? Label = label;
    public Color? LockStripeColor = lockStripeColor;
    public HashSet<string>? LockStriepeClasses = lockStriepeClasses;
};
