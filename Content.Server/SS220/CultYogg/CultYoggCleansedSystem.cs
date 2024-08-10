// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.SS220.CultYogg.EntitySystems;
using Robust.Shared.Random;

namespace Content.Server.SS220.CultYogg;

public sealed class CultYoggCleansedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        //ToDo set time before it deviate
        var query = EntityQueryEnumerator<CultYoggCleansedComponent>();
        while (query.MoveNext(out var uid, out var cleansedComp))
        {
            cleansedComp.BeforeDeclinesTime -= frameTime;

            if (cleansedComp.AmountOfHolyWater >= cleansedComp.AmountToCleance)
                RemComp<CultYoggComponent>(uid);

            if (cleansedComp.BeforeDeclinesTime > 0)
                continue;

            RemComp<CultYoggCleansedComponent>(uid);
        }
    }
}
