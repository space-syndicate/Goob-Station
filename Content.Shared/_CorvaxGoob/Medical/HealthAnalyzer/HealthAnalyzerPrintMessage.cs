// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.Medical.HealthAnalyzer;

/// <summary>
/// Requests a printable report for the patient currently scanned by a health analyzer.
/// </summary>
[Serializable, NetSerializable]
public sealed class HealthAnalyzerPrintMessage : BoundUserInterfaceMessage
{
}
