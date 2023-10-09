using Robust.Shared.Prototypes;

namespace Content.Shared.Photography;

[RegisterComponent]
public sealed partial class PhotoCameraComponent : Component
{
    /// <summary>
    /// How many film charges can be loaded into camera
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public uint MaxFilm = 10;

    /// <summary>
    /// How many film charges left in camera (How many photos can be made until reloading is required)
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public uint FilmLeft = 10;

    /// <summary>
    /// Dimensions of a photo, in tiles. X is Width, Y is Height.
    /// </summary>
    [DataField]
    public Vector2i SelectedPhotoDimensions = new(5, 5);

    /// <summary>
    /// Available dimensions of a photo, can be switched via context menu of a camera.
    /// </summary>
    [DataField]
    public Vector2i[] AvailablePhotoDimensions = {
        new(3,3),
        new(4,4),
        new(5,5),
        new(6,6),
        new(7,7)
    };

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan PrintingTime = TimeSpan.FromSeconds(3);

    [DataField]
    public ProtoId<EntityPrototype> PhotoPrototypeId = "Photo";

    /// <summary>
    /// The time photo was taken at, is used to find out whether camera finished printing or not
    /// </summary>
    public TimeSpan? TimePhotoTakenAt;
}
