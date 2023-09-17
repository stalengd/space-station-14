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
public sealed class EntityData
{
    public string PrototypeId { get; }
    public AppearanceComponentState? Appearance { get; }

    public EntityData(string prototypeId, AppearanceComponentState? appearance = null)
    {
        PrototypeId = prototypeId;
        Appearance = appearance;
    }
}

[Serializable, NetSerializable]
public sealed class PhotoData
{
    public string Id { get; }
    public Vector2i PhotoSize { get; }
    public HashSet<EntityData> Entities { get; }

    public PhotoData(string id, Vector2i photoSize)
    {
        Id = id;
        PhotoSize = photoSize;
        Entities = new();
    }

    public void AddEntity(EntityData entityData)
    {
        Entities.Add(entityData);
    }
}
