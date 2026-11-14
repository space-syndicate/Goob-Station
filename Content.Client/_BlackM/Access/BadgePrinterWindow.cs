using System;
using System.Collections.Generic;
using System.Text;
using Content.Shared._BlackM.Access;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client._BlackM.Access;

public sealed class BadgePrinterWindow : DefaultWindow
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    public event Action<List<string>>? OnPrintPressed;
    public event Action? OnEjectPressed;
    public event Action? OnEjectPassportPressed;
    public event Action? OnReprintPassportPressed;

    private readonly Label _cardStatusLabel;
    private readonly Label _passportStatusLabel;
    private readonly BoxContainer _badgeList;
    private readonly Button _printButton;
    private readonly Button _ejectButton;
    private readonly Button _ejectPassportButton;
    private readonly Button _reprintPassportButton;

    private readonly Dictionary<string, CheckBox> _checkboxes = new();

    private static readonly Color AccentColor = Color.FromHex("#5fa8d3");
    private static readonly Color PanelBg = Color.FromHex("#1b1f24");
    private static readonly Color PanelBgLight = Color.FromHex("#242a31");

    public BadgePrinterWindow()
    {
        IoCManager.InjectDependencies(this);

        MinSize = new Vector2i(560, 560);
        SetSize = new Vector2i(600, 640);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 6,
        };

        var headerPanel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = PanelBgLight,
                BorderColor = AccentColor,
                BorderThickness = new Thickness(0, 0, 0, 2),
                ContentMarginLeftOverride = 10,
                ContentMarginRightOverride = 10,
                ContentMarginTopOverride = 8,
                ContentMarginBottomOverride = 8,
            }
        };

        var headerBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 8,
        };

        var icon = new TextureRect
        {
            TextureScale = new Vector2(2f, 2f),
            VerticalAlignment = VAlignment.Center,
            Texture = LoadIcon("_BlackM/Objects/Misc/access_card_holder.rsi", "icon"),
        };

        var infoBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
            MinWidth = 0,
        };

        _cardStatusLabel = new Label
        {
            Text = Loc.GetString("badge-printer-no-card"),
            FontColorOverride = Color.Gray,
            ClipText = true,
        };

        infoBox.AddChild(_cardStatusLabel);

        _ejectButton = new Button
        {
            Text = Loc.GetString("badge-printer-eject"),
            VerticalAlignment = VAlignment.Center,
            HorizontalExpand = false,
            MinWidth = 96,
            Disabled = true,
        };
        _ejectButton.OnPressed += _ => OnEjectPressed?.Invoke();

        headerBox.AddChild(icon);
        headerBox.AddChild(infoBox);
        headerBox.AddChild(_ejectButton);
        headerPanel.AddChild(headerBox);

        var passportPanel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = PanelBgLight,
                ContentMarginLeftOverride = 10,
                ContentMarginRightOverride = 10,
                ContentMarginTopOverride = 8,
                ContentMarginBottomOverride = 8,
            }
        };

        var passportBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 8,
        };

        var passportIcon = new TextureRect
        {
            TextureScale = new Vector2(2f, 2f),
            VerticalAlignment = VAlignment.Center,
            Texture = LoadIcon("_BlackM/Objects/Misc/passport.rsi", "icon"),
        };

        _passportStatusLabel = new Label
        {
            Text = Loc.GetString("badge-printer-no-passport"),
            FontColorOverride = Color.Gray,
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
            ClipText = true,
        };

        _reprintPassportButton = new Button
        {
            Text = Loc.GetString("badge-printer-reprint-passport-button"),
            VerticalAlignment = VAlignment.Center,
            HorizontalExpand = false,
            MinWidth = 140,
            Disabled = true,
        };
        _reprintPassportButton.OnPressed += _ => OnReprintPassportPressed?.Invoke();

        _ejectPassportButton = new Button
        {
            Text = Loc.GetString("badge-printer-eject"),
            VerticalAlignment = VAlignment.Center,
            HorizontalExpand = false,
            MinWidth = 96,
            Disabled = true,
        };
        _ejectPassportButton.OnPressed += _ => OnEjectPassportPressed?.Invoke();

        passportBox.AddChild(passportIcon);
        passportBox.AddChild(_passportStatusLabel);
        passportBox.AddChild(_reprintPassportButton);
        passportBox.AddChild(_ejectPassportButton);
        passportPanel.AddChild(passportBox);

        var listPanel = new PanelContainer
        {
            VerticalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = PanelBg,
                ContentMarginLeftOverride = 6,
                ContentMarginRightOverride = 6,
                ContentMarginTopOverride = 6,
                ContentMarginBottomOverride = 6,
            }
        };

        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
        };

        _badgeList = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 8,
        };

        scroll.AddChild(_badgeList);
        listPanel.AddChild(scroll);

        _printButton = new Button
        {
            Text = Loc.GetString("badge-printer-print-button"),
            HorizontalExpand = true,
        };
        _printButton.OnPressed += _ =>
        {
            var selected = new List<string>();
            foreach (var (protoId, box) in _checkboxes)
            {
                if (box.Pressed)
                    selected.Add(protoId);
            }

            OnPrintPressed?.Invoke(selected);
        };

        root.AddChild(headerPanel);
        root.AddChild(passportPanel);
        root.AddChild(listPanel);
        root.AddChild(_printButton);

        Contents.AddChild(root);
    }

    public void UpdateState(BadgePrinterBuiState state)
    {
        _cardStatusLabel.Text = state.HasCard
            ? Loc.GetString("badge-printer-card-inserted")
            : Loc.GetString("badge-printer-no-card");
        _cardStatusLabel.FontColorOverride = state.HasCard ? AccentColor : Color.Gray;

        _ejectButton.Disabled = !state.HasCard;
        _printButton.Disabled = !state.HasCard || !state.HasPermit;
        _printButton.ToolTip = state.HasPermit
            ? null
            : Loc.GetString("badge-printer-no-permit");

        if (state.HasPassport)
        {
            _passportStatusLabel.Text = Loc.GetString("badge-printer-passport-inserted", ("name", state.PassportOwnerName ?? string.Empty));
            _passportStatusLabel.FontColorOverride = AccentColor;
        }
        else
        {
            _passportStatusLabel.Text = Loc.GetString("badge-printer-no-passport");
            _passportStatusLabel.FontColorOverride = Color.Gray;
        }

        _ejectPassportButton.Disabled = !state.HasPassport;
        _reprintPassportButton.Disabled = !state.HasPassport;

        var previouslyChecked = new HashSet<string>();
        foreach (var (id, box) in _checkboxes)
        {
            if (box.Pressed)
                previouslyChecked.Add(id);
        }

        _badgeList.RemoveAllChildren();
        _checkboxes.Clear();

        string? currentDepartment = null;
        GridContainer? departmentGrid = null;

        foreach (var option in state.Options)
        {
            if (option.Department != currentDepartment)
            {
                currentDepartment = option.Department;

                var departmentHeader = new PanelContainer
                {
                    HorizontalExpand = true,
                    PanelOverride = new StyleBoxFlat
                    {
                        BackgroundColor = PanelBgLight,
                        BorderColor = AccentColor,
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        ContentMarginLeftOverride = 8,
                        ContentMarginRightOverride = 8,
                        ContentMarginTopOverride = 4,
                        ContentMarginBottomOverride = 4,
                    }
                };
                departmentHeader.AddChild(new Label
                {
                    Text = currentDepartment,
                    FontColorOverride = AccentColor,
                });
                _badgeList.AddChild(departmentHeader);

                departmentGrid = new GridContainer
                {
                    Columns = 2,
                    HorizontalExpand = true,
                    HSeparationOverride = 6,
                    VSeparationOverride = 6,
                };
                _badgeList.AddChild(departmentGrid);
            }

            departmentGrid!.AddChild(BuildBadgeCard(option, previouslyChecked.Contains(option.ProtoId)));
        }
    }

    private Control BuildBadgeCard(BadgePrinterOptionData option, bool isChecked)
    {
        var panel = new PanelContainer
        {
            HorizontalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = PanelBgLight,
                ContentMarginLeftOverride = 6,
                ContentMarginRightOverride = 6,
                ContentMarginTopOverride = 4,
                ContentMarginBottomOverride = 4,
            }
        };

        var card = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 3,
        };

        var header = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
            SeparationOverride = 6,
        };

        var icon = new TextureRect
        {
            Texture = LoadIcon(option.IconRsi, option.IconState),
            TextureScale = new Vector2(2f, 2f),
            VerticalAlignment = VAlignment.Center,
        };

        var checkBox = new CheckBox
        {
            Pressed = isChecked && option.Remaining is not 0,
            VerticalAlignment = VAlignment.Center,
            HorizontalExpand = false,
            MinWidth = 24,
            Disabled = option.Remaining is 0,
        };
        _checkboxes[option.ProtoId] = checkBox;

        header.AddChild(icon);
        header.AddChild(checkBox);
        card.AddChild(header);
        card.AddChild(new Label
        {
            Text = WrapText(option.Name, 28),
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
        });

        if (!string.IsNullOrWhiteSpace(option.Description))
        {
            card.AddChild(new Label
            {
                Text = WrapText(option.Description, 32),
                FontColorOverride = Color.DarkGray,
                HorizontalExpand = true,
            });
        }

        if (!string.IsNullOrWhiteSpace(option.Warning))
        {
            card.AddChild(new Label
            {
                Text = WrapText(option.Warning, 32),
                FontColorOverride = Color.IndianRed,
                HorizontalExpand = true,
            });
        }

        var outOfStock = option.Remaining is 0;

        if (option.Remaining is { } remaining)
        {
            card.AddChild(new Label
            {
                Text = Loc.GetString("badge-printer-remaining", ("count", remaining)),
                FontColorOverride = outOfStock ? Color.IndianRed : AccentColor,
                HorizontalExpand = true,
            });
        }

        panel.AddChild(card);

        return panel;
    }

    private static string WrapText(string text, int maxLineLength)
    {
        var result = new StringBuilder(text.Length + 16);
        var lineLength = 0;

        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (lineLength > 0 && lineLength + word.Length + 1 > maxLineLength)
            {
                result.Append('\n');
                lineLength = 0;
            }
            else if (lineLength > 0)
            {
                result.Append(' ');
                lineLength++;
            }

            result.Append(word);
            lineLength += word.Length;
        }

        return result.ToString();
    }

    private Texture? LoadIcon(string rsiPath, string state)
    {
        try
        {
            var specifier = new SpriteSpecifier.Rsi(new ResPath(rsiPath), state);
            return _entMan.System<SpriteSystem>().Frame0(specifier);
        }
        catch
        {
            return null;
        }
    }
}
