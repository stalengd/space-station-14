using System;
using System.Linq;
using Content.Server.Storage.EntitySystems;
using Content.Server.Store.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.Cult;
using Content.Shared.Store;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Server.Actions;

namespace Content.Server.SS220.Cult;

public sealed class CultSystem : SharedCultSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    public override void Initialize()
    {
        base.Initialize();

       //SubscribeLocalEvent<CultComponent, ComponentStartup>(OnCompInit);
        //SubscribeLocalEvent<CultComponent, MapInitEvent>(OnMapInit);
    }
    protected override void OnCompInit(EntityUid uid, CultComponent comp, ComponentStartup args)
    {
        base.OnCompInit(uid, comp, args);

        _actions.AddAction(uid, ref comp.PukeShroomActionEntity, comp.PukeShroomAction);
    }
}
