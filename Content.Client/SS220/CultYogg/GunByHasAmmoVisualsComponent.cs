using Content.Shared.Hands.Components;

namespace Content.Client.SS220.CultYogg;

/// <summary>
/// Sets which sprite RSI is used for displaying the fire visuals and what state to use based on the fire stacks
/// accumulated.
/// </summary>
[RegisterComponent]
public sealed partial class GunByHasAmmoVisualsComponent : Component
{
    /// <summary>
    ///     Sprite layer that will have its visibility toggled when this item is toggled.
    /// </summary>
    [DataField("spriteLayer")]
    public string? SpriteLayer = "light";

    /// <summary>
    ///     Layers to add to the sprite of the player that is holding this entity (while the component is toggled on).
    /// </summary>
    [DataField("inhandVisuals")]
    public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();

}
