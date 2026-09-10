// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Paper;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Paper.UI;

/// <summary>
/// Handles insert helper data and text insertion.
/// </summary>
public sealed partial class PaperWindow
{
    private const int ManifestClosedTextMaxLength = 24;

    /// <summary>
    /// Prevents invalid time/date data from crashing the UI.
    /// The server sends the values, but the client still clamps unsafe deltas before formatting.
    /// </summary>
    private static readonly TimeSpan MaxClockAdvance = TimeSpan.FromDays(30);

    [Dependency] private IGameTiming _gameTiming = default!;

    public void UpdateInsertData(PaperComponent.PaperInsertDataMessage data)
    {
        _insertData = data;

        _insertStationButton.Disabled = false;
        _insertTimeDateButton.Disabled = false;
        _insertOwnNameButton.Disabled = string.IsNullOrWhiteSpace(data.OwnName);
        _insertOwnJobButton.Disabled = string.IsNullOrWhiteSpace(data.OwnJob);

        _manifestOptionButton.Clear();

        if (data.ManifestEntries.Length == 0)
        {
            AddManifestPlaceholder();
            _manifestOptionButton.Disabled = true;
            _insertManifestNameButton.Disabled = true;
            _insertManifestJobButton.Disabled = true;
            return;
        }

        for (var i = 0; i < data.ManifestEntries.Length; i++)
        {
            var entry = data.ManifestEntries[i];
            _manifestOptionButton.AddItem($"{entry.Name} - {entry.JobTitle}", i);
        }

        _manifestOptionButton.SelectId(0);
        UpdateManifestOptionDisplayText();
        _manifestOptionButton.Disabled = false;
        _insertManifestNameButton.Disabled = false;
        _insertManifestJobButton.Disabled = false;
    }

    private void ClearInsertData()
    {
        _insertData = null;
        _insertStationButton.Disabled = true;
        _insertTimeDateButton.Disabled = true;
        _insertOwnNameButton.Disabled = true;
        _insertOwnJobButton.Disabled = true;
        _manifestOptionButton.Clear();
        AddManifestPlaceholder();
        _manifestOptionButton.Disabled = true;
        _insertManifestNameButton.Disabled = true;
        _insertManifestJobButton.Disabled = true;
    }

    private void AddManifestPlaceholder()
    {
        _manifestOptionButton.AddItem(Loc.GetString("paper-insert-helper-manifest-placeholder"), 0);
        UpdateManifestOptionDisplayText();
    }

    private void UpdateManifestOptionDisplayText()
    {
        if (_manifestOptionLabel == null)
            return;

        var text = Loc.GetString("paper-insert-helper-manifest-placeholder");
        if (_insertData != null &&
            _insertData.ManifestEntries.Length > 0 &&
            _manifestOptionButton.SelectedId >= 0 &&
            _manifestOptionButton.SelectedId < _insertData.ManifestEntries.Length)
        {
            var entry = _insertData.ManifestEntries[_manifestOptionButton.SelectedId];
            text = $"{entry.Name} - {entry.JobTitle}";
        }

        _manifestOptionLabel.Text = EllipsizeText(text, ManifestClosedTextMaxLength);
    }

    private static string EllipsizeText(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        if (maxLength <= 3)
            return new string('.', maxLength);

        return text[..(maxLength - 3)].TrimEnd() + "...";
    }

    private static Label? FindFirstLabel(Control control)
    {
        foreach (var child in control.Children)
        {
            if (child is Label label)
                return label;

            var nested = FindFirstLabel(child);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private PaperComponent.PaperInsertManifestEntry? GetSelectedManifestEntry()
    {
        if (_insertData == null ||
            _insertData.ManifestEntries.Length == 0 ||
            _manifestOptionButton.SelectedId < 0 ||
            _manifestOptionButton.SelectedId >= _insertData.ManifestEntries.Length)
        {
            return null;
        }

        return _insertData.ManifestEntries[_manifestOptionButton.SelectedId];
    }

    private void InsertHelperText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        Input.InsertAtCursor(text);
        Input.GrabKeyboardFocus();
        UpdateFillState();
    }

    private string FormatInsertHelperTimeDate(PaperComponent.PaperInsertDataMessage data)
    {
        var elapsed = _gameTiming.CurTime - data.GameTime;
        if (elapsed < TimeSpan.Zero || elapsed > MaxClockAdvance)
            elapsed = TimeSpan.Zero;

        var shiftTime = data.ShiftTime + elapsed;
        var serverDate = GetSafeServerDate(data).Add(elapsed);

        return $"{shiftTime:hh\\:mm\\:ss}, {serverDate.Day:00}.{serverDate.Month:00}.{serverDate.Year + 1000:0000}";
    }

    private static DateTime GetSafeServerDate(PaperComponent.PaperInsertDataMessage data)
    {
        // The server sends valid values, but clamping here keeps the UI resilient to old servers,
        // corrupted packets, or test harnesses that construct messages manually.
        var year = Math.Clamp(data.ServerYear, 1, 8999);
        var month = Math.Clamp(data.ServerMonth, 1, 12);
        var day = Math.Clamp(data.ServerDay, 1, DateTime.DaysInMonth(year, month));

        return new DateTime(year, month, day);
    }
}
