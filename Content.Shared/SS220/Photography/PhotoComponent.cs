// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Decals;
using Content.Shared.Hands.Components;
using Content.Shared.SS220.Photocopier;
using Content.Shared.StatusEffect;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Photography;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PhotoComponent : Component, IPhotocopyableComponent
{
    /// <summary>
    /// ID of a photo to receive from the server
    /// </summary>
    [DataField, AutoNetworkedField]
    public string PhotoID = "";
    [DataField, AutoNetworkedField]
    public List<string> SeenObjects = new();

    public IPhotocopiedComponentData GetPhotocopiedData()
    {
        return new PhotoPhotocopiedData()
        {
            PhotoID = PhotoID,
            SeenObjects = SeenObjects
        };
    }
}

[Serializable]
public sealed class PhotoPhotocopiedData : IPhotocopiedComponentData
{
    public string PhotoID = "";
    public List<string> SeenObjects = new();

    public void RestoreFromData(EntityUid uid, Component someComponent)
    {
        if (someComponent is not PhotoComponent component)
            return;

        component.PhotoID = PhotoID;
        component.SeenObjects = SeenObjects;

        var entSys = IoCManager.Resolve<IEntityManager>();
        entSys.Dirty(uid, component);
    }
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
    public int? GridIndex = null;
    public Angle Rotation;
    public SpriteState Sprite;
    public PointLightComponentState? PointLight;
    public OccluderComponent.OccluderComponentState? Occluder;

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
public struct SpriteState
{
    public bool GranularLayersRendering;
    public bool Visible;
    public int DrawDepth;
    public Vector2 Scale;
    public Angle Rotation;
    public Vector2 Offset;
    public Color Color;
    public ResPath BaseRSI;
    //public bool OverrideContainerOcclusion; // Not needed
    //public bool ContainerOccluded; // Not needed
    //public ShaderInstance? PostShader; // Problematic
    public bool GetScreenTexture;
    public bool RaiseShaderEvent;
    public uint RenderOrder;
    public List<Layer> Layers = new();
    public Dictionary<string, int> LayerMap = new(); // Partial support

    public SpriteState()
    {
    }

    [Serializable, NetSerializable]
    public struct Layer
    {
        public DirectionOffset DirOffset;
        public string? Shader;
        public string? RsiPath;
        public string? RsiState;
        public Angle Rotation;
        public Vector2 Scale;
        public Vector2 Offset;
        public bool Visible;
        public Color Color;
        public float AnimationTime;
        public LayerRenderingStrategy RenderingStrategy;
        public bool Cycle;
        public PrototypeCopyToShaderParameters? CopyToShaderParameters;
    }

    // Should be mapped exactly to Robust.Client.GameObjects.SpriteComponent.DirectionOffset
    [Serializable, NetSerializable]
    public enum DirectionOffset : byte
    {
        None = 0,
        Clockwise = 1,
        CounterClockwise = 2,
        Flip = 3,
    }
}

[Serializable, NetSerializable]
public sealed class PhotoGridData
{
    public List<(Vector2i, int)> Tiles;
    public Vector2 Position;
    public Angle Rotation;
    public DecalGridState? DecalGridState;

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
    public string Id { get; set; }
    public float PhotoSize { get; }
    public List<PhotoEntityData> Entities { get; }
    public List<PhotoGridData> Grids { get; }
    public Vector2 CameraPosition { get; }
    public Angle CameraRotation { get; }
    public bool Valid { get; } = true;

    public PhotoData(string id, float photoSize, Vector2 cameraPos, Angle cameraRot, bool valid = true)
    {
        Id = id;
        PhotoSize = photoSize;
        CameraPosition = cameraPos;
        CameraRotation = cameraRot;
        Entities = new();
        Grids = new();
        Valid = valid;
    }
}

[Serializable, NetSerializable]
public sealed class PhotoTakeRequest : EntityEventArgs
{
    public readonly NetEntity Camera;
    public readonly PhotoData? Data;

    public PhotoTakeRequest(NetEntity camera, PhotoData? data)
    {
        Camera = camera;
        Data = data;
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
