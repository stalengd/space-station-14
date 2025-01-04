using Content.Server.Afk;
using Content.Shared.SS220.Afk;

namespace Content.Server.SS220.Afk;

public sealed class AfkSystem220 : EntitySystem
{
    [Dependency] private readonly IAfkManager _afkManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<PlayerActivityMessage>(OnActivityMessage);
    }

    private void OnActivityMessage(PlayerActivityMessage message, EntitySessionEventArgs args)
    {
        _afkManager.PlayerDidAction(args.SenderSession);
    }
}
