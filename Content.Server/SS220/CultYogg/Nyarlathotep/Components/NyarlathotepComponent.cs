// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Content.Server.SS220.CultYogg.Nyarlathotep.EntitySystems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Server.SS220.CultYogg.Nyarlathotep;
using Robust.Shared.Audio;

namespace Content.Server.SS220.CultYogg.Nyarlathotep.Components;


[RegisterComponent, Access(typeof(NyarlathotepSystem))]
public sealed partial class NyarlathotepComponent : Component
{
    [DataField("summonMusic")]
    public SoundSpecifier SummonMusic = new SoundCollectionSpecifier("NukeMusic");//ToDo make own
}
