// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.SS220.CultYogg.EntitySystems;
using Content.Server.Actions;
using Content.Server.Polymorph.Systems;
using Content.Shared.Popups;
using Content.Server.SS220.GameTicking.Rules;
using Content.Shared.SS220.Telepathy;
using static Content.Shared.Administration.Notes.AdminMessageEuiState;
using Content.Server.SS220.Telepathy;

namespace Content.Server.SS220.CultYogg;

public sealed class MiGoSystem : SharedMiGoSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly TelepathySystem _telepathySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiGoComponent, MiGoEnslaveDoAfterEvent>(MiGoEnslaveOnDoAfter);
    }
    private void MiGoEnslaveOnDoAfter(Entity<MiGoComponent> uid, ref MiGoEnslaveDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        //ToDo Remove clients effects
        var ev = new CultYoggEnslavedEvent(args.Target);
        RaiseLocalEvent(uid, ref ev);

        args.Handled = true;
    }

    public void MarkupForReplacement(EntityUid uid, MiGoComponent comp, bool isMarkedToReplace)
    {
        comp.MayBeReplaced = isMarkedToReplace;

        if (TryComp<TelepathyComponent>(uid, out var telepathy))
            return;

        if (telepathy == null)
            return;

        var meta = EnsureComp<MetaDataComponent>(uid);

        //sending other cultists informing message
        if (isMarkedToReplace)
            RaiseLocalEvent(uid, new TelepathyAnnouncementSendEvent() { Message = Loc.GetString("cult-yogg-migo-can-replace", ("name", meta.EntityName)), TelepathyChannel = telepathy.TelepathyChannelPrototype });
        else
            RaiseLocalEvent(uid, new TelepathyAnnouncementSendEvent() { Message = Loc.GetString("cult-yogg-migo-cancel-replace", ("name", meta.EntityName)), TelepathyChannel = telepathy.TelepathyChannelPrototype });
    }
}
