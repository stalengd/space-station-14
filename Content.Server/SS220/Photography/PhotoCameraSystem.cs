// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.SS220.Photography;

namespace Content.Server.SS220.Photography;

public sealed class PhotoCameraSystem : EntitySystem
{
    [Dependency] private readonly PhotoManager _photo = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhotoCameraComponent, UseInHandEvent>(OnCameraActivate);
        SubscribeLocalEvent<PhotoFilmComponent, AfterInteractEvent>(OnFilmUse);
        SubscribeLocalEvent<PhotoFilmComponent, ExaminedEvent>(OnFilmExamine);
    }

    private void OnFilmExamine(Entity<PhotoFilmComponent> entity, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("photo-film-component-charges", ("charges", entity.Comp.Charges)));
    }

    private void OnFilmUse(Entity<PhotoFilmComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (args.Target is not { Valid: true } target || !HasComp<PhotoCameraComponent>(target))
            return;

        _charges.AddCharges(target, entity.Comp.Charges);
        EntityManager.DeleteEntity(entity);
    }

    private void OnCameraActivate(Entity<PhotoCameraComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        TryComp<LimitedChargesComponent>(entity, out var charges);
        if (_charges.IsEmpty(entity, charges))
        {
            _popup.PopupEntity(Loc.GetString("photo-camera-component-no-charges-message"), entity, args.User, PopupType.Medium);
            return;
        }

        if (!TryPhoto(entity, out var photo))
            return;

        _charges.UseCharge(entity, charges);
        args.Handled = true;
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
