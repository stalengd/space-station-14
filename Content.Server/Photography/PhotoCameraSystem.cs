// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Interaction;
using Content.Shared.Photography;
using Robust.Shared.Map;

namespace Content.Server.Photography;

public sealed class PhotoCameraSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly PhotoManager _photo = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhotoCameraComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, PhotoCameraComponent component, ActivateInWorldEvent args)
    {
        if (!TryPhoto(uid, component, out var photo))
            return;

        // TODO: cooldown & film charges
    }

    private bool TryPhoto(EntityUid uid, PhotoCameraComponent component, [NotNullWhen(true)] out EntityUid? photoEntity)
    {
        photoEntity = null;

        if (component.FilmLeft <= 0)
            return false;

        if (!TryComp<TransformComponent>(uid, out var xform))
            return false;

        Angle cameraRotation;

        if (xform.GridUid is { } gridToAllignWith)
            cameraRotation = _transform.GetWorldRotation(gridToAllignWith);
        else
            cameraRotation = _transform.GetWorldRotation(xform);

        var id = _photo.TryCapture(xform.MapPosition, cameraRotation, component.SelectedPhotoDimensions);
        if (id is null)
            return false;

        photoEntity = Spawn(component.PhotoPrototypeId, xform.MapPosition);
        var photoComp = EnsureComp<PhotoComponent>(photoEntity.Value);
        photoComp.PhotoID = id;
        Dirty(photoEntity.Value, photoComp);

        return true;
    }
}
