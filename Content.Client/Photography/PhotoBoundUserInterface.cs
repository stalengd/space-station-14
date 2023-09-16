using Content.Client.Photography.UI;

namespace Content.Client.Photography;

public sealed class PhotoBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entityMgr = default!;

    private PhotoWindow? _window;

    public PhotoBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    /// <inheritdoc/>
    protected override void Open()
    {
        base.Open();

        _window = new PhotoWindow();
        _window.OnClose += Close;

        /*
        if (_entityMgr.TryGetComponent<PhotoComponent>(owner, out var photo))
            _window.SetVisuals(photo);
        */

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }
}
