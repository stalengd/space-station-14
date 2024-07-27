// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg;

[RegisterComponent]
public sealed partial class CultYoggAltarComponent : Component
{
    [DataField]
    public int RequiredAmountMiGo = 3;

    [DataField(readOnly: true)]
    public int CurrentlyAmoutMiGo = 0;

    [DataField]
    public float RitualStartRange = 250f;
    [DataField]
    public bool Used = false;

    [Serializable, NetSerializable]
    public enum CultYoggAltarVisuals
    {
        Sacrificed,
    }
}
