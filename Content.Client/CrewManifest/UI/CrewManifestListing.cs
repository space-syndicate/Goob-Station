// SPDX-License-Identifier: MIT

using Content.Shared.CrewManifest;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.CrewManifest.UI;

public sealed class CrewManifestListing : BoxContainer
{
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IClipboardManager _clipboard = default!;
    private readonly SpriteSystem _spriteSystem;

    // CorvaxGoob Start - crew-manifest-search
    private CrewManifestEntries? _entries;
    private LineEdit? _searchBar;

    public LineEdit? SearchBar
    {
        get => _searchBar;
        set
        {
            if (_searchBar != null)
                _searchBar.OnTextChanged -= OnSearchChanged;

            _searchBar = value;

            if (_searchBar != null)
                _searchBar.OnTextChanged += OnSearchChanged;
        }
    }
    // CorvaxGoob End

    public CrewManifestListing()
    {
        IoCManager.InjectDependencies(this);
        _spriteSystem = _entitySystem.GetEntitySystem<SpriteSystem>();
    }

    // CorvaxGoob Edit Start - crew-manifest-search
    public void AddCrewManifestEntries(CrewManifestEntries entries)
    {
        _entries = entries;
        Refresh();
    }

    private void OnSearchChanged(LineEdit.LineEditEventArgs args)
    {
        Refresh();
    }

    private void Refresh()
    {
        DisposeAllChildren();
        RemoveAllChildren();

        if (_entries == null)
            return;

        var filter = _searchBar?.Text.Trim();
        var entryDict = new Dictionary<DepartmentPrototype, List<CrewManifestEntry>>();
        var hasEntries = false;

        foreach (var entry in _entries.Entries)
        {
            if (!string.IsNullOrEmpty(filter) &&
                !entry.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) &&
                !entry.JobTitle.Contains(filter, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
            {
                // this is a little expensive, and could be better
                if (department.Roles.Contains(entry.JobPrototype))
                {
                    entryDict.GetOrNew(department).Add(entry);
                    hasEntries = true;
                }
            }
        }

        if (!hasEntries && !string.IsNullOrEmpty(filter))
        {
            AddChild(new Label
            {
                Text = Loc.GetString("crew-manifest-no-matches"),
                HorizontalExpand = true
            });
            return;
        }
    // CorvaxGoob End

        var entryList = new List<(DepartmentPrototype section, List<CrewManifestEntry> entries)>();

        foreach (var (section, listing) in entryDict)
        {
            entryList.Add((section, listing));
        }

        entryList.Sort((a, b) => DepartmentUIComparer.Instance.Compare(a.section, b.section));

        foreach (var item in entryList)
        {
            AddChild(new CrewManifestSection(_prototypeManager, _spriteSystem, _clipboard, item.section, item.entries)); // CorvaxGoob-ClipboardManifest
        }
    }
}
