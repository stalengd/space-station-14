using Content.Client.Photography.UI;
using Content.Shared.Photography;

namespace Content.Client.Photography;

public sealed class PhotoBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entityMgr = default!;

    private PhotoWindow? _window;
    private readonly PhotoVisualizer _photoVisualizer;
    private string? _photoId;

    public PhotoBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _photoVisualizer = _entityMgr.System<PhotoVisualizer>();
        _callback = new(OnPhotoDataReceived);
    }

    private PhotoDataRequestCallback _callback;

    private void OnPhotoDataReceived(string id)
    {
        Logger.DebugS("PHOTO", "DEETA RECEIVED!");
        if (_photoVisualizer.TryGetVisualization(id, out var eye))
        {
            Logger.DebugS("PHOTO", "EYE RECEIVED!");
            _window?.SetVisuals(eye);
        }
    }

    /// <inheritdoc/>
    protected override void Open()
    {
        base.Open();

        _window = new PhotoWindow();
        _window.OnClose += Close;

        if (_entityMgr.TryGetComponent<PhotoComponent>(Owner, out var photo))
        {
            _photoId = photo.PhotoID;
            _photoVisualizer.RequestPhotoData(photo.PhotoID, _callback);
        }

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _window?.Dispose();
            if (_photoId is not null)
                _photoVisualizer.DisposePhotoDataRequestCallback(_photoId, _callback);
        }
    }
}
