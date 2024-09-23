// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using System.Numerics;
using Content.Shared.SS220.CultYogg.Components;

namespace Content.Server.SS220.CultYogg;

[RegisterComponent, NetworkedComponent]
public sealed partial class RaveComponent : SharedRaveComponent
{
    /// <summary>
    /// The random time between incidents: min, max.
    /// </summary>
    public Vector2 TimeBetweenIncidents = new Vector2(0, 5);

    public TimeSpan MinIntervalPhrase = TimeSpan.FromSeconds(20);

    public TimeSpan MaxIntervalPhrase = TimeSpan.FromSeconds(40);

    public TimeSpan MinIntervalSound = TimeSpan.FromSeconds(20);

    public TimeSpan MaxIntervalSound = TimeSpan.FromSeconds(40);

    public float NextIncidentTime;
}
