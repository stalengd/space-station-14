// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.CultYogg;

[RegisterComponent, NetworkedComponent]

/// <summary>
/// Used to mark object us corrupted for exorcism
/// </summary>
public sealed partial class CultYoggCorruptedComponent : Component
{
    /// <summary>
    /// Prototype ID of the original entity, <see langword="null"/> if none.
    /// </summary>
    public string? PreviousForm;
    /// <summary>
    /// Prototype ID of the corruption reverse effect entity, <see langword="null"/> if none.
    /// </summary>
    public string? CorruptionReverseEffect;
}
