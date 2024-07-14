// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.StatusIcon;
using Content.Shared.Antag;
using Content.Shared.Roles;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;


namespace Content.Shared.SS220.CultYogg;

/// <summary>
/// Used to markup cult's sacrifice targets
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CultYoggSacrificialComponent : Component
{
    /// <summary>
    /// Icon
    /// </summary>
    [DataField]
    public bool IconVisibleToGhost { get; set; } = true;

    [DataField]
    public ProtoId<StatusIconPrototype> StatusIcon = "CultYoggSacraficialTarget";
}
