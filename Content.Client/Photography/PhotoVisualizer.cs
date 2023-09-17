using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Photography;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace Content.Client.Photography;

public sealed class PhotoVisualizer : EntitySystem
{
    [Dependency] private readonly IMapManager _mapMan = default!;

    private EyeSystem _eye = default!;
    private SharedTransformSystem _transform = default!;
    private AppearanceSystem _appearance = default!;
    private ISawmill _sawmill = Logger.GetSawmill("photo-visualizer");

    private Dictionary<string, PhotoVisualisation> _currentlyVisualized = new();
    private Dictionary<string, PhotoDataRequestDesc> _demandedPhotos = new();
    private Dictionary<string, PhotoData> _photoDataCache = new();

    public override void Initialize()
    {
        base.Initialize();
        IoCManager.InjectDependencies(this);

        _eye = EntityManager.System<EyeSystem>();
        _transform = EntityManager.System<SharedTransformSystem>();
        _appearance = EntityManager.System<AppearanceSystem>();

        SubscribeNetworkEvent<PhotoDataRequestResponse>(OnPhotoDataReceived);
    }

    private void OnPhotoDataReceived(PhotoDataRequestResponse args)
    {
        if (!_demandedPhotos.TryGetValue(args.Id, out var desc) || desc.State == PhotoDataRequestState.Completed)
            return;

        if (args.Data is not { } data)
        {
            _sawmill.Warning("Received PhotoDataRequestResponse with ID " + args.Id + " but Data is null! A bad request was sent?");
            _demandedPhotos.Remove(args.Id);
            return;
        }

        _photoDataCache.Add(args.Id, data);
        desc.State = PhotoDataRequestState.Completed;

        _sawmill.Info("Received PhotoDataRequestResponse for a photo with ID " + args.Id);

        foreach (var callback in desc.Callbacks)
        {
            try
            {
                callback.Invoke(args.Id);
            }
            catch (Exception e)
            {
                _sawmill.Error($"PhotoData request callback failed! Exception: {e}");
            }
        }

        desc.Callbacks.Clear();
    }

    public bool RequestPhotoData(string id, PhotoDataRequestCallback callback)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        if (_demandedPhotos.TryGetValue(id, out var existingDesc) && existingDesc.State == PhotoDataRequestState.Completed)
        {
            callback.Invoke(id);
            return true;
        }

        var desc = new PhotoDataRequestDesc();
        desc.Callbacks.Add(callback);
        _demandedPhotos.Add(id, desc);

        var ev = new PhotoDataRequest(id);
        RaiseNetworkEvent(ev);

        return true;
    }

    public void DisposePhotoDataRequestCallback(string id, PhotoDataRequestCallback callback)
    {
        if (!_demandedPhotos.TryGetValue(id, out var desc))
            return;

        desc.Callbacks.Remove(callback);
    }

    private void DisposePhotoDataRequest(string id)
    {
        _demandedPhotos.Remove(id);
        _photoDataCache.Remove(id);
    }

    public bool TryGetVisualization(string id, [NotNullWhen(true)] out EyeComponent? eye)
    {
        if (!_photoDataCache.TryGetValue(id, out var data))
        {
            eye = null;
            return false;
        }

        return TryGetVisualization(data, out eye);
    }

    public bool TryGetVisualization(PhotoData data, [NotNullWhen(true)] out EyeComponent? eye)
    {
        if (_currentlyVisualized.TryGetValue(data.Id, out var existingVisualization))
        {
            eye = existingVisualization.Eye;
            return true;
        }

        var mapId = _mapMan.CreateMap();
        _mapMan.AddUninitializedMap(mapId);
        var origin = new MapCoordinates(Vector2.Zero, mapId);

        var camera = Spawn(null, origin);
        eye = EnsureComp<EyeComponent>(camera);
        _eye.SetDrawFov(camera, false, eye);
        _eye.SetZoom(camera, Vector2.One, eye);
        var cameraXform = EnsureComp<TransformComponent>(camera);
        _transform.SetWorldPosition(cameraXform, data.CameraPos);

        foreach (var gridDesc in data.Grids)
        {
            var grid = _mapMan.CreateGrid(mapId);
            var gridXform = EnsureComp<TransformComponent>(grid.Owner);

            _transform.SetWorldPositionRotation(gridXform, gridDesc.Position, gridDesc.Rotation);

            foreach (var (indices, tileType) in gridDesc.Tiles)
            {
                var tile = new Tile(tileType);
                grid.SetTile(indices, tile);
            }
        }

        foreach (var entityDesc in data.Entities)
        {
            var worldPos = entityDesc.PosRot.Item1;

            EntityUid parent;
            if (_mapMan.TryFindGridAt(mapId, entityDesc.PosRot.Item1, out var gridUid, out _))
            {
                parent = gridUid;
            }
            else if (_mapMan.GetMapEntityId(mapId) is { Valid: true } mapEnt)
            {
                parent = mapEnt;
            }
            else
            {
                continue;
            }

            EntityCoordinates coords = new(parent, _transform.GetInvWorldMatrix(parent).Transform(worldPos));

            var entity = Spawn(entityDesc.PrototypeId, coords);
            var xform = EnsureComp<TransformComponent>(entity);

            _transform.SetWorldRotation(xform, entityDesc.PosRot.Item2);

            if (entityDesc.Appearance is not null)
            {
                var appearanceComp = EnsureComp<AppearanceComponent>(entity);
                foreach (var appearanceData in entityDesc.Appearance.Data)
                {
                    _appearance.SetData(entity, appearanceData.Key, appearanceData.Value, appearanceComp);
                }
            }
        }

        var photoVisDesc = new PhotoVisualisation(mapId, camera, origin, eye);
        _currentlyVisualized.Add(data.Id, photoVisDesc);

        return true;
    }

    public void DisposeVisualization(PhotoData data)
    {
        DisposeVisualization(data.Id);
    }

    public void DisposeVisualization(string id)
    {
        if (!_currentlyVisualized.ContainsKey(id))
            return;
    }
}

public delegate void PhotoDataRequestCallback(string id);

internal sealed class PhotoDataRequestDesc
{
    public PhotoDataRequestState State;
    public HashSet<PhotoDataRequestCallback> Callbacks;

    public PhotoDataRequestDesc(PhotoDataRequestState state = PhotoDataRequestState.Awaiting)
    {
        State = state;
        Callbacks = new();
    }
}

internal enum PhotoDataRequestState
{
    Awaiting,
    Completed,
}

public sealed class PhotoVisualisation
{
    public readonly MapId MapId;
    public readonly EntityUid Camera;
    public readonly EyeComponent Eye;
    public readonly MapCoordinates Origin;

    public PhotoVisualisation(MapId mapId, EntityUid camera, MapCoordinates origin, EyeComponent eye)
    {
        MapId = mapId;
        Origin = origin;
        Camera = camera;
        Eye = eye;
    }
}
