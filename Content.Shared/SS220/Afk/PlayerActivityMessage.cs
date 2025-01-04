using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Afk;

/// <summary>
/// "Hey, I am not AFK!" type of message.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlayerActivityMessage : EntityEventArgs;
