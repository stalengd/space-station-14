using Content.Client.Photography.UI;
using Content.Shared.Photography;

namespace Content.Client.Photography;

public sealed class PhotoBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entityMgr = default!;

    private PhotoWindow? _window;
    private readonly PhotoVisualizer _photoVisualizer;
    private PhotoEyeRequest? _photoEyeRequest;

    public PhotoBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _photoVisualizer = _entityMgr.System<PhotoVisualizer>();
        _initCallback = new(OnPhotoDataReceived);
        _disposeCallback = new(OnVisualizationDisposed);
    }

    private PhotoEyeRequest.OnVisualizationInitCallback _initCallback;
    private PhotoEyeRequest.OnDisposeCallback _disposeCallback;

    private void OnPhotoDataReceived(EyeComponent eye)
    {
        _window?.SetVisuals(eye);
    }

    private void OnVisualizationDisposed()
    {
        _window?.SetVisuals(null);
        _photoEyeRequest = null;
    }

    /// <inheritdoc/>
    protected override void Open()
    {
        base.Open();

        _window = new PhotoWindow();
        _window.OnClose += Close;

        if (_entityMgr.TryGetComponent<PhotoComponent>(Owner, out var photo))
        {
            _photoEyeRequest = new(photo.PhotoID)
            {
                OnVisualizationInit = _initCallback,
                OnDispose = _disposeCallback
            };
            _photoVisualizer.RequestPhotoEye(_photoEyeRequest);
        }

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _window?.Dispose();
            _photoEyeRequest?.Dispose();
        }
    }
}
