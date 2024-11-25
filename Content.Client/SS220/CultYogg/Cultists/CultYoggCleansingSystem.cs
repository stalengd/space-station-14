using Content.Client.Atmos.Components;
using Content.Shared.Atmos;
using Robust.Client.GameObjects;
using Content.Shared.SS220.CultYogg.Cultists;

namespace Content.Client.SS220.CultYogg.Cultists;

/// <summary>
/// This handles the display of fire effects on flammable entities.
/// </summary>
public sealed class CultYoggCleansingSystem : VisualizerSystem<CultYoggCleansedComponent>
{
    [Dependency] protected readonly AppearanceSystem _appearance = default!;
    [Dependency] protected readonly AnimationPlayerSystem AnimationSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<CultYoggCleansedComponent, ComponentStartup>(OnCompInit);
        //SubscribeLocalEvent<CultYoggCleansedComponent, ComponentShutdown>(OnShutdown);
    }
    private void OnCompInit(Entity<CultYoggCleansedComponent> uid, ref ComponentStartup args)
    {
        //Spawn("EffectHearts", _transform.GetMapCoordinates(uid));//ToDo figure out how to add continous effect

        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp(uid, out AppearanceComponent? appearance))
            return;

        sprite.LayerMapReserveBlank(CultYoggCleansedVisualLayers.Cleanse);
        sprite.LayerSetVisible(CultYoggCleansedVisualLayers.Cleanse, true);
        sprite.LayerSetShader(CultYoggCleansedVisualLayers.Cleanse, "unshaded");

        if (uid.Comp.Sprite != null)
            sprite.LayerSetRSI(CultYoggCleansedVisualLayers.Cleanse, uid.Comp.Sprite);

        //UpdateAppearance(uid, uid.Comp, sprite, appearance);
    }

    private void UpdateAppearance(EntityUid uid, CultYoggCleansedComponent component, SpriteComponent sprite, AppearanceComponent appearance)
    {
        if (!sprite.LayerMapTryGet(CultYoggCleansedVisualLayers.Cleanse, out var index))
            return;

        //_appearance.TryGetData<bool>(uid, FireVisuals.OnFire, out var onFire, appearance);
        //_appearance.TryGetData<float>(uid, FireVisuals.FireStacks, out var fireStacks, appearance);
        sprite.LayerSetVisible(index, true);
    }
    private void OnShutdown(Entity<CultYoggCleansedComponent> uid, ref ComponentShutdown args)
    {
        // Need LayerMapTryGet because Init fails if there's no existing sprite / appearancecomp
        // which means in some setups (most frequently no AppearanceComp) the layer never exists.
        if (TryComp<SpriteComponent>(uid, out var sprite) &&
            sprite.LayerMapTryGet(CultYoggCleansedVisualLayers.Cleanse, out var layer))
        {
            sprite.RemoveLayer(layer);
        }
    }
}
public enum CultYoggCleansedVisualLayers : byte
{
    Cleanse
}
