// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.CultYogg;
using Content.Shared.SS220.CultYogg.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.CultYogg;

public sealed partial class CultYoggAltarSystem : SharedCultYoggAltarSystem
{
    [Dependency] SpriteSystem _sprite = default!;
    [Dependency] AppearanceSystem _appearance = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CultYoggAltarComponent, MiGoSacrificeDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(Entity<CultYoggAltarComponent> ent, ref MiGoSacrificeDoAfterEvent args)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearanceComp))
            return;

        UpdateAppearance(ent, ent.Comp, appearanceComp);
    }
}
