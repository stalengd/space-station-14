// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Nutrition;
using Content.Shared.Humanoid;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Mind;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Nutrition.EntitySystems;
using System.Diagnostics.CodeAnalysis;


namespace Content.Shared.SS220.Cult;

public abstract class SharedFoodBehaviourSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoodBehaviourComponent, ConsumeDoAfterEvent>(OnDoAfter);
    }
    private void OnDoAfter(Entity<FoodBehaviourComponent> entity, ref ConsumeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        args.Handled = true;

        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
        {
            AnimalCorruption((EntityUid) args.Target);//beast must be transformed
            return;
        }
        if (!_entityManager.TryGetComponent<CultComponent>(args.Target, out var comp))
        {
            //bigure out function to increase amount of consumed shrooms
            return;
        }
    }
    private void AnimalCorruption(EntityUid uid)
    {

    }
    private bool CheckForCorruption(EntityUid uid, [NotNullWhen(true)] out CultCorruptedPrototype? corruption)//if item in list of corrupted
    {
        var idOfEnity = _entityManager.GetComponent<MetaDataComponent>(uid).EntityPrototype!.ID;

        foreach (var entProto in _prototypeManager.EnumeratePrototypes<CultCorruptedPrototype>())//idk if it isn't shitcode
        {
            if (idOfEnity == entProto.ID)
            {
                corruption = entProto;
                return true;
            }
        }
        corruption = null;
        return false;
    }
}
