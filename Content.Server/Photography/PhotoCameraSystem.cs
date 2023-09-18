using System.Diagnostics.CodeAnalysis;
using Content.Shared.Interaction;
using Content.Shared.Photography;

namespace Content.Server.Photography;

public sealed class PhotoCameraSystem : EntitySystem
{
    private PhotoManager _photoManager = default!;

    private const string PHOTO_PROTO_ID = "Photo";

    public override void Initialize()
    {
        base.Initialize();
        _photoManager = EntityManager.System<PhotoManager>();

        SubscribeLocalEvent<PhotoCameraComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, PhotoCameraComponent component, ActivateInWorldEvent args)
    {
        if (!TryPhoto(uid, component, out var photo))
            return;
    }

    private bool TryPhoto(EntityUid uid, PhotoCameraComponent component, [NotNullWhen(true)] out EntityUid? photoEntity)
    {
        // gib me entity
        photoEntity = null;

        if (component.FilmLeft <= 0)
            return false;

        if (!TryComp<TransformComponent>(uid, out var xform))
            return false;

        // capture
        var id = _photoManager.TryCapture(xform, component.SelectedPhotoDimensions);
        if (id is null)
            return false;

        photoEntity = Spawn(PHOTO_PROTO_ID, xform.MapPosition);
        var photoComp = EnsureComp<PhotoComponent>(photoEntity.Value);
        photoComp.PhotoID = id;
        Dirty(photoEntity.Value, photoComp);

        return true;
    }
}
