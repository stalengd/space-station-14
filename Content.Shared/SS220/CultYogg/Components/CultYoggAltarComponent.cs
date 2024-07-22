// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.CultYogg;

[RegisterComponent]
public sealed partial class CultYoggAltarComponent : Component
{
    [DataField]
    public int RequiredAmountMiGo = 3;
    [DataField(readOnly: true)]
    public int? CurrentlyAmoutMiGo;
    [DataField]
    public float RitualStartRange = 250f;
}
