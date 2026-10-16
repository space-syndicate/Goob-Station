using System.Text;

namespace Content.Goobstation.Shared._Aquila.Xenobiology;

[ByRefEvent]
public readonly record struct SlimeScannerInfoEvent(StringBuilder Text);
