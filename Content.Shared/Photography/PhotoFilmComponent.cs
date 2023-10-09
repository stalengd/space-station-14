namespace Content.Shared.Photography;

[RegisterComponent]
public sealed partial class PhotoFilmComponent : Component
{
    /// <summary>
    /// How many charges of film it adds to a camera when used
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public uint Charges = 10;
}
