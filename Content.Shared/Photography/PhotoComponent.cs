using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Decals;
using Content.Shared.Hands.Components;
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
    public readonly string PrototypeId;
    public Vector2 Position;
    public Angle Rotation;
    public AppearanceComponentState? Appearance;
    public HumanoidAppearanceState? HumanoidAppearance;
    public PointLightComponentState? PointLight;
    public OccluderComponent.OccluderComponentState? Occluder;
    public DamageableComponentState? Damageable;
    public HandsComponentState? Hands;
    public Dictionary<string, string>? Inventory;
    public Dictionary<string, string>? HandsContents;

    public PhotoEntityData(
        string prototypeId,
        Vector2 position,
        Angle rotation)
    {
        PrototypeId = prototypeId;
        Position = position;
        Rotation = rotation;
    }
}

[Serializable, NetSerializable]
public sealed class PhotoGridData
{
    public List<(Vector2i, int)> Tiles;
    public List<Decal> Decals;
    public Vector2 Position;
    public Angle Rotation;

    public PhotoGridData(Vector2 pos, Angle rot)
    {
        Position = pos;
        Rotation = rot;
        Tiles = new();
        Decals = new();
    }
}

[Serializable, NetSerializable]
public sealed class PhotoData
{
    public string Id { get; }
    public Vector2i PhotoSize { get; }
    public List<PhotoEntityData> Entities { get; }
    public List<PhotoGridData> Grids { get; }
    public Vector2 CameraPosition { get; }
    public Angle CameraRotation { get; }

    public PhotoData(string id, Vector2i photoSize, Vector2 cameraPos, Angle cameraRot)
    {
        Id = id;
        PhotoSize = photoSize;
        CameraPosition = cameraPos;
        CameraRotation = cameraRot;
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
