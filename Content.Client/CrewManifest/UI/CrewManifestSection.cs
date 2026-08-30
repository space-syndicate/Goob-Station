// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.CrewManifest;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using System.Numerics;
using Content.Shared.Roles;
using Robust.Client.UserInterface;

namespace Content.Client.CrewManifest.UI;

public sealed class CrewManifestSection : BoxContainer
{
    public CrewManifestSection(
        IPrototypeManager prototypeManager,
        SpriteSystem spriteSystem,
        IClipboardManager clipboardManager, // CorvaxGoob-ClipboardManifest
        DepartmentPrototype section,
        List<CrewManifestEntry> entries)
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;

        AddChild(new Label()
        {
            StyleClasses = { "LabelBig" },
            Text = Loc.GetString(section.Name)
        });

        var gridContainer = new GridContainer()
        {
            HorizontalExpand = true,
            Columns = 2
        };

        AddChild(gridContainer);

        foreach (var entry in entries)
        {
            var name = new RichTextLabel();

            // CorvaxGoob-Start-ClipboardManifest
            var containerButtonName = new ContainerButton()
            {
                HorizontalExpand = true
            };

            var containerButtonTitle = new ContainerButton()
            {
                HorizontalExpand = true
            };
            // CorvaxGoob-End

            name.SetMessage(entry.Name);

            var titleContainer = new BoxContainer()
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true
            };

            var title = new RichTextLabel();
            title.SetMessage(entry.JobTitle);


            if (prototypeManager.TryIndex<JobIconPrototype>(entry.JobIcon, out var jobIcon))
            {
                var icon = new TextureRect()
                {
                    TextureScale = new Vector2(2, 2),
                    VerticalAlignment = VAlignment.Center,
                    Texture = spriteSystem.Frame0(jobIcon.Icon),
                    Margin = new Thickness(0, 0, 4, 0)
                };

                titleContainer.AddChild(icon);
                titleContainer.AddChild(title);
            }
            else
            {
                titleContainer.AddChild(title);
            }

            // CorvaxGoob-Start-ClipboardManifest

            containerButtonName.OnButtonDown += (args) =>
            {
                if (name.Text is not null)
                    clipboardManager.SetText(name.Text);
            };

            containerButtonTitle.OnButtonDown += (args) =>
            {
                if (title.Text is not null)
                    clipboardManager.SetText(title.Text);
            };

            containerButtonName.AddChild(name);
            containerButtonTitle.AddChild(titleContainer);

            gridContainer.AddChild(containerButtonName);
            gridContainer.AddChild(containerButtonTitle);
            // CorvaxGoob-End-ClipboardManifest
        }
    }

    private void ContainerButtonName_OnButtonDown(BaseButton.ButtonEventArgs obj)
    {
        throw new NotImplementedException();
    }
}
