using Content.Shared.GameTicking;
using Content.Shared.Photography;

namespace Content.Client.Photography;

public sealed partial class PhotoVisualizer : EntitySystem
{
    private Dictionary<string, HashSet<PhotoEyeRequest>> _eyeRequests = new();
    private Dictionary<string, PhotoData> _photoDataCache = new();

    private void InitializeCache()
    {
        SubscribeNetworkEvent<PhotoDataRequestResponse>(OnPhotoDataReceived);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        foreach (var (id, requests) in _eyeRequests)
        {
            foreach (var request in requests)
                request.Dispose(); // slower than could be but not too bad
        }
    }

    private void OnPhotoDataReceived(PhotoDataRequestResponse args)
    {
        if (args.Data is not { } data)
            return;

        if (!_eyeRequests.TryGetValue(data.Id, out var requestsSet) || requestsSet.Count == 0)
            return;

        _photoDataCache.Add(data.Id, data);
        if (TryGetVisualization(data, out var eye))
        {
            foreach (var request in requestsSet)
            {
                request.OnVisualizationInit?.Invoke(eye);
            }
        }
    }

    public void ForgetRequestAndCleanup(PhotoEyeRequest request)
    {
        if (!_eyeRequests.TryGetValue(request.PhotoId, out var requestSet))
            return;

        requestSet.Remove(request);
        DereferenceIfGargabe(request.PhotoId);
    }

    private void DereferenceIfGargabe(string id)
    {
        if (!_eyeRequests.TryGetValue(id, out var requestSet))
            return;

        if (requestSet.Count == 0)
        {
            DisposeVisualization(id);
            _photoDataCache.Remove(id);
            _eyeRequests.Remove(id);
        }
    }

    public void RequestPhotoEye(PhotoEyeRequest request)
    {
        if (!_eyeRequests.TryGetValue(request.PhotoId, out var requestSet))
        {
            requestSet = new();
            _eyeRequests.Add(request.PhotoId, requestSet);
        }

        requestSet.Add(request);

        if (_currentlyVisualized.TryGetValue(request.PhotoId, out var photoVis))
        {
            request.OnVisualizationInit?.Invoke(photoVis.Eye);
            return;
        }

        if (_photoDataCache.TryGetValue(request.PhotoId, out var photoData))
        {
            if (TryGetVisualization(photoData, out var eye))
                request.OnVisualizationInit?.Invoke(eye);
            return;
        }

        var ev = new PhotoDataRequest(request.PhotoId);
        RaiseNetworkEvent(ev);
    }
}

public sealed class PhotoEyeRequest
{
    public readonly string PhotoId;
    public OnVisualizationInitCallback? OnVisualizationInit;
    public OnDisposeCallback? OnDispose;

    public PhotoEyeRequest(string photoId)
    {
        PhotoId = photoId;
    }

    public void Dispose()
    {
        OnDispose?.Invoke();
        var sysMan = IoCManager.Resolve<IEntitySystemManager>();
        var photoVisualizer = sysMan.GetEntitySystem<PhotoVisualizer>();
        photoVisualizer.ForgetRequestAndCleanup(this);
    }

    public delegate void OnVisualizationInitCallback(EyeComponent eye);
    public delegate void OnDisposeCallback();
}
