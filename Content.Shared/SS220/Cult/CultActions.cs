// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Cult;

public sealed partial class CultPukeShroomEvent : InstantActionEvent
{
}

public sealed partial class CultCorruptItemEvent : EntityTargetActionEvent
{
}

public sealed partial class CultCorruptItemInHandEvent : InstantActionEvent
{
}

public sealed partial class CultAscendingEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class CultCorruptDoAfterEvent : SimpleDoAfterEvent
{
    public readonly bool InHand;

    public readonly CultCorruptedPrototype? Proto;

    public CultCorruptDoAfterEvent(CultCorruptedPrototype? proto, bool inHand)
    {
        InHand = inHand;
        Proto = proto;
    }
}
