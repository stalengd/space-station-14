// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Interaction;
using Content.Shared.SS220.Photography;
using Robust.Shared.Map;

namespace Content.Server.SS220.Photography;

public sealed class PhotoCameraSystem : EntitySystem
{
    [Dependency] private readonly PhotoManager _photo = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhotoCameraComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnActivate(Entity<PhotoCameraComponent> entity, ref ActivateInWorldEvent args)
    {
        if (!TryPhoto(entity, out var photo))
            return;

        // TODO: cooldown & film charges
    }

    private bool TryPhoto(Entity<PhotoCameraComponent> entity, [NotNullWhen(true)] out EntityUid? photoEntity)
    {
        photoEntity = null;

        if (entity.Comp.FilmLeft <= 0)
            return false;

        if (!TryComp<TransformComponent>(entity, out var xform))
            return false;

        Angle cameraRotation;

        if (xform.GridUid is { } gridToAllignWith)
            cameraRotation = _transform.GetWorldRotation(gridToAllignWith);
        else
            cameraRotation = _transform.GetWorldRotation(xform);

        var id = _photo.TryCapture(xform.MapPosition, cameraRotation, entity.Comp.SelectedPhotoDimensions);
        if (id is null)
            return false;

        photoEntity = Spawn(entity.Comp.PhotoPrototypeId, xform.MapPosition);
        var photoComp = EnsureComp<PhotoComponent>(photoEntity.Value);
        photoComp.PhotoID = id;
        Dirty(photoEntity.Value, photoComp);

        return true;
    }
}
