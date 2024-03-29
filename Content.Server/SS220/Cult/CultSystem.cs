using System.Linq;
using Content.Server.Storage.EntitySystems;
using Content.Server.Store.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.Cult;
using Content.Shared.Store;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.Cult;

public sealed class CultSystem : SharedCultSystem
{
    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<CultComponent, MapInitEvent>(OnMapInit);
    }
}
