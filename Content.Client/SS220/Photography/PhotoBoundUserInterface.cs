// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.Photography.UI;
using Content.Shared.SS220.Photography;

namespace Content.Client.SS220.Photography;

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

    private void OnPhotoDataReceived(EyeComponent eye, float size)
    {
        _window?.SetVisuals(eye, size);
    }

    private void OnVisualizationDisposed()
    {
        _window?.SetVisuals(null, 5);
        _photoEyeRequest = null;
    }

    /// <inheritdoc/>
    protected override void Open()
    {
        base.Open();

        _window = new PhotoWindow();
        _window.OnClose += Close;
        _window.ScreenshotComplete += () =>
        {
            _photoEyeRequest?.Dispose();
        };

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
