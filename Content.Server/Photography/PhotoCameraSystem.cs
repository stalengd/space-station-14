using System.Diagnostics.CodeAnalysis;
using Content.Shared.Interaction;
using Content.Shared.Photography;
using Robust.Shared.Map;

namespace Content.Server.Photography;

public sealed class PhotoCameraSystem : EntitySystem
{
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    [Dependency] private readonly IMapManager _map = default!;
    private PhotoManager _photo = default!;

    private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        IoCManager.InjectDependencies(this);
        _photo = _sysMan.GetEntitySystem<PhotoManager>();
        _transform = _sysMan.GetEntitySystem<SharedTransformSystem>();

        SubscribeLocalEvent<PhotoCameraComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, PhotoCameraComponent component, ActivateInWorldEvent args)
    {
        if (!TryPhoto(uid, component, out var photo))
            return;
    }

    private bool TryPhoto(EntityUid uid, PhotoCameraComponent component, [NotNullWhen(true)] out EntityUid? photoEntity)
    {
        photoEntity = null;

        if (component.FilmLeft <= 0)
            return false;

        if (!TryComp<TransformComponent>(uid, out var xform))
            return false;

        Angle cameraRotation;
        if (_map.TryFindGridAt(xform.MapID, _transform.GetWorldPosition(xform), out var gridEntityToAllignWith, out _))
            cameraRotation = _transform.GetWorldRotation(gridEntityToAllignWith);
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
