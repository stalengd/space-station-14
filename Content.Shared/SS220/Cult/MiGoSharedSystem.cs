// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;

namespace Content.Shared.SS220.Cult;

public abstract class SharedMiGoSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiGoComponent, ComponentStartup>(OnCompInit);

        // actions
        SubscribeLocalEvent<MiGoComponent, MiGoEnslavementEvent>(Enslave);
        SubscribeLocalEvent<MiGoComponent, MiGoAstralEvent>(MiGoAstral);
        SubscribeLocalEvent<MiGoComponent, MiGoHealEvent>(MiGoHeal);
        SubscribeLocalEvent<MiGoComponent, MiGoErectEvent>(MiGoErect);
        SubscribeLocalEvent<MiGoComponent, MiGoSacrificeEvent>(MiGoSacrifice);
    }

    protected virtual void OnCompInit(EntityUid uid, MiGoComponent comp, ComponentStartup args)
    {

        _actions.AddAction(uid, ref comp.MiGoEnslavementActionEntity, comp.MiGoEnslavementAction);
        _actions.AddAction(uid, ref comp.MiGoAstralActionEntity, comp.MiGoAstralAction);
    }

    private void Enslave(EntityUid uid, MiGoComponent comp, MiGoEnslavementEvent args)
    {

    }

    private void MiGoAstral(EntityUid uid, MiGoComponent comp, MiGoAstralEvent args)
    {
        //ToDo https://github.com/TheArturZh/space-station-14/blob/b0ee614751216474ddbeabab970b3ab505f63845/Content.Shared/SS220/DarkReaper/DarkReaperSharedSystem.cs#L4
    }
    private void MiGoHeal(EntityUid uid, MiGoComponent comp, MiGoHealEvent args)
    {

    }
    private void MiGoErect(EntityUid uid, MiGoComponent comp, MiGoErectEvent args)
    {

    }
    private void MiGoSacrifice(EntityUid uid, MiGoComponent comp, MiGoSacrificeEvent args)
    {

    }
}
