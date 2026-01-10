using System.Numerics;
using Content.Client.Imperial.Roadmap.UI;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Imperial.UI;


[Virtual]
public class ImperialCheckbox : BoxContainer
{
    [Dependency] private readonly IEntityManager _entityManager = default!;


    public event Action<bool>? OnCheckboxToggled;

    public BorderedPanelContainer CheckboxButtonContainer;
    public TextureButton CheckboxButton;
    public Label TextLabel;

    private string _texturePath = "/Textures/Imperial/Interface/Style/RoundedCheckbox/check_box_rounded.svg.96dpi.png";
    private bool _toggled = false;

    [ViewVariables]
    public string TexturePath
    {
        get => _texturePath;
        set
        {
            _texturePath = value;

            if (_toggled) CheckboxButton.TextureNormal = GetTextureFromPath(value);
        }
    }

    [ViewVariables]
    public string? LabelText { get => TextLabel.Text; set => TextLabel.Text = value; }

    [ViewVariables]
    public bool Toggled { get => _toggled; set => OnCheckboxPressed(value); }

    [ViewVariables]
    public float BorderRadius { get => CheckboxButtonContainer.BorderRadius; set => CheckboxButtonContainer.BorderRadius = value; }

    [ViewVariables]
    public Color BackgroundColor { get => CheckboxButtonContainer.BackgroundPanelColor; set => CheckboxButtonContainer.BackgroundPanelColor = value; }

    public ImperialCheckbox()
    {
        IoCManager.InjectDependencies(this);

        CheckboxButtonContainer = new BorderedPanelContainer()
        {
            BackgroundPanelColor = StyleImperial.ImperialDark,
            MaxSize = new Vector2(20, 20),
            SetSize = new Vector2(20, 20),
            VerticalAlignment = VAlignment.Center
        };

        CheckboxButton = new TextureButton()
        {
            VerticalAlignment = VAlignment.Stretch,
            HorizontalAlignment = HAlignment.Stretch,
            MaxSize = new Vector2(15, 15)
        };

        TextLabel = new Label()
        {
            Margin = new Thickness(15, 0, 0, 0),
            Align = Label.AlignMode.Center
        };

        Orientation = LayoutOrientation.Horizontal;
        HorizontalExpand = true;

        CheckboxButtonContainer.AddChild(CheckboxButton);
        AddChild(CheckboxButtonContainer);
        AddChild(TextLabel);

        CheckboxButton.OnPressed += (_) => OnCheckboxPressed();
    }

    private void OnCheckboxPressed(bool? toggled = null)
    {
        SetCheckboxWithoutEvent(toggled ?? !_toggled);

        OnCheckboxToggled?.Invoke(_toggled);
    }

    #region Helpers

    private Texture GetTextureFromPath(string path) => _entityManager.System<SpriteSystem>().Frame0(new SpriteSpecifier.Texture(new(path)));

    #endregion

    #region Public API

    public void SetCheckboxWithoutEvent(bool toggled)
    {
        _toggled = toggled;
        CheckboxButton.TextureNormal = _toggled ? GetTextureFromPath(_texturePath) : null;
    }

    #endregion
}
