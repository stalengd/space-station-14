// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.TextureFade;
using Content.Shared.SS220.CultYogg.Rave;
using Content.Shared.SS220.EntityEffects;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;

namespace Content.Client.SS220.CultYogg.Rave;

public sealed class RaveSystem : SharedRaveSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private readonly EntProtoId _effectPrototype = "CultYoggRaveEffect";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RaveComponent, ComponentAdd>(OnAdded);
        SubscribeLocalEvent<RaveComponent, ComponentRemove>(OnRemoved);
        SubscribeLocalEvent<RaveComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<RaveComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<RaveComponent, OnChemRemoveHallucinationsEvent>(OnVisionCleansed);
    }
    private void OnAdded(Entity<RaveComponent> entity, ref ComponentAdd args)
    {
        if (entity.Owner != _playerManager.LocalEntity)
            return;

        var effectEntity = Spawn(_effectPrototype);
        entity.Comp.EffectEntity = effectEntity;

        if (!TryComp<TextureFadeOverlayComponent>(effectEntity, out var overlay))
            return;

        overlay.IsEnabled = true;
    }
    private void OnVisionCleansed(Entity<RaveComponent> entity, ref OnChemRemoveHallucinationsEvent args)
    {
        if (entity.Comp.EffectEntity is not { } effectEntity)
            return;

        Del(effectEntity);
    }
    private void OnRemoved(Entity<RaveComponent> entity, ref ComponentRemove args)
    {
        if (entity.Comp.EffectEntity is not { } effectEntity)
            return;

        Del(effectEntity);
    }
    private void OnPlayerAttached(Entity<RaveComponent> entity, ref LocalPlayerAttachedEvent args)
    {
        if (entity.Owner != _playerManager.LocalEntity)
            return;

        var effectEntity = Spawn(_effectPrototype);
        entity.Comp.EffectEntity = effectEntity;

        if (!TryComp<TextureFadeOverlayComponent>(effectEntity, out var overlay))
            return;

        overlay.IsEnabled = true;
    }
    private void OnPlayerDetached(Entity<RaveComponent> entity, ref LocalPlayerDetachedEvent args)
    {
        if (entity.Comp.EffectEntity is not { } effectEntity)
            return;

        Del(effectEntity);
    }
}