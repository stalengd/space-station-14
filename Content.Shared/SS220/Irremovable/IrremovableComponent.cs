// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Irremovable;

[RegisterComponent]
public sealed partial class IrremovableComponent : Component
{
    [DataField]
    public bool InHandItem = false;
    [DataField]
    public bool ShouldDropOnDeath = true;
}
