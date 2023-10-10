// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.GameTicking;
using Robust.Shared.Map;

namespace Content.Server.SS220.Photography;

public sealed class PhotoReservedMap : EntitySystem
{
    [Dependency] private readonly IMapManager _mapMan = default!;

    public MapId? ReservedMap { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public MapId EnsureMap()
    {
        if (!ReservedMap.HasValue || !_mapMan.MapExists(ReservedMap.Value))
        {
            ReservedMap = _mapMan.CreateMap();
            _mapMan.SetMapPaused(ReservedMap.Value, true);
            _mapMan.AddUninitializedMap(ReservedMap.Value);
        }

        return ReservedMap.Value;
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        if (ReservedMap.HasValue && _mapMan.MapExists(ReservedMap.Value))
            _mapMan.DeleteMap(ReservedMap.Value);
    }
}
