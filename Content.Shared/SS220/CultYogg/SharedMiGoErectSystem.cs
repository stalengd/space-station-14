using Content.Shared.Emag.Systems;
using Content.Shared.Popups;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Wires;

namespace Content.Shared.SS220.CultYogg;

public abstract class SharedMiGoErectSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
    }
}
