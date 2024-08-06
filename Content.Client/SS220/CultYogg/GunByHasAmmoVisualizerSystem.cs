using System.Linq;
using Content.Client.Atmos.Components;
using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared.Atmos;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Client.SS220.CultYogg;

/// <summary>
/// This handles the display of fire effects on flammable entities.
/// </summary>
public sealed class GunByHasAmmoVisualizerSystem : VisualizerSystem<GunByHasAmmoVisualsComponent>
{
    [Dependency] private readonly SharedItemSystem _itemSys = default!;
    public override void Initialize()
    {
        base.Initialize();


        SubscribeLocalEvent<GunByHasAmmoVisualsComponent, GetInhandVisualsEvent>(OnGetHeldVisuals, after: new[] { typeof(ItemSystem) });
    }

    protected override void OnAppearanceChange(EntityUid uid, GunByHasAmmoVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!AppearanceSystem.TryGetData<bool>(uid, AmmoVisuals.HasAmmo, out var enabled, args.Component))
            return;

        // Update the item's sprite
        if (args.Sprite != null && component.SpriteLayer != null && args.Sprite.LayerMapTryGet(component.SpriteLayer, out var layer))
        {
            args.Sprite.LayerSetVisible(layer, enabled);
        }

        // update clothing & in-hand visuals.
        _itemSys.VisualsChanged(uid);
    }

    private void OnGetHeldVisuals(EntityUid uid, GunByHasAmmoVisualsComponent component, GetInhandVisualsEvent args)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance)
            || !AppearanceSystem.TryGetData<bool>(uid, AmmoVisuals.HasAmmo, out var enabled, appearance)
            || !enabled)
            return;

        if (!component.InhandVisuals.TryGetValue(args.Location, out var layers))
            return;

        var i = 0;
        var defaultKey = $"inhand-{args.Location.ToString().ToLowerInvariant()}-toggle";
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                key = i == 0 ? defaultKey : $"{defaultKey}-{i}";
                i++;
            }

            args.Layers.Add((key, layer));
        }
    }
}
