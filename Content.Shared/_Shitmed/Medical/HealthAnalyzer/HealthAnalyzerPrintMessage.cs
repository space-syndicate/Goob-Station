// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Shitmed.Medical.HealthAnalyzer;

/// <summary>
/// Requests a complete printable report for the patient currently scanned by a health analyzer.
/// </summary>
[Serializable, NetSerializable]
public sealed class HealthAnalyzerPrintMessage : BoundUserInterfaceMessage
{
}
