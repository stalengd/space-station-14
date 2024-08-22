// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Server.SS220.CultYogg.Components;

[RegisterComponent]
public sealed partial class MiGoReplacementComponent : Component
{
    //
    public bool MayBeReplaced = false;

    //Should the timer count down the time
    public bool ShouldBeCounted = false;

    //Time to replace MiGo
    public float BeforeReplacemetTime = 15;

    public float ReplacementTimer = 0;
}
