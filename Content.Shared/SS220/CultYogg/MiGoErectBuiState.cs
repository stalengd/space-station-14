// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg;

[Serializable, NetSerializable]
public sealed class MiGoErectBuiState : BoundUserInterfaceState
{
    public List<CultYoggBuildingPrototype> Buildings = [];
}

[Serializable, NetSerializable]
public enum MiGoErectUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class MiGoErectBuildingSelectedMessage : BoundUserInterfaceMessage
{
    public ProtoId<CultYoggBuildingPrototype> BuildingId;
}
