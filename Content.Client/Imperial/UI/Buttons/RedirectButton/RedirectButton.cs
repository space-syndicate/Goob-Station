using Robust.Client.UserInterface;

namespace Content.Client.Imperial.UI;


[Virtual]
public class RedirectButton : BaseImperialButton
{
    [Dependency] private readonly IUriOpener _uriOpener = default!;

    [ViewVariables]
    public string? Href { get; set; }


    public RedirectButton()
    {
        IoCManager.InjectDependencies(this);

        OnPressed += (_) => _uriOpener.OpenUri(Href ?? "");
    }
}
