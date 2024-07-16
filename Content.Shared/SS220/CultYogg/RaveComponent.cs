using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared.SS220.CultYogg;

[RegisterComponent, NetworkedComponent]
public sealed partial class RaveComponent : Component
{
    /// <summary>
    /// The random time between incidents, (min, max).
    /// </summary>
    public Vector2 TimeBetweenIncidents = new Vector2(5, 10);

    public float NextIncidentTime;
}
