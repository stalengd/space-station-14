using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Photography;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PhotoComponent : Component
{
    /// <summary>
    /// ID of a photo to receive from the server
    /// </summary>
    [DataField, AutoNetworkedField]
    public string PhotoID = "";
}

[Serializable, NetSerializable]
public enum PhotoUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class PhotoEntityData
{
    public string PrototypeId { get; }
    public (Vector2, Angle) PosRot { get; }
    public AppearanceComponentState? Appearance { get; }
    public HumanoidAppearanceState? HumanoidAppearance { get; }
    public PointLightComponentState? PointLight { get; }
    public OccluderComponent.OccluderComponentState? Occluder { get; }
    public DamageableComponentState? Damageable { get; }

    public PhotoEntityData(
        string prototypeId,
        (Vector2, Angle) posrot,
        AppearanceComponentState? appearance = null,
        HumanoidAppearanceState? humanoidAppearance = null,
        PointLightComponentState? pointLight = null,
        OccluderComponent.OccluderComponentState? occluder = null,
        DamageableComponentState? damageable = null)
    {
        PrototypeId = prototypeId;
        Appearance = appearance;
        PosRot = posrot;
        HumanoidAppearance = humanoidAppearance;
        PointLight = pointLight;
        Occluder = occluder;
        Damageable = damageable;
    }
}

[Serializable, NetSerializable]
public sealed class PhotoGridData
{
    public List<(Vector2i, int)> Tiles;
    public Vector2 Position;
    public Angle Rotation;

    public PhotoGridData(Vector2 pos, Angle rot)
    {
        Position = pos;
        Rotation = rot;
        Tiles = new();
    }
}

[Serializable, NetSerializable]
public sealed class PhotoData
{
    public string Id { get; }
    public Vector2i PhotoSize { get; }
    public HashSet<PhotoEntityData> Entities { get; }
    public HashSet<PhotoGridData> Grids { get; }
    public Vector2 CameraPos { get; }

    public PhotoData(string id, Vector2i photoSize, Vector2 cameraPos)
    {
        Id = id;
        PhotoSize = photoSize;
        CameraPos = cameraPos;
        Entities = new();
        Grids = new();
    }
}

[Serializable, NetSerializable]
public sealed class PhotoDataRequest : EntityEventArgs
{
    public string Id { get; }

    public PhotoDataRequest(string id)
    {
        Id = id;
    }
}

[Serializable, NetSerializable]
public sealed class PhotoDataRequestResponse : EntityEventArgs
{
    public readonly PhotoData? Data;
    public readonly string Id;

    public PhotoDataRequestResponse(PhotoData? data, string id)
    {
        Data = data;
        Id = id;
    }
}
